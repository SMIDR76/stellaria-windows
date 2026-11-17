#Requires -Version 5.1
param([switch]$NoStart)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pack = Join-Path $env:USERPROFILE '.glzr\zebar\stellaria'
$home2 = Join-Path $env:USERPROFILE '.stellaria'
$bin = Join-Path $home2 'bin'
$helper = Join-Path $bin 'stellaria-helper.exe'

function Step($text) { Write-Host "-> $text" -ForegroundColor Cyan }

function Invoke-Helper([string[]]$Arguments) {
  $process = Start-Process -FilePath $helper -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
  if ($process.ExitCode -ne 0) { throw "stellaria-helper $($Arguments -join ' ') failed (exit code $($process.ExitCode)), see %LOCALAPPDATA%\Stellaria\logs\helper.log." }
}

if (Test-Path $helper) {
  Step 'Stopping a running Stellaria'
  $stop = Start-Process -FilePath $helper -ArgumentList '--shutdown' -Wait -PassThru -WindowStyle Hidden
  if ($stop.ExitCode -ne 0) {
    Start-Process -FilePath $helper -ArgumentList '--quit' -Wait -WindowStyle Hidden
    Get-Process zebar, StellariaSwitcher, stellaria-helper -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep 1
  }
}
Get-Process stellaria-launcher -ErrorAction SilentlyContinue | Stop-Process -Force

Step 'Installing the Zebar pack'
New-Item -ItemType Directory -Force $pack | Out-Null
robocopy (Join-Path $root 'pack') $pack /MIR /XF runtime.json /XD node_modules /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying the pack failed (robocopy $LASTEXITCODE)." }

Step 'Installing the helper'
New-Item -ItemType Directory -Force $bin | Out-Null
Get-ChildItem -LiteralPath $bin | Remove-Item -Recurse -Force
$prebuilt = Join-Path $root 'bin'
$project = Join-Path $root 'helper\src\Stellaria.Helper\Stellaria.Helper.csproj'
if (Test-Path (Join-Path $prebuilt 'stellaria-helper.exe')) {
  robocopy $prebuilt $bin /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
} elseif ((Test-Path $project) -and (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  $vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
  $vswhere = Join-Path $vsInstaller 'vswhere.exe'
  $nativeTools = (Test-Path $vswhere) -and (& $vswhere -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath)
  if ($nativeTools) {
    $env:Path = "$env:Path;$vsInstaller"
    dotnet publish $project -c Release -r win-x64 -o $bin --nologo | Out-Null
  } else {
    dotnet publish $project -c Release -r win-x64 --self-contained -p:PublishAot=false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $bin --nologo | Out-Null
  }
  if ($LASTEXITCODE -ne 0) { throw 'Building the helper failed.' }
  Get-ChildItem -LiteralPath $bin -Filter *.pdb | Remove-Item -Force
} else {
  throw 'No prebuilt helper and no .NET SDK: download a release archive or install the .NET 10 SDK.'
}

$launcherSource = Join-Path $root 'launcher'
if ((Test-Path (Join-Path $launcherSource 'Cargo.toml')) -and (Test-Path (Join-Path $env:USERPROFILE '.cargo\bin\cargo.exe'))) {
  Step 'Building the launcher'
  & (Join-Path $launcherSource 'build.ps1') | Out-Null
  Copy-Item (Join-Path $launcherSource 'target\release\stellaria-launcher.exe') $bin -Force
}

$catalogue = Join-Path $home2 'registry'
$bundled = Join-Path $root 'registry\index.json.minisig'
$keyFile = Join-Path $env:USERPROFILE '.stellaria-signing\stellaria-registry.key'
if (Test-Path $bundled) {
  New-Item -ItemType Directory -Force $catalogue | Out-Null
  Copy-Item (Join-Path $root 'registry\index.json'), $bundled $catalogue -Force
} elseif ((Test-Path $keyFile) -and (Test-Path (Join-Path $root 'helper\tools\Stellaria.Registry')) -and (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Step 'Signing the catalogue'
  $env:STELLARIA_SIGNING_KEY = (Get-Content $keyFile -Raw).Trim()
  dotnet run --project (Join-Path $root 'helper\tools\Stellaria.Registry') -c Release -- build (Join-Path $root 'registry') $catalogue | Out-Null
  Remove-Item Env:STELLARIA_SIGNING_KEY
}

$switcher = Join-Path $pack 'native\StellariaSwitcher.exe'
if (-not (Test-Path $switcher)) {
  Step 'Building the Alt+Tab switcher'
  $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
  & $csc -nologo -target:winexe -optimize+ -platform:x64 "-out:$switcher" -r:System.Windows.Forms.dll -r:System.Drawing.dll (Join-Path $pack 'native\StellariaSwitcher.cs') | Out-Null
}

Step 'Registering Stellaria with Zebar and Windows'
Invoke-Helper @('--install')

Step 'Adding Start menu shortcuts'
$shell = New-Object -ComObject WScript.Shell
$menu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Stellaria'
New-Item -ItemType Directory -Force $menu | Out-Null
$restore = $shell.CreateShortcut((Join-Path $menu 'Stellaria - Rétablir la barre des tâches.lnk'))
$restore.TargetPath = $helper
$restore.Arguments = '--restore-shell'
$restore.IconLocation = Join-Path $env:WINDIR 'System32\imageres.dll,72'
$restore.Save()
$launcherExe = Join-Path $bin 'stellaria-launcher.exe'
if (Test-Path $launcherExe) {
  $app = $shell.CreateShortcut((Join-Path $menu 'Stellaria.lnk'))
  $app.TargetPath = $launcherExe
  $app.IconLocation = "$launcherExe,0"
  $app.Save()
  $classes = 'HKCU:\Software\Classes\stellaria'
  New-Item -Path "$classes\shell\open\command" -Force | Out-Null
  Set-Item -Path $classes -Value 'URL:Stellaria'
  New-ItemProperty -Path $classes -Name 'URL Protocol' -Value '' -Force | Out-Null
  Set-Item -Path "$classes\shell\open\command" -Value "`"$launcherExe`" `"%1`""
}
$relaunch = $shell.CreateShortcut((Join-Path $menu 'Stellaria - Relancer.lnk'))
$relaunch.TargetPath = $helper
$relaunch.Arguments = '--start'
$relaunch.IconLocation = "$helper,0"
$relaunch.Save()

if (-not $NoStart) {
  Step 'Starting Stellaria'
  Invoke-Helper @('--start')
}

Write-Host 'Stellaria is installed. Press Win+Alt+K for the shortcut cheat sheet.' -ForegroundColor Green
