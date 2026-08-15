using API.Hubs;
using API.Services.Sync;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace API.Services.ChatRelay;

/// <summary>
/// Realtime relay gateway that lets call signaling cross the AreaNode &lt;-&gt; Central
/// boundary without touching the periodic sync pipeline.
///
/// - On Central: routes a signal to the ChatHub group when the target employee is
///   online locally, otherwise forwards it to the AreaNode connection serving that
///   employee (or drops it when nobody is reachable).
/// - On AreaNode: forwards the signal up the relay connection to Central.
/// </summary>
public class ChatRelayGateway
{
    private readonly SyncRuntimeOptions _options;
    private readonly ChatPresenceRegistry _presenceRegistry;
    private readonly ChatRelayNodeRegistry _nodeRegistry;
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly IHubContext<ChatRelayHub> _relayHubContext;
    private readonly AreaNodeChatRelayWorker? _areaNodeWorker;

    public ChatRelayGateway(
        IOptions<SyncRuntimeOptions> options,
        ChatPresenceRegistry presenceRegistry,
        ChatRelayNodeRegistry nodeRegistry,
        IHubContext<ChatHub> chatHubContext,
        IHubContext<ChatRelayHub> relayHubContext,
        AreaNodeChatRelayWorker? areaNodeWorker)
    {
        _options = options.Value;
        _presenceRegistry = presenceRegistry;
        _nodeRegistry = nodeRegistry;
        _chatHubContext = chatHubContext;
        _relayHubContext = relayHubContext;
        _areaNodeWorker = areaNodeWorker;
    }

    public bool IsEnabled => !string.Equals(_options.Mode, SyncRuntimeModes.Standalone, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Relay a call signaling message toward the target employee's server.
    /// Returns false when there is no reachable destination.
    /// </summary>
    public async Task<bool> RelaySignalAsync(RelaySignal signal, string? excludeNodeConnectionId = null)
    {
        if (signal == null || signal.TargetEmployeeId <= 0) return false;

        if (string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
        {
            return await RouteOnCentralAsync(signal, excludeNodeConnectionId);
        }

        if (string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase) &&
            _areaNodeWorker != null)
        {
            return await _areaNodeWorker.SendSignalAsync(signal);
        }

        return false;
    }

    /// <summary>
    /// Forward a "call handled elsewhere" notice to the peer backend so that the
    /// same employee's devices on the OTHER side (mobile on VPS vs web on local
    /// docker) stop ringing. Local broadcast is handled by the ChatHub itself via
    /// Clients.OthersInGroup so the accepting connection is not dismissed.
    /// </summary>
    public async Task NotifyCallHandledElsewhereAsync(int employeeId, int? conversationId)
    {
        if (employeeId <= 0 || !IsEnabled) return;

        var relaySignal = new RelaySignal
        {
            Kind = RelaySignalKind.CallHandledElsewhere,
            TargetEmployeeId = employeeId,
            FromEmployeeId = employeeId,
            ConversationId = conversationId
        };

        if (string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
        {
            if (_nodeRegistry.TryGetNodeConnection(employeeId, out var nodeConnectionId) &&
                !string.IsNullOrWhiteSpace(nodeConnectionId))
            {
                await _relayHubContext.Clients.Client(nodeConnectionId).SendAsync("RelaySignal", relaySignal);
            }
            return;
        }

        if (string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase) &&
            _areaNodeWorker != null)
        {
            await _areaNodeWorker.SendSignalAsync(relaySignal);
        }
    }

    /// <summary>
    /// Broadcast a call signaling message to every backend where the target
    /// employee is currently online (local ChatHub group + remote node / peer
    /// backend). Used for call setup signals so a callee online on both VPS and
    /// local docker rings on ALL of their devices.
    /// </summary>
    public async Task BroadcastCallSignalAsync(RelaySignal signal)
    {
        if (signal == null || signal.TargetEmployeeId <= 0) return;

        if (_presenceRegistry.IsOnline(signal.TargetEmployeeId))
        {
            var eventName = RelaySignalHelper.GetClientEventName(signal);
            var payload = RelaySignalHelper.BuildClientPayload(signal);
            await _chatHubContext.Clients.Group($"user_{signal.TargetEmployeeId}").SendAsync(eventName, payload);
        }

        if (!IsEnabled) return;

        if (string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
        {
            if (_nodeRegistry.TryGetNodeConnection(signal.TargetEmployeeId, out var nodeConnectionId) &&
                !string.IsNullOrWhiteSpace(nodeConnectionId))
            {
                await _relayHubContext.Clients.Client(nodeConnectionId).SendAsync("RelaySignal", signal);
            }
            return;
        }

        if (string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase) &&
            _areaNodeWorker != null)
        {
            await _areaNodeWorker.SendSignalAsync(signal);
        }
    }

    private async Task<bool> RouteOnCentralAsync(RelaySignal signal, string? excludeNodeConnectionId = null)
    {
        if (signal.Kind == RelaySignalKind.CallHandledElsewhere)
        {
            if (_presenceRegistry.IsOnline(signal.TargetEmployeeId))
            {
                var eventName = RelaySignalHelper.GetClientEventName(signal);
                var payload = RelaySignalHelper.BuildClientPayload(signal);
                await _chatHubContext.Clients.Group($"user_{signal.TargetEmployeeId}").SendAsync(eventName, payload);
                return true;
            }

            if (_nodeRegistry.TryGetNodeConnection(signal.TargetEmployeeId, out var nodeConnectionId) &&
                !string.IsNullOrWhiteSpace(nodeConnectionId) &&
                !string.Equals(nodeConnectionId, excludeNodeConnectionId, StringComparison.Ordinal))
            {
                await _relayHubContext.Clients.Client(nodeConnectionId).SendAsync("RelaySignal", signal);
                return true;
            }

            return false;
        }

        if (_presenceRegistry.IsOnline(signal.TargetEmployeeId))
        {
            var eventName = RelaySignalHelper.GetClientEventName(signal);
            var payload = RelaySignalHelper.BuildClientPayload(signal);
            await _chatHubContext.Clients.Group($"user_{signal.TargetEmployeeId}").SendAsync(eventName, payload);
            return true;
        }

        if (_nodeRegistry.TryGetNodeConnection(signal.TargetEmployeeId, out var nodeConnectionId2) &&
            !string.IsNullOrWhiteSpace(nodeConnectionId2) &&
            !string.Equals(nodeConnectionId2, excludeNodeConnectionId, StringComparison.Ordinal))
        {
            await _relayHubContext.Clients.Client(nodeConnectionId2).SendAsync("RelaySignal", signal);
            return true;
        }

        return false;
    }
}
