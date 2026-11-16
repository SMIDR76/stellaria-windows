#Requires -Version 5.1
param([switch]$NoStart)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$zebarExe = Join-Path $env:ProgramFiles 'glzr.io\Zebar\zebar.exe'
$pack = Join-Path $env:USERPROFILE '.glzr\zebar\caelestia'
$home2 = Join-Path $env:USERPROFILE '.caelestia'
$bin = Join-Path $home2 'bin'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Step($text) { Write-Host "-> $text" -ForegroundColor Cyan }

if (-not (Test-Path $zebarExe)) {
  throw "Zebar 3.3 or later is required: winget install -e --id glzr-io.zebar"
}

Step 'Stopping a running Caelestia'
Get-Process zebar, CaelestiaSwitcher -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path (Join-Path $bin 'caelestia-helper.exe')) { & (Join-Path $bin 'caelestia-helper.exe') --quit; Start-Sleep 2 }
Get-Process caelestia-helper -ErrorAction SilentlyContinue | Stop-Process -Force
Add-Type -Namespace CaelestiaInstall -Name WorkArea -MemberDefinition '[StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; } [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint action, uint param, ref RECT rect, uint flags);'
Add-Type -AssemblyName System.Windows.Forms
foreach ($screen in [System.Windows.Forms.Screen]::AllScreens) {
  $rect = New-Object CaelestiaInstall.WorkArea+RECT
  $rect.L = $screen.Bounds.Left; $rect.T = $screen.Bounds.Top; $rect.R = $screen.Bounds.Right; $rect.B = $screen.Bounds.Bottom
  [void][CaelestiaInstall.WorkArea]::SystemParametersInfo(0x2F, 0, [ref]$rect, 2)
}

Step 'Installing the Zebar pack'
New-Item -ItemType Directory -Force $pack | Out-Null
robocopy (Join-Path $repo 'pack') $pack /MIR /XF runtime.json *.exe /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying the pack failed (robocopy $LASTEXITCODE)." }

Step 'Installing the helper'
New-Item -ItemType Directory -Force $bin | Out-Null
Get-ChildItem -LiteralPath $bin | Remove-Item -Recurse -Force
$prebuilt = Join-Path $PSScriptRoot 'helper-bin'
$project = Join-Path $repo 'helper\src\Caelestia.Helper\Caelestia.Helper.csproj'
$vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
$vswhere = Join-Path $vsInstaller 'vswhere.exe'
$nativeTools = (Test-Path $vswhere) -and (& $vswhere -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath)
if ((Get-Command dotnet -ErrorAction SilentlyContinue) -and $nativeTools) {
  $env:Path = "$env:Path;$vsInstaller"
  dotnet publish $project -c Release -r win-x64 -o $bin --nologo | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'Building the helper failed.' }
} elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
  dotnet publish $project -c Release -r win-x64 --self-contained `
    -p:PublishAot=false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $bin --nologo | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'Building the helper failed.' }
} elseif (Test-Path (Join-Path $prebuilt 'caelestia-helper.exe')) {
  robocopy $prebuilt $bin /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
} else {
  throw 'No .NET SDK and no prebuilt helper (scripts\helper-bin). Install the .NET 10 SDK or use a release archive.'
}
Copy-Item (Join-Path $PSScriptRoot 'restore-taskbar.ps1') (Join-Path $home2 'restore-taskbar.ps1') -Force

Step 'Building the Alt+Tab switcher'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$switcher = Join-Path $pack 'native\CaelestiaSwitcher.exe'
& $csc -nologo -target:winexe -optimize+ -platform:x64 "-out:$switcher" -r:System.Windows.Forms.dll -r:System.Drawing.dll (Join-Path $pack 'native\CaelestiaSwitcher.cs') | Out-Null

Step "Registering the widgets in Zebar's startup list"
$settingsPath = Join-Path $env:USERPROFILE '.glzr\zebar\settings.json'
$settings = if (Test-Path $settingsPath) { [IO.File]::ReadAllText($settingsPath).TrimStart([char]0xFEFF) | ConvertFrom-Json } else { [pscustomobject]@{ startupConfigs = @() } }
$widgets = @(
  @('edge', 'top'), @('edge', 'right'), @('edge', 'bottom'), @('bar', 'default'), @('frame', 'default'), @('dashboard', 'default'),
  @('popout', 'default'), @('desktop-clock', 'default'), @('osd', 'default'), @('utilities', 'default'), @('toasts', 'default'),
  @('launcher', 'default'), @('session', 'default'), @('settings', 'default'), @('island', 'default')
)
$others = @($settings.startupConfigs | Where-Object { $_.pack -ne 'caelestia' })
$settings.startupConfigs = $others + @($widgets | ForEach-Object { [pscustomobject]@{ pack = 'caelestia'; widget = $_[0]; preset = $_[1] } })
[IO.File]::WriteAllText($settingsPath, ($settings | ConvertTo-Json -Depth 6), $utf8)

Step 'Registering the logon tasks'
$user = "$env:USERDOMAIN\$env:USERNAME"
$taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew -StartWhenAvailable
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$tasks = @(
  @{ Name = 'Caelestia - Helper'; Exe = Join-Path $bin 'caelestia-helper.exe'; Dir = $bin; Delay = 'PT0S' },
  @{ Name = 'Caelestia - Selecteur Alt+Tab'; Exe = $switcher; Dir = Split-Path $switcher; Delay = 'PT1S' },
  @{ Name = 'Caelestia - Zebar'; Exe = $zebarExe; Dir = Split-Path $zebarExe; Delay = 'PT3S' }
)
foreach ($task in $tasks) {
  $trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
  $trigger.Delay = $task.Delay
  $action = New-ScheduledTaskAction -Execute $task.Exe -WorkingDirectory $task.Dir
  Register-ScheduledTask -TaskName $task.Name -Action $action -Trigger $trigger -Settings $taskSettings -Principal $principal -Force | Out-Null
}

Step 'Adding Start menu shortcuts'
$shell = New-Object -ComObject WScript.Shell
$menu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Caelestia'
New-Item -ItemType Directory -Force $menu | Out-Null
$restore = $shell.CreateShortcut((Join-Path $menu 'Caelestia - Rétablir la barre des tâches.lnk'))
$restore.TargetPath = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
$restore.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$(Join-Path $home2 'restore-taskbar.ps1')`""
$restore.IconLocation = Join-Path $env:WINDIR 'System32\imageres.dll,72'
$restore.Save()
$relaunch = $shell.CreateShortcut((Join-Path $menu 'Caelestia - Relancer.lnk'))
$relaunch.TargetPath = Join-Path $env:WINDIR 'System32\schtasks.exe'
$relaunch.Arguments = '/Run /TN "Caelestia - Zebar"'
$relaunch.WindowStyle = 7
$relaunch.Save()

if (-not $NoStart) {
  Step 'Starting Caelestia'
  foreach ($task in $tasks) { Start-ScheduledTask -TaskName $task.Name; Start-Sleep -Milliseconds 800 }
}

Write-Host "Caelestia is installed. Press Win+Alt+K for the shortcut cheat sheet." -ForegroundColor Green
