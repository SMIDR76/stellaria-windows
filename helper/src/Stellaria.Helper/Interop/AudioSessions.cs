using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Stellaria.Helper.Interop;

[GeneratedComInterface]
[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
internal partial interface IAudioSessionManager2
{
    [PreserveSig]
    int GetAudioSessionControl(nint sessionGuid, uint flags, out nint control);

    [PreserveSig]
    int GetSimpleAudioVolume(nint sessionGuid, uint flags, out nint volume);

    [PreserveSig]
    int GetSessionEnumerator(out IAudioSessionEnumerator enumerator);
}

[GeneratedComInterface]
[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
internal partial interface IAudioSessionEnumerator
{
    [PreserveSig]
    int GetCount(out int count);

    [PreserveSig]
    int GetSession(int index, out IAudioSessionControl2 session);
}

[GeneratedComInterface]
[Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
internal partial interface IAudioSessionControl2
{
    [PreserveSig]
    int GetState(out int state);

    [PreserveSig]
    int GetDisplayName(out nint name);

    [PreserveSig]
    int SetDisplayName(nint name, nint eventContext);

    [PreserveSig]
    int GetIconPath(out nint path);

    [PreserveSig]
    int SetIconPath(nint path, nint eventContext);

    [PreserveSig]
    int GetGroupingParam(out Guid grouping);

    [PreserveSig]
    int SetGroupingParam(nint grouping, nint eventContext);

    [PreserveSig]
    int RegisterAudioSessionNotification(nint client);

    [PreserveSig]
    int UnregisterAudioSessionNotification(nint client);

    [PreserveSig]
    int GetSessionIdentifier(out nint identifier);

    [PreserveSig]
    int GetSessionInstanceIdentifier(out nint identifier);

    [PreserveSig]
    int GetProcessId(out uint processId);

    [PreserveSig]
    int IsSystemSoundsSession();

    [PreserveSig]
    int SetDuckingPreference(int optOut);
}

[GeneratedComInterface]
[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
internal partial interface ISimpleAudioVolume
{
    [PreserveSig]
    int SetMasterVolume(float level, in Guid eventContext);

    [PreserveSig]
    int GetMasterVolume(out float level);

    [PreserveSig]
    int SetMute(int mute, in Guid eventContext);

    [PreserveSig]
    int GetMute(out int mute);
}

internal sealed record AudioSession(string Id, uint ProcessId, string DisplayName, bool IsSystemSounds, float Volume, bool Muted);

internal static unsafe partial class AudioSessions
{
    public const int StateExpired = 2;
    private const uint ClassContextAll = 0x17;
    private static readonly Guid s_managerInterface = new("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");

    public static List<AudioSession> List()
    {
        var sessions = new List<AudioSession>();
        foreach (var (control, volume) in Enumerate())
        {
            sessions.Add(Describe(control, volume));
        }

        return sessions;
    }

    public static bool SetVolume(string id, float level) => Apply(id, volume => volume.SetMasterVolume(Math.Clamp(level, 0f, 1f), Guid.Empty));

    public static bool SetMute(string id, bool muted) => Apply(id, volume => volume.SetMute(muted ? 1 : 0, Guid.Empty));

    private static bool Apply(string id, Func<ISimpleAudioVolume, int> change)
    {
        foreach (var (control, volume) in Enumerate())
        {
            if (Text(control.GetSessionInstanceIdentifier(out var pointer), pointer) == id)
            {
                Marshal.ThrowExceptionForHR(change(volume));
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<(IAudioSessionControl2 Control, ISimpleAudioVolume Volume)> Enumerate()
    {
        if (Manager() is not { } manager)
        {
            yield break;
        }

        Marshal.ThrowExceptionForHR(manager.GetSessionEnumerator(out var enumerator));
        Marshal.ThrowExceptionForHR(enumerator.GetCount(out var count));
        for (var index = 0; index < count; index++)
        {
            if (enumerator.GetSession(index, out var control) != 0 || control.GetState(out var state) != 0 || state == StateExpired)
            {
                continue;
            }

            yield return (control, (ISimpleAudioVolume)control);
        }
    }

    private static IAudioSessionManager2? Manager()
    {
        var device = AudioEndpoints.DefaultPlaybackDevice();
        if (device is null)
        {
            return null;
        }

        Marshal.ThrowExceptionForHR(device.Activate(s_managerInterface, ClassContextAll, 0, out var pointer));
        try
        {
            return ComInterfaceMarshaller<IAudioSessionManager2>.ConvertToManaged((void*)pointer);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    private static AudioSession Describe(IAudioSessionControl2 control, ISimpleAudioVolume volume)
    {
        var id = Text(control.GetSessionInstanceIdentifier(out var idPointer), idPointer) ?? "";
        var name = Text(control.GetDisplayName(out var namePointer), namePointer) ?? "";
        control.GetProcessId(out var processId);
        volume.GetMasterVolume(out var level);
        volume.GetMute(out var muted);
        return new AudioSession(id, processId, name, control.IsSystemSoundsSession() == 0, level, muted != 0);
    }

    private static string? Text(int result, nint pointer)
    {
        if (result != 0 || pointer == 0)
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
}
