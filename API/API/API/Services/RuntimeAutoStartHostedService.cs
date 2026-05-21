namespace API.Services;

public sealed class RuntimeAutoStartHostedService : IHostedService
{
    private readonly RuntimeOrchestrator _runtimeOrchestrator;
    private readonly ILogger<RuntimeAutoStartHostedService> _logger;

    public RuntimeAutoStartHostedService(RuntimeOrchestrator runtimeOrchestrator, ILogger<RuntimeAutoStartHostedService> logger)
    {
        _runtimeOrchestrator = runtimeOrchestrator;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _runtimeOrchestrator.EnsureAutoStartAsync(cancellationToken);
            _logger.LogInformation("Runtime auto-start applied.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Runtime auto-start failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
