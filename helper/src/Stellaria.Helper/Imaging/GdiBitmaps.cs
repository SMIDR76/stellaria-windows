using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Imaging;

internal static unsafe class GdiBitmaps
{
    public static PixelImage? ReadAndDelete(nint bitmap)
    {
        if (bitmap == 0)
        {
            return null;
        }

        try
        {
            return Read(bitmap);
        }
        finally
        {
            Gdi32.DeleteObject(bitmap);
        }
    }

    public static PixelImage? Read(nint bitmap)
    {
        if (Gdi32.GetObject(bitmap, sizeof(BitmapHeader), out var header) == 0 || header.Width <= 0 || header.Height == 0)
        {
            return null;
        }

        var width = header.Width;
        var height = Math.Abs(header.Height);
        var pixels = new byte[width * height * 4];
        var info = Gdi32.TopDown32(width, height);
        var context = Gdi32.CreateCompatibleDC(0);
        try
        {
            fixed (byte* destination = pixels)
            {
                if (Gdi32.GetDIBits(context, bitmap, 0, (uint)height, destination, &info, Gdi32.DibRgbColors) != height)
                {
                    return null;
                }
            }
        }
        finally
        {
            Gdi32.DeleteDC(context);
        }

        if (header.BitsPerPixel < 32)
        {
            for (var offset = 3; offset < pixels.Length; offset += 4)
            {
                pixels[offset] = 255;
            }
        }

        return new PixelImage(width, height, pixels, true);
    }
}
