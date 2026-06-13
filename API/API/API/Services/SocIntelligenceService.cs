using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface ISocIntelligenceService
{
    Task<AlarmClassificationResult?> ClassifyAlarmAsync(long alarmId);
    Task<List<SopRecommendation>> RecommendSopAsync(long alarmId);
    Task<EscalationPrediction?> PredictEscalationRiskAsync(long alarmId);
    Task<List<AlarmAnomaly>> DetectAnomaliesAsync();
    Task<object> GetIntelligenceAsync();
}

public class SocIntelligenceService : ISocIntelligenceService
{
    private readonly ApplicationDbContext _db;

    public SocIntelligenceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AlarmClassificationResult?> ClassifyAlarmAsync(long alarmId)
    {
        var alarm = await _db.Alarms.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AlarmId == alarmId);
        if (alarm == null) return null;

        var summary = (alarm.Summary ?? "").ToLowerInvariant();
        var keywords = new List<string>();

        var criticalPatterns = new[] { "breach", "intrusion", "forced", "explosion", "fire", "weapon", "attack", "hostage", "emergency" };
        var highPatterns = new[] { "denied", "unauthorized", "tamper", "vandalism", "theft", "sabotage", "critical", "bypass", "tailgate" };
        var mediumPatterns = new[] { "overstay", "offline", "fault", "anomaly", "suspicious", "unusual", "violation", "mismatch" };
        var lowPatterns = new[] { "drill", "test", "manual", "maintenance", "info", "warning" };

        string predictedSeverity;
        if (criticalPatterns.Any(p => summary.Contains(p)))
        {
            predictedSeverity = "Critical";
            keywords.AddRange(criticalPatterns.Where(p => summary.Contains(p)));
        }
        else if (highPatterns.Any(p => summary.Contains(p)))
        {
            predictedSeverity = "High";
            keywords.AddRange(highPatterns.Where(p => summary.Contains(p)));
        }
        else if (mediumPatterns.Any(p => summary.Contains(p)))
        {
            predictedSeverity = "Medium";
            keywords.AddRange(mediumPatterns.Where(p => summary.Contains(p)));
        }
        else if (lowPatterns.Any(p => summary.Contains(p)))
        {
            predictedSeverity = "Low";
            keywords.AddRange(lowPatterns.Where(p => summary.Contains(p)));
        }
        else
        {
            predictedSeverity = alarm.Severity;
        }

        if (alarm.SecurityEventId.HasValue)
        {
            var secEvent = await _db.SecurityEvents.AsNoTracking()
                .FirstOrDefaultAsync(e => e.SecurityEventId == alarm.SecurityEventId);
            if (secEvent?.Confidence.HasValue == true && secEvent.Confidence < 50)
            {
                predictedSeverity = DemoteSeverity(predictedSeverity);
                keywords.Add("low-confidence-source");
            }
        }

        var predictedType = ClassifyAlarmType(summary, alarm.AlarmType);

        var severityOrder = new[] { "Low", "Medium", "High", "Critical" };
        var currentIdx = Array.IndexOf(severityOrder, alarm.Severity);
        var predictedIdx = Array.IndexOf(severityOrder, predictedSeverity);
        var confidence = currentIdx == predictedIdx
            ? "cao"
            : (Math.Abs(currentIdx - predictedIdx) <= 1 ? "trung binh" : "thap");

