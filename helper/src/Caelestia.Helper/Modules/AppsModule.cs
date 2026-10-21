using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Protocol;

namespace Caelestia.Helper.Modules;

internal sealed class AppsModule : IHelperModule
{
    public static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private readonly SemaphoreSlim _refresh = new(1, 1);
    private List<ShellApp> _apps = [];
    private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;
    private ModuleContext _context = null!;

    public string Name => "apps";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        context.Router.Register("apps.list", async (_, _) =>
        {
            var apps = await AppsAsync(forceRefresh: false).ConfigureAwait(false);
            var list = new JsonArray();
            foreach (var app in apps)
            {
                JsonNode item = new JsonObject
                {
                    ["name"] = app.Name,
                    ["id"] = app.Id,
                    ["icon"] = await context.Icons.GetAsync($"shell:AppsFolder\\{app.Id}").ConfigureAwait(false),
                };
                list.Add(item);
            }

            return list;
        });
        context.Router.Register("apps.launch", async (request, _) =>
        {
            var id = request.RequireString("id");
            if (!await IsKnownAsync(id).ConfigureAwait(false))
            {
                throw new HelperException(ErrorCode.NotFound, "This application is not in the Start menu.");
            }

            if (!ShellExecute.Open($"shell:AppsFolder\\{id}"))
            {
                throw new HelperException(ErrorCode.Failed, "Windows could not start the application.");
            }

            return null;
        });
        return Task.CompletedTask;
    }

    private async Task<bool> IsKnownAsync(string id)
    {
        if ((await AppsAsync(forceRefresh: false).ConfigureAwait(false)).Any(app => app.Id == id))
        {
            return true;
        }

        return (await AppsAsync(forceRefresh: true).ConfigureAwait(false)).Any(app => app.Id == id);
    }

    private async Task<List<ShellApp>> AppsAsync(bool forceRefresh)
    {
        await _refresh.WaitAsync().ConfigureAwait(false);
        try
        {
            if (forceRefresh || _context.Time.GetUtcNow() - _loadedAt > CacheLifetime)
            {
                _apps = await Task.Run(() => AppCatalog.Filter(ShellFolders.StartMenuApps())).ConfigureAwait(false);
                _loadedAt = _context.Time.GetUtcNow();
            }

            return _apps;
        }
        finally
        {
            _refresh.Release();
        }
    }
}
