using System.Security.Cryptography;
using System.Text;

namespace Caelestia.Helper.Server;

internal sealed record HandshakeRequest(string Method, string Target, IReadOnlyDictionary<string, string> Headers);

internal abstract record HandshakeDecision;

internal sealed record HandshakeAccepted(string Widget, string Monitor, string AcceptKey) : HandshakeDecision;

internal sealed record HandshakeRejected(int StatusCode, string Reason) : HandshakeDecision;

internal static class WebSocketHandshake
{
    public const int MaxHeaderBytes = 8192;

    private const string Rfc6455Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    public static HandshakeRequest? Parse(string headerText)
    {
        var lines = headerText.Split("\r\n");
        var requestLine = lines[0].Split(' ');
        if (requestLine.Length != 3 || !requestLine[2].StartsWith("HTTP/1.1", StringComparison.Ordinal))
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Skip(1))
        {
            if (line.Length == 0)
            {
                break;
            }

            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                return null;
            }

            headers[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        return new HandshakeRequest(requestLine[0], requestLine[1], headers);
    }

    public static HandshakeDecision Decide(HandshakeRequest request, string expectedToken)
    {
        if (request.Method != "GET")
        {
            return new HandshakeRejected(405, "Only GET can upgrade to a WebSocket.");
        }

        if (!HeaderContains(request, "Upgrade", "websocket") || !HeaderContains(request, "Connection", "upgrade"))
        {
            return new HandshakeRejected(400, "Missing WebSocket upgrade headers.");
        }

        if (!request.Headers.TryGetValue("Sec-WebSocket-Version", out var version) || version != "13")
        {
            return new HandshakeRejected(400, "Only WebSocket version 13 is supported.");
        }

        if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out var key) || key.Length == 0)
        {
            return new HandshakeRejected(400, "Missing Sec-WebSocket-Key.");
        }

        if (!request.Headers.TryGetValue("Origin", out var origin) || !IsAllowedOrigin(origin))
        {
            return new HandshakeRejected(403, "Origin is not a local Zebar widget.");
        }

        var query = ParseQuery(request.Target);
        if (!query.TryGetValue("token", out var token) || !TokensMatch(token, expectedToken))
        {
            return new HandshakeRejected(403, "Invalid token.");
        }

        query.TryGetValue("widget", out var widget);
        query.TryGetValue("monitor", out var monitor);
        return new HandshakeAccepted(widget ?? "unknown", monitor ?? "", ComputeAcceptKey(key));
    }

    public static string ComputeAcceptKey(string key) =>
        Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + Rfc6455Guid)));

    public static bool IsAllowedOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        return uri.Host is "127.0.0.1" or "localhost" && uri.AbsolutePath == "/";
    }

    public static Dictionary<string, string> ParseQuery(string target)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var start = target.IndexOf('?', StringComparison.Ordinal);
        if (start < 0)
        {
            return result;
        }

        foreach (var pair in target[(start + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            var name = separator < 0 ? pair : pair[..separator];
            var value = separator < 0 ? "" : pair[(separator + 1)..];
            result[Uri.UnescapeDataString(name)] = Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        return result;
    }

    public static string AcceptResponse(string acceptKey) =>
        "HTTP/1.1 101 Switching Protocols\r\n" +
        "Upgrade: websocket\r\n" +
        "Connection: Upgrade\r\n" +
        $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";

    public static string RejectResponse(HandshakeRejected rejection)
    {
        var reason = rejection.StatusCode switch
        {
            403 => "Forbidden",
            405 => "Method Not Allowed",
            _ => "Bad Request",
        };
        var body = Encoding.UTF8.GetByteCount(rejection.Reason);
        return $"HTTP/1.1 {rejection.StatusCode} {reason}\r\n" +
               "Content-Type: text/plain; charset=utf-8\r\n" +
               $"Content-Length: {body}\r\n" +
               "Connection: close\r\n\r\n" +
               rejection.Reason;
    }

    private static bool HeaderContains(HandshakeRequest request, string name, string token) =>
        request.Headers.TryGetValue(name, out var value) &&
        value.Split(',').Any(part => part.Trim().Equals(token, StringComparison.OrdinalIgnoreCase));

    private static bool TokensMatch(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
}
