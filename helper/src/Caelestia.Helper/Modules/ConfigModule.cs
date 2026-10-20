using System.Text.Json.Nodes;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Modules;

internal sealed class ConfigModule : IHelperModule
{
    public string Name => "config";

    public Task StartAsync(ModuleContext context)
    {
        context.Config.Changed += snapshot => context.Hub.Publish("config", snapshot);
        context.Router.Register("config.set", (request, _) =>
        {
            var path = request.RequireString("path");
            if (!request.Params.TryGetPropertyValue("value", out var value))
            {
                throw new HelperException(ErrorCode.InvalidParams, "Parameter 'value' is required.");
            }

            return Task.FromResult<JsonNode?>(context.Config.Set(path, value));
        });
        context.Hub.Publish("config", context.Config.Snapshot());
        return Task.CompletedTask;
    }
}
