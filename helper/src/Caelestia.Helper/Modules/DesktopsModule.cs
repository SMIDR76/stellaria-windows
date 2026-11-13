using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Protocol;
using Microsoft.Win32;

namespace Caelestia.Helper.Modules;

internal sealed record DesktopSnapshot(IReadOnlyList<(Guid Id, string? Name)> Items, int Current)
{
    public JsonObject ToJson()
    {
        var items = new JsonArray();
        foreach (var (id, name) in Items)
        {
            items.Add((JsonNode)new JsonObject { ["id"] = id.ToString("D"), ["name"] = name });
        }

        return new JsonObject { ["items"] = items, ["current"] = Current };
    }
}

/// <summary>
/// Publishes the Windows virtual desktops, so the bar can show them where a tiling window manager would show its
/// workspaces. Windows has no public API to list or switch desktops: the list is read from the registry Explorer keeps
/// up to date, and switching sends the documented Ctrl+Win+Left/Right shortcuts.
/// </summary>
internal sealed class DesktopsModule : IHelperModule
{
    public const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops";
    public const int MaxDesktops = 32;
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(400);
    public static readonly TimeSpan SwitchStepDelay = TimeSpan.FromMilliseconds(80);

    private const byte Left = 0x25;
    private const byte Right = 0x27;

    private readonly Lock _gate = new();
    private string _published = "";
    private Timer? _poll;

    public string Name => "desktops";

    public Task StartAsync(ModuleContext context)
    {
        Publish(context);
        _poll = new Timer(_ => Publish(context), null, PollInterval, PollInterval);
        context.Shutdown.Register(() => _poll.Dispose());
        context.Router.Register("desktops.switch", async (request, _) =>
        {
            var target = request.RequireInteger("index", 0, MaxDesktops - 1);
            var snapshot = Read();
            if (target >= snapshot.Items.Count)
            {
                throw new HelperException(ErrorCode.NotFound, "There is no desktop at this index.");
            }

            var steps = target - snapshot.Current;
            byte[] chord = [KeyboardInput.Control, KeyboardInput.LeftWindows, steps < 0 ? Left : Right];
            for (var step = 0; step < Math.Abs(steps); step++)
            {
                await context.Loop.InvokeAsync(() => KeyboardInput.Press(chord)).ConfigureAwait(false);
                await Task.Delay(SwitchStepDelay).ConfigureAwait(false);
            }

            Publish(context);
            return null;
        });
        return Task.CompletedTask;
    }

    public static DesktopSnapshot Parse(byte[]? ids, byte[]? current, Func<Guid, string?> nameOf)
    {
        var items = new List<(Guid, string?)>();
        if (ids is { Length: > 0 } && ids.Length % 16 == 0)
        {
            for (var offset = 0; offset < ids.Length && items.Count < MaxDesktops; offset += 16)
            {
                var id = new Guid(ids.AsSpan(offset, 16));
                items.Add((id, nameOf(id)));
            }
        }

        if (items.Count == 0)
        {
            return new DesktopSnapshot([(Guid.Empty, null)], 0);
        }

        var index = current is { Length: 16 } ? items.FindIndex(item => item.Item1 == new Guid(current)) : -1;
        return new DesktopSnapshot(items, Math.Max(0, index));
    }

    private static DesktopSnapshot Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        var ids = key?.GetValue("VirtualDesktopIDs") as byte[];
        var current = key?.GetValue("CurrentVirtualDesktop") as byte[];
        return Parse(ids, current, id =>
        {
            using var desktop = key?.OpenSubKey($@"Desktops\{id:B}");
            return desktop?.GetValue("Name") as string is { Length: > 0 } name ? name : null;
        });
    }

    private void Publish(ModuleContext context)
    {
        try
        {
            var data = Read().ToJson();
            var text = data.ToJsonString();
            lock (_gate)
            {
                if (text == _published)
                {
                    return;
                }

                _published = text;
            }

            context.Hub.Publish("desktops", data);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            context.Log.Warning("desktops", "The virtual desktop list could not be read.", exception);
        }
    }
}
