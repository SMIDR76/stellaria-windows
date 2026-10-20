using Caelestia.Helper.Desktop;

namespace Caelestia.Helper.Hosting;

internal static class ShellRecovery
{
    public static void ShowTaskbar(Log? log)
    {
        try
        {
            Taskbar.SetVisible(true);
        }
        catch (Exception exception)
        {
            log?.Error("recovery", "Could not show the Windows taskbar.", exception);
        }
    }

    public static void RestoreOriginalTaskbar(HelperState state, Log? log)
    {
        ShowTaskbar(log);
        if (state.OriginalTaskbarAutoHide is not { } original)
        {
            return;
        }

        try
        {
            Taskbar.SetAutoHide(original);
            state.OriginalTaskbarAutoHide = null;
            log?.Info("recovery", $"Taskbar auto-hide restored to {original}.");
        }
        catch (Exception exception)
        {
            log?.Error("recovery", "Could not restore the taskbar auto-hide setting.", exception);
        }
    }
}
