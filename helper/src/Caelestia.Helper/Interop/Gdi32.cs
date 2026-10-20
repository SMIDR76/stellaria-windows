using System.Runtime.InteropServices;

namespace Caelestia.Helper.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapHeader
{
    public int Type;
    public int Width;
    public int Height;
    public int WidthBytes;
    public ushort Planes;
    public ushort BitsPerPixel;
    public nint Bits;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapInfoHeader
{
    public uint Size;
    public int Width;
    public int Height;
    public ushort Planes;
    public ushort BitCount;
    public uint Compression;
    public uint SizeImage;
    public int XPelsPerMeter;
    public int YPelsPerMeter;
    public uint ColorsUsed;
    public uint ColorsImportant;
}

internal static unsafe partial class Gdi32
{
    public const uint DibRgbColors = 0;
    public const uint BiRgb = 0;

    [LibraryImport("gdi32.dll", EntryPoint = "GetObjectW")]
    public static partial int GetObject(nint handle, int size, out BitmapHeader bitmap);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint handle);

    [LibraryImport("gdi32.dll")]
    public static partial nint CreateCompatibleDC(nint hdc);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(nint hdc);

    [LibraryImport("gdi32.dll")]
    public static partial nint SelectObject(nint hdc, nint handle);

    [LibraryImport("gdi32.dll")]
    public static partial nint CreateDIBSection(nint hdc, BitmapInfoHeader* info, uint usage, out nint bits, nint section, uint offset);

    [LibraryImport("gdi32.dll")]
    public static partial int GetDIBits(nint hdc, nint bitmap, uint startScan, uint lines, void* bits, BitmapInfoHeader* info, uint usage);

    public static BitmapInfoHeader TopDown32(int width, int height) => new()
    {
        Size = (uint)sizeof(BitmapInfoHeader),
        Width = width,
        Height = -height,
        Planes = 1,
        BitCount = 32,
        Compression = BiRgb,
    };
}
