using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Server;

internal sealed class ClientSession : IAsyncDisposable
{
    public const int OutboundCapacity = 512;
    public const int MaxInboundBytes = 1024 * 1024;

    private readonly WebSocket _socket;
    private readonly RequestRouter _router;
    private readonly Log _log;
    private readonly Channel<string> _outbound;
    private readonly CancellationTokenSource _lifetime;

    public ClientSession(WebSocket socket, string widget, string monitor, RequestRouter router, Log log, CancellationToken shutdown)
    {
        _socket = socket;
        Widget = widget;
        Monitor = monitor;
        _router = router;
        _log = log;
        _outbound = Channel.CreateBounded<string>(new BoundedChannelOptions(OutboundCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(shutdown);
    }

    public string Widget { get; }

    public string Monitor { get; }

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
            await ReceiveLoopAsync(token).ConfigureAwait(false);
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
        if (_socket.State == WebSocketState.Open)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            try
            {
                await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "closing", timeout.Token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or IOException)
            {
            }
        }

        _socket.Dispose();
        _lifetime.Dispose();
    }

    public string Describe() => string.IsNullOrEmpty(Monitor) ? Widget : $"{Widget}@{Monitor}";

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

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();
        while (_socket.State == WebSocketState.Open)
        {
            var result = await _socket.ReceiveAsync(buffer, token).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            message.Write(buffer, 0, result.Count);
            if (message.Length > MaxInboundBytes)
            {
                _log.Warning("session", $"{Describe()} sent a message larger than {MaxInboundBytes} bytes.");
                return;
            }

            if (!result.EndOfMessage)
            {
                continue;
            }

            var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            message.SetLength(0);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                Handle(text, token);
            }
        }
    }

    private void Handle(string text, CancellationToken token)
    {
        switch (ClientMessage.Parse(text))
        {
            case HeartbeatMessage heartbeat:
                LastHeartbeat = DateTimeOffset.UtcNow;
                ReportsVisible = heartbeat.Visible;
                break;
            case RequestMessage request:
                _ = RespondAsync(request, token);
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
}
