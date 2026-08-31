using System.Threading.Channels;

namespace API.Services.Sync;

public interface ISyncSignalNotifier
{
    ChannelReader<byte> LocalSyncTriggerReader { get; }
    ChannelReader<byte> CentralInboxTriggerReader { get; }

    void TriggerLocalSync();
    void TriggerCentralInbox();
    Task BroadcastSyncNeededAsync(string? areaNodeId = null, string? scopeType = null, string? scopeId = null, CancellationToken cancellationToken = default);
    Task NotifyUpstreamPendingAsync(string areaNodeId, CancellationToken cancellationToken = default);
}
