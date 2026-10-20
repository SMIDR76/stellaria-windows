using Caelestia.Helper.Hosting;
using Caelestia.Helper.Tests.TestSupport;

namespace Caelestia.Helper.Tests.Hosting;

public sealed class LogTests
{
    [Fact]
    public void Lines_contain_level_source_and_message()
    {
        var line = Log.Format(new DateTimeOffset(2026, 10, 21, 9, 5, 7, 42, TimeSpan.FromHours(2)), LogLevel.Warning, "server", "Rejected", null);

        Assert.Equal("2026-10-21 09:05:07.042 +02:00 WARNING [server] Rejected", line);
    }

    [Fact]
    public void Messages_below_the_minimum_level_are_skipped()
    {
        using var directory = new TemporaryDirectory();
        using (var log = new Log(directory.Path, LogLevel.Info))
        {
            log.Debug("test", "hidden");
            log.Info("test", "shown");
        }

        var content = File.ReadAllText(Path.Combine(directory.Path, "helper.log"));
        Assert.DoesNotContain("hidden", content, StringComparison.Ordinal);
        Assert.Contains("shown", content, StringComparison.Ordinal);
    }

    [Fact]
    public void A_full_log_is_rotated_into_numbered_archives()
    {
        using var directory = new TemporaryDirectory();
        var current = Path.Combine(directory.Path, "helper.log");
        File.WriteAllText(current, new string('x', (int)Log.MaxFileBytes));

        using (var log = new Log(directory.Path, LogLevel.Info))
        {
            log.Info("test", "after rotation");
        }

        Assert.True(File.Exists($"{current}.1"));
        Assert.Contains("after rotation", File.ReadAllText(current), StringComparison.Ordinal);
    }
}
