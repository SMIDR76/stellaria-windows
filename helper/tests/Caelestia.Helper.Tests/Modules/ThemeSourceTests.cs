using Caelestia.Helper.Imaging;
using Caelestia.Helper.Modules;

namespace Caelestia.Helper.Tests.Modules;

public sealed class ThemeSourceTests
{
    private static int[] Print(int value) => Enumerable.Repeat(value, Fingerprint.Length).ToArray();

    [Fact]
    public void An_announced_change_is_always_published()
    {
        Assert.True(ThemeSourceModule.ShouldPublish(true, Print(10), Print(10)));
    }

    [Fact]
    public void A_periodic_check_publishes_only_a_visible_change()
    {
        Assert.False(ThemeSourceModule.ShouldPublish(false, Print(100), Print(110)));
        Assert.True(ThemeSourceModule.ShouldPublish(false, Print(100), Print(130)));
    }

    [Fact]
    public void The_first_sample_is_published()
    {
        Assert.True(ThemeSourceModule.ShouldPublish(false, null, Print(0)));
    }

    [Fact]
    public void The_primary_monitor_background_can_be_captured()
    {
        var capture = DesktopCapture.PrimaryMonitorBackground();
        Assert.SkipWhen(capture is null, "No desktop window to capture in this session.");

        Assert.True(capture.Width > 0 && capture.Height > 0);
        Assert.Equal(Fingerprint.Length, Fingerprint.Compute(capture).Length);
    }
}
