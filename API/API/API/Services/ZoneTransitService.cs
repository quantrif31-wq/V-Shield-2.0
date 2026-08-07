using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IZoneTransitService
{
    Task ProcessAccessLogAsync(int accessLogId);
    Task ProcessTransitAsync(int employeeId, int? gateId, string direction, DateTime timestamp, string source);
    Task<List<ZoneTransit>> GetTransitsAsync(int employeeId, DateTime date);
    Task<ZoneTransit> CreateTransitAsync(int employeeId, int securityZoneId, int? accessPointId, int? accessLogId, string direction, DateTime timestamp, string source, bool isAutoDerived);
    Task<List<ZoneTransit>> QueryTransitsAsync(int? employeeId, int? departmentId, int? securityZoneId, string? direction, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
}

public class ZoneTransitService : IZoneTransitService
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendanceZoneService _attendanceZoneService;

    public ZoneTransitService(
        ApplicationDbContext context,
        IAttendanceZoneService attendanceZoneService)
    {
        _context = context;
        _attendanceZoneService = attendanceZoneService;
    }

    public async Task ProcessAccessLogAsync(int accessLogId)
    {
        var accessLog = await _context.AccessLogs
            .Include(al => al.Gate)
            .FirstOrDefaultAsync(al => al.LogId == accessLogId);

        if (accessLog == null || accessLog.EmployeeId == null || !IsSuccessfulAccess(accessLog.ResultStatus))
            return;

        if (await _context.Set<ZoneTransit>().AnyAsync(t => t.AccessLogId == accessLogId))
        {
            await _attendanceZoneService.DeriveAttendanceAsync(
                accessLog.EmployeeId.Value,
                (accessLog.Timestamp ?? DateTime.Now).Date);
            return;
        }

        await ProcessTransitCoreAsync(
            accessLog.EmployeeId.Value,
            accessLog.GateId,
            accessLog.Direction,
            accessLog.Timestamp ?? DateTime.Now,
            ZoneTransitSources.AccessLog,
            accessLogId);
    }

    private static bool IsSuccessfulAccess(string? resultStatus) =>
        resultStatus?.Trim().ToUpperInvariant() is "SUCCESS" or "GRANTED" or "APPROVED" or "MATCHED" or "OK";

    public async Task ProcessTransitAsync(int employeeId, int? gateId, string direction, DateTime timestamp, string source)
    {
        await ProcessTransitCoreAsync(employeeId, gateId, direction, timestamp, source, null);
    }

    private async Task ProcessTransitCoreAsync(int employeeId, int? gateId, string direction, DateTime timestamp, string source, int? accessLogId)
    {
        if (!gateId.HasValue) return;

        var normalizedDirection = string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase)
            ? "IN"
            : string.Equals(direction, "OUT", StringComparison.OrdinalIgnoreCase)
                ? "OUT"
                : null;
        if (normalizedDirection == null) return;

        var lanes = await _context.Lanes
            .Include(l => l.AccessPoint)
                .ThenInclude(ap => ap!.SecurityZone)
            .Where(l => l.GateId == gateId.Value && l.IsActive)
            .OrderBy(l => l.LaneId)
            .ToListAsync();

        var lane = lanes.FirstOrDefault(item =>
        {
            var isBidirectional = string.Equals(item.Direction, "Bidirectional", StringComparison.OrdinalIgnoreCase);
            var isEntry = string.Equals(item.Direction, "Entry", StringComparison.OrdinalIgnoreCase);
            return isBidirectional || (isEntry && normalizedDirection == "IN") || (!isEntry && normalizedDirection == "OUT");
        });
        if (lane == null) return;

        var accessPoint = lane.AccessPoint?.SecurityZone != null
            ? lane.AccessPoint
            : await _context.AccessPoints
                .Include(ap => ap.SecurityZone)
                .Where(ap => ap.SiteId == lane.SiteId && ap.IsActive && ap.SecurityZone != null)
                .OrderBy(ap => ap.AccessPointId)
                .FirstOrDefaultAsync();
        if (accessPoint?.SecurityZone == null) return;

        var transit = new ZoneTransit
        {
            EmployeeId = employeeId,
            SecurityZoneId = accessPoint.SecurityZone.SecurityZoneId,
            AccessPointId = accessPoint.AccessPointId,
            AccessLogId = accessLogId,
            Direction = normalizedDirection,
            Timestamp = timestamp,
            Source = source,
            IsAutoDerived = true
        };

        _context.Set<ZoneTransit>().Add(transit);
        await _context.SaveChangesAsync();
        await _attendanceZoneService.DeriveAttendanceAsync(employeeId, timestamp.Date);
    }

    public async Task<List<ZoneTransit>> GetTransitsAsync(int employeeId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return await _context.Set<ZoneTransit>()
            .Include(t => t.SecurityZone)
            .Include(t => t.AccessPoint)
            .Include(t => t.AccessLog).ThenInclude(al => al!.Gate)
            .Where(t => t.EmployeeId == employeeId
                        && t.Timestamp >= dayStart
                        && t.Timestamp < dayEnd)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<ZoneTransit> CreateTransitAsync(int employeeId, int securityZoneId, int? accessPointId, int? accessLogId, string direction, DateTime timestamp, string source, bool isAutoDerived)
    {
        var transit = new ZoneTransit
        {
            EmployeeId = employeeId,
            SecurityZoneId = securityZoneId,
            AccessPointId = accessPointId,
            AccessLogId = accessLogId,
            Direction = direction,
            Timestamp = timestamp,
            Source = source,
            IsAutoDerived = isAutoDerived
        };

        _context.Set<ZoneTransit>().Add(transit);
        await _context.SaveChangesAsync();
        return transit;
    }

    public async Task<List<ZoneTransit>> QueryTransitsAsync(int? employeeId, int? departmentId, int? securityZoneId, string? direction, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _context.Set<ZoneTransit>()
            .Include(t => t.Employee)
            .Include(t => t.SecurityZone)
            .Include(t => t.AccessPoint)
            .Include(t => t.AccessLog).ThenInclude(al => al!.Gate)
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(t => t.EmployeeId == employeeId.Value);

        if (departmentId.HasValue)
            query = query.Where(t => t.Employee.DepartmentId == departmentId.Value);

        if (securityZoneId.HasValue)
            query = query.Where(t => t.SecurityZoneId == securityZoneId.Value);

        if (!string.IsNullOrWhiteSpace(direction))
            query = query.Where(t => t.Direction == direction.Trim());

        if (fromDate.HasValue)
            query = query.Where(t => t.Timestamp >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(t => t.Timestamp <= toDate.Value.Date.AddDays(1));

        return await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
