using System.Text.Json.Nodes;
using Stellaria.Helper.Desktop;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Protocol;

namespace Stellaria.Helper.Modules;

internal sealed class VisualiserModule : IHelperModule
{
    public const int BarCount = 48;
    public static readonly TimeSpan Lease = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(33);
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    private readonly object _gate = new();
    private DateTimeOffset _leaseUntil = DateTimeOffset.MinValue;
    private ModuleContext _context = null!;
    private Thread? _worker;

    public string Name => "visualiser";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        context.Router.Register("visualiser.listen", (request, _) =>
        {
            var active = request.RequireBoolean("active");
            lock (_gate)
            {
                _leaseUntil = active ? context.Time.GetUtcNow() + Lease : DateTimeOffset.MinValue;
                if (active && _worker is null)
                {
                    _worker = new Thread(Run) { IsBackground = true, Name = "Stellaria visualiser" };
                    _worker.SetApartmentState(ApartmentState.MTA);
                    _worker.Start();
                }
            }

            return Task.FromResult<JsonNode?>(new JsonObject { ["bars"] = BarCount });
        });
        return Task.CompletedTask;
    }

    public static bool IsSilent(int[] bars) => bars.All(bar => bar == 0);

    private bool Leased()
    {
        lock (_gate)
        {
            if (_context.Time.GetUtcNow() < _leaseUntil)
            {
                return true;
            }

            _worker = null;
            return false;
        }
    }

    private void Run()
    {
        LoopbackCapture? capture = null;
        SpectrumAnalyser? analyser = null;
        var wasSilent = true;
        string? device = null;
        var checkedAt = DateTimeOffset.MinValue;
        try
        {
            while (!_context.Shutdown.IsCancellationRequested && Leased())
            {
                try
                {
                    var now = _context.Time.GetUtcNow();
                    if (now - checkedAt > TimeSpan.FromSeconds(5))
                    {
                        checkedAt = now;
                        var current = AudioEndpoints.DefaultPlaybackId();
                        if (capture is not null && current != device)
                        {
                            capture.Dispose();
                            capture = null;
                        }

                        device = current;
                    }

                    if (capture is null)
                    {
                        capture = LoopbackCapture.Open();
                        if (capture is null)
                        {
                            Thread.Sleep(RetryDelay);
                            continue;
                        }

                        analyser = new SpectrumAnalyser(BarCount, capture.SampleRate);
                    }

                    capture.Read(analyser!.Add);
                    var bars = analyser.Bars();
                    var silent = IsSilent(bars);
                    if (!silent || !wasSilent)
                    {
                        _context.Hub.Raise("visualiser", new JsonObject { ["bars"] = new JsonArray([.. bars.Select(bar => (JsonNode?)JsonValue.Create(bar))]) });
                    }

                    wasSilent = silent;
                    Thread.Sleep(FrameInterval);
                }
                catch (Exception error) when (error is System.Runtime.InteropServices.COMException or InvalidCastException or UnauthorizedAccessException)
                {
                    _context.Log.Warning("visualiser", $"Audio capture restarted: {error.Message}");
                    capture?.Dispose();
                    capture = null;
                    Thread.Sleep(RetryDelay);
                }
            }
        }
        finally
        {
            capture?.Dispose();
        }
    }
}
