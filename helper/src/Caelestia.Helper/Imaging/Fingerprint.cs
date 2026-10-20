namespace Caelestia.Helper.Imaging;

internal static class Fingerprint
{
    public const int GridSize = 8;
    public const int Length = GridSize * GridSize * 3;

    public static int[] Compute(PixelImage image)
    {
        var print = new int[Length];
        for (var cellY = 0; cellY < GridSize; cellY++)
        {
            var top = cellY * image.Height / GridSize;
            var bottom = Math.Max(top + 1, (cellY + 1) * image.Height / GridSize);
            for (var cellX = 0; cellX < GridSize; cellX++)
            {
                var left = cellX * image.Width / GridSize;
                var right = Math.Max(left + 1, (cellX + 1) * image.Width / GridSize);
                long red = 0, green = 0, blue = 0;
                for (var y = top; y < bottom; y++)
                {
                    var offset = (y * image.Width + left) * 4;
                    for (var x = left; x < right; x++, offset += 4)
                    {
                        blue += image.Bgra[offset];
                        green += image.Bgra[offset + 1];
                        red += image.Bgra[offset + 2];
                    }
                }

                var count = (bottom - top) * (right - left);
                var index = (cellY * GridSize + cellX) * 3;
                print[index] = (int)(red / count);
                print[index + 1] = (int)(green / count);
                print[index + 2] = (int)(blue / count);
            }
        }

        return print;
    }

    public static double Difference(int[]? first, int[]? second)
    {
        if (first is null || second is null || first.Length != second.Length)
        {
            return double.MaxValue;
        }

        long total = 0;
        for (var index = 0; index < first.Length; index++)
        {
            total += Math.Abs(first[index] - second[index]);
        }

        return (double)total / first.Length;
    }
}
