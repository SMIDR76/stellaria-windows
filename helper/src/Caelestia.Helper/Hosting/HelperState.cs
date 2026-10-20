using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Caelestia.Helper.Hosting;

internal sealed class HelperState(string path)
{
    private readonly Lock _gate = new();

    public bool? OriginalTaskbarAutoHide
    {
        get => Read()?["originalTaskbarAutoHide"] is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : null;
        set => Update(state => state["originalTaskbarAutoHide"] = value is { } flag ? JsonValue.Create(flag) : null);
    }

    private JsonObject? Read()
    {
        lock (_gate)
        {
            try
            {
                return File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject : null;
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }

    private void Update(Action<JsonObject> change)
    {
        lock (_gate)
        {
            var state = Read() ?? [];
            change(state);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = $"{path}.tmp";
            File.WriteAllText(temporary, state.ToJsonString(), new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
        }
    }
}
