using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Server;

namespace Caelestia.Helper.Modules;

internal sealed partial class ThemeModule : IHelperModule
{
    public const int MaxRoles = 64;
    public const string FileName = "switcher-theme.json";

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
        return Task.CompletedTask;
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
