using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Server;

internal sealed class ClientSession : IAsyncDisposable
{
    public const int OutboundCapacity = 512;
    public const int MaxInboundBytes = 64 * 1024;
    public const int RequestLimit = 200;
    public static readonly TimeSpan RequestWindow = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan AuthenticationTimeout = TimeSpan.FromSeconds(5);

    private readonly WebSocket _socket;
    private readonly string _token;
    private readonly RequestRouter _router;
    private readonly Log _log;
    private readonly Action<ClientSession> _authenticated;
    private readonly Channel<string> _outbound;
    private readonly CancellationTokenSource _lifetime;
    private readonly RateLimiter _rate;

    public ClientSession(
        WebSocket socket,
        string token,
        RequestRouter router,
        Log log,
        Action<ClientSession> authenticated,
        TimeProvider time,
        CancellationToken shutdown)
    {
        _socket = socket;
        _token = token;
        _router = router;
        _log = log;
        _authenticated = authenticated;
        _rate = new RateLimiter(RequestLimit, RequestWindow, time);
        _outbound = Channel.CreateBounded<string>(new BoundedChannelOptions(OutboundCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(shutdown);
    }

    public string Widget { get; private set; } = "unauthenticated";

    public string Monitor { get; private set; } = "";

    public bool IsAuthenticated { get; private set; }

    public DateTimeOffset LastHeartbeat { get; private set; } = DateTimeOffset.UtcNow;

    public bool ReportsVisible { get; private set; }

    public void Enqueue(string message)
    {
        if (!_outbound.Writer.TryWrite(message))
        {
            _log.Warning("session", $"{Describe()} is not reading fast enough, closing it so it reconnects with a fresh snapshot.");
            _lifetime.Cancel();
        }
    }

    public async Task RunAsync()
    {
        var token = _lifetime.Token;
        var sending = SendLoopAsync(token);
        try
        {
            if (await AuthenticateAsync(token).ConfigureAwait(false))
            {
                await ReceiveLoopAsync(token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (WebSocketException exception)
        {
            _log.Debug("session", $"{Describe()} disconnected: {exception.Message}");
        }
        finally
        {
            _outbound.Writer.TryComplete();
            await _lifetime.CancelAsync().ConfigureAwait(false);
            await sending.ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifetime.CancelAsync().ConfigureAwait(false);
        await CloseAsync(WebSocketCloseStatus.NormalClosure, "closing").ConfigureAwait(false);
        _socket.Dispose();
        _lifetime.Dispose();
    }

    public string Describe() => string.IsNullOrEmpty(Monitor) ? Widget : $"{Widget}@{Monitor}";

    private async Task<bool> AuthenticateAsync(CancellationToken token)
    {
        var receiving = ReceiveTextAsync(token);
        if (await Task.WhenAny(receiving, Task.Delay(AuthenticationTimeout, token)).ConfigureAwait(false) != receiving)
        {
            _log.Warning("session", "A connection did not authenticate in time.");
            await CloseAsync(WebSocketCloseStatus.PolicyViolation, "authentication timeout").ConfigureAwait(false);
            await DrainAsync(receiving).ConfigureAwait(false);
            return false;
        }

        var text = await receiving.ConfigureAwait(false);
        if (text is null)
        {
            return false;
        }

        if (ClientMessage.Parse(text) is not AuthMessage auth || !WebSocketHandshake.TokensMatch(auth.Token, _token))
        {
            _log.Warning("session", "A connection sent a wrong or missing authentication token.");
            await CloseAsync(WebSocketCloseStatus.PolicyViolation, "authentication failed").ConfigureAwait(false);
            return false;
        }

        Widget = auth.Widget;
        Monitor = auth.Monitor;
        IsAuthenticated = true;
        _authenticated(this);
        return true;
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        while (await ReceiveTextAsync(token).ConfigureAwait(false) is { } text)
        {
            Handle(text, token);
        }
    }

    private async Task<string?> ReceiveTextAsync(CancellationToken token)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();
        while (_socket.State == WebSocketState.Open)
        {
            var result = await _socket.ReceiveAsync(buffer, token).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(buffer, 0, result.Count);
            if (message.Length > MaxInboundBytes)
            {
                _log.Warning("session", $"{Describe()} sent a message larger than {MaxInboundBytes} bytes.");
                await CloseAsync(WebSocketCloseStatus.MessageTooBig, "message too big").ConfigureAwait(false);
                return null;
            }

            if (result.EndOfMessage)
            {
                if (result.MessageType != WebSocketMessageType.Text)
                {
                    message.SetLength(0);
                    continue;
                }

                return Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            }
        }

        return null;
    }

    private void Handle(string text, CancellationToken token)
    {
        switch (ClientMessage.Parse(text))
        {
            case HeartbeatMessage heartbeat:
                LastHeartbeat = DateTimeOffset.UtcNow;
                ReportsVisible = heartbeat.Visible;
                break;
            case RequestMessage request when !_rate.TryAcquire():
                Enqueue(Messages.ReplyFailure(request.Id, ErrorCode.RateLimited, "Too many requests."));
                break;
            case RequestMessage request:
                _ = RespondAsync(request, token);
                break;
            case AuthMessage:
                _log.Warning("session", $"{Describe()} tried to authenticate twice.");
                break;
            case InvalidMessage invalid:
                _log.Warning("session", $"{Describe()} sent an invalid message: {invalid.Reason}");
                if (invalid.Id is not null)
                {
                    Enqueue(Messages.ReplyFailure(invalid.Id, ErrorCode.InvalidRequest, invalid.Reason));
                }

                break;
        }
    }

    private async Task RespondAsync(RequestMessage request, CancellationToken token)
    {
        try
        {
            Enqueue(await _router.DispatchAsync(request, Widget, Monitor, token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private async Task SendLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (var message in _outbound.Reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                await _socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is WebSocketException or IOException)
        {
            _log.Debug("session", $"{Describe()} send failed: {exception.Message}");
            await _lifetime.CancelAsync().ConfigureAwait(false);
        }
    }

    private static async Task DrainAsync(Task<string?> receiving)
    {
        try
        {
            await receiving.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or TimeoutException or IOException)
        {
        }
    }

    private async Task CloseAsync(WebSocketCloseStatus status, string reason)
    {
        if (_socket.State is not (WebSocketState.Open or WebSocketState.CloseReceived))
        {
            return;
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await _socket.CloseOutputAsync(status, reason, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or IOException)
        {
        }
    }
}
