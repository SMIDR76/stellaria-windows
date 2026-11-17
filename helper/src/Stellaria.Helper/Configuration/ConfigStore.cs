using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Stellaria.Helper.Hosting;
using Stellaria.Helper.Protocol;

namespace Stellaria.Helper.Configuration;

internal sealed class ConfigStore(string path, Log log)
{
    private readonly Lock _gate = new();
    private JsonObject _values = [];

    public event Action<JsonObject>? Changed;

    public void Load()
    {
        JsonObject? file = null;
        try
        {
            if (File.Exists(path))
            {
                file = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            log.Warning("config", "The configuration file could not be read, defaults are used.", exception);
        }

        var values = new JsonObject();
        foreach (var option in ConfigSchema.Options)
        {
            if (file is null || !file.TryGetPropertyValue(option.Path, out var stored))
            {
                values[option.Path] = option.Default();
            }
            else if (option.Validate(stored) is { } problem)
            {
                log.Warning("config", $"Ignoring '{option.Path}': {problem}.");
                values[option.Path] = option.Default();
            }
            else
            {
                values[option.Path] = stored?.DeepClone();
            }
        }

        lock (_gate)
        {
            _values = values;
        }
    }

    public JsonObject Snapshot()
    {
        lock (_gate)
        {
            var snapshot = (JsonObject)_values.DeepClone();
            snapshot["version"] = ConfigSchema.Version;
            return snapshot;
        }
    }

    public JsonNode? Get(string optionPath)
    {
        lock (_gate)
        {
            return _values[optionPath]?.DeepClone();
        }
    }

    public bool GetFlag(string optionPath) => Get(optionPath)?.GetValue<bool>() ?? false;

    public string? GetString(string optionPath) => Get(optionPath) is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public JsonObject Set(string optionPath, JsonNode? value)
    {
        var option = ConfigSchema.Find(optionPath) ?? throw new HelperException(ErrorCode.InvalidParams, $"Unknown setting '{optionPath}'.");
        if (option.Validate(value) is { } problem)
        {
            throw new HelperException(ErrorCode.InvalidParams, $"Setting '{optionPath}' {problem}.");
        }

        JsonObject snapshot;
        lock (_gate)
        {
            _values[optionPath] = value?.DeepClone();
            Save();
            snapshot = Snapshot();
        }

        Changed?.Invoke(snapshot);
        return snapshot;
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var document = (JsonObject)_values.DeepClone();
        document["version"] = ConfigSchema.Version;
        var temporary = $"{path}.tmp";
        File.WriteAllText(temporary, document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }
}
