using Stellaria.Helper.Desktop;
using Stellaria.Helper.Modules;

namespace Stellaria.Helper.Tests.Desktop;

public sealed class SpectrumAnalyserTests
{
    private const int SampleRate = 48000;

    private static int[] Feed(Func<int, float> wave, int bars = 48)
    {
        var analyser = new SpectrumAnalyser(bars, SampleRate);
        for (var index = 0; index < SpectrumAnalyser.FftSize * 2; index++)
        {
            analyser.Add(wave(index));
        }

        return analyser.Bars();
    }

    private static int BarFor(double frequency, int bars = 48) =>
        (int)Math.Floor(Math.Log(frequency / SpectrumAnalyser.LowestFrequency) / Math.Log(SpectrumAnalyser.HighestFrequency / SpectrumAnalyser.LowestFrequency) * bars);

    [Fact]
    public void Silence_draws_no_bar()
    {
        var bars = Feed(_ => 0f);

        Assert.True(VisualiserModule.IsSilent(bars));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(8000)]
    public void A_tone_lights_the_bar_of_its_frequency(double frequency)
    {
        var bars = Feed(index => (float)(0.5 * Math.Sin(2 * Math.PI * frequency * index / SampleRate)));
        var loudest = Array.IndexOf(bars, bars.Max());

        Assert.InRange(loudest, BarFor(frequency) - 1, BarFor(frequency) + 1);
        Assert.True(bars.Max() > 60);
        Assert.True(bars[0] < bars.Max() / 2 || BarFor(frequency) <= 1);
    }

    [Fact]
    public void Bars_fall_back_smoothly_when_the_sound_stops()
    {
        var analyser = new SpectrumAnalyser(48, SampleRate);
        for (var index = 0; index < SpectrumAnalyser.FftSize; index++)
        {
            analyser.Add((float)(0.5 * Math.Sin(2 * Math.PI * 1000 * index / SampleRate)));
        }

        var loud = analyser.Bars().Max();
        for (var index = 0; index < SpectrumAnalyser.FftSize; index++)
        {
            analyser.Add(0);
        }

        var falling = analyser.Bars().Max();

        Assert.InRange(falling, loud - 7, loud - 5);
    }
}
