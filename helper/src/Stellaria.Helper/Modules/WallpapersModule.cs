using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Stellaria.Helper.Desktop;
using Stellaria.Helper.Imaging;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Protocol;
using Microsoft.Win32;

namespace Stellaria.Helper.Modules;

internal sealed class WallpapersModule : IHelperModule
{
    public const int ThumbnailWidth = 384;
    public const int ThumbnailHeight = 216;
    public const int MaxListed = 500;
    public const uint SetDesktopWallpaper = 0x0014;
    public const uint UpdateIniFileAndBroadcast = 0x01 | 0x02;

    public static readonly IReadOnlyDictionary<string, string> SettingsPages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["home"] = "ms-settings:",
        ["sound"] = "ms-settings:sound",
        ["network-status"] = "ms-settings:network-status",
        ["regionlanguage"] = "ms-settings:regionlanguage",
        ["display"] = "ms-settings:display",
        ["bluetooth"] = "ms-settings:bluetooth",
        ["personalization"] = "ms-settings:personalization",
    };

    private ModuleContext _context = null!;

    public string Name => "wallpapers";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        context.Router.Register("wallpapers.list", async (_, _) => await ListAsync().ConfigureAwait(false));
        context.Router.Register("wallpapers.apply", (request, _) =>
        {
            Apply(request.RequireString("path"));
            return Task.FromResult<JsonNode?>(null);
        });
        context.Router.Register("folders.open", (request, _) =>
        {
            OpenFolder(request.RequireString("name"));
            return Task.FromResult<JsonNode?>(null);
        });
        context.Router.Register("shell.openSettings", (request, _) =>
        {
            var page = request.RequireString("page");
            if (!SettingsPages.TryGetValue(page, out var uri))
            {
                throw new HelperException(ErrorCode.InvalidParams, "Unknown settings page.");
            }

            if (!ShellExecute.Open(uri))
            {
                throw new HelperException(ErrorCode.Failed, "Windows could not open the settings page.");
            }

            return Task.FromResult<JsonNode?>(null);
        });
        return Task.CompletedTask;
    }

    public static string? CurrentWallpaper()
    {
        using var desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
        return desktop?.GetValue("WallPaper") is string path && File.Exists(path) ? path : null;
    }

    public static string ThumbnailKey(string path, long length, DateTime lastWrite) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{path.ToUpperInvariant()}|{length.ToString(CultureInfo.InvariantCulture)}|{lastWrite.Ticks.ToString(CultureInfo.InvariantCulture)}")));

    private string Directory => SafePaths.WallpaperDirectory(_context.Config.GetString("wallpapers.directory"));

    private async Task<JsonNode> ListAsync()
    {
        var directory = Directory;
        var thumbnails = Path.Combine(_context.Paths.CacheDirectory, "thumbnails");
        System.IO.Directory.CreateDirectory(thumbnails);
        var items = new JsonArray();
        var files = new DirectoryInfo(directory).EnumerateFiles()
            .Where(file => SafePaths.IsImage(file.Name))
            .OrderBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(MaxListed);
        foreach (var file in files)
        {
            var thumbnail = await ThumbnailAsync(file, thumbnails).ConfigureAwait(false);
            if (thumbnail is null)
            {
                continue;
            }

            JsonNode item = new JsonObject { ["path"] = file.FullName, ["name"] = Path.GetFileNameWithoutExtension(file.Name), ["thumb"] = thumbnail };
            items.Add(item);
        }

        return new JsonObject { ["directory"] = directory, ["current"] = CurrentWallpaper(), ["items"] = items };
    }

    private async Task<string?> ThumbnailAsync(FileInfo file, string cache)
    {
        var cached = Path.Combine(cache, ThumbnailKey(file.FullName, file.Length, file.LastWriteTimeUtc) + ".jpg");
        try
        {
            if (!File.Exists(cached))
            {
                var image = await ImageCodec.DecodeAsync(await File.ReadAllBytesAsync(file.FullName).ConfigureAwait(false), ThumbnailWidth, ThumbnailHeight, ImageFit.Cover).ConfigureAwait(false);
                await File.WriteAllBytesAsync(cached, await ImageCodec.EncodeJpegAsync(image).ConfigureAwait(false)).ConfigureAwait(false);
            }

            return Convert.ToBase64String(await File.ReadAllBytesAsync(cached).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Runtime.InteropServices.COMException)
        {
            _context.Log.Warning("wallpapers", "A wallpaper could not be read and is skipped.", exception);
            return null;
        }
    }

    private void Apply(string path)
    {
        if (!SafePaths.IsInside(path, Directory) || !SafePaths.IsImage(path))
        {
            throw new HelperException(ErrorCode.Denied, "Only images from the wallpaper folder can be applied.");
        }

        if (!File.Exists(path))
        {
            throw new HelperException(ErrorCode.NotFound, "This wallpaper no longer exists.");
        }

        if (!User32.SystemParametersInfo(SetDesktopWallpaper, 0, Path.GetFullPath(path), UpdateIniFileAndBroadcast))
        {
            throw new HelperException(ErrorCode.Failed, "Windows refused the wallpaper.");
        }
    }

    private void OpenFolder(string name)
    {
        var folder = name switch
        {
            "wallpapers" => Directory,
            "screenshots" => SafePaths.ScreenshotsDirectory(_context.Config.GetString("screenshots.directory")),
            _ => throw new HelperException(ErrorCode.InvalidParams, "Unknown folder."),
        };
        if (!System.IO.Directory.Exists(folder))
        {
            throw new HelperException(ErrorCode.NotFound, "This folder does not exist.");
        }

        if (!ShellExecute.Open(folder))
        {
            throw new HelperException(ErrorCode.Failed, "Windows could not open the folder.");
        }
    }
}
