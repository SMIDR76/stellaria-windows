#Requires -Version 5.1
param([switch]$NoStart)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pack = Join-Path $env:USERPROFILE '.glzr\zebar\caelestia'
$home2 = Join-Path $env:USERPROFILE '.caelestia'
$bin = Join-Path $home2 'bin'
$helper = Join-Path $bin 'caelestia-helper.exe'

function Step($text) { Write-Host "-> $text" -ForegroundColor Cyan }

function Invoke-Helper([string[]]$Arguments) {
  $process = Start-Process -FilePath $helper -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
  if ($process.ExitCode -ne 0) { throw "caelestia-helper $($Arguments -join ' ') failed (exit code $($process.ExitCode)), see %LOCALAPPDATA%\Caelestia\logs\helper.log." }
}

if (Test-Path $helper) {
  Step 'Stopping a running Caelestia'
  $stop = Start-Process -FilePath $helper -ArgumentList '--shutdown' -Wait -PassThru -WindowStyle Hidden
  if ($stop.ExitCode -ne 0) {
    Start-Process -FilePath $helper -ArgumentList '--quit' -Wait -WindowStyle Hidden
    Get-Process zebar, CaelestiaSwitcher, caelestia-helper -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep 1
  }
}

Step 'Installing the Zebar pack'
New-Item -ItemType Directory -Force $pack | Out-Null
robocopy (Join-Path $root 'pack') $pack /MIR /XF runtime.json /XD node_modules /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying the pack failed (robocopy $LASTEXITCODE)." }

Step 'Installing the helper'
New-Item -ItemType Directory -Force $bin | Out-Null
Get-ChildItem -LiteralPath $bin | Remove-Item -Recurse -Force
$prebuilt = Join-Path $root 'bin'
$project = Join-Path $root 'helper\src\Caelestia.Helper\Caelestia.Helper.csproj'
if (Test-Path (Join-Path $prebuilt 'caelestia-helper.exe')) {
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

$switcher = Join-Path $pack 'native\CaelestiaSwitcher.exe'
if (-not (Test-Path $switcher)) {
  Step 'Building the Alt+Tab switcher'
  $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
  & $csc -nologo -target:winexe -optimize+ -platform:x64 "-out:$switcher" -r:System.Windows.Forms.dll -r:System.Drawing.dll (Join-Path $pack 'native\CaelestiaSwitcher.cs') | Out-Null
}

Step 'Registering Caelestia with Zebar and Windows'
Invoke-Helper @('--install')

Step 'Adding Start menu shortcuts'
$shell = New-Object -ComObject WScript.Shell
$menu = Join-Path ([Environment]::GetFolderPath('Programs')) 'Caelestia'
New-Item -ItemType Directory -Force $menu | Out-Null
$restore = $shell.CreateShortcut((Join-Path $menu 'Caelestia - Rétablir la barre des tâches.lnk'))
$restore.TargetPath = $helper
$restore.Arguments = '--restore-shell'
$restore.IconLocation = Join-Path $env:WINDIR 'System32\imageres.dll,72'
$restore.Save()
$relaunch = $shell.CreateShortcut((Join-Path $menu 'Caelestia - Relancer.lnk'))
$relaunch.TargetPath = $helper
$relaunch.Arguments = '--start'
$relaunch.Save()

if (-not $NoStart) {
  Step 'Starting Caelestia'
  Invoke-Helper @('--start')
}

Write-Host 'Caelestia is installed. Press Win+Alt+K for the shortcut cheat sheet.' -ForegroundColor Green
