using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Server;

internal static class RuntimeFile
{
    public static string NewToken() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    public static string Render(int port, string token, int processId) => new JsonObject
    {
        ["protocolVersion"] = Messages.ProtocolVersion,
        ["port"] = port,
        ["token"] = token,
        ["pid"] = processId,
    }.ToJsonString();

    public static void Write(string path, int port, string token, int processId)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException("Runtime file needs a directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporary = $"{path}.{processId}.tmp";
        File.WriteAllText(temporary, Render(port, token, processId), new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    public static void DeleteIfOwned(string path, int processId)
    {
        try
        {
            if (File.Exists(path) && JsonNode.Parse(File.ReadAllText(path))?["pid"]?.GetValue<int>() == processId)
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
        }
    }
}
