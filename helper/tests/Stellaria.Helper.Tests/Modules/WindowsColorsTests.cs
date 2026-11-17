using System.Text.Json.Nodes;
using Stellaria.Helper.Modules;
using Stellaria.Helper.Protocol;
using Stellaria.Helper.Server;

namespace Stellaria.Helper.Tests.Modules;

public sealed class WindowsColorsTests
{
    private const string Palette = """["#ffdad4","#ffb4a8","#e5897b","#c0695c","#904a3f","#73342a","#3d0800"]""";

    private static RequestContext Request(string json) => new((JsonObject)JsonNode.Parse(json)!, "bar", "");

    [Fact]
    public void A_valid_request_keeps_seven_shades_with_the_accent_in_the_middle()
    {
        var colors = WindowsColorsModule.Validate(Request($$"""{"palette":{{Palette}},"dark":true,"titleBars":false}"""));

        Assert.Equal(7, colors.Palette.Length);
        Assert.Equal(0xC0695Cu, colors.Accent);
        Assert.True(colors.Dark);
        Assert.False(colors.TitleBars);
    }

    [Theory]
    [InlineData("""{"palette":["#ffffff"],"dark":true,"titleBars":false}""")]
    [InlineData("""{"palette":["#ffdad4","#ffb4a8","#e5897b","red","#904a3f","#73342a","#3d0800"],"dark":true,"titleBars":false}""")]
    [InlineData("""{"palette":["#ffdad4","#ffb4a8","#e5897b","#c0695c","#904a3f","#73342a","#3d0800"],"titleBars":false}""")]
    [InlineData("""{"palette":"#c0695c","dark":true,"titleBars":false}""")]
    public void Malformed_requests_are_refused(string json)
    {
        var exception = Assert.Throws<HelperException>(() => WindowsColorsModule.Validate(Request(json)));

        Assert.Equal(ErrorCode.InvalidParams, exception.Code);
    }

    [Fact]
    public void Colours_are_written_in_the_registry_byte_order()
    {
        Assert.Equal(0xFF5C69C0u, WindowsColorsModule.ToAbgr(0xC0695C));
        Assert.Equal(0x00332211u, WindowsColorsModule.ToAbgr(0x112233, 0));
    }

    [Fact]
    public void The_accent_palette_holds_eight_rgba_entries_lightest_first()
    {
        var colors = WindowsColorsModule.Validate(Request($$"""{"palette":{{Palette}},"dark":false,"titleBars":true}"""));

        var bytes = WindowsColorsModule.AccentPalette(colors.Palette);

        Assert.Equal(32, bytes.Length);
        Assert.Equal(new byte[] { 0xFF, 0xDA, 0xD4, 0x00 }, bytes[..4]);
        Assert.Equal(new byte[] { 0xC0, 0x69, 0x5C, 0x00 }, bytes[12..16]);
        Assert.Equal(bytes[24..28], bytes[28..32]);
    }
}
