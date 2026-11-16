namespace Caelestia.Helper.Desktop;

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
