using System.Text.Json.Nodes;
using Stellaria.Helper.Desktop;
using Stellaria.Helper.Interop;
using Stellaria.Helper.Protocol;

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

    public const int EscapeId = 20;
    public const uint EscapeKey = 0x1B;
    public const int MaximumOwnerLength = 64;
    public static readonly TimeSpan EscapeLease = TimeSpan.FromMinutes(2);

    private readonly HashSet<int> _registered = [];
    private readonly HashSet<int> _reported = [];
    private readonly HashSet<string> _escapeOwners = new(StringComparer.Ordinal);
    private DateTimeOffset _escapeSince;
    private ModuleContext _context = null!;
    private Timer? _retry;

    public string Name => "hotkeys";

    public static readonly TimeSpan WinTapMaximumHold = TimeSpan.FromMilliseconds(400);

    public async Task StartAsync(ModuleContext context)
    {
        _context = context;
        _retry = new Timer(_ => context.Loop.Post(() =>
        {
            RegisterMissing();
            ExpireEscape();
        }), null, RetryInterval, RetryInterval);
        context.Shutdown.Register(() => _retry.Dispose());
        context.Router.Register("hotkeys.escape", async (request, _) =>
        {
            var owner = request.RequireString("owner");
            if (owner.Length > MaximumOwnerLength)
            {
                throw new HelperException(ErrorCode.InvalidParams, "'owner' is too long.");
            }

            var active = request.RequireBoolean("active");
            await context.Loop.InvokeAsync(() => SetEscape($"{request.Widget}:{owner}", active)).ConfigureAwait(false);
            return null;
        });
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

    private void SetEscape(string owner, bool active)
    {
        if (active)
        {
            _escapeOwners.Add(owner);
        }
        else
        {
            _escapeOwners.Remove(owner);
        }

        var registered = _registered.Contains(EscapeId);
        if (_escapeOwners.Count > 0 && !registered)
        {
            if (_context.Loop.RegisterHotkey(EscapeId, 0, EscapeKey, EscapePressed))
            {
                _registered.Add(EscapeId);
                _escapeSince = _context.Time.GetUtcNow();
            }
        }
        else if (_escapeOwners.Count == 0 && registered)
        {
            ReleaseEscape();
        }
    }

    private void EscapePressed()
    {
        ReleaseEscape();
        _context.Hub.Raise("hotkey", new JsonObject { ["name"] = "escape", ["monitor"] = DesktopReader.MonitorUnderCursor() });
    }

    private void ExpireEscape()
    {
        if (_registered.Contains(EscapeId) && _context.Time.GetUtcNow() - _escapeSince > EscapeLease)
        {
            ReleaseEscape();
        }
    }

    private void ReleaseEscape()
    {
        _escapeOwners.Clear();
        _context.Loop.UnregisterHotkey(EscapeId);
        _registered.Remove(EscapeId);
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
