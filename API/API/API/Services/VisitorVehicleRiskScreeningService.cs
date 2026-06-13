using API.Data;
using API.Models;
using API.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IVisitorVehicleRiskScreeningService
{
    Task<AiRecommendationResult> ScreenVisitorAsync(int visitId, int? requestedByUserId);
    Task<AiRecommendationResult> ScreenVehicleAsync(int vehicleId, int? requestedByUserId);
}

public class VisitorVehicleRiskScreeningService : IVisitorVehicleRiskScreeningService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiRecommendationService _aiRec;

    public VisitorVehicleRiskScreeningService(ApplicationDbContext db, IAiRecommendationService aiRec)
    {
        _db = db;
        _aiRec = aiRec;
    }

    public async Task<AiRecommendationResult> ScreenVisitorAsync(int visitId, int? requestedByUserId)
    {
        var visit = await _db.Visits.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VisitId == visitId);
        if (visit == null)
            throw new KeyNotFoundException($"Visit {visitId} not found.");

        // Load host employee separately (avoid FK issues in tests)
        Employee? hostEmployee = null;
        if (visit.HostEmployeeId.HasValue)
        {
            hostEmployee = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == visit.HostEmployeeId.Value);
        }

        // 1. Watchlist matches
        var watchlistMatches = await _db.WatchlistMatches.AsNoTracking()
            .Include(m => m.WatchlistEntry)
            .Where(m => m.VisitId == visitId)
            .ToListAsync();

        // 2. Past overstays/no-shows by same visitor name/phone/email
        var pastVisits = await _db.Visits.AsNoTracking()
            .Where(v => v.VisitorName == visit.VisitorName
                || (!string.IsNullOrWhiteSpace(visit.VisitorPhone) && v.VisitorPhone == visit.VisitorPhone)
                || (!string.IsNullOrWhiteSpace(visit.VisitorEmail) && v.VisitorEmail == visit.VisitorEmail))
            .Where(v => v.VisitId != visitId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        var pastOverstays = pastVisits.Count(v => v.Status == VisitStatuses.Overstay);
        var pastNoShows = pastVisits.Count(v => v.Status == VisitStatuses.Invited || v.Status == VisitStatuses.Approved);
        var pastDenied = pastVisits.Count(v => v.Status == VisitStatuses.Denied);

        // 3. Host approval status
        var hostApproved = visit.Status != VisitStatuses.Invited;

        // 4. Parking permits for this visit
        var parkingPermits = await _db.ParkingPermits.AsNoTracking()
            .Where(p => p.VisitId == visitId)
            .ToListAsync();

        // 5. Lane events for visitor vehicles
        var laneEvents = await _db.LaneEvents.AsNoTracking()
            .Where(l => l.Note != null && l.Note.Contains(visit.VisitorName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.OccurredAtUtc)
            .Take(10)
            .ToListAsync();

        // Build signals
        var watchlistSummary = watchlistMatches.Any()
            ? string.Join("; ", watchlistMatches.Select(m =>
                $"Watchlist: {m.WatchlistEntry?.DisplayName ?? "N/A"} ({m.WatchlistEntry?.Severity ?? "Unknown"}) - {m.Status}"))
            : "Khong co match watchlist";

        var pastVisitSummary = pastVisits.Any()
            ? $"{pastVisits.Count} lan truoc: {pastOverstays} overstay, {pastNoShows} no-show, {pastDenied} denied"
            : "Khong co lich su tham truoc";

        var hostInfo = hostEmployee != null
            ? $"{hostEmployee.FullName} (ID:{visit.HostEmployeeId})"
            : "Khong co host";

        var inputData = new Dictionary<string, string>
        {
            ["visitor_name"] = visit.VisitorName,
            ["visitor_type"] = visit.VisitorType,
            ["visitor_phone"] = visit.VisitorPhone ?? "N/A",
            ["visitor_email"] = visit.VisitorEmail ?? "N/A",
            ["visit_purpose"] = $"EscortRequired:{visit.EscortRequired}, NDA:{visit.NdaRequired}, Briefing:{visit.SafetyBriefingRequired}",
            ["visit_window"] = $"{visit.ExpectedInUtc:yyyy-MM-dd HH:mm} -> {visit.ExpectedOutUtc:yyyy-MM-dd HH:mm}",
            ["host_info"] = hostInfo,
            ["host_approved"] = hostApproved ? "Yes" : "Pending",
            ["watchlist_matches"] = watchlistSummary,
            ["past_visits"] = pastVisitSummary,
            ["parking_permits"] = parkingPermits.Any()
                ? string.Join("; ", parkingPermits.Select(p => $"{p.PermitType} (Area:{p.ParkingAreaId})"))
                : "Khong co permit",
            ["lane_events"] = laneEvents.Any()
                ? string.Join("; ", laneEvents.Take(5).Select(l => $"{l.EventType} - {l.Direction} ({l.OccurredAtUtc:HH:mm})"))
                : "Khong co lane event",
            ["requirements"] = visit.NdaRequired ? "NDA required" : "No NDA"
        };

        return await _aiRec.AnalyzeAsync(
            "visitor", "visitor", visitId.ToString(),
            "visitor-screening",
            inputData,
            requestedByUserId);
    }

    public async Task<AiRecommendationResult> ScreenVehicleAsync(int vehicleId, int? requestedByUserId)
    {
        var vehicle = await _db.Vehicles.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);
        if (vehicle == null)
            throw new KeyNotFoundException($"Vehicle {vehicleId} not found.");

        // Load navigation properties separately
        Employee? owner = null;
        if (vehicle.EmployeeId.HasValue)
        {
            owner = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == vehicle.EmployeeId.Value);
        }
        var vehicleType = vehicle.VehicleTypeId.HasValue
            ? await _db.VehicleTypes.AsNoTracking()
                .FirstOrDefaultAsync(vt => vt.VehicleTypeId == vehicle.VehicleTypeId.Value)
            : null;

        // 1. Watchlist matches (via WatchlistMatch join table)
        var watchlistMatches = await _db.WatchlistMatches.AsNoTracking()
            .Include(m => m.WatchlistEntry)
            .Where(m => m.VehicleId == vehicleId)
            .ToListAsync();

        // 2. Duplicate plate check
        var duplicatePlates = await _db.Vehicles.AsNoTracking()
            .Where(v => v.LicensePlate == vehicle.LicensePlate && v.VehicleId != vehicleId)
            .ToListAsync();

        // 3. Lane events
        var laneEvents = await _db.LaneEvents.AsNoTracking()
            .Where(l => l.PlateText == vehicle.LicensePlate
                || l.VehicleId == vehicleId)
            .OrderByDescending(l => l.OccurredAtUtc)
            .Take(20)
            .ToListAsync();

        // 4. Parking permits
        var permits = await _db.ParkingPermits.AsNoTracking()
            .Where(p => p.VehicleId == vehicleId)
            .ToListAsync();

        // 5. Access logs for this plate
        var accessLogs = await _db.AccessLogs.AsNoTracking()
            .Where(l => l.CapturedLicensePlate == vehicle.LicensePlate)
            .OrderByDescending(l => l.Timestamp)
            .Take(20)
            .ToListAsync();

        // 6. Past overstays/no-shows from linked visits
        var linkedVisitIds = permits
            .Where(p => p.VisitId.HasValue)
            .Select(p => p.VisitId!.Value)
            .Distinct()
            .ToList();
        var linkedVisits = linkedVisitIds.Count > 0
            ? await _db.Visits.AsNoTracking()
                .Where(v => linkedVisitIds.Contains(v.VisitId))
                .ToListAsync()
            : new List<Visit>();
        var pastOverstays = linkedVisits.Count(v => v.Status == VisitStatuses.Overstay);

        // 7. Visit links with watchlist context
        var visitWatchlistMatches = linkedVisitIds.Count > 0
            ? await _db.WatchlistMatches.AsNoTracking()
                .Include(m => m.WatchlistEntry)
                .Where(m => m.VisitId.HasValue && linkedVisitIds.Contains(m.VisitId.Value))
                .ToListAsync()
            : new List<WatchlistMatch>();

        var inputData = new Dictionary<string, string>
        {
            ["license_plate"] = vehicle.LicensePlate,
            ["vehicle_type"] = vehicleType?.TypeName ?? "N/A",
            ["owner"] = owner?.FullName ?? "N/A",
            ["parking_status"] = vehicle.ParkingStatus,
            ["watchlist_matches"] = watchlistMatches.Any()
                ? string.Join("; ", watchlistMatches.Select(m => $"Plate: {m.WatchlistEntry?.DisplayName ?? "N/A"} ({m.WatchlistEntry?.Severity ?? "Unknown"}) - {m.Status}"))
                : "Khong co match watchlist",
            ["visit_watchlist_matches"] = visitWatchlistMatches.Any()
                ? $"{visitWatchlistMatches.Count} match tu visit lien quan"
                : "Khong co match tu visit",
            ["past_overstays"] = $"{pastOverstays} overstay tu visit lien quan",
            ["duplicate_plates"] = duplicatePlates.Any()
                ? $"{duplicatePlates.Count} xe khac cung bien so"
                : "Khong co bien so trung",
            ["lane_events"] = laneEvents.Any()
                ? string.Join("; ", laneEvents.Take(10).Select(l => $"{l.EventType} {l.Direction} ({l.OccurredAtUtc:yyyy-MM-dd HH:mm})"))
                : "Khong co lane event",
            ["parking_permits"] = permits.Any()
                ? string.Join("; ", permits.Select(p => $"{p.PermitType} ({p.ValidFromUtc:MM-dd} -> {p.ValidToUtc:MM-dd})"))
                : "Khong co parking permit",
            ["access_history"] = accessLogs.Any()
                ? string.Join("; ", accessLogs.Take(10).Select(l => $"{l.Timestamp:yyyy-MM-dd HH:mm} - {l.ResultStatus}"))
                : "Khong co access log",
            ["description"] = vehicle.Description ?? "N/A"
        };

        return await _aiRec.AnalyzeAsync(
            "visitor", "vehicle", vehicleId.ToString(),
            "visitor-screening",
            inputData,
            requestedByUserId);
    }
}
