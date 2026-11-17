using Stellaria.Helper.Desktop;
using Stellaria.Helper.Modules;

namespace Stellaria.Helper.Tests.Desktop;

public sealed class ZebarWatchdogTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_responsive_zebar_is_left_alone()
    {
        Assert.False(ZebarWatchdog.ShouldRestart(null, Now, DateTimeOffset.MinValue));
    }

    [Fact]
    public void A_short_hang_is_tolerated()
    {
        Assert.False(ZebarWatchdog.ShouldRestart(Now.AddSeconds(-10), Now, DateTimeOffset.MinValue));
    }

    [Fact]
    public void A_hang_of_fifteen_seconds_triggers_a_restart()
    {
        Assert.True(ZebarWatchdog.ShouldRestart(Now.AddSeconds(-15), Now, DateTimeOffset.MinValue));
    }

    [Fact]
    public void Restarts_are_spaced_out_so_a_broken_setup_cannot_loop()
    {
        Assert.False(ZebarWatchdog.ShouldRestart(Now.AddSeconds(-20), Now, Now.AddMinutes(-5)));
        Assert.True(ZebarWatchdog.ShouldRestart(Now.AddSeconds(-20), Now, Now.AddMinutes(-11)));
    }

    [Fact]
    public void Docks_pushed_away_for_six_seconds_after_a_display_change_trigger_a_restart()
    {
        Assert.False(ShellModule.ShouldRestartForPushedDocks(null, Now));
        Assert.False(ShellModule.ShouldRestartForPushedDocks(Now.AddSeconds(-4), Now));
        Assert.True(ShellModule.ShouldRestartForPushedDocks(Now.AddSeconds(-6), Now));
    }
}
