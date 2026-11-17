using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Security;
using Caelestia.Helper.Server;
using Caelestia.Helper.Tests.TestSupport;

namespace Caelestia.Helper.Tests.Server;

public sealed class HelperServerTests : IAsyncLifetime
{
    private const int TestFirstPort = 47680;
    private const int TestLastPort = 47699;

    private readonly TemporaryDirectory _directory = new();
    private readonly CancellationTokenSource _shutdown = new();
    private Log _log = null!;
    private StateHub _hub = null!;
    private HelperServer _server = null!;
    private Task _running = Task.CompletedTask;

    public ValueTask InitializeAsync()
    {
        _log = new Log(_directory.Path, LogLevel.Debug);
        _hub = new StateHub();
        var router = new RequestRouter(_log);
        router.Register("echo.widget", Scopes.Basic, (context, _) => Task.FromResult<JsonNode?>(new JsonObject { ["widget"] = context.Widget }));
        router.Register("always.fails", Scopes.Basic, (_, _) => throw new HelperException(ErrorCode.NotFound, "Nothing here."));
        router.Register("secret.read", Scopes.Clipboard, (_, _) => Task.FromResult<JsonNode?>(new JsonObject { ["secret"] = true }));
        _server = new HelperServer(_hub, router, _log, "9.9.9", TimeProvider.System);
        _server.Start(TestFirstPort, TestLastPort);
        _running = _server.RunAsync(_shutdown.Token);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _shutdown.CancelAsync();
        await _running;
        await _server.DisposeAsync();
        _log.Dispose();
        _shutdown.Dispose();
        _directory.Dispose();
    }

    [Fact]
    public async Task An_authenticated_widget_receives_hello_then_the_current_state()
    {
        _hub.Publish("idle", new JsonObject { ["seconds"] = 42 });
        using var client = await ConnectAsync();
        await AuthenticateAsync(client, _server.Token);

        var hello = await ReceiveAsync(client);
        var state = await ReceiveAsync(client);

        Assert.Equal("hello", hello["kind"]!.GetValue<string>());
        Assert.Equal(Messages.ProtocolVersion, hello["protocolVersion"]!.GetValue<int>());
        Assert.Equal("idle", state["topic"]!.GetValue<string>());
        Assert.Equal(42, state["data"]!["seconds"]!.GetValue<int>());
    }

