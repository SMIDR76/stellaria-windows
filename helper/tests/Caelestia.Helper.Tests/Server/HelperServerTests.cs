using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Protocol;
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

    public Task InitializeAsync()
    {
        _log = new Log(_directory.Path, LogLevel.Debug);
        _hub = new StateHub();
        var router = new RequestRouter(_log);
        router.Register("echo.widget", (context, _) => Task.FromResult<JsonNode?>(new JsonObject { ["widget"] = context.Widget }));
        router.Register("always.fails", (_, _) => throw new HelperException(ErrorCode.NotFound, "Nothing here."));
        _server = new HelperServer(_hub, router, _log, "9.9.9");
        _server.Start(TestFirstPort, TestLastPort);
        _running = _server.RunAsync(_shutdown.Token);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _shutdown.CancelAsync();
        await _running;
        await _server.DisposeAsync();
        _log.Dispose();
        _shutdown.Dispose();
        _directory.Dispose();
    }

    [Fact]
    public async Task A_widget_receives_hello_then_the_current_state()
    {
        _hub.Publish("idle", new JsonObject { ["seconds"] = 42 });
        using var client = await ConnectAsync(_server.Token);

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
        using var client = await ConnectAsync(_server.Token);
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
        using var client = await ConnectAsync(_server.Token, widget: "dashboard");
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
        using var client = await ConnectAsync(_server.Token);
        await ReceiveAsync(client);

        await SendAsync(client, """{"kind":"request","id":"b1","method":"always.fails"}""");
        var failed = await ReceiveAsync(client);
        await SendAsync(client, """{"kind":"request","id":"b2","method":"does.not.exist"}""");
        var unknown = await ReceiveAsync(client);

        Assert.Equal("not_found", failed["error"]!["code"]!.GetValue<string>());
        Assert.Equal("unknown_method", unknown["error"]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_connection_with_a_wrong_token_is_refused()
    {
        var exception = await Assert.ThrowsAnyAsync<WebSocketException>(() => ConnectAsync("wrong-token"));

        Assert.Contains("403", exception.Message, StringComparison.Ordinal);
    }

    private async Task<ClientWebSocket> ConnectAsync(string token, string widget = "bar")
    {
        var client = new ClientWebSocket();
        client.Options.SetRequestHeader("Origin", "http://127.0.0.1:6124");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await client.ConnectAsync(new Uri($"ws://127.0.0.1:{_server.Port}/?token={token}&widget={widget}"), timeout.Token);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

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
}
