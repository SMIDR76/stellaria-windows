using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Modules;

internal static class DesktopEventHooks
{
    private static readonly (uint Min, uint Max)[] s_ranges =
    [
        (WinEvents.SystemForeground, WinEvents.SystemForeground),
        (WinEvents.SystemMinimizeStart, WinEvents.SystemMinimizeEnd),
        (WinEvents.ObjectCreate, WinEvents.ObjectHide),
        (WinEvents.ObjectLocationChange, WinEvents.ObjectNameChange),
        (WinEvents.ObjectCloaked, WinEvents.ObjectUncloaked),
    ];

    public static Task InstallAsync(MessageLoop loop) => loop.InvokeAsync(() =>
    {
        foreach (var (min, max) in s_ranges)
        {
            loop.HookWinEvents(min, max);
        }
    });

    public static bool IsWindowEvent(int objectId, int childId) => objectId == WinEvents.ObjectIdWindow && childId == 0;
}
