using System.Text.Json.Nodes;
using Stellaria.Helper.Protocol;
using Stellaria.Helper.Server;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;

namespace Stellaria.Helper.Modules;

internal sealed record WifiNetwork(string Ssid, int Bars, bool Secured);

internal sealed class WifiModule : IHelperModule
{
    public const int MaxSsidLength = 32;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 63;

    private readonly SemaphoreSlim _gate = new(1, 1);

    public string Name => "wifi";

    public Task StartAsync(ModuleContext context)
    {
        context.Router.Register("wifi.status", async (_, _) =>
        {
            var adapter = await AdapterAsync(requireAccess: false).ConfigureAwait(false);
            return new JsonObject
            {
                ["available"] = adapter is not null,
                ["connected"] = adapter is null ? null : await ConnectedSsidAsync(adapter).ConfigureAwait(false),
            };
        });
        context.Router.Register("wifi.scan", async (_, cancellation) =>
        {
            await _gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                var adapter = await RequireAdapterAsync().ConfigureAwait(false);
                await adapter.ScanAsync();
                var connected = await ConnectedSsidAsync(adapter).ConfigureAwait(false);
                var saved = SavedProfiles();
                var list = new JsonArray();
                foreach (var network in Merge(adapter.NetworkReport.AvailableNetworks.Select(Describe)))
                {
                    list.Add((JsonNode)new JsonObject
                    {
                        ["ssid"] = network.Ssid,
                        ["signal"] = network.Bars,
                        ["secured"] = network.Secured,
                        ["saved"] = saved.Contains(network.Ssid),
                        ["connected"] = network.Ssid == connected,
                    });
                }

                return new JsonObject { ["connected"] = connected, ["networks"] = list };
            }
            finally
            {
                _gate.Release();
            }
        });
        context.Router.Register("wifi.connect", async (request, cancellation) =>
        {
            var ssid = request.RequireString("ssid");
            var password = request.Params["password"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
            Validate(ssid, password);
            await _gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                var adapter = await RequireAdapterAsync().ConfigureAwait(false);
                var network = adapter.NetworkReport.AvailableNetworks
                    .Where(candidate => candidate.Ssid == ssid)
                    .OrderByDescending(candidate => candidate.SignalBars)
                    .FirstOrDefault()
                    ?? throw new HelperException(ErrorCode.NotFound, "This network is no longer in range. Scan again.");
                var result = password is null
                    ? await adapter.ConnectAsync(network, WiFiReconnectionKind.Automatic)
                    : await adapter.ConnectAsync(network, WiFiReconnectionKind.Automatic, new PasswordCredential { Password = password });
                return new JsonObject { ["status"] = StatusName(result.ConnectionStatus) };
            }
            finally
            {
                _gate.Release();
            }
        });
        context.Router.Register("wifi.disconnect", async (_, _) =>
        {
            var adapter = await RequireAdapterAsync().ConfigureAwait(false);
            adapter.Disconnect();
            return null;
        });
        return Task.CompletedTask;
    }

    public static void Validate(string ssid, string? password)
    {
        if (ssid.Length > MaxSsidLength)
        {
            throw new HelperException(ErrorCode.InvalidParams, "A network name has at most 32 characters.");
        }

        if (password is not null && (password.Length < MinPasswordLength || password.Length > MaxPasswordLength))
        {
            throw new HelperException(ErrorCode.InvalidParams, "A Wi-Fi password has 8 to 63 characters.");
        }
    }

    public static IEnumerable<WifiNetwork> Merge(IEnumerable<WifiNetwork> networks) =>
        networks
            .Where(network => network.Ssid.Length > 0)
            .GroupBy(network => network.Ssid, StringComparer.Ordinal)
            .Select(group => group.MaxBy(network => network.Bars)!)
            .OrderByDescending(network => network.Bars)
            .ThenBy(network => network.Ssid, StringComparer.CurrentCultureIgnoreCase);

    public static string StatusName(WiFiConnectionStatus status) => status switch
    {
        WiFiConnectionStatus.Success => "connected",
        WiFiConnectionStatus.InvalidCredential => "invalid_password",
        WiFiConnectionStatus.AccessRevoked => "denied",
        WiFiConnectionStatus.NetworkNotAvailable => "out_of_range",
        WiFiConnectionStatus.Timeout => "timeout",
        WiFiConnectionStatus.UnsupportedAuthenticationProtocol => "unsupported",
        _ => "failed",
    };

    private static WifiNetwork Describe(WiFiAvailableNetwork network) => new(
        network.Ssid,
        network.SignalBars,
        network.SecuritySettings.NetworkAuthenticationType is not (NetworkAuthenticationType.None or NetworkAuthenticationType.Open80211));

    private static async Task<WiFiAdapter?> AdapterAsync(bool requireAccess)
    {
        if (requireAccess && await WiFiAdapter.RequestAccessAsync() != WiFiAccessStatus.Allowed)
        {
            throw new HelperException(
                ErrorCode.Denied,
                "Windows hides Wi-Fi networks from desktop apps without location access. Allow it in Settings, Privacy & security, Location.");
        }

        return (await WiFiAdapter.FindAllAdaptersAsync()).FirstOrDefault();
    }

    private static async Task<WiFiAdapter> RequireAdapterAsync() =>
        await AdapterAsync(requireAccess: true).ConfigureAwait(false)
        ?? throw new HelperException(ErrorCode.Unsupported, "This PC has no Wi-Fi adapter, or it is turned off.");

    private static async Task<string?> ConnectedSsidAsync(WiFiAdapter adapter)
    {
        var profile = await adapter.NetworkAdapter.GetConnectedProfileAsync();
        return profile?.IsWlanConnectionProfile == true ? profile.WlanConnectionProfileDetails.GetConnectedSsid() : null;
    }

    private static HashSet<string> SavedProfiles() =>
        NetworkInformation.GetConnectionProfiles()
            .Where(profile => profile.IsWlanConnectionProfile)
            .Select(profile => profile.ProfileName)
            .ToHashSet(StringComparer.Ordinal);
}
