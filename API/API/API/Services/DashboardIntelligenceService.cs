using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IDashboardIntelligenceService
{
    Task<object> GetIntelligenceAsync();
}

public class DashboardIntelligenceService : IDashboardIntelligenceService
{
    private readonly ApplicationDbContext _db;

    public DashboardIntelligenceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetIntelligenceAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var weekStart = GetStartOfWeek(today, DayOfWeek.Monday);
        var weekEnd = weekStart.AddDays(7);
        var lastWeekStart = weekStart.AddDays(-7);
        var lastWeekEnd = weekStart;

        var attendancesToday = await _db.Attendances.AsNoTracking()
            .Where(a => a.WorkDate == today).ToListAsync();
        var attendancesYesterday = await _db.Attendances.AsNoTracking()
            .Where(a => a.WorkDate == yesterday).ToListAsync();

        var schedulesToday = await _db.WorkSchedules.AsNoTracking()
            .Where(s => s.WorkDate == today && s.Status != WorkScheduleStatuses.Cancelled).ToListAsync();

        var todaysLogs = await _db.AccessLogs.AsNoTracking()
            .Where(log => log.Timestamp >= today && log.Timestamp < tomorrow)
            .Select(log => new { log.Timestamp, log.Direction, log.IsBypass, log.ExceptionReasonId, log.ResultStatus })
            .ToListAsync();
        var yesterdaysLogs = await _db.AccessLogs.AsNoTracking()
            .Where(log => log.Timestamp >= yesterday && log.Timestamp < today)
            .Select(log => new { log.Timestamp, log.Direction, log.IsBypass, log.ExceptionReasonId, log.ResultStatus })
            .ToListAsync();

        var weeklyLogs = await _db.AccessLogs.AsNoTracking()
            .Where(log => log.Timestamp >= weekStart && log.Timestamp < weekEnd)
            .Select(log => new { log.Timestamp, log.Direction })
            .ToListAsync();
        var lastWeekLogs = await _db.AccessLogs.AsNoTracking()
            .Where(log => log.Timestamp >= lastWeekStart && log.Timestamp < lastWeekEnd)
            .Select(log => new { log.Timestamp, log.Direction })
            .ToListAsync();

        var lastWeekAttendances = await _db.Attendances.AsNoTracking()
            .Where(a => a.WorkDate >= lastWeekStart && a.WorkDate < lastWeekEnd).ToListAsync();

        var schedulesTodayCount = schedulesToday.Count;
        var checkedInCount = attendancesToday.Count(a => a.CheckIn.HasValue && a.Status != AttendanceStatuses.Leave);
        var lateCount = attendancesToday.Count(a => a.LateMinutes > 0);
        var notCheckedInCount = Math.Max(0, schedulesTodayCount - checkedInCount);
        var totalOvertime = attendancesToday.Sum(a => a.OvertimeHours);
        var scheduleLeaveCount = schedulesToday.Count(s => s.Status == WorkScheduleStatuses.Leave);

        var dailyInCount = todaysLogs.Count(l => string.Equals(l.Direction, "IN", StringComparison.OrdinalIgnoreCase));
        var dailyOutCount = todaysLogs.Count(l => string.Equals(l.Direction, "OUT", StringComparison.OrdinalIgnoreCase));
        var dailyExceptions = todaysLogs.Count(l => l.IsBypass == true || l.ExceptionReasonId != null);

        var yesterdayInCount = yesterdaysLogs.Count(l => string.Equals(l.Direction, "IN", StringComparison.OrdinalIgnoreCase));
        var yesterdayOutCount = yesterdaysLogs.Count(l => string.Equals(l.Direction, "OUT", StringComparison.OrdinalIgnoreCase));

        var vehiclesInside = await _db.Vehicles.AsNoTracking()
            .CountAsync(v => v.ParkingStatus != null && v.ParkingStatus.ToUpper() == "IN");

        var employeeCount = await _db.Employees.AsNoTracking().CountAsync();
        var trainedCount = await _db.EmployeeFaceModels.AsNoTracking().Select(m => m.EmployeeId).Distinct().CountAsync();

        // --- Summary ---
        var trafficChangeIn = CalcPercentChange(yesterdayInCount, dailyInCount);
        var trafficChangeOut = CalcPercentChange(yesterdayOutCount, dailyOutCount);

