using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Stellaria.Helper.Protocol;
using Stellaria.Helper.Server;

namespace Stellaria.Helper.Modules;

internal sealed partial class ThemeModule : IHelperModule
{
    public const int MaxRoles = 64;
    public const string FileName = "switcher-theme.json";
    public const int MaxOptions = 64;

    private static readonly Dictionary<string, (int Minimum, int Maximum)?> s_settable = new(StringComparer.Ordinal)
    {
        ["frameBorder"] = (4, 24),
        ["frameRounding"] = (0, 40),
        ["activeWindow"] = null,
        ["doNotDisturb"] = null,
        ["clockSeconds"] = null,
        ["tintIcons"] = null,
        ["trayCollapse"] = null,
        ["island"] = null,
        ["desktopClock"] = null,
        ["hotCorner"] = null,
        ["toasts"] = null,
        ["systemToasts"] = null,
        ["idleScreen"] = null,
        ["gameMode"] = null,
        ["hideTaskbar"] = null,
        ["smartScheme"] = null,
        ["windowsColors"] = null,
        ["windowsTitleBars"] = null,
        ["hoverDashboard"] = null,
        ["hoverOsd"] = null,
        ["hoverUtilities"] = null,
        ["hoverPopouts"] = null,
        ["osdOnVolume"] = null,
        ["weather"] = null,
        ["lyrics"] = null,
        ["idleMinutes"] = (1, 60),
        ["batteryAlerts"] = null,
    };

    public const int MaxHiddenApps = 200;
    public const int MaxTextLength = 512;

    private static readonly string[] s_modes = ["dark", "light"];
    private static readonly string[] s_variants = ["tonalspot", "vibrant", "expressive", "fidelity", "content", "fruitsalad", "rainbow", "neutral", "monochrome"];
    private static readonly string[] s_actions = ["wallpaperPicker", "wallpaperFolder"];
    private static readonly string[] s_barEntries = ["logo", "workspaces", "tasks", "activeWindow", "spacer", "tray", "weather", "clock", "status", "power"];
    private static readonly string[] s_statusEntries = ["keyboard", "capsLock", "mic", "volume", "bluetooth", "battery", "network", "notifications"];
    private static readonly string[] s_appPages = ["home", "customize", "customize/appearance", "customize/wallpaper", "customize/bar", "customize/desktop", "customize/panels", "help"];

    private static readonly string[] s_languages = ["auto", "fr", "en", "es", "de", "pt-BR", "it", "pl", "ru", "tr", "zh-CN", "ja", "ko"];

    private JsonObject? _options;

    public string Name => "theme";

    public Task StartAsync(ModuleContext context)
    {
        var path = Path.Combine(context.Paths.DataDirectory, FileName);
        context.Router.Register("theme.publish", (request, _) =>
        {
            var theme = Validate(request);
            var text = theme.ToJsonString();
            if (!File.Exists(path) || File.ReadAllText(path) != text)
            {
                var temporary = $"{path}.tmp";
                File.WriteAllText(temporary, text, new UTF8Encoding(false));
                File.Move(temporary, path, overwrite: true);
            }

            return Task.FromResult<JsonNode?>(null);
        });
        context.Router.Register("theme.options", (request, _) =>
        {
            _options = ValidateOptions(request.Params["options"]);
            return Task.FromResult<JsonNode?>(null);
        });
        context.Router.Register("launcher.options", (_, _) => Task.FromResult<JsonNode?>(_options?.DeepClone()));
        context.Router.Register("launcher.themeAction", (request, _) =>
        {
            var name = request.RequireString("name");
            if (!s_actions.Contains(name, StringComparer.Ordinal))
            {
                throw new HelperException(ErrorCode.InvalidParams, $"'name' must be one of {string.Join(", ", s_actions)}.");
            }

            context.Hub.Raise("theme.action", new JsonObject { ["name"] = name });
            return Task.FromResult<JsonNode?>(null);
        });
        context.Router.Register("stellaria.open", (request, _) =>
        {
            var page = request.Params["page"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : "customize";
            if (!s_appPages.Contains(page, StringComparer.Ordinal))
            {
                throw new HelperException(ErrorCode.InvalidParams, $"'page' must be one of {string.Join(", ", s_appPages)}.");
            }

            var app = Path.Combine(AppContext.BaseDirectory, "stellaria-launcher.exe");
            if (!File.Exists(app))
            {
                throw new HelperException(ErrorCode.NotFound, "The Stellaria app is not installed next to the helper.");
            }

            Stellaria.Helper.Interop.User32.AllowSetForegroundWindow(-1);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(app, $"stellaria://page/{page}") { UseShellExecute = false })?.Dispose();
            return Task.FromResult<JsonNode?>(null);
        });
        context.Router.Register("launcher.setOption", (request, _) =>
        {
            var name = request.RequireString("name");
            var value = ValidateSetting(name, request.Params["value"]);
            if (_options is not null)
            {
                _options[name] = value.DeepClone();
            }

            context.Hub.Raise("theme.option", new JsonObject { ["name"] = name, ["value"] = value });
            return Task.FromResult<JsonNode?>(_options?.DeepClone());
        });
        return Task.CompletedTask;
    }

