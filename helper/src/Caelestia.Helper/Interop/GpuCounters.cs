using System.Runtime.InteropServices;

namespace Caelestia.Helper.Interop;

[StructLayout(LayoutKind.Explicit)]
internal struct PdhCounterValue
{
    [FieldOffset(0)]
    public uint Status;

    [FieldOffset(8)]
    public double Value;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PdhCounterItem
{
    public nint Name;
    public PdhCounterValue Value;
}

internal sealed unsafe partial class GpuCounters : IDisposable
{
    public const string ThreeDEnginePath = @"\GPU Engine(*engtype_3D)\Utilization Percentage";
    public const uint FormatDouble = 0x00000200;
    public const uint MoreData = 0x800007D2;
    public const uint ValidData = 0;
    public const uint NewData = 1;

    private readonly Lock _gate = new();
    private nint _query;
    private readonly nint _counter;

    public GpuCounters()
    {
        if (PdhOpenQuery(null, 0, out _query) != 0)
        {
            _query = 0;
            return;
        }

        if (PdhAddEnglishCounter(_query, ThreeDEnginePath, 0, out _counter) != 0)
        {
            PdhCloseQuery(_query);
            _query = 0;
            return;
        }

        PdhCollectQueryData(_query);
    }

    public bool Available => _query != 0;

    public double? ThreeDUsage()
    {
        lock (_gate)
        {
            if (_query == 0 || PdhCollectQueryData(_query) != 0)
            {
                return null;
            }

            uint size = 0;
            var status = PdhGetFormattedCounterArray(_counter, FormatDouble, ref size, out var count, null);
            if (status != MoreData || size == 0)
            {
                return count == 0 ? 0 : null;
            }

            var buffer = new byte[size];
            fixed (byte* items = buffer)
            {
                if (PdhGetFormattedCounterArray(_counter, FormatDouble, ref size, out count, (PdhCounterItem*)items) != 0)
                {
                    return null;
                }

                return Sum((PdhCounterItem*)items, count);
            }
        }
    }

    public static double Sum(PdhCounterItem* items, uint count)
    {
        var total = 0.0;
        for (var index = 0; index < count; index++)
        {
            if (items[index].Value.Status is ValidData or NewData)
            {
                total += items[index].Value.Value;
            }
        }

        return Math.Clamp(total, 0, 100);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_query != 0)
            {
                PdhCloseQuery(_query);
                _query = 0;
            }
        }
    }

    [LibraryImport("pdh.dll", EntryPoint = "PdhOpenQueryW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint PdhOpenQuery(string? dataSource, nint userData, out nint query);

    [LibraryImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint PdhAddEnglishCounter(nint query, string path, nint userData, out nint counter);

    [LibraryImport("pdh.dll")]
    private static partial uint PdhCollectQueryData(nint query);

    [LibraryImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterArrayW")]
    private static partial uint PdhGetFormattedCounterArray(nint counter, uint format, ref uint bufferSize, out uint itemCount, PdhCounterItem* items);

    [LibraryImport("pdh.dll")]
    private static partial uint PdhCloseQuery(nint query);
}
