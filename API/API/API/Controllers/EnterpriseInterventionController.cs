using System.Security.Claims;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/intervention")]
[Authorize(Roles = "Admin,BaoVe")]
public class EnterpriseInterventionController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EnterpriseInterventionController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/enterprise/intervention/overview - Thống kê tổng quan
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            PendingCount = await _context.OperationalInterventionRequests.CountAsync(r => r.Status == "Pending"),
            AcceptedCount = await _context.OperationalInterventionRequests.CountAsync(r => r.Status == "Accepted"),
            RejectedCount = await _context.OperationalInterventionRequests.CountAsync(r => r.Status == "Rejected"),
            ExecutedCount = await _context.OperationalInterventionRequests.CountAsync(r => r.Status == "Executed"),
            ExpiredCount = await _context.OperationalInterventionRequests.CountAsync(r => r.Status == "Expired"),
            TotalCount = await _context.OperationalInterventionRequests.CountAsync(),
            OldestPendingMinutes = await GetOldestPendingAgeMinutesAsync()
        });
    }

    /// <summary>
    /// POST /api/enterprise/intervention/requests - Tạo yêu cầu can thiệp (BaoVe)
    /// </summary>
    [HttpPost("requests")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateInterventionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InterventionType))
            return BadRequest(new { message = "InterventionType is required." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Reason is required." });

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Cannot identify user." });

        var now = DateTime.UtcNow;
        var item = new OperationalInterventionRequest
        {
            RequestedByUserId = userId.Value,
            LaneId = request.LaneId?.Trim(),
            LaneName = request.LaneName?.Trim(),
            InterventionType = request.InterventionType.Trim(),
            SubjectName = request.SubjectName?.Trim(),
            SubjectId = request.SubjectId?.Trim(),
            SubjectType = request.SubjectType?.Trim(),
            PlateNumber = request.PlateNumber?.Trim(),
            QrPayload = request.QrPayload?.Trim(),
            Reason = request.Reason.Trim(),
            Note = request.Note?.Trim(),
            Status = "Pending",
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "medium" : request.Priority.Trim(),
            CreatedAtUtc = now,
            ExpiresAtUtc = request.ExpiresInMinutes > 0
                ? now.AddMinutes(request.ExpiresInMinutes)
                : now.AddHours(4) // Mặc định 4 giờ
        };

        _context.OperationalInterventionRequests.Add(item);
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    /// <summary>
    /// GET /api/enterprise/intervention/requests - Lấy danh sách yêu cầu
    /// </summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? interventionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.OperationalInterventionRequests
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(r => r.Priority == priority.Trim());
        if (!string.IsNullOrWhiteSpace(interventionType))
            query = query.Where(r => r.InterventionType == interventionType.Trim());

        // Role filtering: BaoVe only sees their own requests
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        var userId = GetCurrentUserId();
        if (currentRole == "BaoVe" && userId.HasValue)
        {
            query = query.Where(r => r.RequestedByUserId == userId.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Priority == "critical" ? 0 :
                                     r.Priority == "high" ? 1 :
                                     r.Priority == "medium" ? 2 : 3)
            .ThenBy(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// GET /api/enterprise/intervention/requests/{requestId:long} - Chi tiết yêu cầu
    /// </summary>
    [HttpGet("requests/{requestId:long}")]
    public async Task<IActionResult> GetRequestDetail(long requestId)
    {
        var item = await _context.OperationalInterventionRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OperationalInterventionRequestId == requestId);

        if (item == null)
            return NotFound(new { message = "Intervention request not found." });

        return Ok(item);
    }

    /// <summary>
    /// PATCH /api/enterprise/intervention/requests/{requestId:long}/accept - Admin chấp nhận
    /// </summary>
    [HttpPatch("requests/{requestId:long}/accept")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AcceptRequest(long requestId, [FromBody] AcceptRejectRequest request)
    {
        var item = await _context.OperationalInterventionRequests.FindAsync(requestId);
        if (item == null)
            return NotFound(new { message = "Intervention request not found." });
        if (item.Status != "Pending")
            return BadRequest(new { message = $"Request is in '{item.Status}' state, cannot accept." });

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Cannot identify user." });

        item.Status = "Accepted";
        item.AcceptedAtUtc = DateTime.UtcNow;
        item.AcceptedByUserId = userId;
        if (!string.IsNullOrWhiteSpace(request.Note))
            item.Note = request.Note.Trim();

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>
    /// PATCH /api/enterprise/intervention/requests/{requestId:long}/reject - Admin từ chối
    /// </summary>
    [HttpPatch("requests/{requestId:long}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectRequest(long requestId, [FromBody] AcceptRejectRequest request)
    {
        var item = await _context.OperationalInterventionRequests.FindAsync(requestId);
        if (item == null)
            return NotFound(new { message = "Intervention request not found." });
        if (item.Status != "Pending")
            return BadRequest(new { message = $"Request is in '{item.Status}' state, cannot reject." });

        if (string.IsNullOrWhiteSpace(request.Note))
            return BadRequest(new { message = "Rejection reason is required." });

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Cannot identify user." });

        item.Status = "Rejected";
        item.RejectedAtUtc = DateTime.UtcNow;
        item.RejectedByUserId = userId;
        item.RejectionReason = request.Note.Trim();

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>
    /// PATCH /api/enterprise/intervention/requests/{requestId:long}/execute - Admin thực thi
    /// </summary>
    [HttpPatch("requests/{requestId:long}/execute")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExecuteRequest(long requestId, [FromBody] ExecuteRequestPayload request)
    {
        var item = await _context.OperationalInterventionRequests.FindAsync(requestId);
        if (item == null)
            return NotFound(new { message = "Intervention request not found." });
        if (item.Status != "Accepted")
            return BadRequest(new { message = $"Request is in '{item.Status}' state, must be 'Accepted' before execution." });

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Cannot identify user." });

        item.Status = "Executed";
        item.ExecutedAtUtc = DateTime.UtcNow;
        item.ExecutedByUserId = userId;
        if (!string.IsNullOrWhiteSpace(request.Note))
            item.Note = request.Note.Trim();

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>
    /// POST /api/enterprise/intervention/requests/expire - Hết hạn các request quá hạn (có thể gọi từ background job)
    /// </summary>
    [HttpPost("requests/expire")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExpireOverdueRequests()
    {
        var now = DateTime.UtcNow;
        var expired = await _context.OperationalInterventionRequests
            .Where(r => r.Status == "Pending" && r.ExpiresAtUtc != null && r.ExpiresAtUtc <= now)
            .ToListAsync();

        foreach (var item in expired)
        {
            item.Status = "Expired";
        }

        await _context.SaveChangesAsync();
        return Ok(new { expiredCount = expired.Count });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task<double> GetOldestPendingAgeMinutesAsync()
    {
        var oldest = await _context.OperationalInterventionRequests
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => (DateTime?)r.CreatedAtUtc)
            .FirstOrDefaultAsync();

        return oldest.HasValue ? Math.Round((DateTime.UtcNow - oldest.Value).TotalMinutes, 2) : 0;
    }

    // ===================== DTOs =====================

    public sealed record CreateInterventionRequest(
        string InterventionType,
        string Reason,
        string? LaneId,
        string? LaneName,
        string? SubjectName,
        string? SubjectId,
        string? SubjectType,
        string? PlateNumber,
        string? QrPayload,
        string? Note,
        string? Priority,
        int ExpiresInMinutes);

    public sealed record AcceptRejectRequest(string? Note);

    public sealed record ExecuteRequestPayload(string? Note);
}
