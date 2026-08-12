using API.Data;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using API.Middleware;

namespace API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[RequireOperationalTask(UserOperationalScopeService.TaskDashboard)]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDashboardIntelligenceService _intelligence;

    public DashboardController(ApplicationDbContext context, IDashboardIntelligenceService intelligence)
    {
        _context = context;
        _intelligence = intelligence;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var weekStart = GetStartOfWeek(today, DayOfWeek.Monday);
        var weekEnd = weekStart.AddDays(7);
        var nowUtc = DateTime.UtcNow;

        // NOTE: All queries must be awaited sequentially because DbContext is NOT thread-safe.
        // Using Task.WhenAll with the same DbContext causes InvalidOperationException.

        var vehiclesInside = await _context.Vehicles.AsNoTracking()
            .CountAsync(v => v.ParkingStatus != null && v.ParkingStatus.ToUpper() == "IN");

        var expectedVisitorsToday = await _context.PreRegistrations.AsNoTracking()
            .CountAsync(r => r.ExpectedTimeIn >= today && r.ExpectedTimeIn < tomorrow);

        var pendingRegistrations = await _context.PreRegistrations.AsNoTracking()
            .CountAsync(r => r.Status != null && r.Status.ToUpper() == "PENDING");

        var camerasConfigured = await _context.Cameras.AsNoTracking().CountAsync();
        var gatesConfigured = await _context.Gates.AsNoTracking().CountAsync();
        var guestProfiles = await _context.GuestProfiles.AsNoTracking().CountAsync();
        var checkedInVisitors = await _context.Visits.AsNoTracking()
            .CountAsync(visit =>
                visit.Status == VisitStatuses.CheckedIn ||
                visit.Status == VisitStatuses.Overstay);

        var employeeCount = await _context.Employees.AsNoTracking().CountAsync();
        var trainedEmployeeCount = await _context.EmployeeFaceModels.AsNoTracking()
            .Select(m => m.EmployeeId)
            .Distinct()
            .CountAsync();
        var openAlarms = await _context.Alarms.AsNoTracking()
            .CountAsync(alarm => alarm.State != "Closed");
        var criticalOpenAlarms = await _context.Alarms.AsNoTracking()
            .CountAsync(alarm => alarm.State != "Closed" && alarm.Severity == "Critical");
        var offlineDevices = await _context.SecurityDevices.AsNoTracking()
            .CountAsync(device => device.Status == "Offline");
        var degradedDevices = await _context.SecurityDevices.AsNoTracking()
            .CountAsync(device => device.Status == "Degraded");
        var activeEmergencyPasses = await _context.EmergencyPasses.AsNoTracking()
            .CountAsync(item => item.Status == "Active" && item.ValidToUtc > nowUtc);
        var pendingInterventions = await _context.OperationalInterventionRequests.AsNoTracking()
            .CountAsync(item => item.Status == "Pending");
        var oldestPendingIntervention = await _context.OperationalInterventionRequests.AsNoTracking()
            .Where(item => item.Status == "Pending")
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => (DateTime?)item.CreatedAtUtc)
            .FirstOrDefaultAsync();

        var todaysLogs = await _context.AccessLogs.AsNoTracking()
            .Where(log => log.Timestamp >= today && log.Timestamp < tomorrow)
            .Select(log => new
            {
                log.Direction,
                log.IsBypass,
                log.ExceptionReasonId,
                log.ResultStatus,
                log.CapturedFaceImageUrl,
                log.CapturedLicensePlate
            })
            .ToListAsync();

        var weeklyLogs = await _context.AccessLogs.AsNoTracking()
            .Where(log => log.Timestamp >= weekStart && log.Timestamp < weekEnd)
            .Select(log => new
            {
                log.Timestamp,
                log.Direction
            })
            .ToListAsync();

        var recentActivitiesRaw = await _context.AccessLogs.AsNoTracking()
            .OrderByDescending(log => log.Timestamp)
            .Take(6)
            .Select(log => new
            {
                log.LogId,
                log.Timestamp,
                log.Direction,
                log.CapturedLicensePlate,
                log.ResultStatus,
                log.IsBypass,
                log.Note,
                GateName = log.Gate != null ? log.Gate.GateName : null,
                CameraName = log.Camera != null ? log.Camera.CameraName : null,
                EmployeeName = log.Employee != null ? log.Employee.FullName : null,
                VisitorName = log.VisitorDetail != null
    ? log.VisitorDetail.FullName
    : null,
                ExceptionReason = log.ExceptionReason != null ? log.ExceptionReason.Description : null
            })
            .ToListAsync();
        var recentLaneEventsRaw = await _context.LaneEvents.AsNoTracking()
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(6)
            .Select(item => new
            {
                item.LaneEventId,
                item.OccurredAtUtc,
                item.EventType,
                item.Direction,
                item.PlateText,
                item.Note,
                LaneName = item.Lane != null ? item.Lane.Name : null
            })
            .ToListAsync();
        var recentAlarmsRaw = await _context.Alarms.AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(6)
            .Select(item => new
            {
                item.AlarmId,
                item.CreatedAtUtc,
                item.AlarmType,
                item.Severity,
                item.State,
                item.Summary
            })
            .ToListAsync();
        var recentInterventionsRaw = await _context.OperationalInterventionRequests.AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(6)
            .Select(item => new
            {
                item.OperationalInterventionRequestId,
                item.CreatedAtUtc,
                item.InterventionType,
                item.SubjectName,
                item.PlateNumber,
                item.Status,
                item.Priority,
                item.Reason,
                item.LaneName
            })
            .ToListAsync();

        var schedulesToday = await _context.WorkSchedules.AsNoTracking()
            .Where(s => s.WorkDate == today &&
                        s.Status != WorkScheduleStatuses.Cancelled)
            .ToListAsync();

        var attendancesToday = await _context.Attendances.AsNoTracking()
            .Where(a => a.WorkDate == today)
            .ToListAsync();

        var pendingLeaveApprovals = await _context.LeaveRequests.AsNoTracking()
            .CountAsync(l => l.Status == LeaveRequestStatuses.Pending);

        var workingEmployeeIdsToday = schedulesToday
            .Where(s => s.Status != WorkScheduleStatuses.Leave)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToHashSet();

        var checkedInEmployeeIdsToday = attendancesToday
            .Where(a => a.CheckIn.HasValue &&
                        a.Status != AttendanceStatuses.Leave)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToHashSet();

        var lateEmployeeIdsToday = attendancesToday
            .Where(a => a.LateMinutes > 0)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToHashSet();

        var totalOvertimeHoursToday = Math.Round(attendancesToday.Sum(a => a.OvertimeHours), 2);
        var successfulStatuses = new[] { "APPROVED", "SUCCESS", "GRANTED", "OK", "MATCHED" };

        var traffic = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var day = weekStart.AddDays(offset);
                var nextDay = day.AddDays(1);
                var dayLogs = weeklyLogs
                    .Where(log => log.Timestamp >= day && log.Timestamp < nextDay)
                    .ToList();

                return new
                {
                    label = GetVietnameseWeekdayLabel(day.DayOfWeek),
                    date = day,
                    checkIn = dayLogs.Count(log => string.Equals(log.Direction, "IN", StringComparison.OrdinalIgnoreCase)),
                    checkOut = dayLogs.Count(log => string.Equals(log.Direction, "OUT", StringComparison.OrdinalIgnoreCase))
                };
            })
            .ToList();

        var dailyIn = todaysLogs.Count(log => string.Equals(log.Direction, "IN", StringComparison.OrdinalIgnoreCase));
        var dailyOut = todaysLogs.Count(log => string.Equals(log.Direction, "OUT", StringComparison.OrdinalIgnoreCase));
        var dailyExceptions = todaysLogs.Count(log =>
            log.IsBypass == true ||
            log.ExceptionReasonId != null ||
            (!string.IsNullOrWhiteSpace(log.ResultStatus) && !successfulStatuses.Contains(log.ResultStatus.ToUpper())));

        var recognitionCoverage = employeeCount == 0
            ? 0
            : (int)Math.Round(trainedEmployeeCount * 100.0 / employeeCount);

        var recentActivities = recentActivitiesRaw
            .Select(activity => new DashboardRecentActivityItem
            {
                Id = $"access-{activity.LogId}",
                OccurredAt = activity.Timestamp ?? DateTime.MinValue,
                Kind = "Access",
                Title = (activity.EmployeeName ?? activity.VisitorName ?? "Chua xac dinh").Trim(),
                Subtitle = activity.GateName ?? "Chua gan cong",
                Status = activity.ResultStatus ?? (string.Equals(activity.Direction, "IN", StringComparison.OrdinalIgnoreCase) ? "Vao" : "Ra"),
                Severity = activity.IsBypass == true ? "warning" : "info",
                Route = "/access-logs",
                Meta = activity.CapturedLicensePlate ?? activity.CameraName ?? activity.ExceptionReason ?? activity.Note
            })
            .Concat(recentLaneEventsRaw.Select(item => new DashboardRecentActivityItem
            {
                Id = $"lane-{item.LaneEventId}",
                OccurredAt = item.OccurredAtUtc,
                Kind = "Lane",
                Title = item.EventType,
                Subtitle = item.LaneName ?? "Lane event",
                Status = item.Direction,
                Severity = item.EventType is "EMERGENCY_PASS" or "ESCALATION_REQUEST" ? "warning" : "info",
                Route = "/gate-transit-monitor",
                Meta = item.PlateText ?? item.Note
            }))
            .Concat(recentAlarmsRaw.Select(item => new DashboardRecentActivityItem
            {
                Id = $"alarm-{item.AlarmId}",
                OccurredAt = item.CreatedAtUtc,
                Kind = "Alarm",
                Title = item.AlarmType,
                Subtitle = item.Summary,
                Status = item.State,
                Severity = item.Severity == "Critical" ? "danger" : item.Severity == "High" ? "warning" : "info",
                Route = "/soc-console",
                Meta = item.Severity
            }))
            .Concat(recentInterventionsRaw.Select(item => new DashboardRecentActivityItem
            {
                Id = $"intervention-{item.OperationalInterventionRequestId}",
                OccurredAt = item.CreatedAtUtc,
                Kind = "Intervention",
                Title = item.SubjectName ?? item.InterventionType,
                Subtitle = item.Reason,
                Status = item.Status,
                Severity = item.Priority == "critical" ? "danger" : item.Priority == "high" ? "warning" : "info",
                Route = "/exceptions",
                Meta = item.LaneName ?? item.PlateNumber ?? item.InterventionType
            }))
            .OrderByDescending(item => item.OccurredAt)
            .Take(10)
            .Select(item => new
            {
                item.Id,
                item.OccurredAt,
                item.Kind,
                item.Title,
                item.Subtitle,
                item.Status,
                item.Severity,
                item.Route,
                item.Meta
            })
            .ToList();

        return Ok(new
        {
            snapshot = new
            {
                generatedAt = DateTime.Now,
                vehiclesInside,
                expectedVisitorsToday,
                pendingRegistrations,
                dailyCheckIn = dailyIn,
                dailyCheckOut = dailyOut,
                dailyExceptions,
                camerasConfigured,
                gatesConfigured,
                guestProfiles,
                employeeCount,
                trainedEmployeeCount,
                recognitionCoverage,
                checkedInVisitors,
                openAlarms,
                criticalOpenAlarms,
                offlineDevices,
                degradedDevices,
                activeEmergencyPasses,
                pendingInterventions,
                oldestPendingInterventionMinutes = oldestPendingIntervention.HasValue
                    ? Math.Round((nowUtc - oldestPendingIntervention.Value).TotalMinutes, 0)
                    : 0,
                employeesWorkingToday = workingEmployeeIdsToday.Count,
                employeesNotCheckedIn = Math.Max(0, workingEmployeeIdsToday.Count - checkedInEmployeeIdsToday.Count),
                employeesLateToday = lateEmployeeIdsToday.Count,
                pendingLeaveApprovals,
                totalShiftsToday = schedulesToday.Count,
                totalOvertimeHoursToday
            },
            weeklyTraffic = traffic,
            recentActivities
        });
    }

    [HttpGet("intelligence")]
    public async Task<IActionResult> GetIntelligence()
    {
        var result = await _intelligence.GetIntelligenceAsync();
        return Ok(result);
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        var today = DateTime.Today;
        var nowUtc = DateTime.UtcNow;
        var todayStartUtc = today;
        var todayEndUtc = today.AddDays(1);

        // --- Lượt ra/vào theo ngày (30 ngày) ---
        var dayLogs = await _context.AccessLogs.AsNoTracking()
            .Where(log => log.Timestamp >= today.AddDays(-29) && log.Timestamp < todayEndUtc)
            .Select(log => new { log.Timestamp, log.Direction, log.GateNameSnapshot, log.IsBypass, log.ResultStatus, log.ExceptionReasonId })
            .ToListAsync();

        var trafficByDay = dayLogs
            .GroupBy(log => (log.Timestamp ?? DateTime.MinValue).Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key,
                label = g.Key.ToString("dd/MM"),
                checkIn = g.Count(log => string.Equals(log.Direction, "IN", StringComparison.OrdinalIgnoreCase)),
                checkOut = g.Count(log => string.Equals(log.Direction, "OUT", StringComparison.OrdinalIgnoreCase)),
                total = g.Count()
            })
            .ToList();

        // --- Ra/vào theo cổng ---
        var trafficByGate = dayLogs
            .GroupBy(log => log.GateNameSnapshot ?? "Chưa gán cổng")
            .OrderByDescending(g => g.Count())
            .Select(g => new
            {
                gate = g.Key,
                checkIn = g.Count(log => string.Equals(log.Direction, "IN", StringComparison.OrdinalIgnoreCase)),
                checkOut = g.Count(log => string.Equals(log.Direction, "OUT", StringComparison.OrdinalIgnoreCase)),
                total = g.Count()
            })
            .ToList();

        // --- Ra/vào theo giờ trong ngày (7 ngày) ---
        var hourlyLogs = await _context.AccessLogs.AsNoTracking()
            .Where(log => log.Timestamp >= today.AddDays(-6) && log.Timestamp < todayEndUtc)
            .Select(log => new { log.Timestamp, log.Direction })
            .ToListAsync();

        var weekdayOrder = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var hourlyByWeekday = weekdayOrder.Select(day =>
        {
            var date = GetStartOfWeek(today, DayOfWeek.Monday).AddDays((int)day);
            var nextDate = date.AddDays(1);
            var hours = Enumerable.Range(0, 24).Select(hour =>
            {
                var inCount = hourlyLogs.Count(log =>
                    (log.Timestamp ?? DateTime.MinValue).Date == date &&
                    (log.Timestamp ?? DateTime.MinValue).Hour == hour &&
                    string.Equals(log.Direction, "IN", StringComparison.OrdinalIgnoreCase));
                var outCount = hourlyLogs.Count(log =>
                    (log.Timestamp ?? DateTime.MinValue).Date == date &&
                    (log.Timestamp ?? DateTime.MinValue).Hour == hour &&
                    string.Equals(log.Direction, "OUT", StringComparison.OrdinalIgnoreCase));
                return new { hour, checkIn = inCount, checkOut = outCount };
            }).ToList();
            return new
            {
                day = GetVietnameseWeekdayLabel(day),
                date = date,
                hours
            };
        }).ToList();

        // --- Chấm công: tỷ trọng trạng thái (30 ngày) ---
        var attendances = await _context.Attendances.AsNoTracking()
            .Where(a => a.WorkDate >= today.AddDays(-29) && a.WorkDate < todayEndUtc)
            .Select(a => new { a.Status, a.LateMinutes, a.OvertimeHours, a.WorkDate })
            .ToListAsync();

        var attendanceStatus = attendances
            .GroupBy(a => a.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        var attendanceTrend = attendances
            .GroupBy(a => a.WorkDate)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key,
                label = g.Key.ToString("dd/MM"),
                late = g.Count(a => a.LateMinutes > 0),
                absent = g.Count(a => a.Status == AttendanceStatuses.Absent),
                total = g.Count()
            })
            .ToList();

        // --- Khách thăm (30 ngày) ---
        var visits = await _context.Visits.AsNoTracking()
            .Where(v => v.ExpectedInUtc >= today.AddDays(-29) && v.ExpectedInUtc < todayEndUtc)
            .Select(v => new { v.ExpectedInUtc, v.Status })
            .ToListAsync();

        var visitorTrend = visits
            .GroupBy(v => v.ExpectedInUtc.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key,
                label = g.Key.ToString("dd/MM"),
                total = g.Count()
            })
            .ToList();

        var visitorStatus = visits
            .GroupBy(v => v.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        // --- Báo động theo mức độ & trạng thái ---
        var alarms = await _context.Alarms.AsNoTracking()
            .Where(a => a.CreatedAtUtc >= today.AddDays(-29))
            .Select(a => new { a.Severity, a.State })
            .ToListAsync();

        var alarmBySeverity = alarms
            .GroupBy(a => a.Severity ?? "Không rõ")
            .Select(g => new { severity = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        var alarmByState = alarms
            .GroupBy(a => a.State ?? "Không rõ")
            .Select(g => new { state = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        // --- Thiết bị ---
        var devices = await _context.SecurityDevices.AsNoTracking()
            .Select(d => new { d.Status, d.DeviceType })
            .ToListAsync();

        var deviceByStatus = devices
            .GroupBy(d => d.Status ?? "Không rõ")
            .Select(g => new { status = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        // --- Bất thường hôm nay ---
        var successfulStatuses = new[] { "APPROVED", "SUCCESS", "GRANTED", "OK", "MATCHED" };
        var todayAnomalies = dayLogs
            .Where(log =>
                (log.Timestamp ?? DateTime.MinValue) >= todayStartUtc &&
                (log.Timestamp ?? DateTime.MinValue) < todayEndUtc &&
                (log.IsBypass == true ||
                 log.ExceptionReasonId != null ||
                 (!string.IsNullOrWhiteSpace(log.ResultStatus) && !successfulStatuses.Contains(log.ResultStatus.ToUpper()))))
            .ToList();

        var exceptionByReason = todayAnomalies
            .GroupBy(log => log.ExceptionReasonId == null ? "Bypass / từ chối" : "Có lý do")
            .Select(g => new { reason = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        return Ok(new
        {
            generatedAt = DateTime.Now,
            kpis = new
            {
                todayTotal = dayLogs.Count(log => (log.Timestamp ?? DateTime.MinValue) >= todayStartUtc),
                todayCheckIn = dayLogs.Count(log => (log.Timestamp ?? DateTime.MinValue) >= todayStartUtc && string.Equals(log.Direction, "IN", StringComparison.OrdinalIgnoreCase)),
                todayCheckOut = dayLogs.Count(log => (log.Timestamp ?? DateTime.MinValue) >= todayStartUtc && string.Equals(log.Direction, "OUT", StringComparison.OrdinalIgnoreCase)),
                todayAnomalies = todayAnomalies.Count,
                todayVisitors = visitorTrend.Where(x => x.date == today).Sum(x => x.total),
                openAlarms = alarms.Count(a => a.State != "Closed"),
                criticalAlarms = alarms.Count(a => a.State != "Closed" && a.Severity == "Critical"),
                offlineDevices = devices.Count(d => d.Status == "Offline"),
                degradedDevices = devices.Count(d => d.Status == "Degraded"),
                vehiclesInside = await _context.Vehicles.AsNoTracking().CountAsync(v => v.ParkingStatus != null && v.ParkingStatus.ToUpper() == "IN"),
                checkedInVisitors = await _context.Visits.AsNoTracking().CountAsync(v => v.Status == VisitStatuses.CheckedIn || v.Status == VisitStatuses.Overstay)
            },
            trafficByDay,
            trafficByGate,
            hourlyByWeekday,
            attendanceStatus,
            attendanceTrend,
            visitorTrend,
            visitorStatus,
            alarmBySeverity,
            alarmByState,
            deviceByStatus,
            exceptionByReason,
            anomalies = todayAnomalies
                .OrderByDescending(log => log.Timestamp)
                .Take(10)
                .Select(log => new
                {
                    time = log.Timestamp,
                    gate = log.GateNameSnapshot,
                    direction = log.Direction,
                    note = log.IsBypass == true ? "Bypass" : log.ResultStatus
                })
                .ToList()
        });
    }

    private static DateTime GetStartOfWeek(DateTime value, DayOfWeek startOfWeek)
    {
        var diff = (7 + (value.DayOfWeek - startOfWeek)) % 7;
        return value.AddDays(-1 * diff).Date;
    }

    private static string GetVietnameseWeekdayLabel(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
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

    private sealed class DashboardRecentActivityItem
    {
        public string Id { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Severity { get; set; } = "info";
        public string Route { get; set; } = "/";
        public string? Meta { get; set; }
    }
}



