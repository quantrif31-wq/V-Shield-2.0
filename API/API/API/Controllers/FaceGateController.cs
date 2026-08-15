using API.Data;
using API.Models;
using API.Services.AccessPolicyComparison;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers;

/// <summary>
/// Gate-based face access: chọn cổng (xác nhận mật khẩu), kiểm tra quyền theo
/// cổng, ghi nhận điểm danh khi được phép và lưu kẻ xâm nhập khi bị từ chối.
/// </summary>
[ApiController]
[Route("api/face-gate")]
[Authorize]
public class FaceGateController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILegacyGateAccessEvaluator _legacyEvaluator;

    public FaceGateController(ApplicationDbContext db, ILegacyGateAccessEvaluator legacyEvaluator)
    {
        _db = db;
        _legacyEvaluator = legacyEvaluator;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out var id) ? id : (int?)null;
    }

    private int? GetCurrentEmployeeId()
    {
        var claim = User.FindFirstValue("employeeId");
        return int.TryParse(claim, out var id) ? id : (int?)null;
    }

    /// <summary>Danh sách cổng (chọn để quét).</summary>
    [HttpGet("gates")]
    public async Task<IActionResult> GetGates(CancellationToken cancellationToken)
    {
        var gates = await _db.Gates
            .AsNoTracking()
            .OrderBy(g => g.GateName)
            .Select(g => new
            {
                g.GateId,
                g.GateName,
                g.Location,
                CameraCount = _db.Cameras.Count(c => c.GateId == g.GateId)
            })
            .ToListAsync(cancellationToken);
        return Ok(new { success = true, gates });
    }

    /// <summary>Xác nhận mật khẩu người dùng hiện tại (cho thao tác chọn/đổi cổng).</summary>
    [HttpPost("verify-password")]
    public async Task<IActionResult> VerifyPassword([FromBody] FaceGateVerifyPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Vui lòng nhập mật khẩu." });

        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId.Value, cancellationToken);
        if (user == null) return Unauthorized(new { message = "Người dùng không tồn tại." });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Mật khẩu không đúng." });

        return Ok(new { success = true, message = "Xác thực thành công." });
    }

    /// <summary>
    /// Kiểm tra quyền truy cập của một nhân viên qua cổng cụ thể.
    /// Trả về allow/deny/unknown + blacklist + thông tin nhân viên.
    /// </summary>
    [HttpGet("check-access")]
    public async Task<IActionResult> CheckAccess([FromQuery] int employeeId, [FromQuery] int? gateId,
        CancellationToken cancellationToken)
    {
        if (employeeId <= 0)
            return BadRequest(new { message = "employeeId không hợp lệ." });

        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);

        var payload = new
        {
            success = true,
            employeeId,
            employeeName = employee?.FullName,
            known = employee != null,
            blacklist = false,
            blacklistReason = (string?)null,
            gateId,
            allowed = (bool?)null,
            reason = (string?)null
        };

        if (employee == null)
        {
            payload = payload with { reason = "unknown-employee" };
            return Ok(payload);
        }

        // Blacklist check: WatchlistEntry person matched by employee identifier.
        var blacklist = await _db.WatchlistEntries
            .AsNoTracking()
            .Where(w => w.IsActive && w.EntityType == "Person")
            .ToListAsync(cancellationToken);
        var matchedBlack = blacklist.FirstOrDefault(w =>
            !string.IsNullOrWhiteSpace(w.Identifier) &&
            string.Equals(w.Identifier.Trim(), employeeId.ToString(), StringComparison.OrdinalIgnoreCase));
        if (matchedBlack != null)
        {
            payload = payload with
            {
                blacklist = true,
                blacklistReason = matchedBlack.Reason,
                allowed = false,
                reason = "blacklist"
            };
            return Ok(payload);
        }

        if (!gateId.HasValue)
        {
            payload = payload with { reason = "no-gate" };
            return Ok(payload);
        }

        var evaluation = await _legacyEvaluator.EvaluateAsync(
            new LegacyGateEvaluationInput(employeeId, gateId.Value, DateTime.UtcNow),
            cancellationToken);

        var allowed = string.Equals(evaluation.Decision, PolicyEvaluationDecisions.Allow, StringComparison.OrdinalIgnoreCase);
        return Ok(payload with
        {
            gateId = gateId.Value,
            allowed = allowed,
            reason = allowed ? "allowed" : evaluation.ReasonCode
        });
    }

    /// <summary>
    /// Ghi nhận một kết quả quét face: nếu allowed → ghi ZoneTransit + attendance
    /// (chấm công FaceAI); nếu denied/unknown/blacklist → lưu kẻ xâm nhập.
    /// </summary>
    [HttpPost("record")]
    public async Task<IActionResult> Record([FromBody] FaceGateRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "Body bắt buộc." });

        if (string.Equals(request.Decision, "allowed", StringComparison.OrdinalIgnoreCase))
        {
            if (request.EmployeeId <= 0)
                return BadRequest(new { message = "employeeId bắt buộc khi được phép." });

            var gate = request.GateId.HasValue
                ? await _db.Gates.AsNoTracking().FirstOrDefaultAsync(g => g.GateId == request.GateId, cancellationToken)
                : null;

            // Ghi ZoneTransit (FaceAI) để hệ thống chấm công suy ra check-in/check-out.
            var lane = request.LaneId.HasValue
                ? await _db.Lanes.AsNoTracking().FirstOrDefaultAsync(l => l.LaneId == request.LaneId, cancellationToken)
                : null;
            int? zoneId = null;
            if (lane?.AccessPointId is { } apId)
            {
                zoneId = await _db.AccessPoints.AsNoTracking()
                    .Where(a => a.AccessPointId == apId)
                    .Select(a => a.SecurityZoneId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            zoneId ??= await _db.SecurityZones.AsNoTracking()
                .Where(z => z.IsActive)
                .Select(z => (int?)z.SecurityZoneId)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;

            var transit = new ZoneTransit
            {
                EmployeeId = request.EmployeeId,
                SecurityZoneId = zoneId.Value,
                Timestamp = DateTime.UtcNow,
                Direction = string.Equals(request.Direction, "OUT", StringComparison.OrdinalIgnoreCase) ? "OUT" : "IN",
                Source = ZoneTransitSources.FaceAi,
                IsAutoDerived = false
            };
            _db.ZoneTransits.Add(transit);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                decision = "allowed",
                employeeId = request.EmployeeId,
                gateId = request.GateId,
                gateName = gate?.GateName,
                attendanceRecorded = true
            });
        }

        // denied / blacklist / unknown -> intruder
        var intruder = new FaceIntruder
        {
            CameraId = request.CameraId,
            GateId = request.GateId,
            GateName = request.GateName,
            EmployeeId = request.EmployeeId > 0 ? request.EmployeeId : null,
            EmployeeName = request.EmployeeName,
            Reason = string.Equals(request.Decision, "blacklist", StringComparison.OrdinalIgnoreCase)
                ? FaceIntruderReasons.Blacklist
                : request.EmployeeId > 0 ? FaceIntruderReasons.Denied : FaceIntruderReasons.Unknown,
            ReasonDetail = request.ReasonDetail,
            Distance = request.Distance,
            SnapshotBase64 = request.SnapshotBase64,
            FaceCropBase64 = request.FaceCropBase64,
            OccurredAtUtc = DateTime.UtcNow
        };
        _db.FaceIntruders.Add(intruder);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, decision = intruder.Reason, intruderId = intruder.Id });
    }

    /// <summary>Danh sách kẻ xâm nhập (mỗi người 1 thẻ).</summary>
    [HttpGet("intruders")]
    public async Task<IActionResult> GetIntruders(CancellationToken cancellationToken,
        [FromQuery] string? reason,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.FaceIntruders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(reason))
            query = query.Where(i => i.Reason == reason);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.OccurredAtUtc)
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(Math.Min(100, Math.Max(1, pageSize)))
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, total, page, items });
    }

    [HttpDelete("intruders/{id:int}")]
    public async Task<IActionResult> DeleteIntruder(int id, CancellationToken cancellationToken)
    {
        var intruder = await _db.FaceIntruders.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (intruder == null)
            return NotFound(new { message = "Không tìm thấy." });
        _db.FaceIntruders.Remove(intruder);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true, deleted = id });
    }
}

public sealed record FaceGateVerifyPasswordRequest(string? Password);

public sealed record FaceGateRecordRequest(
    int EmployeeId,
    string? Decision = null,
    string? EmployeeName = null,
    int? GateId = null,
    string? GateName = null,
    int? LaneId = null,
    string? Direction = "IN",
    string? CameraId = null,
    string? ReasonDetail = null,
    double? Distance = null,
    string? SnapshotBase64 = null,
    string? FaceCropBase64 = null);
