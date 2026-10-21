namespace Caelestia.Helper.Desktop;

internal static class SafePaths
{
    public static readonly IReadOnlySet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp" };

    public static bool IsInside(string candidate, string directory)
    {
        if (!Path.IsPathFullyQualified(candidate) || !Path.IsPathFullyQualified(directory))
        {
            return false;
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(candidate);
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsImage(string path) => ImageExtensions.Contains(Path.GetExtension(path));

    public static string WallpaperDirectory(string? configured)
    {
        if (configured is not null && Directory.Exists(configured))
        {
            return configured;
        }

        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var wallpapers = Path.Combine(pictures, "Wallpapers");
        return Directory.Exists(wallpapers) ? wallpapers : pictures;
    }

    public static string ScreenshotsDirectory(string? configured)
    {
        if (configured is not null && Directory.Exists(configured))
        {
            return configured;
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
    }
}
