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
    public async Task<bool> RelaySignalAsync(RelaySignal signal)
    {
        if (signal == null || signal.TargetEmployeeId <= 0) return false;

        if (string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
        {
            return await RouteOnCentralAsync(signal);
        }

        if (string.Equals(_options.Mode, SyncRuntimeModes.AreaNode, StringComparison.OrdinalIgnoreCase) &&
            _areaNodeWorker != null)
        {
            return await _areaNodeWorker.SendSignalAsync(signal);
        }

        return false;
    }

    private async Task<bool> RouteOnCentralAsync(RelaySignal signal)
    {
        if (_presenceRegistry.IsOnline(signal.TargetEmployeeId))
        {
            var eventName = RelaySignalHelper.GetClientEventName(signal);
            var payload = RelaySignalHelper.BuildClientPayload(signal);
            await _chatHubContext.Clients.Group($"user_{signal.TargetEmployeeId}").SendAsync(eventName, payload);
            return true;
        }

        if (_nodeRegistry.TryGetNodeConnection(signal.TargetEmployeeId, out var nodeConnectionId) &&
            !string.IsNullOrWhiteSpace(nodeConnectionId))
        {
            await _relayHubContext.Clients.Client(nodeConnectionId).SendAsync("RelaySignal", signal);
            return true;
        }

        return false;
    }
}
