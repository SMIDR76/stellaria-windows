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
    };

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
            if (!RoleName().IsMatch(name) || value is not JsonValue json || !(json.TryGetValue<bool>(out _) || json.TryGetValue<int>(out _)))
            {
                throw new HelperException(ErrorCode.InvalidParams, "Every option must be a short name mapped to true, false or an integer.");
            }

            clean[name] = value.DeepClone();
        }

        return clean;
    }

    public static JsonNode ValidateSetting(string name, JsonNode? value)
    {
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

    [GeneratedRegex("^[A-Za-z]{1,32}$")]
    private static partial Regex RoleName();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColour();
}
