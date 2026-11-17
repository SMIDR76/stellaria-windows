using System.Runtime.InteropServices;

namespace Stellaria.Helper.Interop;

internal static unsafe partial class Kernel32
{
    public const uint ProcessQueryLimitedInformation = 0x1000;
    public const uint ProcessTerminate = 0x0001;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TerminateProcess(nint process, uint exitCode);
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

    [LibraryImport("kernel32.dll")]
    public static partial int GetApplicationUserModelId(nint process, ref uint length, char* buffer);

    public static string ApplicationUserModelId(nint process)
    {
        var buffer = stackalloc char[256];
        uint length = 256;
        return GetApplicationUserModelId(process, ref length, buffer) == 0 && length > 1 ? new string(buffer, 0, (int)length - 1) : "";
    }
}

internal static unsafe partial class WindowProperties
{
    private const ushort VtLpwstr = 31;
    private static readonly Guid s_propertyStore = new("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99");
    private static readonly Guid s_appUserModel = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid Format;
        public uint Id;
    }

    [LibraryImport("shell32.dll")]
    private static partial int SHGetPropertyStoreForWindow(nint hwnd, in Guid iid, out nint store);

    [LibraryImport("ole32.dll")]
    private static partial int PropVariantClear(byte* variant);

    public static string AppUserModelId(nint hwnd)
    {
        if (SHGetPropertyStoreForWindow(hwnd, in s_propertyStore, out var store) != 0 || store == 0)
        {
            return "";
        }

        var variant = stackalloc byte[24];
        new Span<byte>(variant, 24).Clear();
        try
        {
            var key = new PropertyKey { Format = s_appUserModel, Id = 5 };
            var table = *(nint**)store;
            var getValue = (delegate* unmanaged[Stdcall]<nint, PropertyKey*, byte*, int>)table[5];
            if (getValue(store, &key, variant) != 0 || *(ushort*)variant != VtLpwstr)
            {
                return "";
            }

            var text = *(char**)(variant + 8);
            return text == null ? "" : new string(text);
        }
        finally
        {
            PropVariantClear(variant);
            var release = (delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)store)[2];
            release(store);
        }
    }
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
