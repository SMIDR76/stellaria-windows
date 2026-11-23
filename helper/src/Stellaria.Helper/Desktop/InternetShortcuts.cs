namespace Stellaria.Helper.Desktop;

internal static class InternetShortcuts
{
    public const int MaxShortcuts = 2000;
    public const int MaxFileBytes = 16 * 1024;

    public static string[] StartMenuRoots() =>
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
    ];

    public static (string? Url, string? IconFile) Parse(string text)
    {
        string? url = null;
        string? icon = null;
        var inShortcut = false;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith('['))
            {
                inShortcut = string.Equals(line, "[InternetShortcut]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inShortcut || line.IndexOf('=') is not (> 0 and var split))
            {
                continue;
            }

            var key = line[..split].Trim();
            var value = line[(split + 1)..].Trim();
            if (value.Length == 0)
            {
                continue;
            }

            if (string.Equals(key, "URL", StringComparison.OrdinalIgnoreCase))
            {
                url ??= value;
            }
            else if (string.Equals(key, "IconFile", StringComparison.OrdinalIgnoreCase))
            {
                icon ??= Environment.ExpandEnvironmentVariables(value);
            }
        }

        return (url, icon);
    }

    public static Dictionary<string, string> IconSources(IEnumerable<string> roots)
    {
        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
        var seen = 0;
        foreach (var root in roots.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.url", options))
            {
                if (++seen > MaxShortcuts)
                {
                    return sources;
                }

                try
                {
                    if (new FileInfo(file).Length > MaxFileBytes)
                    {
                        continue;
                    }

                    var (url, icon) = Parse(File.ReadAllText(file));
                    if (url is not null && !sources.ContainsKey(url))
                    {
                        sources[url] = icon is not null && Path.IsPathFullyQualified(icon) && File.Exists(icon) ? icon : file;
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                }
            }
        }

        return sources;
    }
}
