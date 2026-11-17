using Stellaria.Helper.Server;
using Stellaria.Helper.Tests.TestSupport;

namespace Stellaria.Helper.Tests.Server;

public sealed class RateLimiterTests
{
    [Fact]
    public void Requests_above_the_limit_are_refused_inside_the_window()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var limiter = new RateLimiter(3, TimeSpan.FromSeconds(10), time);

        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void Capacity_comes_back_once_old_requests_leave_the_window()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var limiter = new RateLimiter(2, TimeSpan.FromSeconds(10), time);
        limiter.TryAcquire();
        time.Advance(TimeSpan.FromSeconds(6));
        limiter.TryAcquire();

        time.Advance(TimeSpan.FromSeconds(4));

        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }
}
