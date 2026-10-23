$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
 dotnet publish src/AutoLogout.App/AutoLogout.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out/app
 if ($LASTEXITCODE -ne 0) { throw 'App publish failed' }
 dotnet publish src/AutoLogout.Host/AutoLogout.Host.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out/host
 if ($LASTEXITCODE -ne 0) { throw 'Host publish failed' }
 Write-Host 'Done. Outputs: out/app/AutoLogout.App.exe and out/host/AutoLogout.Host.exe'
} finally { Pop-Location }

