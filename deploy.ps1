param(
    [Parameter(Mandatory)]
    [string]$ResourceGroup,

    [switch]$SkipInfra
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

# ── Infrastructure ─────────────────────────────────────────────────────────────

if (-not $SkipInfra) {
    Write-Host "==> Deploying Azure infrastructure..." -ForegroundColor Cyan

    $result = az deployment group create `
        --resource-group $ResourceGroup `
        --template-file "$Root\infra\main.bicep" `
        --parameters "$Root\infra\main.bicepparam" `
        --output json | ConvertFrom-Json

    if (-not $?) { Write-Host "Infrastructure deployment failed." -ForegroundColor Red; exit 1 }

    $appUrl     = $result.properties.outputs.appUrl.value
    $acrName    = $result.properties.outputs.acrName.value
    $acrServer  = $result.properties.outputs.acrLoginServer.value
    $appName    = ($result.properties.outputs.storageAccountName.value)  # same as appName param

    Write-Host "    App URL    : $appUrl"    -ForegroundColor DarkGray
    Write-Host "    ACR Server : $acrServer" -ForegroundColor DarkGray
} else {
    Write-Host "==> Skipping infrastructure deployment (-SkipInfra)." -ForegroundColor Yellow

    # Read values from existing resources
    $params     = Get-Content "$Root\infra\main.bicepparam" -Raw
    $appName    = ([regex]::Match($params, "param appName = '([^']+)'")).Groups[1].Value
    $acrName    = "${appName}acr"
    $acrServer  = az acr show --name $acrName --query loginServer -o tsv
}

# ── Build .NET backend ────────────────────────────────────────────────────────

Write-Host "==> Building .NET backend..." -ForegroundColor Cyan
Set-Location "$Root\src\PrivateInsta.Api"
dotnet build -c Release --nologo
if (-not $?) { Write-Host ".NET build failed." -ForegroundColor Red; exit 1 }
Set-Location $Root

# ── Build React frontend ───────────────────────────────────────────────────────

Write-Host "==> Building React frontend..." -ForegroundColor Cyan
Set-Location "$Root\src\PrivateInsta.Web"
npm run build
if (-not $?) { Write-Host "React build failed." -ForegroundColor Red; exit 1 }
Set-Location $Root

# ── Build & push Docker image via ACR Tasks (no local Docker required) ────────

$ImageTag = "${acrServer}/${appName}:latest"

Write-Host "==> Building and pushing image via ACR Tasks..." -ForegroundColor Cyan
az acr build `
    --registry $acrName `
    --image "${appName}:latest" `
    --file "$Root\src\PrivateInsta.Api\Dockerfile" `
    $Root
if (-not $?) { Write-Host "ACR build failed." -ForegroundColor Red; exit 1 }

# ── Deploy to Container App ───────────────────────────────────────────────────

Write-Host "==> Updating Container App '$appName'..." -ForegroundColor Cyan
az containerapp update `
    --name $appName `
    --resource-group $ResourceGroup `
    --image $ImageTag
if (-not $?) { Write-Host "Container App update failed." -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "Deployment complete: https://$appName.<env-domain>" -ForegroundColor Green
Write-Host "(Run 'az containerapp show --name $appName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv' for the exact URL)" -ForegroundColor DarkGray
