using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Caelestia.Helper.Hosting;

namespace Caelestia.Helper.Interop;

internal sealed unsafe class MessageLoop : IDisposable
{
    private const string WindowClass = "Caelestia.Helper.MessageWindow";
    private const uint RunQueuedActions = User32.WmApp + 1;

    private static MessageLoop? s_current;

    private readonly Log _log;
    private readonly ConcurrentQueue<Action> _actions = new();
    private readonly List<nint> _hooks = [];
    private readonly Dictionary<int, Action> _hotkeys = [];
    private Func<uint, bool, bool, bool>? _keyboard;
    private nint _keyboardHook;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;
    private nint _window;
    private uint _threadId;

    public MessageLoop(Log log)
    {
        _log = log;
        _thread = new Thread(Run) { IsBackground = true, Name = "Caelestia message loop" };
        _thread.SetApartmentState(ApartmentState.STA);
    }

    public event Action<uint, nint, int, int>? WinEvent;

    public event Action<uint, nuint>? BroadcastReceived;

    public event Action? SessionEnding;

    public Task StartAsync()
    {
        _thread.Start();
        return _ready.Task;
    }

    public void Post(Action action)
    {
        _actions.Enqueue(action);
        if (_window != 0)
        {
            User32.PostMessage(_window, RunQueuedActions, 0, 0);
        }
    }

    public Task InvokeAsync(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        return completion.Task;
    }

