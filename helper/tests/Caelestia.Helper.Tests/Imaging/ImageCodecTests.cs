using Caelestia.Helper.Imaging;

namespace Caelestia.Helper.Tests.Imaging;

public sealed class ImageCodecTests
{
    private static PixelImage Solid(int width, int height, byte blue, byte green, byte red)
    {
        var pixels = new byte[width * height * 4];
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = blue;
            pixels[offset + 1] = green;
            pixels[offset + 2] = red;
            pixels[offset + 3] = 255;
        }

        return new PixelImage(width, height, pixels, true);
    }

    [Fact]
    public async Task Png_round_trip_keeps_every_pixel()
    {
        var image = new PixelImage(2, 2, [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 10, 20, 30, 255], true);

        var decoded = await ImageCodec.DecodeAsync(await ImageCodec.EncodePngAsync(image), 2, 2, ImageFit.Contain);

        Assert.Equal(2, decoded.Width);
        Assert.Equal(image.Bgra, decoded.Bgra);
    }

    [Fact]
    public async Task Encoders_produce_standard_file_signatures()
    {
        var image = Solid(4, 4, 0, 128, 255);

        var png = await ImageCodec.EncodePngAsync(image);
        var jpeg = await ImageCodec.EncodeJpegAsync(image);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);
        Assert.Equal(new byte[] { 0xFF, 0xD8 }, jpeg[..2]);
    }

    [Fact]
    public async Task Encoding_with_a_maximum_size_shrinks_the_image()
    {
        var png = await ImageCodec.EncodePngAsync(Solid(400, 200, 1, 2, 3), maxWidth: 100);

        var decoded = await ImageCodec.DecodeAsync(png, 1000, 1000, ImageFit.Contain);

        Assert.Equal((100, 50), (decoded.Width, decoded.Height));
    }

    [Theory]
    [InlineData(1920, 1080, 384, 216, false, 384, 216)]
    [InlineData(1000, 1000, 384, 216, false, 216, 216)]
    [InlineData(1000, 1000, 384, 216, true, 384, 384)]
    [InlineData(100, 50, 384, 216, false, 100, 50)]
    public void Scaling_keeps_the_aspect_ratio_and_never_enlarges(int width, int height, int maxWidth, int maxHeight, bool cover, int expectedWidth, int expectedHeight)
    {
        var fit = cover ? ImageFit.Cover : ImageFit.Contain;

        Assert.Equal((expectedWidth, expectedHeight), ImageCodec.Scale(width, height, maxWidth, maxHeight, fit));
    }

    [Fact]
    public void Cropping_copies_the_requested_rectangle()
    {
        var image = new PixelImage(3, 1, [1, 1, 1, 255, 2, 2, 2, 255, 3, 3, 3, 255], true);

        var cropped = ImageCodec.Crop(image, 1, 0, 2, 1);

        Assert.Equal(new byte[] { 2, 2, 2, 255, 3, 3, 3, 255 }, cropped.Bgra);
    }

    [Fact]
    public void Identical_images_have_no_fingerprint_difference()
    {
        var image = Solid(64, 32, 10, 20, 30);

        Assert.Equal(0, Fingerprint.Difference(Fingerprint.Compute(image), Fingerprint.Compute(image)));
    }

    [Fact]
    public void Black_and_white_images_are_maximally_different()
    {
        var black = Fingerprint.Compute(Solid(16, 16, 0, 0, 0));
        var white = Fingerprint.Compute(Solid(16, 16, 255, 255, 255));

        Assert.Equal(255, Fingerprint.Difference(black, white));
    }

    [Fact]
    public void Fingerprints_store_red_green_blue_in_that_order()
    {
        var print = Fingerprint.Compute(Solid(8, 8, blue: 3, green: 2, red: 1));

        Assert.Equal(new[] { 1, 2, 3 }, print[..3]);
    }
}
