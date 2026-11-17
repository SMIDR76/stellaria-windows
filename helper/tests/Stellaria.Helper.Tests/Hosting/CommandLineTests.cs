using Stellaria.Helper.Hosting;

namespace Stellaria.Helper.Tests.Hosting;

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
        var options = CommandLine.Parse(["--verbose", "--pack", @"C:\packs\stellaria"]);

        Assert.True(options.Verbose);
        Assert.Equal(@"C:\packs\stellaria", options.PackDirectory);
    }

    [Fact]
    public void The_recovery_command_is_recognised()
    {
        Assert.True(CommandLine.Parse(["--restore-shell"]).RestoreShell);
    }

    [Fact]
    public void Unknown_arguments_are_refused()
    {
        Assert.Throws<ArgumentException>(() => CommandLine.Parse(["--fly"]));
    }
}
