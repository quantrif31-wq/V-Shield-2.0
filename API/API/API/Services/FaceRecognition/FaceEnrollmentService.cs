using System.Net;
using System.Security.Claims;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.FaceRecognition;

public sealed record CreateFaceEnrollmentRequest(int EmployeeId, int EmployeeFaceVideoId);
public sealed record FaceEnrollmentJobDto(
    Guid JobId, int EmployeeId, string EmployeeName, int VideoId, string Status,
    int AttemptCount, DateTime CreatedAtUtc, DateTime? StartedAtUtc,
    DateTime? PreparedAtUtc, DateTime? CompletedAtUtc, int? UsableFrameCount,
    int? EncodingCount, double? QualityScore, string? DuplicateSubjectId,
    double? DuplicateDistance, string? FailureCode, string? FailureMessage,
    bool CanActivate, bool CanCancel, bool CanRetry);

public sealed class FaceEnrollmentOptions
{
    public const string SectionName = "FaceEnrollment";
    public bool WorkerEnabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 5;
    public int MaxConcurrentJobs { get; set; } = 1;
    public int MaxAttempts { get; set; } = 3;
}

public interface IFaceEnrollmentService
{
    Task<FaceEnrollmentJobDto> CreateAsync(CreateFaceEnrollmentRequest request, int userId, CancellationToken token);
    Task<IReadOnlyList<FaceEnrollmentJobDto>> ListAsync(CancellationToken token);
    Task<FaceEnrollmentJobDto?> GetAsync(Guid id, CancellationToken token);
    Task<FaceEnrollmentJobDto> CancelAsync(Guid id, CancellationToken token);
    Task<FaceEnrollmentJobDto> RetryAsync(Guid id, CancellationToken token);
    Task<FaceEnrollmentJobDto> ActivateAsync(Guid id, CancellationToken token);
    Task<bool> ProcessNextAsync(CancellationToken token);
    Task RecoverAsync(CancellationToken token);
}

public sealed class FaceEnrollmentService : IFaceEnrollmentService
{
    private readonly ApplicationDbContext _db;
    private readonly IFaceRecognitionClient _runtime;
    private readonly IFaceStoragePathResolver _storage;
    private readonly FaceEnrollmentOptions _options;

    public FaceEnrollmentService(ApplicationDbContext db, IFaceRecognitionClient runtime,
        IFaceStoragePathResolver storage, FaceEnrollmentOptions options)
    {
        _db = db; _runtime = runtime; _storage = storage; _options = options;
    }

