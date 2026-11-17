using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Stellaria.Helper.Interop;

[GeneratedComInterface]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
internal partial interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(int dataFlow, uint stateMask, out nint devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
}

[GeneratedComInterface]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
internal partial interface IMMDevice
{
    [PreserveSig]
    int Activate(in Guid interfaceId, uint context, nint activationParams, out nint result);

    [PreserveSig]
    int OpenPropertyStore(uint access, out nint properties);

    [PreserveSig]
    int GetId(out nint id);

    [PreserveSig]
    int GetState(out uint state);
}

[GeneratedComInterface]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
internal partial interface IPolicyConfig
{
    [PreserveSig]
    int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string id, nint format);

    [PreserveSig]
    int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, int useDefault, nint format);

    [PreserveSig]
    int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id);

    [PreserveSig]
    int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, nint endpointFormat, nint mixFormat);

    [PreserveSig]
    int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, int useDefault, nint defaultPeriod, nint minimumPeriod);

    [PreserveSig]
    int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, nint period);

    [PreserveSig]
    int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, nint mode);

    [PreserveSig]
    int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, nint mode);

    [PreserveSig]
    int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, nint key, nint value);

    [PreserveSig]
    int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, nint key, nint value);

    [PreserveSig]
    int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, int role);

    [PreserveSig]
    int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string id, int visible);
}

internal static unsafe partial class AudioEndpoints
{
    public const uint DeviceStateActive = 0x1;
    public const int RoleCount = 3;

    private static readonly Guid s_enumeratorClass = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid s_enumeratorInterface = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private static readonly Guid s_policyConfigClass = new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
    private static readonly Guid s_policyConfigInterface = new("F8679F50-850A-41CF-9C72-430F290290C8");

    public static bool IsActiveEndpoint(string id)
    {
        var enumerator = Create<IMMDeviceEnumerator>(s_enumeratorClass, s_enumeratorInterface);
        return enumerator.GetDevice(id, out var device) == 0 && device.GetState(out var state) == 0 && (state & DeviceStateActive) != 0;
    }

    public static string? DefaultPlaybackId()
    {
        var enumerator = Create<IMMDeviceEnumerator>(s_enumeratorClass, s_enumeratorInterface);
        if (enumerator.GetDefaultAudioEndpoint(0, 0, out var device) != 0 || device.GetId(out var pointer) != 0)
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

    public static IMMDevice? DefaultPlaybackDevice()
    {
        var enumerator = Create<IMMDeviceEnumerator>(s_enumeratorClass, s_enumeratorInterface);
        return enumerator.GetDefaultAudioEndpoint(0, 0, out var device) == 0 ? device : null;
    }

    public static int SetDefault(string id)
    {
        var policy = Create<IPolicyConfig>(s_policyConfigClass, s_policyConfigInterface);
        for (var role = 0; role < RoleCount; role++)
        {
            var result = policy.SetDefaultEndpoint(id, role);
            if (result != 0)
            {
                return result;
            }
        }

        return 0;
    }

    private static T Create<T>(Guid classId, Guid interfaceId)
    {
        Marshal.ThrowExceptionForHR(CoCreateInstance(classId, 0, ClassContextInprocServer, interfaceId, out var pointer));
        try
        {
            return (T)ComInterfaceMarshaller<T>.ConvertToManaged((void*)pointer)!;
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    private const uint ClassContextInprocServer = 0x1;

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid classId, nint outer, uint context, in Guid interfaceId, out nint result);
}
