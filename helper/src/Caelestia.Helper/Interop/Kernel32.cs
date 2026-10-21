using System.Runtime.InteropServices;

namespace Caelestia.Helper.Interop;

internal static unsafe partial class Kernel32
{
    public const uint ProcessQueryLimitedInformation = 0x1000;
    public const uint EsContinuous = 0x80000000;
    public const uint EsSystemRequired = 0x00000001;
    public const uint EsDisplayRequired = 0x00000002;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint processId);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool QueryFullProcessImageName(nint process, uint flags, char* name, ref uint size);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetProcessTimes(nint process, out long creation, out long exit, out long kernel, out long user);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("kernel32.dll")]
    public static partial uint SetThreadExecutionState(uint flags);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandle(string? moduleName);
}

internal static partial class Dwmapi
{
    public const int ExtendedFrameBounds = 9;
    public const int Cloaked = 14;

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmGetWindowAttribute(nint hwnd, int attribute, out Rect value, int size);

    public static bool IsCloaked(nint hwnd) => DwmGetWindowAttribute(hwnd, Cloaked, out int cloaked, sizeof(int)) == 0 && cloaked != 0;

    public static unsafe Rect? FrameBounds(nint hwnd) =>
        DwmGetWindowAttribute(hwnd, ExtendedFrameBounds, out Rect frame, sizeof(Rect)) == 0 ? frame : null;
}

internal static partial class Shell32
{
    public const uint AbmNew = 0x00;
    public const uint AbmSetPos = 0x03;
    public const uint AbmGetState = 0x04;
    public const uint AbmSetState = 0x0A;
    public const uint AbsAutoHide = 0x01;

    [LibraryImport("shell32.dll")]
    public static partial nuint SHAppBarMessage(uint message, ref AppBarData data);
}
