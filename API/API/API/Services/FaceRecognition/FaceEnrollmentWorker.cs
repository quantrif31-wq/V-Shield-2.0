namespace API.Services.FaceRecognition;

public sealed class FaceEnrollmentWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FaceEnrollmentOptions _options;
    private readonly ILogger<FaceEnrollmentWorker> _logger;

    public FaceEnrollmentWorker(IServiceScopeFactory scopeFactory,
        FaceEnrollmentOptions options, ILogger<FaceEnrollmentWorker> logger)
    {
        _scopeFactory = scopeFactory; _options = options; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Recover(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        do
        {
            try
            {
                await Recover(stoppingToken);
                for (var i = 0; i < _options.MaxConcurrentJobs; i++)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IFaceEnrollmentService>();
                    if (!await service.ProcessNextAsync(stoppingToken)) break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face enrollment worker cycle failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task Recover(CancellationToken token)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IFaceEnrollmentService>().RecoverAsync(token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Face enrollment recovery cycle failed.");
        }
    }
}
