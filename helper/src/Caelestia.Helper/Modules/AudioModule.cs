using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Server;

namespace Caelestia.Helper.Modules;

internal sealed class AudioModule : IHelperModule
{
    public const int MaxEndpointIdLength = 256;

    public string Name => "audio";

    public Task StartAsync(ModuleContext context)
    {
        context.Router.Register("audio.setDefault", async (request, _) =>
        {
            var id = request.RequireString("id");
            if (id.Length > MaxEndpointIdLength)
            {
                throw new HelperException(ErrorCode.InvalidParams, "Audio device id is too long.");
            }

            await context.Loop.InvokeAsync(() =>
            {
                if (!AudioEndpoints.IsActiveEndpoint(id))
                {
                    throw new HelperException(ErrorCode.NotFound, "This audio device is not connected.");
                }

                var result = AudioEndpoints.SetDefault(id);
                if (result != 0)
                {
                    throw new HelperException(ErrorCode.Failed, $"Windows refused the audio device (0x{result:X8}).");
                }
            }).ConfigureAwait(false);
            return null;
        });
        context.Router.Register("audio.sessions", async (_, _) =>
        {
            var sessions = await context.Loop.InvokeAsync(AudioSessions.List).ConfigureAwait(false);
            var list = new JsonArray();
            foreach (var session in sessions)
            {
                var process = context.Desktop.Processes.Find(session.ProcessId);
                var name = session.IsSystemSounds ? "system" : SessionName(session, process);
                var icon = process is null ? null : await context.Icons.GetAsync(process.ImagePath).ConfigureAwait(false);
                list.Add((JsonNode)new JsonObject
                {
                    ["id"] = session.Id,
                    ["name"] = name,
                    ["system"] = session.IsSystemSounds,
                    ["volume"] = (int)Math.Round(session.Volume * 100),
                    ["muted"] = session.Muted,
                    ["icon"] = icon,
                });
            }

            return list;
        });
        context.Router.Register("audio.setSessionVolume", async (request, _) =>
        {
            var id = RequireSessionId(request);
            var volume = request.RequireInteger("volume", 0, 100);
            await ChangeSessionAsync(context, () => AudioSessions.SetVolume(id, volume / 100f)).ConfigureAwait(false);
            return null;
        });
        context.Router.Register("audio.setSessionMute", async (request, _) =>
        {
            var id = RequireSessionId(request);
            var muted = request.RequireBoolean("muted");
            await ChangeSessionAsync(context, () => AudioSessions.SetMute(id, muted)).ConfigureAwait(false);
            return null;
        });
        return Task.CompletedTask;
    }

    public const int MaxSessionIdLength = 1024;

    public static string SessionName(AudioSession session, ProcessIdentity? process)
    {
        if (session.DisplayName.Length > 0 && !session.DisplayName.StartsWith('@'))
        {
            return session.DisplayName;
        }

        return process?.Name ?? $"PID {session.ProcessId}";
    }

    private static string RequireSessionId(RequestContext request)
    {
        var id = request.RequireString("id");
        return id.Length <= MaxSessionIdLength ? id : throw new HelperException(ErrorCode.InvalidParams, "Audio session id is too long.");
    }

    private static async Task ChangeSessionAsync(ModuleContext context, Func<bool> change)
    {
        if (!await context.Loop.InvokeAsync(change).ConfigureAwait(false))
        {
            throw new HelperException(ErrorCode.NotFound, "This audio session has ended.");
        }
    }
}
