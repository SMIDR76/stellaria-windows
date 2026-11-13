using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Modules;

/// <summary>
/// Feeds the audio visualiser drawn on the desktop. The spectrum is sent about 30 times a second, so unlike other
/// data it only goes to the widgets that draw it, and the loopback capture only runs while one of them is connected.
/// </summary>
internal sealed class VisualiserModule : IHelperModule
{
    public const int BandCount = 45;
    public const int FftSize = 2048;
    public static readonly IReadOnlySet<string> Viewers = new HashSet<string>(StringComparer.Ordinal) { "desktop-clock" };
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(33);
    public static readonly TimeSpan IdleInterval = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(3);
    public const long SilenceMilliseconds = 1500;

    public string Name => "visualiser";

    public Task StartAsync(ModuleContext context)
    {
        var thread = new Thread(() => Run(context)) { IsBackground = true, Name = "Caelestia visualiser" };
        thread.Start();
        return Task.CompletedTask;
    }

    private static void Run(ModuleContext context)
    {
        LoopbackCapture? capture = null;
        Spectrum? spectrum = null;
        var samples = new float[FftSize];
        var silentSent = false;
        while (!context.Shutdown.IsCancellationRequested)
        {
            var viewers = context.Hub.Sessions.Where(session => session.IsAuthenticated && Viewers.Contains(session.Widget)).ToList();
            if (viewers.Count == 0 || !context.Config.GetFlag("visualiser.enabled"))
            {
                capture?.Dispose();
                capture = null;
                context.Shutdown.WaitHandle.WaitOne(IdleInterval);
                continue;
            }

            if (capture is null)
            {
                try
                {
                    capture = LoopbackCapture.Open();
                }
                catch (Exception exception)
                {
                    context.Log.Warning("visualiser", "The audio loopback capture could not start.", exception);
                }

                if (capture is null)
                {
                    context.Shutdown.WaitHandle.WaitOne(RetryInterval);
                    continue;
                }

                spectrum = new Spectrum(FftSize, BandCount, capture.SampleRate);
                silentSent = false;
            }

            if (capture.Pump() != 0)
            {
                // The default device changed or disappeared: reopen on the next frame.
                capture.Dispose();
                capture = null;
                continue;
            }

            var playing = Environment.TickCount64 - capture.LastAudioTicks < SilenceMilliseconds;
            if (playing || !silentSent)
            {
                capture.Latest(samples);
                var bars = playing ? spectrum!.Process(samples) : new int[BandCount];
                silentSent = !playing;
                var message = Messages.Event("visualiser", new JsonObject { ["bars"] = ToJson(bars) });
                foreach (var viewer in viewers)
                {
                    viewer.Enqueue(message);
                }
            }

            context.Shutdown.WaitHandle.WaitOne(FrameInterval);
        }

        capture?.Dispose();
    }

    private static JsonArray ToJson(int[] bars)
    {
        var array = new JsonArray();
        foreach (var bar in bars)
        {
            array.Add((JsonNode)bar);
        }

        return array;
    }
}
