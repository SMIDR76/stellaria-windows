using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Stellaria.Helper.Hosting;
using Stellaria.Helper.Setup;
using Stellaria.Helper.Tests.TestSupport;

namespace Stellaria.Helper.Tests.Setup;

public sealed class SetupTests
{
    private static readonly StartupWidget[] s_widgets = [new("bar", "default"), new("edge", "top")];

    [Theory]
    [InlineData("--install", "install")]
    [InlineData("--uninstall", "uninstall")]
    [InlineData("--shutdown", "shutdown")]
    [InlineData("--start", "start")]
    public void Setup_verbs_are_recognised(string argument, string verb)
    {
        Assert.Equal(verb, CommandLine.Parse([argument]).Setup);
    }

    [Fact]
    public void Install_paths_can_be_given()
    {
        var options = CommandLine.Parse(["--install", "--zebar", @"C:\Zebar\zebar.exe", "--switcher", @"C:\Pack\switcher.exe"]);

        Assert.Equal(@"C:\Zebar\zebar.exe", options.Zebar);
        Assert.Equal(@"C:\Pack\switcher.exe", options.Switcher);
    }

    [Fact]
    public void Two_setup_verbs_are_refused()
    {
        Assert.Throws<ArgumentException>(() => CommandLine.Parse(["--install", "--uninstall"]));
    }

    [Fact]
    public void Merging_replaces_only_the_stellaria_entries_and_keeps_other_settings()
    {
        const string existing = """
            {"$schema":"x","startupConfigs":[{"pack":"other","widget":"a","preset":"p"},{"pack":"stellaria","widget":"old","preset":"default"}],"keep":true}
            """;

        var merged = JsonNode.Parse(ZebarSettings.Merge(existing, s_widgets))!.AsObject();
        var startup = merged["startupConfigs"]!.AsArray();

        Assert.Equal(3, startup.Count);
        Assert.Equal("other", startup[0]!["pack"]!.GetValue<string>());
        Assert.Equal("bar", startup[1]!["widget"]!.GetValue<string>());
        Assert.Equal("top", startup[2]!["preset"]!.GetValue<string>());
        Assert.True(merged["keep"]!.GetValue<bool>());
    }

    [Fact]
    public void Merging_accepts_a_missing_or_bom_prefixed_file()
    {
        Assert.Equal(2, JsonNode.Parse(ZebarSettings.Merge(null, s_widgets))!["startupConfigs"]!.AsArray().Count);
        Assert.Equal(2, JsonNode.Parse(ZebarSettings.Merge("\uFEFF{}", s_widgets))!["startupConfigs"]!.AsArray().Count);
    }

    [Fact]
    public void Unmerging_removes_every_stellaria_entry()
    {
        var merged = ZebarSettings.Merge("""{"startupConfigs":[{"pack":"other","widget":"a","preset":"p"}]}""", s_widgets);

        var startup = JsonNode.Parse(ZebarSettings.Unmerge(merged))!["startupConfigs"]!.AsArray();

        Assert.Single(startup);
        Assert.Equal("other", startup[0]!["pack"]!.GetValue<string>());
    }

    [Fact]
    public void Settings_are_written_without_byte_order_mark_and_backed_up()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "settings.json");
        File.WriteAllText(path, "{\"old\":true}");

        ZebarSettings.Write(path, "{}\n");

        var bytes = File.ReadAllBytes(path);
        Assert.Equal((byte)'{', bytes[0]);
        Assert.Equal("{\"old\":true}", File.ReadAllText(path + ZebarSettings.BackupSuffix));
    }

    [Fact]
    public void The_startup_list_is_read_from_the_pack()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "startup.json"), """[{"widget":"bar"},{"widget":"edge","preset":"top"}]""", Encoding.UTF8);

        var widgets = ZebarSettings.ReadStartupList(directory.Path);

        Assert.Equal([new StartupWidget("bar", "default"), new StartupWidget("edge", "top")], widgets);
    }

    [Fact]
    public void The_shipped_startup_list_matches_the_pack()
    {
        var pack = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "pack"));
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(pack, "zpack.json")))!["widgets"]!.AsArray();
        var presets = manifest.SelectMany(widget => widget!["presets"]!.AsArray().Select(preset => new StartupWidget(widget["name"]!.GetValue<string>(), preset!["name"]!.GetValue<string>()))).ToHashSet();

        var widgets = ZebarSettings.ReadStartupList(pack);

        Assert.NotEmpty(widgets);
        Assert.All(widgets, widget => Assert.Contains(widget, presets));
    }

    [Fact]
    public void Logon_tasks_run_for_the_current_user_without_elevation()
    {
        var xml = ScheduledTasks.Xml(new LogonTask("Stellaria - Helper", @"C:\Users\A & B\.stellaria\bin\stellaria-helper.exe", @"C:\Users\A & B\.stellaria\bin", TimeSpan.FromSeconds(3)), @"PC\A & B");

        var task = XDocument.Parse(xml);
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        Assert.Equal("LeastPrivilege", task.Descendants(ns + "RunLevel").Single().Value);
        Assert.Equal(@"PC\A & B", task.Descendants(ns + "LogonTrigger").Single().Element(ns + "UserId")!.Value);
        Assert.Equal("PT3S", task.Descendants(ns + "Delay").Single().Value);
        Assert.Equal(@"C:\Users\A & B\.stellaria\bin\stellaria-helper.exe", task.Descendants(ns + "Command").Single().Value);
    }
}
