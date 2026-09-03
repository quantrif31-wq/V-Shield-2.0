using API.Services.CameraRelay;
using API.Services.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace API.Hubs;

[AllowAnonymous]
public sealed class CameraRelayHub : Hub
{
    private static readonly Regex StreamNamePattern = new("^[A-Za-z0-9_.-]{1,96}$", RegexOptions.Compiled);
    private readonly CameraRelayRegistry _registry;
    private readonly SyncRuntimeOptions _options;
    private readonly ILogger<CameraRelayHub> _logger;

    public CameraRelayHub(CameraRelayRegistry registry, IOptions<SyncRuntimeOptions> options, ILogger<CameraRelayHub> logger)
    {
        _registry = registry;
        _options = options.Value;
        _logger = logger;
    }

    private string NodeId => Context.GetHttpContext()?.Request.Query["nodeId"].ToString() ?? string.Empty;

    public override async Task OnConnectedAsync()
    {
        if (!string.IsNullOrWhiteSpace(NodeId))
        {
            var secret = Context.GetHttpContext()?.Request.Query["nodeSecret"].ToString() ?? string.Empty;
            if (!await IsValidNodeAsync(NodeId, secret)) { Context.Abort(); return; }
            _registry.RegisterNode(NodeId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"camera-node_{NodeId}");
            await base.OnConnectedAsync();
            return;
        }

        if (Context.User?.Identity?.IsAuthenticated != true) { Context.Abort(); return; }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var nodeId = NodeId;
        if (!string.IsNullOrWhiteSpace(nodeId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"camera-node_{nodeId}");
        foreach (var session in _registry.RemoveConnection(Context.ConnectionId))
            await Clients.Group($"camera-node_{session.NodeId}").SendAsync("CameraRelayStop", session.SessionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<object> OpenStream(string nodeId, string streamName)
    {
        EnsureViewer();
        if (!string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
            throw new HubException("Camera relay chỉ được xem từ máy chủ trung tâm.");
        if (string.IsNullOrWhiteSpace(nodeId) || !StreamNamePattern.IsMatch(streamName ?? string.Empty))
            throw new HubException("Nguồn camera không hợp lệ.");
        if (!_registry.TryGetNodeConnection(nodeId, out _))
            throw new HubException("Node camera local đang ngoại tuyến.");

        var session = _registry.CreateSession(Context.ConnectionId, nodeId, streamName);
        await Clients.Group($"camera-node_{nodeId}").SendAsync("CameraRelayStart", session.SessionId, streamName);
        return new { sessionId = session.SessionId };
    }

    public async Task Signal(string sessionId, string kind, string value)
    {
        if (!IsSignalKind(kind) || !_registry.TryGetSession(sessionId, out var session) || session == null)
            throw new HubException("Phiên camera không hợp lệ.");

        if (Context.ConnectionId == session.ViewerConnectionId)
        {
            EnsureViewer();
            await Clients.Group($"camera-node_{session.NodeId}").SendAsync("CameraRelaySignal", sessionId, kind, value ?? string.Empty);
            return;
        }

        if (_registry.TryGetNodeConnection(session.NodeId, out var nodeConnectionId) && nodeConnectionId == Context.ConnectionId)
        {
            await Clients.Client(session.ViewerConnectionId).SendAsync("CameraRelaySignal", sessionId, kind, value ?? string.Empty);
            return;
        }
        throw new HubException("Không có quyền dùng phiên camera này.");
    }

    public async Task CloseStream(string sessionId)
    {
        if (!_registry.TryGetSession(sessionId, out var session) || session == null) return;
        if (Context.ConnectionId != session.ViewerConnectionId) throw new HubException("Không có quyền đóng phiên camera này.");
        _registry.RemoveSession(sessionId);
        await Clients.Group($"camera-node_{session.NodeId}").SendAsync("CameraRelayStop", sessionId);
    }

    private async Task<bool> IsValidNodeAsync(string nodeId, string secret)
    {
        if (!string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            using var scope = Context.GetHttpContext()!.RequestServices.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<CentralSyncService>();
            return await sync.ValidateNodeAsync(nodeId, secret, CancellationToken.None) != null;
        }
        catch (Exception error)
        {
            _logger.LogWarning(error, "Camera relay validation failed for node {NodeId}", nodeId);
            return false;
        }
    }

    private void EnsureViewer()
    {
        if (Context.User?.Identity?.IsAuthenticated != true) throw new HubException("Cần đăng nhập để xem camera.");
    }
    private static bool IsSignalKind(string value) => value is "offer" or "answer" or "candidate";
}
