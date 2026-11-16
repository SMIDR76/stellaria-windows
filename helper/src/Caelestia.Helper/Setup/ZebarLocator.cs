using Microsoft.Win32;

namespace Caelestia.Helper.Setup;

internal static class ZebarLocator
{
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    public static string? Find()
    {
        foreach (var candidate in Candidates())
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private static IEnumerable<string?> Candidates()
    {
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "glzr.io", "Zebar", "zebar.exe");
        foreach (var location in InstallLocations())
        {
            yield return Path.Combine(location, "zebar.exe");
        }

        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "zebar", "current", "zebar.exe");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Zebar", "zebar.exe");
    }

    private static IEnumerable<string> InstallLocations()
    {
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            using var uninstall = hive.OpenSubKey(UninstallKey);
            if (uninstall is null)
            {
                continue;
            }

            foreach (var name in uninstall.GetSubKeyNames())
            {
                using var entry = uninstall.OpenSubKey(name);
                if (entry?.GetValue("DisplayName") is string display && display.StartsWith("Zebar", StringComparison.OrdinalIgnoreCase) &&
                    entry.GetValue("InstallLocation") is string location && location.Length > 0)
                {
                    yield return location;
                }
            }
        }
    }
}
