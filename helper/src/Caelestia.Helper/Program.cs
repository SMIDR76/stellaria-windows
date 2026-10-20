using System.Reflection;
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

        using var instance = SingleInstance.Acquire(InstanceName);
        if (!instance.IsOwner)
        {
            return 0;
        }

        var paths = AppPaths.ForCurrentUser(options.PackDirectory);
        paths.EnsureCreated();
        using var log = new Log(paths.LogDirectory, options.Verbose ? LogLevel.Debug : LogLevel.Info);
        using var quit = new EventWaitHandle(false, EventResetMode.ManualReset, QuitEventName);
        try
        {
            RunAsync(paths, log, quit).GetAwaiter().GetResult();
            return 0;
        }
        catch (Exception exception)
        {
            log.Error("host", "Helper stopped because of an unhandled exception.", exception);
            return 1;
        }
    }

    private static async Task RunAsync(AppPaths paths, Log log, EventWaitHandle quit)
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
            var desktop = new DesktopReader(new ProcessDirectory());
            var context = new ModuleContext(hub, router, loop, log, paths, desktop, new ForegroundTracker(desktop), new IconCache(log), TimeProvider.System, shutdown.Token);
            await new ModuleHost(log).StartAsync(CreateModules(), context).ConfigureAwait(false);

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

    private static IEnumerable<IHelperModule> CreateModules() =>
    [
        new MonitorsModule(),
        new FocusModule(),
        new TasksModule(),
    ];

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
