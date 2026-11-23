using System.Text.Json.Nodes;
using Stellaria.Helper.Modules;
using Stellaria.Helper.Protocol;
using Stellaria.Helper.Server;

namespace Stellaria.Helper.Tests.Modules;

public sealed class HealthAndThemeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 21, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_visible_widget_without_frames_for_more_than_ten_seconds_is_stalled()
    {
        Assert.True(HealthModule.IsStalled(true, true, s_now.AddSeconds(-11), s_now));
        Assert.False(HealthModule.IsStalled(true, true, s_now.AddSeconds(-9), s_now));
    }

    [Fact]
    public void Hidden_or_unauthenticated_widgets_are_never_stalled()
    {
        Assert.False(HealthModule.IsStalled(true, false, s_now.AddMinutes(-5), s_now));
        Assert.False(HealthModule.IsStalled(false, true, s_now.AddMinutes(-5), s_now));
    }

    private static RequestContext Request(string json) => new((JsonObject)JsonNode.Parse(json)!, "bar", "");

    [Fact]
    public void A_valid_theme_is_normalised_to_lowercase()
    {
        var theme = ThemeModule.Validate(Request("""{"roles":{"primary":"#ABCDEF","surface":"#101010"},"transparent":false}"""));

        Assert.Equal("#abcdef", theme["roles"]!["primary"]!.GetValue<string>());
        Assert.False(theme["transparent"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData("""{"roles":{},"transparent":false}""")]
    [InlineData("""{"roles":{"primary":"red"},"transparent":false}""")]
    [InlineData("""{"roles":{"pri mary":"#ffffff"},"transparent":false}""")]
    [InlineData("""{"roles":{"primary":"#ffffff\"; rm -rf"},"transparent":false}""")]
    [InlineData("""{"roles":{"primary":"#ffffff"}}""")]
    public void Malformed_themes_are_refused(string json)
    {
        var exception = Assert.Throws<HelperException>(() => ThemeModule.Validate(Request(json)));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }

    [Fact]
    public void The_app_can_change_whitelisted_options_within_their_range()
    {
        Assert.Equal(16, ThemeModule.ValidateSetting("frameBorder", JsonValue.Create(16))!.GetValue<int>());
        Assert.True(ThemeModule.ValidateSetting("doNotDisturb", JsonValue.Create(true))!.GetValue<bool>());
    }

    [Theory]
    [InlineData("frameBorder", "30")]
    [InlineData("frameRounding", "true")]
    [InlineData("activeWindow", "1")]
    [InlineData("welcomed", "false")]
    [InlineData("catalogUrl", "\"https://example.org/index.json\"")]
    public void Unknown_options_or_values_out_of_range_are_refused(string name, string json)
    {
        var exception = Assert.Throws<HelperException>(() => ThemeModule.ValidateSetting(name, JsonNode.Parse(json)));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }

    [Fact]
    public void Reported_options_only_carry_flags_and_integers()
    {
        Assert.Equal(2, ThemeModule.ValidateOptions(JsonNode.Parse("""{"island":true,"frameBorder":12}""")).Count);
        Assert.Equal("fr", ThemeModule.ValidateOptions(JsonNode.Parse("""{"language":"fr"}"""))["language"]!.GetValue<string>());
        Assert.Throws<HelperException>(() => ThemeModule.ValidateOptions(new JsonObject { ["wallpaperDirectory"] = new string('x', 600) }));
        Assert.Single(ThemeModule.ValidateOptions(JsonNode.Parse("""{"hiddenApps":[{"id":"steam://rungameid/1","name":"Game"}]}""")));
        Assert.Throws<HelperException>(() => ThemeModule.ValidateOptions(JsonNode.Parse("""{"folders":[1, 2]}""")));
        Assert.Throws<HelperException>(() => ThemeModule.ValidateOptions(new JsonObject { ["barOrder"] = new JsonArray(new string('x', 40)) }));
    }

    [Theory]
    [InlineData("mode", "\"light\"")]
    [InlineData("variant", "\"expressive\"")]
    [InlineData("scheme", "\"catppuccin/mocha\"")]
    [InlineData("scheme", "\"\"")]
    [InlineData("wallpaperDirectory", "\"D:\\\\Wallpapers\"")]
    [InlineData("wallpaperDirectory", "\"\"")]
    [InlineData("hiddenApps", "[\"steam://rungameid/1\"]")]
    [InlineData("idleMinutes", "30")]
    [InlineData("barOrder", "[\"power\",\"logo\",\"workspaces\",\"tasks\",\"activeWindow\",\"spacer\",\"tray\",\"weather\",\"clock\",\"status\"]")]
    [InlineData("barHidden", "[\"weather\",\"tasks\"]")]
    [InlineData("statusOrder", "[\"battery\",\"keyboard\",\"capsLock\",\"mic\",\"volume\",\"bluetooth\",\"network\",\"notifications\"]")]
    [InlineData("statusHidden", "[]")]
    public void The_app_can_change_the_look_and_the_lists(string name, string json)
    {
        Assert.NotNull(ThemeModule.ValidateSetting(name, JsonNode.Parse(json)));
    }

    [Theory]
    [InlineData("mode", "\"blue\"")]
    [InlineData("variant", "\"neon\"")]
    [InlineData("scheme", "\"../../evil\"")]
    [InlineData("wallpaperDirectory", "\"relative\\\\folder\"")]
    [InlineData("wallpaperDirectory", "\"C:\\\\a\\\\..\\\\Windows\"")]
    [InlineData("hiddenApps", "\"steam://rungameid/1\"")]
    [InlineData("idleMinutes", "0")]
    [InlineData("barOrder", "[\"logo\",\"spacer\"]")]
    [InlineData("barOrder", "[\"logo\",\"logo\",\"workspaces\",\"tasks\",\"activeWindow\",\"spacer\",\"tray\",\"weather\",\"clock\",\"status\"]")]
    [InlineData("barHidden", "[\"spacer\"]")]
    [InlineData("statusHidden", "[\"wifi\"]")]
    public void Wrong_looks_and_lists_are_refused(string name, string json)
    {
        Assert.Throws<HelperException>(() => ThemeModule.ValidateSetting(name, JsonNode.Parse(json)));
    }

    [Fact]
    public void The_app_can_only_pick_a_supported_theme_language()
    {
        Assert.Equal("pt-BR", ThemeModule.ValidateSetting("language", JsonValue.Create("pt-BR"))!.GetValue<string>());
        Assert.Throws<HelperException>(() => ThemeModule.ValidateSetting("language", JsonValue.Create("xx")));
        Assert.Throws<HelperException>(() => ThemeModule.ValidateSetting("language", JsonValue.Create(true)));
    }
}
