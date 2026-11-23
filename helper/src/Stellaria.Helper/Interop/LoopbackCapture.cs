using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Stellaria.Helper.Interop;

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
    int IsFormatSupported(int shareMode, nint format, out nint closest);

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
    int GetBuffer(out nint data, out uint frames, out uint flags, out ulong devicePosition, out ulong counterPosition);

    [PreserveSig]
    int ReleaseBuffer(uint frames);

    [PreserveSig]
    int GetNextPacketSize(out uint frames);
}

internal sealed unsafe class LoopbackCapture : IDisposable
{
    private const uint ClassContextAll = 0x17;
    private const uint StreamFlagsLoopback = 0x00020000;
    private const uint BufferSilent = 0x2;
    private const ushort FormatPcm = 1;
    private const ushort FormatFloat = 3;
    private const ushort FormatExtensible = 0xFFFE;

    private static readonly Guid s_audioClient = new("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
    private static readonly Guid s_captureClient = new("C8ADBD64-E71E-48A0-A4DE-185C395CD317");
    private static readonly Guid s_floatSubFormat = new("00000003-0000-0010-8000-00AA00389B71");

    private readonly IAudioClient _client;
    private readonly IAudioCaptureClient _capture;
    private readonly int _channels;
    private readonly bool _float;
    private readonly int _bits;

    private LoopbackCapture(IAudioClient client, IAudioCaptureClient capture, int channels, bool isFloat, int bits, int sampleRate)
    {
        _client = client;
        _capture = capture;
        _channels = channels;
        _float = isFloat;
        _bits = bits;
        SampleRate = sampleRate;
    }

    public int SampleRate { get; }

    public static LoopbackCapture? Open()
    {
        var device = AudioEndpoints.DefaultPlaybackDevice();
        if (device is null)
        {
            return null;
        }

        Marshal.ThrowExceptionForHR(device.Activate(s_audioClient, ClassContextAll, 0, out var clientPointer));
        IAudioClient client;
        try
        {
            client = ComInterfaceMarshaller<IAudioClient>.ConvertToManaged((void*)clientPointer)!;
        }
        finally
        {
            Marshal.Release(clientPointer);
        }

        Marshal.ThrowExceptionForHR(client.GetMixFormat(out var format));
        try
        {
            var tag = *(ushort*)format;
            var channels = *(ushort*)(format + 2);
            var sampleRate = *(int*)(format + 4);
            var bits = *(ushort*)(format + 14);
            var isFloat = tag == FormatFloat || (tag == FormatExtensible && *(Guid*)(format + 24) == s_floatSubFormat);
            if (channels == 0 || !(isFloat && bits == 32) && !(tag is FormatPcm or FormatExtensible && bits == 16))
            {
                return null;
            }

            Marshal.ThrowExceptionForHR(client.Initialize(0, StreamFlagsLoopback, 1_000_000, 0, format, 0));
            Marshal.ThrowExceptionForHR(client.GetService(s_captureClient, out var capturePointer));
            IAudioCaptureClient capture;
            try
            {
                capture = ComInterfaceMarshaller<IAudioCaptureClient>.ConvertToManaged((void*)capturePointer)!;
            }
            finally
            {
                Marshal.Release(capturePointer);
            }

            Marshal.ThrowExceptionForHR(client.Start());
            return new LoopbackCapture(client, capture, channels, isFloat, bits, sampleRate);
        }
        finally
        {
            Marshal.FreeCoTaskMem(format);
        }
    }

    public void Read(Action<float> sample)
    {
        while (true)
        {
            Marshal.ThrowExceptionForHR(_capture.GetNextPacketSize(out var packet));
            if (packet == 0)
            {
                return;
            }

            Marshal.ThrowExceptionForHR(_capture.GetBuffer(out var data, out var frames, out var flags, out _, out _));
            try
            {
                for (var frame = 0; frame < frames; frame++)
                {
                    if ((flags & BufferSilent) != 0)
                    {
                        sample(0);
                        continue;
                    }

                    var sum = 0f;
                    for (var channel = 0; channel < _channels; channel++)
                    {
                        var index = frame * _channels + channel;
                        sum += _float ? ((float*)data)[index] : ((short*)data)[index] / 32768f;
                    }

                    sample(sum / _channels);
                }
            }
            finally
            {
                _capture.ReleaseBuffer(frames);
            }
        }
    }

    public void Dispose()
    {
        _client.Stop();
    }
}
