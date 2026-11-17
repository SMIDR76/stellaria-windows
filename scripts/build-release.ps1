#Requires -Version 5.1
param(
  [string]$Version = '',
  [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$stage = Join-Path $artifacts 'stage'

function Step($text) { Write-Host "-> $text" -ForegroundColor Cyan }

if (-not $Version) {
  $Version = ([xml](Get-Content (Join-Path $root 'helper\Directory.Build.props'))).Project.PropertyGroup.Version
}

if (Test-Path $artifacts) { Remove-Item $artifacts -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null

Step "Publishing the helper $Version (NativeAOT)"
$vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if (Test-Path $vsInstaller) { $env:Path = "$env:Path;$vsInstaller" }
$bin = Join-Path $stage 'bin'
dotnet publish (Join-Path $root 'helper\src\Caelestia.Helper\Caelestia.Helper.csproj') -c Release -r win-x64 -o $bin --nologo `
  "-p:Version=$Version" -p:ContinuousIntegrationBuild=true -p:Deterministic=true
if ($LASTEXITCODE -ne 0) { throw 'Publishing the helper failed.' }
Get-ChildItem -LiteralPath $bin -Filter *.pdb | Remove-Item -Force

Step 'Building the launcher'
& (Join-Path $root 'launcher\build.ps1')
Copy-Item (Join-Path $root 'launcher\target\release\caelestia-launcher.exe') $bin

Step 'Copying the Zebar pack'
$pack = Join-Path $stage 'pack'
robocopy (Join-Path $root 'pack') $pack /MIR /XF runtime.json *.exe /XD node_modules /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying the pack failed (robocopy $LASTEXITCODE)." }

Step 'Building the Alt+Tab switcher'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc -nologo -target:winexe -optimize+ -platform:x64 "-out:$(Join-Path $pack 'native\CaelestiaSwitcher.exe')" -r:System.Windows.Forms.dll -r:System.Drawing.dll (Join-Path $pack 'native\CaelestiaSwitcher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Building the switcher failed.' }

Step 'Signing the catalogue'
$keyFile = Join-Path $env:USERPROFILE '.caelestia-signing\caelestia-registry.key'
if (-not $env:CAELESTIA_SIGNING_KEY -and (Test-Path $keyFile)) { $env:CAELESTIA_SIGNING_KEY = (Get-Content $keyFile -Raw).Trim() }
if ($env:CAELESTIA_SIGNING_KEY) {
  dotnet run --project (Join-Path $root 'helper\tools\Caelestia.Registry') -c Release -- build (Join-Path $root 'registry') (Join-Path $stage 'registry')
  if ($LASTEXITCODE -ne 0) { throw 'Building the catalogue failed.' }
} else {
  Write-Warning 'No catalogue signing key: the release ships without a bundled catalogue.'
}

Step 'Adding licences and scripts'
Copy-Item (Join-Path $root 'LICENSE'), (Join-Path $root 'THIRD_PARTY_NOTICES.md'), (Join-Path $root 'README.md') $stage
Copy-Item (Join-Path $root 'licenses') (Join-Path $stage 'licenses') -Recurse
New-Item -ItemType Directory -Force (Join-Path $stage 'scripts') | Out-Null
Copy-Item (Join-Path $root 'scripts\install.ps1'), (Join-Path $root 'scripts\uninstall.ps1') (Join-Path $stage 'scripts')

Step 'Creating the portable archive'
$zip = Join-Path $artifacts "Caelestia-$Version-portable.zip"
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal

if (-not $SkipInstaller) {
  Step 'Compiling the installer'
  $iscc = @(
    (Get-Command iscc -ErrorAction SilentlyContinue).Source,
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
  ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
  if (-not $iscc) { throw 'Inno Setup (ISCC.exe) was not found. Install it or pass -SkipInstaller.' }
  & $iscc "/DAppVersion=$Version" "/DStage=$stage" "/O$artifacts" (Join-Path $root 'installer\caelestia.iss')
  if ($LASTEXITCODE -ne 0) { throw 'Compiling the installer failed.' }
}

Step 'Writing checksums'
$sums = Get-ChildItem $artifacts -File | Where-Object { $_.Extension -in '.exe', '.zip' } | Sort-Object Name | ForEach-Object {
  '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
}
[IO.File]::WriteAllLines((Join-Path $artifacts 'SHA256SUMS.txt'), [string[]]$sums, (New-Object System.Text.UTF8Encoding($false)))

Get-ChildItem $artifacts -File | ForEach-Object { '{0,-40} {1,8:N0} Ko' -f $_.Name, ($_.Length / 1KB) }
