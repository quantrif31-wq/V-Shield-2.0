using System.Net;
using API.Models;

namespace API.Services.FaceRecognition;

public sealed class FaceCameraReconcileOptions
{
    public const string SectionName = "FaceRecognition";
    public int ReconcileIntervalSeconds { get; set; } = 10;
    public bool ReconcileOnStartup { get; set; } = true;
}

public interface IFaceCameraSessionReconciler
{
    Task<FaceCameraReconcileResultDto> ReconcileAsync(CancellationToken cancellationToken);
}

public sealed class FaceCameraSessionReconciler :
    BackgroundService,
    IFaceCameraSessionReconciler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FaceCameraReconcileOptions _options;
    private readonly ILogger<FaceCameraSessionReconciler> _logger;
    private readonly SemaphoreSlim _cycleGate = new(1, 1);
    private bool? _lastRuntimeAvailable;

    public FaceCameraSessionReconciler(
        IServiceScopeFactory scopeFactory,
        FaceCameraReconcileOptions options,
        ILogger<FaceCameraSessionReconciler> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<FaceCameraReconcileResultDto> ReconcileAsync(
        CancellationToken cancellationToken)
    {
        if (!await _cycleGate.WaitAsync(0, cancellationToken))
        {
            return new FaceCameraReconcileResultDto(
                false, true, _lastRuntimeAvailable ?? false, 0, 0, 0, 0, 0, 0);
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cycle = scope.ServiceProvider.GetRequiredService<FaceCameraReconciliationCycle>();
            var result = await cycle.RunAsync(cancellationToken);
            LogRuntimeTransition(result.RuntimeAvailable);
            return result;
        }
        finally
        {
            _cycleGate.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.ReconcileOnStartup)
        {
            await RunWithoutStoppingHost(stoppingToken);
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.ReconcileIntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunWithoutStoppingHost(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
    }

    private async Task RunWithoutStoppingHost(CancellationToken cancellationToken)
    {
        try
        {
            await ReconcileAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face camera reconciliation cycle failed.");
        }
    }

    private void LogRuntimeTransition(bool available)
    {
        if (_lastRuntimeAvailable == available)
        {
            return;
        }
        _lastRuntimeAvailable = available;
        if (available)
        {
            _logger.LogInformation("Face Runtime is available; reconciliation is active.");
        }
        else
        {
            _logger.LogWarning("Face Runtime is unavailable; desired camera states remain persisted.");
        }
    }
}

public sealed class FaceCameraReconciliationCycle
{
    private readonly IFaceCameraConfigurationStore _store;
    private readonly IFaceRecognitionClient _runtime;

    public FaceCameraReconciliationCycle(
        IFaceCameraConfigurationStore store,
        IFaceRecognitionClient runtime)
    {
        _store = store;
        _runtime = runtime;
    }

    public async Task<FaceCameraReconcileResultDto> RunAsync(
        CancellationToken cancellationToken)
    {
        var configurations = await _store.LoadManagedAsync(cancellationToken);
        var inventory = await _store.GetRuntimeInventoryAsync(cancellationToken);
        var managedIds = configurations.Select(x => x.RuntimeCameraId)
            .ToHashSet(StringComparer.Ordinal);
        var unmanagedCount = inventory.Sessions.Keys.Count(x => !managedIds.Contains(x));

        if (!inventory.Available)
        {
            foreach (var configuration in configurations)
            {
                await _store.MarkFailureAsync(
                    configuration,
                    FaceCameraSyncStatuses.Unavailable,
                    "Face Runtime is unavailable.",
                    cancellationToken);
            }
            return new FaceCameraReconcileResultDto(
                true, false, false, configurations.Count, 0, 0, 0, 0, unmanagedCount);
        }

        var started = 0;
        var stopped = 0;
        var restarted = 0;
        var failed = 0;
        foreach (var configuration in configurations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _store.RefreshConfigurationVersionAsync(configuration, cancellationToken);
                inventory.Sessions.TryGetValue(configuration.RuntimeCameraId, out var session);

                if (configuration.DesiredState == FaceCameraDesiredStates.Stopped)
                {
                    if (session?.Enabled == true)
                    {
                        var response = await _runtime.StopCameraAsync(
                            configuration.RuntimeCameraId,
                            cancellationToken);
                        if (!IsSuccessfulStop(response.StatusCode))
                        {
                            throw new FaceRuntimeOperationException(response.StatusCode);
                        }
                        stopped++;
                    }
                    await _store.MarkSyncedAsync(configuration, cancellationToken);
                    continue;
                }

                var versionChanged =
                    configuration.ConfigurationVersion != configuration.LastAppliedVersion;
                if (session?.Enabled == true && versionChanged)
                {
                    var stopResponse = await _runtime.StopCameraAsync(
                        configuration.RuntimeCameraId,
                        cancellationToken);
                    if (!IsSuccessfulStop(stopResponse.StatusCode))
                    {
                        throw new FaceRuntimeOperationException(stopResponse.StatusCode);
                    }
                    var startResponse = await _runtime.StartCameraAsync(
                        configuration.RuntimeCameraId,
                        _store.CreateStartRequest(configuration),
                        cancellationToken);
                    if (!startResponse.StatusCode.IsSuccess())
                    {
                        throw new FaceRuntimeOperationException(startResponse.StatusCode);
                    }
                    restarted++;
                    await _store.MarkSyncedAsync(configuration, cancellationToken);
                    continue;
                }

                if (session?.Enabled != true && configuration.AutoRestore)
                {
                    var response = await _runtime.StartCameraAsync(
                        configuration.RuntimeCameraId,
                        _store.CreateStartRequest(configuration),
                        cancellationToken);
                    if (!response.StatusCode.IsSuccess())
                    {
                        throw new FaceRuntimeOperationException(response.StatusCode);
                    }
                    started++;
                    await _store.MarkSyncedAsync(configuration, cancellationToken);
                    continue;
                }

                if (session?.Enabled == true && !versionChanged)
                {
                    await _store.MarkSyncedAsync(configuration, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (FaceRuntimeUnavailableException)
            {
                failed++;
                await _store.MarkFailureAsync(
                    configuration,
                    FaceCameraSyncStatuses.Unavailable,
                    "Face Runtime is unavailable.",
                    cancellationToken);
            }
            catch (Exception)
            {
                failed++;
                await _store.MarkFailureAsync(
                    configuration,
                    FaceCameraSyncStatuses.Error,
                    "Face Runtime rejected camera synchronization.",
                    cancellationToken);
            }
        }

        return new FaceCameraReconcileResultDto(
            true,
            false,
            true,
            configurations.Count,
            started,
            stopped,
            restarted,
            failed,
            unmanagedCount);
    }

    private static bool IsSuccessfulStop(HttpStatusCode statusCode) =>
        statusCode.IsSuccess() || statusCode == HttpStatusCode.NotFound;

    private sealed class FaceRuntimeOperationException : Exception
    {
        public FaceRuntimeOperationException(HttpStatusCode statusCode)
            : base($"Runtime operation failed with HTTP {(int)statusCode}.")
        {
        }
    }
}
