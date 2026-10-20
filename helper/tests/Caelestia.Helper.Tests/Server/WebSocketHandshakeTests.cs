using Caelestia.Helper.Server;

namespace Caelestia.Helper.Tests.Server;

public sealed class WebSocketHandshakeTests
{
    private const string Token = "secret-token";

    private static string Request(string target, string origin = "http://127.0.0.1:6124") =>
        $"GET {target} HTTP/1.1\r\n" +
        "Host: 127.0.0.1:47610\r\n" +
        "Upgrade: websocket\r\n" +
        "Connection: keep-alive, Upgrade\r\n" +
        "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
        "Sec-WebSocket-Version: 13\r\n" +
        $"Origin: {origin}\r\n\r\n";

    [Fact]
    public void Accept_key_matches_the_rfc_6455_example()
    {
        Assert.Equal("s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", WebSocketHandshake.ComputeAcceptKey("dGhlIHNhbXBsZSBub25jZQ=="));
    }

    [Fact]
    public void A_valid_widget_request_is_accepted_with_its_identity()
    {
        var request = WebSocketHandshake.Parse(Request($"/?token={Token}&widget=bar&monitor=%5C%5C.%5CDISPLAY1"));

        var decision = Assert.IsType<HandshakeAccepted>(WebSocketHandshake.Decide(request!, Token));

        Assert.Equal("bar", decision.Widget);
        Assert.Equal(@"\\.\DISPLAY1", decision.Monitor);
        Assert.Equal("s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", decision.AcceptKey);
    }

    [Fact]
    public void A_wrong_token_is_forbidden()
    {
        var request = WebSocketHandshake.Parse(Request("/?token=guess&widget=bar"));

        var decision = Assert.IsType<HandshakeRejected>(WebSocketHandshake.Decide(request!, Token));

        Assert.Equal(403, decision.StatusCode);
    }

    [Theory]
    [InlineData("http://127.0.0.1:6124", true)]
    [InlineData("http://localhost:6124", true)]
    [InlineData("https://127.0.0.1:6124", false)]
    [InlineData("http://example.com", false)]
    [InlineData("http://127.0.0.1.example.com", false)]
    [InlineData("null", false)]
    public void Only_local_http_origins_are_allowed(string origin, bool allowed)
    {
        Assert.Equal(allowed, WebSocketHandshake.IsAllowedOrigin(origin));
    }

    [Fact]
    public void A_foreign_origin_is_forbidden_even_with_the_token()
    {
        var request = WebSocketHandshake.Parse(Request($"/?token={Token}", "http://evil.example"));

        var decision = Assert.IsType<HandshakeRejected>(WebSocketHandshake.Decide(request!, Token));

        Assert.Equal(403, decision.StatusCode);
    }

    [Fact]
    public void A_request_without_upgrade_headers_is_a_bad_request()
    {
        var request = WebSocketHandshake.Parse($"GET /?token={Token} HTTP/1.1\r\nHost: 127.0.0.1\r\nOrigin: http://127.0.0.1:6124\r\n\r\n");

        var decision = Assert.IsType<HandshakeRejected>(WebSocketHandshake.Decide(request!, Token));

        Assert.Equal(400, decision.StatusCode);
    }

    [Fact]
    public void A_malformed_request_line_is_not_parsed()
    {
        Assert.Null(WebSocketHandshake.Parse("NONSENSE\r\n\r\n"));
    }

    [Fact]
    public void Query_values_are_url_decoded()
    {
        var query = WebSocketHandshake.ParseQuery("/?a=1&name=hello%20world&flag");

        Assert.Equal("1", query["a"]);
        Assert.Equal("hello world", query["name"]);
        Assert.Equal("", query["flag"]);
    }
}
