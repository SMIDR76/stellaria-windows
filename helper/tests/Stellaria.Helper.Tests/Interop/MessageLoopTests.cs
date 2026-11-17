using Stellaria.Helper.Hosting;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Tests.TestSupport;

namespace Stellaria.Helper.Tests.Interop;

public sealed class MessageLoopTests : IAsyncLifetime
{
    private const uint ControlAltShift = 0x0002 | 0x0001 | 0x0004;
    private const uint F24 = 0x87;

    private readonly TemporaryDirectory _directory = new();
    private Log _log = null!;
    private MessageLoop _loop = null!;

    public async ValueTask InitializeAsync()
    {
        _log = new Log(_directory.Path, LogLevel.Debug);
        _loop = new MessageLoop(_log);
        await _loop.StartAsync();
    }

    public ValueTask DisposeAsync()
    {
        _loop.Dispose();
        _log.Dispose();
        _directory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Invoked_work_runs_on_the_loop_thread()
    {
        var callerThread = Environment.CurrentManagedThreadId;

        var loopThread = await _loop.InvokeAsync(() => Environment.CurrentManagedThreadId);

        Assert.NotEqual(callerThread, loopThread);
        Assert.Equal(loopThread, await _loop.InvokeAsync(() => Environment.CurrentManagedThreadId));
    }

    [Fact]
    public async Task Exceptions_thrown_on_the_loop_reach_the_caller()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _loop.InvokeAsync(() => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public void Hotkeys_cannot_be_registered_from_another_thread()
    {
        Assert.Throws<InvalidOperationException>(() => _loop.RegisterHotkey(99, ControlAltShift, F24, () => { }));
    }

    [Fact]
    public async Task A_registered_hotkey_calls_back_on_the_loop_thread()
    {
        var pressed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var registered = await _loop.InvokeAsync(() => _loop.RegisterHotkey(99, ControlAltShift, F24, () => pressed.TrySetResult(Environment.CurrentManagedThreadId)));
        Assert.True(registered, "Ctrl+Alt+Shift+F24 should be free on a test machine.");
        var loopThread = await _loop.InvokeAsync(() => Environment.CurrentManagedThreadId);

        try
        {
            Press(0x11, 0x12, 0x10, (byte)F24);
            var thread = await pressed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(loopThread, thread);
        }
        finally
        {
            await _loop.InvokeAsync(() => _loop.UnregisterHotkey(99));
        }
    }

    private static void Press(params byte[] keys)
    {
        foreach (var key in keys)
        {
            User32.keybd_event(key, 0, 0, 0);
        }

        foreach (var key in keys.Reverse())
        {
            User32.keybd_event(key, 0, User32.KeyEventKeyUp, 0);
        }
    }

    [Fact]
    public async Task Waking_from_sleep_raises_the_resumed_event()
    {
        var resumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _loop.Resumed += () => resumed.TrySetResult();

        Assert.True(User32.PostMessage(_loop.Window, User32.WmPowerBroadcast, User32.PbtResumeAutomatic, 0));

        await resumed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }
}
