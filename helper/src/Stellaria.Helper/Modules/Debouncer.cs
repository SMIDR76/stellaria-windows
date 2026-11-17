using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Modules;

internal sealed class Debouncer : IDisposable
{
    private readonly MessageLoop _loop;
    private readonly Action _action;
    private readonly TimeSpan _delay;
    private readonly Timer _timer;
    private int _pending;

    public Debouncer(MessageLoop loop, TimeSpan delay, Action action)
    {
        _loop = loop;
        _delay = delay;
        _action = action;
        _timer = new Timer(_ => Fire(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Schedule()
    {
        if (Interlocked.Exchange(ref _pending, 1) == 0)
        {
            _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose() => _timer.Dispose();

    private void Fire()
    {
        Interlocked.Exchange(ref _pending, 0);
        _loop.Post(_action);
    }
}
