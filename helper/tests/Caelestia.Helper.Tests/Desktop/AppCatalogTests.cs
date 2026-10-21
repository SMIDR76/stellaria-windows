using Caelestia.Helper.Desktop;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Tests.Desktop;

public sealed class AppCatalogTests
{
    [Theory]
    [InlineData("Uninstall Steam", @"{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}\Steam\uninstall.exe", false)]
    [InlineData("Lisezmoi", @"C:\Program Files\App\readme.txt", false)]
    [InlineData("Steam Support Center", @"https://help.steampowered.com.url", false)]
    [InlineData("Calculatrice", "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", true)]
    [InlineData("Helper", @"C:\Tools\helper.exe", true)]
    public void Uninstallers_and_documents_are_not_offered(string name, string id, bool launchable)
    {
        Assert.Equal(launchable, AppCatalog.IsLaunchable(new ShellApp(name, id)));
    }

    [Fact]
    public void Duplicates_are_removed_and_names_sorted()
    {
        var apps = AppCatalog.Filter([new ShellApp("Zen", "zen"), new ShellApp("code", "vscode"), new ShellApp("Code copy", "vscode")]);

        Assert.Equal(["code", "Zen"], apps.Select(app => app.Name));
    }

    [Fact]
    public void The_start_menu_lists_built_in_applications()
    {
        var apps = ShellFolders.StartMenuApps();

        Assert.NotEmpty(apps);
        Assert.All(apps, app => Assert.False(string.IsNullOrWhiteSpace(app.Id)));
    }
}
