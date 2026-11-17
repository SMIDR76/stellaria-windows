using System.Text.Json.Nodes;
using Stellaria.Helper.Desktop;
using Stellaria.Helper.Interop;

namespace Stellaria.Helper.Modules;

internal sealed record Hotkey(int Id, string Name, uint Modifiers, uint Key, string Shortcut, string? FocusWindowTitle);

internal sealed class HotkeysModule : IHelperModule
{
    public const uint Alt = 0x0001;
    public const uint Win = 0x0008;
    public static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan FocusTimeout = TimeSpan.FromMilliseconds(2500);
    public static readonly TimeSpan FocusPollInterval = TimeSpan.FromMilliseconds(25);

    public static readonly IReadOnlyList<Hotkey> Shortcuts =
    [
        new(1, "wallpapers", Alt, 0x45, "Alt+E", "Zebar - stellaria / launcher"),
        new(2, "launcher", Alt, 0x20, "Alt+Space", "Zebar - stellaria / launcher"),
        new(3, "dashboard", Win | Alt, 0x41, "Win+Alt+A", null),
        new(4, "session", Win | Alt, 0x50, "Win+Alt+P", "Zebar - stellaria / session"),
        new(5, "utilities", Win | Alt, 0x55, "Win+Alt+U", null),
        new(6, "clipboard", Win | Alt, 0x56, "Win+Alt+V", "Zebar - stellaria / launcher"),
        new(7, "emoji", Win | Alt, 0xBE, "Win+Alt+.", "Zebar - stellaria / launcher"),
        new(8, "settings", Win | Alt, 0x53, "Win+Alt+S", "Zebar - stellaria / settings"),
        new(9, "shortcuts", Win | Alt, 0x4B, "Win+Alt+K", "Zebar - stellaria / launcher"),
    ];

    private readonly HashSet<int> _registered = [];
    private readonly HashSet<int> _reported = [];
    private ModuleContext _context = null!;
    private Timer? _retry;

    public string Name => "hotkeys";

    public static readonly TimeSpan WinTapMaximumHold = TimeSpan.FromMilliseconds(400);

    public async Task StartAsync(ModuleContext context)
    {
        _context = context;
        _retry = new Timer(_ => context.Loop.Post(RegisterMissing), null, RetryInterval, RetryInterval);
        context.Shutdown.Register(() => _retry.Dispose());
        await context.Loop.InvokeAsync(RegisterMissing).ConfigureAwait(false);

        var tap = new WinKeyTap(WinTapMaximumHold);
        var launcher = Shortcuts.Single(hotkey => hotkey.Name == "launcher");
        await context.Loop.InvokeAsync(() => context.Loop.HookKeyboard((key, down, injected) =>
        {
            if (injected || !tap.Handle(key, down, Environment.TickCount64) || !context.Config.GetFlag("launcher.winKey"))
            {
                return false;
            }

            KeyboardInput.MaskWindowsKey();
            Pressed(launcher, "Windows key");
            return false;
        })).ConfigureAwait(false);
    }

    private void RegisterMissing()
    {
        foreach (var hotkey in Shortcuts.Where(hotkey => !_registered.Contains(hotkey.Id)))
        {
            if (_context.Loop.RegisterHotkey(hotkey.Id, hotkey.Modifiers, hotkey.Key, () => Pressed(hotkey, hotkey.Shortcut)))
            {
                _registered.Add(hotkey.Id);
                continue;
            }

            if (_reported.Add(hotkey.Id))
            {
                _context.Log.Warning("hotkeys", $"{hotkey.Shortcut} is already used by another program, retrying every {RetryInterval.TotalSeconds} seconds.");
                _context.Hub.Raise("hotkey.unavailable", new JsonObject { ["name"] = hotkey.Name, ["shortcut"] = hotkey.Shortcut });
            }
        }
    }

    private void Pressed(Hotkey hotkey, string source)
    {
        _context.Log.Info("hotkeys", $"{hotkey.Name} opened by {source}.");
        _context.Hub.Raise("hotkey", new JsonObject { ["name"] = hotkey.Name, ["monitor"] = DesktopReader.MonitorUnderCursor() });
        if (hotkey.FocusWindowTitle is { } title)
        {
            _ = FocusWhenShownAsync(title);
        }
    }

    private async Task FocusWhenShownAsync(string title)
    {
        var deadline = _context.Time.GetUtcNow() + FocusTimeout;
        while (_context.Time.GetUtcNow() < deadline && !_context.Shutdown.IsCancellationRequested)
        {
            var focused = await _context.Loop.InvokeAsync(() =>
            {
                var window = User32.FindWindow(null, title);
                return window != 0 && (User32.GetForegroundWindow() == window || User32.SetForegroundWindow(window));
            }).ConfigureAwait(false);
            if (focused)
            {
                return;
            }

            await Task.Delay(FocusPollInterval).ConfigureAwait(false);
        }
    }
}
