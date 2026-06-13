using API.Data;
using API.Models;
using API.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IUebaRiskGraphService
{
    Task<AiRecommendationResult> ExplainEmployeeRiskAsync(int employeeId, int? requestedByUserId);
}

public class UebaRiskGraphService : IUebaRiskGraphService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiRecommendationService _aiRec;

    public UebaRiskGraphService(ApplicationDbContext db, IAiRecommendationService aiRec)
    {
        _db = db;
        _aiRec = aiRec;
    }

    public async Task<AiRecommendationResult> ExplainEmployeeRiskAsync(int employeeId, int? requestedByUserId)
    {
        var employee = await _db.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee == null)
            throw new KeyNotFoundException($"Employee {employeeId} not found.");

        var profile = await _db.UEBAProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId);

        var anomalies = await _db.UEBAAnomalies.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Status == UEBAStatuses.Open)
            .OrderByDescending(a => a.DetectedAt)
            .Take(10)
            .ToListAsync();

        var recentAccess = await _db.AccessLogs.AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.Timestamp)
            .Take(20)
            .ToListAsync();

        var workSchedules = await _db.WorkSchedules.AsNoTracking()
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.WorkDate)
            .Take(10)
            .ToListAsync();

        // Peer baseline: average risk of same department
        var peerRiskAvg = 0.0;
        if (employee.DepartmentId.HasValue)
        {
            peerRiskAvg = await _db.UEBAProfiles.AsNoTracking()
                .Where(p => p.EmployeeId != employeeId
                    && _db.Employees.Any(e => e.EmployeeId == p.EmployeeId && e.DepartmentId == employee.DepartmentId))
                .AverageAsync(p => (double?)p.RiskScore) ?? 0;
        }

        string Truncate(string? value, int maxLen) =>
            string.IsNullOrEmpty(value) ? "N/A" : (value.Length > maxLen ? value[..maxLen] : value);

        var inputData = new Dictionary<string, string>
        {
            ["employee_info"] = $"{employee.FullName} (ID:{employeeId}) - {employee.Department?.Name ?? "N/A"} - {employee.Position?.Name ?? "N/A"}",
            ["risk_score"] = profile?.RiskScore.ToString("F1") ?? "Chua co profile",
            ["risk_factors"] = anomalies.Any()
                ? string.Join("; ", anomalies.Select(a => $"{a.AnomalyType}: {Truncate(a.Description, 100)} ({a.Severity})"))
                : "Khong co anomaly open",
            ["peer_baseline"] = $"Diem TB phong ban: {peerRiskAvg:F1}",
            ["access_history"] = recentAccess.Any()
                ? string.Join("; ", recentAccess.Take(10).Select(l =>
                    $"{l.Timestamp:yyyy-MM-dd HH:mm} - {l.Direction} - Gate:{l.GateId}"))
                : "Khong co access log",
            ["device_info"] = "N/A",
            ["profile_summary"] = profile != null
                ? $"Tong truy cap: {profile.TotalAccessCount}, TB/ngay: {profile.AvgAccessPerDay:F1}, " +
                  $"Gio T:{profile.TypicalStartHour}h-{profile.TypicalEndHour}h, " +
                  $"Weekend ratio: {profile.WeekendAccessRatio:F1}%, Ty le bypass: {profile.BypassRate:F1}%"
                : "Chua co profile"
        };

        return await _aiRec.AnalyzeAsync(
            "ueba", "employee", employeeId.ToString(),
            "ueba-risk-analysis", inputData, requestedByUserId);
    }
}
