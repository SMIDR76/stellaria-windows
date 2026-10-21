using System.Reflection;
using Caelestia.Helper.Configuration;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Modules;
using Caelestia.Helper.Server;

namespace Caelestia.Helper;

internal static class Program
{
    public const string InstanceName = @"Local\Caelestia.Helper";
    public const string QuitEventName = @"Local\Caelestia.Helper.Quit";
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
            return RestoreShell(state, log);
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
        log.Info("host", $"Caelestia helper {version} starting for pack {paths.PackDirectory}.");
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
            var context = new ModuleContext(hub, router, loop, log, paths, config, desktop, new ForegroundTracker(desktop), new IconCache(log), TimeProvider.System, shutdown.Token);
            await new ModuleHost(log).StartAsync(CreateModules(state), context).ConfigureAwait(false);

            await using var server = new HelperServer(hub, router, log, version, TimeProvider.System);
            server.Start();
            RuntimeFile.Write(paths.RuntimeFile, server.Port, server.Token, Environment.ProcessId);
            try
            {
                await server.RunAsync(shutdown.Token).ConfigureAwait(false);
            }
            finally
            {
                RuntimeFile.DeleteIfOwned(paths.RuntimeFile, Environment.ProcessId);
                log.Info("host", "Helper stopped.");
            }
        }
        finally
        {
            registration.Unregister(null);
        }
    }

    private static IEnumerable<IHelperModule> CreateModules(HelperState state) =>
    [
        new ConfigModule(),
        new MonitorsModule(),
        new FocusModule(),
        new TasksModule(),
        new ShellModule(state),
        new KeyboardModule(),
        new IdleModule(),
        new PowerModule(),
        new HotkeysModule(),
        new AudioModule(),
        new DisplayModule(),
        new RadiosModule(),
        new ClipboardModule(),
        new MediaModule(),
        new WindowModule(),
        new AppsModule(),
        new WallpapersModule(),
        new SystemModule(),
    ];

    private static int RestoreShell(HelperState state, Log log)
    {
        RequestQuit();
        var deadline = DateTime.UtcNow + QuitWait;
        while (DateTime.UtcNow < deadline)
        {
            using var probe = SingleInstance.Acquire(InstanceName);
            if (probe.IsOwner)
            {
                break;
            }

            Thread.Sleep(100);
        }

        ShellRecovery.RestoreOriginalTaskbar(state, log);
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
