using Stellaria.Helper.Imaging;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Modules;
using Stellaria.Helper.Protocol;
using Windows.Devices.Radios;

namespace Stellaria.Helper.Tests.Modules;

public sealed class DeviceModulesTests
{
    [Theory]
    [InlineData(RadioKind.WiFi, "wifi")]
    [InlineData(RadioKind.Bluetooth, "bluetooth")]
    [InlineData(RadioKind.MobileBroadband, "mobile")]
    [InlineData(RadioKind.Other, "other")]
    public void Radio_kinds_have_stable_protocol_names(RadioKind kind, string name)
    {
        Assert.Equal(name, RadiosModule.KindName(kind));
    }

    [Fact]
    public void Long_clipboard_texts_are_truncated()
    {
        Assert.Equal(ClipboardModule.MaxTextLength, ClipboardModule.Truncate(new string('x', 10_000)).Length);
        Assert.Equal("short", ClipboardModule.Truncate("short"));
    }

    [Fact]
    public void Unknown_window_actions_are_invalid_params()
    {
        var exception = Assert.Throws<HelperException>(() => WindowModule.Act(1, "explode"));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }

    [Fact]
    public void Acting_without_a_window_is_not_found()
    {
        var exception = Assert.Throws<HelperException>(() => WindowModule.Act(0, "close"));

        Assert.Equal(ErrorCode.NotFound, exception.Code);
    }

    [Fact]
    public void The_taskbar_window_can_be_captured_and_scaled_down()
    {
        var taskbar = User32.FindWindow("Shell_TrayWnd", null);
        Assert.SkipWhen(taskbar == 0 || !User32.GetWindowRect(taskbar, out _), "No taskbar on this machine.");
        User32.GetWindowRect(taskbar, out var bounds);

        var image = WindowCapture.PrintRegion(taskbar, bounds.Width, bounds.Height, WindowModule.PrintFullContent, 0, 0, bounds.Width, bounds.Height, 64);

        Assert.NotNull(image);
        Assert.Equal(Math.Min(64, bounds.Width), image.Width);
        Assert.All(image.Bgra.Where((_, index) => index % 4 == 3), alpha => Assert.Equal(255, alpha));
    }
}
