#Requires -Version 5.1
param(
  [string]$Version = '',
  [switch]$SkipInstaller,
  [ValidateSet('All', 'Binaries', 'Package', 'Finish')]
  [string]$Phase = 'All',
  [string]$SignedBinaries = '',
  [string]$SignedSetup = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$stage = Join-Path $artifacts 'stage'
$unsigned = Join-Path $artifacts 'unsigned'
$bin = Join-Path $stage 'bin'
$pack = Join-Path $stage 'pack'
$switcherBuild = Join-Path $unsigned 'StellariaSwitcher.exe'

function Step($text) { Write-Host "-> $text" -ForegroundColor Cyan }

if (-not $Version) {
  $Version = ([xml](Get-Content (Join-Path $root 'helper\Directory.Build.props'))).Project.PropertyGroup.Version
}
$numericVersion = ($Version -split '[-+]')[0]

if ($Phase -in 'All', 'Binaries') {
  if (Test-Path $artifacts) { Remove-Item $artifacts -Recurse -Force }
  New-Item -ItemType Directory -Force $stage, $unsigned | Out-Null

  Step "Publishing the helper $Version (NativeAOT)"
  $vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
  if (Test-Path $vsInstaller) { $env:Path = "$env:Path;$vsInstaller" }
  dotnet publish (Join-Path $root 'helper\src\Stellaria.Helper\Stellaria.Helper.csproj') -c Release -r win-x64 -o $bin --nologo `
    "-p:Version=$Version" -p:IncludeSourceRevisionInInformationalVersion=false -p:ContinuousIntegrationBuild=true -p:Deterministic=true
  if ($LASTEXITCODE -ne 0) { throw 'Publishing the helper failed.' }
  Get-ChildItem -LiteralPath $bin -Filter *.pdb | Remove-Item -Force

  Step 'Building the launcher'
  & (Join-Path $root 'launcher\build.ps1')
  Copy-Item (Join-Path $root 'launcher\target\release\stellaria-launcher.exe') $bin

  Step 'Building the Alt+Tab switcher'
  $assemblyInfo = Join-Path $unsigned 'SwitcherInfo.cs'
  @(
    'using System.Reflection;',
    "[assembly: AssemblyVersion(`"$numericVersion.0`")]",
    "[assembly: AssemblyFileVersion(`"$numericVersion.0`")]",
    "[assembly: AssemblyInformationalVersion(`"$Version`")]"
  ) | Set-Content -Path $assemblyInfo -Encoding UTF8
  $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
  & $csc -nologo -target:winexe -optimize+ -platform:x64 "-out:$switcherBuild" "-win32icon:$(Join-Path $root 'launcher\icons\icon.ico')" -r:System.Windows.Forms.dll -r:System.Drawing.dll (Join-Path $root 'pack\native\StellariaSwitcher.cs') $assemblyInfo
  if ($LASTEXITCODE -ne 0) { throw 'Building the switcher failed.' }
  Remove-Item $assemblyInfo

  Copy-Item (Join-Path $bin 'stellaria-helper.exe'), (Join-Path $bin 'stellaria-launcher.exe') $unsigned
}

