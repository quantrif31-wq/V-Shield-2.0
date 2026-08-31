using Microsoft.Extensions.Options;

namespace API.Services.Sync;

public class CentralSyncInboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CentralSyncInboxWorker> _logger;
    private readonly SyncRuntimeOptions _options;
    private readonly ISyncSignalNotifier _syncSignalNotifier;

    public CentralSyncInboxWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<CentralSyncInboxWorker> logger,
        IOptions<SyncRuntimeOptions> options,
        ISyncSignalNotifier syncSignalNotifier)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _syncSignalNotifier = syncSignalNotifier;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fallbackInterval = TimeSpan.FromSeconds(Math.Max(5, Math.Min(_options.PushIntervalSeconds * 5, 30)));

        while (!stoppingToken.IsCancellationRequested)
        {
            int processedCount = 0;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var centralSyncService = scope.ServiceProvider.GetRequiredService<CentralSyncService>();
                processedCount = await centralSyncService.ProcessPendingInboundBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Central sync inbox replay cycle failed.");
            }

            // High-load draining loop: if we processed a full batch, drain more immediately!
            if (processedCount >= _options.BatchSize)
            {
                continue;
            }

            // Wait for real-time wake-up trigger or fallback timeout
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(fallbackInterval);
                await _syncSignalNotifier.CentralInboxTriggerReader.WaitToReadAsync(cts.Token);
                while (_syncSignalNotifier.CentralInboxTriggerReader.TryRead(out _)) { }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Fallback timeout elapsed
            }
        }
    }
}
