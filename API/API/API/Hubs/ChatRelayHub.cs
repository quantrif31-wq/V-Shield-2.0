using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using API.Services.ChatRelay;
using API.Services.Sync;
using Microsoft.Extensions.Options;

namespace API.Hubs;

/// <summary>
/// Server-side relay hub (mapped on Central / VPS). AreaNodes connect to this
/// hub as SignalR clients using their node credentials, register the set of
/// employees they currently serve, and exchange call signaling both ways.
///
/// Signals arriving here are routed by <see cref="ChatRelayGateway"/>: targets
/// online on Central are delivered to the local ChatHub group, targets served by
/// another node are forwarded down to that node's connection.
/// </summary>
[AllowAnonymous]
public class ChatRelayHub : Hub
{
    private readonly ChatRelayGateway _gateway;
    private readonly ChatRelayNodeRegistry _nodeRegistry;
    private readonly SyncRuntimeOptions _options;
    private readonly ILogger<ChatRelayHub> _logger;

    public ChatRelayHub(
        ChatRelayGateway gateway,
        ChatRelayNodeRegistry nodeRegistry,
        IOptions<SyncRuntimeOptions> options,
        ILogger<ChatRelayHub> logger)
    {
        _gateway = gateway;
        _nodeRegistry = nodeRegistry;
        _options = options.Value;
        _logger = logger;
    }

    private string GetNodeId() =>
        Context.GetHttpContext()?.Request.Query["nodeId"].ToString() ?? string.Empty;

    public override async Task OnConnectedAsync()
    {
        var nodeId = GetNodeId();
        var nodeSecret = Context.GetHttpContext()?.Request.Query["nodeSecret"].ToString() ?? string.Empty;

        var isValid = false;
        if (!string.IsNullOrWhiteSpace(nodeId) &&
            string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var scope = Context.GetHttpContext()!.RequestServices.CreateScope();
                var centralSyncService = scope.ServiceProvider.GetRequiredService<CentralSyncService>();
                var node = await centralSyncService.ValidateNodeAsync(nodeId, nodeSecret, CancellationToken.None);
                isValid = node != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chat relay node validation failed for node {NodeId}", nodeId);
            }
        }

        if (!isValid)
        {
            _logger.LogWarning("Rejecting chat relay connection for node {NodeId} (mode {Mode})", nodeId, _options.Mode);
            Context.Abort();
            return;
        }

        _logger.LogInformation("Chat relay node {NodeId} connected ({ConnectionId})", nodeId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _nodeRegistry.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Called by an AreaNode to refresh which employees it serves.</summary>
    public Task RegisterPresence(int[] employeeIds)
    {
        _nodeRegistry.ReplaceEmployees(Context.ConnectionId, employeeIds ?? Array.Empty<int>());
        _logger.LogDebug("Node {NodeId} registered {Count} employees", GetNodeId(), employeeIds?.Length ?? 0);
        return Task.CompletedTask;
    }

    /// <summary>Signal forwarded from an AreaNode for a target on Central (or another node).</summary>
    public async Task RelaySignal(RelaySignal signal)
    {
        await _gateway.RelaySignalAsync(signal, excludeNodeConnectionId: Context.ConnectionId);
    }
}
