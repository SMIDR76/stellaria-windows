#Requires -Version 5.1
param([switch]$KeepSettings)

$ErrorActionPreference = 'Stop'
$pack = Join-Path $env:USERPROFILE '.glzr\zebar\caelestia'
$home2 = Join-Path $env:USERPROFILE '.caelestia'
$helper = Join-Path $home2 'bin\caelestia-helper.exe'

Write-Host '-> Restoring Windows and removing Caelestia from startup' -ForegroundColor Cyan
if (Test-Path $helper) {
  $process = Start-Process -FilePath $helper -ArgumentList '--uninstall' -Wait -PassThru -WindowStyle Hidden
  if ($process.ExitCode -ne 0) { Write-Warning "caelestia-helper --uninstall returned $($process.ExitCode), see %LOCALAPPDATA%\Caelestia\logs\helper.log." }
}

Write-Host '-> Removing the shortcuts and files' -ForegroundColor Cyan
Get-Process caelestia-launcher -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path 'HKCU:\Software\Classes\caelestia') { Remove-Item 'HKCU:\Software\Classes\caelestia' -Recurse -Force }
$menu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Caelestia'
if (Test-Path $menu) { Remove-Item $menu -Recurse -Force }
if (Test-Path $pack) { Remove-Item $pack -Recurse -Force }
if (Test-Path $home2) { Remove-Item $home2 -Recurse -Force }
if (-not $KeepSettings) {
  foreach ($path in (Join-Path $env:APPDATA 'Caelestia'), (Join-Path $env:LOCALAPPDATA 'Caelestia'), (Join-Path $env:APPDATA 'zebar\webview-cache\caelestia')) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
  }
}

Write-Host 'Caelestia has been removed. Zebar itself is still installed (winget uninstall glzr-io.zebar).' -ForegroundColor Green
