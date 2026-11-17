using System.Runtime.InteropServices;

namespace Stellaria.Helper.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public Rect(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public readonly int Width => Right - Left;

    public readonly int Height => Bottom - Top;

    public readonly bool IsEmpty => Width <= 0 || Height <= 0;

    public readonly bool Intersects(Rect other) =>
        Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;

    public readonly bool Covers(Rect other) =>
        Left <= other.Left && Top <= other.Top && Right >= other.Right && Bottom >= other.Bottom;

    public override readonly string ToString() => $"{Left},{Top},{Right},{Bottom}";
}

[StructLayout(LayoutKind.Sequential)]
internal struct Point
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct MonitorInfoEx
{
    public int Size;
    public Rect Monitor;
    public Rect Work;
    public uint Flags;
    public fixed char Device[32];

    public readonly bool IsPrimary => (Flags & 1) != 0;

    public readonly string DeviceName
    {
        get
        {
            fixed (char* device = Device)
            {
                return new string(device);
            }
        }
    }

    public static MonitorInfoEx Create() => new() { Size = sizeof(MonitorInfoEx) };
}

[StructLayout(LayoutKind.Sequential)]
internal struct Msg
{
    public nint Hwnd;
    public uint Message;
    public nuint WParam;
    public nint LParam;
    public uint Time;
    public Point Point;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct WndClassEx
{
    public int Size;
    public uint Style;
    public delegate* unmanaged<nint, uint, nuint, nint, nint> WndProc;
    public int ClassExtra;
    public int WindowExtra;
    public nint Instance;
    public nint Icon;
    public nint Cursor;
    public nint Background;
    public char* MenuName;
    public char* ClassName;
    public nint IconSmall;
}

[StructLayout(LayoutKind.Sequential)]
internal struct AppBarData
{
    public int Size;
    public nint Hwnd;
    public uint CallbackMessage;
    public uint Edge;
    public Rect Rect;
    public nint LParam;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LastInputInfo
{
    public uint Size;
    public uint Time;
}
