using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Protocol;
using Windows.ApplicationModel.DataTransfer;

namespace Caelestia.Helper.Modules;

internal sealed class ClipboardModule : IHelperModule
{
    public const int DefaultLimit = 40;
    public const int MaxLimit = 100;
    public const int MaxTextLength = 4096;

    public string Name => "clipboard";

    public Task StartAsync(ModuleContext context)
    {
        context.Router.Register("clipboard.history", async (request, _) =>
        {
            var limit = request.OptionalInteger("limit", DefaultLimit, 1, MaxLimit);
            return await ReadHistoryAsync(context, limit).ConfigureAwait(false);
        });
        context.Router.Register("clipboard.paste", async (request, _) =>
        {
            var id = request.RequireString("id");
            await PasteAsync(context, id).ConfigureAwait(false);
            return null;
        });
        context.Router.Register("input.paste", async (_, _) =>
        {
            if (!await KeyboardInput.SendToWindowAsync(context.Loop, context.Foreground.LastUserWindow, KeyboardInput.Paste).ConfigureAwait(false))
            {
                throw new HelperException(ErrorCode.Failed, "There is no window to paste into.");
            }

            return null;
        });
        return Task.CompletedTask;
    }

    public static string Truncate(string text) => text.Length <= MaxTextLength ? text : text[..MaxTextLength];

    private static async Task<JsonNode?> ReadHistoryAsync(ModuleContext context, int limit)
    {
        var enabled = await context.Loop.InvokeAsync(Clipboard.IsHistoryEnabled).ConfigureAwait(false);
        var items = new JsonArray();
        if (!enabled)
        {
            return new JsonObject { ["enabled"] = false, ["items"] = items };
        }

        var operation = await context.Loop.InvokeAsync(Clipboard.GetHistoryItemsAsync).ConfigureAwait(false);
        var history = await operation;
        if (history.Status != ClipboardHistoryItemsResultStatus.Success)
        {
            throw new HelperException(ErrorCode.Denied, "Windows refused to share the clipboard history.");
        }

        foreach (var item in history.Items)
        {
            if (items.Count >= limit)
            {
                break;
            }

            if (!item.Content.Contains(StandardDataFormats.Text))
            {
                continue;
            }

            JsonNode entry = new JsonObject
            {
                ["id"] = item.Id,
                ["kind"] = "text",
                ["time"] = item.Timestamp.ToUnixTimeMilliseconds(),
                ["text"] = Truncate(await item.Content.GetTextAsync()),
            };
            items.Add(entry);
        }

        return new JsonObject { ["enabled"] = true, ["items"] = items };
    }

    private static async Task PasteAsync(ModuleContext context, string id)
    {
        var operation = await context.Loop.InvokeAsync(Clipboard.GetHistoryItemsAsync).ConfigureAwait(false);
        var history = await operation;
        var item = history.Status == ClipboardHistoryItemsResultStatus.Success ? history.Items.FirstOrDefault(candidate => candidate.Id == id) : null;
        if (item is null)
        {
            throw new HelperException(ErrorCode.NotFound, "This clipboard entry no longer exists.");
        }

        var selected = await context.Loop.InvokeAsync(() => Clipboard.SetHistoryItemAsContent(item)).ConfigureAwait(false);
        if (selected != SetHistoryItemAsContentStatus.Success)
        {
            throw new HelperException(ErrorCode.Failed, "Windows refused to restore the clipboard entry.");
        }

        if (!await KeyboardInput.SendToWindowAsync(context.Loop, context.Foreground.LastUserWindow, KeyboardInput.Paste).ConfigureAwait(false))
        {
            throw new HelperException(ErrorCode.Failed, "The entry is in the clipboard but there is no window to paste into.");
        }
    }
}