        return new AlarmClassificationResult
        {
            AlarmId = alarmId,
            PredictedSeverity = predictedSeverity,
            PredictedAlarmType = predictedType,
            Confidence = confidence,
            MatchedKeywords = keywords.Distinct().ToList(),
            OriginalSeverity = alarm.Severity,
            OriginalAlarmType = alarm.AlarmType
        };
    }

    public async Task<List<SopRecommendation>> RecommendSopAsync(long alarmId)
    {
        var alarm = await _db.Alarms.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AlarmId == alarmId);
        if (alarm == null) return new List<SopRecommendation>();

        var templates = await _db.SopTemplates.AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync();

        var scored = templates.Select(t =>
        {
            var score = 0;

            if (string.Equals(t.AlarmType, alarm.AlarmType, StringComparison.OrdinalIgnoreCase))
                score += 50;
            else if (t.AlarmType == "Generic")
                score += 10;

            if (t.Name.Contains(alarm.Severity, StringComparison.OrdinalIgnoreCase))
                score += 20;

            if (!string.IsNullOrWhiteSpace(alarm.Summary))
            {
                var summaryLower = alarm.Summary.ToLowerInvariant();
                var nameLower = t.Name.ToLowerInvariant();
                var summaryWords = summaryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var nameWords = nameLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var overlap = summaryWords.Intersect(nameWords).Count();
                score += overlap * 5;
            }

            var stepCount = CountChecklistSteps(t.ChecklistJson);
            score -= stepCount > 20 ? 10 : 0;

            return new SopRecommendation
            {
                SopTemplateId = t.SopTemplateId,
                Name = t.Name,
                AlarmType = t.AlarmType,
                StepCount = stepCount,
                RelevanceScore = Math.Min(100, score),
                Reason = BuildReason(score, t, alarm)
            };
        })
        .Where(s => s.RelevanceScore > 0)
        .OrderByDescending(s => s.RelevanceScore)
        .Take(5)
        .ToList();

        return scored;
    }

    public async Task<EscalationPrediction?> PredictEscalationRiskAsync(long alarmId)
    {
        var alarm = await _db.Alarms.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AlarmId == alarmId);
        if (alarm == null) return null;

        if (alarm.State == "Closed" || alarm.State == "Escalated")
        {
            return new EscalationPrediction
            {
                AlarmId = alarmId,
                RiskScore = alarm.State == "Escalated" ? 100 : 0,
                RiskLevel = alarm.State == "Escalated" ? "da_leo_thang" : "da_dong",
                Factors = new List<string> { $"Trang thai hien tai: {alarm.State}" },
                Recommendation = alarm.State == "Escalated"
                    ? "Alarm da duoc leo thang. Can xu ly ngay."
                    : "Alarm da dong. Khong can xu ly."
            };
        }

        var factors = new List<string>();
        var risk = 0;

        switch (alarm.Severity)
        {
            case "Critical": risk += 35; factors.Add("Muc do nghiem trong: Critical (+35)"); break;
            case "High": risk += 25; factors.Add("Muc do nghiem trong: High (+25)"); break;
            case "Medium": risk += 15; factors.Add("Muc do nghiem trong: Medium (+15)"); break;
            default: risk += 5; factors.Add("Muc do nghiem trong: Low (+5)"); break;
        }

        var ageHours = (DateTime.UtcNow - alarm.CreatedAtUtc).TotalHours;
        if (ageHours > 4) { risk += 20; factors.Add($"Da qua {ageHours:F1}h chua xu ly (+20)"); }
        else if (ageHours > 2) { risk += 15; factors.Add($"Da qua {ageHours:F1}h chua xu ly (+15)"); }
        else if (ageHours > 1) { risk += 10; factors.Add($"Da qua {ageHours:F1}h chua xu ly (+10)"); }
        else if (ageHours > 0.5) { risk += 5; factors.Add($"Da qua {ageHours:F1}h chua xu ly (+5)"); }

        if (alarm.AssignedToUserId == null)
        {
            risk += 15;
            factors.Add("Chua duoc phan cong (+15)");
        }

        var hour = alarm.CreatedAtUtc.Hour;
        if (hour < 6 || hour >= 22)
        {
            risk += 10;
            factors.Add($"Tao ngoai gio hanh chinh ({hour}h) (+10)");
        }

        var sameTypeCount = await _db.Alarms.AsNoTracking()
            .CountAsync(a => a.AlarmType == alarm.AlarmType
                && a.State == "Escalated"
                && a.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30));
        var totalSameType = await _db.Alarms.AsNoTracking()
            .CountAsync(a => a.AlarmType == alarm.AlarmType
                && a.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30));
        if (totalSameType > 0)
        {
            var escalationRate = sameTypeCount * 100.0 / totalSameType;
            if (escalationRate > 50)
            {
                risk += 15;
                factors.Add($"Ty le leo thang lich su cho {alarm.AlarmType}: {escalationRate:F0}% (+15)");
            }
            else if (escalationRate > 20)
            {
                risk += 8;
                factors.Add($"Ty le leo thang lich su cho {alarm.AlarmType}: {escalationRate:F0}% (+8)");
            }
        }

        risk = Math.Min(100, risk);

        string riskLevel;
        string recommendation;

        if (risk >= 70)
        {
            riskLevel = "cao";
            recommendation = "Can phan cong nhan su xu ly ngay. Canh bao co nguy co leo thang cao.";
        }
        else if (risk >= 40)
        {
            riskLevel = "trung_binh";
            recommendation = "Nen xu ly som. Theo doi neu chua co phan hoi trong 30 phut.";
        }
        else
        {
            riskLevel = "thap";
            recommendation = "Rui ro leo thang thap. Co the xu ly theo quy trinh chuan.";
        }

        return new EscalationPrediction
        {
            AlarmId = alarmId,
            RiskScore = risk,
            RiskLevel = riskLevel,
            Factors = factors,
            Recommendation = recommendation
        };
    }

    public async Task<List<AlarmAnomaly>> DetectAnomaliesAsync()
    {
        var anomalies = new List<AlarmAnomaly>();
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var sevenDaysAgo = todayStart.AddDays(-7);

        var hourlyAlarms = await _db.Alarms.AsNoTracking()
            .Where(a => a.CreatedAtUtc >= sevenDaysAgo)
            .GroupBy(a => new { a.CreatedAtUtc.Date, a.CreatedAtUtc.Hour })
            .Select(g => new { g.Key.Date, g.Key.Hour, Count = g.Count() })
            .ToListAsync();

        var todayAlarms = hourlyAlarms.Where(h => h.Date == todayStart).ToList();
        var historyAlarms = hourlyAlarms.Where(h => h.Date < todayStart).ToList();

        foreach (var hourGroup in todayAlarms)
        {
            var historyForHour = historyAlarms
                .Where(h => h.Hour == hourGroup.Hour)
                .Select(h => h.Count)
                .ToList();

            if (historyForHour.Count < 2) continue;

            var avg = historyForHour.Average();
            var stdDev = Math.Sqrt(historyForHour.Sum(x => Math.Pow(x - avg, 2)) / historyForHour.Count);
            var threshold = avg + 2 * stdDev;

            if (hourGroup.Count > threshold && threshold > 0)
            {
                anomalies.Add(new AlarmAnomaly
                {
                    Type = "volume_spike",
                    Severity = hourGroup.Count > avg + 3 * stdDev ? "Critical" : "High",
                    Hour = hourGroup.Hour,
                    CurrentCount = hourGroup.Count,
                    ExpectedCount = (int)Math.Round(avg),
                    Threshold = (int)Math.Round(threshold),
                    Deviation = Math.Round((hourGroup.Count - avg) / Math.Max(1, avg) * 100, 0),
                    Detail = $"Luong alarm tai gio {hourGroup.Hour}: {hourGroup.Count} (trung binh {avg:F1}, nguong {threshold:F1})"
                });
            }
        }

        var nowHour = now.Hour;
        var nowHistory = historyAlarms
            .Where(h => h.Hour == nowHour)
            .Select(h => h.Count)
            .ToList();

        if (nowHistory.Count >= 2)
        {
            var nowAvg = nowHistory.Average();
            var nowStdDev = Math.Sqrt(nowHistory.Sum(x => Math.Pow(x - nowAvg, 2)) / nowHistory.Count);
            var nowThreshold = nowAvg + 2 * nowStdDev;
            var currentHourCount = todayAlarms.Where(h => h.Hour == nowHour).Sum(h => h.Count);

            if (currentHourCount > nowThreshold && nowThreshold > 1)
            {
                var existing = anomalies.FirstOrDefault(a => a.Hour == nowHour);
                if (existing == null)
                {
                    anomalies.Add(new AlarmAnomaly
                    {
                        Type = "realtime_surge",
                        Severity = currentHourCount > nowAvg + 3 * nowStdDev ? "Critical" : "High",
                        Hour = nowHour,
                        CurrentCount = currentHourCount,
                        ExpectedCount = (int)Math.Round(nowAvg),
                        Threshold = (int)Math.Round(nowThreshold),
                        Deviation = Math.Round((currentHourCount - nowAvg) / Math.Max(1, nowAvg) * 100, 0),
                        Detail = $"Tang dot bien gio nay: {currentHourCount} alarm (trung binh {nowAvg:F1})"
                    });
                }
            }
        }

        var unassignedCritical = await _db.Alarms.AsNoTracking()
            .CountAsync(a => a.Severity == "Critical" && a.State != "Closed" && a.AssignedToUserId == null);
        if (unassignedCritical > 0)
        {
            anomalies.Add(new AlarmAnomaly
            {
                Type = "unassigned_critical",
                Severity = "Critical",
                CurrentCount = unassignedCritical,
                Detail = $"Co {unassignedCritical} alarm Critical chua duoc phan cong"
            });
        }

        var oldAlarms = await _db.Alarms.AsNoTracking()
            .CountAsync(a => a.State != "Closed" && a.State != "Escalated"
                && a.CreatedAtUtc <= now.AddHours(-4));
        if (oldAlarms > 2)
        {
            anomalies.Add(new AlarmAnomaly
            {
                Type = "aging_alarms",
                Severity = "High",
                CurrentCount = oldAlarms,
                Detail = $"Co {oldAlarms} alarm tren 4h chua xu ly"
            });
        }

        return anomalies;
    }

    public async Task<object> GetIntelligenceAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var yesterdayStart = todayStart.AddDays(-1);
        var sevenDaysAgo = todayStart.AddDays(-7);

        var alarmsToday = await _db.Alarms.AsNoTracking()
            .Where(a => a.CreatedAtUtc >= todayStart).ToListAsync();
        var alarmsYesterday = await _db.Alarms.AsNoTracking()
            .Where(a => a.CreatedAtUtc >= yesterdayStart && a.CreatedAtUtc < todayStart).ToListAsync();
        var alarms7d = await _db.Alarms.AsNoTracking()
            .Where(a => a.CreatedAtUtc >= sevenDaysAgo).ToListAsync();

        var totalToday = alarmsToday.Count;
        var totalYesterday = alarmsYesterday.Count;
        var changePercent = totalYesterday > 0
            ? Math.Round((totalToday - totalYesterday) * 100.0 / totalYesterday, 1)
            : 0;

        var bySeverity = alarmsToday.GroupBy(a => a.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
        var byType = alarmsToday.GroupBy(a => a.AlarmType)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());
        var byHour = alarmsToday.GroupBy(a => a.CreatedAtUtc.Hour)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        var openCount = alarmsToday.Count(a => a.State != "Closed");
        var criticalOpen = alarmsToday.Count(a => a.Severity == "Critical" && a.State != "Closed");
        var avgResolutionHours = alarms7d.Where(a => a.ClosedAtUtc.HasValue)
            .Select(a => (a.ClosedAtUtc!.Value - a.CreatedAtUtc).TotalHours)
            .DefaultIfEmpty(0)
            .Average();

        var overallRisk = criticalOpen > 0 ? "cao"
            : openCount > 20 ? "trung_binh"
            : "thap";

        var anomalies = await DetectAnomaliesAsync();

        var summary = $"Hom nay co {totalToday} alarm ({changePercent:+#;-#;0}% so voi hom qua). "
            + $"{criticalOpen} alarm Critical dang mo. "
            + $"{anomalies.Count} bat thuong duoc phat hien.";

        return new
        {
            summary,
            generatedAt = DateTime.Now,
            overallRisk,
            statistics = new
            {
                totalToday,
                totalYesterday,
                changePercent,
                openAlarms = openCount,
                criticalOpenAlarms = criticalOpen,
                avgResolutionHours = Math.Round(avgResolutionHours, 1),
                bySeverity,
                byType,
                byHour
            },
            anomalies
        };
    }

    private static string ClassifyAlarmType(string summary, string fallback)
    {
        var s = summary.ToLowerInvariant();

        if (s.Contains("overstay") || s.Contains("visitor")) return "VisitorOverstay";
        if (s.Contains("offline") || s.Contains("heartbeat") || s.Contains("stale")) return "DeviceOffline";
        if (s.Contains("denied") || s.Contains("unauthorized")) return "AccessDenied";
        if (s.Contains("tamper") || s.Contains("vandalism")) return "Tamper";
        if (s.Contains("breach") || s.Contains("intrusion") || s.Contains("forced")) return "Breach";
        if (s.Contains("fire") || s.Contains("smoke")) return "FireAlarm";
        if (s.Contains("drill") || s.Contains("test") || s.Contains("manual")) return "ManualDrill";
        if (s.Contains("fault") || s.Contains("failure") || s.Contains("error")) return "SystemFault";
        if (s.Contains("bypass") || s.Contains("tailgate")) return "SecurityBypass";
        if (s.Contains("anomaly") || s.Contains("suspicious")) return "SuspiciousActivity";
        if (s.Contains("emergency")) return "Emergency";

        return fallback;
    }

    private static string DemoteSeverity(string severity) => severity switch
    {
        "Critical" => "High",
        "High" => "Medium",
        "Medium" => "Low",
        _ => severity
    };

    private static int CountChecklistSteps(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
                ? doc.RootElement.GetArrayLength()
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string BuildReason(int score, SopTemplate template, Alarm alarm)
    {
        if (score >= 60) return $"Phu hop cao voi alarm type '{alarm.AlarmType}'";
        if (score >= 30) return $"Phu hop trung binh voi alarm type '{alarm.AlarmType}'";
        return $"Co lien quan toi alarm type '{alarm.AlarmType}'";
    }
}

public class AlarmClassificationResult
{
    public long AlarmId { get; set; }
    public string PredictedSeverity { get; set; } = string.Empty;
    public string PredictedAlarmType { get; set; } = string.Empty;
    public string Confidence { get; set; } = "trung binh";
    public List<string> MatchedKeywords { get; set; } = new();
    public string OriginalSeverity { get; set; } = string.Empty;
    public string OriginalAlarmType { get; set; } = string.Empty;
}

public class SopRecommendation
{
    public int SopTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AlarmType { get; set; } = string.Empty;
    public int StepCount { get; set; }
    public int RelevanceScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class EscalationPrediction
{
    public long AlarmId { get; set; }
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = "thap";
    public List<string> Factors { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}

public class AlarmAnomaly
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public int? Hour { get; set; }
    public int CurrentCount { get; set; }
    public int ExpectedCount { get; set; }
    public int Threshold { get; set; }
    public double Deviation { get; set; }
    public string Detail { get; set; } = string.Empty;
}
