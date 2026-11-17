using System.Collections.Frozen;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Stellaria.Helper.Security;

namespace Stellaria.Helper.Catalog;

internal static partial class CatalogItems
{
    public static readonly FrozenSet<string> Types = new[] { "core", "preset", "pack" }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> Modes = new[] { "dark", "light" }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> Variants = new[]
    {
        "tonalspot", "vibrant", "expressive", "fidelity", "content", "fruitsalad", "rainbow", "neutral", "monochrome",
    }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> PresetOptions = new[]
    {
        "tintIcons", "smartScheme", "clockSeconds", "desktopClock", "island",
    }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> Licenses = new[]
    {
        "GPL-3.0-only", "GPL-3.0-or-later", "MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "MPL-2.0", "CC0-1.0", "CC-BY-4.0", "CC-BY-SA-4.0",
    }.ToFrozenSet(StringComparer.Ordinal);

    public static List<string> Validate(JsonObject item)
    {
        var errors = new List<string>();
        var id = Text(item, "id");
        var type = Text(item, "type");
        if (id is null || !ItemId().IsMatch(id))
        {
            errors.Add("'id' must be 2 to 48 lowercase letters, digits or dashes.");
        }

        if (type is null || !Types.Contains(type))
        {
            errors.Add("'type' must be core, preset or pack.");
        }

        if (Text(item, "version") is not { } version || !SemVer().IsMatch(version))
        {
            errors.Add("'version' must be a semantic version such as 1.2.0.");
        }

        if (Text(item, "license") is not { } license || !Licenses.Contains(license))
        {
            errors.Add($"'license' must be one of: {string.Join(", ", Licenses.Order(StringComparer.Ordinal))}.");
        }

        foreach (var field in new[] { "name", "summary" })
        {
            if (item[field] is not JsonObject localized || Text(localized, "fr") is null || Text(localized, "en") is null)
            {
                errors.Add($"'{field}' needs a French and an English text.");
            }
        }

        if (item["author"] is not JsonObject author || Text(author, "name") is null)
        {
            errors.Add("'author.name' is required.");
        }

        switch (type)
        {
            case "preset":
                ValidatePreset(item["preset"], errors);
                break;
            case "pack":
                ValidateArtifact(item["artifact"], errors, required: true);
                if (item["scopes"] is not JsonArray scopes || scopes.Any(scope => scope?.GetValue<string>() is not { } name || !Scopes.Theme.Contains(name)))
                {
                    errors.Add("'scopes' must list theme scopes only.");
                }

                break;
            case "core":
                ValidateArtifact(item["artifact"], errors, required: false);
                break;
        }

        return errors;
    }

    private static void ValidatePreset(JsonNode? node, List<string> errors)
    {
        if (node is not JsonObject preset)
        {
            errors.Add("A preset needs a 'preset' object.");
            return;
        }

        foreach (var (key, _) in preset)
        {
            if (key is not ("scheme" or "mode" or "variant" or "options"))
            {
                errors.Add($"'preset.{key}' is not a preset setting.");
            }
        }

        if (preset["scheme"] is JsonObject scheme && (Text(scheme, "name") is not { } name || !Slug().IsMatch(name) || Text(scheme, "flavour") is not { } flavour || !Slug().IsMatch(flavour)))
        {
            errors.Add("'preset.scheme' needs a 'name' and a 'flavour'.");
        }

        if (Text(preset, "mode") is { } mode && !Modes.Contains(mode))
        {
            errors.Add("'preset.mode' must be dark, light or auto.");
        }

        if (Text(preset, "variant") is { } variant && !Variants.Contains(variant))
        {
            errors.Add("'preset.variant' is not a Material You variant.");
        }

        if (preset["options"] is JsonObject options)
        {
            foreach (var (key, value) in options)
            {
                if (!PresetOptions.Contains(key) || value is not JsonValue flag || !flag.TryGetValue<bool>(out _))
                {
                    errors.Add($"'preset.options.{key}' is not a visual on/off option.");
                }
            }
        }
    }

    private static void ValidateArtifact(JsonNode? node, List<string> errors, bool required)
    {
        if (node is null)
        {
            if (required)
            {
                errors.Add("'artifact' is required.");
            }

            return;
        }

        if (node is not JsonObject artifact || Text(artifact, "url") is not { } url || !url.StartsWith("https://github.com/", StringComparison.Ordinal) ||
            Text(artifact, "sha256") is not { } sha || !Sha256().IsMatch(sha))
        {
            errors.Add("'artifact' needs an https://github.com/ 'url' and a lowercase hexadecimal 'sha256'.");
        }
    }

    private static string? Text(JsonObject node, string name) =>
        node[name] is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > 0 ? text : null;

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,47}$")]
    private static partial Regex ItemId();

    [GeneratedRegex("^[a-z0-9]{1,32}$")]
    private static partial Regex Slug();

    [GeneratedRegex(@"^\d{1,4}\.\d{1,4}\.\d{1,6}$")]
    private static partial Regex SemVer();

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha256();
}
