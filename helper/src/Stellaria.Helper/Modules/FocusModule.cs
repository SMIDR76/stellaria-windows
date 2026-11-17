using System.Text.Json.Nodes;
using Stellaria.Helper.Desktop;
using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Modules;

internal sealed class FocusModule : IHelperModule
{
    public static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(50);

    private readonly Dictionary<string, JsonObject> _byMonitor = new(StringComparer.OrdinalIgnoreCase);
    private ModuleContext _context = null!;
    private Debouncer _refresh = null!;
    private string _active = "";

    public string Name => "focus";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        _refresh = new Debouncer(context.Loop, RefreshDelay, Refresh);
        context.Loop.WinEvent += (eventType, hwnd, objectId, childId) =>
        {
            if (eventType == WinEvents.SystemForeground ||
                (eventType == WinEvents.ObjectNameChange && DesktopEventHooks.IsWindowEvent(objectId, childId) && hwnd == User32.GetForegroundWindow()))
            {
                _refresh.Schedule();
            }
        };
        return context.Loop.InvokeAsync(Refresh);
    }

    private void Refresh()
    {
        var hwnd = User32.GetForegroundWindow();
        if (hwnd == 0)
        {
            return;
        }

        var process = _context.Desktop.Processes.Find(_context.Desktop.TaskProcessId(hwnd));
        if (WindowRules.IsOwnProcess(process?.Name ?? ""))
        {
            return;
        }

        _context.Foreground.Observe(hwnd);
        var monitor = DesktopReader.MonitorOf(hwnd);
        var shell = WindowRules.IsShellWindow(User32.ClassName(hwnd));
        _byMonitor[monitor] = new JsonObject
        {
            ["title"] = shell ? "" : User32.WindowText(hwnd),
            ["process"] = shell ? "" : process?.Name ?? "",
            ["exe"] = shell ? "" : process?.ImagePath ?? "",
        };
        _active = monitor;

        var monitors = new JsonObject();
        foreach (var (device, entry) in _byMonitor)
        {
            monitors[device] = entry.DeepClone();
        }

        _context.Hub.Publish("focus", new JsonObject { ["monitors"] = monitors, ["active"] = _active });
    }
}
