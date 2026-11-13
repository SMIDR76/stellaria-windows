using Caelestia.Helper.Desktop;

namespace Caelestia.Helper.Tests.Desktop;

public sealed class WinKeyTapTests
{
    private const uint E = 0x45;

    private static WinKeyTap Create() => new(TimeSpan.FromMilliseconds(400));

    [Fact]
    public void A_short_press_of_the_windows_key_alone_is_a_tap()
    {
        var tap = Create();

        Assert.False(tap.Handle(WinKeyTap.LeftWindows, down: true, 1000));
        Assert.True(tap.Handle(WinKeyTap.LeftWindows, down: false, 1150));
    }

    [Fact]
    public void A_shortcut_with_another_key_is_not_a_tap()
    {
        var tap = Create();

        tap.Handle(WinKeyTap.LeftWindows, down: true, 1000);
        tap.Handle(E, down: true, 1050);
        tap.Handle(E, down: false, 1080);

        Assert.False(tap.Handle(WinKeyTap.LeftWindows, down: false, 1100));
    }

    [Fact]
    public void Auto_repeat_after_a_shortcut_does_not_turn_it_into_a_tap()
    {
        var tap = Create();

        tap.Handle(WinKeyTap.RightWindows, down: true, 1000);
        tap.Handle(E, down: true, 1050);
        tap.Handle(WinKeyTap.RightWindows, down: true, 1100);

        Assert.False(tap.Handle(WinKeyTap.RightWindows, down: false, 1150));
    }

    [Fact]
    public void Holding_the_key_too_long_is_not_a_tap()
    {
        var tap = Create();

        tap.Handle(WinKeyTap.LeftWindows, down: true, 1000);

        Assert.False(tap.Handle(WinKeyTap.LeftWindows, down: false, 1500));
    }

    [Fact]
    public void A_new_press_starts_over_after_a_shortcut()
    {
        var tap = Create();

        tap.Handle(WinKeyTap.LeftWindows, down: true, 1000);
        tap.Handle(E, down: true, 1050);
        tap.Handle(WinKeyTap.LeftWindows, down: false, 1100);
        tap.Handle(WinKeyTap.LeftWindows, down: true, 2000);

        Assert.True(tap.Handle(WinKeyTap.LeftWindows, down: false, 2100));
    }
}
