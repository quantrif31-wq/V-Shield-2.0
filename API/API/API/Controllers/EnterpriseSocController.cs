using System.Security.Claims;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/soc")]
[Authorize(Roles = "Admin,BaoVe")]
public class EnterpriseSocController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EnterpriseSocController(ApplicationDbContext context)
    {
        _context = context;
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
            return BadRequest(new { message = "Name and EventType are required." });

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
            return BadRequest(new { message = "Summary is required." });

        var alarm = new Alarm
        {
            SecurityEventId = request.SecurityEventId,
            AlarmType = string.IsNullOrWhiteSpace(request.AlarmType) ? "Generic" : request.AlarmType.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? "Medium" : request.Severity.Trim(),
            State = "New",
            Summary = request.Summary.Trim(),
            SiteId = request.SiteId
        };

        _context.Alarms.Add(alarm);
        await _context.SaveChangesAsync();
        return Ok(alarm);
    }

    [HttpPatch("alarms/{alarmId:long}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlarm(long alarmId)
    {
        var alarm = await _context.Alarms.FindAsync(alarmId);
        if (alarm == null)
            return NotFound(new { message = "Alarm not found." });

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
            return NotFound(new { message = "Alarm not found." });

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
            return NotFound(new { message = "Alarm not found." });
        if (string.IsNullOrWhiteSpace(request.Comment))
            return BadRequest(new { message = "Comment is required." });

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
            return NotFound(new { message = "Alarm not found." });

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
            return BadRequest(new { message = "Name is required." });

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
            return BadRequest(new { message = "Active SOP template not found." });

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
            return NotFound(new { message = "SOP execution not found." });

        var template = await _context.SopTemplates.FindAsync(execution.SopTemplateId);
        if (template == null)
            return BadRequest(new { message = "SOP template not found." });

        var requiredSteps = ExtractChecklistSteps(template.ChecklistJson, requiredOnly: true);
        var completedSteps = ExtractChecklistSteps(request.CompletedStepsJson, requiredOnly: false);
        var missingSteps = requiredSteps
            .Where(step => !completedSteps.Contains(step))
            .ToArray();
        if (missingSteps.Length > 0)
            return BadRequest(new { message = "Required SOP steps are missing.", missingSteps });

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
            return BadRequest(new { message = "Title is required." });

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
            return NotFound(new { message = "Incident not found." });
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Text is required." });

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
            return NotFound(new { message = "Incident not found." });
        if (string.IsNullOrWhiteSpace(request.Note))
            return BadRequest(new { message = "Outcome note is required before incident closure." });

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
            return NotFound(new { message = "Dispatch task not found." });

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
            return BadRequest(new { message = "Summary is required." });

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
    public sealed record AlarmRequest(long? SecurityEventId, string? AlarmType, string? Severity, string Summary, int? SiteId);
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