        var inTrafficWord = trafficChangeIn switch { > 10 => "tang manh", > 0 => "tang nhe", < -10 => "giam manh", < 0 => "giam nhe", _ => "on dinh" };
        var outTrafficWord = trafficChangeOut switch { > 10 => "tang manh", > 0 => "tang nhe", < -10 => "giam manh", < 0 => "giam nhe", _ => "on dinh" };

        var summaryLines = new List<string>();

        summaryLines.Add($"Hom nay co {schedulesTodayCount} nhan su duoc phan cong lam viec, " +
            $"trong do {checkedInCount} nguoi da check-in" +
            (lateCount > 0 ? $", {lateCount} nguoi di tre ({Math.Round(lateCount * 100.0 / Math.Max(1, checkedInCount))}%)." : "."));

        if (notCheckedInCount > 0)
            summaryLines.Add($"{notCheckedInCount} nhan su chua check-in, can nhac nho.");

        if (totalOvertime > 0)
            summaryLines.Add($"Tong thoi gian tang ca hom nay: {Math.Round(totalOvertime, 1)}h.");

        if (dailyExceptions > 0)
            summaryLines.Add($"Co {dailyExceptions} bat thuong trong qua trinh ra vao, nen kiem tra chi tiet.");
        else
            summaryLines.Add("Khong phat hien bat thuong nao trong ra vao hom nay.");

        summaryLines.Add($"Luong vao hom nay {inTrafficWord} ({dailyInCount} luot, " +
            $"hom qua: {yesterdayInCount}); luong ra {outTrafficWord} ({dailyOutCount} luot, hom qua: {yesterdayOutCount}).");

        if (scheduleLeaveCount > 0)
            summaryLines.Add($"{scheduleLeaveCount} nhan su nghi phep theo lich.");

        var totalTraffic = dailyInCount + dailyOutCount;
        var recognitionCoverage = employeeCount == 0 ? 0 : Math.Round(trainedCount * 100.0 / employeeCount);
        summaryLines.Add($"Do phu nhan dien AI dat {recognitionCoverage}% ({trainedCount}/{employeeCount} nhan su).");

        var summary = string.Join(" ", summaryLines);