    public static JsonObject ValidateOptions(JsonNode? node)
    {
        if (node is not JsonObject options || options.Count > MaxOptions)
        {
            throw new HelperException(ErrorCode.InvalidParams, $"'options' must map at most {MaxOptions} option names to values.");
        }

        var clean = new JsonObject();
        foreach (var (name, value) in options)
        {
            var valid = RoleName().IsMatch(name) && value switch
            {
                JsonValue json => json.TryGetValue<bool>(out _) || json.TryGetValue<int>(out _) || (json.TryGetValue<string>(out var text) && text.Length <= MaxTextLength),
                JsonArray list => list.Count <= MaxHiddenApps && list.All(entry =>
                    (entry is JsonValue word && word.TryGetValue<string>(out var id) && id.Length <= 32) ||
                    (entry is JsonObject app && app.Count <= 2 && app.All(field => field.Key is "id" or "name" && field.Value is JsonValue part && part.TryGetValue<string>(out var piece) && piece.Length <= MaxTextLength))),
                _ => false,
            };
            if (!valid)
            {
                throw new HelperException(ErrorCode.InvalidParams, "Every option must be a short name mapped to true, false, an integer, a short text or a list of applications.");
            }

            clean[name] = value!.DeepClone();
        }

        return clean;
    }

    public static JsonNode ValidateSetting(string name, JsonNode? value)
    {
        switch (name)
        {
            case "mode":
                return Choice(name, value, s_modes);
            case "variant":
                return Choice(name, value, s_variants);
            case "scheme":
                if (value is JsonValue scheme && scheme.TryGetValue<string>(out var id) && (id.Length == 0 || SchemeId().IsMatch(id)))
                {
                    return JsonValue.Create(id);
                }

                throw new HelperException(ErrorCode.InvalidParams, "'scheme' must be empty (dynamic colours) or name/flavour.");
            case "wallpaperDirectory":
                if (value is JsonValue folder && folder.TryGetValue<string>(out var path) && path.Length <= 260 && (path.Length == 0 || DrivePath().IsMatch(path)) && !path.Contains("..", StringComparison.Ordinal))
                {
                    return JsonValue.Create(path);
                }

                throw new HelperException(ErrorCode.InvalidParams, "'wallpaperDirectory' must be empty or a full path such as C:\\Pictures.");
            case "barOrder":
                return Arrangement(name, value, s_barEntries, complete: true);
            case "barHidden":
                return Arrangement(name, value, s_barEntries.Where(entry => entry != "spacer").ToArray(), complete: false);
            case "statusOrder":
                return Arrangement(name, value, s_statusEntries, complete: true);
            case "statusHidden":
                return Arrangement(name, value, s_statusEntries, complete: false);
            case "hiddenApps":
                if (value is JsonArray ids && ids.Count <= MaxHiddenApps && ids.All(entry => entry is JsonValue item && item.TryGetValue<string>(out var text) && text.Length is > 0 and <= MaxTextLength))
                {
                    return ids.DeepClone();
                }

                throw new HelperException(ErrorCode.InvalidParams, $"'hiddenApps' must be a list of at most {MaxHiddenApps} application identifiers.");
        }

        if (name == "language")
        {
            if (value is JsonValue choice && choice.TryGetValue<string>(out var code) && s_languages.Contains(code, StringComparer.Ordinal))
            {
                return JsonValue.Create(code);
            }

            throw new HelperException(ErrorCode.InvalidParams, $"'language' must be one of {string.Join(", ", s_languages)}.");
        }

        if (!s_settable.TryGetValue(name, out var range))
        {
            throw new HelperException(ErrorCode.InvalidParams, $"'{name}' cannot be changed from the app.");
        }

        if (range is { } limits)
        {
            if (value is JsonValue json && json.TryGetValue<int>(out var number) && number >= limits.Minimum && number <= limits.Maximum)
            {
                return JsonValue.Create(number);
            }

            throw new HelperException(ErrorCode.InvalidParams, $"'{name}' must be an integer between {limits.Minimum} and {limits.Maximum}.");
        }

        if (value is JsonValue flag && flag.TryGetValue<bool>(out var enabled))
        {
            return JsonValue.Create(enabled);
        }

        throw new HelperException(ErrorCode.InvalidParams, $"'{name}' must be true or false.");
    }

