using Stellaria.Helper.Desktop;

namespace Stellaria.Helper.Hosting;

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
        try
        {
            if (state.OriginalTaskbarAutoHide is { } original)
            {
                Taskbar.SetAutoHide(original);
                state.OriginalTaskbarAutoHide = null;
                log?.Info("recovery", $"Taskbar auto-hide restored to {original}.");
            }

            Taskbar.Refresh();
        }
        catch (Exception exception)
        {
            log?.Error("recovery", "Could not restore the taskbar auto-hide setting.", exception);
        }
    }
}
