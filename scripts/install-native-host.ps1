param(
 [Parameter(Mandatory=$true)][string]$HostExe,
 [string]$ChromeId = "",
 [string]$EdgeId = ""
)
$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path $HostExe).Path
$origins = @()
foreach ($id in @($ChromeId, $EdgeId)) {
 if ($id -match '^[a-p]{32}$') { $origins += "chrome-extension://$id/" }
 elseif ($id) { throw "확장 ID 형식 오류: $id" }
}
$origins = @($origins | Select-Object -Unique)
if ($origins.Count -eq 0) { throw 'ChromeId 또는 EdgeId가 필요합니다. chrome://extensions 또는 edge://extensions에서 확인하세요.' }
$manifest = Join-Path (Split-Path $resolved) 'com.autologout.bridge.json'
$json = @{ name='com.autologout.bridge'; description='AutoLogout native bridge'; path=$resolved; type='stdio'; allowed_origins=$origins } | ConvertTo-Json -Depth 3
[System.IO.File]::WriteAllText($manifest, $json, (New-Object System.Text.UTF8Encoding($false)))
foreach ($browser in @('Google\Chrome','Microsoft\Edge')) {
 $key = "HKCU:\Software\$browser\NativeMessagingHosts\com.autologout.bridge"
 New-Item -Path $key -Force | Out-Null
 Set-Item -Path $key -Value $manifest
}
Write-Host 'Native Messaging host registered for Chrome and Edge.'
Write-Host $manifest

