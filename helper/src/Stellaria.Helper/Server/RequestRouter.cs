using System.Text.Json.Nodes;
using Stellaria.Helper.Hosting;
using Stellaria.Helper.Protocol;
using Stellaria.Helper.Security;

namespace Stellaria.Helper.Server;

internal delegate Task<JsonNode?> RequestHandler(RequestContext context, CancellationToken cancellation);

internal sealed record RequestContext(JsonObject Params, string Widget, string Monitor)
{
    public string RequireString(string name)
    {
        if (Params[name] is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > 0)
        {
            return text;
        }

        throw new HelperException(ErrorCode.InvalidParams, $"Parameter '{name}' must be a non empty string.");
    }

    public bool RequireBoolean(string name)
    {
        if (Params[name] is JsonValue value && value.TryGetValue<bool>(out var flag))
        {
            return flag;
        }

        throw new HelperException(ErrorCode.InvalidParams, $"Parameter '{name}' must be a boolean.");
    }

    public int RequireInteger(string name, int minimum, int maximum)
    {
        if (Params[name] is JsonValue value && value.TryGetValue<int>(out var number) && number >= minimum && number <= maximum)
        {
            return number;
        }

        throw new HelperException(ErrorCode.InvalidParams, $"Parameter '{name}' must be an integer between {minimum} and {maximum}.");
    }

    public int OptionalInteger(string name, int fallback, int minimum, int maximum) =>
        Params.ContainsKey(name) ? RequireInteger(name, minimum, maximum) : fallback;
}

internal sealed class RequestRouter(Log log)
{
    private readonly Dictionary<string, (RequestHandler Handler, string Scope)> _handlers = new(StringComparer.Ordinal);

    public void Register(string method, RequestHandler handler) =>
        Register(method, Scopes.ForMethod(method) ?? throw new InvalidOperationException($"Method '{method}' has no scope in Security/Scopes.cs."), handler);

    public void Register(string method, string scope, RequestHandler handler)
    {
        if (!Scopes.IsKnown(scope))
        {
            throw new InvalidOperationException($"Scope '{scope}' of method '{method}' is unknown.");
        }

        if (!_handlers.TryAdd(method, (handler, scope)))
        {
            throw new InvalidOperationException($"Method '{method}' is registered twice.");
        }
    }

    public async Task<string> DispatchAsync(RequestMessage request, string widget, string monitor, Grant grant, CancellationToken cancellation)
    {
        if (!_handlers.TryGetValue(request.Method, out var entry))
        {
            return Messages.ReplyFailure(request.Id, ErrorCode.UnknownMethod, $"Unknown method '{request.Method}'.");
        }

        if (!grant.Allows(entry.Scope))
        {
            log.Warning("access", $"Denied {request.Method} to pack {grant.Pack} ({widget}): it needs the '{entry.Scope}' scope.");
            return Messages.ReplyFailure(request.Id, ErrorCode.Denied, $"This pack is not allowed to use '{request.Method}' (scope '{entry.Scope}').");
        }

        var handler = entry.Handler;

        try
        {
            var data = await handler(new RequestContext(request.Params, widget, monitor), cancellation).ConfigureAwait(false);
            return Messages.ReplySuccess(request.Id, data);
        }
        catch (HelperException exception)
        {
            log.Warning("router", $"{request.Method} from {widget} failed with {exception.Code}: {exception.Message}");
            return Messages.ReplyFailure(request.Id, exception.Code, exception.Message);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            log.Error("router", $"{request.Method} from {widget} crashed.", exception);
            return Messages.ReplyFailure(request.Id, ErrorCode.Failed, exception.Message);
        }
    }
}
