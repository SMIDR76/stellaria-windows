using Stellaria.Helper.Desktop;

namespace Stellaria.Helper.Tests.Desktop;

public sealed class InternetShortcutsTests
{
    [Fact]
    public void A_steam_shortcut_gives_its_url_and_icon_file()
    {
        var (url, icon) = InternetShortcuts.Parse("[{000214A0-0000-0000-C000-000000000046}]\r\nProp3=19,0\r\n[InternetShortcut]\r\nIDList=\r\nIconIndex=0\r\nURL=steam://rungameid/1874880\r\nIconFile=C:\\Program Files (x86)\\Steam\\steam\\games\\f2e5.ico\r\n");

        Assert.Equal("steam://rungameid/1874880", url);
        Assert.Equal("C:\\Program Files (x86)\\Steam\\steam\\games\\f2e5.ico", icon);
    }

    [Fact]
    public void Keys_outside_the_shortcut_section_are_ignored()
    {
        var (url, icon) = InternetShortcuts.Parse("[Other]\nURL=https://wrong.example\nIconFile=C:\\wrong.ico\n[InternetShortcut]\nURL=https://gitforwindows.org/faq\n");

        Assert.Equal("https://gitforwindows.org/faq", url);
        Assert.Null(icon);
    }

    [Fact]
    public void A_shortcut_without_an_icon_falls_back_to_the_file_itself()
    {
        var folder = Directory.CreateTempSubdirectory("stellaria-url-");
        try
        {
            var withIcon = Path.Combine(folder.FullName, "Game.url");
            var icon = Path.Combine(folder.FullName, "game.ico");
            File.WriteAllBytes(icon, [0, 0, 1, 0]);
            File.WriteAllText(withIcon, $"[InternetShortcut]\nURL=steam://rungameid/1\nIconFile={icon}\n");
            var withoutIcon = Path.Combine(folder.FullName, "Site.url");
            File.WriteAllText(withoutIcon, "[InternetShortcut]\nURL=https://example.org/\nIconFile=C:\\missing\\nothing.ico\n");

            var sources = InternetShortcuts.IconSources([folder.FullName]);

            Assert.Equal(icon, sources["steam://rungameid/1"]);
            Assert.Equal(withoutIcon, sources["https://example.org/"]);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
