using System.Globalization;
using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Modules;

internal sealed class KeyboardModule : IHelperModule
{
    public const int CapsLockKey = 0x14;
    public const int NumLockKey = 0x90;
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(150);

    private ModuleContext _context = null!;
    private Timer? _poll;
    private List<KeyboardLayout> _layouts = [];

    public string Name => "keyboard";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        context.Router.Register("keyboard.setLayout", async (request, _) =>
        {
            var id = request.RequireString("id");
            await context.Loop.InvokeAsync(() => SetLayout(id)).ConfigureAwait(false);
            return null;
        });
        context.Loop.BroadcastReceived += message =>
        {
            if (message == User32.WmSettingChange)
            {
                _layouts = KeyboardLayouts.Installed();
                Publish();
            }
        };
        _poll = new Timer(_ => context.Loop.Post(Publish), null, PollInterval, PollInterval);
        context.Shutdown.Register(() => _poll.Dispose());
        return context.Loop.InvokeAsync(() =>
        {
            _layouts = KeyboardLayouts.Installed();
            Publish();
        });
    }

    public static bool IsToggled(short keyState) => (keyState & 1) != 0;

    private void Publish()
    {
        var layouts = new JsonArray();
        foreach (var layout in _layouts)
        {
            JsonNode item = new JsonObject { ["id"] = layout.Id, ["languageTag"] = layout.LanguageTag, ["name"] = layout.Name };
            layouts.Add(item);
        }

        _context.Hub.Publish("keyboard", new JsonObject
        {
            ["capsLock"] = IsToggled(User32.GetKeyState(CapsLockKey)),
            ["numLock"] = IsToggled(User32.GetKeyState(NumLockKey)),
            ["layout"] = KeyboardLayouts.CurrentFor(_context.Foreground.LastUserWindow),
            ["layouts"] = layouts,
        });
    }

    private void SetLayout(string id)
    {
        if (!_layouts.Any(layout => layout.Id == id))
        {
            throw new HelperException(ErrorCode.NotFound, "This keyboard layout is not installed.");
        }

        var window = _context.Foreground.LastUserWindow;
        if (window == 0)
        {
            throw new HelperException(ErrorCode.Failed, "There is no window to switch the layout of.");
        }

        var hkl = long.Parse(id, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        if (!User32.PostMessage(window, User32.WmInputLangChangeRequest, 0, (nint)hkl))
        {
            throw new HelperException(ErrorCode.Failed, "Windows refused the layout change.");
        }
    }
}
