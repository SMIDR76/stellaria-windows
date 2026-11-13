using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Caelestia.Helper.Interop;

[GeneratedComInterface]
[Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2")]
internal partial interface IAudioClient
{
    [PreserveSig]
    int Initialize(int shareMode, uint streamFlags, long bufferDuration, long periodicity, nint format, nint sessionGuid);

    [PreserveSig]
    int GetBufferSize(out uint frames);

    [PreserveSig]
    int GetStreamLatency(out long latency);

    [PreserveSig]
    int GetCurrentPadding(out uint frames);

    [PreserveSig]
    int IsFormatSupported(int shareMode, nint format, out nint closestMatch);

    [PreserveSig]
    int GetMixFormat(out nint format);

    [PreserveSig]
    int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);

    [PreserveSig]
    int Start();

    [PreserveSig]
    int Stop();

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int SetEventHandle(nint handle);

    [PreserveSig]
    int GetService(in Guid interfaceId, out nint service);
}

[GeneratedComInterface]
[Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317")]
internal partial interface IAudioCaptureClient
{
    [PreserveSig]
    int GetBuffer(out nint data, out uint frames, out uint flags, out ulong devicePosition, out ulong performancePosition);

    [PreserveSig]
    int ReleaseBuffer(uint frames);

    [PreserveSig]
    int GetNextPacketSize(out uint frames);
}

/// <summary>
/// Captures what the default playback device plays (WASAPI loopback) and keeps the latest mono samples.
/// Only 32 bit float mix formats are read, which is what Windows uses in shared mode.
/// </summary>
internal sealed unsafe class LoopbackCapture : IDisposable
{
    public const int RingSize = 4096;
    private const uint StreamFlagsLoopback = 0x00020000;
    private const uint BufferFlagsSilent = 0x2;
    private const long BufferDuration = 1_000_000;
    private const uint ClassContextAll = 0x17;
    private const ushort FormatFloat = 3;
    private const ushort FormatExtensible = 0xFFFE;
    private static readonly Guid s_clientInterface = new("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
    private static readonly Guid s_captureInterface = new("C8ADBD64-E71E-48A0-A4DE-185C395CD317");
    private static readonly Guid s_floatSubFormat = new("00000003-0000-0010-8000-00AA00389B71");

    private readonly IAudioClient _client;
    private readonly IAudioCaptureClient _capture;
    private readonly int _channels;
    private readonly float[] _ring = new float[RingSize];
    private readonly Lock _gate = new();
    private int _write;

    private LoopbackCapture(IAudioClient client, IAudioCaptureClient capture, int channels, int sampleRate)
    {
        _client = client;
        _capture = capture;
        _channels = channels;
        SampleRate = sampleRate;
    }

    public int SampleRate { get; }

    public long LastAudioTicks { get; private set; }

    /// <summary>Opens a loopback stream on the default playback device, or returns null when there is none.</summary>
    public static LoopbackCapture? Open()
    {
        var device = AudioEndpoints.DefaultPlaybackDevice();
        if (device is null)
        {
            return null;
        }

        Marshal.ThrowExceptionForHR(device.Activate(s_clientInterface, ClassContextAll, 0, out var clientPointer));
        var client = Wrap<IAudioClient>(clientPointer);
        Marshal.ThrowExceptionForHR(client.GetMixFormat(out var format));
        try
        {
            var tag = *(ushort*)format;
            var channels = *(ushort*)(format + 2);
            var sampleRate = *(int*)(format + 4);
            var bits = *(ushort*)(format + 14);
            var isFloat = tag == FormatFloat || (tag == FormatExtensible && *(Guid*)(format + 24) == s_floatSubFormat);
            if (!isFloat || bits != 32 || channels == 0)
            {
                return null;
            }

            Marshal.ThrowExceptionForHR(client.Initialize(0, StreamFlagsLoopback, BufferDuration, 0, format, 0));
            Marshal.ThrowExceptionForHR(client.GetService(s_captureInterface, out var capturePointer));
            var capture = Wrap<IAudioCaptureClient>(capturePointer);
            Marshal.ThrowExceptionForHR(client.Start());
            return new LoopbackCapture(client, capture, channels, sampleRate);
        }
        finally
        {
            Marshal.FreeCoTaskMem(format);
        }
    }

    /// <summary>Drains the packets Windows has captured. Returns an HRESULT, non zero when the device went away.</summary>
    public int Pump()
    {
        while (true)
        {
            var result = _capture.GetNextPacketSize(out var available);
            if (result != 0 || available == 0)
            {
                return result;
            }

            result = _capture.GetBuffer(out var data, out var frames, out var flags, out _, out _);
            if (result != 0)
            {
                return result;
            }

            lock (_gate)
            {
                var samples = (float*)data;
                for (var frame = 0; frame < frames; frame++)
                {
                    var sum = 0f;
                    if ((flags & BufferFlagsSilent) == 0)
                    {
                        for (var channel = 0; channel < _channels; channel++)
                        {
                            sum += samples[(frame * _channels) + channel];
                        }
                    }

                    _ring[_write] = sum / _channels;
                    _write = (_write + 1) % RingSize;
                }
            }

            if ((flags & BufferFlagsSilent) == 0 && frames > 0)
            {
                LastAudioTicks = Environment.TickCount64;
            }

            _capture.ReleaseBuffer(frames);
        }
    }

    /// <summary>Copies the most recent samples, oldest first.</summary>
    public void Latest(Span<float> destination)
    {
        lock (_gate)
        {
            var start = (_write - destination.Length + RingSize) % RingSize;
            for (var index = 0; index < destination.Length; index++)
            {
                destination[index] = _ring[(start + index) % RingSize];
            }
        }
    }

    public void Dispose() => _client.Stop();

    private static T Wrap<T>(nint pointer)
    {
        try
        {
            return ComInterfaceMarshaller<T>.ConvertToManaged((void*)pointer)!;
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }
}
