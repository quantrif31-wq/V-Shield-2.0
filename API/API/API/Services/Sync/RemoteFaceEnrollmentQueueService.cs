using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Sync;

/// <summary>
/// Hàng đợi đăng ký Face ID từ xa (VPS side). Local node gọi các phương thức
/// này để claim job, tải frame, rồi báo kết quả. Claim là atomic để nhiều local
/// không xử lý trùng một job.
/// </summary>
public class RemoteFaceEnrollmentQueueService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RemoteFaceEnrollmentQueueService> _logger;

    public RemoteFaceEnrollmentQueueService(
        ApplicationDbContext db,
        ILogger<RemoteFaceEnrollmentQueueService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Claim job Pending tiếp theo cho một node (atomic). Trả job + danh sách ảnh
    /// (data URI) để local chạy AI. Trả null nếu không còn job Pending.
    /// </summary>
    public async Task<ClaimedFaceEnrollmentJob?> ClaimNextAsync(
        string areaNodeId, CancellationToken cancellationToken)
    {
        // Atomic claim: đánh dấu Processing ngay trong câu lệnh UPDATE, dùng
        // điều kiện Status = Pending để chống nhiều local claim cùng job.
        var job = await _db.RemoteFaceEnrollmentJobs
            .Where(j => j.Status == RemoteFaceEnrollmentJobStatuses.Pending)
            .OrderBy(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (job == null)
        {
            return null;
        }

        var claimed = await _db.RemoteFaceEnrollmentJobs
            .Where(j => j.Id == job.Id && j.Status == RemoteFaceEnrollmentJobStatuses.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.Status, RemoteFaceEnrollmentJobStatuses.Processing)
                .SetProperty(j => j.AssignedNodeId, areaNodeId)
                .SetProperty(j => j.AssignedAtUtc, DateTime.UtcNow)
                .SetProperty(j => j.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken);

        if (claimed == 0)
        {
            // Node khác vừa claim — trả null để local thử lại chu kỳ sau.
            return null;
        }

        var frames = await _db.RemoteFaceEnrollmentFrames
            .AsNoTracking()
            .Where(f => f.JobId == job.Id)
            .OrderBy(f => f.Ordinal)
            .Select(f => f.ImageData)
            .ToListAsync(cancellationToken);

        return new ClaimedFaceEnrollmentJob(
            job.Id,
            job.EmployeeId,
            frames);
    }

    /// <summary>Local báo hoàn thành: lưu kết quả template + EmployeeFaceModel Active.</summary>
    public async Task CompleteAsync(
        Guid jobId, string areaNodeId, string modelFileName,
        string? checksum, int? encodingCount, string? templateContent,
        CancellationToken cancellationToken)
    {
        var job = await _db.RemoteFaceEnrollmentJobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            throw new InvalidOperationException("Không tìm thấy job.");
        }

        job.Status = RemoteFaceEnrollmentJobStatuses.Completed;
        job.CompletedAtUtc = DateTime.UtcNow;
        job.UpdatedAtUtc = DateTime.UtcNow;
        job.ResultModelFileName = modelFileName;
        job.ResultChecksum = checksum;
        job.ResultEncodingCount = encodingCount;
        job.TemplateContent = templateContent;

        // Lưu EmployeeFaceModel Active (phát xuống mọi local qua sync).
        var activeModels = await _db.EmployeeFaceModels
            .Where(m => m.EmployeeId == job.EmployeeId && m.Status == FaceModelLifecycleStatuses.Active)
            .ToListAsync(cancellationToken);
        foreach (var item in activeModels)
        {
            item.Status = FaceModelLifecycleStatuses.Archived;
            item.ArchivedAtUtc = DateTime.UtcNow;
        }

        // Version tăng dần theo các model đã có của employee (unique (EmployeeId, Version)).
        var maxVersion = await _db.EmployeeFaceModels
            .Where(m => m.EmployeeId == job.EmployeeId)
            .OrderByDescending(m => m.Version)
            .Select(m => m.Version)
            .FirstOrDefaultAsync(cancellationToken);

        _db.EmployeeFaceModels.Add(new EmployeeFaceModel
        {
            EmployeeId = job.EmployeeId,
            ModelFileName = modelFileName,
            ModelPath = $"models/active/{modelFileName}",
            ModelChecksum = checksum,
            EncodingCount = encodingCount,
            Version = (maxVersion ?? 0) + 1,
            Status = FaceModelLifecycleStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            ActivatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Local báo thất bại. Job quay về Pending nếu chưa vượt số lần thử.</summary>
    public async Task FailAsync(
        Guid jobId, string areaNodeId, string? failureCode, string? failureMessage,
        CancellationToken cancellationToken)
    {
        var job = await _db.RemoteFaceEnrollmentJobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            throw new InvalidOperationException("Không tìm thấy job.");
        }

        job.Status = RemoteFaceEnrollmentJobStatuses.Failed;
        job.FailureCode = failureCode;
        job.FailureMessage = failureMessage;
        job.CompletedAtUtc = DateTime.UtcNow;
        job.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
    /// <summary>Trả danh sách template Face ID Active (để local tải về nạp vào Face Runtime).</summary>
    public async Task<IReadOnlyList<FaceTemplateDto>> GetActiveTemplatesAsync(CancellationToken cancellationToken)
    {
        var templates = await _db.RemoteFaceEnrollmentJobs
            .AsNoTracking()
            .Where(j => j.Status == RemoteFaceEnrollmentJobStatuses.Completed &&
                        j.TemplateContent != null &&
                        j.ResultModelFileName != null)
            .OrderByDescending(j => j.CompletedAtUtc)
            .Select(j => new FaceTemplateDto(
                j.EmployeeId,
                j.ResultModelFileName!,
                j.ResultChecksum,
                j.TemplateContent!))
            .ToListAsync(cancellationToken);

        // Chỉ giữ 1 template mới nhất cho mỗi employee.
        var byEmployee = templates
            .GroupBy(t => t.EmployeeId)
            .Select(g => g.First())
            .OrderBy(t => t.EmployeeId)
            .ToList();

        return byEmployee;
    }
}

public sealed record ClaimedFaceEnrollmentJob(Guid JobId, int EmployeeId, IReadOnlyList<string> Frames);

public sealed record FaceTemplateDto(
    int EmployeeId,
    string ModelFileName,
    string? Checksum,
    string TemplateContent);
