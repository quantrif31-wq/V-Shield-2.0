using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IPlateFuzzyService
{
    Task<List<FuzzyMatchResult>> FindSimilarPlatesAsync(string plate, double minScore = 0.6, int maxResults = 10);
    Task<List<PlateTimelineEntry>> GetPlateTimelineAsync(string plate, int hours = 24);
    Task<List<PlateAnomaly>> CheckAnomaliesAsync(string plate, int hours = 24);
    Task<SuggestCorrectionResult> SuggestCorrectionAsync(string rawOcr);
}

public class FuzzyMatchResult
{
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public double Score { get; set; }
    public bool IsExactMatch { get; set; }
}

public class PlateTimelineEntry
{
    public int LogId { get; set; }
    public DateTime? Timestamp { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? GateName { get; set; }
    public string? CameraName { get; set; }
    public string? SecurityZoneName { get; set; }
    public string? ResultStatus { get; set; }
}

public class PlateAnomaly
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public List<PlateTimelineEntry> RelatedEntries { get; set; } = new();
}

public class SuggestCorrectionResult
{
    public string RawInput { get; set; } = string.Empty;
    public string Normalized { get; set; } = string.Empty;
    public string? SuggestedPlate { get; set; }
    public double Confidence { get; set; }
    public bool IsKnownPlate { get; set; }
    public string? Source { get; set; }
    public string? OwnerName { get; set; }
}

public class PlateFuzzyService : IPlateFuzzyService
{
    private readonly ApplicationDbContext _context;

    public PlateFuzzyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FuzzyMatchResult>> FindSimilarPlatesAsync(string plate, double minScore = 0.6, int maxResults = 10)
    {
        var queryPlate = LicensePlateHelper.NormalizeForMatch(plate);
        if (string.IsNullOrEmpty(queryPlate)) return new();

        var variants = LicensePlateHelper.GetConfusableVariants(queryPlate);

        var vehicles = await _context.Vehicles
            .Include(v => v.Employee)
            .Where(v => v.LicensePlate != null)
            .ToListAsync();

        var results = new List<FuzzyMatchResult>();

        foreach (var vehicle in vehicles)
        {
            var vehicleNorm = LicensePlateHelper.NormalizeForMatch(vehicle.LicensePlate);
            if (string.IsNullOrEmpty(vehicleNorm)) continue;

            var bestScore = 0.0;

            foreach (var variant in variants)
            {
                var score = LicensePlateHelper.FuzzyMatchScore(variant, vehicleNorm);
                if (score > bestScore) bestScore = score;
            }

            if (bestScore >= minScore)
            {
                results.Add(new FuzzyMatchResult
                {
                    VehicleId = vehicle.VehicleId,
                    LicensePlate = vehicle.LicensePlate,
                    OwnerName = vehicle.Employee?.FullName,
                    Score = Math.Round(bestScore, 2),
                    IsExactMatch = vehicleNorm == queryPlate
                });
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.IsExactMatch)
            .Take(maxResults)
            .ToList();
    }

    public async Task<List<PlateTimelineEntry>> GetPlateTimelineAsync(string plate, int hours = 24)
    {
        var queryPlate = LicensePlateHelper.NormalizeForMatch(plate);
        if (string.IsNullOrEmpty(queryPlate)) return new();

        var since = DateTime.Now.AddHours(-hours);

        var logs = await _context.AccessLogs
            .Include(l => l.Gate)
            .Include(l => l.Camera)
            .Where(l => l.CapturedLicensePlate != null
                        && l.Timestamp >= since
                        && l.ResultStatus != null
                        && l.ResultStatus != "FAILED")
            .ToListAsync();

        var matched = logs
            .Where(l => LicensePlateHelper.FuzzyMatchScore(
                LicensePlateHelper.NormalizeForMatch(l.CapturedLicensePlate), queryPlate) >= 0.7)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new PlateTimelineEntry
            {
                LogId = l.LogId,
                Timestamp = l.Timestamp,
                Direction = l.Direction,
                GateName = l.Gate?.GateName ?? l.GateNameSnapshot,
                CameraName = l.Camera?.CameraName ?? l.CameraNameSnapshot,
                SecurityZoneName = l.SecurityZoneNameSnapshot,
                ResultStatus = l.ResultStatus
            })
            .ToList();

        return matched;
    }

