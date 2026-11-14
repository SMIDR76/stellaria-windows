#Requires -Version 5.1
<#
.SYNOPSIS
  Removes Caelestia for Windows and gives the Windows taskbar back.

.PARAMETER KeepSettings
  Keep %APPDATA%\Caelestia (settings) and the widget settings stored by Zebar.
#>
param([switch]$KeepSettings)

$ErrorActionPreference = 'Stop'
$pack = Join-Path $env:USERPROFILE '.glzr\zebar\caelestia'
$home2 = Join-Path $env:USERPROFILE '.caelestia'
$helper = Join-Path $home2 'bin\caelestia-helper.exe'
$utf8 = New-Object System.Text.UTF8Encoding($false)

Write-Host '-> Restoring the taskbar and stopping Caelestia' -ForegroundColor Cyan
if (Test-Path $helper) { & $helper --restore-shell }
Get-Process zebar, CaelestiaSwitcher, caelestia-helper -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host '-> Removing the logon tasks and shortcuts' -ForegroundColor Cyan
Get-ScheduledTask -TaskName 'Caelestia - *' -ErrorAction SilentlyContinue | Unregister-ScheduledTask -Confirm:$false
Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'CaelestiaSwitcher', 'CaelestiaHelper' -ErrorAction SilentlyContinue
$menu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Caelestia'
if (Test-Path $menu) { Remove-Item $menu -Recurse -Force }

Write-Host "-> Removing the widgets from Zebar's startup list" -ForegroundColor Cyan
$settingsPath = Join-Path $env:USERPROFILE '.glzr\zebar\settings.json'
if (Test-Path $settingsPath) {
  $settings = [IO.File]::ReadAllText($settingsPath).TrimStart([char]0xFEFF) | ConvertFrom-Json
  $settings.startupConfigs = @($settings.startupConfigs | Where-Object { $_.pack -ne 'caelestia' })
  [IO.File]::WriteAllText($settingsPath, ($settings | ConvertTo-Json -Depth 6), $utf8)
}

Write-Host '-> Removing the files' -ForegroundColor Cyan
if (Test-Path $pack) { Remove-Item $pack -Recurse -Force }
if (Test-Path $home2) { Remove-Item $home2 -Recurse -Force }
if (-not $KeepSettings) {
  foreach ($path in (Join-Path $env:APPDATA 'Caelestia'), (Join-Path $env:LOCALAPPDATA 'Caelestia'), (Join-Path $env:APPDATA 'zebar\webview-cache\caelestia')) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
  }
}

Write-Host 'Caelestia has been removed. Zebar itself is still installed (winget uninstall glzr-io.zebar).' -ForegroundColor Green
