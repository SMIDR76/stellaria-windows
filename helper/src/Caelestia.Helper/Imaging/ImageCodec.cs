using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Caelestia.Helper.Imaging;

internal sealed record PixelImage(int Width, int Height, byte[] Bgra, bool Premultiplied);

internal enum ImageFit
{
    Contain,
    Cover,
}

internal static class ImageCodec
{
    public const double JpegQuality = 0.82;

    public static async Task<PixelImage> DecodeAsync(byte[] encoded, int maxWidth, int maxHeight, ImageFit fit)
    {
        using var stream = await ToStreamAsync(encoded).ConfigureAwait(false);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var (width, height) = Scale((int)decoder.OrientedPixelWidth, (int)decoder.OrientedPixelHeight, maxWidth, maxHeight, fit);
        var transform = new BitmapTransform
        {
            ScaledWidth = (uint)width,
            ScaledHeight = (uint)height,
            InterpolationMode = BitmapInterpolationMode.Fant,
        };
        var pixels = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform,
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.ColorManageToSRgb);
        return new PixelImage(width, height, pixels.DetachPixelData(), true);
    }

    public static Task<byte[]> EncodePngAsync(PixelImage image, int maxWidth = int.MaxValue, int maxHeight = int.MaxValue) =>
        EncodeAsync(image, BitmapEncoder.PngEncoderId, null, maxWidth, maxHeight);

    public static Task<byte[]> EncodeJpegAsync(PixelImage image, int maxWidth = int.MaxValue, int maxHeight = int.MaxValue) =>
        EncodeAsync(image, BitmapEncoder.JpegEncoderId, JpegQuality, maxWidth, maxHeight);

    public static (int Width, int Height) Scale(int sourceWidth, int sourceHeight, int maxWidth, int maxHeight, ImageFit fit)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            throw new ArgumentException("Image dimensions must be positive.");
        }

        var horizontal = (double)maxWidth / sourceWidth;
        var vertical = (double)maxHeight / sourceHeight;
        var factor = fit == ImageFit.Cover ? Math.Max(horizontal, vertical) : Math.Min(horizontal, vertical);
        factor = Math.Min(1.0, factor);
        return (Math.Max(1, (int)Math.Round(sourceWidth * factor)), Math.Max(1, (int)Math.Round(sourceHeight * factor)));
    }

    private static async Task<byte[]> EncodeAsync(PixelImage image, Guid encoderId, double? quality, int maxWidth, int maxHeight)
    {
        using var stream = new InMemoryRandomAccessStream();
        BitmapEncoder encoder;
        if (quality is { } value)
        {
            var options = new BitmapPropertySet { ["ImageQuality"] = new BitmapTypedValue(value, Windows.Foundation.PropertyType.Single) };
            encoder = await BitmapEncoder.CreateAsync(encoderId, stream, options);
        }
        else
        {
            encoder = await BitmapEncoder.CreateAsync(encoderId, stream);
        }

        var alpha = encoderId == BitmapEncoder.JpegEncoderId ? BitmapAlphaMode.Ignore
            : image.Premultiplied ? BitmapAlphaMode.Premultiplied : BitmapAlphaMode.Straight;
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, alpha, (uint)image.Width, (uint)image.Height, 96, 96, image.Bgra);
        var (width, height) = Scale(image.Width, image.Height, maxWidth, maxHeight, ImageFit.Contain);
        if (width != image.Width || height != image.Height)
        {
            encoder.BitmapTransform.ScaledWidth = (uint)width;
            encoder.BitmapTransform.ScaledHeight = (uint)height;
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
        }

        await encoder.FlushAsync();
        return await ReadAllAsync(stream).ConfigureAwait(false);
    }

    private static async Task<IRandomAccessStream> ToStreamAsync(byte[] bytes)
    {
        var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(bytes);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        stream.Seek(0);
        return stream;
    }

    private static async Task<byte[]> ReadAllAsync(IRandomAccessStream stream)
    {
        stream.Seek(0);
        var bytes = new byte[stream.Size];
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(bytes);
        return bytes;
    }
}
