using System.Collections.Frozen;

namespace Stellaria.Helper.Security;

internal static class Scopes
{
    public const string Basic = "basic";
    public const string Media = "media";
    public const string Audio = "audio";
    public const string Display = "display";
    public const string Network = "network";
    public const string Windows = "windows";
    public const string Apps = "apps";
    public const string Clipboard = "clipboard";
    public const string Notifications = "notifications";
    public const string Input = "input";
    public const string System = "system";
    public const string Appearance = "appearance";
    public const string Config = "config";
    public const string Launcher = "launcher";

    public static readonly FrozenSet<string> Sensitive = new[] { Windows, Apps, Clipboard, Notifications, Input, System, Config }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> Theme = new[]
    {
        Basic, Media, Audio, Display, Network, Windows, Apps, Clipboard, Notifications, Input, System, Appearance, Config,
    }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> Everything = Theme.Append(Launcher).ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> s_methods = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["system.info"] = Basic,
        ["system.gpu"] = Basic,
        ["wallpapers.list"] = Basic,
        ["widgets.reload"] = Basic,
        ["hotkeys.escape"] = Input,
        ["media.artwork"] = Media,
        ["media.control"] = Media,
        ["audio.sessions"] = Audio,
        ["audio.setDefault"] = Audio,
        ["audio.setSessionMute"] = Audio,
        ["audio.setSessionVolume"] = Audio,
        ["display.brightness"] = Display,
        ["display.setBrightness"] = Display,
        ["radios.list"] = Network,
        ["radios.set"] = Network,
        ["wifi.status"] = Network,
        ["wifi.scan"] = Network,
        ["wifi.connect"] = Network,
        ["wifi.disconnect"] = Network,
        ["bluetooth.devices"] = Network,
        ["bluetooth.discover"] = Network,
        ["bluetooth.pair"] = Network,
        ["bluetooth.unpair"] = Network,
        ["tasks.activate"] = Windows,
        ["tasks.close"] = Windows,
        ["window.act"] = Windows,
        ["window.preview"] = Windows,
        ["shell.taskView"] = Windows,
        ["desktops.switch"] = Windows,
        ["apps.list"] = Apps,
        ["apps.launch"] = Apps,
        ["folders.open"] = Apps,
        ["clipboard.history"] = Clipboard,
        ["clipboard.paste"] = Clipboard,
        ["input.paste"] = Input,
        ["keyboard.setLayout"] = Input,
        ["power.run"] = System,
        ["power.keepAwake"] = System,
        ["capture.record"] = System,
        ["capture.region"] = System,
        ["shell.openSettings"] = System,
        ["theme.publish"] = Appearance,
        ["wallpapers.apply"] = Appearance,
        ["windows.setColors"] = Appearance,
        ["windows.restoreColors"] = Appearance,
        ["config.set"] = Config,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> s_topics = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["config"] = Basic,
        ["monitors"] = Basic,
        ["idle"] = Basic,
        ["power"] = Basic,
        ["shell"] = Basic,
        ["wallpaper"] = Basic,
        ["desktops"] = Basic,
        ["focus"] = Windows,
        ["tasks"] = Windows,
        ["icons"] = Windows,
        ["keyboard"] = Input,
        ["notifications"] = Notifications,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> s_events = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["widgets.reload"] = Basic,
        ["appbars.paused"] = Basic,
        ["appbars.repaired"] = Basic,
        ["hotkey"] = Input,
        ["hotkey.unavailable"] = Input,
        ["notification"] = Notifications,
        ["theme.preset"] = Appearance,
        ["theme.undo"] = Appearance,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static string? ForMethod(string method) =>
        s_methods.GetValueOrDefault(method) ?? (method.StartsWith("catalog.", StringComparison.Ordinal) || method.StartsWith("launcher.", StringComparison.Ordinal) ? Launcher : null);

    public static string? ForTopic(string topic) => s_topics.GetValueOrDefault(topic);

    public static string? ForEvent(string name) => s_events.GetValueOrDefault(name);

    public static bool IsKnown(string scope) => Everything.Contains(scope);
}
