using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Desktop;

internal sealed class DesktopReader(ProcessDirectory processes)
{
    public ProcessDirectory Processes { get; } = processes;

    public static List<MonitorFacts> Monitors()
    {
        var result = new List<MonitorFacts>();
        foreach (var monitor in User32.DisplayMonitors())
        {
            var info = MonitorInfoEx.Create();
            if (User32.GetMonitorInfo(monitor, ref info))
            {
                result.Add(new MonitorFacts(info.DeviceName, info.Monitor, info.Work, info.IsPrimary));
            }
        }

        return result;
    }

    public static string MonitorOf(nint hwnd) => User32.MonitorDevice(User32.MonitorFromWindow(hwnd, User32.MonitorDefaultToNearest));

    public static string MonitorUnderCursor() =>
        User32.GetCursorPos(out var point) ? User32.MonitorDevice(User32.MonitorFromPoint(point, User32.MonitorDefaultToNearest)) : "";

    public WindowFacts Read(nint hwnd)
    {
        User32.GetWindowRect(hwnd, out var bounds);
        return new WindowFacts(
            hwnd,
            User32.ClassName(hwnd),
            User32.WindowText(hwnd),
            Processes.NameOf(User32.ProcessId(hwnd)),
            User32.GetWindowLong(hwnd, User32.GwlExStyle),
            User32.IsWindowVisible(hwnd),
            User32.IsIconic(hwnd),
            User32.IsZoomed(hwnd),
            Dwmapi.IsCloaked(hwnd),
            User32.GetWindow(hwnd, User32.GwOwner) != 0,
            bounds);
    }

    public IEnumerable<WindowFacts> VisibleWindowsTopToBottom()
    {
        foreach (var hwnd in User32.TopLevelWindows())
        {
            if (User32.IsWindowVisible(hwnd))
            {
                yield return Read(hwnd);
            }
        }
    }

    public uint TaskProcessId(nint hwnd)
    {
        var processId = User32.ProcessId(hwnd);
        if (!string.Equals(User32.ClassName(hwnd), "ApplicationFrameWindow", StringComparison.Ordinal))
        {
            return processId;
        }

        foreach (var child in User32.ChildWindows(hwnd))
        {
            var childProcess = User32.ProcessId(child);
            if (childProcess != processId)
            {
                return childProcess;
            }
        }

        return processId;
    }
}
