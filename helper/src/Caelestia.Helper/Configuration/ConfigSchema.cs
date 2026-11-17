using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Caelestia.Helper.Configuration;

internal delegate string? ConfigValidator(JsonNode? value);

internal sealed record ConfigOption(string Path, Func<JsonNode?> Default, ConfigValidator Validate);

internal static partial class ConfigSchema
{
    public const int Version = 1;
    public const int MaxListLength = 200;
    public const int MaxIdentifierLength = 512;

    public static readonly string[] Languages = ["auto", "en", "fr", "es", "tr", "ar", "kmr", "kiu"];
    public static readonly string[] Modes = ["dark", "light", "auto"];
    public static readonly string[] Variants = ["tonalspot", "vibrant", "expressive", "fidelity", "content", "fruitsalad", "rainbow", "neutral", "monochrome"];

    public static IReadOnlyList<ConfigOption> Options { get; } =
    [
        Choice("language", "auto", Languages),
        Choice("theme.mode", "dark", Modes),
        Choice("theme.variant", "tonalspot", Variants),
        new("theme.scheme", () => null, ValidateScheme),
        Flag("theme.smartScheme", true),
        Flag("theme.transparency", false),
        Flag("performance.gameMode", false),
        Flag("taskbar.hide", true),
        Flag("clock.seconds", false),
        Flag("desktopClock.enabled", true),
        Flag("idle.enabled", true),
        Integer("idle.minutes", 10, 1, 60),
        Flag("panels.hoverDashboard", true),
        Flag("panels.hoverOsd", true),
        Flag("panels.hoverUtilities", true),
        Flag("panels.hoverPopouts", true),
        Flag("osd.onVolume", true),
        Flag("notifications.theme", true),
        Flag("notifications.system", true),
        Flag("notifications.history", true),
        Flag("media.lyrics", true),
        Flag("weather.enabled", true),
        new("weather.location", () => null, ValidateLocation),
        Flag("launcher.winKey", true),
        IdentifierList("launcher.favourites"),
        IdentifierList("launcher.hidden"),
        new("wallpapers.directory", () => null, ValidateDirectory),
        new("screenshots.directory", () => null, ValidateDirectory),
        new("catalog.url", () => null, value =>
            value is null || (value is JsonValue json && json.TryGetValue<string>(out var url) && url.StartsWith("https://", StringComparison.Ordinal) && url.EndsWith("/index.json", StringComparison.Ordinal))
                ? null
                : "must be an https:// address ending with /index.json"),
    ];

    public static ConfigOption? Find(string path) => Options.FirstOrDefault(option => option.Path == path);

    private static ConfigOption Flag(string path, bool fallback) =>
        new(path, () => JsonValue.Create(fallback), value => value is JsonValue json && json.TryGetValue<bool>(out _) ? null : "must be true or false");

    private static ConfigOption Integer(string path, int fallback, int minimum, int maximum) =>
        new(path, () => JsonValue.Create(fallback), value =>
            value is JsonValue json && json.TryGetValue<int>(out var number) && number >= minimum && number <= maximum
                ? null
                : $"must be an integer between {minimum} and {maximum}");

    private static ConfigOption Choice(string path, string fallback, string[] allowed) =>
        new(path, () => JsonValue.Create(fallback), value =>
            value is JsonValue json && json.TryGetValue<string>(out var text) && allowed.Contains(text, StringComparer.Ordinal)
                ? null
                : $"must be one of {string.Join(", ", allowed)}");

    private static ConfigOption IdentifierList(string path) =>
        new(path, () => new JsonArray(), value =>
        {
            if (value is not JsonArray list || list.Count > MaxListLength)
            {
                return $"must be a list of at most {MaxListLength} application identifiers";
            }

            return list.All(item => item is JsonValue json && json.TryGetValue<string>(out var id) && IsApplicationIdentifier(id))
                ? null
                : "contains an invalid application identifier";
        });

    public static bool IsApplicationIdentifier(string id) => id.Length is > 0 and <= MaxIdentifierLength && ApplicationIdentifier().IsMatch(id);

    private static string? ValidateScheme(JsonNode? value)
    {
        if (value is null)
        {
            return null;
        }

        return value is JsonObject scheme && scheme.Count == 2 &&
               scheme["name"] is JsonValue name && name.TryGetValue<string>(out var nameText) && SchemeName().IsMatch(nameText) &&
               scheme["flavour"] is JsonValue flavour && flavour.TryGetValue<string>(out var flavourText) && SchemeName().IsMatch(flavourText)
            ? null
            : "must be null or { name, flavour } with short lowercase names";
    }

    private static string? ValidateLocation(JsonNode? value)
    {
        if (value is null)
        {
            return null;
        }

        return value is JsonObject location && location.Count == 3 &&
               location["name"] is JsonValue name && name.TryGetValue<string>(out var text) && text.Length is > 0 and <= 64 &&
               location["latitude"] is JsonValue latitude && latitude.TryGetValue<double>(out var lat) && lat is >= -90 and <= 90 &&
               location["longitude"] is JsonValue longitude && longitude.TryGetValue<double>(out var lon) && lon is >= -180 and <= 180
            ? null
            : "must be null or { name, latitude, longitude } with valid coordinates";
    }

    private static string? ValidateDirectory(JsonNode? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is not JsonValue json || !json.TryGetValue<string>(out var path) || path.Length > 1024 || !System.IO.Path.IsPathFullyQualified(path))
        {
            return "must be null or an absolute folder path";
        }

        return Directory.Exists(path) ? null : "folder does not exist";
    }

    [GeneratedRegex("^[^\\x00-\\x1F\"<>|*?]+$")]
    private static partial Regex ApplicationIdentifier();

    [GeneratedRegex(@"^[a-z0-9-]{1,32}$")]
    private static partial Regex SchemeName();
}
