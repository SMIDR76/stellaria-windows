using Caelestia.Helper.Configuration;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Server;

namespace Caelestia.Helper.Modules;

internal sealed record ModuleContext(
    StateHub Hub,
    RequestRouter Router,
    MessageLoop Loop,
    Log Log,
    AppPaths Paths,
    ConfigStore Config,
    DesktopReader Desktop,
    ForegroundTracker Foreground,
    IconCache Icons,
    TimeProvider Time,
    CancellationToken Shutdown);

internal interface IHelperModule
{
    string Name { get; }

    Task StartAsync(ModuleContext context);
}

internal sealed class ModuleHost(Log log)
{
    private readonly List<IHelperModule> _running = [];

    public IReadOnlyList<IHelperModule> Running => _running;

    public async Task StartAsync(IEnumerable<IHelperModule> modules, ModuleContext context)
    {
        foreach (var module in modules)
        {
            try
            {
                await module.StartAsync(context).ConfigureAwait(false);
                _running.Add(module);
                log.Info("modules", $"{module.Name} started.");
            }
            catch (Exception exception)
            {
                log.Error("modules", $"{module.Name} could not start and stays disabled.", exception);
            }
        }
    }
}
