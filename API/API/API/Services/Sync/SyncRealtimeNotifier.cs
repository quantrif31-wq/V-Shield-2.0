using API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace API.Services.Sync;

public class SyncRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ISyncSignalNotifier _syncSignalNotifier;
    private readonly ILogger<SyncRealtimeNotifier> _logger;

    public SyncRealtimeNotifier(
        IHubContext<NotificationHub> hubContext,
        ISyncSignalNotifier syncSignalNotifier,
        ILogger<SyncRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _syncSignalNotifier = syncSignalNotifier;
        _logger = logger;
    }

    public async Task PublishAsync(string aggregateType, string? aggregateId, string action, string sourceSystem, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("SyncEventApplied", new
            {
                aggregateType,
                aggregateId,
                action,
                sourceSystem,
                occurredAtUtc = DateTime.UtcNow
            }, cancellationToken);

            _ = _syncSignalNotifier.BroadcastSyncNeededAsync(null, aggregateType, aggregateId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Skipping realtime sync notification for {AggregateType}/{AggregateId}", aggregateType, aggregateId);
        }
    }
}
