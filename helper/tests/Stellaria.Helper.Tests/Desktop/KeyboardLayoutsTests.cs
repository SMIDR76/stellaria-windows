using Stellaria.Helper.Desktop;
using Stellaria.Helper.Modules;

namespace Stellaria.Helper.Tests.Desktop;

public sealed class KeyboardLayoutsTests
{
    [Fact]
    public void A_layout_handle_splits_into_language_and_device()
    {
        Assert.Equal((0x040C, 0x040C), KeyboardLayouts.Split(0x040C040C));
        Assert.Equal((0x041F, 0xF00A), KeyboardLayouts.Split(unchecked((long)0xFFFFFFFFF00A041F)));
    }

    [Fact]
    public void Handles_are_formatted_as_uppercase_hexadecimal()
    {
        Assert.Equal("40C040C", KeyboardLayouts.Format(0x040C040C));
    }

    [Theory]
    [InlineData(0x040C, 0x040C, "0000040C")]
    [InlineData(0x0409, 0x0000, "00000409")]
    public void Ordinary_layouts_map_to_their_registry_key(int language, int device, string key)
    {
        Assert.Equal(key, KeyboardLayouts.DirectLayoutKey(language, device));
    }

    [Fact]
    public void Layouts_with_a_layout_id_need_a_registry_search()
    {
        Assert.Null(KeyboardLayouts.DirectLayoutKey(0x041F, 0xF00A));
    }

    [Theory]
    [InlineData(0x040C, "fr-FR")]
    [InlineData(0x041F, "tr-TR")]
    [InlineData(0x0401, "ar-SA")]
    public void Language_ids_become_standard_tags(int language, string tag)
    {
        Assert.Equal(tag, KeyboardLayouts.LanguageTag(language));
    }

    [Theory]
    [InlineData(0x0001, true)]
    [InlineData(unchecked((short)0xFF81), true)]
    [InlineData(unchecked((short)0xFF80), false)]
    [InlineData(0x0000, false)]
    public void Only_the_low_bit_of_the_key_state_means_toggled(short state, bool toggled)
    {
        Assert.Equal(toggled, KeyboardModule.IsToggled(state));
    }

    [Fact]
    public void The_installed_layouts_include_the_current_one()
    {
        var installed = KeyboardLayouts.Installed();

        Assert.NotEmpty(installed);
        Assert.All(installed, layout => Assert.False(string.IsNullOrEmpty(layout.Name)));
    }
}
