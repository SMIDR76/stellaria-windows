using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Windows;

internal sealed record WindowFacts(
    nint Handle,
    string ClassName,
    string Title,
    string ProcessName,
    int ExtendedStyle,
    bool Visible,
    bool Minimized,
    bool Maximized,
    bool Cloaked,
    bool HasOwner,
    Rect Bounds);

internal sealed record MonitorFacts(string Device, Rect Bounds, Rect Work, bool Primary);

internal enum MonitorCoverage
{
    Free,
    Maximized,
    Fullscreen,
}

internal static class WindowRules
{
    public static readonly IReadOnlySet<string> ShellClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "Windows.UI.Core.CoreWindow",
    };

    public static readonly IReadOnlySet<string> OwnProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "zebar",
        "caelestia-helper",
    };

    public static bool IsOwnProcess(string processName) => OwnProcesses.Contains(processName);

    public static bool IsShellWindow(string className) => ShellClasses.Contains(className);

    public static bool IsTask(WindowFacts window)
    {
        if (!window.Visible || window.Title.Length == 0 || window.Cloaked)
        {
            return false;
        }

        if (IsShellWindow(window.ClassName) || IsOwnProcess(window.ProcessName))
        {
            return false;
        }

        var appWindow = (window.ExtendedStyle & User32.WsExAppWindow) != 0;
        if (appWindow)
        {
            return true;
        }

        var hiddenFromTaskbar = (window.ExtendedStyle & (User32.WsExToolWindow | User32.WsExNoActivate)) != 0;
        return !hiddenFromTaskbar && !window.HasOwner;
    }

    public static bool CanCoverMonitor(WindowFacts window)
    {
        if (!window.Visible || window.Minimized || window.Cloaked || window.Bounds.IsEmpty)
        {
            return false;
        }

        if ((window.ExtendedStyle & (User32.WsExTransparent | User32.WsExToolWindow | User32.WsExNoActivate)) != 0)
        {
            return false;
        }

        return !IsShellWindow(window.ClassName) && !IsOwnProcess(window.ProcessName);
    }

    public static IReadOnlyDictionary<string, MonitorCoverage> Coverage(IEnumerable<WindowFacts> windowsTopToBottom, IReadOnlyList<MonitorFacts> monitors)
    {
        var result = new Dictionary<string, MonitorCoverage>(StringComparer.OrdinalIgnoreCase);
        foreach (var window in windowsTopToBottom)
        {
            if (result.Count == monitors.Count)
            {
                break;
            }

            if (!CanCoverMonitor(window))
            {
                continue;
            }

            foreach (var monitor in monitors)
            {
                if (result.ContainsKey(monitor.Device) || !window.Bounds.Intersects(monitor.Bounds))
                {
                    continue;
                }

                result[monitor.Device] = window.Bounds.Covers(monitor.Bounds)
                    ? MonitorCoverage.Fullscreen
                    : window.Maximized ? MonitorCoverage.Maximized : MonitorCoverage.Free;
            }
        }

        foreach (var monitor in monitors)
        {
            result.TryAdd(monitor.Device, MonitorCoverage.Free);
        }

        return result;
    }
}
