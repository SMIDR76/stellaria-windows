using System.Runtime.InteropServices;

namespace Caelestia.Helper.Interop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct PhysicalMonitor
{
    public nint Handle;
    public fixed char Description[128];
}

internal static unsafe partial class MonitorBrightness
{
    public const int Unsupported = -1;

    public static Dictionary<string, int> ReadAll()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var monitor in User32.DisplayMonitors())
        {
            var device = User32.MonitorDevice(monitor);
            result[device] = WithFirstPhysical(monitor, Read) ?? Unsupported;
        }

        return result;
    }

    public static bool Write(string device, int percent)
    {
        foreach (var monitor in User32.DisplayMonitors())
        {
            if (string.Equals(User32.MonitorDevice(monitor), device, StringComparison.OrdinalIgnoreCase))
            {
                return WithFirstPhysical(monitor, handle => Set(handle, percent));
            }
        }

        return false;
    }

    public static int ToPercent(uint minimum, uint current, uint maximum) =>
        maximum > minimum ? (int)Math.Round(100.0 * (Math.Clamp(current, minimum, maximum) - minimum) / (maximum - minimum)) : Unsupported;

    public static uint FromPercent(uint minimum, uint maximum, int percent) =>
        (uint)Math.Round(minimum + (maximum - minimum) * Math.Clamp(percent, 0, 100) / 100.0);

    private static int? Read(nint handle) =>
        GetMonitorBrightness(handle, out var minimum, out var current, out var maximum) ? ToPercent(minimum, current, maximum) : null;

    private static bool Set(nint handle, int percent) =>
        GetMonitorBrightness(handle, out var minimum, out _, out var maximum) && maximum > minimum && SetMonitorBrightness(handle, FromPercent(minimum, maximum, percent));

    private static T? WithFirstPhysical<T>(nint monitor, Func<nint, T> action)
    {
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out var count) || count == 0)
        {
            return default;
        }

        var physical = new PhysicalMonitor[count];
        fixed (PhysicalMonitor* pointer = physical)
        {
            if (!GetPhysicalMonitorsFromHMONITOR(monitor, count, pointer))
            {
                return default;
            }

            try
            {
                return action(physical[0].Handle);
            }
            finally
            {
                DestroyPhysicalMonitors(count, pointer);
            }
        }
    }

    [LibraryImport("dxva2.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint monitor, out uint count);

    [LibraryImport("dxva2.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetPhysicalMonitorsFromHMONITOR(nint monitor, uint count, PhysicalMonitor* monitors);

    [LibraryImport("dxva2.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyPhysicalMonitors(uint count, PhysicalMonitor* monitors);

    [LibraryImport("dxva2.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorBrightness(nint handle, out uint minimum, out uint current, out uint maximum);

    [LibraryImport("dxva2.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetMonitorBrightness(nint handle, uint value);
}