    [Fact]
    public async Task Later_state_changes_are_pushed_to_connected_widgets()
    {
        using var client = await ConnectAsync();
        await AuthenticateAsync(client, _server.Token);
        await ReceiveAsync(client);
        await WaitForSessionsAsync(1);

        _hub.Publish("power", new JsonObject { ["keepAwake"] = true });

        var state = await ReceiveAsync(client);
        Assert.Equal("power", state["topic"]!.GetValue<string>());
        Assert.True(state["data"]!["keepAwake"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Requests_are_answered_with_the_widget_identity()
    {
        using var client = await ConnectAsync();
        await AuthenticateAsync(client, _server.Token, widget: "dashboard");
        await ReceiveAsync(client);

        await SendAsync(client, """{"kind":"request","id":"a1","method":"echo.widget"}""");
        var reply = await ReceiveAsync(client);

        Assert.Equal("a1", reply["id"]!.GetValue<string>());
        Assert.True(reply["ok"]!.GetValue<bool>());
        Assert.Equal("dashboard", reply["data"]!["widget"]!.GetValue<string>());
    }

    [Fact]
    public async Task Handler_errors_and_unknown_methods_become_failure_replies()
    {
        using var client = await ConnectAsync();
        await AuthenticateAsync(client, _server.Token);
        await ReceiveAsync(client);

        await SendAsync(client, """{"kind":"request","id":"b1","method":"always.fails"}""");
        var failed = await ReceiveAsync(client);
        await SendAsync(client, """{"kind":"request","id":"b2","method":"does.not.exist"}""");
        var unknown = await ReceiveAsync(client);

        Assert.Equal("not_found", failed["error"]!["code"]!.GetValue<string>());
        Assert.Equal("unknown_method", unknown["error"]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_wrong_token_closes_the_connection_without_sending_any_state()
    {
        _hub.Publish("idle", new JsonObject { ["seconds"] = 42 });
        using var client = await ConnectAsync();

        await AuthenticateAsync(client, "wrong-token");

        Assert.Equal(WebSocketCloseStatus.PolicyViolation, await ReceiveCloseAsync(client));
    }

    [Fact]
    public async Task A_request_before_authentication_closes_the_connection()
    {
        using var client = await ConnectAsync();

        await SendAsync(client, """{"kind":"request","id":"x","method":"echo.widget"}""");

        Assert.Equal(WebSocketCloseStatus.PolicyViolation, await ReceiveCloseAsync(client));
    }

    [Fact]
    public async Task A_silent_connection_is_closed_after_the_authentication_timeout()
    {
        using var client = await ConnectAsync();

        Assert.Equal(WebSocketCloseStatus.PolicyViolation, await ReceiveCloseAsync(client, ClientSession.AuthenticationTimeout + TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public async Task A_foreign_origin_cannot_open_a_socket()
    {
        var exception = await Assert.ThrowsAnyAsync<WebSocketException>(() => ConnectAsync("http://evil.example"));

        Assert.Contains("403", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_community_pack_only_sees_what_its_scopes_allow()
    {
        var root = Path.Combine(_directory.Path, "zebar");
        _server.Access.LoadCommunity(AccessRegistry.ParsePacks($$"""[{"id":"neon","directory":"{{Path.Combine(root, "neon").Replace("\\", "\\\\")}}","scopes":["media"]}]""", root));
        _hub.Publish("idle", new JsonObject { ["seconds"] = 1 });
        _hub.Publish("notifications", new JsonObject { ["items"] = new JsonArray() });
        using var client = await ConnectAsync();
        await AuthenticateAsync(client, _server.Access.TokenOf("neon")!);

        Assert.Equal("hello", (await ReceiveAsync(client))["kind"]!.GetValue<string>());
        Assert.Equal("idle", (await ReceiveAsync(client))["topic"]!.GetValue<string>());
        await SendAsync(client, """{"kind":"request","id":"c1","method":"secret.read"}""");
        var denied = await ReceiveAsync(client);

        Assert.Equal("c1", denied["id"]!.GetValue<string>());
        Assert.Equal("denied", denied["error"]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task The_official_pack_keeps_every_theme_scope()
    {
        using var client = await ConnectAsync();
        await AuthenticateAsync(client, _server.Token);
        await ReceiveAsync(client);

        await SendAsync(client, """{"kind":"request","id":"d1","method":"secret.read"}""");
        var reply = await ReceiveAsync(client);

        Assert.True(reply["ok"]!.GetValue<bool>());
    }

    private async Task<ClientWebSocket> ConnectAsync(string origin = "http://127.0.0.1:6124")
    {
        var client = new ClientWebSocket();
        client.Options.SetRequestHeader("Origin", origin);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await client.ConnectAsync(new Uri($"ws://127.0.0.1:{_server.Port}/"), timeout.Token);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static Task AuthenticateAsync(ClientWebSocket client, string token, string widget = "bar") =>
        SendAsync(client, new JsonObject { ["kind"] = "auth", ["token"] = token, ["widget"] = widget, ["monitor"] = "" }.ToJsonString());

    private async Task WaitForSessionsAsync(int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_hub.SessionCount < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }

    private static async Task SendAsync(ClientWebSocket client, string text)
    {
        await client.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<JsonNode> ReceiveAsync(ClientWebSocket client)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var result = await client.ReceiveAsync(buffer, timeout.Token);
            var message = JsonNode.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count))!;
            if (message["kind"]!.GetValue<string>() != "ping")
            {
                return message;
            }
        }
    }

    private static async Task<WebSocketCloseStatus?> ReceiveCloseAsync(ClientWebSocket client, TimeSpan? wait = null)
    {
        using var timeout = new CancellationTokenSource(wait ?? TimeSpan.FromSeconds(5));
        var buffer = new byte[1024];
        while (true)
        {
            var result = await client.ReceiveAsync(buffer, timeout.Token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return result.CloseStatus;
            }

            Assert.Fail($"Expected the server to close, but it sent: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
        }
    }
}
