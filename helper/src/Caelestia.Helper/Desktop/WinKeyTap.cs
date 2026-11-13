namespace Caelestia.Helper.Desktop;

/// <summary>
/// Recognises a short press of a Windows key on its own, the gesture Caelestia uses to open the launcher.
/// A press that is combined with another key (Win+E, Win+L…) or held too long is not a tap.
/// </summary>
internal sealed class WinKeyTap(TimeSpan maximumHold)
{
    public const uint LeftWindows = 0x5B;
    public const uint RightWindows = 0x5C;

    private State _state = State.Idle;
    private long _pressedAt;

    private enum State
    {
        Idle,
        Pressed,
        Combined,
    }

    public static bool IsWindowsKey(uint virtualKey) => virtualKey is LeftWindows or RightWindows;

    /// <summary>Feeds one key event. Returns true on the release that completes a tap.</summary>
    public bool Handle(uint virtualKey, bool down, long timestampMilliseconds)
    {
        if (IsWindowsKey(virtualKey))
        {
            if (down)
            {
                if (_state == State.Idle)
                {
                    _state = State.Pressed;
                    _pressedAt = timestampMilliseconds;
                }

                return false;
            }

            var tapped = _state == State.Pressed && timestampMilliseconds - _pressedAt <= maximumHold.TotalMilliseconds;
            _state = State.Idle;
            return tapped;
        }

        if (down && _state == State.Pressed)
        {
            _state = State.Combined;
        }

        return false;
    }
}
