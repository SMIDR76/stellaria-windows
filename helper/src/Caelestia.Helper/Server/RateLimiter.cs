namespace Caelestia.Helper.Server;

internal sealed class RateLimiter(int limit, TimeSpan window, TimeProvider time)
{
    private readonly Queue<DateTimeOffset> _accepted = new();

    public bool TryAcquire()
    {
        var now = time.GetUtcNow();
        while (_accepted.Count > 0 && now - _accepted.Peek() >= window)
        {
            _accepted.Dequeue();
        }

        if (_accepted.Count >= limit)
        {
            return false;
        }

        _accepted.Enqueue(now);
        return true;
    }
}