    public static JsonObject Validate(RequestContext request)
    {
        if (request.Params["roles"] is not JsonObject roles || roles.Count is 0 or > MaxRoles)
        {
            throw new HelperException(ErrorCode.InvalidParams, $"'roles' must map 1 to {MaxRoles} role names to colours.");
        }

        var clean = new JsonObject();
        foreach (var (role, value) in roles)
        {
            if (!RoleName().IsMatch(role) || value is not JsonValue colour || !colour.TryGetValue<string>(out var hex) || !HexColour().IsMatch(hex))
            {
                throw new HelperException(ErrorCode.InvalidParams, "Every role must be a short name mapped to a #rrggbb colour.");
            }

            clean[role] = hex.ToLowerInvariant();
        }

        return new JsonObject { ["roles"] = clean, ["transparent"] = request.RequireBoolean("transparent") };
    }

    private static JsonNode Arrangement(string name, JsonNode? value, string[] allowed, bool complete)
    {
        if (value is JsonArray list && list.All(entry => entry is JsonValue item && item.TryGetValue<string>(out var id) && allowed.Contains(id, StringComparer.Ordinal)))
        {
            var ids = list.Select(entry => entry!.GetValue<string>()).ToList();
            if (ids.Distinct(StringComparer.Ordinal).Count() == ids.Count && (!complete || ids.Count == allowed.Length))
            {
                return new JsonArray([.. ids.Select(id => (JsonNode?)JsonValue.Create(id))]);
            }
        }

        throw new HelperException(ErrorCode.InvalidParams, complete
            ? $"'{name}' must list each of {string.Join(", ", allowed)} exactly once."
            : $"'{name}' must be a list of distinct entries among {string.Join(", ", allowed)}.");
    }

    private static JsonNode Choice(string name, JsonNode? value, string[] allowed)
    {
        if (value is JsonValue choice && choice.TryGetValue<string>(out var text) && allowed.Contains(text, StringComparer.Ordinal))
        {
            return JsonValue.Create(text);
        }

        throw new HelperException(ErrorCode.InvalidParams, $"'{name}' must be one of {string.Join(", ", allowed)}.");
    }

    [GeneratedRegex("^[A-Za-z]{1,32}$")]
    private static partial Regex RoleName();

    [GeneratedRegex("^[a-z0-9-]{1,40}/[a-z0-9-]{1,40}$")]
    private static partial Regex SchemeId();

    [GeneratedRegex(@"^[A-Za-z]:\\")]
    private static partial Regex DrivePath();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColour();
}
