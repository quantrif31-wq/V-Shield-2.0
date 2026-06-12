using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface ICompanyHierarchyBackfillService
{
    Task<CompanyHierarchyBackfillReport> BackfillDefaultSiteAsync(
        CompanyHierarchyBackfillRequest request,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<CompanyAssetMapReport> GetAssetMapAsync(CancellationToken cancellationToken = default);
}

public sealed class CompanyHierarchyBackfillService : ICompanyHierarchyBackfillService
{
    private readonly ApplicationDbContext _context;

    public CompanyHierarchyBackfillService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyHierarchyBackfillReport> BackfillDefaultSiteAsync(
        CompanyHierarchyBackfillRequest request,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var company = await EnsureCompanyAsync(request, cancellationToken);
        var site = await EnsureSiteAsync(company.CompanyId, request, cancellationToken);
        var zone = await EnsureLegacyZoneAsync(site.SiteId, cancellationToken);
        var fallbackAccessPoint = await EnsureFallbackAccessPointAsync(site.SiteId, zone.SecurityZoneId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var gatesMapped = await BackfillGatesAsync(site.SiteId, zone.SecurityZoneId, fallbackAccessPoint.AccessPointId, cancellationToken);
        var cameraDevicesCreated = await BackfillCameraDevicesAsync(site.SiteId, fallbackAccessPoint.AccessPointId, cancellationToken);
        var employeesMapped = await BackfillEmployeesAsync(site.SiteId, now, cancellationToken);
        var vehiclesMapped = await BackfillVehiclesAsync(site.SiteId, cancellationToken);
        var accessLogSnapshotsUpdated = await BackfillAccessLogSnapshotsAsync(site, zone, cancellationToken);
        var securityEventSnapshotsUpdated = await BackfillSecurityEventSnapshotsAsync(cancellationToken);

        var report = new CompanyHierarchyBackfillReport(
            company.CompanyId,
            site.SiteId,
            zone.SecurityZoneId,
            gatesMapped,
            cameraDevicesCreated,
            employeesMapped,
            vehiclesMapped,
            accessLogSnapshotsUpdated,
            securityEventSnapshotsUpdated);

        _context.OutboxEvents.Add(new OutboxEvent
        {
            EventType = "FoundationBackfillCompleted",
            AggregateType = "CompanySecurityFoundation",
            AggregateId = site.SiteId.ToString(),
            PayloadJson = JsonSerializer.Serialize(report),
            Status = "Pending",
            NextAttemptAtUtc = now,
            CorrelationId = Guid.NewGuid().ToString("N")
        });

        _context.SecurityOperationsChecks.Add(new SecurityOperationsCheck
        {
            CheckType = "FoundationBackfill",
            Name = $"Default site backfill for site {site.Code}",
            Status = "Passed",
            Evidence = $"CompanyId={company.CompanyId}; SiteId={site.SiteId}; RunByUserId={userId?.ToString() ?? "system"}",
            CheckedAtUtc = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<CompanyAssetMapReport> GetAssetMapAsync(CancellationToken cancellationToken = default)
    {
        var lanes = await _context.Lanes
            .AsNoTracking()
            .Include(lane => lane.Site)
            .Include(lane => lane.AccessPoint)
                .ThenInclude(accessPoint => accessPoint!.SecurityZone)
            .ToListAsync(cancellationToken);
        var laneByGateId = lanes
            .Where(lane => lane.GateId.HasValue)
            .GroupBy(lane => lane.GateId!.Value)
            .ToDictionary(group => group.Key, group => group.First());
        var devicesByCameraSerial = await _context.SecurityDevices
            .AsNoTracking()
            .Where(device => device.SerialNumber != null && device.SerialNumber.StartsWith("legacy-camera-"))
            .ToDictionaryAsync(device => device.SerialNumber!, cancellationToken);

        var gates = await _context.Gates
            .AsNoTracking()
            .OrderBy(gate => gate.GateName)
            .Select(gate => new { gate.GateId, gate.GateName, gate.Location })
            .ToListAsync(cancellationToken);
        var gateItems = gates.Select(gate =>
        {
            laneByGateId.TryGetValue(gate.GateId, out var lane);
            return new GateAssetMapItem(
                gate.GateId,
                gate.GateName,
                gate.Location,
                lane?.SiteId,
                lane?.Site?.Name,
                lane?.AccessPointId,
                lane?.AccessPoint?.Name,
                lane?.LaneId,
                lane?.Name);
        }).ToList();

        var cameras = await _context.Cameras
            .AsNoTracking()
            .OrderBy(camera => camera.CameraName)
            .Select(camera => new
            {
                camera.CameraId,
                camera.CameraName,
                camera.CameraType,
                camera.GateId
            })
            .ToListAsync(cancellationToken);
        var cameraItems = cameras.Select(camera =>
        {
            laneByGateId.TryGetValue(camera.GateId ?? 0, out var lane);
            devicesByCameraSerial.TryGetValue($"legacy-camera-{camera.CameraId}", out var device);
            return new CameraAssetMapItem(
                camera.CameraId,
                camera.CameraName,
                camera.CameraType,
                camera.GateId,
                lane?.SiteId ?? device?.SiteId,
                lane?.Site?.Name,
                lane?.AccessPointId ?? device?.AccessPointId,
                device?.SecurityDeviceId,
                device?.Status);
        }).ToList();

        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.Site)
            .Include(vehicle => vehicle.Employee)
            .OrderBy(vehicle => vehicle.LicensePlate)
            .Select(vehicle => new VehicleAssetMapItem(
                vehicle.VehicleId,
                vehicle.LicensePlate,
                vehicle.EmployeeId,
                vehicle.Employee != null ? vehicle.Employee.FullName : null,
                vehicle.SiteId,
                vehicle.Site != null ? vehicle.Site.Name : null,
                vehicle.ParkingStatus))
            .ToListAsync(cancellationToken);

        return new CompanyAssetMapReport(gateItems, cameraItems, vehicles);
    }

    private async Task<Company> EnsureCompanyAsync(CompanyHierarchyBackfillRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.CompanyCode, "VSHIELD");
        var company = await _context.Companies.FirstOrDefaultAsync(item => item.Code == code, cancellationToken);
        if (company != null)
            return company;

        company = new Company
        {
            Name = NormalizeName(request.CompanyName, "V-Shield Company"),
            Code = code,
            IsActive = true
        };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync(cancellationToken);
        return company;
    }

    private async Task<Site> EnsureSiteAsync(int companyId, CompanyHierarchyBackfillRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.SiteCode, "HQ");
        var site = await _context.Sites.FirstOrDefaultAsync(
            item => item.CompanyId == companyId && item.Code == code,
            cancellationToken);
        if (site != null)
            return site;

        site = new Site
        {
            CompanyId = companyId,
            Name = NormalizeName(request.SiteName, "Headquarters"),
            Code = code,
            TimeZoneId = NormalizeName(request.TimeZoneId, "Asia/Ho_Chi_Minh"),
            IsActive = true
        };
        _context.Sites.Add(site);
        await _context.SaveChangesAsync(cancellationToken);
        return site;
    }

    private async Task<SecurityZone> EnsureLegacyZoneAsync(int siteId, CancellationToken cancellationToken)
    {
        var zone = await _context.SecurityZones.FirstOrDefaultAsync(
            item => item.SiteId == siteId && item.Code == "LEGACY",
            cancellationToken);
        if (zone != null)
            return zone;

        zone = new SecurityZone
        {
            SiteId = siteId,
            Name = "Legacy Mapped Operations",
            Code = "LEGACY",
            SecurityLevel = "Normal",
            IsRestricted = false,
            IsActive = true
        };
        _context.SecurityZones.Add(zone);
        await _context.SaveChangesAsync(cancellationToken);
        return zone;
    }

    private async Task<AccessPoint> EnsureFallbackAccessPointAsync(int siteId, int zoneId, CancellationToken cancellationToken)
    {
        var accessPoint = await _context.AccessPoints.FirstOrDefaultAsync(
            item => item.SiteId == siteId && item.Name == "Legacy Unmapped Access Point",
            cancellationToken);
        if (accessPoint != null)
            return accessPoint;

        accessPoint = new AccessPoint
        {
            SiteId = siteId,
            SecurityZoneId = zoneId,
            Name = "Legacy Unmapped Access Point",
            Type = "Gate",
            DirectionMode = "Bidirectional",
            IsActive = true
        };
        _context.AccessPoints.Add(accessPoint);
        await _context.SaveChangesAsync(cancellationToken);
        return accessPoint;
    }

    private async Task<int> BackfillGatesAsync(
        int siteId,
        int zoneId,
        int fallbackAccessPointId,
        CancellationToken cancellationToken)
    {
        var gates = await _context.Gates
            .OrderBy(gate => gate.GateId)
            .ToListAsync(cancellationToken);
        var existingLanes = await _context.Lanes
            .Where(lane => lane.GateId != null)
            .ToListAsync(cancellationToken);
        var laneByGate = existingLanes
            .GroupBy(lane => lane.GateId!.Value)
            .ToDictionary(group => group.Key, group => group.First());
        var mapped = 0;

        foreach (var gate in gates)
        {
            if (laneByGate.TryGetValue(gate.GateId, out var lane))
            {
                var changed = false;
                if (lane.SiteId <= 0)
                {
                    lane.SiteId = siteId;
                    changed = true;
                }
                if (!lane.AccessPointId.HasValue)
                {
                    lane.AccessPointId = fallbackAccessPointId;
                    changed = true;
                }
                if (changed)
                    mapped++;
                continue;
            }

            var accessPoint = new AccessPoint
            {
                SiteId = siteId,
                SecurityZoneId = zoneId,
                Name = $"{TrimForName(gate.GateName, "Gate")} Access Point",
                Type = "Gate",
                DirectionMode = "Bidirectional",
                IsActive = true
            };
            _context.AccessPoints.Add(accessPoint);
            await _context.SaveChangesAsync(cancellationToken);

            _context.Lanes.Add(new Lane
            {
                SiteId = siteId,
                GateId = gate.GateId,
                AccessPointId = accessPoint.AccessPointId,
                Name = $"{TrimForName(gate.GateName, "Gate")} Lane",
                Direction = "Bidirectional",
                IsActive = true
            });
            mapped++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return mapped;
    }

    private async Task<int> BackfillCameraDevicesAsync(
        int siteId,
        int fallbackAccessPointId,
        CancellationToken cancellationToken)
    {
        var cameras = await _context.Cameras
            .OrderBy(camera => camera.CameraId)
            .ToListAsync(cancellationToken);
        var lanes = await _context.Lanes
            .Where(lane => lane.GateId != null)
            .ToListAsync(cancellationToken);
        var laneByGate = lanes
            .GroupBy(lane => lane.GateId!.Value)
            .ToDictionary(group => group.Key, group => group.First());
        var existingCameraDeviceSerials = await _context.SecurityDevices
            .Where(device => device.SerialNumber != null && device.SerialNumber.StartsWith("legacy-camera-"))
            .Select(device => device.SerialNumber!)
            .ToListAsync(cancellationToken);
        var existing = existingCameraDeviceSerials.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var camera in cameras)
        {
            var serial = $"legacy-camera-{camera.CameraId}";
            if (existing.Contains(serial))
                continue;

            var lane = camera.GateId.HasValue && laneByGate.TryGetValue(camera.GateId.Value, out var mappedLane)
                ? mappedLane
                : null;
            _context.SecurityDevices.Add(new SecurityDevice
            {
                SiteId = lane?.SiteId ?? siteId,
                AccessPointId = lane?.AccessPointId ?? fallbackAccessPointId,
                DeviceType = "Camera",
                Name = TrimForName(camera.CameraName, $"Camera {camera.CameraId}"),
                Vendor = "Legacy",
                Model = camera.CameraType,
                SerialNumber = serial,
                Status = "Active",
                LastSeenAtUtc = DateTime.UtcNow,
                IsActive = true
            });
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<int> BackfillEmployeesAsync(int siteId, DateTime now, CancellationToken cancellationToken)
    {
        var employees = await _context.Employees
            .Where(employee => employee.PrimarySiteId == null)
            .ToListAsync(cancellationToken);

        foreach (var employee in employees)
        {
            employee.PrimarySiteId = siteId;
            employee.LifecycleUpdatedAtUtc = employee.LifecycleUpdatedAtUtc ?? now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return employees.Count;
    }

    private async Task<int> BackfillVehiclesAsync(int siteId, CancellationToken cancellationToken)
    {
        var vehicles = await _context.Vehicles
            .Include(vehicle => vehicle.Employee)
            .Where(vehicle => vehicle.SiteId == null)
            .ToListAsync(cancellationToken);

        foreach (var vehicle in vehicles)
        {
            vehicle.SiteId = vehicle.Employee?.PrimarySiteId ?? siteId;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return vehicles.Count;
    }

    private async Task<int> BackfillAccessLogSnapshotsAsync(Site fallbackSite, SecurityZone fallbackZone, CancellationToken cancellationToken)
    {
        var lanes = await _context.Lanes
            .Include(lane => lane.Site)
            .Include(lane => lane.AccessPoint)
                .ThenInclude(accessPoint => accessPoint!.SecurityZone)
            .Where(lane => lane.GateId != null)
            .ToListAsync(cancellationToken);
        var laneByGate = lanes
            .GroupBy(lane => lane.GateId!.Value)
            .ToDictionary(group => group.Key, group => group.First());
        var gateNames = await _context.Gates
            .ToDictionaryAsync(gate => gate.GateId, gate => gate.GateName, cancellationToken);
        var cameras = await _context.Cameras
            .Select(camera => new { camera.CameraId, camera.CameraName, camera.GateId })
            .ToListAsync(cancellationToken);
        var cameraById = cameras.ToDictionary(camera => camera.CameraId);
        var logs = await _context.AccessLogs
            .Where(log => log.SiteNameSnapshot == null ||
                          log.GateNameSnapshot == null ||
                          log.CameraNameSnapshot == null)
            .ToListAsync(cancellationToken);
        var updated = 0;

        foreach (var log in logs)
        {
            cameraById.TryGetValue(log.CameraId ?? 0, out var camera);
            var gateId = log.GateId ?? camera?.GateId;
            var lane = gateId.HasValue && laneByGate.TryGetValue(gateId.Value, out var mappedLane)
                ? mappedLane
                : null;

            log.SiteNameSnapshot ??= lane?.Site?.Name ?? fallbackSite.Name;
            log.SecurityZoneNameSnapshot ??= lane?.AccessPoint?.SecurityZone?.Name ?? fallbackZone.Name;
            log.AccessPointNameSnapshot ??= lane?.AccessPoint?.Name;
            log.LaneNameSnapshot ??= lane?.Name;
            if (gateId.HasValue && gateNames.TryGetValue(gateId.Value, out var gateName))
                log.GateNameSnapshot ??= gateName;
            log.CameraNameSnapshot ??= camera?.CameraName;
            updated++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return updated;
    }

    private async Task<int> BackfillSecurityEventSnapshotsAsync(CancellationToken cancellationToken)
    {
        var sites = await _context.Sites.ToDictionaryAsync(site => site.SiteId, site => site.Name, cancellationToken);
        var zones = await _context.SecurityZones.ToDictionaryAsync(zone => zone.SecurityZoneId, zone => zone.Name, cancellationToken);
        var accessPoints = await _context.AccessPoints.ToDictionaryAsync(accessPoint => accessPoint.AccessPointId, accessPoint => accessPoint.Name, cancellationToken);
        var events = await _context.SecurityEvents
            .Where(item => item.SiteNameSnapshot == null ||
                           item.SecurityZoneNameSnapshot == null ||
                           item.AccessPointNameSnapshot == null)
            .ToListAsync(cancellationToken);
        var updated = 0;

        foreach (var item in events)
        {
            if (item.SiteId.HasValue && sites.TryGetValue(item.SiteId.Value, out var siteName))
                item.SiteNameSnapshot ??= siteName;
            if (item.SecurityZoneId.HasValue && zones.TryGetValue(item.SecurityZoneId.Value, out var zoneName))
                item.SecurityZoneNameSnapshot ??= zoneName;
            if (item.AccessPointId.HasValue && accessPoints.TryGetValue(item.AccessPointId.Value, out var accessPointName))
                item.AccessPointNameSnapshot ??= accessPointName;
            updated++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return updated;
    }

    private static string NormalizeCode(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.ToUpperInvariant();
    }

    private static string NormalizeName(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string TrimForName(string? value, string fallback)
    {
        var name = NormalizeName(value, fallback);
        return name.Length <= 120 ? name : name[..120];
    }
}

public sealed record CompanyHierarchyBackfillRequest(
    string? CompanyName,
    string? CompanyCode,
    string? SiteName,
    string? SiteCode,
    string? TimeZoneId);

public sealed record CompanyHierarchyBackfillReport(
    int CompanyId,
    int SiteId,
    int SecurityZoneId,
    int GatesMapped,
    int CameraDevicesCreated,
    int EmployeesMapped,
    int VehiclesMapped,
    int AccessLogSnapshotsUpdated,
    int SecurityEventSnapshotsUpdated);

public sealed record CompanyAssetMapReport(
    IReadOnlyList<GateAssetMapItem> Gates,
    IReadOnlyList<CameraAssetMapItem> Cameras,
    IReadOnlyList<VehicleAssetMapItem> Vehicles);

public sealed record GateAssetMapItem(
    int GateId,
    string GateName,
    string? Location,
    int? SiteId,
    string? SiteName,
    int? AccessPointId,
    string? AccessPointName,
    int? LaneId,
    string? LaneName);

public sealed record CameraAssetMapItem(
    int CameraId,
    string CameraName,
    string? CameraType,
    int? GateId,
    int? SiteId,
    string? SiteName,
    int? AccessPointId,
    int? SecurityDeviceId,
    string? SecurityDeviceStatus);

public sealed record VehicleAssetMapItem(
    int VehicleId,
    string LicensePlate,
    int? EmployeeId,
    string? EmployeeName,
    int? SiteId,
    string? SiteName,
    string ParkingStatus);
