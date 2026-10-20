using Caelestia.Helper.Hosting;
using Caelestia.Helper.Tests.TestSupport;

namespace Caelestia.Helper.Tests.Hosting;

public sealed class HelperStateTests
{
    [Fact]
    public void The_original_taskbar_setting_survives_a_restart()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "state.json");

        new HelperState(path).OriginalTaskbarAutoHide = false;

        Assert.False(new HelperState(path).OriginalTaskbarAutoHide);
    }

    [Fact]
    public void Clearing_the_setting_forgets_it()
    {
        using var directory = new TemporaryDirectory();
        var state = new HelperState(Path.Combine(directory.Path, "state.json")) { OriginalTaskbarAutoHide = true };

        state.OriginalTaskbarAutoHide = null;

        Assert.Null(state.OriginalTaskbarAutoHide);
    }

    [Fact]
    public void A_corrupted_file_reads_as_no_saved_setting()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "state.json");
        File.WriteAllText(path, "{not json");

        Assert.Null(new HelperState(path).OriginalTaskbarAutoHide);
    }
}
