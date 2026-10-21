using System.Text.Json.Nodes;
using Caelestia.Helper.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Caelestia.Helper.Modules;

internal sealed class MediaModule : IHelperModule
{
    public const int ArtworkSize = 320;
    public const long MaxArtworkBytes = 10 * 1024 * 1024;

    public string Name => "media";

    public Task StartAsync(ModuleContext context)
    {
        context.Router.Register("media.artwork", async (_, _) =>
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var session = manager.GetCurrentSession();
            if (session is null)
            {
                return null;
            }

            var properties = await session.TryGetMediaPropertiesAsync();
            return new JsonObject
            {
                ["app"] = session.SourceAppUserModelId,
                ["title"] = properties.Title,
                ["artist"] = properties.Artist,
                ["album"] = properties.AlbumTitle,
                ["image"] = await ArtworkAsync(properties.Thumbnail, context).ConfigureAwait(false),
            };
        });
        return Task.CompletedTask;
    }

    private static async Task<string?> ArtworkAsync(IRandomAccessStreamReference? thumbnail, ModuleContext context)
    {
        if (thumbnail is null)
        {
            return null;
        }

        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            if (stream.Size is 0 or > MaxArtworkBytes)
            {
                return null;
            }

            var bytes = new byte[stream.Size];
            using (var reader = new DataReader(stream))
            {
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(bytes);
            }

            var image = await ImageCodec.DecodeAsync(bytes, ArtworkSize, ArtworkSize, ImageFit.Cover).ConfigureAwait(false);
            return Convert.ToBase64String(await ImageCodec.EncodeJpegAsync(image).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Runtime.InteropServices.COMException)
        {
            context.Log.Warning("media", "The artwork of the current media could not be read.", exception);
            return null;
        }
    }
}
