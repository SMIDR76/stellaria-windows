using System.Text.Json.Nodes;
using Stellaria.Helper.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Stellaria.Helper.Modules;

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
            var playback = session.GetPlaybackInfo();
            return new JsonObject
            {
                ["app"] = session.SourceAppUserModelId,
                ["title"] = properties.Title,
                ["artist"] = properties.Artist,
                ["album"] = properties.AlbumTitle,
                ["image"] = await ArtworkAsync(properties.Thumbnail, context).ConfigureAwait(false),
                ["playing"] = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                ["shuffle"] = playback.IsShuffleActive,
                ["repeat"] = RepeatName(playback.AutoRepeatMode),
                ["canShuffle"] = playback.Controls.IsShuffleEnabled,
                ["canRepeat"] = playback.Controls.IsRepeatEnabled,
            };
        });
        context.Router.Register("media.control", async (request, _) =>
        {
            var action = request.RequireString("action");
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var session = manager.GetCurrentSession()
                ?? throw new Protocol.HelperException(Protocol.ErrorCode.NotFound, "Nothing is playing.");
            var playback = session.GetPlaybackInfo();
            var done = action switch
            {
                "playPause" => await session.TryTogglePlayPauseAsync(),
                "next" => await session.TrySkipNextAsync(),
                "previous" => await session.TrySkipPreviousAsync(),
                "shuffle" => await session.TryChangeShuffleActiveAsync(!(playback.IsShuffleActive ?? false)),
                "repeat" => await session.TryChangeAutoRepeatModeAsync(NextRepeat(playback.AutoRepeatMode)),
                _ => throw new Protocol.HelperException(Protocol.ErrorCode.InvalidParams, "Unknown media action."),
            };
            if (!done)
            {
                throw new Protocol.HelperException(Protocol.ErrorCode.Unsupported, "The player refused this action.");
            }

            return null;
        });
        return Task.CompletedTask;
    }

    public static string RepeatName(Windows.Media.MediaPlaybackAutoRepeatMode? mode) => mode switch
    {
        Windows.Media.MediaPlaybackAutoRepeatMode.Track => "track",
        Windows.Media.MediaPlaybackAutoRepeatMode.List => "list",
        _ => "none",
    };

    public static Windows.Media.MediaPlaybackAutoRepeatMode NextRepeat(Windows.Media.MediaPlaybackAutoRepeatMode? mode) => mode switch
    {
        Windows.Media.MediaPlaybackAutoRepeatMode.List => Windows.Media.MediaPlaybackAutoRepeatMode.Track,
        Windows.Media.MediaPlaybackAutoRepeatMode.Track => Windows.Media.MediaPlaybackAutoRepeatMode.None,
        _ => Windows.Media.MediaPlaybackAutoRepeatMode.List,
    };

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
