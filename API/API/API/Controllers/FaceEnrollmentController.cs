using System.Security.Claims;
using System.Text.Json;
using API.Data;
using API.Models;
using API.Services.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;

namespace API.Controllers;

/// <summary>
/// Nhân viên tự đăng ký Face ID cho chính tài khoản của mình (không cần chọn
/// nhân viên — EmployeeId được lấy từ tài khoản đang đăng nhập). Không yêu cầu
/// quyền operational "monitoring", chỉ cần đã đăng nhập.
/// </summary>
[Route("api/FaceEnrollment")]
[ApiController]
[Authorize]
public class FaceEnrollmentController : ControllerBase
{
    private readonly IFaceRecognitionClient _faceRecognitionClient;
    private readonly ApplicationDbContext _context;

    public FaceEnrollmentController(
        IFaceRecognitionClient faceRecognitionClient,
        ApplicationDbContext context)
    {
        _faceRecognitionClient = faceRecognitionClient;
        _context = context;
    }

    /// <summary>Trạng thái Face ID của tài khoản đang đăng nhập.</summary>
    [HttpGet("my-status")]
    public async Task<IActionResult> GetMyFaceStatus(CancellationToken cancellationToken)
    {
        var employeeId = await ResolveCurrentEmployeeIdAsync(cancellationToken);
        if (employeeId == null)
        {
            return Ok(new
            {
                hasEmployee = false,
                message = "Tài khoản chưa gắn với nhân viên."
            });
        }

        var model = await _context.EmployeeFaceModels
            .Where(m => m.EmployeeId == employeeId.Value && m.Status == FaceModelLifecycleStatuses.Active)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            hasEmployee = true,
            employeeId = employeeId.Value,
            hasFaceId = model != null,
            modelFileName = model?.ModelFileName,
            checksum = model?.ModelChecksum,
            encodingCount = model?.EncodingCount,
            version = model?.Version,
            activatedAtUtc = model?.ActivatedAtUtc
        });
    }

    /// <summary>
    /// Gỡ Face ID của tài khoản đang đăng nhập (chỉ bản Active). Không xóa
    /// mẫu vật lý, chỉ chuyển trạng thái về Archived để dừng nhận diện.
    /// </summary>
    [HttpDelete("self-face-id")]
    public async Task<IActionResult> DeleteMyFaceId(CancellationToken cancellationToken)
    {
        var employeeId = await ResolveCurrentEmployeeIdAsync(cancellationToken);
        if (employeeId == null)
        {
            return BadRequest(new
            {
                message = "Tài khoản chưa gắn với nhân viên. Liên hệ quản trị viên để được cấu hình."
            });
        }

        var active = await _context.EmployeeFaceModels
            .Where(m => m.EmployeeId == employeeId.Value && m.Status == FaceModelLifecycleStatuses.Active)
            .ToListAsync(cancellationToken);

        if (active.Count == 0)
        {
            return Ok(new { success = true, removed = false, message = "Bạn chưa có Face ID." });
        }

        var now = DateTime.UtcNow;
        foreach (var item in active)
        {
            item.Status = FaceModelLifecycleStatuses.Archived;
            item.ArchivedAtUtc = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Xóa model vật lý khỏi registry face-runtime để dừng nhận diện ngay.
        var revoke = await _faceRecognitionClient.RevokeSubjectModelAsync(
            employeeId.Value.ToString(), cancellationToken);
        var revoked = revoke != null && (int)revoke.StatusCode is >= 200 and < 300;

        return Ok(new
        {
            success = true,
            removed = true,
            removedCount = active.Count,
            runtimeRevoked = revoked,
            message = revoked
                ? "Đã gỡ Face ID. Bạn có thể đăng ký mới bất cứ lúc nào."
                : "Đã gỡ Face ID trong hệ thống, nhưng runtime chưa xác nhận. Thử lại nếu cần."
        });
    }

    /// <summary>
    /// Đăng ký Face ID bằng một loạt ảnh khuôn mặt (webcam thiết bị).
    /// EmployeeId được lấy tự động từ tài khoản đang đăng nhập.
    /// </summary>
    [HttpPost("enroll-self")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> EnrollSelf(
        [FromBody] FaceEnrollmentSelfRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Images is null || request.Images.Count == 0 || request.Images.Count > 200)
        {
            return BadRequest(new { message = "Vui lòng cung cấp từ 1 đến 200 ảnh khuôn mặt." });
        }

        var employeeId = await ResolveCurrentEmployeeIdAsync(cancellationToken);
        if (employeeId == null)
        {
            return BadRequest(new
            {
                message = "Tài khoản chưa gắn với nhân viên. Liên hệ quản trị viên để được cấu hình."
            });
        }

        var response = await _faceRecognitionClient.LiveEnrollAsync(
            employeeId.Value.ToString(), request.Images, cancellationToken);

        if ((int)response.StatusCode is < 200 or >= 300)
        {
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = response.Body,
                ContentType = response.ContentType ?? "application/json"
            };
        }

        // Ghi nhận vào bảng EmployeeFaceModel để quản lý qua DB.
        var payload = TryParseJson(response.Body);
        var modelFileName = GetString(payload, "modelFileName");
        var checksum = GetString(payload, "checksum");
        var encodingCount = GetInt(payload, "encodingCount");
        var registryVersion = GetInt(payload, "registryVersion");

        if (!string.IsNullOrWhiteSpace(modelFileName))
        {
            await UpsertActiveFaceModelAsync(
                employeeId.Value, modelFileName, checksum, encodingCount,
                cancellationToken);
        }

        return Ok(new
        {
            success = true,
            employeeId = employeeId.Value,
            modelFileName,
            checksum,
            encodingCount,
            registryVersion,
            message = "Đăng ký Face ID thành công."
        });
    }

    /// <summary>
    /// Khách gửi ảnh khuôn mặt để đăng ký từ xa. Tạo job chờ local node (có AI)
    /// xử lý qua cơ chế sync. Dùng khi backend chạy ở VPS (không có AI).
    /// </summary>
    [HttpPost("submit-remote")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> SubmitRemote(
        [FromBody] FaceEnrollmentSelfRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Images is null || request.Images.Count == 0 || request.Images.Count > 200)
        {
            return BadRequest(new { message = "Vui lòng cung cấp từ 1 đến 200 ảnh khuôn mặt." });
        }

        var employeeId = await ResolveCurrentEmployeeIdAsync(cancellationToken);
        if (employeeId == null)
        {
            return BadRequest(new
            {
                message = "Tài khoản chưa gắn với nhân viên. Liên hệ quản trị viên để được cấu hình."
            });
        }

        var user = await _context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId.Value, cancellationToken);

        var job = new RemoteFaceEnrollmentJob
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId.Value,
            Status = RemoteFaceEnrollmentJobStatuses.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var ordinal = 0;
        foreach (var image in request.Images)
        {
            job.Frames.Add(new RemoteFaceEnrollmentFrame
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                Ordinal = ordinal++,
                ImageData = image
            });
        }

        _context.RemoteFaceEnrollmentJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            jobId = job.Id,
            status = job.Status,
            message = "Đã tiếp nhận ảnh. Hệ thống sẽ xử lý và thông báo kết quả sau."
        });
    }

    private async Task UpsertActiveFaceModelAsync(
        int employeeId,
        string modelFileName,
        string? checksum,
        int? encodingCount,
        CancellationToken cancellationToken)
    {
        var existing = await _context.EmployeeFaceModels
            .Where(m => m.EmployeeId == employeeId && m.Status == FaceModelLifecycleStatuses.Active)
            .ToListAsync(cancellationToken);

        foreach (var item in existing)
        {
            item.Status = FaceModelLifecycleStatuses.Archived;
            item.ArchivedAtUtc = DateTime.UtcNow;
        }

        var maxVersion = await _context.EmployeeFaceModels
            .Where(m => m.EmployeeId == employeeId)
            .OrderByDescending(m => m.Version)
            .Select(m => m.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var model = new EmployeeFaceModel
        {
            EmployeeId = employeeId,
            ModelFileName = modelFileName,
            ModelPath = $"models/active/{modelFileName}",
            ModelChecksum = checksum,
            EncodingCount = encodingCount,
            Version = (maxVersion ?? 0) + 1,
            Status = FaceModelLifecycleStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            ActivatedAtUtc = DateTime.UtcNow
        };
        _context.EmployeeFaceModels.Add(model);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int?> ResolveCurrentEmployeeIdAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        var user = await _context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        return user?.EmployeeId;
    }

    private static JsonElement? TryParseJson(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement? element, string property)
    {
        if (element is { } el && el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }
        return null;
    }

    private static int? GetInt(JsonElement? element, string property)
    {
        if (element is { } el && el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }
        return null;
    }
}

public sealed record FaceEnrollmentSelfRequest(IReadOnlyList<string> Images);
