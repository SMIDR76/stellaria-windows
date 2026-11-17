using System.Runtime.InteropServices;

namespace Stellaria.Helper.Interop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct ShellExecuteInfo
{
    public int Size;
    public uint Mask;
    public nint Window;
    public char* Verb;
    public char* File;
    public char* Parameters;
    public char* Directory;
    public int Show;
    public nint InstanceApplication;
    public nint IdList;
    public char* Class;
    public nint ClassKey;
    public uint HotKey;
    public nint IconOrMonitor;
    public nint Process;
}

internal static unsafe partial class ShellExecute
{
    public const uint FlagNoUi = 0x00000400;

    public static bool Open(string target)
    {
        fixed (char* verb = "open")
        fixed (char* file = target)
        {
            var info = new ShellExecuteInfo
            {
                Size = sizeof(ShellExecuteInfo),
                Mask = FlagNoUi,
                Verb = verb,
                File = file,
                Show = User32.SwShowNormal,
            };
            return ShellExecuteEx(&info);
        }
    }

    [LibraryImport("shell32.dll", EntryPoint = "ShellExecuteExW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShellExecuteEx(ShellExecuteInfo* info);
}
