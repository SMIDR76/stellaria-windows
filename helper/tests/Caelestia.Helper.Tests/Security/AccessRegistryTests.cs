using System.Text.Json.Nodes;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Security;
using Caelestia.Helper.Server;
using Caelestia.Helper.Tests.TestSupport;

namespace Caelestia.Helper.Tests.Security;

public sealed class AccessRegistryTests
{
    private const string Root = @"C:\Users\someone\.glzr\zebar";

    [Fact]
    public void The_official_pack_gets_every_theme_scope_but_not_the_launcher_one()
    {
        var access = new AccessRegistry();

        var official = access.Resolve(access.OfficialToken)!;
        var launcher = access.Resolve(access.LauncherToken)!;

        Assert.All(Scopes.Theme, scope => Assert.True(official.Allows(scope)));
        Assert.False(official.Allows(Scopes.Launcher));
        Assert.True(launcher.Allows(Scopes.Launcher));
        Assert.Null(access.Resolve("not-a-token"));
    }

    [Fact]
    public void Community_packs_get_basic_plus_their_known_declared_scopes()
    {
        var packs = AccessRegistry.ParsePacks($$"""[{"id":"neon","directory":"{{Escape(Root)}}\\neon","scopes":["media","launcher","made-up"]}]""", Root);
        var access = new AccessRegistry();
        access.LoadCommunity(packs);

        var grant = access.Resolve(access.TokenOf("neon")!)!;

        Assert.True(grant.Allows(Scopes.Basic));
        Assert.True(grant.Allows(Scopes.Media));
        Assert.False(grant.Allows(Scopes.Launcher));
        Assert.False(grant.Allows(Scopes.Clipboard));
    }

    [Theory]
    [InlineData("""[{"id":"caelestia","directory":"C:\\Users\\someone\\.glzr\\zebar\\x"}]""")]
    [InlineData("""[{"id":"launcher","directory":"C:\\Users\\someone\\.glzr\\zebar\\x"}]""")]
    [InlineData("""[{"id":"Bad Id","directory":"C:\\Users\\someone\\.glzr\\zebar\\x"}]""")]
    [InlineData("""[{"id":"escape","directory":"C:\\Users\\someone\\.glzr\\zebar\\..\\..\\AppData"}]""")]
    [InlineData("""[{"id":"elsewhere","directory":"D:\\packs\\elsewhere"}]""")]
    [InlineData("""[{"id":"root","directory":"C:\\Users\\someone\\.glzr\\zebar"}]""")]
    [InlineData("not json")]
    public void Reserved_invalid_or_outside_packs_are_ignored(string text)
    {
        if (text == "not json")
        {
            Assert.ThrowsAny<System.Text.Json.JsonException>(() => AccessRegistry.ParsePacks(text, Root));
            return;
        }

        Assert.Empty(AccessRegistry.ParsePacks(text, Root));
    }

    [Fact]
    public void Reloading_revokes_the_previous_community_tokens()
    {
        var access = new AccessRegistry();
        access.LoadCommunity([new PackEntry("neon", Root + @"\neon", [])]);
        var first = access.TokenOf("neon")!;

        access.LoadCommunity([]);

        Assert.Null(access.Resolve(first));
        Assert.Null(access.TokenOf("neon"));
    }

    [Fact]
    public void Every_pack_gets_its_own_runtime_file()
    {
        using var directory = new TemporaryDirectory();
        var paths = new AppPaths(Path.Combine(directory.Path, "pack"), Path.Combine(directory.Path, "config"), Path.Combine(directory.Path, "data"));
        var access = new AccessRegistry();
        access.LoadCommunity([new PackEntry("neon", Path.Combine(directory.Path, "neon"), [])]);
        using var log = new Log(Path.Combine(directory.Path, "logs"), LogLevel.Debug);

        access.WriteRuntimeFiles(paths, 47610, 1234, log);

        Assert.Equal(access.OfficialToken, Token(paths.RuntimeFile));
        Assert.Equal(access.LauncherToken, Token(Path.Combine(paths.DataDirectory, AccessRegistry.LauncherFileName)));
        Assert.Equal(access.TokenOf("neon"), Token(Path.Combine(directory.Path, "neon", "runtime.json")));
        Assert.Equal(Messages.ProtocolVersion, JsonNode.Parse(File.ReadAllText(paths.RuntimeFile))!["protocolVersion"]!.GetValue<int>());

        access.DeleteRuntimeFiles(paths, 1234);

        Assert.False(File.Exists(Path.Combine(directory.Path, "neon", "runtime.json")));
    }

    [Fact]
    public void Every_method_of_the_helper_has_a_scope()
    {
        var methods = new[]
        {
            "apps.launch", "apps.list", "audio.sessions", "audio.setDefault", "audio.setSessionMute", "audio.setSessionVolume", "bluetooth.devices",
            "bluetooth.discover", "bluetooth.pair", "bluetooth.unpair", "capture.record", "capture.region", "clipboard.history", "clipboard.paste", "config.set",
            "desktops.switch", "display.brightness", "display.setBrightness", "folders.open", "input.paste", "keyboard.setLayout", "media.artwork",
            "media.control", "power.keepAwake", "power.run", "radios.list", "radios.set", "shell.openSettings", "shell.taskView", "system.gpu", "system.info",
            "tasks.activate", "tasks.close", "theme.publish", "wallpapers.apply", "wallpapers.list", "widgets.reload", "wifi.connect", "wifi.disconnect",
            "wifi.scan", "wifi.status", "window.act", "window.preview", "windows.restoreColors", "windows.setColors",
        };

        Assert.All(methods, method => Assert.NotNull(Scopes.ForMethod(method)));
        Assert.Equal(Scopes.Clipboard, Scopes.ForMethod("clipboard.history"));
        Assert.Equal(Scopes.Notifications, Scopes.ForTopic("notifications"));
        Assert.Equal(Scopes.Launcher, Scopes.ForMethod("catalog.install"));
        Assert.Null(Scopes.ForMethod("anything.else"));
    }

    [Fact]
    public void An_unscoped_method_cannot_be_registered()
    {
        using var directory = new TemporaryDirectory();
        using var log = new Log(directory.Path, LogLevel.Debug);
        var router = new RequestRouter(log);

        Assert.Throws<InvalidOperationException>(() => router.Register("anything.else", (_, _) => Task.FromResult<JsonNode?>(null)));
    }

    private static string Escape(string path) => path.Replace("\\", "\\\\", StringComparison.Ordinal);

    private static string Token(string file) => JsonNode.Parse(File.ReadAllText(file))!["token"]!.GetValue<string>();
}
