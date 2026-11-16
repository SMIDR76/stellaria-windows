using System.Text.Json.Nodes;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Server;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Caelestia.Helper.Modules;

internal sealed class BluetoothModule : IHelperModule
{
    public const int MaxDeviceIdLength = 512;
    public static readonly TimeSpan DiscoveryTime = TimeSpan.FromSeconds(8);

    public string Name => "bluetooth";

    public Task StartAsync(ModuleContext context)
    {
        context.Router.Register("bluetooth.devices", async (_, _) =>
        {
            var list = new JsonArray();
            foreach (var info in await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true)))
            {
                using var device = await BluetoothDevice.FromIdAsync(info.Id);
                list.Add((JsonNode)Describe(info, device?.ConnectionStatus == BluetoothConnectionStatus.Connected, KindOf(device)));
            }

            foreach (var info in await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true)))
            {
                using var device = await BluetoothLEDevice.FromIdAsync(info.Id);
                list.Add((JsonNode)Describe(info, device?.ConnectionStatus == BluetoothConnectionStatus.Connected, KindOf(device)));
            }

            return list;
        });
        context.Router.Register("bluetooth.discover", async (_, cancellation) =>
        {
            var found = new Dictionary<string, DeviceInformation>(StringComparer.Ordinal);
            var watcher = DeviceInformation.CreateWatcher(
                BluetoothDevice.GetDeviceSelectorFromPairingState(false),
                null,
                DeviceInformationKind.AssociationEndpoint);
            watcher.Added += (_, info) =>
            {
                lock (found)
                {
                    if (!string.IsNullOrWhiteSpace(info.Name))
                    {
                        found[info.Id] = info;
                    }
                }
            };
            watcher.Updated += (_, _) => { };
            watcher.Start();
            try
            {
                await Task.Delay(DiscoveryTime, cancellation).ConfigureAwait(false);
            }
            finally
            {
                if (watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
                {
                    watcher.Stop();
                }
            }

            var list = new JsonArray();
            lock (found)
            {
                foreach (var info in found.Values.OrderBy(info => info.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    list.Add((JsonNode)Describe(info, false, "other"));
                }
            }

            return list;
        });
        context.Router.Register("bluetooth.pair", async (request, _) =>
        {
            var info = await RequireDeviceAsync(request).ConfigureAwait(false);
            var result = await info.Pairing.PairAsync();
            return new JsonObject { ["status"] = PairingName(result.Status) };
        });
        context.Router.Register("bluetooth.unpair", async (request, _) =>
        {
            var info = await RequireDeviceAsync(request).ConfigureAwait(false);
            var result = await info.Pairing.UnpairAsync();
            if (result.Status is not (DeviceUnpairingResultStatus.Unpaired or DeviceUnpairingResultStatus.AlreadyUnpaired))
            {
                throw new HelperException(ErrorCode.Failed, "Windows could not forget this device.");
            }

            return null;
        });
        return Task.CompletedTask;
    }

    public static string PairingName(DevicePairingResultStatus status) => status switch
    {
        DevicePairingResultStatus.Paired or DevicePairingResultStatus.AlreadyPaired => "paired",
        DevicePairingResultStatus.AuthenticationFailure or DevicePairingResultStatus.AuthenticationTimeout => "authentication_failed",
        DevicePairingResultStatus.ConnectionRejected or DevicePairingResultStatus.RejectedByHandler or DevicePairingResultStatus.PairingCanceled => "rejected",
        DevicePairingResultStatus.NotReadyToPair or DevicePairingResultStatus.NoSupportedProfiles => "not_ready",
        _ => "failed",
    };

    public static string KindOf(BluetoothMajorClass major) => major switch
    {
        BluetoothMajorClass.AudioVideo => "audio",
        BluetoothMajorClass.Peripheral => "input",
        BluetoothMajorClass.Phone => "phone",
        BluetoothMajorClass.Computer => "computer",
        BluetoothMajorClass.Wearable => "watch",
        _ => "other",
    };

    private static string KindOf(BluetoothDevice? device) => device is null ? "other" : KindOf(device.ClassOfDevice.MajorClass);

    private static string KindOf(BluetoothLEDevice? device) => device?.Appearance.Category switch
    {
        null => "other",
        var category when category == BluetoothLEAppearanceCategories.HumanInterfaceDevice => "input",
        var category when category == BluetoothLEAppearanceCategories.Watch => "watch",
        var category when category == BluetoothLEAppearanceCategories.Phone => "phone",
        var category when category == BluetoothLEAppearanceCategories.Computer => "computer",
        _ => "other",
    };

    private static JsonObject Describe(DeviceInformation info, bool connected, string kind) => new()
    {
        ["id"] = info.Id,
        ["name"] = info.Name,
        ["connected"] = connected,
        ["kind"] = kind,
    };

    private static async Task<DeviceInformation> RequireDeviceAsync(RequestContext request)
    {
        var id = request.RequireString("id");
        if (id.Length > MaxDeviceIdLength || !id.StartsWith("Bluetooth", StringComparison.OrdinalIgnoreCase))
        {
            throw new HelperException(ErrorCode.InvalidParams, "This is not a Bluetooth device id.");
        }

        try
        {
            return await DeviceInformation.CreateFromIdAsync(id);
        }
        catch (Exception exception) when (exception is ArgumentException or System.Runtime.InteropServices.COMException)
        {
            throw new HelperException(ErrorCode.NotFound, "This Bluetooth device is not available.");
        }
    }
}
