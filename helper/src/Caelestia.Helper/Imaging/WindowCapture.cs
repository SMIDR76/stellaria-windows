using Caelestia.Helper.Interop;

namespace Caelestia.Helper.Imaging;

internal static unsafe class WindowCapture
{
    private const int Halftone = 4;
    private const uint SourceCopy = 0x00CC0020;

    public static PixelImage? PrintRegion(nint window, int width, int height, uint flags, int regionX, int regionY, int regionWidth, int regionHeight, int maxWidth)
    {
        regionX = Math.Clamp(regionX, 0, width);
        regionY = Math.Clamp(regionY, 0, height);
        regionWidth = Math.Min(regionWidth, width - regionX);
        regionHeight = Math.Min(regionHeight, height - regionY);
        if (regionWidth <= 0 || regionHeight <= 0 || maxWidth <= 0)
        {
            return null;
        }

        var targetWidth = Math.Min(regionWidth, maxWidth);
        var targetHeight = Math.Max(1, (int)((long)regionHeight * targetWidth / regionWidth));
        using var full = DibSurface.Create(width, height);
        using var target = DibSurface.Create(targetWidth, targetHeight);
        if (full is null || target is null || !User32.PrintWindow(window, full.Context, flags))
        {
            return null;
        }

        Gdi32.SetStretchBltMode(target.Context, Halftone);
        Gdi32.SetBrushOrgEx(target.Context, 0, 0, 0);
        if (!Gdi32.StretchBlt(target.Context, 0, 0, targetWidth, targetHeight, full.Context, regionX, regionY, regionWidth, regionHeight, SourceCopy))
        {
            return null;
        }

        var pixels = new byte[targetWidth * targetHeight * 4];
        new ReadOnlySpan<byte>((void*)target.Bits, pixels.Length).CopyTo(pixels);
        for (var alpha = 3; alpha < pixels.Length; alpha += 4)
        {
            pixels[alpha] = 255;
        }

        return new PixelImage(targetWidth, targetHeight, pixels, true);
    }

    private sealed class DibSurface : IDisposable
    {
        private readonly nint _bitmap;
        private readonly nint _previous;

        private DibSurface(nint context, nint bitmap, nint bits)
        {
            Context = context;
            Bits = bits;
            _bitmap = bitmap;
            _previous = Gdi32.SelectObject(context, bitmap);
        }

        public nint Context { get; }

        public nint Bits { get; }

        public static DibSurface? Create(int width, int height)
        {
            var info = Gdi32.TopDown32(width, height);
            var context = Gdi32.CreateCompatibleDC(0);
            var bitmap = Gdi32.CreateDIBSection(context, &info, Gdi32.DibRgbColors, out var bits, 0, 0);
            if (bitmap != 0)
            {
                return new DibSurface(context, bitmap, bits);
            }

            Gdi32.DeleteDC(context);
            return null;
        }

        public void Dispose()
        {
            Gdi32.SelectObject(Context, _previous);
            Gdi32.DeleteObject(_bitmap);
            Gdi32.DeleteDC(Context);
        }
    }
}
