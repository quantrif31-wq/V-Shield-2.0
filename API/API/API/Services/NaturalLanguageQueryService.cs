using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using API.Data;
using API.Models;
using API.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface INaturalLanguageQueryService
{
    /// <summary>
    /// Xử lý câu hỏi ngôn ngữ tự nhiên và trả về kết quả từ các nguồn dữ liệu được phép.
    /// </summary>
    Task<NaturalLanguageQueryResult> QueryAsync(string query, int? requestedByUserId);
}

public class NaturalLanguageQueryResult
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string NormalizedQuery { get; set; } = string.Empty;
    public string Intent { get; set; } = "unknown";
    public string Summary { get; set; } = string.Empty;
    public List<QueryResultRow> Results { get; set; } = new();
    public List<string> DataSources { get; set; } = new();
    public bool IsActionable { get; set; }
    public string? DraftRecommendation { get; set; }
    public int TotalCount { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class QueryResultRow
{
    public string Source { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public string? Severity { get; set; }
    public string? Link { get; set; }
}

public class NaturalLanguageQueryService : INaturalLanguageQueryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<NaturalLanguageQueryService> _logger;

    // Whitelist data sources - only these can be queried
    private static readonly HashSet<string> AllowedDataSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "access_logs", "security_events", "ai_events", "alarms", "incidents",
        "devices", "visitors", "vehicles", "employees", "evidence",
        "sop_templates", "dispatch_tasks", "shift_handovers"
    };

    // Blocked patterns for prompt injection protection
    private static readonly Regex[] InjectionPatterns =
    {
        new(@"ignore\s+(all\s+)?(previous|above|below|instructions|prompt)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"forget\s+(all\s+)?(previous|above|below)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"you\s+are\s+(now|not\s+an?\s+ai|a\s+(free|different))", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"system\s+(prompt|instruction|message)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(delete|drop|truncate|alter|insert|update|exec|execute)\s", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(--|#|/\*).*(select|from|where)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    // Vietnamese keyword patterns for intent matching
    private static readonly Dictionary<string, (string Intent, string DataSource)> KeywordPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Access logs / truy cap
        ["vao cong"] = ("query_access", "access_logs"),
        ["ra cong"] = ("query_access", "access_logs"),
        ["truy cap"] = ("query_access", "access_logs"),
        ["ai vao"] = ("query_access", "access_logs"),
        ["ai ra"] = ("query_access", "access_logs"),
        ["log truy cap"] = ("query_access", "access_logs"),
        ["qua cong"] = ("query_access", "access_logs"),
        ["di qua"] = ("query_access", "access_logs"),
        ["xuat hien"] = ("query_access", "access_logs"),
        ["gio nao"] = ("query_access", "access_logs"),

        // Devices / thiet bi
        ["camera"] = ("query_device", "devices"),
        ["thiet bi"] = ("query_device", "devices"),
        ["thiet bi stale"] = ("query_device", "devices"),
        ["camera chet"] = ("query_device", "devices"),
        ["camera offline"] = ("query_device", "devices"),
        ["camera hong"] = ("query_device", "devices"),
        ["cong B"] = ("query_device", "devices"),
        ["cong A"] = ("query_device", "devices"),
        ["thiet bi offline"] = ("query_device", "devices"),
        ["thiet bi hong"] = ("query_device", "devices"),
        ["heartbeat"] = ("query_device", "devices"),
        ["device health"] = ("query_device", "devices"),

        // Alarms / canh bao
        ["alarm"] = ("query_alarm", "alarms"),
        ["canh bao"] = ("query_alarm", "alarms"),
        ["bao dong"] = ("query_alarm", "alarms"),
        ["su co"] = ("query_alarm", "alarms"),
        ["cao"] = ("query_alarm", "alarms"),
        ["nghiem trong"] = ("query_alarm", "alarms"),

        // Incidents
        ["incident"] = ("query_incident", "incidents"),
        ["vu vice"] = ("query_incident", "incidents"),
        ["da xu ly"] = ("query_incident", "incidents"),
        ["chua xu ly"] = ("query_incident", "incidents"),

        // Visitors / khach
        ["khach"] = ("query_visitor", "visitors"),
        ["tham"] = ("query_visitor", "visitors"),
        ["nguoi ngoai"] = ("query_visitor", "visitors"),
        ["khach ra vao"] = ("query_visitor", "visitors"),

        // Vehicles / xe
        ["xe"] = ("query_vehicle", "vehicles"),
        ["bien so"] = ("query_vehicle", "vehicles"),
        ["phuong tien"] = ("query_vehicle", "vehicles"),
        ["parking"] = ("query_vehicle", "vehicles"),
        ["do xe"] = ("query_vehicle", "vehicles"),

        // Employees / nhan vien
        ["nhan vien"] = ("query_employee", "employees"),
        ["nhan su"] = ("query_employee", "employees"),
        ["ai di lam"] = ("query_employee", "employees"),

        // Evidence / bang chung
        ["bang chung"] = ("query_evidence", "evidence"),
        ["evidence"] = ("query_evidence", "evidence"),
        ["export"] = ("query_evidence", "evidence"),
        ["xuat bang chung"] = ("query_evidence", "evidence"),

        // Time patterns
        ["hom qua"] = ("time_filter", ""),
        ["hom nay"] = ("time_filter", ""),
        ["7 ngay"] = ("time_filter", ""),
        ["tuan nay"] = ("time_filter", ""),
        ["tuan truoc"] = ("time_filter", ""),
        ["thang nay"] = ("time_filter", ""),
        ["sau 22h"] = ("time_filter", ""),
        ["sau 10h"] = ("time_filter", ""),
        ["truoc 6h"] = ("time_filter", ""),
        ["ngoai gio"] = ("time_filter", ""),
        ["gio hanh chinh"] = ("time_filter", ""),
        ["cuoi tuan"] = ("time_filter", ""),
        ["sang som"] = ("time_filter", ""),
        ["dem khuya"] = ("time_filter", ""),

        // Action / action patterns
        ["khoa"] = ("action_suggestion", ""),
        ["chan"] = ("action_suggestion", ""),
        ["xoa"] = ("action_suggestion", ""),
        ["dua vao danh sach"] = ("action_suggestion", ""),
        ["canh bao"] = ("action_suggestion", ""),
        ["vo hieu hoa"] = ("action_suggestion", ""),
    };

    public NaturalLanguageQueryService(ApplicationDbContext db, ILogger<NaturalLanguageQueryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<NaturalLanguageQueryResult> QueryAsync(string query, int? requestedByUserId)
    {
        var result = new NaturalLanguageQueryResult
        {
            OriginalQuery = query,
            NormalizedQuery = NormalizeQuery(query),
            Warnings = new List<string>()
        };

        // 1. Prompt injection check
        var injectionCheck = DetectInjection(query);
        if (injectionCheck.IsInjection)
        {
            _logger.LogWarning("Prompt injection detected from user {UserId}: {Query}", requestedByUserId, query);
            result.Summary = "Truy van chua noi dung khong hop le. Chi chap nhan cau hoi ve du lieu bao mat.";
            result.Intent = "blocked_injection";
            return result;
        }

        // 2. Detect intent and data sources
        var normalized = result.NormalizedQuery;
        var detectedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var primaryIntent = "query_access";
        var bestMatchLength = 0;

        foreach (var kvp in KeywordPatterns)
        {
            if (normalized.Contains(kvp.Key.ToLowerInvariant()))
            {
                if (kvp.Key.Length > bestMatchLength)
                {
                    primaryIntent = kvp.Value.Intent;
                    bestMatchLength = kvp.Key.Length;
                }
                if (!string.IsNullOrEmpty(kvp.Value.DataSource))
                    detectedSources.Add(kvp.Value.DataSource);
            }
        }

        // 3. Check if actionable
        if (primaryIntent == "action_suggestion")
        {
            result.IsActionable = true;
            result.Intent = "action_suggestion";
            result.DraftRecommendation = GenerateDraftRecommendation(query, detectedSources);
            result.Summary = "Truy van cua ban co ve yeu cau mot hanh dong. Vui long xem ban phep duyet duoi day.";
            return result;
        }

        // 4. Execute query against allowed data sources
        result.Intent = primaryIntent;

        // Default to recent access logs if no specific source detected
        if (detectedSources.Count == 0)
            detectedSources.Add("access_logs");

        // Validate against whitelist
        var invalidSources = detectedSources.Where(s => !AllowedDataSources.Contains(s)).ToList();
        foreach (var invalid in invalidSources)
        {
            detectedSources.Remove(invalid);
            result.Warnings.Add($"Nguon du lieu '{invalid}' khong nam trong whitelist.");
        }

        if (detectedSources.Count == 0)
        {
            result.Summary = "Khong tim thay nguon du lieu phu hop cho cau hoi cua ban. Vui long thu lai voi cau hoi khac.";
            return result;
        }

        result.DataSources = detectedSources.ToList();

        // Parse time range from query
        var (fromDate, toDate) = ParseTimeRange(normalized);

        // Execute queries
        foreach (var source in detectedSources)
        {
            switch (source)
            {
                case "access_logs":
                    await QueryAccessLogsAsync(result, fromDate, toDate, normalized);
                    break;
                case "devices":
                    await QueryDevicesAsync(result, normalized);
                    break;
                case "alarms":
                    await QueryAlarmsAsync(result, fromDate, toDate, normalized);
                    break;
                case "incidents":
                    await QueryIncidentsAsync(result, fromDate, toDate, normalized);
                    break;
                case "visitors":
                    await QueryVisitorsAsync(result, fromDate, toDate, normalized);
                    break;
                case "vehicles":
                    await QueryVehiclesAsync(result, normalized);
                    break;
                case "employees":
                    await QueryEmployeesAsync(result, normalized);
                    break;
            }
        }

        // 5. Apply limit
        result.TotalCount = result.Results.Count;
        if (result.Results.Count > 50)
        {
            result.Results = result.Results.Take(50).ToList();
            result.Warnings.Add($"Hien thi {result.Results.Count}/{result.TotalCount} ket qua.");
        }

        // 6. Generate summary
        result.Summary = GenerateNaturalLanguageSummary(result, normalized);

        // 7. Log audit
        LogQueryAudit(requestedByUserId, query, result.Intent, result.TotalCount);

        return result;
    }

    private async Task QueryAccessLogsAsync(NaturalLanguageQueryResult result, DateTime? from, DateTime? to, string normalized)
    {
        var query = _db.AccessLogs.AsNoTracking()
            .Include(l => l.Employee)
            .Include(l => l.Gate)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        // Apply additional filters from query text
        if (normalized.Contains("tu choi") || normalized.Contains("denied") || normalized.Contains("deny"))
            query = query.Where(l => l.ResultStatus == "Denied");
        if (normalized.Contains("cho phep") || normalized.Contains("allow") || normalized.Contains("granted"))
            query = query.Where(l => l.ResultStatus == "Approved" || l.ResultStatus == "Granted");
        if (normalized.Contains("khong nhan dien") || normalized.Contains("no face") || normalized.Contains("unrecognized"))
            query = query.Where(l => l.ResultStatus == "Unrecognized");

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Take(20)
            .ToListAsync();

        foreach (var log in logs)
        {
            result.Results.Add(new QueryResultRow
            {
                Source = "access_logs",
                Label = $"{(log.Employee?.FullName ?? "Unknown")} - Gate {log.GateId}",
                Detail = $"{log.Direction} - {log.ResultStatus}" +
                    (!string.IsNullOrEmpty(log.CapturedLicensePlate) ? $" - Plate: {log.CapturedLicensePlate}" : ""),
                Timestamp = log.Timestamp,
                Severity = log.ResultStatus == "Denied" ? "High" : "Low",
                Link = $"/access-logs/{log.LogId}"
            });
        }
    }

    private async Task QueryDevicesAsync(NaturalLanguageQueryResult result, string normalized)
    {
        var query = _db.SecurityDevices.AsNoTracking().AsQueryable();

        bool staleOnly = normalized.Contains("stale") || normalized.Contains("chet") || normalized.Contains("hong");
        bool offlineOnly = normalized.Contains("offline") || normalized.Contains("mat ket noi");
        bool specificGate = false;
        int? gateId = null;

        if (normalized.Contains("cong a")) { gateId = 1; specificGate = true; }
        else if (normalized.Contains("cong b")) { gateId = 2; specificGate = true; }

        if (specificGate && gateId.HasValue)
            query = query.Where(d => d.SiteId == gateId || d.Name.Contains($"Gate {gateId}") || d.Name.Contains($"Cong {gateId}"));

        if (staleOnly)
        {
            var staleThreshold = DateTime.UtcNow.AddMinutes(-15);
            query = query.Where(d => d.LastSeenAtUtc == null || d.LastSeenAtUtc < staleThreshold);
        }
        else if (offlineOnly)
        {
            query = query.Where(d => d.Status == "Offline" || d.Status == "Fault" || d.Status == "Tamper");
        }

        var devices = await query
            .OrderByDescending(d => d.LastSeenAtUtc)
            .Take(20)
            .ToListAsync();

        foreach (var device in devices)
        {
            var now = DateTime.UtcNow;
            var minutesSinceLastSeen = device.LastSeenAtUtc.HasValue
                ? (now - device.LastSeenAtUtc.Value).TotalMinutes
                : 0;

            result.Results.Add(new QueryResultRow
            {
                Source = "devices",
                Label = device.Name,
                Detail = $"Status: {device.Status} - Last seen: {minutesSinceLastSeen:F0} phut truoc",
                Timestamp = device.LastSeenAtUtc ?? now,
                Severity = device.Status == "Offline" || device.Status == "Fault" ? "High" : "Medium",
                Link = $"/device-management/{device.SecurityDeviceId}"
            });
        }
    }

    private async Task QueryAlarmsAsync(NaturalLanguageQueryResult result, DateTime? from, DateTime? to, string normalized)
    {
        var query = _db.Alarms.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(a => a.CreatedAtUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAtUtc <= to.Value);

        if (normalized.Contains("chua xu ly") || normalized.Contains("open") || normalized.Contains("mo"))
            query = query.Where(a => a.State == "New" || a.State == "Acknowledged" || a.State == "Assigned");
        if (normalized.Contains("da xu ly") || normalized.Contains("closed") || normalized.Contains("dong"))
            query = query.Where(a => a.State == "Closed" || a.State == "Resolved");

        bool criticalOnly = normalized.Contains("cao") || normalized.Contains("critical") || normalized.Contains("nghiem trong");
        if (criticalOnly)
            query = query.Where(a => a.Severity == "Critical" || a.Severity == "High");

        var alarms = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        foreach (var alarm in alarms)
        {
            result.Results.Add(new QueryResultRow
            {
                Source = "alarms",
                Label = $"[{alarm.Severity}] {alarm.AlarmType}",
                Detail = alarm.Summary ?? "N/A",
                Timestamp = alarm.CreatedAtUtc,
                Severity = alarm.Severity,
                Link = $"/enterprise-security"
            });
        }
    }

    private async Task QueryIncidentsAsync(NaturalLanguageQueryResult result, DateTime? from, DateTime? to, string normalized)
    {
        var query = _db.Incidents.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(i => i.OpenedAtUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(i => i.OpenedAtUtc <= to.Value);

        if (normalized.Contains("chua xu ly") || normalized.Contains("open") || normalized.Contains("mo"))
            query = query.Where(i => i.Status == "Open");
        if (normalized.Contains("da xu ly") || normalized.Contains("closed") || normalized.Contains("dong"))
            query = query.Where(i => i.Status == "Closed");

        var incidents = await query
            .OrderByDescending(i => i.OpenedAtUtc)
            .Take(20)
            .ToListAsync();

        foreach (var incident in incidents)
        {
            result.Results.Add(new QueryResultRow
            {
                Source = "incidents",
                Label = $"[{incident.Severity}] {incident.Title}",
                Detail = $"Status: {incident.Status} - Outcome: {incident.Outcome ?? "Dang xu ly"}",
                Timestamp = incident.OpenedAtUtc,
                Severity = incident.Severity,
                Link = $"/enterprise-security"
            });
        }
    }

    private async Task QueryVisitorsAsync(NaturalLanguageQueryResult result, DateTime? from, DateTime? to, string normalized)
    {
        var query = _db.Visits.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(v => v.ExpectedInUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(v => v.ExpectedInUtc <= to.Value);

        if (normalized.Contains("qua han") || normalized.Contains("overstay"))
            query = query.Where(v => v.Status == VisitStatuses.Overstay);
        else if (normalized.Contains("dang tham") || normalized.Contains("checked in"))
            query = query.Where(v => v.Status == VisitStatuses.CheckedIn);

        var visits = await query
            .OrderByDescending(v => v.ExpectedInUtc)
            .Take(20)
            .ToListAsync();

        foreach (var visit in visits)
        {
            var statusSeverity = visit.Status == VisitStatuses.Overstay ? "High" : "Low";
            result.Results.Add(new QueryResultRow
            {
                Source = "visitors",
                Label = visit.VisitorName,
                Detail = $"Status: {visit.Status} - Host: {visit.HostEmployeeId} - Escort: {visit.EscortRequired}",
                Timestamp = visit.ExpectedInUtc,
                Severity = statusSeverity,
                Link = $"/guest-profiles"
            });
        }
    }

    private async Task QueryVehiclesAsync(NaturalLanguageQueryResult result, string normalized)
    {
        var query = _db.Vehicles.AsNoTracking().AsQueryable();

        if (normalized.Contains("qua han") || normalized.Contains("expired"))
        {
            // Check parking permits for expired
            return; // Allow fallback to basic vehicle list
        }

        var vehicles = await query
            .OrderBy(v => v.LicensePlate)
            .Take(20)
            .ToListAsync();

        foreach (var vehicle in vehicles)
        {
            result.Results.Add(new QueryResultRow
            {
                Source = "vehicles",
                Label = vehicle.LicensePlate,
                Detail = $"Type: {vehicle.VehicleTypeId} - Parking: {vehicle.ParkingStatus}",
                Timestamp = null,
                Link = $"/vehicles/{vehicle.VehicleId}"
            });
        }
    }

    private async Task QueryEmployeesAsync(NaturalLanguageQueryResult result, string normalized)
    {
        var query = _db.Employees.AsNoTracking().AsQueryable();

        if (normalized.Contains("nghi vice") || normalized.Contains("terminated") || normalized.Contains("thoi vice"))
            query = query.Where(e => e.LifecycleStatus == "Terminated");

        var employees = await query
            .OrderBy(e => e.FullName)
            .Take(20)
            .ToListAsync();

        foreach (var employee in employees)
        {
            result.Results.Add(new QueryResultRow
            {
                Source = "employees",
                Label = employee.FullName,
                Detail = $"Department: {employee.DepartmentId} - Status: {(employee.Status == true ? "Active" : "Inactive")} - Lifecycle: {employee.LifecycleStatus}",
                Link = $"/employees/{employee.EmployeeId}"
            });
        }
    }

    private static (DateTime? From, DateTime? To) ParseTimeRange(string normalized)
    {
        var now = DateTime.UtcNow;
        DateTime? from = null;
        DateTime? to = null;

        if (normalized.Contains("7 ngay") || normalized.Contains("tuan nay"))
        {
            from = now.AddDays(-7);
        }
        else if (normalized.Contains("tuan truoc"))
        {
            from = now.AddDays(-14);
            to = now.AddDays(-7);
        }
        else if (normalized.Contains("thang nay"))
        {
            from = now.AddMonths(-1);
        }
        else if (normalized.Contains("hom qua"))
        {
            from = now.Date.AddDays(-1);
            to = now.Date;
        }
        else if (normalized.Contains("hom nay"))
        {
            from = now.Date;
        }

        // Time-of-day filters
        if (normalized.Contains("sau 22h") || normalized.Contains("sau 10h") || normalized.Contains("dem khuya"))
        {
            // Filter to after 22:00 each day
            if (from == null) from = now.AddDays(-7);
        }
        else if (normalized.Contains("sang som") || normalized.Contains("truoc 6h"))
        {
            if (from == null) from = now.AddDays(-7);
        }
        else if (normalized.Contains("cuoi tuan"))
        {
            if (from == null) from = now.AddDays(-14);
        }
        else if (normalized.Contains("ngoai gio") || normalized.Contains("gio hanh chinh"))
        {
            if (from == null) from = now.AddDays(-7);
        }

        // Default: last 24 hours
        if (from == null && to == null)
            from = now.AddHours(-24);

        return (from, to);
    }

    private static string GenerateNaturalLanguageSummary(NaturalLanguageQueryResult result, string normalized)
    {
        if (result.Results.Count == 0)
        {
            return "Khong tim thay du lieu phu hop. Thu mo rong dieu kien hoac thay doi cau hoi.";
        }

        var sourceLabels = string.Join(", ", result.DataSources.Select(s => s switch
        {
            "access_logs" => "log truy cap",
            "devices" => "thiet bi",
            "alarms" => "alarm",
            "incidents" => "incident",
            "visitors" => "khach tham",
            "vehicles" => "phuong tien",
            "employees" => "nhan vien",
            _ => s
        }));

        var severityCounts = result.Results
            .Where(r => !string.IsNullOrEmpty(r.Severity))
            .GroupBy(r => r.Severity!)
            .ToDictionary(g => g.Key, g => g.Count());

        var severityPart = severityCounts.Count > 0
            ? string.Join(", ", severityCounts.Select(kv => $"{kv.Value} {kv.Key.ToLowerInvariant()}"))
            : "";

        return $"Tim thay {result.TotalCount} ket qua tu {sourceLabels}. " +
            (!string.IsNullOrEmpty(severityPart) ? $"Trong do: {severityPart}. " : "") +
            "Hien thi " + result.Results.Count + " ket qua moi nhat.";
    }

    private static string GenerateDraftRecommendation(string query, HashSet<string> detectedSources)
    {
        return $"[De xuat] Truy van '{query}' gợi y hanh dong. Vui long xem xet va phe duyet truoc khi thuc hien. " +
            $"Cac nguon du lieu lien quan: {string.Join(", ", detectedSources.DefaultIfEmpty("khong xac dinh"))}.";
    }

    private static InjectionDetectionResult DetectInjection(string query)
    {
        foreach (var pattern in InjectionPatterns)
        {
            if (pattern.IsMatch(query))
                return new InjectionDetectionResult { IsInjection = true, MatchedPattern = pattern.ToString() };
        }
        return new InjectionDetectionResult { IsInjection = false };
    }

    private static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return string.Empty;
        var normalized = query.ToLowerInvariant().Trim();
        // Remove extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ");
        // Remove common punctuation
        normalized = Regex.Replace(normalized, @"[?,;:!.\""''„“”«»]", "");
        return normalized;
    }

    private void LogQueryAudit(int? userId, string query, string intent, int resultCount)
    {
        _logger.LogInformation(
            "NLQuery - User: {UserId} | Intent: {Intent} | Results: {Count} | Query: {Query}",
            userId, intent, resultCount, ComputeHash(query));
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private class InjectionDetectionResult
    {
        public bool IsInjection { get; set; }
        public string? MatchedPattern { get; set; }
    }
}
