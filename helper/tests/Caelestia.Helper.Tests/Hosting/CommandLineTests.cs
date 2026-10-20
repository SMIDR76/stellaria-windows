using Caelestia.Helper.Hosting;

namespace Caelestia.Helper.Tests.Hosting;

public sealed class CommandLineTests
{
    [Fact]
    public void No_argument_starts_the_helper_with_defaults()
    {
        var options = CommandLine.Parse([]);

        Assert.Equal(new CommandLine(false, false, null), options);
    }

    [Fact]
    public void Flags_and_pack_directory_are_read()
    {
        var options = CommandLine.Parse(["--verbose", "--pack", @"C:\packs\caelestia"]);

        Assert.True(options.Verbose);
        Assert.Equal(@"C:\packs\caelestia", options.PackDirectory);
    }

    [Fact]
    public void Unknown_arguments_are_refused()
    {
        Assert.Throws<ArgumentException>(() => CommandLine.Parse(["--fly"]));
    }
}
