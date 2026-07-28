using System.Net;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.FaceRecognition;

public sealed class FaceRecognitionEventOptions
{
    public const string SectionName = "FaceRecognitionEvents";
    public bool CollectorEnabled { get; set; } = true;
    public int PollIntervalMilliseconds { get; set; } = 1000;
    public int BatchSize { get; set; } = 100;
    public int MaxParallelCameras { get; set; } = 2;
    public bool StoreUnknownEvents { get; set; }
    public int RetentionDays { get; set; } = 90;
}

public sealed record FaceRecognitionCollectorHealth(
    bool Enabled, int CameraCount, int GapCount, DateTime? LastSuccessAtUtc,
    IReadOnlyList<FaceRecognitionCollectorCheckpoint> Checkpoints);

public interface IFaceRecognitionEventCollector
{
    Task RunCycleAsync(CancellationToken token);
    Task<FaceRecognitionCollectorHealth> HealthAsync(CancellationToken token);
}

public sealed class FaceRecognitionEventCollector : BackgroundService, IFaceRecognitionEventCollector
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FaceRecognitionEventOptions _options;
    private readonly ILogger<FaceRecognitionEventCollector> _logger;
    private readonly SemaphoreSlim _cycleGate = new(1, 1);

    public FaceRecognitionEventCollector(IServiceScopeFactory scopeFactory,
        FaceRecognitionEventOptions options, ILogger<FaceRecognitionEventCollector> logger)
    {
        _scopeFactory = scopeFactory; _options = options; _logger = logger;
    }

    public async Task RunCycleAsync(CancellationToken token)
    {
        if (!await _cycleGate.WaitAsync(0, token)) return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var runtime = scope.ServiceProvider.GetRequiredService<IFaceRecognitionClient>();
            var camerasResponse = await runtime.GetCamerasAsync(token);
            if (!IsSuccess(camerasResponse.StatusCode)) return;
            var cameras = ParseCameraIds(camerasResponse.Body);
            await Parallel.ForEachAsync(cameras,
                new ParallelOptions { MaxDegreeOfParallelism = _options.MaxParallelCameras,
                                      CancellationToken = token },
                async (cameraId, ct) =>
                {
                    try
                    {
                        using var child = _scopeFactory.CreateScope();
                        await CollectCamera(
                            child.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                            child.ServiceProvider.GetRequiredService<IFaceRecognitionClient>(),
                            cameraId, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        await RecordCameraFailureAsync(cameraId, ex, ct);
                        _logger.LogWarning(ex,
                            "Recognition event collection failed for camera {CameraId}: {Error}",
                            cameraId, ex.GetType().Name);
                    }
                });
        }
        catch (FaceRuntimeUnavailableException) { }
        finally { _cycleGate.Release(); }
    }

    public async Task<FaceRecognitionCollectorHealth> HealthAsync(CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var checkpoints = await db.FaceRecognitionCollectorCheckpoints.AsNoTracking()
            .OrderBy(c => c.CameraId).ToListAsync(token);
        return new(_options.CollectorEnabled, checkpoints.Count,
            checkpoints.Count(c => c.GapDetectedAtUtc.HasValue),
            checkpoints.Select(c => c.LastSuccessAtUtc).Max(), checkpoints);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(_options.PollIntervalMilliseconds));
        try
        {
            do { await RunCycleAsync(stoppingToken); }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task CollectCamera(ApplicationDbContext db, IFaceRecognitionClient runtime,
        string cameraId, CancellationToken token)
    {
        var checkpoint = await db.FaceRecognitionCollectorCheckpoints
            .SingleOrDefaultAsync(c => c.CameraId == cameraId, token);
        var response = await runtime.GetCameraEventsAsync(cameraId,
            checkpoint?.LastSequence ?? 0, checkpoint?.RuntimeSessionGeneration,
            _options.BatchSize, token);
        if (response.StatusCode != HttpStatusCode.OK || response.Payload is null) return;
        var payload = response.Payload;
        var generationReset = checkpoint is not null &&
                              checkpoint.RuntimeSessionGeneration != 0 &&
                              checkpoint.RuntimeSessionGeneration != payload.SessionGeneration;
        if (generationReset)
        {
            response = await runtime.GetCameraEventsAsync(cameraId, 0,
                payload.SessionGeneration, _options.BatchSize, token);
            if (response.StatusCode != HttpStatusCode.OK || response.Payload is null) return;
            payload = response.Payload;
        }
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(token) : null;
        checkpoint ??= new FaceRecognitionCollectorCheckpoint { CameraId = cameraId };
        if (db.Entry(checkpoint).State == EntityState.Detached)
            db.FaceRecognitionCollectorCheckpoints.Add(checkpoint);
        if (generationReset) checkpoint.LastSequence = 0;
        var syncRun = Guid.NewGuid();
        foreach (var runtimeEvent in payload.Events.OrderBy(e => e.Sequence))
        {
            if (!Guid.TryParse(runtimeEvent.EventId, out var eventId)) continue;
            if (runtimeEvent.EventType == "Unknown" && !_options.StoreUnknownEvents)
            {
                checkpoint.LastSequence = Math.Max(checkpoint.LastSequence, runtimeEvent.Sequence);
                continue;
            }
            var existing = await db.FaceRecognitionEvents.AsNoTracking()
                .FirstOrDefaultAsync(e => e.RuntimeEventId == eventId, token);
            if (existing is not null)
            {
                if (!PayloadMatches(existing, runtimeEvent))
                    AddAudit(db, "EVENT_PAYLOAD_CONFLICT", cameraId, runtimeEvent.EventId);
                checkpoint.LastSequence = Math.Max(checkpoint.LastSequence, runtimeEvent.Sequence);
                continue;
            }
            var entity = await Reconcile(db, runtimeEvent, eventId, syncRun, token);
            db.FaceRecognitionEvents.Add(entity);
            if (entity.MatchStatus is FaceRecognitionMatchStatuses.EmployeeMissing or
                FaceRecognitionMatchStatuses.ModelMismatch or
                FaceRecognitionMatchStatuses.CameraUnmanaged)
            {
                AddAudit(db, entity.MatchStatus.ToUpperInvariant(), cameraId,
                    runtimeEvent.EventId);
            }
            if (entity.FaceCameraConfigurationId.HasValue &&
                !string.IsNullOrWhiteSpace(runtimeEvent.LaneId) &&
                !string.Equals(runtimeEvent.LaneId, entity.LaneId?.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                AddAudit(db, "RUNTIME_LANE_MISMATCH", cameraId,
                    runtimeEvent.EventId);
            }
            checkpoint.LastSequence = Math.Max(checkpoint.LastSequence, runtimeEvent.Sequence);
            checkpoint.LastEventOccurredAtUtc = runtimeEvent.OccurredAtUtc;
        }
        checkpoint.RuntimeSessionGeneration = payload.SessionGeneration;
        var completedAtUtc = DateTime.UtcNow;
        checkpoint.LastPollAtUtc = completedAtUtc;
        checkpoint.LastSuccessAtUtc = completedAtUtc;
        checkpoint.LastErrorCode = checkpoint.LastErrorMessage = null;
        if (payload.GapDetected || generationReset)
        {
            checkpoint.GapDetectedAtUtc = DateTime.UtcNow;
            AddAudit(db, payload.GapDetected ? "EVENT_BUFFER_GAP" : "SESSION_GENERATION_RESET",
                cameraId, payload.SessionGeneration.ToString());
        }
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
    }

    private async Task RecordCameraFailureAsync(string cameraId, Exception error, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var checkpoint = await db.FaceRecognitionCollectorCheckpoints
            .SingleOrDefaultAsync(c => c.CameraId == cameraId, token)
            ?? new FaceRecognitionCollectorCheckpoint { CameraId = cameraId };
        if (db.Entry(checkpoint).State == EntityState.Detached)
            db.FaceRecognitionCollectorCheckpoints.Add(checkpoint);
        checkpoint.LastPollAtUtc = DateTime.UtcNow;
        checkpoint.LastErrorCode = error is FaceRuntimeUnavailableException unavailable
            ? unavailable.FailureKind.ToString()
            : error.GetType().Name;
        checkpoint.LastErrorMessage = "Recognition runtime collection failed.";
        await db.SaveChangesAsync(token);
    }

    private static bool PayloadMatches(
        FaceRecognitionEvent existing, FaceRuntimeRecognitionEvent runtimeEvent) =>
        existing.CameraId == runtimeEvent.CameraId &&
        existing.RuntimeSequence == runtimeEvent.Sequence &&
        existing.RuntimeSessionGeneration == runtimeEvent.SessionGeneration &&
        existing.EventType == runtimeEvent.EventType &&
        existing.RuntimeSubjectId == runtimeEvent.SubjectId &&
        existing.OccurredAtUtc == runtimeEvent.OccurredAtUtc.ToUniversalTime();

    private static async Task<FaceRecognitionEvent> Reconcile(
        ApplicationDbContext db, FaceRuntimeRecognitionEvent item, Guid eventId,
        Guid syncRun, CancellationToken token)
    {
        var config = await db.FaceCameraConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.RuntimeCameraId == item.CameraId, token);
        Employee? employee = null;
        if (int.TryParse(item.SubjectId, out var employeeId))
            employee = await db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, token);
        EmployeeFaceModel? model = null;
        if (employee is not null)
            model = await db.EmployeeFaceModels.AsNoTracking()
                .FirstOrDefaultAsync(m => m.EmployeeId == employee.EmployeeId &&
                    m.Status == FaceModelLifecycleStatuses.Active, token);
        var status = item.EventType == "Unknown"
            ? FaceRecognitionMatchStatuses.IgnoredUnknown
            : employee is null ? FaceRecognitionMatchStatuses.EmployeeMissing
            : model is null ? FaceRecognitionMatchStatuses.ModelMissing
            : model.ModelFileName != item.ModelFileName ||
              !(model.ModelChecksum ?? "").StartsWith(item.ModelChecksumPrefix ?? "\0",
                  StringComparison.OrdinalIgnoreCase)
                ? FaceRecognitionMatchStatuses.ModelMismatch
                : config is null ? FaceRecognitionMatchStatuses.CameraUnmanaged
                : FaceRecognitionMatchStatuses.Matched;
        return new FaceRecognitionEvent {
            RuntimeEventId = eventId, CameraId = item.CameraId,
            FaceCameraConfigurationId = config?.Id, LaneId = config?.LaneId,
            EmployeeId = employee?.EmployeeId, RuntimeSubjectId = item.SubjectId,
            EventType = item.EventType, OccurredAtUtc = item.OccurredAtUtc.ToUniversalTime(),
            ReceivedAtUtc = DateTime.UtcNow, RuntimeSequence = item.Sequence,
            RuntimeSessionGeneration = item.SessionGeneration,
            RecognitionDistance = item.Distance, ModelRegistryVersion = item.ModelRegistryVersion,
            EmployeeFaceModelId = model?.Id, ModelFileName = item.ModelFileName,
            ModelChecksumPrefix = item.ModelChecksumPrefix, MatchStatus = status,
            SyncRunId = syncRun, CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static IReadOnlyList<string> ParseCameraIds(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("sessions").EnumerateArray()
            .Select(s => s.GetProperty("cameraId").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id)).Cast<string>().Distinct().ToList();
    }

    private static void AddAudit(ApplicationDbContext db, string action, string cameraId, string detail) =>
        db.SystemAuditLogs.Add(new SystemAuditLog {
            TimestampUtc = DateTime.UtcNow, EventCategory = "FACE_RECOGNITION_SYNC",
            Severity = "WARNING", ActionType = action, EntityName = "FaceCamera",
            EntityId = cameraId, NewValuesJson = JsonSerializer.Serialize(new { detail }),
            IsSuccess = false
        });
    private static bool IsSuccess(HttpStatusCode code) => (int)code is >= 200 and < 300;
}