    public Task<T> InvokeAsync<T>(Func<T> function)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                completion.SetResult(function());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        return completion.Task;
    }

    public void HookWinEvents(uint eventMin, uint eventMax)
    {
        EnsureOnLoop();
        var hook = User32.SetWinEventHook(eventMin, eventMax, 0, &OnWinEvent, 0, 0, WinEvents.OutOfContext | WinEvents.SkipOwnProcess);
        if (hook == 0)
        {
            throw new InvalidOperationException($"SetWinEventHook failed for events 0x{eventMin:X}-0x{eventMax:X}.");
        }

        _hooks.Add(hook);
    }

    /// <summary>
    /// Installs a low level keyboard hook. The handler receives the virtual key, whether it is a press and whether the
    /// event was injected by a program, and returns true to swallow the key. It runs on the loop thread and must be fast:
    /// Windows drops hooks that take longer than about a second.
    /// </summary>
    public void HookKeyboard(Func<uint, bool, bool, bool> handler)
    {
        EnsureOnLoop();
        _keyboard = handler;
        if (_keyboardHook != 0)
        {
            return;
        }

        _keyboardHook = User32.SetWindowsHookEx(User32.WhKeyboardLowLevel, &OnLowLevelKeyboard, Kernel32.GetModuleHandle(null), 0);
        if (_keyboardHook == 0)
        {
            throw new InvalidOperationException("SetWindowsHookEx failed for the keyboard hook.");
        }
    }

    public bool RegisterHotkey(int id, uint modifiers, uint virtualKey, Action pressed)
    {
        EnsureOnLoop();
        if (!User32.RegisterHotKey(_window, id, modifiers | User32.ModNoRepeat, virtualKey))
        {
            return false;
        }

        _hotkeys[id] = pressed;
        return true;
    }

    public void UnregisterHotkey(int id)
    {
        EnsureOnLoop();
        if (_hotkeys.Remove(id))
        {
            User32.UnregisterHotKey(_window, id);
        }
    }

    public void Dispose()
    {
        if (_threadId != 0)
        {
            User32.PostThreadMessage(_threadId, User32.WmQuit, 0, 0);
            _thread.Join(TimeSpan.FromSeconds(5));
        }
    }

    private void EnsureOnLoop()
    {
        if (Environment.CurrentManagedThreadId != _thread.ManagedThreadId)
        {
            throw new InvalidOperationException("This call must run on the message loop thread. Use Post or InvokeAsync.");
        }
    }

    private void Run()
    {
        if (Interlocked.CompareExchange(ref s_current, this, null) is not null)
        {
            _ready.SetException(new InvalidOperationException("Only one message loop can run at a time in a process."));
            return;
        }

        try
        {
            _threadId = Kernel32.GetCurrentThreadId();
            _window = CreateMessageWindow();
            _ready.SetResult();
            while (User32.GetMessage(out var message, 0, 0, 0) > 0)
            {
                User32.TranslateMessage(message);
                User32.DispatchMessage(message);
            }
        }
        catch (Exception exception)
        {
            _log.Error("message-loop", "Message loop crashed.", exception);
            _ready.TrySetException(exception);
        }
        finally
        {
            foreach (var hook in _hooks)
            {
                User32.UnhookWinEvent(hook);
            }

            if (_keyboardHook != 0)
            {
                User32.UnhookWindowsHookEx(_keyboardHook);
            }

            foreach (var id in _hotkeys.Keys)
            {
                User32.UnregisterHotKey(_window, id);
            }

            if (_window != 0)
            {
                User32.DestroyWindow(_window);
            }

            s_current = null;
        }
    }

    private static nint CreateMessageWindow()
    {
        var instance = Kernel32.GetModuleHandle(null);
        fixed (char* className = WindowClass)
        {
            var windowClass = new WndClassEx
            {
                Size = sizeof(WndClassEx),
                WndProc = &WindowProcedure,
                Instance = instance,
                ClassName = className,
            };
            if (User32.RegisterClassEx(&windowClass) == 0 && Marshal.GetLastPInvokeError() != 1410)
            {
                throw new InvalidOperationException("RegisterClassEx failed for the message window.");
            }
        }

        var window = User32.CreateWindowEx(WsExToolWindowStyle, WindowClass, "Caelestia helper", WsPopupStyle, 0, 0, 0, 0, 0, 0, instance, 0);
        if (window == 0)
        {
            throw new InvalidOperationException("CreateWindowEx failed for the message window.");
        }

        return window;
    }

    private const uint WsPopupStyle = 0x80000000;
    private const uint WsExToolWindowStyle = User32.WsExToolWindow;

    private void RunActions()
    {
        while (_actions.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                _log.Error("message-loop", "A queued action failed.", exception);
            }
        }
    }

    [UnmanagedCallersOnly]
    private static nint WindowProcedure(nint hwnd, uint message, nuint wParam, nint lParam)
    {
        var loop = s_current;
        if (loop is null)
        {
            return User32.DefWindowProc(hwnd, message, wParam, lParam);
        }

        try
        {
            switch (message)
            {
                case RunQueuedActions:
                    loop.RunActions();
                    return 0;
                case User32.WmHotkey when loop._hotkeys.TryGetValue((int)wParam, out var pressed):
                    User32.AllowSetForegroundWindow(-1);
                    pressed();
                    return 0;
                case User32.WmSettingChange:
                case User32.WmDisplayChange:
                    loop.BroadcastReceived?.Invoke(message, wParam);
                    break;
                case User32.WmQueryEndSession:
                    return 1;
                case User32.WmEndSession when wParam != 0:
                    loop.SessionEnding?.Invoke();
                    return 0;
            }
        }
        catch (Exception exception)
        {
            loop._log.Error("message-loop", $"Handling message 0x{message:X} failed.", exception);
        }

        return User32.DefWindowProc(hwnd, message, wParam, lParam);
    }

    [UnmanagedCallersOnly]
    private static nint OnLowLevelKeyboard(int code, nuint wParam, nint lParam)
    {
        var loop = s_current;
        if (code >= 0 && loop?._keyboard is { } handler)
        {
            var data = (KeyboardHookData*)lParam;
            var down = wParam is KeyDown or SystemKeyDown;
            try
            {
                if (handler(data->VirtualKey, down, (data->Flags & User32.KeyboardHookInjected) != 0))
                {
                    return 1;
                }
            }
            catch (Exception exception)
            {
                loop._log.Error("message-loop", "Handling a key event failed.", exception);
            }
        }

        return User32.CallNextHookEx(0, code, wParam, lParam);
    }

    private const nuint KeyDown = 0x0100;
    private const nuint SystemKeyDown = 0x0104;

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardHookData
    {
        public uint VirtualKey;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [UnmanagedCallersOnly]
    private static void OnWinEvent(nint hook, uint eventType, nint hwnd, int objectId, int childId, uint eventThread, uint eventTime)
    {
        var loop = s_current;
        if (loop is null)
        {
            return;
        }

        try
        {
            loop.WinEvent?.Invoke(eventType, hwnd, objectId, childId);
        }
        catch (Exception exception)
        {
            loop._log.Error("message-loop", $"Handling window event 0x{eventType:X} failed.", exception);
        }
    }
}

internal static class WinEvents
{
    public const uint OutOfContext = 0x0000;
    public const uint SkipOwnProcess = 0x0002;
    public const uint SystemForeground = 0x0003;
    public const uint SystemMinimizeStart = 0x0016;
    public const uint SystemMinimizeEnd = 0x0017;
    public const uint ObjectCreate = 0x8000;
    public const uint ObjectHide = 0x8003;
    public const uint ObjectLocationChange = 0x800B;
    public const uint ObjectNameChange = 0x800C;
    public const uint ObjectCloaked = 0x8017;
    public const uint ObjectUncloaked = 0x8018;
    public const int ObjectIdWindow = 0;
}
