#Requires -Version 5.1
param([switch]$Debug)

$ErrorActionPreference = 'Stop'
$launcher = $PSScriptRoot
$pack = Join-Path (Split-Path -Parent $launcher) 'pack'
$dist = Join-Path $launcher 'dist'

if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $dist 'shared\styles'), (Join-Path $dist 'shared\assets\fonts') | Out-Null
Copy-Item (Join-Path $launcher 'ui\*') $dist -Recurse
Copy-Item (Join-Path $pack 'common.css'), (Join-Path $pack 'schemes.json') (Join-Path $dist 'shared')
Copy-Item (Join-Path $pack 'styles\fonts.css') (Join-Path $dist 'shared\styles')
Copy-Item (Join-Path $pack 'assets\fonts\*.woff2') (Join-Path $dist 'shared\assets\fonts')

$cargo = Join-Path $env:USERPROFILE '.cargo\bin\cargo.exe'
if (-not (Test-Path $cargo)) { $cargo = 'cargo' }
$manifest = Join-Path $launcher 'Cargo.toml'
[string[]]$buildMode = @(); if (-not $Debug) { $buildMode = @('--release') }
$ErrorActionPreference = 'Continue'
& $cargo clean --manifest-path $manifest @buildMode -p caelestia-launcher 2>&1 | Out-Null
& $cargo build --manifest-path $manifest @buildMode 2>&1 | ForEach-Object { "$_" } | Where-Object { $_ -match 'error|warning: unused|Finished' }
$exit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($exit -ne 0) { throw 'Building the launcher failed.' }
