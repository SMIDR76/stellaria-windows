using Stellaria.Helper.Imaging;
using Stellaria.Helper.Modules;

namespace Stellaria.Helper.Tests.Modules;

public sealed class ThemeSourceTests
{
    private static int[] Print(int value) => [.. Enumerable.Repeat(value, Fingerprint.Length)];

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

    [Theory]
    [InlineData(null, 480)]
    [InlineData("1920", 1920)]
    [InlineData("100", 320)]
    [InlineData("9000", 2560)]
    [InlineData("\"wide\"", 480)]
    public void The_app_gets_a_preview_as_wide_as_it_asks_within_bounds(string? json, int expected)
    {
        Assert.Equal(expected, ThemeSourceModule.PreviewWidthFor(json is null ? null : System.Text.Json.Nodes.JsonNode.Parse(json)));
    }
}
