using System.Text.Json.Nodes;
using Stellaria.Helper.Configuration;
using Stellaria.Helper.Hosting;
using Stellaria.Helper.Protocol;
using Stellaria.Helper.Tests.TestSupport;

namespace Stellaria.Helper.Tests.Configuration;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly Log _log;

    public ConfigStoreTests()
    {
        _log = new Log(Path.Combine(_directory.Path, "logs"), LogLevel.Debug);
    }

    private string ConfigPath => Path.Combine(_directory.Path, "config.json");

    public void Dispose()
    {
        _log.Dispose();
        _directory.Dispose();
    }

    private ConfigStore Load()
    {
        var store = new ConfigStore(ConfigPath, _log);
        store.Load();
        return store;
    }

    [Fact]
    public void A_missing_file_gives_every_default()
    {
        var snapshot = Load().Snapshot();

        Assert.Equal(ConfigSchema.Version, snapshot["version"]!.GetValue<int>());
        Assert.Equal("auto", snapshot["language"]!.GetValue<string>());
        Assert.True(snapshot["taskbar.hide"]!.GetValue<bool>());
        Assert.Equal(10, snapshot["idle.minutes"]!.GetValue<int>());
        Assert.Empty(snapshot["launcher.favourites"]!.AsArray());
        Assert.Null(snapshot["wallpapers.directory"]);
    }

    [Fact]
    public void Saved_values_are_read_back_after_a_restart()
    {
        Load().Set("idle.minutes", JsonValue.Create(25));

        Assert.Equal(25, Load().Get("idle.minutes")!.GetValue<int>());
    }

    [Fact]
    public void Invalid_values_in_the_file_fall_back_to_defaults_without_failing()
    {
        File.WriteAllText(ConfigPath, """{"idle.minutes":999,"language":"klingon","clock.seconds":true,"unknown":1}""");

        var store = Load();

        Assert.Equal(10, store.Get("idle.minutes")!.GetValue<int>());
        Assert.Equal("auto", store.GetString("language"));
        Assert.True(store.GetFlag("clock.seconds"));
        Assert.False(store.Snapshot().ContainsKey("unknown"));
    }

    [Fact]
    public void A_corrupted_file_gives_defaults()
    {
        File.WriteAllText(ConfigPath, "{ this is not json");

        Assert.Equal("dark", Load().GetString("theme.mode"));
    }

    [Theory]
    [InlineData("idle.minutes", "0")]
    [InlineData("idle.minutes", "\"5\"")]
    [InlineData("taskbar.hide", "1")]
    [InlineData("language", "\"de\"")]
    [InlineData("theme.scheme", """{"name":"Nord!","flavour":"default"}""")]
    [InlineData("weather.location", """{"name":"Paris","latitude":91,"longitude":2}""")]
    [InlineData("launcher.favourites", "[\"bad\\u0000id\"]")]
    [InlineData("wallpapers.directory", "\"relative\\\\folder\"")]
    [InlineData("wallpapers.directory", "\"C:\\\\definitely\\\\missing\\\\folder\"")]
    public void Invalid_values_are_refused(string optionPath, string json)
    {
        var exception = Assert.Throws<HelperException>(() => Load().Set(optionPath, JsonNode.Parse(json)));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }

    [Fact]
    public void Unknown_settings_are_refused()
    {
        var exception = Assert.Throws<HelperException>(() => Load().Set("shell.command", JsonValue.Create("calc.exe")));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }

    [Fact]
    public void Valid_structured_values_are_accepted()
    {
        var store = Load();

        store.Set("theme.scheme", JsonNode.Parse("""{"name":"catppuccin","flavour":"mocha"}"""));
        store.Set("weather.location", JsonNode.Parse("""{"name":"Tunceli","latitude":39.1,"longitude":39.5}"""));
        store.Set("launcher.favourites", JsonNode.Parse("""["Microsoft.WindowsCalculator_8wekyb3d8bbwe!App","C:\\Program Files (x86)\\Steam\\steam.exe"]"""));
        store.Set("wallpapers.directory", JsonValue.Create(_directory.Path));

        Assert.Equal("mocha", store.Get("theme.scheme")!["flavour"]!.GetValue<string>());
        Assert.Equal(2, store.Get("launcher.favourites")!.AsArray().Count);
        Assert.Equal(_directory.Path, store.GetString("wallpapers.directory"));
    }

    [Fact]
    public void Changes_are_announced_with_the_full_configuration()
    {
        var store = Load();
        JsonObject? announced = null;
        store.Changed += snapshot => announced = snapshot;

        store.Set("media.lyrics", JsonValue.Create(false));

        Assert.False(announced!["media.lyrics"]!.GetValue<bool>());
        Assert.True(announced["weather.enabled"]!.GetValue<bool>());
    }
}
