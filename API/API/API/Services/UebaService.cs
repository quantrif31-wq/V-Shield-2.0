using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IUebaService
{
    Task<UEBAProfile?> GetProfileAsync(int employeeId);
    Task<List<UEBAProfile>> GetProfilesAsync(int page = 1, int pageSize = 20);
    Task<UEBAProfile> BuildProfileAsync(int employeeId);
    Task AnalyzeAccessLogAsync(AccessLog log);
    Task<List<UEBAAnomaly>> GetAnomaliesAsync(int? employeeId = null, string? type = null,
        string? severity = null, string? status = null, DateTime? fromDate = null,
        DateTime? toDate = null, int maxResults = 50);
    Task ResolveAnomalyAsync(int anomalyId, string resolution, int resolvedBy);
    Task MarkFalsePositiveAsync(int anomalyId, int resolvedBy);
    Task<object> GetSummaryAsync();
}

public class UebaService : IUebaService
{
    private readonly ApplicationDbContext _db;

    public UebaService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UEBAProfile?> GetProfileAsync(int employeeId)
    {
        return await _db.Set<UEBAProfile>()
            .AsNoTracking()
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId);
    }

    public async Task<List<UEBAProfile>> GetProfilesAsync(int page = 1, int pageSize = 20)
    {
        return await _db.Set<UEBAProfile>()
            .AsNoTracking()
            .Include(p => p.Employee)
            .OrderByDescending(p => p.RiskScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<UEBAProfile> BuildProfileAsync(int employeeId)
    {
        var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

        var logs = await _db.AccessLogs.AsNoTracking()
            .Where(l => l.EmployeeId == employeeId && l.Timestamp >= ninetyDaysAgo)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();

        var totalCount = logs.Count;
        var daysWithAccess = logs.Select(l => l.Timestamp!.Value.Date).Distinct().Count();
        var totalDays = Math.Max(1, (int)(DateTime.UtcNow - ninetyDaysAgo).TotalDays);
        var avgPerDay = Math.Round(totalCount * 1.0 / totalDays, 2);

        var hours = logs.Where(l => l.Timestamp.HasValue)
            .Select(l => l.Timestamp!.Value.Hour)
            .OrderBy(h => h)
            .ToList();

        var typicalStart = hours.Count > 0 ? hours[Math.Max(0, (int)(hours.Count * 0.1))] : 8;
        var typicalEnd = hours.Count > 0 ? hours[Math.Max(0, (int)(hours.Count * 0.9))] : 17;

        var weekendLogs = logs.Count(l => l.Timestamp!.Value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        var weekendRatio = totalCount > 0 ? Math.Round(weekendLogs * 100.0 / totalCount, 1) : 0;

        var inCount = logs.Count(l => string.Equals(l.Direction, "IN", StringComparison.OrdinalIgnoreCase));
        var outCount = logs.Count(l => string.Equals(l.Direction, "OUT", StringComparison.OrdinalIgnoreCase));
        var inOutRatio = outCount > 0 ? Math.Round(inCount * 1.0 / outCount, 2) : inCount;

        var bypassCount = logs.Count(l => l.IsBypass == true);
        var bypassRate = totalCount > 0 ? Math.Round(bypassCount * 100.0 / totalCount, 1) : 0;

        var gateGroups = logs.Where(l => l.GateId.HasValue)
            .GroupBy(l => l.GateId!.Value)
            .Select(g => new { gateId = g.Key, count = g.Count(), percentage = Math.Round(g.Count() * 100.0 / Math.Max(1, totalCount), 1) })
            .OrderByDescending(g => g.count)
            .ToList();

        var unusualHours = hours.GroupBy(h => h)
            .Select(g => new { hour = g.Key, count = g.Count() })
            .OrderByDescending(g => g.count)
            .Select(g => new { g.hour, g.count, isUnusual = g.count < totalCount * 0.02 })
            .ToList();

        var daysSinceLastAccess = logs.Count > 0 && logs.Last().Timestamp.HasValue
            ? (int)(DateTime.UtcNow - logs.Last().Timestamp!.Value).TotalDays
            : 999;

        var riskScore = CalculateRiskScore(bypassRate, daysSinceLastAccess, weekendRatio, avgPerDay, totalCount);

        var profile = await _db.Set<UEBAProfile>()
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId);

        if (profile == null)
        {
            profile = new UEBAProfile { EmployeeId = employeeId };
            _db.Set<UEBAProfile>().Add(profile);
        }

        profile.TotalAccessCount = totalCount;
        profile.DaysSinceLastAccess = daysSinceLastAccess;
        profile.AvgAccessPerDay = avgPerDay;
        profile.TypicalStartHour = typicalStart;
        profile.TypicalEndHour = typicalEnd;
        profile.WeekendAccessRatio = weekendRatio;
        profile.InOutRatio = inOutRatio;
        profile.BypassRate = bypassRate;
        profile.RiskScore = riskScore;
        profile.LastBuiltAt = DateTime.UtcNow;
        profile.CommonGatesJson = JsonSerializer.Serialize(gateGroups);
        profile.UnusualHoursJson = JsonSerializer.Serialize(unusualHours);

        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task AnalyzeAccessLogAsync(AccessLog log)
    {
        var employeeId = log.EmployeeId;
        if (employeeId == null) return;

        var profile = await _db.Set<UEBAProfile>()
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId.Value);

        if (profile == null || profile.LastBuiltAt < DateTime.UtcNow.AddDays(-1))
        {
            profile = await BuildProfileAsync(employeeId.Value);
        }

        var anomalies = new List<UEBAAnomaly>();
        var timestamp = log.Timestamp ?? DateTime.UtcNow;
        var hour = timestamp.Hour;
        var isWeekend = timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        if (profile.TotalAccessCount > 0)
        {
            if (hour < profile.TypicalStartHour - 2 || hour > profile.TypicalEndHour + 2)
            {
                anomalies.Add(new UEBAAnomaly
                {
                    EmployeeId = employeeId.Value,
                    AccessLogId = log.LogId,
                    EventTimestamp = timestamp,
                    AnomalyType = UEBAAnomalyTypes.UnusualTime,
                    Severity = UEBASeverities.Medium,
                    Description = $"NV #{employeeId} access luc {hour}h, ngoai khung gio thong thuong ({profile.TypicalStartHour}h-{profile.TypicalEndHour}h).",
                    SupportingData = $"Hour={hour}, TypicalRange={profile.TypicalStartHour}-{profile.TypicalEndHour}"
                });
            }

            if (log.GateId.HasValue && !string.IsNullOrWhiteSpace(profile.CommonGatesJson))
            {
                var commonGates = JsonSerializer.Deserialize<List<GateStat>>(profile.CommonGatesJson) ?? new();
                if (commonGates.Count > 0 && !commonGates.Any(g => g.gateId == log.GateId.Value))
                {
                    anomalies.Add(new UEBAAnomaly
                    {
                        EmployeeId = employeeId.Value,
                        AccessLogId = log.LogId,
                        EventTimestamp = timestamp,
                        AnomalyType = UEBAAnomalyTypes.UnusualGate,
                        Severity = UEBASeverities.High,
                        Description = $"NV #{employeeId} access tai cong #{log.GateId}, khong nam trong danh sach cong thuong dung.",
                        SupportingData = $"GateId={log.GateId}"
                    });
                }
            }

            if (isWeekend && profile.WeekendAccessRatio < 5)
            {
                anomalies.Add(new UEBAAnomaly
                {
                    EmployeeId = employeeId.Value,
                    AccessLogId = log.LogId,
                    EventTimestamp = timestamp,
                    AnomalyType = UEBAAnomalyTypes.OutOfHours,
                    Severity = UEBASeverities.Medium,
                    Description = $"NV #{employeeId} access cuoi tuan, khong phai thoi quen (chi {profile.WeekendAccessRatio}% cac luot truoc).",
                    SupportingData = $"WeekendRatio={profile.WeekendAccessRatio}%"
                });
            }

            var recentLogs = await _db.AccessLogs.AsNoTracking()
                .Where(l => l.EmployeeId == employeeId && l.LogId != log.LogId && l.Timestamp >= timestamp.AddMinutes(-30))
                .CountAsync();

            if (recentLogs > 5)
            {
                anomalies.Add(new UEBAAnomaly
                {
                    EmployeeId = employeeId.Value,
                    AccessLogId = log.LogId,
                    EventTimestamp = timestamp,
                    AnomalyType = UEBAAnomalyTypes.UnusualFrequency,
                    Severity = UEBASeverities.High,
                    Description = $"NV #{employeeId} co {recentLogs + 1} luot access trong 30 phut. Tan suat bat thuong cao.",
                    SupportingData = $"Recent30m={recentLogs + 1}, AvgPerDay={profile.AvgAccessPerDay}"
                });
            }

            if (log.IsBypass == true && profile.BypassRate < 10)
            {
                anomalies.Add(new UEBAAnomaly
                {
                    EmployeeId = employeeId.Value,
                    AccessLogId = log.LogId,
                    EventTimestamp = timestamp,
                    AnomalyType = UEBAAnomalyTypes.BypassPattern,
                    Severity = UEBASeverities.High,
                    Description = $"NV #{employeeId} su dung bypass. Ty le bypass thap hon binh thuong ({profile.BypassRate}%).",
                    SupportingData = $"BypassRate={profile.BypassRate}%"
                });
            }
        }
        else
        {
            anomalies.Add(new UEBAAnomaly
            {
                EmployeeId = employeeId.Value,
                AccessLogId = log.LogId,
                EventTimestamp = timestamp,
                AnomalyType = UEBAAnomalyTypes.FirstTimeAccess,
                Severity = UEBASeverities.Low,
                Description = $"NV #{employeeId} access lan dau trong 90 ngay qua.",
                SupportingData = $"TotalAccessCount={profile.TotalAccessCount}"
            });
        }

        if (anomalies.Count > 0)
        {
            var existingKeys = await _db.Set<UEBAAnomaly>()
                .Where(a => a.EmployeeId == employeeId.Value && a.Status == UEBAStatuses.Open)
                .Select(a => new { a.AccessLogId, a.AnomalyType })
                .ToListAsync();

            var existingSet = existingKeys
                .Where(k => k.AccessLogId.HasValue)
                .Select(k => (k.AccessLogId!.Value, k.AnomalyType))
                .ToHashSet();

            var newOnes = anomalies
                .Where(a => !a.AccessLogId.HasValue || !existingSet.Contains((a.AccessLogId.Value, a.AnomalyType)))
                .ToList();

            if (newOnes.Count > 0)
            {
                _db.Set<UEBAAnomaly>().AddRange(newOnes);
                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task<List<UEBAAnomaly>> GetAnomaliesAsync(
        int? employeeId = null, string? type = null, string? severity = null,
        string? status = null, DateTime? fromDate = null, DateTime? toDate = null, int maxResults = 50)
    {
        var query = _db.Set<UEBAAnomaly>()
            .AsNoTracking()
            .Include(a => a.Employee)
            .AsQueryable();

        if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(a => a.AnomalyType == type);
        if (!string.IsNullOrWhiteSpace(severity)) query = query.Where(a => a.Severity == severity);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (fromDate.HasValue) query = query.Where(a => a.EventTimestamp >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.EventTimestamp < toDate.Value);

        return await query.OrderByDescending(a => a.DetectedAt).Take(maxResults).ToListAsync();
    }

    public async Task ResolveAnomalyAsync(int anomalyId, string resolution, int resolvedBy)
    {
        var anomaly = await _db.Set<UEBAAnomaly>().FindAsync(anomalyId)
            ?? throw new KeyNotFoundException($"UEBA anomaly #{anomalyId} not found.");
        anomaly.Status = UEBAStatuses.Resolved;
        anomaly.Resolution = resolution;
        anomaly.ResolvedBy = resolvedBy;
        anomaly.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkFalsePositiveAsync(int anomalyId, int resolvedBy)
    {
        var anomaly = await _db.Set<UEBAAnomaly>().FindAsync(anomalyId)
            ?? throw new KeyNotFoundException($"UEBA anomaly #{anomalyId} not found.");
        anomaly.Status = UEBAStatuses.FalsePositive;
        anomaly.ResolvedBy = resolvedBy;
        anomaly.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<object> GetSummaryAsync()
    {
        var openAnomalies = await _db.Set<UEBAAnomaly>().CountAsync(a => a.Status == UEBAStatuses.Open);
        var resolvedToday = await _db.Set<UEBAAnomaly>()
            .CountAsync(a => a.Status == UEBAStatuses.Resolved && a.ResolvedAt >= DateTime.UtcNow.Date);
        var highRiskCount = await _db.Set<UEBAProfile>().CountAsync(p => p.RiskScore > 60);
        var totalProfiles = await _db.Set<UEBAProfile>().CountAsync();
        var typeDistribution = await _db.Set<UEBAAnomaly>()
            .Where(a => a.Status == UEBAStatuses.Open)
            .GroupBy(a => a.AnomalyType)
            .Select(g => new { type = g.Key, count = g.Count() })
            .ToListAsync();

        return new
        {
            openAnomalies,
            resolvedToday,
            highRiskProfiles = highRiskCount,
            totalProfiles,
            typeDistribution
        };
    }

    private static double CalculateRiskScore(double bypassRate, int daysSinceLastAccess,
        double weekendRatio, double avgPerDay, int totalCount)
    {
        var score = 0.0;
        score += Math.Min(30, bypassRate * 3);
        score += Math.Min(20, daysSinceLastAccess > 30 ? 20 : daysSinceLastAccess * 0.7);
        score += Math.Min(20, weekendRatio > 20 ? 20 : weekendRatio * 0.8);
        if (avgPerDay > 20) score += 15;
        if (totalCount < 5) score += 15;
        return Math.Round(Math.Min(100, score), 1);
    }

    private class GateStat
    {
        public int gateId { get; set; }
        public int count { get; set; }
        public double percentage { get; set; }
    }
}
