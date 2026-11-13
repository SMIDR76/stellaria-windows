using Caelestia.Helper.Desktop;

namespace Caelestia.Helper.Tests.Desktop;

public sealed class SpectrumTests
{
    private const int SampleRate = 48000;
    private const int Size = 2048;

    private static float[] Sine(double frequency, double amplitude = 0.5) =>
        Enumerable.Range(0, Size).Select(index => (float)(amplitude * Math.Sin(2 * Math.PI * frequency * index / SampleRate))).ToArray();

    [Fact]
    public void The_fft_puts_a_pure_tone_in_its_bin()
    {
        var bin = 64;
        var real = Enumerable.Range(0, Size).Select(index => Math.Sin(2 * Math.PI * bin * index / Size)).ToArray();
        var imaginary = new double[Size];

        Spectrum.Fft(real, imaginary);

        var magnitudes = Enumerable.Range(0, Size / 2).Select(index => Math.Sqrt((real[index] * real[index]) + (imaginary[index] * imaginary[index]))).ToArray();
        Assert.Equal(bin, Array.IndexOf(magnitudes, magnitudes.Max()));
    }

    [Fact]
    public void Bands_cover_increasing_non_empty_ranges()
    {
        var bands = Spectrum.BandBins(Size, 45, SampleRate);

        Assert.Equal(45, bands.Length);
        Assert.All(bands, band => Assert.True(band.End > band.Start));
        Assert.All(bands.Zip(bands.Skip(1)), pair => Assert.True(pair.Second.Start >= pair.First.Start));
        Assert.True(bands[^1].End <= Size / 2);
    }

    [Fact]
    public void Silence_gives_empty_bars()
    {
        var spectrum = new Spectrum(Size, 45, SampleRate);

        Assert.All(spectrum.Process(new float[Size]), level => Assert.Equal(0, level));
    }

    [Fact]
    public void A_low_tone_lights_the_low_bands_more_than_the_high_ones()
    {
        var spectrum = new Spectrum(Size, 45, SampleRate);

        var levels = spectrum.Process(Sine(100));

        Assert.True(levels.Take(10).Max() > 50);
        Assert.True(levels.Skip(35).Max() < levels.Take(10).Max());
    }

    [Fact]
    public void Bars_fall_slowly_after_the_sound_stops()
    {
        var spectrum = new Spectrum(Size, 45, SampleRate);
        var loud = spectrum.Process(Sine(1000)).Max();

        var after = spectrum.Process(new float[Size]).Max();

        Assert.InRange(after, 1, loud - 1);
    }
}
