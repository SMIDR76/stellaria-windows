using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Security;

namespace Caelestia.Helper.Server;

internal sealed class HelperServer(StateHub hub, RequestRouter router, Log log, string helperVersion, TimeProvider time, AccessRegistry? access = null) : IAsyncDisposable
{
    public const int FirstPort = 47610;
    public const int LastPort = 47619;
    public const int MaxConnections = 64;
    public static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(5);

    private readonly List<Task> _sessionTasks = [];
    private readonly Lock _gate = new();
    private TcpListener? _listener;
    private int _openConnections;

    public AccessRegistry Access { get; } = access ?? new AccessRegistry();

    public string Token => Access.OfficialToken;

    public int Port { get; private set; }

    public void Start(int firstPort = FirstPort, int lastPort = LastPort)
    {
        for (var port = firstPort; port <= lastPort; port++)
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
            }
            catch (SocketException exception) when (exception.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                continue;
            }

            _listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            log.Info("server", $"Listening on 127.0.0.1:{Port}.");
            return;
        }

        throw new InvalidOperationException($"No free port between {firstPort} and {lastPort}.");
    }

    public async Task RunAsync(CancellationToken shutdown)
    {
        var listener = _listener ?? throw new InvalidOperationException("Start must be called before RunAsync.");
        var pings = PingLoopAsync(shutdown);
        try
        {
            while (!shutdown.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(shutdown).ConfigureAwait(false);
                var task = ServeAsync(client, shutdown);
                lock (_gate)
                {
                    _sessionTasks.RemoveAll(existing => existing.IsCompleted);
                    _sessionTasks.Add(task);
                }
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
        }
        finally
        {
            listener.Stop();
            await pings.ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listener?.Stop();
        Task[] pending;
        lock (_gate)
        {
            pending = [.. _sessionTasks];
        }

        await Task.WhenAll(pending).ConfigureAwait(false);
    }

    private async Task ServeAsync(TcpClient client, CancellationToken shutdown)
    {
        var admitted = Interlocked.Increment(ref _openConnections) <= MaxConnections;
        try
        {
            using (client)
            {
                client.NoDelay = true;
                var stream = client.GetStream();
                if (!await HandshakeAsync(stream, admitted, shutdown).ConfigureAwait(false))
                {
                    return;
                }

                var socket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions { IsServer = true, KeepAliveInterval = TimeSpan.Zero });
                await using var session = new ClientSession(socket, Access.Resolve, router, log, OnAuthenticated, time, shutdown);
                try
                {
                    await session.RunAsync().ConfigureAwait(false);
                }
                finally
                {
                    if (session.IsAuthenticated)
                    {
                        hub.Detach(session);
                        log.Info("server", $"{session.Describe()} disconnected.");
                    }
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _openConnections);
        }
    }

    private void OnAuthenticated(ClientSession session)
    {
        session.Enqueue(Messages.Hello(helperVersion));
        hub.Attach(session);
        log.Info("server", $"{session.Describe()} connected.");
    }

    private async Task<bool> HandshakeAsync(NetworkStream stream, bool admitted, CancellationToken shutdown)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(shutdown);
            timeout.CancelAfter(HandshakeTimeout);
            var header = await ReadHeaderAsync(stream, timeout.Token).ConfigureAwait(false);
            var request = header is null ? null : WebSocketHandshake.Parse(header);
            HandshakeDecision decision = request is null
                ? new HandshakeRejected(400, "Malformed HTTP request.")
                : !admitted
                    ? new HandshakeRejected(503, "Too many open connections.")
                    : WebSocketHandshake.Decide(request, Port);
            if (decision is HandshakeRejected rejected)
            {
                log.Warning("server", $"Rejected a connection: {rejected.Reason}");
                await stream.WriteAsync(Encoding.UTF8.GetBytes(WebSocketHandshake.RejectResponse(rejected)), shutdown).ConfigureAwait(false);
                return false;
            }

            var accepted = (HandshakeAccepted)decision;
            await stream.WriteAsync(Encoding.ASCII.GetBytes(WebSocketHandshake.AcceptResponse(accepted.AcceptKey)), shutdown).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException or SocketException)
        {
            return false;
        }
    }

    private static async Task<string?> ReadHeaderAsync(NetworkStream stream, CancellationToken cancellation)
    {
        var bytes = new List<byte>(1024);
        var single = new byte[1];
        while (bytes.Count < WebSocketHandshake.MaxHeaderBytes)
        {
            if (await stream.ReadAsync(single, cancellation).ConfigureAwait(false) == 0)
            {
                return null;
            }

            bytes.Add(single[0]);
            if (bytes.Count >= 4 && bytes[^4] == '\r' && bytes[^3] == '\n' && bytes[^2] == '\r' && bytes[^1] == '\n')
            {
                return Encoding.ASCII.GetString([.. bytes]);
            }
        }

        return null;
    }

    private async Task PingLoopAsync(CancellationToken shutdown)
    {
        using var timer = new PeriodicTimer(PingInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(shutdown).ConfigureAwait(false))
            {
                hub.Broadcast(Messages.Ping(DateTimeOffset.UtcNow));
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
        }
    }
}
