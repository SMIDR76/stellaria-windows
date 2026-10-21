using System.Text.Json.Nodes;
using Caelestia.Helper.Protocol;
using Windows.Devices.Radios;

namespace Caelestia.Helper.Modules;

internal sealed class RadiosModule : IHelperModule
{
    public static readonly IReadOnlyDictionary<RadioKind, string> KindNames = new Dictionary<RadioKind, string>
    {
        [RadioKind.WiFi] = "wifi",
        [RadioKind.Bluetooth] = "bluetooth",
        [RadioKind.MobileBroadband] = "mobile",
        [RadioKind.FM] = "fm",
    };

    public string Name => "radios";

    public Task StartAsync(ModuleContext context)
    {
        context.Router.Register("radios.list", async (_, _) => ToJson(await Radio.GetRadiosAsync()));
        context.Router.Register("radios.set", async (request, _) =>
        {
            var kind = request.RequireString("kind");
            var on = request.RequireBoolean("on");
            if (!KindNames.Values.Contains(kind))
            {
                throw new HelperException(ErrorCode.InvalidParams, "Unknown radio kind.");
            }

            if (await Radio.RequestAccessAsync() != RadioAccessStatus.Allowed)
            {
                throw new HelperException(ErrorCode.Denied, "Windows does not allow changing radios. Check the privacy settings for radios.");
            }

            var radios = await Radio.GetRadiosAsync();
            var radio = radios.FirstOrDefault(candidate => KindName(candidate.Kind) == kind)
                ?? throw new HelperException(ErrorCode.NotFound, "This device has no such radio.");
            var result = await radio.SetStateAsync(on ? RadioState.On : RadioState.Off);
            if (result != RadioAccessStatus.Allowed)
            {
                throw new HelperException(ErrorCode.Denied, "Windows refused to change the radio.");
            }

            return ToJson(await Radio.GetRadiosAsync());
        });
        return Task.CompletedTask;
    }

    public static string KindName(RadioKind kind) => KindNames.GetValueOrDefault(kind, "other");

    private static JsonArray ToJson(IEnumerable<Radio> radios)
    {
        var list = new JsonArray();
        foreach (var radio in radios)
        {
            JsonNode item = new JsonObject { ["kind"] = KindName(radio.Kind), ["on"] = radio.State == RadioState.On, ["name"] = radio.Name };
            list.Add(item);
        }

        return list;
    }
}
