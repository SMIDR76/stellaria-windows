# Raccourci de secours : arrête Caelestia et rend la barre des tâches de Windows.
# Lancé depuis le menu Démarrer (« Caelestia - Rétablir la barre des tâches »).

Get-Process -Name zebar, CaelestiaSwitcher -ErrorAction SilentlyContinue | Stop-Process -Force
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" |
  Where-Object { $_.CommandLine -match 'caelestia\\helper\.ps1 daemon' } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

Add-Type -Namespace CaelestiaRecovery -Name Shell -MemberDefinition @'
[DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int command);
[StructLayout(LayoutKind.Sequential)] public struct APPBARDATA { public int cbSize; public IntPtr hWnd; public uint uCallbackMessage; public uint uEdge; public int left, top, right, bottom; public IntPtr lParam; }
[DllImport("shell32.dll")] public static extern IntPtr SHAppBarMessage(uint message, ref APPBARDATA data);
'@

# Désactive le masquage automatique que Caelestia avait activé.
$data = New-Object CaelestiaRecovery.Shell+APPBARDATA
$data.cbSize = [Runtime.InteropServices.Marshal]::SizeOf($data)
$state = [CaelestiaRecovery.Shell]::SHAppBarMessage(4, [ref]$data).ToInt64()
$data.lParam = [IntPtr]($state -band (-bnot 1L))
[void][CaelestiaRecovery.Shell]::SHAppBarMessage(10, [ref]$data)

foreach ($className in 'Shell_TrayWnd', 'Shell_SecondaryTrayWnd') {
  $hWnd = [IntPtr]::Zero
  while (($hWnd = [CaelestiaRecovery.Shell]::FindWindowEx([IntPtr]::Zero, $hWnd, $className, $null)) -ne [IntPtr]::Zero) {
    [void][CaelestiaRecovery.Shell]::ShowWindow($hWnd, 5)
  }
}
