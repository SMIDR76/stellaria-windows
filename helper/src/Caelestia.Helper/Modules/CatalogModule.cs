using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Caelestia.Helper.Catalog;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Security;
using Caelestia.Helper.Setup;

namespace Caelestia.Helper.Modules;

internal sealed class CatalogModule : IHelperModule
{
    public const string StateFileName = "catalog.json";
    public const string CacheDirectoryName = "registry";
    public const int MaxIndexBytes = 4 * 1024 * 1024;
    public const int MaxHistory = 10;
    public static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(20);

    private readonly Lock _gate = new();
    private ModuleContext _context = null!;
    private IndexEnvelope? _index;
    private string _source = "none";

    public string Name => "catalog";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        context.Router.Register("catalog.index", async (request, cancellation) =>
        {
            var refresh = request.Params["refresh"] is JsonValue flag && flag.TryGetValue<bool>(out var value) && value;
            await LoadAsync(refresh, cancellation).ConfigureAwait(false);
            return Describe();
        });
        context.Router.Register("catalog.apply", async (request, cancellation) =>
        {
            var id = request.RequireString("id");
            await LoadAsync(false, cancellation).ConfigureAwait(false);
            var item = Find(id) ?? throw new HelperException(ErrorCode.NotFound, $"'{id}' is not in the catalogue.");
            if (item["type"]?.GetValue<string>() != "preset" || item["preset"] is not JsonObject preset)
            {
                throw new HelperException(ErrorCode.Unsupported, $"'{id}' is not a preset.");
            }

            UpdateState(state =>
            {
                var history = state["history"] as JsonArray ?? [];
                history.Add((JsonNode?)(state["active"]?.DeepClone()));
                while (history.Count > MaxHistory)
                {
                    history.RemoveAt(0);
                }

                state["history"] = history;
                state["active"] = id;
            });
            context.Hub.Raise("theme.preset", new JsonObject { ["id"] = id, ["name"] = item["name"]?.DeepClone(), ["preset"] = preset.DeepClone() });
            context.Log.Info("catalog", $"Preset {id} applied.");
            return Describe();
        });
        context.Router.Register("catalog.undo", (_, _) =>
        {
            UpdateState(state =>
            {
                var history = state["history"] as JsonArray ?? [];
                state["active"] = history.Count > 0 ? history[^1]?.DeepClone() : null;
                if (history.Count > 0)
                {
                    history.RemoveAt(history.Count - 1);
                }

                state["history"] = history;
            });
            context.Hub.Raise("theme.undo", new JsonObject());
            return Task.FromResult<JsonNode?>(Describe());
        });
        context.Router.Register("launcher.status", (_, _) => Task.FromResult<JsonNode?>(Status()));
        context.Router.Register("launcher.openSettings", (_, _) =>
        {
            context.Hub.Raise("hotkey", new JsonObject { ["name"] = "settings", ["monitor"] = DesktopReader.MonitorUnderCursor() });
            return Task.FromResult<JsonNode?>(null);
        });
        return Task.CompletedTask;
    }

    private async Task LoadAsync(bool refresh, CancellationToken cancellation)
    {
        if (!refresh && _index is not null)
        {
            return;
        }

        var lastVersion = ReadState()["lastIndexVersion"]?.GetValue<long>() ?? 0;
        var candidates = new List<(string Source, Func<Task<(byte[], string)?>> Read)>();
        if (_context.Config.GetString("catalog.url") is { } url)
        {
            candidates.Add(("online", () => DownloadAsync(url, cancellation)));
        }

        candidates.Add(("cache", () => ReadPairAsync(Path.Combine(_context.Paths.DataDirectory, CacheDirectoryName))));
        candidates.Add(("bundled", () => ReadPairAsync(BundledDirectory())));
        foreach (var (source, read) in candidates)
        {
            try
            {
                if (await read().ConfigureAwait(false) is not var (bytes, signature))
                {
                    continue;
                }

                var envelope = SignedIndex.Open(bytes, signature, TrustedKeys.RegistryKey, lastVersion, _context.Time.GetUtcNow());
                lock (_gate)
                {
                    _index = envelope;
                    _source = source;
                }

                UpdateState(state => state["lastIndexVersion"] = envelope.Version);
                if (source != "cache")
                {
                    WriteCache(bytes, signature);
                }

                _context.Log.Info("catalog", $"Catalogue version {envelope.Version} loaded from {source}.");
                return;
            }
            catch (Exception exception) when (exception is CryptographicException or IOException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or FormatException)
            {
                _context.Log.Warning("catalog", $"The {source} catalogue was refused: {exception.Message}");
            }
        }

        if (_index is null)
        {
            throw new HelperException(ErrorCode.NotFound, "No valid signed catalogue is available.");
        }
    }

    private static async Task<(byte[], string)?> DownloadAsync(string url, CancellationToken cancellation)
    {
        using var client = new HttpClient { Timeout = DownloadTimeout, MaxResponseContentBufferSize = MaxIndexBytes };
        var bytes = await client.GetByteArrayAsync(url, cancellation).ConfigureAwait(false);
        var signature = await client.GetStringAsync(url + ".minisig", cancellation).ConfigureAwait(false);
        return (bytes, signature);
    }

    private static async Task<(byte[], string)?> ReadPairAsync(string directory)
    {
        var index = Path.Combine(directory, "index.json");
        var signature = index + ".minisig";
        if (!File.Exists(index) || !File.Exists(signature) || new FileInfo(index).Length > MaxIndexBytes)
        {
            return null;
        }

        return (await File.ReadAllBytesAsync(index).ConfigureAwait(false), await File.ReadAllTextAsync(signature).ConfigureAwait(false));
    }

    private void WriteCache(byte[] bytes, string signature)
    {
        var directory = Path.Combine(_context.Paths.DataDirectory, CacheDirectoryName);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "index.json"), bytes);
        File.WriteAllText(Path.Combine(directory, "index.json.minisig"), signature, new UTF8Encoding(false));
    }

    private static string BundledDirectory() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", CacheDirectoryName));

    private JsonObject? Find(string id)
    {
        lock (_gate)
        {
            return (_index?.Document["items"] as JsonArray)?.OfType<JsonObject>().FirstOrDefault(item => item["id"]?.GetValue<string>() == id);
        }
    }

    private JsonObject Describe()
    {
        var state = ReadState();
        lock (_gate)
        {
            return new JsonObject
            {
                ["version"] = _index?.Version,
                ["expires"] = _index?.Expires.ToString("O"),
                ["source"] = _source,
                ["items"] = _index?.Document["items"]?.DeepClone() ?? new JsonArray(),
                ["news"] = _index?.Document["news"]?.DeepClone() ?? new JsonArray(),
                ["active"] = state["active"]?.DeepClone(),
                ["canUndo"] = state["history"] is JsonArray history && history.Count > 0,
            };
        }
    }

    private JsonObject Status()
    {
        var zebar = ZebarLocator.Find();
        var pack = Path.Combine(_context.Paths.PackDirectory, "zpack.json");
        string? packVersion = null;
        if (File.Exists(pack))
        {
            try
            {
                packVersion = JsonNode.Parse(File.ReadAllText(pack))?["version"]?.GetValue<string>();
            }
            catch (System.Text.Json.JsonException)
            {
            }
        }

        return new JsonObject
        {
            ["helperVersion"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3),
            ["packVersion"] = packVersion,
            ["packInstalled"] = File.Exists(pack),
            ["zebar"] = zebar,
            ["zebarRunning"] = Process.GetProcessesByName(SetupCommands.ZebarProcess).Length > 0,
            ["tasks"] = new JsonArray([.. ScheduledTasks.All.Select(task => (JsonNode)JsonValue.Create(ScheduledTasks.Exists(task)))]),
            ["sessions"] = _context.Hub.SessionCount,
        };
    }

    private JsonObject ReadState()
    {
        var path = Path.Combine(_context.Paths.DataDirectory, StateFileName);
        try
        {
            return File.Exists(path) && JsonNode.Parse(File.ReadAllText(path)) is JsonObject state ? state : [];
        }
        catch (Exception exception) when (exception is IOException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private void UpdateState(Action<JsonObject> change)
    {
        lock (_gate)
        {
            var state = ReadState();
            change(state);
            var path = Path.Combine(_context.Paths.DataDirectory, StateFileName);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, state.ToJsonString(), new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
        }
    }
}
