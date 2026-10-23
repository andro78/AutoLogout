param([string]$Iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe")
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Iscc)) { throw 'Inno Setup 6 ISCC.exe is required. Supply -Iscc with its full path.' }
& (Join-Path $PSScriptRoot 'build.ps1')
& $Iscc (Join-Path $PSScriptRoot '..\installer\AutoLogout.iss')
if ($LASTEXITCODE -ne 0) { throw "Installer compiler failed: $LASTEXITCODE" }
