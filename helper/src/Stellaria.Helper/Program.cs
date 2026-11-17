using System.Reflection;
using Stellaria.Helper.Configuration;
using Stellaria.Helper.Desktop;
using Stellaria.Helper.Hosting;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Modules;
using Stellaria.Helper.Security;
using Stellaria.Helper.Server;
using Stellaria.Helper.Setup;

namespace Stellaria.Helper;

internal static class Program
{
    public const string InstanceName = @"Local\Stellaria.Helper";
    public const string QuitEventName = @"Local\Stellaria.Helper.Quit";
    public static readonly TimeSpan QuitWait = TimeSpan.FromSeconds(5);

    private static int Main(string[] args)
    {
        CommandLine options;
        try
        {
            options = CommandLine.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        if (options.Quit)
        {
            return RequestQuit() ? 0 : 1;
        }

        var paths = AppPaths.ForCurrentUser(options.PackDirectory);
        paths.EnsureCreated();
        using var log = new Log(paths.LogDirectory, options.Verbose ? LogLevel.Debug : LogLevel.Info);
        var state = new HelperState(Path.Combine(paths.DataDirectory, "state.json"));

        if (options.RestoreShell)
        {
            return RestoreShell(state, log, paths);
        }

        if (options.Setup is { } verb)
        {
            return RunSetup(verb, options, paths, state, log);
        }

        using var instance = SingleInstance.Acquire(InstanceName);
        if (!instance.IsOwner)
        {
            return 0;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            ShellRecovery.ShowTaskbar(log);
            log.Error("host", "Unhandled exception, the taskbar was shown again.", eventArgs.ExceptionObject as Exception);
        };

        using var quit = new EventWaitHandle(false, EventResetMode.ManualReset, QuitEventName);
        try
        {
            RunAsync(paths, log, state, quit).GetAwaiter().GetResult();
            return 0;
        }
        catch (Exception exception)
        {
            log.Error("host", "Helper stopped because of an unhandled exception.", exception);
            return 1;
        }
        finally
        {
            ShellRecovery.ShowTaskbar(log);
        }
    }

    private static async Task RunAsync(AppPaths paths, Log log, HelperState state, EventWaitHandle quit)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        log.Info("host", $"Stellaria helper {version} starting for pack {paths.PackDirectory}.");
        using var shutdown = new CancellationTokenSource();
        var registration = ThreadPool.RegisterWaitForSingleObject(quit, (_, _) => shutdown.Cancel(), null, Timeout.Infinite, executeOnlyOnce: true);
        try
        {
            var hub = new StateHub();
            var router = new RequestRouter(log);
            using var loop = new MessageLoop(log);
            await loop.StartAsync().ConfigureAwait(false);
            await DesktopEventHooks.InstallAsync(loop).ConfigureAwait(false);
            var config = new ConfigStore(paths.ConfigFile, log);
            config.Load();
            var desktop = new DesktopReader(new ProcessDirectory());
            var access = new AccessRegistry();
            access.LoadCommunity(AccessRegistry.ParsePacks(ReadIfExists(Path.Combine(paths.DataDirectory, AccessRegistry.PacksFileName)), ZebarRoot()));
            var context = new ModuleContext(hub, router, loop, log, paths, config, desktop, new ForegroundTracker(desktop), new IconCache(log), TimeProvider.System, shutdown.Token, access);
            await new ModuleHost(log).StartAsync(CreateModules(state), context).ConfigureAwait(false);

            await using var server = new HelperServer(hub, router, log, version, TimeProvider.System, access);
            server.Start();
            access.WriteRuntimeFiles(paths, server.Port, Environment.ProcessId, log);
            try
            {
                await server.RunAsync(shutdown.Token).ConfigureAwait(false);
            }
            finally
            {
                access.DeleteRuntimeFiles(paths, Environment.ProcessId);
                log.Info("host", "Helper stopped.");
            }
        }
        finally
        {
            registration.Unregister(null);
        }
    }

    public static string ZebarRoot() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".glzr", "zebar");

    private static string? ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    private static IEnumerable<IHelperModule> CreateModules(HelperState state) =>
    [
        new ConfigModule(),
        new MonitorsModule(),
        new FocusModule(),
        new TasksModule(),
        new ShellModule(state),
        new KeyboardModule(),
        new DesktopsModule(),
        new IdleModule(),
        new PowerModule(),
        new HotkeysModule(),
        new AudioModule(),
        new DisplayModule(),
        new RadiosModule(),
        new WifiModule(),
        new BluetoothModule(),
        new ClipboardModule(),
        new MediaModule(),
        new NotificationsModule(),
        new WindowModule(),
        new AppsModule(),
        new WallpapersModule(),
        new SystemModule(),
        new ThemeSourceModule(),
        new ThemeModule(),
        new WindowsColorsModule(),
        new CatalogModule(),
        new HealthModule(),
    ];

    private static int RunSetup(string verb, CommandLine options, AppPaths paths, HelperState state, Log log)
    {
        var setup = new SetupCommands(paths, state, log);
        try
        {
            return verb switch
            {
                "install" => setup.Install(options.Zebar, options.Switcher),
                "uninstall" => setup.Uninstall(),
                "shutdown" => setup.Shutdown(),
                "start" => setup.Start(),
                _ => 2,
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine(exception.Message);
            log.Error("setup", $"--{verb} failed.", exception);
            return 1;
        }
    }

    public static void StopRunningHelper()
    {
        RequestQuit();
        var deadline = DateTime.UtcNow + QuitWait;
        while (DateTime.UtcNow < deadline)
        {
            using var probe = SingleInstance.Acquire(InstanceName);
            if (probe.IsOwner)
            {
                return;
            }

            Thread.Sleep(100);
        }

        foreach (var process in System.Diagnostics.Process.GetProcessesByName("stellaria-helper"))
        {
            using (process)
            {
                if (process.Id == Environment.ProcessId)
                {
                    continue;
                }

                try
                {
                    process.Kill();
                    process.WaitForExit(QuitWait);
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    Console.Error.WriteLine($"The running helper could not be stopped: {exception.Message}");
                }
            }
        }
    }

    private static int RestoreShell(HelperState state, Log log, AppPaths paths)
    {
        new SetupCommands(paths, state, log).Shutdown();
        ShellRecovery.RestoreOriginalTaskbar(state, log);
        WindowsColorsModule.RestoreFromBackup(paths.DataDirectory, log);
        log.Info("recovery", "Shell restored on request.");
        return 0;
    }

    private static bool RequestQuit()
    {
        if (!EventWaitHandle.TryOpenExisting(QuitEventName, out var quit))
        {
            return false;
        }

        using (quit)
        {
            return quit.Set();
        }
    }
}
