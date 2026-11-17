using Stellaria.Helper.Desktop;
using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Tests.Desktop;

public sealed class WindowRulesTests
{
    private static readonly MonitorFacts s_wide = new(@"\\.\DISPLAY1", new Rect(0, 0, 3440, 1440), new Rect(48, 10, 3430, 1430), true);
    private static readonly MonitorFacts s_side = new(@"\\.\DISPLAY2", new Rect(-1920, 0, 0, 1080), new Rect(-1872, 10, -10, 1070), false);

    private static WindowFacts Window(
        string title = "Document",
        string process = "notepad",
        string className = "Notepad",
        int style = 0,
        bool visible = true,
        bool minimized = false,
        bool maximized = false,
        bool cloaked = false,
        bool owner = false,
        Rect? bounds = null) =>
        new(1, className, title, process, style, visible, minimized, maximized, cloaked, owner, bounds ?? new Rect(100, 100, 900, 700));

    [Fact]
    public void A_normal_top_level_window_is_a_task()
    {
        Assert.True(WindowRules.IsTask(Window()));
    }

    [Fact]
    public void Untitled_hidden_and_cloaked_windows_are_not_tasks()
    {
        Assert.False(WindowRules.IsTask(Window(title: "")));
        Assert.False(WindowRules.IsTask(Window(visible: false)));
        Assert.False(WindowRules.IsTask(Window(cloaked: true)));
    }

    [Fact]
    public void Tool_windows_and_owned_windows_are_not_tasks_unless_they_opt_in()
    {
        Assert.False(WindowRules.IsTask(Window(style: User32.WsExToolWindow)));
        Assert.False(WindowRules.IsTask(Window(owner: true)));
        Assert.True(WindowRules.IsTask(Window(style: User32.WsExToolWindow | User32.WsExAppWindow, owner: true)));
    }

    [Fact]
    public void Shell_and_stellaria_windows_are_never_tasks()
    {
        Assert.False(WindowRules.IsTask(Window(className: "Shell_TrayWnd")));
        Assert.False(WindowRules.IsTask(Window(process: "zebar")));
        Assert.False(WindowRules.IsTask(Window(process: "stellaria-helper")));
    }

    [Fact]
    public void The_topmost_window_on_each_monitor_decides_its_coverage()
    {
        var game = Window(process: "game", bounds: new Rect(0, 0, 3440, 1440));
        var editor = Window(process: "code", maximized: true, bounds: new Rect(-1872, 10, -10, 1070));

        var coverage = WindowRules.Coverage([game, editor], [s_wide, s_side]);

        Assert.Equal(MonitorCoverage.Fullscreen, coverage[s_wide.Device]);
        Assert.Equal(MonitorCoverage.Maximized, coverage[s_side.Device]);
    }

    [Fact]
    public void A_window_below_another_does_not_change_the_coverage()
    {
        var small = Window(bounds: new Rect(200, 200, 600, 600));
        var fullscreenBehind = Window(process: "game", bounds: new Rect(0, 0, 3440, 1440));

        var coverage = WindowRules.Coverage([small, fullscreenBehind], [s_wide]);

        Assert.Equal(MonitorCoverage.Free, coverage[s_wide.Device]);
    }

    [Fact]
    public void Minimized_transparent_and_widget_windows_are_skipped()
    {
        var minimized = Window(minimized: true, bounds: new Rect(0, 0, 3440, 1440));
        var overlay = Window(style: User32.WsExTransparent, bounds: new Rect(0, 0, 3440, 1440));
        var widget = Window(process: "zebar", bounds: new Rect(0, 0, 3440, 1440));

        var coverage = WindowRules.Coverage([minimized, overlay, widget], [s_wide]);

        Assert.Equal(MonitorCoverage.Free, coverage[s_wide.Device]);
    }

    [Fact]
    public void Every_monitor_gets_a_value_even_without_windows()
    {
        var coverage = WindowRules.Coverage([], [s_wide, s_side]);

        Assert.Equal(2, coverage.Count);
        Assert.All(coverage.Values, value => Assert.Equal(MonitorCoverage.Free, value));
    }
}
