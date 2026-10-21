using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Imaging;

internal static unsafe class WindowCapture
{
    public static PixelImage? Print(nint window, int width, int height, uint flags)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var info = Gdi32.TopDown32(width, height);
        var context = Gdi32.CreateCompatibleDC(0);
        var bitmap = Gdi32.CreateDIBSection(context, &info, Gdi32.DibRgbColors, out var bits, 0, 0);
        if (bitmap == 0)
        {
            Gdi32.DeleteDC(context);
            return null;
        }

        var previous = Gdi32.SelectObject(context, bitmap);
        try
        {
            if (!User32.PrintWindow(window, context, flags))
            {
                return null;
            }

            var pixels = new byte[width * height * 4];
            new ReadOnlySpan<byte>((void*)bits, pixels.Length).CopyTo(pixels);
            for (var alpha = 3; alpha < pixels.Length; alpha += 4)
            {
                pixels[alpha] = 255;
            }

            return new PixelImage(width, height, pixels, true);
        }
        finally
        {
            Gdi32.SelectObject(context, previous);
            Gdi32.DeleteObject(bitmap);
            Gdi32.DeleteDC(context);
        }
    }
}
