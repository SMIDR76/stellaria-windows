using System.Text.Json.Nodes;
using Stellaria.Helper.Desktop;

namespace Stellaria.Helper.Tests.Desktop;

public sealed class FrameEdgesTests
{
    private static JsonObject Pack() => (JsonObject)JsonNode.Parse("""
        {
          "widgets": [
            { "name": "bar", "presets": [ { "width": "48px", "height": "100%", "dockToEdge": { "edge": "left" } } ] },
            { "name": "edge", "presets": [
              { "name": "top", "width": "100%", "height": "10px", "dockToEdge": { "edge": "top" } },
              { "name": "right", "width": "10px", "height": "100%", "dockToEdge": { "edge": "right" } },
              { "name": "bottom", "width": "100%", "height": "10px", "dockToEdge": { "edge": "bottom" } }
            ] }
          ]
        }
        """)!;

    [Fact]
    public void Only_the_edge_thickness_changes()
    {
        var pack = Pack();

        Assert.True(FrameEdges.Apply(pack, 18));

        var edges = pack["widgets"]![1]!["presets"]!.AsArray();
        Assert.Equal("18px", edges[0]!["height"]!.GetValue<string>());
        Assert.Equal("100%", edges[0]!["width"]!.GetValue<string>());
        Assert.Equal("18px", edges[1]!["width"]!.GetValue<string>());
        Assert.Equal("18px", edges[2]!["height"]!.GetValue<string>());
        Assert.Equal("48px", pack["widgets"]![0]!["presets"]![0]!["width"]!.GetValue<string>());
    }

    [Fact]
    public void Nothing_changes_when_the_thickness_is_already_set()
    {
        Assert.False(FrameEdges.Apply(Pack(), 10));
    }

    [Theory]
    [InlineData(1, "4px")]
    [InlineData(99, "24px")]
    public void The_thickness_stays_within_bounds(int border, string expected)
    {
        var pack = Pack();

        FrameEdges.Apply(pack, border);

        Assert.Equal(expected, pack["widgets"]![1]!["presets"]![0]!["height"]!.GetValue<string>());
    }
}