if ($Phase -in 'All', 'Package') {
  if ($SignedBinaries) {
    Step 'Using the signed binaries'
    foreach ($name in 'stellaria-helper.exe', 'stellaria-launcher.exe', 'StellariaSwitcher.exe') {
      $signed = Get-ChildItem -LiteralPath $SignedBinaries -Recurse -Filter $name | Select-Object -First 1
      if (-not $signed) { throw "The signed $name is missing from $SignedBinaries." }
      $target = Join-Path $unsigned $name
      if ($signed.FullName -ne (Resolve-Path $target).Path) { Copy-Item $signed.FullName $target -Force }
    }
  }
  Copy-Item (Join-Path $unsigned 'stellaria-helper.exe'), (Join-Path $unsigned 'stellaria-launcher.exe') $bin -Force

  Step 'Copying the Zebar pack'
  robocopy (Join-Path $root 'pack') $pack /MIR /XF runtime.json *.exe /XD node_modules /NFL /NDL /NJH /NJS /NP | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "Copying the pack failed (robocopy $LASTEXITCODE)." }
  Copy-Item $switcherBuild (Join-Path $pack 'native\StellariaSwitcher.exe') -Force

  Step 'Signing the catalogue'
  $keyFile = Join-Path $env:USERPROFILE '.stellaria-signing\stellaria-registry.key'
  if (-not $env:STELLARIA_SIGNING_KEY -and (Test-Path $keyFile)) { $env:STELLARIA_SIGNING_KEY = (Get-Content $keyFile -Raw).Trim() }
  if ($env:STELLARIA_SIGNING_KEY) {
    dotnet run --project (Join-Path $root 'helper\tools\Stellaria.Registry') -c Release -- build (Join-Path $root 'registry') (Join-Path $stage 'registry')
    if ($LASTEXITCODE -ne 0) { throw 'Building the catalogue failed.' }
  } else {
    Write-Warning 'No catalogue signing key: the release ships without a bundled catalogue.'
  }

  Step 'Listing the Rust crates'
  & (Join-Path $root 'scripts\rust-notices.ps1') | Out-Null

  Step 'Adding licences and scripts'
  Copy-Item (Join-Path $root 'LICENSE'), (Join-Path $root 'THIRD_PARTY_NOTICES.md'), (Join-Path $root 'README.md') $stage
  Copy-Item (Join-Path $root 'licenses') (Join-Path $stage 'licenses') -Recurse -Force
  New-Item -ItemType Directory -Force (Join-Path $stage 'scripts') | Out-Null
  Copy-Item (Join-Path $root 'scripts\install.ps1'), (Join-Path $root 'scripts\uninstall.ps1') (Join-Path $stage 'scripts')

  Step 'Creating the portable archive'
  $zip = Join-Path $artifacts "Stellaria-$Version-portable.zip"
  if (Test-Path $zip) { Remove-Item $zip -Force }
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
    & $iscc "/DAppVersion=$Version" "/DAppNumericVersion=$numericVersion" "/DStage=$stage" "/O$artifacts" (Join-Path $root 'installer\stellaria.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Compiling the installer failed.' }
  }
}

if ($Phase -in 'All', 'Finish') {
  $setupFile = Join-Path $artifacts "StellariaSetup-$Version.exe"
  if ($SignedSetup) {
    Step 'Using the signed installer'
    $signed = Get-ChildItem -LiteralPath $SignedSetup -Recurse -Filter "StellariaSetup-$Version.exe" | Select-Object -First 1
    if (-not $signed) { throw "The signed installer is missing from $SignedSetup." }
    Copy-Item $signed.FullName $setupFile -Force
  }

  if ((Test-Path $setupFile) -and $env:STELLARIA_SIGNING_KEY -and $Version -match '^[0-9]+\.[0-9]+\.[0-9]+$') {
    Step 'Signing the update manifest'
    dotnet run --project (Join-Path $root 'helper\tools\Stellaria.Registry') -c Release -- update $setupFile $Version $artifacts
    if ($LASTEXITCODE -ne 0) { throw 'Signing the update manifest failed.' }
  }

  Step 'Writing checksums'
  $sums = Get-ChildItem $artifacts -File | Where-Object { $_.Extension -in '.exe', '.zip' } | Sort-Object Name | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
  }
  [IO.File]::WriteAllLines((Join-Path $artifacts 'SHA256SUMS.txt'), [string[]]$sums, (New-Object System.Text.UTF8Encoding($false)))

  Get-ChildItem $artifacts -File | ForEach-Object { '{0,-40} {1,8:N0} Ko' -f $_.Name, ($_.Length / 1KB) }
}
