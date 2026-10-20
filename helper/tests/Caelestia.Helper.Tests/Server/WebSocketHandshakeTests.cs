using Caelestia.Helper.Server;

namespace Caelestia.Helper.Tests.Server;

public sealed class WebSocketHandshakeTests
{
    private const int Port = 47610;

    private static string Request(string origin = "http://127.0.0.1:6124", string host = "127.0.0.1:47610") =>
        "GET / HTTP/1.1\r\n" +
        $"Host: {host}\r\n" +
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
    public void A_local_widget_request_is_accepted()
    {
        var decision = Assert.IsType<HandshakeAccepted>(WebSocketHandshake.Decide(WebSocketHandshake.Parse(Request())!, Port));

        Assert.Equal("s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", decision.AcceptKey);
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
    public void A_foreign_origin_is_forbidden()
    {
        var decision = Assert.IsType<HandshakeRejected>(WebSocketHandshake.Decide(WebSocketHandshake.Parse(Request(origin: "http://evil.example"))!, Port));

        Assert.Equal(403, decision.StatusCode);
    }

    [Theory]
    [InlineData("127.0.0.1:47610", true)]
    [InlineData("localhost:47610", true)]
    [InlineData("127.0.0.1:47611", false)]
    [InlineData("evil.example:47610", false)]
    [InlineData("127.0.0.1", false)]
    public void Only_the_helper_address_is_a_valid_host(string host, bool allowed)
    {
        Assert.Equal(allowed, WebSocketHandshake.IsAllowedHost(host, Port));
    }

    [Fact]
    public void A_rebound_domain_is_forbidden_even_from_a_local_origin()
    {
        var decision = Assert.IsType<HandshakeRejected>(WebSocketHandshake.Decide(WebSocketHandshake.Parse(Request(host: "attacker.example:47610"))!, Port));

        Assert.Equal(403, decision.StatusCode);
    }

    [Fact]
    public void A_request_without_upgrade_headers_is_a_bad_request()
    {
        var request = WebSocketHandshake.Parse("GET / HTTP/1.1\r\nHost: 127.0.0.1:47610\r\nOrigin: http://127.0.0.1:6124\r\n\r\n");

        var decision = Assert.IsType<HandshakeRejected>(WebSocketHandshake.Decide(request!, Port));

        Assert.Equal(400, decision.StatusCode);
    }

    [Fact]
    public void A_malformed_request_line_is_not_parsed()
    {
        Assert.Null(WebSocketHandshake.Parse("NONSENSE\r\n\r\n"));
    }

    [Fact]
    public void Tokens_are_compared_exactly()
    {
        Assert.True(WebSocketHandshake.TokensMatch("abc", "abc"));
        Assert.False(WebSocketHandshake.TokensMatch("abd", "abc"));
        Assert.False(WebSocketHandshake.TokensMatch("abcd", "abc"));
    }
}
