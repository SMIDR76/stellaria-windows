using System.Collections.Frozen;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Server;

namespace Caelestia.Helper.Security;

internal sealed record Grant(string Pack, FrozenSet<string> Scopes)
{
    public bool Allows(string? scope) => scope is not null && (scope == Security.Scopes.Basic || Scopes.Contains(scope));
}

internal sealed record PackEntry(string Id, string Directory, IReadOnlyList<string> Scopes);

internal sealed partial class AccessRegistry
{
    public const string OfficialPack = "caelestia";
    public const string LauncherPack = "launcher";
    public const string PacksFileName = "packs.json";
    public const string LauncherFileName = "launcher.json";
    public const int MaxPacks = 64;

    private readonly Lock _gate = new();
    private readonly Dictionary<string, Grant> _byToken = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Token, string Directory)> _community = new(StringComparer.Ordinal);

    public AccessRegistry()
    {
        OfficialToken = RuntimeFile.NewToken();
        LauncherToken = RuntimeFile.NewToken();
        _byToken[OfficialToken] = new Grant(OfficialPack, Security.Scopes.Theme);
        _byToken[LauncherToken] = new Grant(LauncherPack, Security.Scopes.Everything);
    }

    public string OfficialToken { get; }

    public string LauncherToken { get; }

    public IReadOnlyDictionary<string, string> CommunityRuntimeFiles
    {
        get
        {
            lock (_gate)
            {
                return _community.ToDictionary(entry => entry.Key, entry => Path.Combine(entry.Value.Directory, "runtime.json"));
            }
        }
    }

    public Grant? Resolve(string token)
    {
        lock (_gate)
        {
            foreach (var (candidate, grant) in _byToken)
            {
                if (WebSocketHandshake.TokensMatch(token, candidate))
                {
                    return grant;
                }
            }

            return null;
        }
    }

    public static IReadOnlyList<PackEntry> ParsePacks(string? text, string zebarRoot)
    {
        if (string.IsNullOrWhiteSpace(text) || JsonNode.Parse(text) is not JsonArray entries)
        {
            return [];
        }

        var root = Path.GetFullPath(zebarRoot).TrimEnd('\\') + "\\";
        var packs = new List<PackEntry>();
        foreach (var entry in entries.OfType<JsonObject>().Take(MaxPacks))
        {
            var id = entry["id"]?.GetValue<string>();
            var directory = entry["directory"]?.GetValue<string>();
            if (id is null || directory is null || !PackId().IsMatch(id) || id is OfficialPack or LauncherPack)
            {
                continue;
            }

            var full = Path.GetFullPath(directory);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || full.Length <= root.Length)
            {
                continue;
            }

            var scopes = (entry["scopes"] as JsonArray ?? [])
                .Select(scope => scope?.GetValue<string>())
                .Where(scope => scope is not null && Security.Scopes.Theme.Contains(scope))
                .Select(scope => scope!)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            packs.Add(new PackEntry(id, full, scopes));
        }

        return packs;
    }

    public void LoadCommunity(IEnumerable<PackEntry> packs)
    {
        lock (_gate)
        {
            foreach (var (_, (token, _)) in _community)
            {
                _byToken.Remove(token);
            }

            _community.Clear();
            foreach (var pack in packs)
            {
                var token = RuntimeFile.NewToken();
                _community[pack.Id] = (token, pack.Directory);
                _byToken[token] = new Grant(pack.Id, pack.Scopes.Append(Security.Scopes.Basic).ToFrozenSet(StringComparer.Ordinal));
            }
        }
    }

    public string? TokenOf(string pack)
    {
        lock (_gate)
        {
            return _community.TryGetValue(pack, out var entry) ? entry.Token : null;
        }
    }

    public void WriteRuntimeFiles(AppPaths paths, int port, int processId, Log log)
    {
        RuntimeFile.Write(paths.RuntimeFile, port, OfficialToken, processId);
        RuntimeFile.Write(Path.Combine(paths.DataDirectory, LauncherFileName), port, LauncherToken, processId);
        foreach (var (pack, file) in CommunityRuntimeFiles)
        {
            try
            {
                RuntimeFile.Write(file, port, TokenOf(pack)!, processId);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                log.Warning("access", $"The runtime file of pack {pack} could not be written.", exception);
            }
        }
    }

    public void DeleteRuntimeFiles(AppPaths paths, int processId)
    {
        RuntimeFile.DeleteIfOwned(paths.RuntimeFile, processId);
        RuntimeFile.DeleteIfOwned(Path.Combine(paths.DataDirectory, LauncherFileName), processId);
        foreach (var file in CommunityRuntimeFiles.Values)
        {
            RuntimeFile.DeleteIfOwned(file, processId);
        }
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,47}$")]
    private static partial Regex PackId();
}
