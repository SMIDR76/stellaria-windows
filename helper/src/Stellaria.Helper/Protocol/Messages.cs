using System.Text.Json;
using System.Text.Json.Nodes;

namespace Stellaria.Helper.Protocol;

internal static class Messages
{
    public const int ProtocolVersion = 2;

    public static string Hello(string helperVersion) => Serialize(new JsonObject
    {
        ["kind"] = "hello",
        ["protocolVersion"] = ProtocolVersion,
        ["helperVersion"] = helperVersion,
    });

    public static string State(string topic, JsonNode? data) => Serialize(new JsonObject
    {
        ["kind"] = "state",
        ["topic"] = topic,
        ["data"] = data?.DeepClone(),
    });

    public static string Event(string name, JsonNode? data) => Serialize(new JsonObject
    {
        ["kind"] = "event",
        ["name"] = name,
        ["data"] = data?.DeepClone(),
    });

    public static string ReplySuccess(string id, JsonNode? data) => Serialize(new JsonObject
    {
        ["kind"] = "reply",
        ["id"] = id,
        ["ok"] = true,
        ["data"] = data?.DeepClone(),
    });

    public static string ReplyFailure(string id, string code, string message) => Serialize(new JsonObject
    {
        ["kind"] = "reply",
        ["id"] = id,
        ["ok"] = false,
        ["error"] = new JsonObject { ["code"] = code, ["message"] = message },
    });

    public static string Ping(DateTimeOffset time) => Serialize(new JsonObject
    {
        ["kind"] = "ping",
        ["time"] = time.ToUnixTimeMilliseconds(),
    });

    private static string Serialize(JsonObject message) => message.ToJsonString();
}

internal abstract record ClientMessage
{
    public static ClientMessage Parse(string text)
    {
        JsonObject? root;
        try
        {
            root = JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            return new InvalidMessage(null, "Message is not valid JSON.");
        }

        if (root is null)
        {
            return new InvalidMessage(null, "Message must be a JSON object.");
        }

        var kind = ReadString(root, "kind");
        switch (kind)
        {
            case "auth":
                var token = ReadString(root, "token");
                return string.IsNullOrEmpty(token)
                    ? new InvalidMessage(null, "Authentication needs a token.")
                    : new AuthMessage(token, ReadString(root, "widget") ?? "unknown", ReadString(root, "monitor") ?? "");
            case "heartbeat":
                return new HeartbeatMessage(root["visible"] is JsonValue visible && visible.TryGetValue<bool>(out var value) && value);
            case "request":
                var id = ReadString(root, "id");
                var method = ReadString(root, "method");
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(method))
                {
                    return new InvalidMessage(id, "A request needs a non empty id and method.");
                }

                if (root["params"] is not null and not JsonObject)
                {
                    return new InvalidMessage(id, "Request params must be an object.");
                }

                return new RequestMessage(id, method, root["params"] as JsonObject ?? []);
            default:
                return new InvalidMessage(null, $"Unknown message kind '{kind}'.");
        }
    }

    private static string? ReadString(JsonObject root, string name) =>
        root[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}

internal sealed record AuthMessage(string Token, string Widget, string Monitor) : ClientMessage;

internal sealed record RequestMessage(string Id, string Method, JsonObject Params) : ClientMessage;

internal sealed record HeartbeatMessage(bool Visible) : ClientMessage;

internal sealed record InvalidMessage(string? Id, string Reason) : ClientMessage;
