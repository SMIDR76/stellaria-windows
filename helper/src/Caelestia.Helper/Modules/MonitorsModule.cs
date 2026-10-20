using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Modules;

internal sealed class MonitorsModule : IHelperModule
{
    public static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(150);

    private ModuleContext _context = null!;
    private Debouncer _refresh = null!;

    public string Name => "monitors";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        _refresh = new Debouncer(context.Loop, RefreshDelay, Publish);
        context.Loop.WinEvent += (eventType, _, objectId, childId) =>
        {
            if (DesktopEventHooks.IsWindowEvent(objectId, childId) && eventType != WinEvents.ObjectNameChange)
            {
                _refresh.Schedule();
            }
        };
        context.Loop.BroadcastReceived += _ => _refresh.Schedule();
        return context.Loop.InvokeAsync(Publish);
    }

    private void Publish()
    {
        var monitors = DesktopReader.Monitors();
        var coverage = WindowRules.Coverage(_context.Desktop.VisibleWindowsTopToBottom(), monitors);
        var data = new JsonObject();
        foreach (var monitor in monitors)
        {
            var state = coverage[monitor.Device];
            data[monitor.Device] = new JsonObject
            {
                ["fullscreen"] = state == MonitorCoverage.Fullscreen,
                ["covered"] = state != MonitorCoverage.Free,
                ["bounds"] = ToJson(monitor.Bounds),
                ["work"] = ToJson(monitor.Work),
                ["primary"] = monitor.Primary,
            };
        }

        _context.Hub.Publish("monitors", data);
    }

    private static JsonArray ToJson(Rect rect) =>
        new(JsonValue.Create(rect.Left), JsonValue.Create(rect.Top), JsonValue.Create(rect.Right), JsonValue.Create(rect.Bottom));
}
