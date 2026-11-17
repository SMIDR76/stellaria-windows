using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Tests.Interop;

public sealed class AudioEndpointsTests
{
    [Theory]
    [InlineData("{0.0.0.00000000}.{00000000-0000-0000-0000-000000000000}")]
    [InlineData("not an endpoint")]
    public void Unknown_endpoints_are_not_active(string id)
    {
        Assert.False(AudioEndpoints.IsActiveEndpoint(id));
    }

    [Fact]
    public void The_current_default_output_is_active_and_can_be_set_again()
    {
        var current = AudioEndpoints.DefaultPlaybackId();
        Assert.SkipWhen(current is null, "This machine has no audio output.");

        Assert.True(AudioEndpoints.IsActiveEndpoint(current!));
        Assert.Equal(0, AudioEndpoints.SetDefault(current!));
        Assert.Equal(current, AudioEndpoints.DefaultPlaybackId());
    }
}
