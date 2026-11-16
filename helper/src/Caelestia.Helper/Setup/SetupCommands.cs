using System.Diagnostics;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Modules;
using Microsoft.Win32;

namespace Caelestia.Helper.Setup;

internal sealed class SetupCommands(AppPaths paths, HelperState state, Log log)
{
    public const string SwitcherProcess = "CaelestiaSwitcher";
    public const string ZebarProcess = "zebar";
    public static readonly TimeSpan StopWait = TimeSpan.FromSeconds(5);

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public int Install(string? zebarOverride, string? switcherOverride)
    {
        var zebar = zebarOverride ?? ZebarLocator.Find();
        if (zebar is null || !File.Exists(zebar))
        {
            Console.Error.WriteLine("Zebar was not found. Install it first: winget install -e --id glzr-io.zebar");
            log.Error("setup", "Install stopped: Zebar was not found.");
            return 3;
        }

        var helper = Environment.ProcessPath ?? throw new InvalidOperationException("The helper path is unknown.");
        var switcher = switcherOverride ?? Path.Combine(paths.PackDirectory, "native", "CaelestiaSwitcher.exe");
        var widgets = ZebarSettings.ReadStartupList(paths.PackDirectory);
        var settings = ZebarSettings.DefaultPath();
        ZebarSettings.Write(settings, ZebarSettings.Merge(ZebarSettings.ReadText(settings), widgets));
        log.Info("setup", $"{widgets.Count} Caelestia widgets registered in Zebar's startup list.");

        RemoveLegacyRunEntries();
        ScheduledTasks.Register(new LogonTask(ScheduledTasks.HelperTask, helper, Path.GetDirectoryName(helper)!, TimeSpan.Zero));
        if (File.Exists(switcher))
        {
            ScheduledTasks.Register(new LogonTask(ScheduledTasks.SwitcherTask, switcher, Path.GetDirectoryName(switcher)!, TimeSpan.FromSeconds(1)));
        }
        else
        {
            ScheduledTasks.Delete(ScheduledTasks.SwitcherTask);
        }

        ScheduledTasks.Register(new LogonTask(ScheduledTasks.ZebarTask, zebar, Path.GetDirectoryName(zebar)!, TimeSpan.FromSeconds(3)));
        log.Info("setup", $"Logon tasks registered (Zebar: {zebar}).");
        return 0;
    }

    public int Start()
    {
        foreach (var task in ScheduledTasks.All.Where(ScheduledTasks.Exists))
        {
            ScheduledTasks.Start(task);
            Thread.Sleep(800);
        }

        return 0;
    }

    public int Shutdown()
    {
        foreach (var task in ScheduledTasks.All)
        {
            ScheduledTasks.End(task);
        }

        Program.StopRunningHelper();
        var zebarWasRunning = Kill(ZebarProcess);
        Kill(SwitcherProcess);
        if (zebarWasRunning)
        {
            ZebarWatchdog.ReleaseWorkAreas();
        }

        log.Info("setup", "Caelestia stopped.");
        return 0;
    }

    public int Uninstall()
    {
        Shutdown();
        ShellRecovery.RestoreOriginalTaskbar(state, log);
        WindowsColorsModule.RestoreFromBackup(paths.DataDirectory, log);
        foreach (var task in ScheduledTasks.All)
        {
            ScheduledTasks.Delete(task);
        }

        RemoveLegacyRunEntries();
        var settings = ZebarSettings.DefaultPath();
        if (File.Exists(settings))
        {
            ZebarSettings.Write(settings, ZebarSettings.Unmerge(ZebarSettings.ReadText(settings)));
        }

        log.Info("setup", "Caelestia removed from startup; the taskbar and Windows colours were restored.");
        return 0;
    }

    private static void RemoveLegacyRunEntries()
    {
        using var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        run?.DeleteValue("CaelestiaSwitcher", throwOnMissingValue: false);
        run?.DeleteValue("CaelestiaHelper", throwOnMissingValue: false);
    }

    private static bool Kill(string name)
    {
        var found = false;
        foreach (var process in Process.GetProcessesByName(name))
        {
            using (process)
            {
                found = true;
                try
                {
                    process.Kill();
                    process.WaitForExit(StopWait);
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    Console.Error.WriteLine($"{name} ({process.Id}) could not be stopped: {exception.Message}");
                }
            }
        }

        return found;
    }
}
