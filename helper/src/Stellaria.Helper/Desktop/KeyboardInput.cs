using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Desktop;

internal static class KeyboardInput
{
    public const byte Control = 0x11;
    public const byte Alt = 0x12;
    public const byte LeftWindows = 0x5B;
    public const byte V = 0x56;
    public const byte R = 0x52;
    public static readonly TimeSpan FocusSettleDelay = TimeSpan.FromMilliseconds(140);

    public static readonly byte[] Paste = [Control, V];
    public static readonly byte[] RecordClip = [LeftWindows, Alt, R];
    public static readonly byte[] TaskView = [LeftWindows, Tab];
    public const byte Tab = 0x09;

    public static async Task<bool> SendToWindowAsync(MessageLoop loop, nint window, byte[] chord)
    {
        var focused = await loop.InvokeAsync(() =>
        {
            if (window == 0 || !User32.IsWindow(window))
            {
                return false;
            }

            if (User32.IsIconic(window))
            {
                User32.ShowWindow(window, User32.SwRestore);
            }

            return User32.SetForegroundWindow(window) || User32.GetForegroundWindow() == window;
        }).ConfigureAwait(false);
        if (!focused)
        {
            return false;
        }

        await Task.Delay(FocusSettleDelay).ConfigureAwait(false);
        await loop.InvokeAsync(() => Press(chord)).ConfigureAwait(false);
        return true;
    }

    public static void MaskWindowsKey()
    {
        User32.keybd_event(Unassigned, 0, 0, 0);
        User32.keybd_event(Unassigned, 0, User32.KeyEventKeyUp, 0);
    }

    private const byte Unassigned = 0xE8;

    public static void Press(byte[] chord)
    {
        foreach (var key in chord)
        {
            User32.keybd_event(key, 0, 0, 0);
        }

        for (var index = chord.Length - 1; index >= 0; index--)
        {
            User32.keybd_event(chord[index], 0, User32.KeyEventKeyUp, 0);
        }
    }
}
