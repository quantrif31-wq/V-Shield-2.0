using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Route("api/notification-rules")]
[ApiController]
[Authorize(Roles = "Admin,QuanLy")]
public class NotificationRulesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public NotificationRulesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _db.NotificationRules
            .OrderBy(r => r.EventType)
            .Select(r => new
            {
                r.Id,
                r.EventType,
                r.SeverityMin,
                r.RecipientUserId,
                r.RecipientRole,
                r.NotifyWeb,
                r.NotifyMobile,
                r.IsActive,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = rules });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateNotificationRuleRequest request)
    {
        var rule = new NotificationRule
        {
            EventType = request.EventType,
            SeverityMin = request.SeverityMin,
            RecipientUserId = request.RecipientUserId,
            RecipientRole = request.RecipientRole,
            NotifyWeb = request.NotifyWeb,
            NotifyMobile = request.NotifyMobile,
            IsActive = request.IsActive
        };

        _db.NotificationRules.Add(rule);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new { rule.Id } });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] CreateNotificationRuleRequest request)
    {
        var rule = await _db.NotificationRules.FindAsync(id);
        if (rule == null)
            return NotFound(new { success = false, message = "Không tìm thấy rule." });

        rule.EventType = request.EventType;
        rule.SeverityMin = request.SeverityMin;
        rule.RecipientUserId = request.RecipientUserId;
        rule.RecipientRole = request.RecipientRole;
        rule.NotifyWeb = request.NotifyWeb;
        rule.NotifyMobile = request.NotifyMobile;
        rule.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var rule = await _db.NotificationRules.FindAsync(id);
        if (rule == null)
            return NotFound(new { success = false, message = "Không tìm thấy rule." });

        _db.NotificationRules.Remove(rule);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Gợi ý EventType phù hợp với vai trò
    /// </summary>
    [HttpGet("suggestions")]
    public IActionResult GetSuggestions([FromQuery] string? role)
    {
        var suggestions = new List<object>
        {
            new { EventType = "Alarm.Duress", Label = "Báo động uy hiếp", SuggestedRoles = "BaoVe,Admin" },
            new { EventType = "Alarm.EmergencyPass", Label = "Vượt cổng khẩn cấp", SuggestedRoles = "BaoVe,Admin" },
            new { EventType = "Alarm.DeviceOffline", Label = "Thiết bị mất kết nối", SuggestedRoles = "BaoVe" },
            new { EventType = "Alarm.VisitorOverstay", Label = "Khách ở quá giờ", SuggestedRoles = "BaoVe" },
            new { EventType = "Alarm.Generic", Label = "Báo động chung", SuggestedRoles = "BaoVe,Admin" },
            new { EventType = "Approval.LeaveRequest.Submitted", Label = "Đơn nghỉ phép mới", SuggestedRoles = "Admin,QuanLy,NhanSu" },
            new { EventType = "Approval.LeaveRequest.Approved", Label = "Đơn nghỉ phép đã duyệt", SuggestedRoles = "*" },
            new { EventType = "Approval.LeaveRequest.Rejected", Label = "Đơn nghỉ phép bị từ chối", SuggestedRoles = "*" },
            new { EventType = "Approval.VehicleDelegation.Created", Label = "Yêu cầu điều xe mới", SuggestedRoles = "*" },
            new { EventType = "Approval.VehicleDelegation.Approved", Label = "Điều xe đã chấp nhận", SuggestedRoles = "*" },
            new { EventType = "Approval.VehicleDelegation.Rejected", Label = "Điều xe bị từ chối", SuggestedRoles = "*" },
            new { EventType = "Approval.Intervention.Created", Label = "Yêu cầu can thiệp mới", SuggestedRoles = "Admin,QuanLy" },
            new { EventType = "Approval.Intervention.Accepted", Label = "Can thiệp đã chấp nhận", SuggestedRoles = "BaoVe" },
            new { EventType = "Approval.Intervention.Rejected", Label = "Can thiệp bị từ chối", SuggestedRoles = "BaoVe" },
            new { EventType = "Approval.LostFoundClaim.Created", Label = "Yêu cầu nhận đồ thất lạc", SuggestedRoles = "Admin,BaoVe,LeTan" },
            new { EventType = "Approval.EvidenceExport.Created", Label = "Yêu cầu xuất bằng chứng", SuggestedRoles = "Admin" },
            new { EventType = "Approval.EvidenceRedaction.Created", Label = "Yêu cầu làm mờ bằng chứng", SuggestedRoles = "Admin" },
        };

        if (!string.IsNullOrEmpty(role))
        {
            var filtered = suggestions.Where(s =>
            {
                var roles = (string)((dynamic)s).SuggestedRoles;
                return roles == "*" || roles.Split(',').Contains(role, StringComparer.OrdinalIgnoreCase);
            }).ToList();
            return Ok(new { success = true, data = filtered });
        }

        return Ok(new { success = true, data = suggestions });
    }
}

public class CreateNotificationRuleRequest
{
    public string EventType { get; set; } = string.Empty;
    public string? SeverityMin { get; set; }
    public int? RecipientUserId { get; set; }
    public string? RecipientRole { get; set; }
    public bool NotifyWeb { get; set; } = true;
    public bool NotifyMobile { get; set; }
    public bool IsActive { get; set; } = true;
}
