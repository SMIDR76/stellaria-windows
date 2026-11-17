param(
  [Parameter(Mandatory = $true)]
  [ValidateSet('on', 'off', 'build', 'status')]
  [string]$Action
)

$ErrorActionPreference = 'Stop'
$native = Join-Path $PSScriptRoot 'native'
$exe = Join-Path $native 'StellariaSwitcher.exe'
$source = Join-Path $native 'StellariaSwitcher.cs'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$runName = 'StellariaSwitcher'

function Stop-Switcher {
  if (-not (Get-Process -Name StellariaSwitcher -ErrorAction SilentlyContinue)) { return }
  & $exe --quit
  $deadline = (Get-Date).AddSeconds(3)
  while ((Get-Process -Name StellariaSwitcher -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 100 }
  Get-Process -Name StellariaSwitcher -ErrorAction SilentlyContinue | Stop-Process -Force
}

switch ($Action) {
  'build' {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    Stop-Switcher
    & $compiler -nologo -target:winexe -optimize+ -platform:x64 "-out:$exe" -r:System.Windows.Forms.dll -r:System.Drawing.dll $source
    if ($LASTEXITCODE -ne 0) { throw 'Compilation du sélecteur échouée' }
    Write-Output "Compilé : $exe"
  }
  'on' {
    if (-not (Test-Path -LiteralPath $exe)) { throw "Introuvable : $exe" }
    Set-ItemProperty -Path $runKey -Name $runName -Value "`"$exe`""
    Stop-Switcher
    Start-Process -FilePath $exe
    Write-Output 'Sélecteur Alt+Tab Stellaria activé (démarrage automatique compris).'
  }
  'off' {
    Remove-ItemProperty -Path $runKey -Name $runName -ErrorAction SilentlyContinue
    Stop-Switcher
    Write-Output 'Sélecteur Alt+Tab Stellaria désactivé : Alt+Tab de Windows rétabli.'
  }
  'status' {
    $running = [bool](Get-Process -Name StellariaSwitcher -ErrorAction SilentlyContinue)
    $startup = [bool](Get-ItemProperty -Path $runKey -Name $runName -ErrorAction SilentlyContinue)
    Write-Output "En cours : $running ; démarrage automatique : $startup"
  }
}
