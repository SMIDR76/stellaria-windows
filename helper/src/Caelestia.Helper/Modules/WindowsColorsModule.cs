using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Caelestia.Helper.Hosting;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Server;
using Microsoft.Win32;

namespace Caelestia.Helper.Modules;

/// <summary>Seven shades from lightest to darkest (Windows' Light3 to Dark3) as 0xRRGGBB, plus the modes to apply.</summary>
internal sealed record WindowsColors(uint[] Palette, bool Dark, bool TitleBars)
{
    public uint Accent => Palette[3];
}

/// <summary>
/// Gives Windows the theme's colours: the accent of Settings, File Explorer, the Start menu and the title bars, and
/// light or dark mode for apps. Only the user's personalisation values are touched (the ones Settings &gt;
/// Personalisation &gt; Colours writes); the previous ones are saved first and put back by <c>windows.restoreColors</c>,
/// by <c>--restore-shell</c> and therefore by the uninstaller.
/// </summary>
internal sealed partial class WindowsColorsModule : IHelperModule
{
    public const string BackupFileName = "windows-colors-backup.json";
    public const int Shades = 7;

    private const string AccentKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";
    private const string DwmKey = @"Software\Microsoft\Windows\DWM";
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private readonly Lock _gate = new();
    private string _applied = "";

    public string Name => "windowsColors";

    public Task StartAsync(ModuleContext context)
    {
        var backup = Path.Combine(context.Paths.DataDirectory, BackupFileName);
        context.Router.Register("windows.setColors", (request, _) =>
        {
            var colors = Validate(request);
            var signature = $"{string.Join(',', colors.Palette)}|{colors.Dark}|{colors.TitleBars}";
            lock (_gate)
            {
                if (signature != _applied)
                {
                    if (!File.Exists(backup))
                    {
                        WriteAtomically(backup, Capture().ToJsonString());
                        context.Log.Info("windowsColors", "Previous Windows colours saved.");
                    }

                    Apply(colors);
                    _applied = signature;
                    context.Log.Info("windowsColors", $"Windows colours set (accent #{colors.Accent:x6}, {(colors.Dark ? "dark" : "light")}).");
                }
            }

            return Task.FromResult<JsonNode?>(null);
        });
        context.Router.Register("windows.restoreColors", (_, _) =>
        {
            lock (_gate)
            {
                _applied = "";
                RestoreFromBackup(context.Paths.DataDirectory, context.Log);
            }

            return Task.FromResult<JsonNode?>(null);
        });
        return Task.CompletedTask;
    }

    public static WindowsColors Validate(RequestContext request)
    {
        if (request.Params["palette"] is not JsonArray palette || palette.Count != Shades)
        {
            throw new HelperException(ErrorCode.InvalidParams, $"'palette' must list {Shades} #rrggbb colours, lightest first.");
        }

        var shades = new uint[Shades];
        for (var index = 0; index < Shades; index++)
        {
            if (palette[index] is not JsonValue value || !value.TryGetValue<string>(out var hex) || !HexColour().IsMatch(hex))
            {
                throw new HelperException(ErrorCode.InvalidParams, "Every shade must be a #rrggbb colour.");
            }

            shades[index] = Convert.ToUInt32(hex[1..], 16);
        }

        return new WindowsColors(shades, request.RequireBoolean("dark"), request.RequireBoolean("titleBars"));
    }

    /// <summary>0xRRGGBB to the 0xAABBGGRR order the registry and COLORREF use.</summary>
    public static uint ToAbgr(uint rgb, byte alpha = 0xFF) =>
        ((uint)alpha << 24) | ((rgb & 0xFF) << 16) | (rgb & 0xFF00) | ((rgb >> 16) & 0xFF);

    /// <summary>AccentPalette: eight RGBA entries, the seven shades then the darkest again in the last slot.</summary>
    public static byte[] AccentPalette(uint[] palette)
    {
        var bytes = new byte[32];
        for (var index = 0; index < 8; index++)
        {
            var rgb = palette[Math.Min(index, Shades - 1)];
            bytes[index * 4] = (byte)(rgb >> 16);
            bytes[(index * 4) + 1] = (byte)(rgb >> 8);
            bytes[(index * 4) + 2] = (byte)rgb;
            bytes[(index * 4) + 3] = 0;
        }

        return bytes;
    }

    private static void Apply(WindowsColors colors)
    {
        var accentAbgr = ToAbgr(colors.Accent);
        var startAbgr = ToAbgr(colors.Palette[5]);
        using (var accent = Registry.CurrentUser.CreateSubKey(AccentKey))
        {
            accent.SetValue("AccentPalette", AccentPalette(colors.Palette), RegistryValueKind.Binary);
            accent.SetValue("AccentColorMenu", unchecked((int)accentAbgr), RegistryValueKind.DWord);
            accent.SetValue("StartColorMenu", unchecked((int)startAbgr), RegistryValueKind.DWord);
        }

        using (var dwm = Registry.CurrentUser.CreateSubKey(DwmKey))
        {
            var colorization = unchecked((int)(0xC4000000 | colors.Accent));
            dwm.SetValue("AccentColor", unchecked((int)accentAbgr), RegistryValueKind.DWord);
            dwm.SetValue("ColorizationColor", colorization, RegistryValueKind.DWord);
            dwm.SetValue("ColorizationAfterglow", colorization, RegistryValueKind.DWord);
            dwm.SetValue("ColorPrevalence", colors.TitleBars ? 1 : 0, RegistryValueKind.DWord);
        }

        using (var personalize = Registry.CurrentUser.CreateSubKey(PersonalizeKey))
        {
            personalize.SetValue("AppsUseLightTheme", colors.Dark ? 0 : 1, RegistryValueKind.DWord);
            personalize.SetValue("SystemUsesLightTheme", colors.Dark ? 0 : 1, RegistryValueKind.DWord);
        }

        ImmersiveColors.Write(new ImmersiveColorPreference { StartColor = startAbgr & 0xFFFFFF, AccentColor = accentAbgr & 0xFFFFFF });
        ImmersiveColors.Broadcast();
    }

