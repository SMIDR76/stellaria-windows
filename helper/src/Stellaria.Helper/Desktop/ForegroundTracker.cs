using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Desktop;

internal sealed class ForegroundTracker(DesktopReader desktop)
{
    private nint _lastUserWindow;

    public nint LastUserWindow
    {
        get
        {
            var window = Volatile.Read(ref _lastUserWindow);
            return window != 0 && User32.IsWindow(window) ? window : 0;
        }
    }

    public bool Observe(nint hwnd)
    {
        if (hwnd == 0 || hwnd == Volatile.Read(ref _lastUserWindow))
        {
            return false;
        }

        if (WindowRules.IsShellWindow(User32.ClassName(hwnd)) || WindowRules.IsOwnProcess(desktop.Processes.NameOf(User32.ProcessId(hwnd))))
        {
            return false;
        }

        Volatile.Write(ref _lastUserWindow, hwnd);
        return true;
    }
}
