using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Caelestia.Helper.Desktop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DisplayDevice
{
    public int Size;
    public fixed char Name[32];
    public fixed char Description[128];
    public uint StateFlags;
    public fixed char Id[128];
    public fixed char Key[128];
}

internal static unsafe partial class SystemFacts
{
    public const uint PrimaryDevice = 0x4;
    public const int FirstWindows11Build = 22000;
    public static readonly string[] AccountPictureSizes = ["Image448", "Image240", "Image208", "Image192", "Image96"];

    public static string OperatingSystem()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var product = key?.GetValue("ProductName") as string ?? "Windows";
        var build = int.TryParse(key?.GetValue("CurrentBuildNumber") as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : 0;
        var display = key?.GetValue("DisplayVersion") as string;
        return FormatOperatingSystem(product, build, display);
    }

    public static string FormatOperatingSystem(string product, int build, string? displayVersion)
    {
        var name = build >= FirstWindows11Build ? product.Replace("Windows 10", "Windows 11", StringComparison.Ordinal) : product;
        return string.IsNullOrEmpty(displayVersion) ? name : $"{name} {displayVersion}";
    }

    public static string Processor()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        return CollapseSpaces(key?.GetValue("ProcessorNameString") as string ?? "");
    }

    public static string CollapseSpaces(string text) => Spaces().Replace(text, " ").Trim();

    public static string PrimaryGraphicsAdapter()
    {
        for (uint index = 0; ; index++)
        {
            var device = new DisplayDevice { Size = sizeof(DisplayDevice) };
            if (!EnumDisplayDevices(null, index, ref device, 0))
            {
                return "";
            }

            if ((device.StateFlags & PrimaryDevice) != 0)
            {
                return new string(device.Description);
            }
        }
    }

    public static (string Name, string User) Account()
    {
        var user = Environment.UserName;
        var buffer = stackalloc char[256];
        uint length = 256;
        var name = GetUserNameEx(NameDisplay, buffer, ref length) && length > 0 ? new string(buffer, 0, (int)length) : user;
        return (name, user);
    }

    public static string? AccountPicturePath()
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value;
        if (sid is null)
        {
            return null;
        }

        using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\{sid}");
        return AccountPictureSizes.Select(size => key?.GetValue(size) as string).FirstOrDefault(path => path is not null && File.Exists(path));
    }

    private const int NameDisplay = 3;

    [LibraryImport("user32.dll", EntryPoint = "EnumDisplayDevicesW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayDevices(string? device, uint index, ref DisplayDevice displayDevice, uint flags);

    [LibraryImport("secur32.dll", EntryPoint = "GetUserNameExW")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool GetUserNameEx(int format, char* name, ref uint length);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
