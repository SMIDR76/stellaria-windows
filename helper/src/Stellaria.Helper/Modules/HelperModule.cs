using Stellaria.Helper.Configuration;
using Stellaria.Helper.Desktop;
using Stellaria.Helper.Hosting;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Server;

namespace Stellaria.Helper.Modules;

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
    CancellationToken Shutdown,
    Security.AccessRegistry? Access = null);

internal interface IHelperModule
{
    string Name { get; }

    Task StartAsync(ModuleContext context);
}

internal sealed class ModuleHost(Log log)
{
    public async Task StartAsync(IEnumerable<IHelperModule> modules, ModuleContext context)
    {
        foreach (var module in modules)
        {
            try
            {
                await module.StartAsync(context).ConfigureAwait(false);
                log.Info("modules", $"{module.Name} started.");
            }
            catch (Exception exception)
            {
                log.Error("modules", $"{module.Name} could not start and stays disabled.", exception);
            }
        }
    }
}
