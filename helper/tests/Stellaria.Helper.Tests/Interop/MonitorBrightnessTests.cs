using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Tests.Interop;

public sealed class MonitorBrightnessTests
{
    [Theory]
    [InlineData(0u, 50u, 100u, 50)]
    [InlineData(0u, 0u, 100u, 0)]
    [InlineData(20u, 60u, 120u, 40)]
    [InlineData(0u, 150u, 100u, 100)]
    [InlineData(50u, 50u, 50u, -1)]
    public void Raw_values_become_percentages_of_the_monitor_range(uint minimum, uint current, uint maximum, int percent)
    {
        Assert.Equal(percent, MonitorBrightness.ToPercent(minimum, current, maximum));
    }

    [Theory]
    [InlineData(0u, 100u, 30, 30u)]
    [InlineData(20u, 120u, 40, 60u)]
    [InlineData(0u, 100u, 140, 100u)]
    [InlineData(0u, 100u, -5, 0u)]
    public void Percentages_are_clamped_into_the_monitor_range(uint minimum, uint maximum, int percent, uint raw)
    {
        Assert.Equal(raw, MonitorBrightness.FromPercent(minimum, maximum, percent));
    }

    [Fact]
    public void Every_monitor_gets_a_brightness_or_the_unsupported_marker()
    {
        var values = MonitorBrightness.ReadAll();

        Assert.NotEmpty(values);
        Assert.All(values.Values, value => Assert.InRange(value, MonitorBrightness.Unsupported, 100));
    }

    [Fact]
    public void Writing_back_the_current_brightness_leaves_it_unchanged()
    {
        var before = MonitorBrightness.ReadAll().Where(entry => entry.Value != MonitorBrightness.Unsupported).ToList();
        Assert.SkipWhen(before.Count == 0, "No monitor on this machine supports DDC/CI brightness.");

        foreach (var (device, percent) in before)
        {
            Assert.True(MonitorBrightness.Write(device, percent));
        }

        var after = MonitorBrightness.ReadAll();
        Assert.All(before, entry => Assert.InRange(after[entry.Key], entry.Value - 1, entry.Value + 1));
    }

    [Fact]
    public void An_unknown_monitor_cannot_be_changed()
    {
        Assert.False(MonitorBrightness.Write(@"\\.\DISPLAY99", 50));
    }
}
