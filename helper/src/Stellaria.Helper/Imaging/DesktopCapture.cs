using Stellaria.Helper.Desktop;
using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Imaging;

internal static class DesktopCapture
{
    public const uint PrintFullContent = 2;
    public const int SampleWidth = 480;

    public static PixelImage? PrimaryMonitorBackground(int width = SampleWidth)
    {
        var desktop = User32.FindWindow("Progman", null);
        if (desktop == 0 || !User32.GetWindowRect(desktop, out var bounds) || bounds.IsEmpty)
        {
            return null;
        }

        var primary = DesktopReader.Monitors().FirstOrDefault(monitor => monitor.Primary);
        if (primary is null)
        {
            return null;
        }

        return WindowCapture.PrintRegion(desktop, bounds.Width, bounds.Height, PrintFullContent,
            primary.Bounds.Left - bounds.Left, primary.Bounds.Top - bounds.Top, primary.Bounds.Width, primary.Bounds.Height, width);
    }
}
