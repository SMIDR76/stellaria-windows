using Caelestia.Helper.Hosting;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Desktop;

/// <summary>
/// Restarts Zebar when its main thread stops answering. A hung Zebar freezes every Caelestia window at once, while
/// the widget pages, which run in separate WebView2 processes, keep sending heartbeats, so the widget health check
/// alone cannot see it.
/// </summary>
internal sealed class ZebarWatchdog(Log log, ProcessDirectory processes)
{
    public const string ProbeWindowTitle = "Zebar - caelestia / bar";
    public static readonly TimeSpan HungLimit = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan RestartCooldown = TimeSpan.FromMinutes(10);

    private DateTimeOffset? _hungSince;
    private DateTimeOffset _lastRestart = DateTimeOffset.MinValue;

    /// <summary>Decides from successive observations whether Zebar should be restarted now.</summary>
    public static bool ShouldRestart(DateTimeOffset? hungSince, DateTimeOffset now, DateTimeOffset lastRestart) =>
        hungSince is { } since && now - since >= HungLimit && now - lastRestart >= RestartCooldown;

    public void Check(DateTimeOffset now)
    {
        var window = User32.FindWindow(null, ProbeWindowTitle);
        if (window == 0 || !User32.IsHungAppWindow(window))
        {
            if (_hungSince is not null)
            {
                log.Info("watchdog", "Zebar answers again.");
            }

            _hungSince = null;
            return;
        }

        if (_hungSince is null)
        {
            _hungSince = now;
            log.Warning("watchdog", "Zebar stopped answering; it is restarted if this lasts 15 seconds.");
        }

        if (!ShouldRestart(_hungSince, now, _lastRestart))
        {
            return;
        }

        User32.GetWindowThreadProcessId(window, out var processId);
        if (processes.Find(processId) is not { } zebar || !zebar.Name.Equals("zebar", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastRestart = now;
        _hungSince = null;
        log.Warning("watchdog", "Zebar has not answered for 15 seconds, restarting it.");
        Terminate(processId);
        ReleaseWorkAreas();
        if (!ShellExecute.Open(zebar.ImagePath))
        {
            log.Error("watchdog", "Zebar could not be started again.");
        }
    }

    private static void Terminate(uint processId)
    {
        var process = Kernel32.OpenProcess(Kernel32.ProcessTerminate, false, processId);
        if (process == 0)
        {
            return;
        }

        try
        {
            Kernel32.TerminateProcess(process, 1);
        }
        finally
        {
            Kernel32.CloseHandle(process);
        }

        Thread.Sleep(1500);
    }

    /// <summary>
    /// A killed Zebar cannot unregister its docked bars, and Windows keeps their reservations. Giving every screen
    /// its full area back lets the new instance dock against the real edges instead of next to the old bars.
    /// </summary>
    private static void ReleaseWorkAreas()
    {
        foreach (var monitor in DesktopReader.Monitors())
        {
            var bounds = monitor.Bounds;
            User32.SystemParametersInfo(User32.SpiSetWorkArea, 0, ref bounds, 0x2);
        }
    }
}
