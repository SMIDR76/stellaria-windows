using System.Text.Json.Nodes;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Modules;

internal sealed class DisplayModule : IHelperModule
{
    public string Name => "display";

    public Task StartAsync(ModuleContext context)
    {
        context.Router.Register("display.brightness", (_, _) => Task.Run<JsonNode?>(() =>
        {
            var result = new JsonObject();
            foreach (var (device, percent) in MonitorBrightness.ReadAll())
            {
                result[device] = percent;
            }

            return result;
        }));
        context.Router.Register("display.setBrightness", (request, _) =>
        {
            var monitor = request.RequireString("monitor");
            var value = request.RequireInteger("value", 0, 100);
            JsonNode? Apply()
            {
                if (!MonitorBrightness.Write(monitor, value))
                {
                    throw new HelperException(ErrorCode.Unsupported, "This monitor does not accept brightness changes over DDC/CI.");
                }

                return null;
            }

            return Task.Run(Apply);
        });
        return Task.CompletedTask;
    }
}
