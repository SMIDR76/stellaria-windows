using Stellaria.Helper.Desktop;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Tests.TestSupport;

namespace Stellaria.Helper.Tests.Desktop;

public sealed class AppBarRulesTests
{
    private static readonly Rect s_monitor = new(0, 0, 3440, 1440);

    [Fact]
    public void Thin_windows_are_classified_by_the_edge_they_hug()
    {
        Assert.Equal(DockEdge.Left, AppBarRules.Classify(1, new Rect(0, 10, 48, 1430), s_monitor)!.Edge);
        Assert.Equal(DockEdge.Top, AppBarRules.Classify(1, new Rect(0, 0, 3440, 10), s_monitor)!.Edge);
        Assert.Equal(DockEdge.Right, AppBarRules.Classify(1, new Rect(3430, 10, 3440, 1430), s_monitor)!.Edge);
        Assert.Equal(DockEdge.Bottom, AppBarRules.Classify(1, new Rect(0, 1430, 3440, 1440), s_monitor)!.Edge);
    }

    [Fact]
    public void A_window_thicker_than_a_dock_is_never_treated_as_one()
    {
        Assert.Null(AppBarRules.Classify(1, new Rect(48, 240, 3488, 840), s_monitor));
    }

    [Fact]
    public void A_dock_is_reserved_when_the_work_area_stops_at_its_inner_side()
    {
        var bar = AppBarRules.Classify(1, new Rect(0, 10, 48, 1430), s_monitor)!;

        Assert.True(AppBarRules.IsReserved(bar, new Rect(48, 10, 3430, 1430)));
        Assert.False(AppBarRules.IsReserved(bar, new Rect(0, 0, 3440, 1392)));
    }

    [Fact]
    public void A_reservation_by_another_app_on_another_edge_does_not_hide_a_lost_dock()
    {
        var bottom = AppBarRules.Classify(1, new Rect(0, 1430, 3440, 1440), s_monitor)!;
        var left = AppBarRules.Classify(2, new Rect(0, 10, 48, 1430), s_monitor)!;
        var workWithOnlyTaskbar = new Rect(0, 0, 3440, 1392);

        Assert.True(AppBarRules.IsReserved(bottom, workWithOnlyTaskbar));
        Assert.False(AppBarRules.IsReserved(left, workWithOnlyTaskbar));
    }

    [Fact]
    public void A_dock_pushed_inward_by_stacked_reservations_no_longer_touches_its_edge()
    {
        var side = new Rect(-1920, 0, 0, 1080);
        var healthy = AppBarRules.Classify(1, new Rect(-1920, 10, -1872, 1070), side)!;
        var pushed = AppBarRules.Classify(2, new Rect(-1152, 160, -1104, 1220), side)!;

        Assert.True(AppBarRules.TouchesItsEdge(healthy, side));
        Assert.False(AppBarRules.TouchesItsEdge(pushed, side));
    }

    [Theory]
    [InlineData("Zebar - stellaria / bar", true)]
    [InlineData("Zebar - stellaria / edge", true)]
    [InlineData("Zebar - stellaria / dashboard", false)]
    [InlineData("stellaria / bar - Notepad", false)]
    public void Only_bar_and_edge_widgets_are_docks(string title, bool dock)
    {
        Assert.Equal(dock, AppBarRules.IsDockTitle(title));
    }

    [Fact]
    public void The_breaker_trips_on_the_fifth_repair_within_a_minute()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var breaker = new RepairBreaker(5, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), time);

        for (var repair = 0; repair < 4; repair++)
        {
            Assert.False(breaker.RecordRepair());
            time.Advance(TimeSpan.FromSeconds(2));
        }

        Assert.True(breaker.RecordRepair());
        Assert.True(breaker.IsPaused);
        time.Advance(TimeSpan.FromMinutes(5));
        Assert.False(breaker.IsPaused);
    }

    [Fact]
    public void Occasional_repairs_never_trip_the_breaker()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var breaker = new RepairBreaker(5, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), time);

        for (var repair = 0; repair < 20; repair++)
        {
            Assert.False(breaker.RecordRepair());
            time.Advance(TimeSpan.FromSeconds(20));
        }
    }

    [Fact]
    public void An_edge_is_resized_towards_the_screen_centre_and_keeps_its_outer_side()
    {
        var top = AppBarRules.Classify(1, new Rect(0, 0, 3440, 15), s_monitor)!;
        var right = AppBarRules.Classify(2, new Rect(3425, 15, 3440, 1425), s_monitor)!;
        var bottom = AppBarRules.Classify(3, new Rect(0, 1425, 3440, 1440), s_monitor)!;

        Assert.Equal(new Rect(0, 0, 3440, 24), AppBarRules.Resized(top, 24));
        Assert.Equal(new Rect(3416, 15, 3440, 1425), AppBarRules.Resized(right, 24));
        Assert.Equal(new Rect(0, 1434, 3440, 1440), AppBarRules.Resized(bottom, 6));
    }

    [Theory]
    [InlineData(10, 96u, 10)]
    [InlineData(10, 144u, 15)]
    [InlineData(7, 120u, 9)]
    [InlineData(10, 0u, 10)]
    public void The_frame_thickness_follows_the_monitor_scale(int logical, uint dpi, int physical)
    {
        Assert.Equal(physical, AppBarRules.Scaled(logical, dpi));
    }

    [Fact]
    public void Only_edge_windows_are_resized()
    {
        Assert.True(AppBarRules.IsEdgeTitle("zebar - stellaria / edge"));
        Assert.False(AppBarRules.IsEdgeTitle("zebar - stellaria / bar"));
    }
}