    public async Task<FaceEnrollmentJobDto> CreateAsync(CreateFaceEnrollmentRequest request, int userId, CancellationToken token)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, token)
            ?? throw new KeyNotFoundException("Employee not found.");
        if (employee.Status == false || employee.LifecycleStatus != EmployeeLifecycleStates.Active)
            throw new InvalidOperationException("Employee is not active.");
        var video = await _db.EmployeeFaceVideos.FirstOrDefaultAsync(
            v => v.Id == request.EmployeeFaceVideoId && v.EmployeeId == request.EmployeeId, token)
            ?? throw new KeyNotFoundException("Managed employee video not found.");
        string safeFile;
        try { safeFile = _storage.ResolveFile("video_notok", video.FileName); }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Managed employee video reference is invalid.");
        }
        if (!File.Exists(safeFile))
            throw new KeyNotFoundException("Managed employee video content not found.");
        if (await _db.FaceEnrollmentJobs.AnyAsync(j => j.EmployeeId == request.EmployeeId &&
                FaceEnrollmentJobStatuses.NonTerminal.Contains(j.Status), token))
            throw new InvalidOperationException("Employee already has a non-terminal enrollment job.");
        var job = new FaceEnrollmentJob {
            Id = Guid.NewGuid(), EmployeeId = request.EmployeeId,
            EmployeeFaceVideoId = video.Id, RequestedByUserId = userId,
            Status = FaceEnrollmentJobStatuses.Pending,
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        _db.FaceEnrollmentJobs.Add(job);
        try { await _db.SaveChangesAsync(token); }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException(
                "Employee already has a non-terminal enrollment job.");
        }
        return await LoadDto(job.Id, token);
    }

    public async Task<IReadOnlyList<FaceEnrollmentJobDto>> ListAsync(CancellationToken token) =>
        await _db.FaceEnrollmentJobs.AsNoTracking().Include(j => j.Employee)
            .OrderByDescending(j => j.CreatedAtUtc).Take(200).Select(j => Map(j)).ToListAsync(token);

    public async Task<FaceEnrollmentJobDto?> GetAsync(Guid id, CancellationToken token)
    {
        var job = await _db.FaceEnrollmentJobs.AsNoTracking().Include(j => j.Employee)
            .FirstOrDefaultAsync(j => j.Id == id, token);
        return job is null ? null : Map(job);
    }

    public async Task<FaceEnrollmentJobDto> CancelAsync(Guid id, CancellationToken token)
    {
        var job = await RequireJob(id, token);
        if (job.Status == FaceEnrollmentJobStatuses.Prepared)
            await EnsureSuccess(await _runtime.DiscardEnrollmentAsync(id, token));
        else if (job.Status != FaceEnrollmentJobStatuses.Pending)
            throw new InvalidOperationException("Only Pending or Prepared jobs can be cancelled.");
        job.Status = FaceEnrollmentJobStatuses.Cancelled;
        job.CancelledAtUtc = job.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
        return await LoadDto(id, token);
    }

    public async Task<FaceEnrollmentJobDto> RetryAsync(Guid id, CancellationToken token)
    {
        var job = await RequireJob(id, token);
        if (job.Status != FaceEnrollmentJobStatuses.Failed ||
            job.AttemptCount >= _options.MaxAttempts ||
            job.FailureCode is not ("RuntimeUnavailable" or "RuntimeFailure"))
            throw new InvalidOperationException("Job is not retryable.");
        job.Status = FaceEnrollmentJobStatuses.Pending;
        job.FailureCode = job.FailureMessage = null;
        job.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
        return await LoadDto(id, token);
    }

    public async Task<bool> ProcessNextAsync(CancellationToken token)
    {
        var job = await _db.FaceEnrollmentJobs.OrderBy(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(j => j.Status == FaceEnrollmentJobStatuses.Pending, token);
        if (job is null) return false;
        var claimed = await _db.FaceEnrollmentJobs.Where(j => j.Id == job.Id &&
                j.Status == FaceEnrollmentJobStatuses.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, FaceEnrollmentJobStatuses.Processing)
                .SetProperty(j => j.StartedAtUtc, DateTime.UtcNow)
                .SetProperty(j => j.UpdatedAtUtc, DateTime.UtcNow)
                .SetProperty(j => j.AttemptCount, j => j.AttemptCount + 1), token);
        if (claimed != 1) return true;
        _db.ChangeTracker.Clear();
        job = await RequireJob(job.Id, token);
        var video = await _db.EmployeeFaceVideos.AsNoTracking()
            .SingleAsync(v => v.Id == job.EmployeeFaceVideoId, token);
        try
        {
            var response = await _runtime.PrepareEnrollmentAsync(job.Id,
                new(job.EmployeeId.ToString(), $"video_notok/{video.FileName}"), token);
            if (!response.StatusCode.IsSuccess())
            {
                ApplyRuntimeFailure(job, response);
            }
            else
            {
                var result = JsonSerializer.Deserialize<PrepareRuntimeDto>(response.Body,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? throw new InvalidOperationException("Runtime returned an invalid response.");
                job.Status = FaceEnrollmentJobStatuses.Prepared;
                job.PreparedAtUtc = DateTime.UtcNow;
                job.CandidateReference = result.CandidateReference;
                job.CandidateChecksum = result.CandidateChecksum;
                job.CandidateEncodingCount = result.EncodingCount;
                job.TotalInputFrames = result.TotalInputFrames;
                job.ProcessedFrameCount = result.ProcessedFrameCount;
                job.UsableFrameCount = result.UsableFrameCount;
                job.NoFaceFrameCount = result.NoFaceFrameCount;
                job.MultipleFaceFrameCount = result.MultipleFaceFrameCount;
                job.InvalidFrameCount = result.InvalidFrameCount;
                job.QualityScore = result.QualityScore;
            }
        }
        catch (FaceRuntimeUnavailableException)
        {
            job.FailureCode = "RuntimeUnavailable";
            job.FailureMessage = "Face Runtime is unavailable.";
            job.Status = job.AttemptCount < _options.MaxAttempts
                ? FaceEnrollmentJobStatuses.Pending : FaceEnrollmentJobStatuses.Failed;
        }
        job.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
        return true;
    }

    public async Task<FaceEnrollmentJobDto> ActivateAsync(Guid id, CancellationToken token)
    {
        var job = await RequireJob(id, token);
        if (job.Status != FaceEnrollmentJobStatuses.Prepared || job.CandidateChecksum is null)
            throw new InvalidOperationException("Only Prepared jobs can be activated.");
        var model = await _db.EmployeeFaceModels.SingleOrDefaultAsync(
            m => m.SourceEnrollmentJobId == job.Id, token);
        if (model is null)
        {
            var latest = await _db.EmployeeFaceModels.Where(m => m.EmployeeId == job.EmployeeId)
                .MaxAsync(m => (int?)m.Version, token) ?? 0;
            var nextVersion = latest + 1;
            var nextFilename = $"emp_{job.EmployeeId}_v{nextVersion}_{job.Id:N}"[..^24] + ".pkl";
            model = new EmployeeFaceModel {
                EmployeeId = job.EmployeeId, Version = nextVersion,
                Status = FaceModelLifecycleStatuses.Activating,
                ModelFileName = nextFilename, ModelPath = nextFilename,
                ModelChecksum = job.CandidateChecksum,
                EncodingCount = job.CandidateEncodingCount,
                SourceEnrollmentJobId = job.Id, CreatedAt = DateTime.UtcNow
            };
            _db.EmployeeFaceModels.Add(model);
        }
        else if (model.Status is not (FaceModelLifecycleStatuses.Failed or FaceModelLifecycleStatuses.Activating))
            throw new InvalidOperationException("Enrollment job already owns a finalized model.");
        model.Status = FaceModelLifecycleStatuses.Activating;
        model.FailureCode = model.FailureMessage = null;
        var version = model.Version!.Value;
        var filename = model.ModelFileName;
        job.Status = FaceEnrollmentJobStatuses.Activating;
        job.TargetModelVersion = version;
        job.ExpectedModelFileName = filename;
        job.ActivationRequestedAtUtc = job.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
        var response = await _runtime.ActivateEnrollmentAsync(job.Id,
            new(job.EmployeeId.ToString(), version, job.CandidateChecksum, filename), token);
        if (!response.StatusCode.IsSuccess())
        {
            model.Status = FaceModelLifecycleStatuses.Failed;
            job.Status = FaceEnrollmentJobStatuses.Prepared;
            job.FailureCode = "ActivationFailed";
            job.FailureMessage = "Face Runtime rejected candidate activation.";
            await _db.SaveChangesAsync(token);
            throw new InvalidOperationException(job.FailureMessage);
        }
        await FinalizeActivation(job, model, token);
        return await LoadDto(id, token);
    }

    public async Task RecoverAsync(CancellationToken token)
    {
        var jobs = await _db.FaceEnrollmentJobs.Include(j => j.ResultModel)
            .Where(j => j.Status == FaceEnrollmentJobStatuses.Activating).ToListAsync(token);
        if (jobs.Count == 0) return;
        JsonDocument registry;
        try {
            var response = await _runtime.GetModelsAsync(token);
            if (!response.StatusCode.IsSuccess()) return;
            registry = JsonDocument.Parse(response.Body);
        } catch (FaceRuntimeUnavailableException) { return; }
        using (registry)
        foreach (var job in jobs)
        {
            var found = registry.RootElement.GetProperty("models").EnumerateArray().Any(m =>
                m.GetProperty("fileName").GetString() == job.ExpectedModelFileName &&
                m.GetProperty("checksum").GetString() == job.CandidateChecksum);
            if (found && job.ResultModel is not null)
                await FinalizeActivation(job, job.ResultModel, token);
            else if (job.ResultModel is not null && job.TargetModelVersion.HasValue &&
                     job.ExpectedModelFileName is not null && job.CandidateChecksum is not null)
            {
                var response = await _runtime.ActivateEnrollmentAsync(job.Id,
                    new(job.EmployeeId.ToString(), job.TargetModelVersion.Value,
                        job.CandidateChecksum, job.ExpectedModelFileName), token);
                if (response.StatusCode.IsSuccess())
                    await FinalizeActivation(job, job.ResultModel, token);
                else
                    await MarkRecoveryRequired(job, token);
            }
            else await MarkRecoveryRequired(job, token);
        }
    }

    private async Task MarkRecoveryRequired(FaceEnrollmentJob job, CancellationToken token)
    {
        job.Status = FaceEnrollmentJobStatuses.RecoveryRequired;
        job.FailureCode = "ActivationStateUnknown";
        job.FailureMessage = "Runtime activation state requires administrator review.";
        await _db.SaveChangesAsync(token);
    }

    private async Task FinalizeActivation(FaceEnrollmentJob job, EmployeeFaceModel model, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(token) : null;
        var old = await _db.EmployeeFaceModels.Where(m => m.EmployeeId == job.EmployeeId &&
            m.Status == FaceModelLifecycleStatuses.Active).ToListAsync(token);
        foreach (var item in old) { item.Status = FaceModelLifecycleStatuses.Archived; item.ArchivedAtUtc = now; }
        if (old.Count > 0)
            await _db.SaveChangesAsync(token);
        model.Status = FaceModelLifecycleStatuses.Active; model.ActivatedAtUtc = now;
        job.Status = FaceEnrollmentJobStatuses.Completed; job.CompletedAtUtc = job.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
    }

    private void ApplyRuntimeFailure(FaceEnrollmentJob job, FaceRuntimeResponse response)
    {
        RuntimeErrorDto? error = null;
        try { error = JsonSerializer.Deserialize<RuntimeErrorDto>(response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)); } catch (JsonException) { }
        job.FailureCode = Sanitize(error?.FailureCode, 80) ?? "RuntimeFailure";
        job.FailureMessage = Sanitize(error?.Message, 500) ?? "Face Runtime rejected enrollment.";
        job.DuplicateSubjectId = Sanitize(error?.DuplicateSubjectId, 40);
        job.DuplicateDistance = error?.DuplicateDistance;
        var retryable = (int)response.StatusCode >= 500;
        job.Status = retryable && job.AttemptCount < _options.MaxAttempts
            ? FaceEnrollmentJobStatuses.Pending : FaceEnrollmentJobStatuses.Failed;
    }

    private async Task<FaceEnrollmentJob> RequireJob(Guid id, CancellationToken token) =>
        await _db.FaceEnrollmentJobs.FirstOrDefaultAsync(j => j.Id == id, token)
        ?? throw new KeyNotFoundException("Enrollment job not found.");
    private async Task<FaceEnrollmentJobDto> LoadDto(Guid id, CancellationToken token) =>
        Map(await _db.FaceEnrollmentJobs.AsNoTracking().Include(j => j.Employee)
            .SingleAsync(j => j.Id == id, token));
    private static FaceEnrollmentJobDto Map(FaceEnrollmentJob j) => new(
        j.Id, j.EmployeeId, j.Employee.FullName, j.EmployeeFaceVideoId, j.Status,
        j.AttemptCount, j.CreatedAtUtc, j.StartedAtUtc, j.PreparedAtUtc,
        j.CompletedAtUtc, j.UsableFrameCount, j.CandidateEncodingCount, j.QualityScore,
        j.DuplicateSubjectId, j.DuplicateDistance, j.FailureCode, j.FailureMessage,
        j.Status == FaceEnrollmentJobStatuses.Prepared,
        j.Status is FaceEnrollmentJobStatuses.Pending or FaceEnrollmentJobStatuses.Prepared,
        j.Status == FaceEnrollmentJobStatuses.Failed);
    private static string? Sanitize(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null :
        new string(value.Where(c => !char.IsControl(c)).Take(max).ToArray());
    private static async Task EnsureSuccess(FaceRuntimeResponse response)
    {
        await Task.CompletedTask;
        if (!response.StatusCode.IsSuccess()) throw new InvalidOperationException("Face Runtime operation failed.");
    }
    private sealed record PrepareRuntimeDto(string CandidateReference, string CandidateChecksum,
        int TotalInputFrames, int ProcessedFrameCount, int UsableFrameCount,
        int NoFaceFrameCount, int MultipleFaceFrameCount, int InvalidFrameCount,
        int EncodingCount, double QualityScore);
    private sealed record RuntimeErrorDto(string? FailureCode, string? Message,
        string? DuplicateSubjectId, double? DuplicateDistance);
}
