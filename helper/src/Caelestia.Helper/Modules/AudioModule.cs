using Caelestia.Helper.Interop;
using Caelestia.Helper.Protocol;

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
        return Task.CompletedTask;
    }
}
