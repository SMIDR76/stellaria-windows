namespace Stellaria.Helper.Desktop;

internal sealed class SpectrumAnalyser
{
    public const int FftSize = 2048;
    public const double LowestFrequency = 40;
    public const double HighestFrequency = 16000;
    public const double FloorDecibels = -70;
    public const double CeilingDecibels = -12;

    private readonly float[] _samples = new float[FftSize];
    private readonly double[] _window = new double[FftSize];
    private readonly double[] _levels;
    private readonly (int From, int To)[] _bands;
    private int _position;

    public SpectrumAnalyser(int barCount, int sampleRate)
    {
        BarCount = barCount;
        _levels = new double[barCount];
        for (var index = 0; index < FftSize; index++)
        {
            _window[index] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * index / (FftSize - 1));
        }

        var binWidth = (double)sampleRate / FftSize;
        var ratio = Math.Pow(HighestFrequency / LowestFrequency, 1.0 / barCount);
        _bands = new (int, int)[barCount];
        for (var bar = 0; bar < barCount; bar++)
        {
            var low = LowestFrequency * Math.Pow(ratio, bar);
            var high = low * ratio;
            var from = Math.Clamp((int)Math.Floor(low / binWidth), 1, FftSize / 2 - 1);
            var to = Math.Clamp((int)Math.Ceiling(high / binWidth), from + 1, FftSize / 2);
            _bands[bar] = (from, to);
        }
    }

    public int BarCount { get; }

    public void Add(float sample)
    {
        _samples[_position] = sample;
        _position = (_position + 1) % FftSize;
    }

    public int[] Bars()
    {
        var real = new double[FftSize];
        var imaginary = new double[FftSize];
        for (var index = 0; index < FftSize; index++)
        {
            real[index] = _samples[(_position + index) % FftSize] * _window[index];
        }

        Transform(real, imaginary);
        var bars = new int[BarCount];
        for (var bar = 0; bar < BarCount; bar++)
        {
            var (from, to) = _bands[bar];
            var peak = 0.0;
            for (var bin = from; bin < to; bin++)
            {
                peak = Math.Max(peak, Math.Sqrt(real[bin] * real[bin] + imaginary[bin] * imaginary[bin]) / (FftSize / 4.0));
            }

            var decibels = 20 * Math.Log10(Math.Max(peak, 1e-9));
            var target = Math.Clamp((decibels - FloorDecibels) / (CeilingDecibels - FloorDecibels), 0, 1);
            _levels[bar] = target > _levels[bar] ? target : Math.Max(target, _levels[bar] - 0.06);
            bars[bar] = (int)Math.Round(_levels[bar] * 100);
        }

        return bars;
    }

    private static void Transform(double[] real, double[] imaginary)
    {
        var count = real.Length;
        for (int index = 1, swap = 0; index < count; index++)
        {
            var bit = count >> 1;
            for (; (swap & bit) != 0; bit >>= 1)
            {
                swap ^= bit;
            }

            swap ^= bit;
            if (index < swap)
            {
                (real[index], real[swap]) = (real[swap], real[index]);
                (imaginary[index], imaginary[swap]) = (imaginary[swap], imaginary[index]);
            }
        }

        for (var length = 2; length <= count; length <<= 1)
        {
            var angle = -2 * Math.PI / length;
            var stepReal = Math.Cos(angle);
            var stepImaginary = Math.Sin(angle);
            for (var start = 0; start < count; start += length)
            {
                var turnReal = 1.0;
                var turnImaginary = 0.0;
                for (var offset = 0; offset < length / 2; offset++)
                {
                    var even = start + offset;
                    var odd = even + length / 2;
                    var oddReal = real[odd] * turnReal - imaginary[odd] * turnImaginary;
                    var oddImaginary = real[odd] * turnImaginary + imaginary[odd] * turnReal;
                    real[odd] = real[even] - oddReal;
                    imaginary[odd] = imaginary[even] - oddImaginary;
                    real[even] += oddReal;
                    imaginary[even] += oddImaginary;
                    var nextReal = turnReal * stepReal - turnImaginary * stepImaginary;
                    turnImaginary = turnReal * stepImaginary + turnImaginary * stepReal;
                    turnReal = nextReal;
                }
            }
        }
    }
}