    private static JsonObject Capture()
    {
        var saved = new JsonObject();
        using (var accent = Registry.CurrentUser.OpenSubKey(AccentKey))
        {
            if (accent?.GetValue("AccentPalette") is byte[] palette)
            {
                saved["accentPalette"] = Convert.ToBase64String(palette);
            }

            saved["accentColorMenu"] = DWord(accent, "AccentColorMenu");
            saved["startColorMenu"] = DWord(accent, "StartColorMenu");
        }

        using (var dwm = Registry.CurrentUser.OpenSubKey(DwmKey))
        {
            saved["accentColor"] = DWord(dwm, "AccentColor");
            saved["colorizationColor"] = DWord(dwm, "ColorizationColor");
            saved["colorizationAfterglow"] = DWord(dwm, "ColorizationAfterglow");
            saved["colorPrevalence"] = DWord(dwm, "ColorPrevalence");
        }

        using (var personalize = Registry.CurrentUser.OpenSubKey(PersonalizeKey))
        {
            saved["appsUseLightTheme"] = DWord(personalize, "AppsUseLightTheme");
            saved["systemUsesLightTheme"] = DWord(personalize, "SystemUsesLightTheme");
        }

        if (ImmersiveColors.Read() is { } preference)
        {
            saved["preferenceStart"] = preference.StartColor;
            saved["preferenceAccent"] = preference.AccentColor;
        }

        return saved;
    }

    /// <summary>Puts back the colours saved before the first change, then forgets them. Does nothing without a backup.</summary>
    public static void RestoreFromBackup(string dataDirectory, Log log)
    {
        var path = Path.Combine(dataDirectory, BackupFileName);
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject saved)
            {
                throw new InvalidDataException("The backup is not a JSON object.");
            }

            using (var accent = Registry.CurrentUser.CreateSubKey(AccentKey))
            {
                if (saved["accentPalette"]?.GetValue<string>() is { } palette)
                {
                    accent.SetValue("AccentPalette", Convert.FromBase64String(palette), RegistryValueKind.Binary);
                }
                else
                {
                    accent.DeleteValue("AccentPalette", throwOnMissingValue: false);
                }

                Put(accent, "AccentColorMenu", saved["accentColorMenu"]);
                Put(accent, "StartColorMenu", saved["startColorMenu"]);
            }

            using (var dwm = Registry.CurrentUser.CreateSubKey(DwmKey))
            {
                Put(dwm, "AccentColor", saved["accentColor"]);
                Put(dwm, "ColorizationColor", saved["colorizationColor"]);
                Put(dwm, "ColorizationAfterglow", saved["colorizationAfterglow"]);
                Put(dwm, "ColorPrevalence", saved["colorPrevalence"]);
            }

            using (var personalize = Registry.CurrentUser.CreateSubKey(PersonalizeKey))
            {
                Put(personalize, "AppsUseLightTheme", saved["appsUseLightTheme"]);
                Put(personalize, "SystemUsesLightTheme", saved["systemUsesLightTheme"]);
            }

            if (saved["preferenceStart"] is JsonValue start && saved["preferenceAccent"] is JsonValue accentColor)
            {
                ImmersiveColors.Write(new ImmersiveColorPreference { StartColor = start.GetValue<uint>(), AccentColor = accentColor.GetValue<uint>() });
            }

            ImmersiveColors.Broadcast();
            File.Delete(path);
            log.Info("windowsColors", "Previous Windows colours restored.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException or System.Text.Json.JsonException or InvalidOperationException)
        {
            log.Warning("windowsColors", "The saved Windows colours could not be restored.", exception);
        }
    }

    private static JsonNode? DWord(RegistryKey? key, string name) =>
        key?.GetValue(name) is int value ? JsonValue.Create(unchecked((uint)value)) : null;

    // A value that did not exist before is removed again, so Windows falls back to its own default.
    private static void Put(RegistryKey key, string name, JsonNode? value)
    {
        if (value is JsonValue number)
        {
            key.SetValue(name, unchecked((int)number.GetValue<uint>()), RegistryValueKind.DWord);
        }
        else
        {
            key.DeleteValue(name, throwOnMissingValue: false);
        }
    }

    private static void WriteAtomically(string path, string text)
    {
        var temporary = $"{path}.tmp";
        File.WriteAllText(temporary, text, new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColour();
}