    public async Task<List<PlateAnomaly>> CheckAnomaliesAsync(string plate, int hours = 24)
    {
        var anomalies = new List<PlateAnomaly>();
        var timeline = await GetPlateTimelineAsync(plate, hours);
        if (timeline.Count < 2) return anomalies;

        var now = DateTime.Now;

        var rapidEntries = timeline
            .Where(t => t.Direction == "IN")
            .OrderBy(t => t.Timestamp)
            .ToList();

        for (var i = 1; i < rapidEntries.Count; i++)
        {
            var prev = rapidEntries[i - 1];
            var curr = rapidEntries[i];

            if (prev.Timestamp.HasValue && curr.Timestamp.HasValue)
            {
                var gap = (curr.Timestamp.Value - prev.Timestamp.Value).TotalMinutes;

                if (gap <= 5)
                {
                    anomalies.Add(new PlateAnomaly
                    {
                        Type = "RapidReEntry",
                        Severity = "Warning",
                        Description = $"Xe {plate} ra vào {gap:F0} phút ({prev.Timestamp:HH:mm} → {curr.Timestamp:HH:mm})",
                        DetectedAt = curr.Timestamp.Value,
                        RelatedEntries = new List<PlateTimelineEntry> { prev, curr }
                    });
                }
            }
        }

        var todayCount = timeline.Count(t => t.Direction == "IN" && t.Timestamp?.Date == now.Date);
        if (todayCount >= 5)
        {
            anomalies.Add(new PlateAnomaly
            {
                Type = "HighFrequency",
                Severity = "Info",
                Description = $"Xe {plate} ra vào {todayCount} lần hôm nay — bất thường",
                DetectedAt = now,
                RelatedEntries = timeline.TakeLast(5).ToList()
            });
        }

        return anomalies;
    }

    public async Task<SuggestCorrectionResult> SuggestCorrectionAsync(string rawOcr)
    {
        var cleaned = LicensePlateHelper.NormalizeForMatch(rawOcr);
        if (string.IsNullOrEmpty(cleaned))
        {
            return new SuggestCorrectionResult
            {
                RawInput = rawOcr ?? "",
                Normalized = "",
                Source = "Invalid input"
            };
        }

        var vehicles = await _context.Vehicles
            .Include(v => v.Employee)
            .Where(v => v.LicensePlate != null)
            .ToListAsync();

        var matchedVariants = LicensePlateHelper.GetConfusableVariants(cleaned);

        FuzzyMatchResult? best = null;

        foreach (var vehicle in vehicles)
        {
            var vehicleNorm = LicensePlateHelper.NormalizeForMatch(vehicle.LicensePlate);
            if (string.IsNullOrEmpty(vehicleNorm)) continue;

            foreach (var variant in matchedVariants)
            {
                var score = LicensePlateHelper.FuzzyMatchScore(variant, vehicleNorm);
                if (score >= 0.6 && (best == null || score > best.Score))
                {
                    best = new FuzzyMatchResult
                    {
                        VehicleId = vehicle.VehicleId,
                        LicensePlate = vehicle.LicensePlate,
                        OwnerName = vehicle.Employee?.FullName,
                        Score = Math.Round(score, 2),
                        IsExactMatch = vehicleNorm == cleaned
                    };
                }
            }
        }

        if (best != null)
        {
            return new SuggestCorrectionResult
            {
                RawInput = rawOcr ?? "",
                Normalized = cleaned,
                SuggestedPlate = best.LicensePlate,
                Confidence = best.Score,
                IsKnownPlate = true,
                Source = "FuzzyMatch",
                OwnerName = best.OwnerName
            };
        }

        if (LicensePlateHelper.IsVietnamesePlateFormat(cleaned))
        {
            return new SuggestCorrectionResult
            {
                RawInput = rawOcr ?? "",
                Normalized = cleaned,
                SuggestedPlate = cleaned,
                Confidence = 0.5,
                IsKnownPlate = false,
                Source = "FormatValidation"
            };
        }

        return new SuggestCorrectionResult
        {
            RawInput = rawOcr ?? "",
            Normalized = cleaned,
            Source = "Unrecognized"
        };
    }
}
