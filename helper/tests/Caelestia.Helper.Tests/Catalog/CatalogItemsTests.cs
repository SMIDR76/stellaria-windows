using System.Text.Json.Nodes;
using Caelestia.Helper.Catalog;

namespace Caelestia.Helper.Tests.Catalog;

public sealed class CatalogItemsTests
{
    private const string Preset = """
        {"id":"preset-test","type":"preset","version":"1.0.0","license":"MIT","author":{"name":"A"},
         "name":{"fr":"Test","en":"Test"},"summary":{"fr":"Un test","en":"A test"},
         "preset":{"scheme":{"name":"nord","flavour":"medium"},"mode":"dark","variant":"vibrant","options":{"tintIcons":false}}}
        """;

    [Fact]
    public void A_complete_preset_is_valid()
    {
        Assert.Empty(CatalogItems.Validate(Parse(Preset)));
    }

    [Theory]
    [InlineData("\"id\":\"preset-test\"", "\"id\":\"Preset Test\"")]
    [InlineData("\"license\":\"MIT\"", "\"license\":\"Proprietary\"")]
    [InlineData("\"version\":\"1.0.0\"", "\"version\":\"one\"")]
    [InlineData("\"mode\":\"dark\"", "\"mode\":\"neon\"")]
    [InlineData("\"options\":{\"tintIcons\":false}", "\"options\":{\"transparency\":true}")]
    [InlineData("\"options\":{\"tintIcons\":false}", "\"script\":\"alert(1)\"")]
    [InlineData("\"en\":\"A test\"", "\"de\":\"Ein Test\"")]
    public void Unsafe_or_incomplete_presets_are_refused(string original, string replacement)
    {
        Assert.NotEmpty(CatalogItems.Validate(Parse(Preset.Replace(original, replacement, StringComparison.Ordinal))));
    }

    [Fact]
    public void Packs_need_a_github_artifact_with_a_hash_and_theme_scopes_only()
    {
        var pack = Parse(Preset.Replace("\"type\":\"preset\"", "\"type\":\"pack\"", StringComparison.Ordinal));
        pack.Remove("preset");
        pack["scopes"] = new JsonArray("media", "launcher");
        pack["artifact"] = new JsonObject { ["url"] = "https://evil.example/pack.zip", ["sha256"] = "abc" };

        var errors = CatalogItems.Validate(pack);

        Assert.Contains(errors, error => error.Contains("artifact", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("scopes", StringComparison.Ordinal));
    }

    [Fact]
    public void The_official_registry_entries_are_valid()
    {
        var registry = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "registry", "items"));
        var files = Directory.GetFiles(registry, "*.json");

        Assert.NotEmpty(files);
        Assert.All(files, file => Assert.Empty(CatalogItems.Validate(Parse(File.ReadAllText(file)))));
    }

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
}
