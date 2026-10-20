using System.Globalization;
using System.Runtime.InteropServices;
using Caelestia.Helper.Interop;
using Microsoft.Win32;

namespace Caelestia.Helper.Desktop;

internal sealed record KeyboardLayout(string Id, string LanguageTag, string Name);

internal static unsafe partial class KeyboardLayouts
{
    private const string LayoutsKey = @"SYSTEM\CurrentControlSet\Control\Keyboard Layouts";

    public static string Format(long hkl) => hkl.ToString("X", CultureInfo.InvariantCulture);

    public static (int Language, int Device) Split(long hkl) => ((int)(hkl & 0xFFFF), (int)((hkl >> 16) & 0xFFFF));

    public static string LanguageTag(int languageId)
    {
        try
        {
            return CultureInfo.GetCultureInfo(languageId).Name;
        }
        catch (CultureNotFoundException)
        {
            return "";
        }
    }

    public static string? DirectLayoutKey(int language, int device) =>
        (device & 0xF000) == 0xF000 ? null : (device == 0 ? language : device).ToString("X8", CultureInfo.InvariantCulture);

    public static List<KeyboardLayout> Installed()
    {
        var count = User32.GetKeyboardLayoutList(0, null);
        if (count <= 0)
        {
            return [];
        }

        var handles = new nint[count];
        fixed (nint* list = handles)
        {
            count = User32.GetKeyboardLayoutList(count, list);
        }

        var result = new List<KeyboardLayout>(count);
        foreach (var handle in handles.Take(count))
        {
            var hkl = (long)handle;
            var (language, device) = Split(hkl);
            var tag = LanguageTag(language);
            result.Add(new KeyboardLayout(Format(hkl), tag, LayoutName(language, device) ?? tag));
        }

        return result;
    }

    public static string CurrentFor(nint hwnd)
    {
        var thread = hwnd != 0 ? User32.GetWindowThreadProcessId(hwnd, out _) : 0;
        return Format((long)User32.GetKeyboardLayout(thread));
    }

    private static string? LayoutName(int language, int device)
    {
        using var root = Registry.LocalMachine.OpenSubKey(LayoutsKey);
        if (root is null)
        {
            return null;
        }

        var key = DirectLayoutKey(language, device) ?? FindByLayoutId(root, (device & 0x0FFF).ToString("X4", CultureInfo.InvariantCulture));
        if (key is null)
        {
            return null;
        }

        using var layout = root.OpenSubKey(key);
        if (layout?.GetValue("Layout Display Name") is string indirect && Localized(indirect) is { Length: > 0 } localized)
        {
            return localized;
        }

        return layout?.GetValue("Layout Text") as string;
    }

    private static string? FindByLayoutId(RegistryKey root, string layoutId)
    {
        foreach (var name in root.GetSubKeyNames())
        {
            using var key = root.OpenSubKey(name);
            if (key?.GetValue("Layout Id") is string value && string.Equals(value, layoutId, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return null;
    }

    private static string? Localized(string indirect)
    {
        var buffer = stackalloc char[256];
        return SHLoadIndirectString(indirect, buffer, 256, 0) == 0 ? new string(buffer) : null;
    }

    [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHLoadIndirectString(string source, char* output, uint outputLength, nint reserved);
}
