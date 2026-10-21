using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Modules;

internal sealed class ShellModule(HelperState state) : IHelperModule
{
    public static readonly TimeSpan MaintenanceInterval = TimeSpan.FromSeconds(2);

    private ModuleContext _context = null!;
    private RepairBreaker _breaker = null!;
    private Timer? _maintenance;
    private bool _hideTaskbar;

    public string Name => "shell";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        _breaker = new RepairBreaker(5, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), context.Time);
        context.Config.Changed += _ =>
        {
            var hidden = context.Config.GetFlag("taskbar.hide");
            context.Loop.Post(() =>
            {
                if (hidden != _hideTaskbar)
                {
                    ApplyTaskbar(hidden);
                }
            });
        };
        context.Loop.SessionEnding += () => ShellRecovery.ShowTaskbar(context.Log);
        _maintenance = new Timer(_ => context.Loop.Post(Maintain), null, MaintenanceInterval, MaintenanceInterval);
        context.Shutdown.Register(() => _maintenance.Dispose());
        return context.Loop.InvokeAsync(() => ApplyTaskbar(context.Config.GetFlag("taskbar.hide")));
    }

    private void ApplyTaskbar(bool hidden)
    {
        _hideTaskbar = hidden;
        if (hidden)
        {
            state.OriginalTaskbarAutoHide ??= Taskbar.AutoHide;
            Taskbar.SetAutoHide(true);
            Taskbar.SetVisible(false);
        }
        else
        {
            Taskbar.SetVisible(true);
        }

        PublishState();
    }

    private void PublishState() => _context.Hub.Publish("shell", new JsonObject { ["taskbarHidden"] = _hideTaskbar });

    private void Maintain()
    {
        if (_hideTaskbar)
        {
            Taskbar.SetAutoHide(true);
            Taskbar.SetVisible(false);
        }

        if (_breaker.IsPaused)
        {
            return;
        }

        var repaired = RepairAppBars();
        if (repaired == 0)
        {
            return;
        }

        _context.Log.Info("shell", $"Re-registered {repaired} Caelestia app bar(s).");
        _context.Hub.Raise("appbars.repaired", new JsonObject { ["count"] = repaired });
        if (_breaker.RecordRepair())
        {
            _context.Log.Warning("shell", "App bar reservations keep getting lost, repairs paused for 5 minutes.");
            _context.Hub.Raise("appbars.paused", new JsonObject { ["minutes"] = 5 });
        }
    }

    private unsafe int RepairAppBars()
    {
        var repaired = 0;
        foreach (var hwnd in User32.TopLevelWindows())
        {
            if (!User32.IsWindowVisible(hwnd) || !AppBarRules.IsDockTitle(User32.WindowText(hwnd)))
            {
                continue;
            }

            if (!string.Equals(_context.Desktop.Processes.NameOf(User32.ProcessId(hwnd)), "zebar", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var info = MonitorInfoEx.Create();
            if (!User32.GetWindowRect(hwnd, out var bounds) || !User32.GetMonitorInfo(User32.MonitorFromWindow(hwnd, User32.MonitorDefaultToNearest), ref info))
            {
                continue;
            }

            if (AppBarRules.Classify(hwnd, bounds, info.Monitor) is not { } dock || AppBarRules.IsReserved(dock, info.Work))
            {
                continue;
            }

            if (!AppBarRules.TouchesItsEdge(dock, info.Monitor))
            {
                _context.Log.Warning("shell", $"A Caelestia {dock.Edge} dock was pushed away from its screen edge; it is left alone so reservations cannot pile up. Restart Zebar to fix it.");
                continue;
            }

            var data = new AppBarData { Size = sizeof(AppBarData), Hwnd = hwnd, Edge = (uint)dock.Edge, Rect = dock.Bounds };
            Shell32.SHAppBarMessage(Shell32.AbmNew, ref data);
            data.Rect = dock.Bounds;
            Shell32.SHAppBarMessage(Shell32.AbmSetPos, ref data);
            repaired++;
        }

        return repaired;
    }
}
