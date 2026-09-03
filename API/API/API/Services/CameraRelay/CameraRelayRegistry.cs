using System.Collections.Concurrent;

namespace API.Services.CameraRelay;

public sealed record CameraRelaySession(string SessionId, string ViewerConnectionId, string NodeId, string StreamName);

/// <summary>In-memory route table for authenticated VPS viewers and AreaNode camera peers.</summary>
public sealed class CameraRelayRegistry
{
    private readonly ConcurrentDictionary<string, string> _nodeConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CameraRelaySession> _sessions = new();

    public void RegisterNode(string nodeId, string connectionId) => _nodeConnections[nodeId] = connectionId;
    public bool TryGetNodeConnection(string nodeId, out string? connectionId) => _nodeConnections.TryGetValue(nodeId, out connectionId);
    public IReadOnlyList<string> GetOnlineNodeIds() => _nodeConnections.Keys.OrderBy(value => value).ToArray();

    public CameraRelaySession CreateSession(string viewerConnectionId, string nodeId, string streamName)
    {
        var session = new CameraRelaySession(Guid.NewGuid().ToString("N"), viewerConnectionId, nodeId, streamName);
        _sessions[session.SessionId] = session;
        return session;
    }

    public bool TryGetSession(string sessionId, out CameraRelaySession? session) => _sessions.TryGetValue(sessionId, out session);
    public void RemoveSession(string sessionId) => _sessions.TryRemove(sessionId, out _);

    public IReadOnlyList<CameraRelaySession> RemoveConnection(string connectionId)
    {
        var disconnectedNodeIds = _nodeConnections
            .Where(item => item.Value == connectionId)
            .Select(item => item.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var nodeId in disconnectedNodeIds)
            _nodeConnections.TryRemove(nodeId, out _);

        var removed = _sessions.Values
            .Where(session => session.ViewerConnectionId == connectionId || disconnectedNodeIds.Contains(session.NodeId))
            .ToArray();
        foreach (var session in removed) _sessions.TryRemove(session.SessionId, out _);
        return removed;
    }
}
