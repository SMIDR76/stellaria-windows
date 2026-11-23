using System.Text.Json;
using System.Text.Json.Nodes;

namespace Stellaria.Helper.Desktop;

internal static class FrameEdges
{
    public const int DefaultBorder = 10;
    public const int MinimumBorder = 4;
    public const int MaximumBorder = 24;

    public static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static bool Apply(JsonObject pack, int border)
    {
        border = Math.Clamp(border, MinimumBorder, MaximumBorder);
        if (pack["widgets"] is not JsonArray widgets)
        {
            return false;
        }

        var size = $"{border}px";
        var changed = false;
        foreach (var widget in widgets.OfType<JsonObject>())
        {
            if (widget["name"]?.GetValue<string>() != "edge" || widget["presets"] is not JsonArray presets)
            {
                continue;
            }

            foreach (var preset in presets.OfType<JsonObject>())
            {
                var field = preset["dockToEdge"]?["edge"]?.GetValue<string>() switch
                {
                    "top" or "bottom" => "height",
                    "left" or "right" => "width",
                    _ => null,
                };
                if (field is null || preset[field]?.GetValue<string>() == size)
                {
                    continue;
                }

                preset[field] = size;
                changed = true;
            }
        }

        return changed;
    }
}
