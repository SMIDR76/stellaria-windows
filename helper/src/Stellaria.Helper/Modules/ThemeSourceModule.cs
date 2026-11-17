using System.Diagnostics;
using System.Text.Json.Nodes;
using Stellaria.Helper.Imaging;
using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Modules;

internal sealed class ThemeSourceModule : IHelperModule
{
    public const int SampleWidth = 160;
    public const double ChangeThreshold = 18;
    public const uint SetDesktopWallpaper = 0x0014;
    public static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(2500);
    public static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan EngineLookupInterval = TimeSpan.FromSeconds(60);
    public static readonly string[] EngineProcesses = ["wallpaper64", "wallpaper32"];

    private readonly SemaphoreSlim _capturing = new(1, 1);
    private ModuleContext _context = null!;
    private Debouncer _changed = null!;
    private Timer? _check;
    private Timer? _engineLookup;
    private FileSystemWatcher? _engineWatcher;
    private string? _engineDirectory;
    private int[]? _publishedPrint;

    public string Name => "theme-source";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        _changed = new Debouncer(context.Loop, SettleDelay, () => _ = CaptureAsync(force: true));
        context.Loop.BroadcastReceived += (message, wParam) =>
        {
            if (message == User32.WmSettingChange && wParam == SetDesktopWallpaper)
            {
                _changed.Schedule();
            }
        };
        _check = new Timer(_ => _ = CaptureAsync(force: false), null, CheckInterval, CheckInterval);
        _engineLookup = new Timer(_ => WatchEngine(), null, TimeSpan.Zero, EngineLookupInterval);
        context.Shutdown.Register(() =>
        {
            _check.Dispose();
            _engineLookup.Dispose();
            _engineWatcher?.Dispose();
        });
        return CaptureAsync(force: true);
    }

    public static bool ShouldPublish(bool force, int[]? published, int[] current) =>
        force || Fingerprint.Difference(published, current) > ChangeThreshold;

    private void WatchEngine()
    {
        try
        {
            var directory = EngineProcesses
                .SelectMany(Process.GetProcessesByName)
                .Select(process =>
                {
                    using (process)
                    {
                        var image = _context.Desktop.Processes.Find((uint)process.Id)?.ImagePath;
                        return string.IsNullOrEmpty(image) ? null : Path.GetDirectoryName(image);
                    }
                })
                .FirstOrDefault(path => path is not null && File.Exists(Path.Combine(path, "config.json")));
            if (string.Equals(directory, _engineDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _engineWatcher?.Dispose();
            _engineWatcher = null;
            _engineDirectory = directory;
            if (directory is null)
            {
                return;
            }

            _engineWatcher = new FileSystemWatcher(directory, "config.json") { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size };
            _engineWatcher.Changed += (_, _) => _changed.Schedule();
            _engineWatcher.EnableRaisingEvents = true;
            _context.Log.Info("theme-source", "Watching Wallpaper Engine for wallpaper changes.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or ArgumentException)
        {
            _context.Log.Warning("theme-source", "Wallpaper Engine could not be watched.", exception);
        }
    }

    private async Task CaptureAsync(bool force)
    {
        if (!await _capturing.WaitAsync(0).ConfigureAwait(false))
        {
            if (force)
            {
                _changed.Schedule();
            }

            return;
        }

        try
        {
            var (image, source) = await Task.Run(SampleAsync).ConfigureAwait(false);
            if (image is null)
            {
                return;
            }

            var print = Fingerprint.Compute(image);
            var publish = ShouldPublish(force, _publishedPrint, print);
            _context.Log.Debug("theme-source", $"Sampled the wallpaper from {source} ({(force ? "announced change" : "periodic check")}), publish: {publish}.");
            if (!publish)
            {
                return;
            }

            _publishedPrint = print;
            var sample = Convert.ToBase64String(await ImageCodec.EncodePngAsync(image, SampleWidth).ConfigureAwait(false));
            _context.Hub.Publish("wallpaper", new JsonObject
            {
                ["path"] = WallpapersModule.CurrentWallpaper(),
                ["sample"] = sample,
                ["source"] = source,
            });
        }
        catch (Exception exception)
        {
            _context.Log.Error("theme-source", "The wallpaper could not be sampled.", exception);
        }
        finally
        {
            _capturing.Release();
        }
    }

    private static async Task<(PixelImage? Image, string Source)> SampleAsync()
    {
        if (DesktopCapture.PrimaryMonitorBackground() is { } capture)
        {
            return (capture, "capture");
        }

        if (WallpapersModule.CurrentWallpaper() is { } path)
        {
            var image = await ImageCodec.DecodeAsync(await File.ReadAllBytesAsync(path).ConfigureAwait(false), SampleWidth * 4, SampleWidth * 4, ImageFit.Contain).ConfigureAwait(false);
            return (image, "file");
        }

        return (null, "none");
    }
}
