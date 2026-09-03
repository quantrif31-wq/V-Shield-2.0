using System.Collections.Concurrent;

namespace API.Services.CameraRelay;

public sealed record CameraRelaySession(string SessionId, string ViewerConnectionId, string NodeId, string StreamName);
public sealed record CameraRelayReadiness(bool Ready, string Message);

/// <summary>In-memory route table for authenticated VPS viewers and AreaNode camera peers.</summary>
public sealed class CameraRelayRegistry
{
    private readonly ConcurrentDictionary<string, string> _nodeConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CameraRelaySession> _sessions = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CameraRelayReadiness>> _readiness = new();

    public void RegisterNode(string nodeId, string connectionId) => _nodeConnections[nodeId] = connectionId;
    public bool TryGetNodeConnection(string nodeId, out string? connectionId) => _nodeConnections.TryGetValue(nodeId, out connectionId);
    public IReadOnlyList<string> GetOnlineNodeIds() => _nodeConnections.Keys.OrderBy(value => value).ToArray();

    public CameraRelaySession CreateSession(string viewerConnectionId, string nodeId, string streamName)
    {
        var session = new CameraRelaySession(Guid.NewGuid().ToString("N"), viewerConnectionId, nodeId, streamName);
        _sessions[session.SessionId] = session;
        _readiness[session.SessionId] = new TaskCompletionSource<CameraRelayReadiness>(TaskCreationOptions.RunContinuationsAsynchronously);
        return session;
    }

    public bool TryGetSession(string sessionId, out CameraRelaySession? session) => _sessions.TryGetValue(sessionId, out session);
    public void RemoveSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        if (_readiness.TryRemove(sessionId, out var pending))
            pending.TrySetResult(new CameraRelayReadiness(false, "Phiên camera đã đóng."));
    }

    public void CompleteReadiness(string sessionId, bool ready, string? message)
    {
        if (_readiness.TryGetValue(sessionId, out var pending))
            pending.TrySetResult(new CameraRelayReadiness(ready, message ?? string.Empty));
    }

    public async Task<CameraRelayReadiness> WaitForReadinessAsync(string sessionId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!_readiness.TryGetValue(sessionId, out var pending))
            return new CameraRelayReadiness(false, "Phiên camera không tồn tại.");
        try { return await pending.Task.WaitAsync(timeout, cancellationToken); }
        catch (TimeoutException) { return new CameraRelayReadiness(false, "Camera local không phản hồi kịp thời."); }
    }

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
        foreach (var session in removed) RemoveSession(session.SessionId);
        return removed;
    }
}