        // --- Trends (predict next week based on this week + last week) ---
        var thisWeekTrafficByDay = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = weekStart.AddDays(offset);
            var nextDay = day.AddDays(1);
            var dayLogs = weeklyLogs.Where(l => l.Timestamp >= day && l.Timestamp < nextDay).ToList();
            return new
            {
                date = day,
                label = GetDayLabel(day.DayOfWeek),
                checkIn = dayLogs.Count(l => string.Equals(l.Direction, "IN", StringComparison.OrdinalIgnoreCase)),
                checkOut = dayLogs.Count(l => string.Equals(l.Direction, "OUT", StringComparison.OrdinalIgnoreCase))
            };
        }).ToList();

        var lastWeekTrafficByDay = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = lastWeekStart.AddDays(offset);
            var nextDay = day.AddDays(1);
            var dayLogs = lastWeekLogs.Where(l => l.Timestamp >= day && l.Timestamp < nextDay).ToList();
            return new
            {
                date = day,
                checkIn = dayLogs.Count(l => string.Equals(l.Direction, "IN", StringComparison.OrdinalIgnoreCase)),
                checkOut = dayLogs.Count(l => string.Equals(l.Direction, "OUT", StringComparison.OrdinalIgnoreCase))
            };
        }).ToList();

        var lastWeekAttendanceByDay = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = lastWeekStart.AddDays(offset);
            var dayAttendances = lastWeekAttendances.Where(a => a.WorkDate == day).ToList();
            return new
            {
                date = day,
                checkedIn = dayAttendances.Count(a => a.CheckIn.HasValue),
                late = dayAttendances.Count(a => a.LateMinutes > 0)
            };
        }).ToList();

        var trends = new List<object>();
        for (int i = 0; i < 7; i++)
        {
            var nextDate = weekEnd.AddDays(i);
            var thisWeek = thisWeekTrafficByDay[i];
            var lastWeek = lastWeekTrafficByDay[i];

            var predictedIn = (int)Math.Round((thisWeek.checkIn + lastWeek.checkIn) * 0.5);
            var predictedOut = (int)Math.Round((thisWeek.checkOut + lastWeek.checkOut) * 0.5);

            var lastWeekAtt = lastWeekAttendanceByDay[i];
            var predictedHeadcount = (int)Math.Round((thisWeekTrafficByDay[i].checkIn * 0.3) + (lastWeekAtt.checkedIn * 0.7 + lastWeekAtt.checkedIn) * 0.5);

            var confidence = (thisWeek.checkIn + thisWeek.checkOut + lastWeek.checkIn + lastWeek.checkOut) > 20 ? "cao" : "trung binh";

            trends.Add(new
            {
                date = nextDate,
                label = GetDayLabel(nextDate.DayOfWeek),
                predictedCheckIn = predictedIn,
                predictedCheckOut = predictedOut,
                predictedHeadcount = Math.Max(0, predictedHeadcount),
                confidence
            });
        }

        var totalPredictedTraffic = trends.Sum(t => (int)t.GetType().GetProperty("predictedCheckIn")!.GetValue(t)! + (int)t.GetType().GetProperty("predictedCheckOut")!.GetValue(t)!);
        var totalThisWeekTraffic = thisWeekTrafficByDay.Sum(d => d.checkIn + d.checkOut);

        // --- Insights ---
        var insights = new List<object>();

        if (notCheckedInCount > 0)
            insights.Add(new
            {
                type = "warning",
                title = "Nhan vien chua check-in",
                detail = $"{notCheckedInCount} nhan vien co lich lam nhung chua check-in. Can xac nhan ly do.",
                severity = notCheckedInCount > 10 ? "cao" : "trung binh"
            });

        if (lateCount > 3)
        {
            var lateRate = checkedInCount > 0 ? Math.Round(lateCount * 100.0 / checkedInCount) : 0;
            insights.Add(new
            {
                type = "warning",
                title = "Ty le di tre cao",
                detail = $"{lateCount} nhan vien di tre hom nay ({lateRate}%). " +
                    (lateRate > 30 ? "Can danh gia lai gio giac lam viec hoac quy trinh cham cong." : "Can nhac nho de cai thien."),
                severity = lateRate > 30 ? "cao" : "trung binh"
            });
        }

        if (dailyExceptions > 3)
            insights.Add(new
            {
                type = "critical",
                title = "Nhieu bat thuong ra vao",
                detail = $"{dailyExceptions} bat thuong trong ngay, kiem tra neu co dau hieu xam nhap hoac loi thiet bi.",
                severity = "cao"
            });

        if (vehiclesInside > 0 && totalTraffic == 0)
            insights.Add(new
            {
                type = "info",
                title = "Xe trong bai nhung khong co hoat dong ra vao",
                detail = $"{vehiclesInside} xe dang trong bai nhung khong ghi nhan luot ra vao nao. Kiem tra neu can.",
                severity = "thap"
            });

        var peakHours = todaysLogs
            .Where(l => l.Timestamp.HasValue)
            .GroupBy(l => l.Timestamp!.Value.Hour)
            .Select(g => new { hour = g.Key, count = g.Count() })
            .OrderByDescending(g => g.count)
            .Take(3)
            .ToList();

        if (peakHours.Any())
            insights.Add(new
            {
                type = "info",
                title = "Gio cao diem trong ngay",
                detail = $"{string.Join(", ", peakHours.Select(p => $"{p.hour}h ({p.count} luot)"))}",
                severity = "thong tin"
            });

        var attendanceComparison = CalcPercentChange(
            checkedInCount,
            attendancesYesterday.Count(a => a.CheckIn.HasValue && a.Status != AttendanceStatuses.Leave));

        var trafficDirection = totalPredictedTraffic > totalThisWeekTraffic ? "tang" : "giam";
        var trafficPercentChange = totalThisWeekTraffic > 0
            ? Math.Round(Math.Abs(totalPredictedTraffic - totalThisWeekTraffic) * 100.0 / totalThisWeekTraffic)
            : 0;

        return new
        {
            summary,
            generatedAt = DateTime.Now,
            trends,
            insights,
            comparison = new
            {
                attendanceVsYesterday = attendanceComparison,
                trafficVsLastWeek = trafficPercentChange,
                trafficDirection
            }
        };
    }

    private static double CalcPercentChange(int oldValue, int newValue)
    {
        if (oldValue == 0) return newValue > 0 ? 100 : 0;
        return Math.Round((newValue - oldValue) * 100.0 / oldValue, 1);
    }

    private static DateTime GetStartOfWeek(DateTime value, DayOfWeek startOfWeek)
    {
        var diff = (7 + (value.DayOfWeek - startOfWeek)) % 7;
        return value.AddDays(-1 * diff).Date;
    }

    private static string GetDayLabel(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "T2",
        DayOfWeek.Tuesday => "T3",
        DayOfWeek.Wednesday => "T4",
        DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6",
        DayOfWeek.Saturday => "T7",
        _ => "CN"
    };
}
