using Caelestia.Helper.Hosting;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Desktop;

internal sealed class ZebarWatchdog(Log log, ProcessDirectory processes)
{
    public const string ProbeWindowTitle = "Zebar - caelestia / bar";
    public static readonly TimeSpan HungLimit = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan RestartCooldown = TimeSpan.FromMinutes(10);

    private static readonly Lock s_gate = new();
    private static DateTimeOffset s_lastRestart = DateTimeOffset.MinValue;

    private DateTimeOffset? _hungSince;

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

        if (!ShouldRestart(_hungSince, now, LastRestart))
        {
            return;
        }

        User32.GetWindowThreadProcessId(window, out var processId);
        if (TryRestart(log, processes, processId, now, "Zebar has not answered for 15 seconds"))
        {
            _hungSince = null;
        }
    }

    public static bool TryRestartRunning(Log log, ProcessDirectory processes, DateTimeOffset now, string reason, bool ignoreCooldown = false)
    {
        var window = User32.FindWindow(null, ProbeWindowTitle);
        if (window == 0)
        {
            return false;
        }

        User32.GetWindowThreadProcessId(window, out var processId);
        return TryRestart(log, processes, processId, now, reason, ignoreCooldown);
    }

    public static DateTimeOffset LastRestart
    {
        get
        {
            lock (s_gate)
            {
                return s_lastRestart;
            }
        }
    }

    public static bool TryRestart(Log log, ProcessDirectory processes, uint processId, DateTimeOffset now, string reason, bool ignoreCooldown = false)
    {
        lock (s_gate)
        {
            if (!ignoreCooldown && now - s_lastRestart < RestartCooldown)
            {
                return false;
            }

            if (processes.Find(processId) is not { } zebar || !zebar.Name.Equals("zebar", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            s_lastRestart = now;
            log.Warning("watchdog", $"{reason}, restarting Zebar.");
            Terminate(processId);
            ReleaseWorkAreas();
            if (!ShellExecute.Open(zebar.ImagePath))
            {
                log.Error("watchdog", "Zebar could not be started again.");
            }

            return true;
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

    public static void ReleaseWorkAreas()
    {
        foreach (var monitor in DesktopReader.Monitors())
        {
            var bounds = monitor.Bounds;
            User32.SystemParametersInfo(User32.SpiSetWorkArea, 0, ref bounds, 0x2);
        }
    }
}
