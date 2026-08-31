using System.Threading.Channels;
using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace API.Services.Sync;

public sealed class SyncSignalNotifier : ISyncSignalNotifier
{
    private readonly Channel<byte> _localSyncChannel;
    private readonly Channel<byte> _centralInboxChannel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncSignalNotifier> _logger;

    public SyncSignalNotifier(IServiceProvider serviceProvider, ILogger<SyncSignalNotifier> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var channelOptions = new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        };

        _localSyncChannel = Channel.CreateBounded<byte>(channelOptions);
        _centralInboxChannel = Channel.CreateBounded<byte>(channelOptions);
    }

    public ChannelReader<byte> LocalSyncTriggerReader => _localSyncChannel.Reader;
    public ChannelReader<byte> CentralInboxTriggerReader => _centralInboxChannel.Reader;

    public void TriggerLocalSync()
    {
        _localSyncChannel.Writer.TryWrite(1);
        _logger.LogTrace("Local sync worker wake-up trigger pulsed.");
    }

    public void TriggerCentralInbox()
    {
        _centralInboxChannel.Writer.TryWrite(1);
        _logger.LogTrace("Central inbox worker wake-up trigger pulsed.");
    }

    public async Task BroadcastSyncNeededAsync(
        string? areaNodeId = null,
        string? scopeType = null,
        string? scopeId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hubContext = scope.ServiceProvider.GetService<IHubContext<ChatRelayHub>>();
            if (hubContext == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(areaNodeId))
            {
                await hubContext.Clients.Group($""node_{areaNodeId}"")
                    .SendAsync(""NotifySyncNeeded"", scopeType, scopeId, cancellationToken);
            }
            else
            {
                await hubContext.Clients.All
                    .SendAsync(""NotifySyncNeeded"", scopeType, scopeId, cancellationToken);
            }
            _logger.LogDebug(""Broadcasted NotifySyncNeeded to area nodes (Target: {NodeId}, Scope: {ScopeType}/{ScopeId})"", areaNodeId ?? ""All"", scopeType, scopeId);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, ""Failed to broadcast real-time sync trigger via ChatRelayHub."");
        }
    }

    public Task NotifyUpstreamPendingAsync(string areaNodeId, CancellationToken cancellationToken = default)
    {
        TriggerCentralInbox();
        return Task.CompletedTask;
    }
}
