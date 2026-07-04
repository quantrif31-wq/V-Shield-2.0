using Microsoft.Extensions.Options;

namespace API.Services.Sync;

public class CentralSyncInboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CentralSyncInboxWorker> _logger;
    private readonly SyncRuntimeOptions _options;

    public CentralSyncInboxWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<CentralSyncInboxWorker> logger,
        IOptions<SyncRuntimeOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!string.Equals(_options.Mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var centralSyncService = scope.ServiceProvider.GetRequiredService<CentralSyncService>();
                await centralSyncService.ProcessPendingInboundBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Central sync inbox replay cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PushIntervalSeconds)), stoppingToken);
        }
    }
}
