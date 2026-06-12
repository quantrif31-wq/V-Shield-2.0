using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IAttendanceAnomalyService
{
    Task<List<AttendanceAnomaly>> DetectAnomaliesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<AttendanceAnomaly>> GetAnomaliesAsync(
        int? employeeId = null, string? type = null, string? severity = null,
        string? status = null, DateTime? fromDate = null, DateTime? toDate = null, int maxResults = 50);
    Task ResolveAnomalyAsync(int anomalyId, string resolution, int resolvedBy);
    Task MarkFalsePositiveAsync(int anomalyId, int resolvedBy);
    Task<List<object>> PredictAbsencesAsync(int employeeId, int lookAheadDays = 7);
}

public class AttendanceAnomalyService : IAttendanceAnomalyService
{
    private readonly ApplicationDbContext _db;

    public AttendanceAnomalyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AttendanceAnomaly>> DetectAnomaliesAsync(
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var today = DateTime.Today;
        var from = fromDate ?? today.AddDays(-7);
        var to = toDate ?? today.AddDays(1);

        var anomalies = new List<AttendanceAnomaly>();

        var attendances = await _db.Attendances.AsNoTracking()
            .Where(a => a.WorkDate >= from && a.WorkDate < to)
            .Include(a => a.Employee)
            .ToListAsync();

        var schedules = await _db.WorkSchedules.AsNoTracking()
            .Where(s => s.WorkDate >= from && s.WorkDate < to)
            .ToListAsync();

        var scheduleLookup = schedules.ToLookup(s => (s.EmployeeId, s.WorkDate));

        var recentAttendances = await _db.Attendances.AsNoTracking()
            .Where(a => a.WorkDate >= from.AddDays(-30) && a.WorkDate < to)
            .Include(a => a.Employee)
            .ToListAsync();

        var recentAttendancesByEmployee = recentAttendances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.WorkDate).ToList());

        foreach (var attendance in attendances)
        {
            var employeeAnomalies = recentAttendances
                .Where(a => a.EmployeeId == attendance.EmployeeId && a.AttendanceId != attendance.AttendanceId)
                .OrderByDescending(a => a.WorkDate)
                .Take(10)
                .ToList();

            var empSchedule = scheduleLookup[(attendance.EmployeeId, attendance.WorkDate)].FirstOrDefault();

            if (attendance.CheckIn.HasValue && attendance.CheckOut.HasValue)
            {
                var hoursWorked = (attendance.CheckOut.Value - attendance.CheckIn.Value).TotalHours;
                if (hoursWorked < 1)
                {
                    anomalies.Add(new AttendanceAnomaly
                    {
                        EmployeeId = attendance.EmployeeId,
                        AttendanceId = attendance.AttendanceId,
                        WorkDate = attendance.WorkDate,
                        AnomalyType = AttendanceAnomalyTypes.SuspiciousTime,
                        Severity = AnomalySeverities.High,
                        Description = $"{attendance.Employee?.FullName ?? "NV#" + attendance.EmployeeId} check-in va check-out chi cach nhau {hoursWorked:F1}h. Nghi ngo buddy punching.",
                        SupportingData = $"CheckIn={attendance.CheckIn:HH:mm}, CheckOut={attendance.CheckOut:HH:mm}, Hours={hoursWorked:F1}"
                    });
                }

                if (attendance.CheckOut.Value.Hour >= 0 && attendance.CheckOut.Value.Hour < 5 &&
                    empSchedule?.Shift?.EndTime.Hours < 22)
                {
                    anomalies.Add(new AttendanceAnomaly
                    {
                        EmployeeId = attendance.EmployeeId,
                        AttendanceId = attendance.AttendanceId,
                        WorkDate = attendance.WorkDate,
                        AnomalyType = AttendanceAnomalyTypes.SuspiciousTime,
                        Severity = AnomalySeverities.Medium,
                        Description = $"{attendance.Employee?.FullName ?? "NV#" + attendance.EmployeeId} check-out luc {attendance.CheckOut:HH:mm}, khac thuong so voi ca lam.",
                        SupportingData = $"CheckOut={attendance.CheckOut:HH:mm}"
                    });
                }
            }

            if (attendance.CheckIn.HasValue && !attendance.CheckOut.HasValue &&
                attendance.Status != AttendanceStatuses.NotCheckedIn && attendance.Status != AttendanceStatuses.Leave)
            {
                anomalies.Add(new AttendanceAnomaly
                {
                    EmployeeId = attendance.EmployeeId,
                    AttendanceId = attendance.AttendanceId,
                    WorkDate = attendance.WorkDate,
                    AnomalyType = AttendanceAnomalyTypes.MissingCheckOut,
                    Severity = AnomalySeverities.Medium,
                    Description = $"{attendance.Employee?.FullName ?? "NV#" + attendance.EmployeeId} check-in luc {attendance.CheckIn:HH:mm} nhung chua check-out.",
                    SupportingData = $"CheckIn={attendance.CheckIn:HH:mm}, Status={attendance.Status}"
                });
            }

            if (employeeAnomalies.Count >= 3)
            {
                var lateCount = employeeAnomalies.Count(a => a.LateMinutes > 0);
                var earlyCount = employeeAnomalies.Count(a => a.EarlyLeaveMinutes > 0);
                var absentCount = employeeAnomalies.Count(a => a.Status == AttendanceStatuses.Absent || a.Status == AttendanceStatuses.NotCheckedIn);

                if (absentCount >= 3)
                {
                    anomalies.Add(new AttendanceAnomaly
                    {
                        EmployeeId = attendance.EmployeeId,
                        WorkDate = attendance.WorkDate,
                        AnomalyType = AttendanceAnomalyTypes.AbsencePattern,
                        Severity = AnomalySeverities.High,
                        Description = $"{attendance.Employee?.FullName ?? "NV#" + attendance.EmployeeId} co {absentCount} lan vang/khong check-in trong 10 ngay qua. Can theo doi.",
                        SupportingData = $"AbsentCount={absentCount}, LateCount={lateCount}, EarlyCount={earlyCount}"
                    });
                }
            }

            if (attendance.CheckIn.HasValue && empSchedule != null && attendance.LateMinutes > 60)
            {
                anomalies.Add(new AttendanceAnomaly
                {
                    EmployeeId = attendance.EmployeeId,
                    AttendanceId = attendance.AttendanceId,
                    WorkDate = attendance.WorkDate,
                    AnomalyType = AttendanceAnomalyTypes.SuspiciousTime,
                    Severity = AnomalySeverities.Medium,
                    Description = $"{attendance.Employee?.FullName ?? "NV#" + attendance.EmployeeId} di tre {attendance.LateMinutes} phut, vuot qua muc cho phep.",
                    SupportingData = $"LateMinutes={attendance.LateMinutes}"
                });
            }
        }

        var duplicateCheckIns = attendances
            .Where(a => a.CheckIn.HasValue)
            .GroupBy(a => (a.EmployeeId, a.WorkDate))
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateCheckIns)
        {
            var emp = group.First().Employee;
            anomalies.Add(new AttendanceAnomaly
            {
                EmployeeId = group.Key.EmployeeId,
                WorkDate = group.Key.WorkDate,
                AnomalyType = AttendanceAnomalyTypes.DuplicateCheckIn,
                Severity = AnomalySeverities.Medium,
                Description = $"{emp?.FullName ?? "NV#" + group.Key.EmployeeId} co {group.Count()} ban ghi cham cong trong ngay {group.Key.WorkDate:dd/MM/yyyy}. Kiem tra trung lap.",
                SupportingData = $"Count={group.Count()}"
            });
        }

        var savedAnomalies = new List<AttendanceAnomaly>();
        if (anomalies.Count > 0)
        {
            var existingKeys = await _db.Set<AttendanceAnomaly>()
                .Where(a => a.WorkDate >= from && a.WorkDate < to && a.Status == AnomalyStatuses.Open)
                .Select(a => new { a.EmployeeId, a.WorkDate, a.AnomalyType })
                .ToListAsync();

            var existingSet = existingKeys
                .Select(k => (k.EmployeeId, k.WorkDate, k.AnomalyType))
                .ToHashSet();

            var newAnomalies = anomalies
                .Where(a => !existingSet.Contains((a.EmployeeId, a.WorkDate, a.AnomalyType)))
                .ToList();

            if (newAnomalies.Count > 0)
            {
                _db.Set<AttendanceAnomaly>().AddRange(newAnomalies);
                await _db.SaveChangesAsync();
                savedAnomalies = newAnomalies;
            }
        }

        return savedAnomalies;
    }

    public async Task<List<AttendanceAnomaly>> GetAnomaliesAsync(
        int? employeeId = null, string? type = null, string? severity = null,
        string? status = null, DateTime? fromDate = null, DateTime? toDate = null, int maxResults = 50)
    {
        var query = _db.Set<AttendanceAnomaly>()
            .AsNoTracking()
            .Include(a => a.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(a => a.AnomalyType == type);
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(a => a.Severity == severity);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);
        if (fromDate.HasValue)
            query = query.Where(a => a.WorkDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(a => a.WorkDate < toDate.Value);

        return await query
            .OrderByDescending(a => a.DetectedAt)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task ResolveAnomalyAsync(int anomalyId, string resolution, int resolvedBy)
    {
        var anomaly = await _db.Set<AttendanceAnomaly>().FindAsync(anomalyId)
            ?? throw new KeyNotFoundException($"Anomaly #{anomalyId} not found.");

        anomaly.Status = AnomalyStatuses.Resolved;
        anomaly.Resolution = resolution;
        anomaly.ResolvedBy = resolvedBy;
        anomaly.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkFalsePositiveAsync(int anomalyId, int resolvedBy)
    {
        var anomaly = await _db.Set<AttendanceAnomaly>().FindAsync(anomalyId)
            ?? throw new KeyNotFoundException($"Anomaly #{anomalyId} not found.");

        anomaly.Status = AnomalyStatuses.FalsePositive;
        anomaly.ResolvedBy = resolvedBy;
        anomaly.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<object>> PredictAbsencesAsync(int employeeId, int lookAheadDays = 7)
    {
        var ninetyDaysAgo = DateTime.Today.AddDays(-90);

        var historical = await _db.Attendances.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate >= ninetyDaysAgo && a.WorkDate < DateTime.Today)
            .OrderBy(a => a.WorkDate)
            .ToListAsync();

        var schedules = await _db.WorkSchedules.AsNoTracking()
            .Where(s => s.EmployeeId == employeeId && s.WorkDate >= DateTime.Today && s.WorkDate < DateTime.Today.AddDays(lookAheadDays))
            .OrderBy(s => s.WorkDate)
            .ToListAsync();

        var absentDates = historical
            .Where(a => a.Status == AttendanceStatuses.Absent || a.Status == AttendanceStatuses.NotCheckedIn)
            .Select(a => a.WorkDate)
            .ToHashSet();

        var lateDates = historical
            .Where(a => a.LateMinutes > 0)
            .Select(a => a.WorkDate)
            .ToHashSet();

        var dayOfWeekAbsenceCount = Enumerable.Range(0, 7)
            .Select(dow => absentDates.Count(d => d.DayOfWeek == (DayOfWeek)dow))
            .ToList();

        var dayOfWeekLateCount = Enumerable.Range(0, 7)
            .Select(dow => lateDates.Count(d => d.DayOfWeek == (DayOfWeek)dow))
            .ToList();

        var predictions = new List<object>();
        foreach (var schedule in schedules)
        {
            var dow = (int)schedule.WorkDate.DayOfWeek;
            var absenceProb = Math.Round(Math.Min(1, dayOfWeekAbsenceCount[dow] / 13.0), 2);
            var lateProb = Math.Round(Math.Min(1, dayOfWeekLateCount[dow] / 13.0), 2);

            string risk;
            if (absenceProb > 0.5) risk = "cao";
            else if (absenceProb > 0.2) risk = "trung-binh";
            else risk = "thap";

            predictions.Add(new
            {
                date = schedule.WorkDate,
                dayOfWeek = schedule.WorkDate.ToString("dddd", new System.Globalization.CultureInfo("vi-VN")),
                shiftName = schedule.Shift?.ShiftName ?? "Chua xac dinh",
                absenceProbability = absenceProb,
                lateProbability = lateProb,
                risk
            });
        }

        return predictions;
    }
}
