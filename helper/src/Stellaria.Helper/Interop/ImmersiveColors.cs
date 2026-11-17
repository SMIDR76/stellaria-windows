using System.Runtime.InteropServices;

namespace Stellaria.Helper.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct ImmersiveColorPreference
{
    public uint StartColor;

    public uint AccentColor;
}

internal static partial class ImmersiveColors
{
    private const uint WmSettingChange = 0x001A;
    private const uint SmtoAbortIfHung = 0x0002;
    private static readonly nint s_broadcast = 0xFFFF;

    [LibraryImport("uxtheme.dll", EntryPoint = "#120")]
    private static partial int GetUserColorPreference(out ImmersiveColorPreference preference, [MarshalAs(UnmanagedType.Bool)] bool forceReload);

    [LibraryImport("uxtheme.dll", EntryPoint = "#122")]
    private static partial int SetUserColorPreference(in ImmersiveColorPreference preference, [MarshalAs(UnmanagedType.Bool)] bool forceCommit);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
    private static partial nint SendMessageTimeout(nint window, uint message, nint wParam, nint lParam, uint flags, uint timeout, out nint result);

    public static ImmersiveColorPreference? Read() =>
        GetUserColorPreference(out var preference, true) >= 0 ? preference : null;

    public static bool Write(ImmersiveColorPreference preference) => SetUserColorPreference(preference, true) >= 0;

    public static void Broadcast()
    {
        var text = Marshal.StringToHGlobalUni("ImmersiveColorSet");
        try
        {
            SendMessageTimeout(s_broadcast, WmSettingChange, 0, text, SmtoAbortIfHung, 200, out _);
        }
        finally
        {
            Marshal.FreeHGlobal(text);
        }
    }
}
