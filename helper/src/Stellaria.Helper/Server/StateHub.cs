using System.Text.Json.Nodes;
using Stellaria.Helper.Protocol;
using Stellaria.Helper.Security;

namespace Stellaria.Helper.Server;

internal sealed class StateHub
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, string> _states = new(StringComparer.Ordinal);
    private readonly List<ClientSession> _sessions = [];

    public int SessionCount
    {
        get
        {
            lock (_gate)
            {
                return _sessions.Count;
            }
        }
    }

    public IReadOnlyList<ClientSession> Sessions
    {
        get
        {
            lock (_gate)
            {
                return [.. _sessions];
            }
        }
    }

    public bool Publish(string topic, JsonNode? data)
    {
        var message = Messages.State(topic, data);
        ClientSession[] targets;
        lock (_gate)
        {
            if (_states.TryGetValue(topic, out var previous) && previous == message)
            {
                return false;
            }

            _states[topic] = message;
            targets = [.. _sessions];
        }

        var scope = TopicScope(topic);
        foreach (var session in targets)
        {
            if (session.Grant.Allows(scope))
            {
                session.Enqueue(message);
            }
        }

        return true;
    }

    public void Raise(string name, JsonNode? data)
    {
        var message = Messages.Event(name, data);
        var scope = Scopes.ForEvent(name) ?? Scopes.Config;
        foreach (var session in Sessions)
        {
            if (session.Grant.Allows(scope))
            {
                session.Enqueue(message);
            }
        }
    }

    public void Broadcast(string message)
    {
        foreach (var session in Sessions)
        {
            session.Enqueue(message);
        }
    }

    private static string TopicScope(string topic) => Scopes.ForTopic(topic) ?? Scopes.Config;

    public void Attach(ClientSession session)
    {
        string[] snapshot;
        lock (_gate)
        {
            _sessions.Add(session);
            snapshot = [.. _states.Where(state => session.Grant.Allows(TopicScope(state.Key))).Select(state => state.Value)];
        }

        foreach (var message in snapshot)
        {
            session.Enqueue(message);
        }
    }

    public void Detach(ClientSession session)
    {
        lock (_gate)
        {
            _sessions.Remove(session);
        }
    }

    public string? CurrentState(string topic)
    {
        lock (_gate)
        {
            return _states.GetValueOrDefault(topic);
        }
    }
}
