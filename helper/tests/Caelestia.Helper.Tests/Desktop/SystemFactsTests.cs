using Caelestia.Helper.Desktop;
using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Tests.Desktop;

public sealed class SystemFactsTests
{
    [Theory]
    [InlineData("Windows 10 Pro", 26300, "25H2", "Windows 11 Pro 25H2")]
    [InlineData("Windows 10 Pro", 19045, "22H2", "Windows 10 Pro 22H2")]
    [InlineData("Windows 11 Home", 22631, null, "Windows 11 Home")]
    public void Windows_11_is_named_from_the_build_number(string product, int build, string? version, string expected)
    {
        Assert.Equal(expected, SystemFacts.FormatOperatingSystem(product, build, version));
    }

    [Fact]
    public void Processor_names_lose_their_padding()
    {
        Assert.Equal("12th Gen Intel(R) Core(TM) i7-12700KF", SystemFacts.CollapseSpaces("  12th Gen Intel(R)   Core(TM) i7-12700KF  "));
    }

    [Theory]
    [InlineData("NVIDIA GeForce RTX 4060 Ti, 37, 52\r\n", "NVIDIA GeForce RTX 4060 Ti", 37, 52)]
    [InlineData("NVIDIA GeForce RTX 5080, 0, 41", "NVIDIA GeForce RTX 5080", 0, 41)]
    public void Nvidia_smi_output_is_parsed(string output, string name, double usage, double temperature)
    {
        Assert.Equal(new NvidiaReading(name, usage, temperature), NvidiaSmi.Parse(output));
    }

    [Theory]
    [InlineData("")]
    [InlineData("NVIDIA-SMI has failed because it couldn't communicate with the NVIDIA driver.")]
    [InlineData("RTX, [N/A], 40")]
    public void Unexpected_nvidia_smi_output_is_ignored(string output)
    {
        Assert.Null(NvidiaSmi.Parse(output));
    }

    [Fact]
    public unsafe void Gpu_usage_sums_valid_engines_and_caps_at_one_hundred()
    {
        var items = stackalloc PdhCounterItem[3];
        items[0].Value = new PdhCounterValue { Status = GpuCounters.ValidData, Value = 70 };
        items[1].Value = new PdhCounterValue { Status = 0xC0000BC6, Value = 500 };
        items[2].Value = new PdhCounterValue { Status = GpuCounters.NewData, Value = 45 };

        Assert.Equal(100, GpuCounters.Sum(items, 3));
        Assert.Equal(70, GpuCounters.Sum(items, 1));
    }

    [Fact]
    public void The_machine_reports_its_processor_graphics_and_system()
    {
        Assert.False(string.IsNullOrEmpty(SystemFacts.Processor()));
        Assert.False(string.IsNullOrEmpty(SystemFacts.PrimaryGraphicsAdapter()));
        Assert.StartsWith("Windows", SystemFacts.OperatingSystem(), StringComparison.Ordinal);
    }

    [Fact]
    public void Gpu_counters_open_and_return_a_percentage()
    {
        using var counters = new GpuCounters();
        Assert.SkipUnless(counters.Available, "GPU performance counters are not available on this machine.");

        Thread.Sleep(200);
        var usage = counters.ThreeDUsage();

        Assert.NotNull(usage);
        Assert.InRange(usage.Value, 0, 100);
    }
}
