using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Desktop;

internal static class Taskbar
{
    private static readonly string[] s_classes = ["Shell_TrayWnd", "Shell_SecondaryTrayWnd"];

    public static bool AutoHide
    {
        get
        {
            var data = NewData();
            return (Shell32.SHAppBarMessage(Shell32.AbmGetState, ref data) & Shell32.AbsAutoHide) != 0;
        }
    }

    public static void SetAutoHide(bool enabled)
    {
        var data = NewData();
        var state = (uint)Shell32.SHAppBarMessage(Shell32.AbmGetState, ref data);
        var wanted = enabled ? state | Shell32.AbsAutoHide : state & ~Shell32.AbsAutoHide;
        if (wanted == state)
        {
            return;
        }

        data.LParam = (nint)wanted;
        Shell32.SHAppBarMessage(Shell32.AbmSetState, ref data);
    }

    public static int SetVisible(bool visible)
    {
        var changed = 0;
        foreach (var className in s_classes)
        {
            var hwnd = (nint)0;
            while ((hwnd = User32.FindWindowEx(0, hwnd, className, null)) != 0)
            {
                if (User32.IsWindowVisible(hwnd) == visible)
                {
                    continue;
                }

                User32.ShowWindow(hwnd, visible ? User32.SwShow : User32.SwHide);
                changed++;
            }
        }

        return changed;
    }

    private static unsafe AppBarData NewData() => new()
    {
        Size = sizeof(AppBarData),
        Hwnd = User32.FindWindow("Shell_TrayWnd", null),
    };
}
