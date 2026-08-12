using System.Security.Claims;
using System.Text.Json;
using API.Data;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/soc")]
[Authorize]
[RequireOperationalTask("monitoring")]
public class EnterpriseSocController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISocIntelligenceService _socIntel;
    private readonly ISocIncidentCopilotService _incidentCopilot;
    private readonly INotificationService _notificationService;

    public EnterpriseSocController(ApplicationDbContext context, ISocIntelligenceService socIntel, ISocIncidentCopilotService incidentCopilot, INotificationService notificationService)
    {
        _context = context;
        _socIntel = socIntel;
        _incidentCopilot = incidentCopilot;
        _notificationService = notificationService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var oldestOpenAlarm = await _context.Alarms
            .Where(alarm => alarm.State != "Closed")
            .OrderBy(alarm => alarm.CreatedAtUtc)
            .Select(alarm => (DateTime?)alarm.CreatedAtUtc)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            OpenAlarms = await _context.Alarms.CountAsync(alarm => alarm.State != "Closed"),
            CriticalOpenAlarms = await _context.Alarms.CountAsync(alarm => alarm.State != "Closed" && alarm.Severity == "Critical"),
            ActiveSops = await _context.SopExecutions.CountAsync(sop => sop.Status == "InProgress"),
            OpenIncidents = await _context.Incidents.CountAsync(incident => incident.Status != "Closed"),
            OpenDispatchTasks = await _context.DispatchTasks.CountAsync(task => task.Status != "Completed"),
            ShiftHandovers = await _context.ShiftHandovers.CountAsync(),
            MusterSnapshots = await _context.EmergencyMusterSnapshots.CountAsync(),
            OldestOpenAlarmAgeMinutes = oldestOpenAlarm.HasValue
                ? Math.Round((DateTime.UtcNow - oldestOpenAlarm.Value).TotalMinutes, 2)
                : 0
        });
    }

    [HttpPost("alarm-rules")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAlarmRule([FromBody] AlarmRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.EventType))
            return BadRequest(new { message = "Vui lòng nhập tên và loại sự kiện." });

        var rule = new AlarmRule
        {
            Name = request.Name.Trim(),
            EventType = request.EventType.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? "Medium" : request.Severity.Trim(),
            IsActive = request.IsActive
        };

        _context.AlarmRules.Add(rule);
        await _context.SaveChangesAsync();
        return Ok(rule);
    }

    [HttpPost("alarms")]
    public async Task<IActionResult> CreateAlarm([FromBody] AlarmRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
            return BadRequest(new { message = "Vui lòng nhập tóm tắt." });

        var alarm = new Alarm
        {
            SecurityEventId = request.SecurityEventId,
            AlarmType = string.IsNullOrWhiteSpace(request.AlarmType) ? "Generic" : request.AlarmType.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? "Medium" : request.Severity.Trim(),
            State = "New",
            Summary = request.Summary.Trim(),
            SiteId = request.SiteId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Altitude = request.Altitude,
            Accuracy = request.Accuracy,
            SourceDeviceId = request.SourceDeviceId
        };

        _context.Alarms.Add(alarm);
        await _context.SaveChangesAsync();

        await _notificationService.NotifyEventAsync("Alarm.Generic", alarm.Severity == "Critical" ? "Báo động khẩn cấp" : "Báo động mới",
            alarm.Summary, "Alarm", alarm.AlarmId.ToString(), "/soc-console",
            request.Latitude, request.Longitude, null);

        _ = FireAndForgetClassifyAsync(alarm.AlarmId);

        return Ok(alarm);
    }

    private async Task FireAndForgetClassifyAsync(long alarmId)
    {
        try
        {
            var classification = await _socIntel.ClassifyAlarmAsync(alarmId);
            if (classification != null)
            {
                _context.AlarmComments.Add(new AlarmComment
                {
                    AlarmId = alarmId,
                    Comment = $"[AI Phan tich] Phan loai: {classification.PredictedSeverity} ({classification.Confidence}). " +
                        $"Loai: {classification.PredictedAlarmType}. " +
                        (classification.MatchedKeywords.Count > 0
                            ? $"Tu khoa: {string.Join(", ", classification.MatchedKeywords)}."
                            : "Khong tim thay tu khoa dac trung.")
                });
                await _context.SaveChangesAsync();
            }
        }
        catch
        {
        }
    }

    [HttpPatch("alarms/{alarmId:long}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlarm(long alarmId)
    {
        var alarm = await _context.Alarms.FindAsync(alarmId);
        if (alarm == null)
            return NotFound(new { message = "Không tìm thấy báo động." });

        alarm.State = "Acknowledged";
        alarm.AcknowledgedAtUtc = DateTime.UtcNow;
        alarm.AssignedToUserId ??= GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(alarm);
    }

    [HttpPatch("alarms/{alarmId:long}/assign")]
    public async Task<IActionResult> AssignAlarm(long alarmId, [FromBody] AlarmAssignmentRequest request)
    {
        var alarm = await _context.Alarms.FindAsync(alarmId);
        if (alarm == null)
            return NotFound(new { message = "Không tìm thấy báo động." });

        alarm.State = "Assigned";
        alarm.AssignedToUserId = request.AssignedToUserId;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            _context.AlarmComments.Add(new AlarmComment
            {
                AlarmId = alarmId,
                UserId = GetCurrentUserId(),
                Comment = request.Note.Trim()
            });
        }

        await _context.SaveChangesAsync();
        return Ok(alarm);
    }

    [HttpPost("alarms/{alarmId:long}/comments")]
    public async Task<IActionResult> AddAlarmComment(long alarmId, [FromBody] AlarmCommentRequest request)
    {
        if (!await _context.Alarms.AnyAsync(alarm => alarm.AlarmId == alarmId))
            return NotFound(new { message = "Không tìm thấy báo động." });
        if (string.IsNullOrWhiteSpace(request.Comment))
            return BadRequest(new { message = "Vui lòng nhập bình luận." });

        var comment = new AlarmComment
        {
            AlarmId = alarmId,
            UserId = GetCurrentUserId(),
            Comment = request.Comment.Trim()
        };

        _context.AlarmComments.Add(comment);
        await _context.SaveChangesAsync();
        return Ok(comment);
    }

    [HttpPatch("alarms/{alarmId:long}/close")]
    public async Task<IActionResult> CloseAlarm(long alarmId, [FromBody] CloseRequest request)
    {
        var alarm = await _context.Alarms.FindAsync(alarmId);
        if (alarm == null)
            return NotFound(new { message = "Không tìm thấy báo động." });

        alarm.State = "Closed";
        alarm.ClosedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            _context.AlarmComments.Add(new AlarmComment
            {
                AlarmId = alarmId,
                UserId = GetCurrentUserId(),
                Comment = request.Note.Trim()
            });
        }

        await _context.SaveChangesAsync();
        return Ok(alarm);
    }

    [HttpPost("sop-templates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSopTemplate([FromBody] SopTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Vui lòng nhập tên." });

        var template = new SopTemplate
        {
            Name = request.Name.Trim(),
            AlarmType = string.IsNullOrWhiteSpace(request.AlarmType) ? "Generic" : request.AlarmType.Trim(),
            Version = request.Version <= 0 ? 1 : request.Version,
            ChecklistJson = string.IsNullOrWhiteSpace(request.ChecklistJson) ? "[]" : request.ChecklistJson.Trim(),
            IsActive = true
        };

        _context.SopTemplates.Add(template);
        await _context.SaveChangesAsync();
        return Ok(template);
    }

    [HttpPost("sop-executions")]
    public async Task<IActionResult> StartSopExecution([FromBody] SopExecutionRequest request)
    {
        if (!await _context.SopTemplates.AnyAsync(template => template.SopTemplateId == request.SopTemplateId && template.IsActive))
            return BadRequest(new { message = "Không tìm thấy mẫu SOP đang hoạt động." });

        var execution = new SopExecution
        {
            AlarmId = request.AlarmId,
            IncidentId = request.IncidentId,
            SopTemplateId = request.SopTemplateId,
            Status = "InProgress",
            ExecutedByUserId = GetCurrentUserId()
        };

        _context.SopExecutions.Add(execution);
        await _context.SaveChangesAsync();
        return Ok(execution);
    }

    [HttpPatch("sop-executions/{executionId:long}/complete")]
    public async Task<IActionResult> CompleteSopExecution(long executionId, [FromBody] SopCompletionRequest request)
    {
        var execution = await _context.SopExecutions.FindAsync(executionId);
        if (execution == null)
            return NotFound(new { message = "Không tìm thấy lần thực thi SOP." });

        var template = await _context.SopTemplates.FindAsync(execution.SopTemplateId);
        if (template == null)
            return BadRequest(new { message = "Không tìm thấy mẫu SOP." });

        var requiredSteps = ExtractChecklistSteps(template.ChecklistJson, requiredOnly: true);
        var completedSteps = ExtractChecklistSteps(request.CompletedStepsJson, requiredOnly: false);
        var missingSteps = requiredSteps
            .Where(step => !completedSteps.Contains(step))
            .ToArray();
        if (missingSteps.Length > 0)
            return BadRequest(new { message = "Thiếu các bước SOP bắt buộc.", missingSteps });

        execution.Status = "Completed";
        execution.CompletedStepsJson = string.IsNullOrWhiteSpace(request.CompletedStepsJson) ? "[]" : request.CompletedStepsJson.Trim();
        execution.CompletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(execution);
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] IncidentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Vui lòng nhập tiêu đề." });

        var incident = new Incident
        {
            Title = request.Title.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? "Medium" : request.Severity.Trim(),
            Status = "Open",
            PrimaryAlarmId = request.PrimaryAlarmId,
            OwnerUserId = request.OwnerUserId ?? GetCurrentUserId()
        };

        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();
        return Ok(incident);
    }

    [HttpPost("incidents/{incidentId:long}/timeline")]
    public async Task<IActionResult> AddIncidentTimelineItem(long incidentId, [FromBody] IncidentTimelineRequest request)
    {
        if (!await _context.Incidents.AnyAsync(incident => incident.IncidentId == incidentId))
            return NotFound(new { message = "Không tìm thấy sự cố." });
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Vui lòng nhập nội dung." });

        var item = new IncidentTimelineItem
        {
            IncidentId = incidentId,
            ItemType = string.IsNullOrWhiteSpace(request.ItemType) ? "Note" : request.ItemType.Trim(),
            Text = request.Text.Trim(),
            UserId = GetCurrentUserId()
        };

        _context.IncidentTimelineItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPatch("incidents/{incidentId:long}/close")]
    public async Task<IActionResult> CloseIncident(long incidentId, [FromBody] CloseRequest request)
    {
        var incident = await _context.Incidents.FindAsync(incidentId);
        if (incident == null)
            return NotFound(new { message = "Không tìm thấy sự cố." });
        if (string.IsNullOrWhiteSpace(request.Note))
            return BadRequest(new { message = "Vui lòng nhập ghi chú kết quả trước khi đóng sự cố." });

        incident.Status = "Closed";
        incident.Outcome = request.Note.Trim();
        incident.ClosedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(incident);
    }

    [HttpPost("dispatch-tasks")]
    public async Task<IActionResult> CreateDispatchTask([FromBody] DispatchTaskRequest request)
    {
        var task = new DispatchTask
        {
            AlarmId = request.AlarmId,
            IncidentId = request.IncidentId,
            SiteId = request.SiteId,
            LocationText = string.IsNullOrWhiteSpace(request.LocationText) ? "Unspecified" : request.LocationText.Trim(),
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority.Trim(),
            Status = "Open",
            AssignedGuardUserId = request.AssignedGuardUserId,
            Instructions = string.IsNullOrWhiteSpace(request.Instructions) ? "Investigate and report." : request.Instructions.Trim()
        };

        _context.DispatchTasks.Add(task);
        await _context.SaveChangesAsync();
        return Ok(task);
    }

    [HttpPatch("dispatch-tasks/{taskId:long}/complete")]
    public async Task<IActionResult> CompleteDispatchTask(long taskId, [FromBody] CloseRequest request)
    {
        var task = await _context.DispatchTasks.FindAsync(taskId);
        if (task == null)
            return NotFound(new { message = "Không tìm thấy nhiệm vụ điều phối." });

        task.Status = "Completed";
        task.CompletedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note))
            task.Instructions = $"{task.Instructions}\nResult: {request.Note.Trim()}";

        await _context.SaveChangesAsync();
        return Ok(task);
    }

    [HttpPost("shift-handovers")]
    public async Task<IActionResult> CreateShiftHandover([FromBody] ShiftHandoverRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
            return BadRequest(new { message = "Vui lòng nhập tóm tắt." });

        var handover = new ShiftHandover
        {
            SiteId = request.SiteId,
            FromUserId = request.FromUserId ?? GetCurrentUserId(),
            ToUserId = request.ToUserId,
            Summary = request.Summary.Trim()
        };

        _context.ShiftHandovers.Add(handover);
        await _context.SaveChangesAsync();
        return Ok(handover);
    }

    [HttpPost("emergency-muster-snapshots")]
    public async Task<IActionResult> CreateEmergencyMusterSnapshot([FromBody] EmergencyMusterSnapshotRequest request)
    {
        var snapshot = new EmergencyMusterSnapshot
        {
            SiteId = request.SiteId,
            MusterPointId = request.MusterPointId,
            KnownOnsite = request.KnownOnsite,
            AccountedFor = request.AccountedFor,
            VisitorsOnsite = request.VisitorsOnsite,
            UnaccountedFor = Math.Max(0, request.KnownOnsite + request.VisitorsOnsite - request.AccountedFor)
        };

        _context.EmergencyMusterSnapshots.Add(snapshot);
        await _context.SaveChangesAsync();
        return Ok(snapshot);
    }

    [HttpGet("alarms")]
    public async Task<IActionResult> GetAlarms(
        [FromQuery] string? state, [FromQuery] string? severity,
        [FromQuery] string? alarmType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _context.Alarms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(a => a.State == state);
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(a => a.Severity == severity);
        if (!string.IsNullOrWhiteSpace(alarmType))
            query = query.Where(a => a.AlarmType == alarmType);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("alarms/{alarmId:long}")]
    public async Task<IActionResult> GetAlarm(long alarmId)
    {
        var alarm = await _context.Alarms.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AlarmId == alarmId);
        if (alarm == null)
            return NotFound(new { message = "Không tìm thấy báo động." });
        return Ok(alarm);
    }

    [HttpGet("alarms/{alarmId:long}/comments")]
    public async Task<IActionResult> GetAlarmComments(long alarmId)
    {
        if (!await _context.Alarms.AnyAsync(a => a.AlarmId == alarmId))
            return NotFound(new { message = "Không tìm thấy báo động." });

        var comments = await _context.AlarmComments.AsNoTracking()
            .Where(c => c.AlarmId == alarmId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();
        return Ok(comments);
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents(
        [FromQuery] string? status, [FromQuery] string? severity,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _context.Incidents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(i => i.Status == status);
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(i => i.Severity == severity);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.OpenedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("incidents/{incidentId:long}")]
    public async Task<IActionResult> GetIncident(long incidentId)
    {
        var incident = await _context.Incidents.AsNoTracking()
            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);
        if (incident == null)
            return NotFound(new { message = "Không tìm thấy sự cố." });
        return Ok(incident);
    }

    [HttpGet("incidents/{incidentId:long}/items")]
    public async Task<IActionResult> GetIncidentTimelineItems(long incidentId)
    {
        if (!await _context.Incidents.AnyAsync(i => i.IncidentId == incidentId))
            return NotFound(new { message = "Không tìm thấy sự cố." });

        var items = await _context.IncidentTimelineItems.AsNoTracking()
            .Where(t => t.IncidentId == incidentId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("sop-templates")]
    public async Task<IActionResult> GetSopTemplates([FromQuery] bool? activeOnly)
    {
        var query = _context.SopTemplates.AsNoTracking();
        if (activeOnly == true)
            query = query.Where(t => t.IsActive);
        return Ok(await query.OrderBy(t => t.Name).ToListAsync());
    }

    [HttpGet("sop-executions")]
    public async Task<IActionResult> GetSopExecutions(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _context.SopExecutions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("dispatch-tasks")]
    public async Task<IActionResult> GetDispatchTasks(
        [FromQuery] string? status, [FromQuery] string? priority,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _context.DispatchTasks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("intelligence")]
    public async Task<IActionResult> GetIntelligence()
    {
        var result = await _socIntel.GetIntelligenceAsync();
        return Ok(result);
    }

    [HttpGet("alarms/{alarmId:long}/classify")]
    public async Task<IActionResult> ClassifyAlarm(long alarmId)
    {
        var result = await _socIntel.ClassifyAlarmAsync(alarmId);
        if (result == null) return NotFound(new { message = "Không tìm thấy báo động." });
        return Ok(result);
    }

    [HttpGet("alarms/{alarmId:long}/recommend-sop")]
    public async Task<IActionResult> RecommendSop(long alarmId)
    {
        var result = await _socIntel.RecommendSopAsync(alarmId);
        return Ok(result);
    }

    [HttpGet("alarms/{alarmId:long}/escalation-risk")]
    public async Task<IActionResult> PredictEscalationRisk(long alarmId)
    {
        var result = await _socIntel.PredictEscalationRiskAsync(alarmId);
        if (result == null) return NotFound(new { message = "Không tìm thấy báo động." });
        return Ok(result);
    }

    /// <summary>
    /// POST /api/enterprise/soc/incidents/{incidentId:long}/ai-briefing - Phân tích incident bằng AI
    /// </summary>
    [HttpPost("incidents/{incidentId:long}/ai-briefing")]
    public async Task<IActionResult> GetIncidentAiBriefing(long incidentId)
    {
        try
        {
            var result = await _incidentCopilot.AnalyzeIncidentAsync(incidentId, GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Không tìm thấy sự cố." });
        }
    }

    [HttpGet("anomalies")]
    public async Task<IActionResult> GetAnomalies()
    {
        var result = await _socIntel.DetectAnomaliesAsync();
        return Ok(result);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static ISet<string> ExtractChecklistSteps(string? json, bool requiredOnly)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    AddStep(result, item.GetString());
                    continue;
                }

                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var isRequired = !item.TryGetProperty("required", out var required) ||
                                 required.ValueKind != JsonValueKind.False;
                if (requiredOnly && !isRequired)
                    continue;

                if (TryGetStepValue(item, "id", out var step) ||
                    TryGetStepValue(item, "name", out step) ||
                    TryGetStepValue(item, "text", out step) ||
                    TryGetStepValue(item, "step", out step))
                {
                    AddStep(result, step);
                }
            }
        }
        catch (JsonException)
        {
            AddStep(result, json);
        }

        return result;
    }

    private static bool TryGetStepValue(JsonElement item, string propertyName, out string? step)
    {
        step = null;
        if (!item.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return false;

        step = property.GetString();
        return !string.IsNullOrWhiteSpace(step);
    }

    private static void AddStep(ISet<string> result, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            result.Add(value.Trim());
    }

    public sealed record AlarmRuleRequest(string Name, string EventType, string? Severity, bool IsActive);
    public sealed record AlarmRequest(long? SecurityEventId, string? AlarmType, string? Severity, string Summary, int? SiteId,
        decimal? Latitude, decimal? Longitude, decimal? Altitude, decimal? Accuracy, string? SourceDeviceId);
    public sealed record AlarmAssignmentRequest(int? AssignedToUserId, string? Note);
    public sealed record AlarmCommentRequest(string Comment);
    public sealed record CloseRequest(string? Note);
    public sealed record SopTemplateRequest(string Name, string? AlarmType, int Version, string? ChecklistJson);
    public sealed record SopExecutionRequest(long? AlarmId, long? IncidentId, int SopTemplateId);
    public sealed record SopCompletionRequest(string? CompletedStepsJson);
    public sealed record IncidentRequest(string Title, string? Severity, long? PrimaryAlarmId, int? OwnerUserId);
    public sealed record IncidentTimelineRequest(string? ItemType, string Text);
    public sealed record DispatchTaskRequest(long? AlarmId, long? IncidentId, int? SiteId, string? LocationText, string? Priority, int? AssignedGuardUserId, string? Instructions);
    public sealed record ShiftHandoverRequest(int? SiteId, int? FromUserId, int? ToUserId, string Summary);
    public sealed record EmergencyMusterSnapshotRequest(int? SiteId, int? MusterPointId, int KnownOnsite, int AccountedFor, int VisitorsOnsite);
}
