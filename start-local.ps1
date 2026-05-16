$Root = $PSScriptRoot

Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$Root\src\PrivateInsta.Api'; dotnet run --launch-profile https"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$Root\src\PrivateInsta.Web'; npm run dev"
