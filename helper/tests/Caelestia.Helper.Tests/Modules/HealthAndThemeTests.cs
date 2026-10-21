using System.Text.Json.Nodes;
using Caelestia.Helper.Modules;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Server;

namespace Caelestia.Helper.Tests.Modules;

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
}
