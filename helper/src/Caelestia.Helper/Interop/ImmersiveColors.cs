using System.Runtime.InteropServices;

namespace Caelestia.Helper.Interop;

/// <summary>
/// The colour preference Windows Settings writes when the user picks an accent colour. uxtheme exports it by ordinal
/// only (120 and 122); it is what makes the Start menu, Settings and the title bars take a new accent immediately
/// instead of at the next sign-in.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ImmersiveColorPreference
{
    /// <summary>COLORREF (0x00BBGGRR) of the Start menu and taskbar background.</summary>
    public uint StartColor;

    /// <summary>COLORREF (0x00BBGGRR) of the accent.</summary>
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

    /// <summary>Tells running applications that the colour set and light or dark mode changed.</summary>
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
