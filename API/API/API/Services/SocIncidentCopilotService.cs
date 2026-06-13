using API.Data;
using API.Models;
using API.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface ISocIncidentCopilotService
{
    /// <summary>
    /// Phân tích incident và tạo AI briefing.
    /// </summary>
    Task<AiRecommendationResult> AnalyzeIncidentAsync(long incidentId, int? requestedByUserId);
}

public class SocIncidentCopilotService : ISocIncidentCopilotService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiRecommendationService _aiRec;

    public SocIncidentCopilotService(ApplicationDbContext db, IAiRecommendationService aiRec)
    {
        _db = db;
        _aiRec = aiRec;
    }

    public async Task<AiRecommendationResult> AnalyzeIncidentAsync(long incidentId, int? requestedByUserId)
    {
        // 1. Lấy thông tin incident
        var incident = await _db.Incidents.AsNoTracking()
            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

        if (incident == null)
            throw new KeyNotFoundException($"Incident {incidentId} not found.");

        // 2. Lấy alarm liên quan (chỉ qua PrimaryAlarmId vì không có junction table)
        var alarms = new List<Alarm>();
        if (incident.PrimaryAlarmId.HasValue)
        {
            var primaryAlarm = await _db.Alarms.AsNoTracking()
                .FirstOrDefaultAsync(a => a.AlarmId == incident.PrimaryAlarmId);
            if (primaryAlarm != null)
                alarms.Add(primaryAlarm);
        }

        // 3. Lấy access logs liên quan theo thời gian
        var accessLogs = await _db.AccessLogs.AsNoTracking()
            .Where(l => l.Timestamp >= incident.OpenedAtUtc.AddHours(-1)
                && l.Timestamp <= (incident.ClosedAtUtc ?? DateTime.UtcNow))
            .OrderByDescending(l => l.Timestamp)
            .Take(20)
            .ToListAsync();

        // 4. Lấy SOP executions và template riêng (không có navigation property)
        var sopExecutions = await _db.SopExecutions.AsNoTracking()
            .Where(s => s.IncidentId == incidentId || (alarms.Any() && s.AlarmId == alarms.First().AlarmId))
            .ToListAsync();

        var sopTemplateIds = sopExecutions.Select(s => s.SopTemplateId).Distinct().ToList();
        var sopTemplates = await _db.SopTemplates.AsNoTracking()
            .Where(t => sopTemplateIds.Contains(t.SopTemplateId))
            .ToDictionaryAsync(t => t.SopTemplateId, t => t.Name);

        // 5. Lấy timeline items
        var timeline = await _db.IncidentTimelineItems.AsNoTracking()
            .Where(t => t.IncidentId == incidentId)
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync();

        // 6. Lấy dispatch tasks
        var dispatchTasks = await _db.DispatchTasks.AsNoTracking()
            .Where(t => t.IncidentId == incidentId)
            .ToListAsync();

        // 7. Lấy device health info (gần đây nhất)
        var deviceSnapshots = await _db.DeviceHealthSnapshots.AsNoTracking()
            .OrderByDescending(d => d.CapturedAtUtc)
            .Take(10)
            .ToListAsync();

        // Gom dữ liệu
        string Truncate(string? value, int maxLen) =>
            string.IsNullOrEmpty(value) ? "N/A" : (value.Length > maxLen ? value[..maxLen] : value);

        var alarmSummary = alarms.Any()
            ? string.Join("; ", alarms.Select(a => $"[{a.Severity}] {a.AlarmType}: {Truncate(a.Summary, 100)}"))
            : "Khong co alarm lien quan";

        var accessSummary = accessLogs.Any()
            ? string.Join("; ", accessLogs.Select(l =>
                $"{l.Timestamp:HH:mm} - {l.Direction} - {l.ResultStatus} - Gate:{l.GateId}"))
            : "Khong co access logs trong khung thoi gian";

        var sopSummary = sopExecutions.Any()
            ? string.Join("; ", sopExecutions.Select(s =>
            {
                var name = sopTemplates.TryGetValue(s.SopTemplateId, out var n) ? n : "N/A";
                return $"SOP:{name} - {s.Status}";
            }))
            : "Khong co SOP nao duoc chay";

        var timelineSummary = timeline.Any()
            ? string.Join("; ", timeline.Select(t =>
                $"{t.CreatedAtUtc:HH:mm} - [{t.ItemType}] {Truncate(t.Text, 80)}"))
            : "Chua co timeline item";

        var dispatchSummary = dispatchTasks.Any()
            ? string.Join("; ", dispatchTasks.Select(d =>
                $"{d.Priority} - {d.Status} - {d.LocationText}"))
            : "Chua co dispatch task";

        var inputData = new Dictionary<string, string>
        {
            ["alarm_summary"] = Truncate(alarmSummary, 1000),
            ["severity"] = incident.Severity,
            ["timestamp"] = incident.OpenedAtUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["access_logs"] = Truncate(accessSummary, 1000),
            ["device_info"] = deviceSnapshots.Any()
                ? string.Join("; ", deviceSnapshots.Take(5).Select(d => $"{d.Status}: {Truncate(d.Message, 80)}"))
                : "Khong co thong tin thiet bi",
            ["visitor_vehicle_info"] = "Khong co thong tin khach/phuong tien trong pham vi",
            ["incident_title"] = incident.Title ?? "N/A",
            ["incident_outcome"] = incident.Outcome ?? "Dang xu ly",
            ["sop_status"] = sopSummary,
            ["timeline"] = timelineSummary,
            ["dispatch"] = dispatchSummary
        };

        return await _aiRec.AnalyzeAsync(
            "soc", "incident", incidentId.ToString(),
            "soc-incident-analysis",
            inputData,
            requestedByUserId);
    }
}
