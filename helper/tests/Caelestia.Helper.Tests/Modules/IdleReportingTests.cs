using Caelestia.Helper.Modules;

namespace Caelestia.Helper.Tests.Modules;

public sealed class IdleReportingTests
{
    [Fact]
    public void Nothing_is_reported_while_the_user_is_active()
    {
        Assert.Null(IdleReporting.NextReport(0, 3));
        Assert.Null(IdleReporting.NextReport(0, 29));
    }

    [Fact]
    public void Crossing_the_threshold_is_reported_immediately()
    {
        Assert.Equal(30, IdleReporting.NextReport(0, 30));
    }

    [Fact]
    public void Idle_time_is_then_reported_every_five_seconds()
    {
        Assert.Null(IdleReporting.NextReport(30, 34));
        Assert.Equal(35, IdleReporting.NextReport(30, 35));
    }

    [Fact]
    public void Activity_after_idling_is_reported_once_as_zero()
    {
        Assert.Equal(0, IdleReporting.NextReport(120, 1));
        Assert.Null(IdleReporting.NextReport(0, 1));
    }
}
