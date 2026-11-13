namespace Caelestia.Helper.Desktop;

/// <summary>
/// Turns audio samples into the bar heights of the visualiser: a Hann windowed FFT folded into logarithmic bands,
/// with fast rise and slow fall so the bars move like Caelestia's.
/// </summary>
internal sealed class Spectrum
{
    public const double MinimumFrequency = 40;
    public const double MaximumFrequency = 16000;
    public const double FloorDecibels = -70;
    public const double Fall = 0.82;

    private readonly int _size;
    private readonly double[] _window;
    private readonly (int Start, int End)[] _bands;
    private readonly double[] _smoothed;

    public Spectrum(int size, int bandCount, int sampleRate)
    {
        if (size < 2 || (size & (size - 1)) != 0)
        {
            throw new ArgumentException("The FFT size must be a power of two.", nameof(size));
        }

        _size = size;
        _window = new double[size];
        for (var index = 0; index < size; index++)
        {
            _window[index] = 0.5 * (1 - Math.Cos(2 * Math.PI * index / (size - 1)));
        }

        _bands = BandBins(size, bandCount, sampleRate);
        _smoothed = new double[bandCount];
    }

    public int Size => _size;

    /// <summary>Bin ranges [Start, End) of logarithmically spaced bands between 40 Hz and 16 kHz.</summary>
    public static (int Start, int End)[] BandBins(int size, int bandCount, int sampleRate)
    {
        var bands = new (int, int)[bandCount];
        var resolution = (double)sampleRate / size;
        var top = Math.Min(MaximumFrequency, sampleRate / 2.0);
        var previous = Math.Max(1, (int)(MinimumFrequency / resolution));
        for (var band = 0; band < bandCount; band++)
        {
            var edge = MinimumFrequency * Math.Pow(top / MinimumFrequency, (band + 1.0) / bandCount);
            var end = Math.Max(previous + 1, Math.Min(size / 2, (int)Math.Round(edge / resolution)));
            bands[band] = (previous, end);
            previous = Math.Min(end, (size / 2) - 1);
        }

        return bands;
    }

    /// <summary>Returns the band levels between 0 and 100 for the given samples (exactly Size of them).</summary>
    public int[] Process(ReadOnlySpan<float> samples)
    {
        var real = new double[_size];
        var imaginary = new double[_size];
        for (var index = 0; index < _size; index++)
        {
            real[index] = samples[index] * _window[index];
        }

        Fft(real, imaginary);
        var levels = new int[_bands.Length];
        for (var band = 0; band < _bands.Length; band++)
        {
            var (start, end) = _bands[band];
            var peak = 0.0;
            for (var bin = start; bin < end; bin++)
            {
                peak = Math.Max(peak, Math.Sqrt((real[bin] * real[bin]) + (imaginary[bin] * imaginary[bin])));
            }

            var decibels = 20 * Math.Log10((peak * 4 / _size) + 1e-12);
            var level = Math.Clamp((decibels - FloorDecibels) / -FloorDecibels, 0, 1);
            _smoothed[band] = Math.Max(level, _smoothed[band] * Fall);
            levels[band] = (int)Math.Round(_smoothed[band] * 100);
        }

        return levels;
    }

    /// <summary>In place radix-2 Cooley-Tukey FFT.</summary>
    public static void Fft(double[] real, double[] imaginary)
    {
        var count = real.Length;
        for (int index = 1, reversed = 0; index < count; index++)
        {
            var bit = count >> 1;
            for (; (reversed & bit) != 0; bit >>= 1)
            {
                reversed ^= bit;
            }

            reversed ^= bit;
            if (index < reversed)
            {
                (real[index], real[reversed]) = (real[reversed], real[index]);
                (imaginary[index], imaginary[reversed]) = (imaginary[reversed], imaginary[index]);
            }
        }

        for (var length = 2; length <= count; length <<= 1)
        {
            var angle = -2 * Math.PI / length;
            var stepReal = Math.Cos(angle);
            var stepImaginary = Math.Sin(angle);
            for (var start = 0; start < count; start += length)
            {
                var twiddleReal = 1.0;
                var twiddleImaginary = 0.0;
                for (var offset = 0; offset < length / 2; offset++)
                {
                    var even = start + offset;
                    var odd = even + (length / 2);
                    var oddReal = (real[odd] * twiddleReal) - (imaginary[odd] * twiddleImaginary);
                    var oddImaginary = (real[odd] * twiddleImaginary) + (imaginary[odd] * twiddleReal);
                    real[odd] = real[even] - oddReal;
                    imaginary[odd] = imaginary[even] - oddImaginary;
                    real[even] += oddReal;
                    imaginary[even] += oddImaginary;
                    var nextReal = (twiddleReal * stepReal) - (twiddleImaginary * stepImaginary);
                    twiddleImaginary = (twiddleReal * stepImaginary) + (twiddleImaginary * stepReal);
                    twiddleReal = nextReal;
                }
            }
        }
    }
}
