using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IAttendanceZoneService
{
    Task<DeriveAttendanceResult> DeriveAttendanceAsync(int employeeId, DateTime date);
    Task<DeriveAttendanceBatchResult> DeriveBatchAsync(DateTime fromDate, DateTime toDate, int? employeeId = null);
}

public class DeriveAttendanceResult
{
    public int? AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime? FirstEntry { get; set; }
    public DateTime? LastExit { get; set; }
    public decimal ZoneDwellTime { get; set; }
    public int ZoneTransitCount { get; set; }
    public string Status { get; set; } = AttendanceStatuses.NotCheckedIn;
    public string Message { get; set; } = string.Empty;
}

public class DeriveAttendanceBatchResult
{
    public int Processed { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class AttendanceZoneService : IAttendanceZoneService
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendanceCalculationService _calculationService;

    public AttendanceZoneService(
        ApplicationDbContext context,
        IAttendanceCalculationService calculationService)
    {
        _context = context;
        _calculationService = calculationService;
    }

    public async Task<DeriveAttendanceResult> DeriveAttendanceAsync(int employeeId, DateTime date)
    {
        var transits = await _context.Set<ZoneTransit>()
            .Include(t => t.SecurityZone)
            .Where(t => t.EmployeeId == employeeId
                        && t.Timestamp >= date.Date
                        && t.Timestamp < date.Date.AddDays(1))
            .OrderBy(t => t.Timestamp)
            .ToListAsync();

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee == null)
        {
            return new DeriveAttendanceResult
            {
                EmployeeId = employeeId,
                WorkDate = date.Date,
                Message = "Khong tim thay nhan vien."
            };
        }

        if (transits.Count == 0)
        {
            return new DeriveAttendanceResult
            {
                EmployeeId = employeeId,
                EmployeeName = employee.FullName,
                WorkDate = date.Date,
                Message = "Khong co du lieu di chuyen qua khu vuc trong ngay."
            };
        }

        var schedule = await _context.WorkSchedules
            .Include(s => s.Shift)
            .Where(s => s.EmployeeId == employeeId
                        && s.WorkDate == date.Date
                        && s.Status != WorkScheduleStatuses.Cancelled
                        && s.Status != WorkScheduleStatuses.Leave)
            .OrderBy(s => s.Shift.StartTime)
            .FirstOrDefaultAsync();

        var existingAttendance = await _context.Attendances
            .Include(a => a.ZoneTransits)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                                      && a.WorkDate == date.Date);

        var firstEntry = transits.FirstOrDefault(t => t.Direction == "IN")?.Timestamp
                         ?? existingAttendance?.CheckIn;
        if (!firstEntry.HasValue)
        {
            return new DeriveAttendanceResult
            {
                AttendanceId = existingAttendance?.AttendanceId,
                EmployeeId = employeeId,
                EmployeeName = employee.FullName,
                WorkDate = date.Date,
                Message = "Chưa có lượt vào hợp lệ để ghi nhận chấm công."
            };
        }

        var lastExit = transits
            .Where(t => t.Direction == "OUT" && t.Timestamp >= firstEntry.Value)
            .LastOrDefault()?.Timestamp;
        var zoneDwellTime = ComputeZoneDwellTime(transits);

        if (existingAttendance == null)
        {
            existingAttendance = new Attendance
            {
                EmployeeId = employeeId,
                ScheduleId = schedule?.ScheduleId,
                WorkDate = date.Date,
                CheckIn = firstEntry.Value,
                CheckOut = lastExit,
                ZoneDwellTime = zoneDwellTime,
                ZoneTransitCount = transits.Count,
                IsZoneDerived = true,
                Source = AttendanceSources.ZoneTransit,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            existingAttendance.ZoneTransits = transits;

            _context.Attendances.Add(existingAttendance);
        }
        else
        {
            existingAttendance.CheckIn = firstEntry.Value;
            existingAttendance.CheckOut = lastExit ?? existingAttendance.CheckOut;
            existingAttendance.ZoneDwellTime = zoneDwellTime;
            existingAttendance.ZoneTransitCount = transits.Count;
            existingAttendance.IsZoneDerived = true;
            existingAttendance.Source = AttendanceSources.ZoneTransit;
            existingAttendance.UpdatedAt = DateTime.UtcNow;

            if (!transits.All(t => existingAttendance.ZoneTransits.Any(zt => zt.ZoneTransitId == t.ZoneTransitId)))
            {
                existingAttendance.ZoneTransits = transits;
            }
        }

        var calc = _calculationService.Calculate(
            date.Date,
            firstEntry.Value,
            lastExit,
            schedule?.Shift);

        existingAttendance.LateMinutes = calc.LateMinutes;
        existingAttendance.EarlyLeaveMinutes = calc.EarlyLeaveMinutes;
        existingAttendance.TotalWorkingHours = calc.TotalWorkingHours;
        existingAttendance.OvertimeHours = calc.OvertimeHours;
        existingAttendance.Status = calc.Status;

        await _context.SaveChangesAsync();

        if (schedule != null && lastExit.HasValue)
        {
            schedule.Status = WorkScheduleStatuses.Worked;
            schedule.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return new DeriveAttendanceResult
        {
            AttendanceId = existingAttendance.AttendanceId,
            EmployeeId = employeeId,
            EmployeeName = employee.FullName,
            WorkDate = date.Date,
            FirstEntry = firstEntry.Value,
            LastExit = lastExit,
            ZoneDwellTime = zoneDwellTime,
            ZoneTransitCount = transits.Count,
            Status = calc.Status,
            Message = "Da tong hop cham cong tu du lieu di chuyen khu vuc."
        };
    }

    public async Task<DeriveAttendanceBatchResult> DeriveBatchAsync(DateTime fromDate, DateTime toDate, int? employeeId = null)
    {
        var result = new DeriveAttendanceBatchResult();

        var transitQuery = _context.Set<ZoneTransit>()
            .Where(t => t.Timestamp >= fromDate.Date && t.Timestamp < toDate.Date.AddDays(1));

        if (employeeId.HasValue)
            transitQuery = transitQuery.Where(t => t.EmployeeId == employeeId.Value);

        var employeeIds = await transitQuery
            .Select(t => t.EmployeeId)
            .Distinct()
            .ToListAsync();

        foreach (var eid in employeeIds)
        {
            try
            {
                for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                {
                    var deriveResult = await DeriveAttendanceAsync(eid, date);
                    result.Processed++;

                    if (deriveResult.AttendanceId.HasValue)
                    {
                        result.Updated++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Employee {eid}: {ex.Message}");
            }
        }

        return result;
    }

    private static decimal ComputeZoneDwellTime(List<ZoneTransit> transits)
    {
        if (transits.Count < 2) return 0;

        double totalMinutes = 0;
        DateTime? entryTime = null;

        foreach (var t in transits.OrderBy(t => t.Timestamp))
        {
            if (t.Direction == "IN")
            {
                entryTime = t.Timestamp;
            }
            else if (t.Direction == "OUT" && entryTime.HasValue)
            {
                totalMinutes += (t.Timestamp - entryTime.Value).TotalMinutes;
                entryTime = null;
            }
        }

        if (entryTime.HasValue)
        {
            totalMinutes += (DateTime.Now - entryTime.Value).TotalMinutes;
        }

        return Math.Round((decimal)(totalMinutes / 60.0), 2);
    }
}
