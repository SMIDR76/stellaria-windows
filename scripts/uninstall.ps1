#Requires -Version 5.1
param([switch]$KeepSettings)

$ErrorActionPreference = 'Stop'
$pack = Join-Path $env:USERPROFILE '.glzr\zebar\stellaria'
$home2 = Join-Path $env:USERPROFILE '.stellaria'
$helper = Join-Path $home2 'bin\stellaria-helper.exe'

Write-Host '-> Restoring Windows and removing Stellaria from startup' -ForegroundColor Cyan
if (Test-Path $helper) {
  $process = Start-Process -FilePath $helper -ArgumentList '--uninstall' -Wait -PassThru -WindowStyle Hidden
  if ($process.ExitCode -ne 0) { Write-Warning "stellaria-helper --uninstall returned $($process.ExitCode), see %LOCALAPPDATA%\Stellaria\logs\helper.log." }
}

Write-Host '-> Removing the shortcuts and files' -ForegroundColor Cyan
Get-Process stellaria-launcher -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path 'HKCU:\Software\Classes\stellaria') { Remove-Item 'HKCU:\Software\Classes\stellaria' -Recurse -Force }
$menu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Stellaria'
if (Test-Path $menu) { Remove-Item $menu -Recurse -Force }
if (Test-Path $pack) { Remove-Item $pack -Recurse -Force }
if (Test-Path $home2) { Remove-Item $home2 -Recurse -Force }
if (-not $KeepSettings) {
  foreach ($path in (Join-Path $env:APPDATA 'Stellaria'), (Join-Path $env:LOCALAPPDATA 'Stellaria'), (Join-Path $env:APPDATA 'zebar\webview-cache\stellaria')) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
  }
}

Write-Host 'Stellaria has been removed. Zebar itself is still installed (winget uninstall glzr-io.zebar).' -ForegroundColor Green
