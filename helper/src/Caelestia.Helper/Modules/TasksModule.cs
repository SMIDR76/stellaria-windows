using System.Globalization;
using System.Text.Json.Nodes;
using Caelestia.Helper.Desktop;
using Caelestia.Helper.Interop;
using Caelestia.Helper.Protocol;
using Caelestia.Helper.Server;

namespace Caelestia.Helper.Modules;

internal sealed class TasksModule : IHelperModule
{
    public static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(120);

    private List<nint> _order = [];
    private HashSet<nint> _tasks = [];
    private HashSet<string> _publishedIconSources = new(StringComparer.OrdinalIgnoreCase);
    private ModuleContext _context = null!;
    private Debouncer _refresh = null!;

    public string Name => "tasks";

    public Task StartAsync(ModuleContext context)
    {
        _context = context;
        _refresh = new Debouncer(context.Loop, RefreshDelay, Refresh);
        context.Loop.WinEvent += (_, _, objectId, childId) =>
        {
            if (DesktopEventHooks.IsWindowEvent(objectId, childId))
            {
                _refresh.Schedule();
            }
        };
        context.Router.Register("tasks.activate", (request, _) => RunOnWindowAsync(request, Activate));
        context.Router.Register("tasks.close", (request, _) => RunOnWindowAsync(request, hwnd => User32.PostMessage(hwnd, User32.WmClose, 0, 0)));
        return context.Loop.InvokeAsync(Refresh);
    }

    public static nint ParseId(string id) =>
        long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? (nint)value
            : throw new HelperException(ErrorCode.InvalidParams, "Task id must be a positive window handle.");

    private async Task<JsonNode?> RunOnWindowAsync(RequestContext request, Func<nint, bool> action)
    {
        var hwnd = ParseId(request.RequireString("id"));
        var done = await _context.Loop.InvokeAsync(() =>
        {
            if (!_tasks.Contains(hwnd) || !User32.IsWindow(hwnd))
            {
                throw new HelperException(ErrorCode.NotFound, "This window is not an open task.");
            }

            return action(hwnd);
        }).ConfigureAwait(false);
        if (!done)
        {
            throw new HelperException(ErrorCode.Failed, "Windows refused the window action.");
        }

        _refresh.Schedule();
        return null;
    }

    private static bool Activate(nint hwnd)
    {
        var foreground = User32.GetForegroundWindow();
        if (foreground == hwnd && !User32.IsIconic(hwnd))
        {
            return User32.ShowWindow(hwnd, User32.SwMinimize);
        }

        if (User32.IsIconic(hwnd))
        {
            User32.ShowWindow(hwnd, User32.SwRestore);
        }

        if (User32.SetForegroundWindow(hwnd) && User32.GetForegroundWindow() == hwnd)
        {
            return true;
        }

        var foregroundThread = User32.GetWindowThreadProcessId(foreground, out _);
        var currentThread = Kernel32.GetCurrentThreadId();
        var attached = foregroundThread != 0 && foregroundThread != currentThread && User32.AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            User32.BringWindowToTop(hwnd);
            return User32.SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached)
            {
                User32.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }

    private void Refresh()
    {
        var foreground = User32.GetForegroundWindow();
        var candidates = new List<(nint Handle, WindowFacts Facts)>();
        foreach (var hwnd in User32.TopLevelWindows())
        {
            if (!User32.IsWindowVisible(hwnd))
            {
                continue;
            }

            var facts = _context.Desktop.Read(hwnd);
            if (WindowRules.IsTask(facts))
            {
                candidates.Add((hwnd, facts));
            }
        }

        _order = TaskOrder.Merge(_order, [.. candidates.Select(candidate => candidate.Handle)]);
        _tasks = [.. _order];
        var factsByWindow = candidates.ToDictionary(candidate => candidate.Handle, candidate => candidate.Facts);
        var items = new JsonArray();
        var exes = new List<string>();
        foreach (var hwnd in _order)
        {
            var process = _context.Desktop.Processes.Find(_context.Desktop.TaskProcessId(hwnd));
            var exe = process?.ImagePath ?? "";
            exes.Add(exe);
            JsonNode item = new JsonObject
            {
                ["id"] = ((long)hwnd).ToString(CultureInfo.InvariantCulture),
                ["title"] = factsByWindow[hwnd].Title,
                ["process"] = process?.Name ?? factsByWindow[hwnd].ProcessName,
                ["exe"] = exe,
                ["monitor"] = DesktopReader.MonitorOf(hwnd),
                ["minimized"] = factsByWindow[hwnd].Minimized,
                ["active"] = hwnd == foreground,
            };
            items.Add(item);
        }

        _context.Hub.Publish("tasks", new JsonObject { ["items"] = items });
        _ = PublishIconsAsync(exes);
    }

    private async Task PublishIconsAsync(List<string> exes)
    {
        var sources = new HashSet<string>(exes.Where(exe => exe.Length > 0), StringComparer.OrdinalIgnoreCase);
        if (sources.SetEquals(_publishedIconSources))
        {
            return;
        }

        _publishedIconSources = sources;
        var icons = await _context.Icons.ResolveAsync(sources).ConfigureAwait(false);
        var data = new JsonObject();
        foreach (var (exe, icon) in icons)
        {
            data[exe] = icon;
        }

        _context.Hub.Publish("icons", data);
    }
}
