namespace Caelestia.Helper.Hosting;

internal sealed record AppPaths(string PackDirectory, string ConfigDirectory, string DataDirectory)
{
    public string LogDirectory => Path.Combine(DataDirectory, "logs");

    public string CacheDirectory => Path.Combine(DataDirectory, "cache");

    public string ConfigFile => Path.Combine(ConfigDirectory, "config.json");

    public string RuntimeFile => Path.Combine(PackDirectory, "runtime.json");

    public static AppPaths ForCurrentUser(string? packDirectoryOverride)
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var pack = packDirectoryOverride ?? Path.Combine(profile, ".glzr", "zebar", "caelestia");
        return new AppPaths(pack, Path.Combine(roaming, "Caelestia"), Path.Combine(local, "Caelestia"));
    }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(CacheDirectory);
    }
}
