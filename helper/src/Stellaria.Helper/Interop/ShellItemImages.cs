using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Stellaria.Helper.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeSize
{
    public int Width;
    public int Height;
}

[GeneratedComInterface]
[Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
internal partial interface IShellItemImageFactory
{
    [PreserveSig]
    int GetImage(NativeSize size, int flags, out nint bitmap);
}

internal static partial class ShellItemImages
{
    public const int BiggerSizeOk = 0x1;
    public const int IconOnly = 0x4;

    private static readonly Guid s_imageFactory = new("bcc18b79-ba16-442f-80c4-8a59c30c463b");

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHCreateItemFromParsingName(string path, nint bindContext, in Guid interfaceId, out IShellItemImageFactory factory);

    public static nint IconBitmap(string parsingName, int size)
    {
        if (SHCreateItemFromParsingName(parsingName, 0, s_imageFactory, out var factory) != 0)
        {
            return 0;
        }

        return factory.GetImage(new NativeSize { Width = size, Height = size }, BiggerSizeOk | IconOnly, out var bitmap) == 0 ? bitmap : 0;
    }
}
