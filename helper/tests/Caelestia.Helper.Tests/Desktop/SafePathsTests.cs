using Caelestia.Helper.Desktop;
using Caelestia.Helper.Modules;

namespace Caelestia.Helper.Tests.Desktop;

public sealed class SafePathsTests
{
    private const string Folder = @"C:\Users\someone\Pictures\Wallpapers";

    [Theory]
    [InlineData(@"C:\Users\someone\Pictures\Wallpapers\sky.jpg", true)]
    [InlineData(@"C:\Users\someone\Pictures\Wallpapers\sub\sky.jpg", true)]
    [InlineData(@"c:\users\SOMEONE\pictures\wallpapers\sky.jpg", true)]
    [InlineData(@"C:\Users\someone\Pictures\Wallpapers\..\secret.jpg", false)]
    [InlineData(@"C:\Users\someone\Pictures\WallpapersEvil\sky.jpg", false)]
    [InlineData(@"C:\Windows\System32\evil.jpg", false)]
    [InlineData(@"Wallpapers\sky.jpg", false)]
    [InlineData(@"\\server\share\sky.jpg", false)]
    public void Only_paths_inside_the_folder_are_accepted(string candidate, bool inside)
    {
        Assert.Equal(inside, SafePaths.IsInside(candidate, Folder));
    }

    [Theory]
    [InlineData("sky.JPG", true)]
    [InlineData("sky.jpeg", true)]
    [InlineData("sky.png", true)]
    [InlineData("sky.bmp", true)]
    [InlineData("sky.exe", false)]
    [InlineData("sky.jpg.lnk", false)]
    [InlineData("sky", false)]
    public void Only_image_extensions_are_wallpapers(string name, bool image)
    {
        Assert.Equal(image, SafePaths.IsImage(name));
    }

    [Fact]
    public void Thumbnail_keys_change_when_the_file_changes()
    {
        var time = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);
        var key = WallpapersModule.ThumbnailKey(@"C:\w\a.jpg", 100, time);

        Assert.Equal(64, key.Length);
        Assert.Equal(key, WallpapersModule.ThumbnailKey(@"c:\W\A.JPG", 100, time));
        Assert.NotEqual(key, WallpapersModule.ThumbnailKey(@"C:\w\a.jpg", 101, time));
        Assert.NotEqual(key, WallpapersModule.ThumbnailKey(@"C:\w\a.jpg", 100, time.AddSeconds(1)));
    }

    [Fact]
    public void A_missing_configured_folder_falls_back_to_pictures()
    {
        var folder = SafePaths.WallpaperDirectory(@"C:\definitely\missing\folder");

        Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), folder, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Settings_pages_are_a_closed_list_of_settings_uris()
    {
        Assert.All(WallpapersModule.SettingsPages.Values, uri => Assert.StartsWith("ms-settings:", uri, StringComparison.Ordinal));
    }
}
