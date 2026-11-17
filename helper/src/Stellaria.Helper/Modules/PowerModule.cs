using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Protocol;

namespace Stellaria.Helper.Modules;

internal sealed partial class PowerModule : IHelperModule
{
    public static readonly IReadOnlyDictionary<string, string> ShutdownArguments = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["logout"] = "/l",
        ["restart"] = "/r /t 0",
        ["shutdown"] = "/s /t 0",
    };

    private bool _keepAwake;

    public string Name => "power";

    public Task StartAsync(ModuleContext context)
    {
        context.Router.Register("power.keepAwake", async (request, _) =>
        {
            var enabled = request.RequireBoolean("enabled");
            await context.Loop.InvokeAsync(() =>
            {
                Kernel32.SetThreadExecutionState(enabled
                    ? Kernel32.EsContinuous | Kernel32.EsSystemRequired | Kernel32.EsDisplayRequired
                    : Kernel32.EsContinuous);
                _keepAwake = enabled;
            }).ConfigureAwait(false);
            context.Hub.Publish("power", new JsonObject { ["keepAwake"] = _keepAwake });
            return null;
        });
        context.Router.Register("power.run", (request, _) =>
        {
            Run(request.RequireString("action"));
            return Task.FromResult<JsonNode?>(null);
        });
        context.Hub.Publish("power", new JsonObject { ["keepAwake"] = false });
        return Task.CompletedTask;
    }

    private static void Run(string action)
    {
        switch (action)
        {
            case "lock":
                if (!User32.LockWorkStation())
                {
                    throw new HelperException(ErrorCode.Failed, "Windows refused to lock the session.");
                }

                break;
            case "sleep":
                if (SetSuspendState(false, false, false) == 0)
                {
                    throw new HelperException(ErrorCode.Failed, "Windows refused to go to sleep.");
                }

                break;
            case var name when ShutdownArguments.TryGetValue(name, out var arguments):
                var shutdown = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shutdown.exe");
                using (Process.Start(new ProcessStartInfo(shutdown, arguments) { UseShellExecute = false, CreateNoWindow = true }))
                {
                }

                break;
            default:
                throw new HelperException(ErrorCode.InvalidParams, "Unknown power action.");
        }
    }

    [LibraryImport("powrprof.dll")]
    private static partial byte SetSuspendState([MarshalAs(UnmanagedType.U1)] bool hibernate, [MarshalAs(UnmanagedType.U1)] bool force, [MarshalAs(UnmanagedType.U1)] bool disableWakeEvents);
}
