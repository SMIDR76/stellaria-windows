using System.Text.Json.Nodes;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Modules;

internal static class IdleReporting
{
    public const int Threshold = 30;
    public const int Step = 5;

    public static int? NextReport(int lastReported, int idleSeconds)
    {
        if (idleSeconds < Threshold)
        {
            return lastReported == 0 ? null : 0;
        }

        return lastReported == 0 || idleSeconds - lastReported >= Step || idleSeconds < lastReported ? idleSeconds : null;
    }
}

internal sealed class IdleModule : IHelperModule
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private Timer? _poll;
    private int _lastReported;

    public string Name => "idle";

    public Task StartAsync(ModuleContext context)
    {
        context.Hub.Publish("idle", new JsonObject { ["seconds"] = 0 });
        _poll = new Timer(_ => Check(context), null, PollInterval, PollInterval);
        context.Shutdown.Register(() => _poll.Dispose());
        return Task.CompletedTask;
    }

    public static int IdleSeconds()
    {
        var info = new LastInputInfo { Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<LastInputInfo>() };
        return User32.GetLastInputInfo(ref info) ? (int)(unchecked((uint)Environment.TickCount - info.Time) / 1000) : 0;
    }

    private void Check(ModuleContext context)
    {
        if (IdleReporting.NextReport(_lastReported, IdleSeconds()) is not { } report)
        {
            return;
        }

        _lastReported = report;
        context.Hub.Publish("idle", new JsonObject { ["seconds"] = report });
    }
}
