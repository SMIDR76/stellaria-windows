using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Caelestia.Helper.Interop;

[GeneratedComInterface]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
internal partial interface IShellItem
{
    [PreserveSig]
    int BindToHandler(nint bindContext, in Guid handler, in Guid interfaceId, out nint result);

    [PreserveSig]
    int GetParent(out IShellItem parent);

    [PreserveSig]
    int GetDisplayName(uint form, out nint name);

    [PreserveSig]
    int GetAttributes(uint mask, out uint attributes);

    [PreserveSig]
    int Compare(IShellItem other, uint hint, out int order);
}

[GeneratedComInterface]
[Guid("70629033-e363-4a28-a567-0db78006e6d7")]
internal partial interface IEnumShellItems
{
    [PreserveSig]
    int Next(uint count, out IShellItem item, out uint fetched);

    [PreserveSig]
    int Skip(uint count);

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int Clone(out IEnumShellItems copy);
}

internal sealed record ShellApp(string Name, string Id);

internal static unsafe partial class ShellFolders
{
    public const uint DisplayNameNormal = 0x00000000;
    public const uint DisplayNameParentRelativeParsing = 0x80018001;

    private static readonly Guid s_appsFolder = new("1e87508d-89c2-42f0-8a7e-645a0f50ca58");
    private static readonly Guid s_enumItemsHandler = new("94f60519-2850-4924-aa5a-d15e84868039");
    private static readonly Guid s_shellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    private static readonly Guid s_enumShellItems = new("70629033-e363-4a28-a567-0db78006e6d7");

    public static List<ShellApp> StartMenuApps()
    {
        Marshal.ThrowExceptionForHR(SHGetKnownFolderItem(s_appsFolder, 0, 0, s_shellItem, out var folder));
        Marshal.ThrowExceptionForHR(folder.BindToHandler(0, s_enumItemsHandler, s_enumShellItems, out var enumeratorPointer));
        IEnumShellItems enumerator;
        try
        {
            enumerator = ComInterfaceMarshaller<IEnumShellItems>.ConvertToManaged((void*)enumeratorPointer)!;
        }
        finally
        {
            Marshal.Release(enumeratorPointer);
        }

        var apps = new List<ShellApp>();
        while (enumerator.Next(1, out var item, out var fetched) == 0 && fetched == 1)
        {
            var name = DisplayName(item, DisplayNameNormal);
            var id = DisplayName(item, DisplayNameParentRelativeParsing);
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
            {
                apps.Add(new ShellApp(name, id));
            }
        }

        return apps;
    }

    private static string? DisplayName(IShellItem item, uint form)
    {
        if (item.GetDisplayName(form, out var pointer) != 0)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUni(pointer);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pointer);
        }
    }

    [LibraryImport("shell32.dll")]
    private static partial int SHGetKnownFolderItem(in Guid folderId, uint flags, nint token, in Guid interfaceId, out IShellItem item);
}
