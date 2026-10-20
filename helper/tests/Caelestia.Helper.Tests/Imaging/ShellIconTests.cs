using Caelestia.Helper.Imaging;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Tests.Imaging;

public sealed class ShellIconTests
{
    private static readonly string Explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

    [Fact]
    public void The_explorer_icon_is_read_with_transparency()
    {
        var icon = GdiBitmaps.ReadAndDelete(ShellItemImages.IconBitmap(Explorer, 48));

        Assert.NotNull(icon);
        Assert.InRange(icon.Width, 32, 256);
        Assert.Contains(icon.Bgra.Where((_, index) => index % 4 == 3), alpha => alpha < 255);
        Assert.Contains(icon.Bgra.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
    }

    [Fact]
    public void A_missing_file_gives_no_bitmap()
    {
        Assert.Equal(0, ShellItemImages.IconBitmap(@"C:\does\not\exist\nothing.exe", 48));
    }
}
