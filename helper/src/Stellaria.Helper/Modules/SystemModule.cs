using System.Text.Json.Nodes;
using Stellaria.Helper.Desktop;
using Stellaria.Helper.Imaging;
using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Modules;

internal sealed class SystemModule : IHelperModule
{
    public const int PictureSize = 192;

    private readonly GpuCounters _gpu = new();
    private string? _nvidiaSmi;

    public string Name => "system";

    public Task StartAsync(ModuleContext context)
    {
        _nvidiaSmi = NvidiaSmi.Locate();
        context.Shutdown.Register(_gpu.Dispose);
        context.Router.Register("system.info", async (_, _) =>
        {
            var (name, user) = SystemFacts.Account();
            return new JsonObject
            {
                ["name"] = name,
                ["user"] = user,
                ["picture"] = await PictureAsync(context).ConfigureAwait(false),
                ["cpu"] = SystemFacts.Processor(),
                ["gpu"] = SystemFacts.PrimaryGraphicsAdapter(),
                ["os"] = SystemFacts.OperatingSystem(),
            };
        });
        context.Router.Register("system.gpu", async (_, cancellation) =>
        {
            var nvidia = _nvidiaSmi is null ? null : await NvidiaSmi.ReadAsync(_nvidiaSmi, cancellation).ConfigureAwait(false);
            var usage = nvidia?.Usage ?? _gpu.ThreeDUsage();
            return new JsonObject
            {
                ["name"] = nvidia?.Name ?? SystemFacts.PrimaryGraphicsAdapter(),
                ["usage"] = usage is { } percent ? Math.Round(percent, 1) : null,
                ["temperature"] = nvidia?.Temperature,
            };
        });
        return Task.CompletedTask;
    }

    private static async Task<string?> PictureAsync(ModuleContext context)
    {
        if (SystemFacts.AccountPicturePath() is not { } path)
        {
            return null;
        }

        try
        {
            var image = await ImageCodec.DecodeAsync(await File.ReadAllBytesAsync(path).ConfigureAwait(false), PictureSize, PictureSize, ImageFit.Cover).ConfigureAwait(false);
            return Convert.ToBase64String(await ImageCodec.EncodePngAsync(image).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Runtime.InteropServices.COMException)
        {
            context.Log.Warning("system", "The account picture could not be read.", exception);
            return null;
        }
    }
}
