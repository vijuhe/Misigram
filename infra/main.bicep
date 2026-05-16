@description('Base name used for all resources (e.g. misigram).')
@minLength(3)
param appName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('SQL Server administrator login.')
param sqlAdminLogin string

@description('SQL Server administrator password.')
@secure()
param sqlAdminPassword string

@description('Google OAuth Client ID.')
@secure()
param googleClientId string

@description('Google OAuth Client Secret.')
@secure()
param googleClientSecret string

@description('Google accounts allowed to log in.')
param allowedGoogleAccounts array

// ── SQL Server ────────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: '${appName}-sql'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
}

resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: '${appName}-db'
  location: location
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    autoPauseDelay: 60
    minCapacity: 1
  }
}

// ── Storage Account ───────────────────────────────────────────────────────────

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: appName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-04-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: ['https://${appName}.${containerAppsEnv.properties.defaultDomain}']
          allowedMethods: ['GET', 'HEAD']
          allowedHeaders: ['*']
          exposedHeaders: ['*']
          maxAgeInSeconds: 3600
        }
      ]
    }
  }
}

resource mediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  parent: blobService
  name: 'media'
  properties: {
    publicAccess: 'None'
  }
}

// ── Container Registry ────────────────────────────────────────────────────────

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: '${appName}acr'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// ── Container Apps ────────────────────────────────────────────────────────────

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${appName}-env'
  location: location
  properties: {
    zoneRedundant: false
  }
}

var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

var storageConnection = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'

var allowedAccountEnvVars = [for (email, i) in allowedGoogleAccounts: {
  name: 'AllowedGoogleAccounts__${i}'
  value: email
}]

var baseEnvVars = [
  { name: 'ASPNETCORE_ENVIRONMENT',                   value: 'Production' }
  { name: 'ConnectionStrings__DefaultConnection',     secretRef: 'sql-connection' }
  { name: 'BlobStorage__Connection',                  secretRef: 'storage-connection' }
  { name: 'BlobStorage__ContainerName',               value: 'media' }
  { name: 'Authentication__Google__ClientId',          secretRef: 'google-client-id' }
  { name: 'Authentication__Google__ClientSecret',      secretRef: 'google-client-secret' }
  { name: 'AllowedOrigins',                            value: 'https://${appName}.${containerAppsEnv.properties.defaultDomain}' }
]

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: [
        { name: 'sql-connection',       value: sqlConnectionString }
        { name: 'storage-connection',   value: storageConnection }
        { name: 'google-client-id',     value: googleClientId }
        { name: 'google-client-secret', value: googleClientSecret }
        { name: 'acr-password',         value: acr.listCredentials().passwords[0].value }
      ]
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: appName
          // Placeholder for initial provisioning; deploy.ps1 updates this to the real ACR image
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: concat(baseEnvVars, allowedAccountEnvVars)
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
      }
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output appUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output storageAccountName string = storageAccount.name
output acrLoginServer string = acr.properties.loginServer
output acrName string = acr.name
