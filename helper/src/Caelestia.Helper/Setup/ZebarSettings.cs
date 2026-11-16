using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Caelestia.Helper.Setup;

internal sealed record StartupWidget(string Widget, string Preset);

internal static class ZebarSettings
{
    public const string PackName = "caelestia";
    public const string BackupSuffix = ".caelestia-backup";

    private static readonly JsonWriterOptions s_writer = new() { Indented = true };

    public static string DefaultPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".glzr", "zebar", "settings.json");

    public static IReadOnlyList<StartupWidget> ReadStartupList(string packDirectory)
    {
        var file = Path.Combine(packDirectory, "startup.json");
        if (JsonNode.Parse(File.ReadAllText(file)) is not JsonArray entries)
        {
            throw new InvalidDataException($"{file} must be a JSON array.");
        }

        return entries
            .OfType<JsonObject>()
            .Select(entry => new StartupWidget(
                entry["widget"]?.GetValue<string>() ?? throw new InvalidDataException("A startup entry has no widget."),
                entry["preset"]?.GetValue<string>() ?? "default"))
            .ToList();
    }

    public static string Merge(string? existing, IEnumerable<StartupWidget> widgets)
    {
        var root = Parse(existing);
        var startup = new JsonArray();
        foreach (var entry in Others(root))
        {
            startup.Add(entry);
        }

        foreach (var widget in widgets)
        {
            startup.Add((JsonNode)new JsonObject { ["pack"] = PackName, ["widget"] = widget.Widget, ["preset"] = widget.Preset });
        }

        root["startupConfigs"] = startup;
        return Serialize(root);
    }

    public static string Unmerge(string? existing)
    {
        var root = Parse(existing);
        var startup = new JsonArray();
        foreach (var entry in Others(root))
        {
            startup.Add(entry);
        }

        root["startupConfigs"] = startup;
        return Serialize(root);
    }

    public static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path))
        {
            File.Copy(path, path + BackupSuffix, overwrite: true);
        }

        var temporary = $"{path}.tmp";
        File.WriteAllText(temporary, text, new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    public static string? ReadText(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    private static JsonObject Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return JsonNode.Parse(text.TrimStart('﻿')) as JsonObject ?? throw new InvalidDataException("Zebar's settings.json is not a JSON object.");
    }

    private static List<JsonNode> Others(JsonObject root)
    {
        if (root["startupConfigs"] is not JsonArray current)
        {
            return [];
        }

        return current
            .Where(entry => entry is not JsonObject item || item["pack"]?.GetValue<string>() != PackName)
            .Select(entry => entry!.DeepClone())
            .ToList();
    }

    private static string Serialize(JsonObject root)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, s_writer))
        {
            root.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }
}
