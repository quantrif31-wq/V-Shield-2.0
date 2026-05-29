using API.Models;

namespace API.Services;

public interface IAttendanceCalculationService
{
    AttendanceCalculationResult Calculate(
        DateTime workDate,
        DateTime? checkIn,
        DateTime? checkOut,
        Shift? shift);
}

public class AttendanceCalculationService : IAttendanceCalculationService
{
    public AttendanceCalculationResult Calculate(
        DateTime workDate,
        DateTime? checkIn,
        DateTime? checkOut,
        Shift? shift)
    {
        if (checkIn == null)
        {
            return new AttendanceCalculationResult
            {
                Status = AttendanceStatuses.NotCheckedIn
            };
        }

        if (checkOut == null)
        {
            if (shift == null)
            {
                return new AttendanceCalculationResult
                {
                    Status = AttendanceStatuses.OutOfSchedule
                };
            }

            var (shiftStart, _) = BuildShiftWindow(workDate, shift);
            var allowedStart = shiftStart.AddMinutes(shift.AllowedLateMinutes);
            var lateMinutes = Math.Max(0, (int)Math.Floor((checkIn.Value - allowedStart).TotalMinutes));

            return new AttendanceCalculationResult
            {
                LateMinutes = lateMinutes,
                Status = lateMinutes > 0 ? AttendanceStatuses.Late : AttendanceStatuses.CheckedIn
            };
        }

        if (checkOut.Value < checkIn.Value)
            throw new InvalidOperationException("Thoi gian check-out khong duoc nho hon check-in.");

        if (shift == null)
        {
            var rawHours = Math.Max(0, (checkOut.Value - checkIn.Value).TotalHours);
            return new AttendanceCalculationResult
            {
                LateMinutes = 0,
                EarlyLeaveMinutes = 0,
                OvertimeHours = 0,
                TotalWorkingHours = Math.Round((decimal)rawHours, 2),
                Status = AttendanceStatuses.OutOfSchedule
            };
        }

        var (shiftStartWithDate, shiftEndWithDate) = BuildShiftWindow(workDate, shift);
        var allowedStartWithDate = shiftStartWithDate.AddMinutes(shift.AllowedLateMinutes);
        var allowedEndWithDate = shiftEndWithDate.AddMinutes(-shift.AllowedEarlyLeaveMinutes);

        var late = Math.Max(0, (int)Math.Floor((checkIn.Value - allowedStartWithDate).TotalMinutes));
        var early = Math.Max(0, (int)Math.Floor((allowedEndWithDate - checkOut.Value).TotalMinutes));
        var overtimeHours = Math.Max(0, (checkOut.Value - shiftEndWithDate).TotalHours);
        var totalHours = Math.Max(0, (checkOut.Value - checkIn.Value).TotalHours - (shift.BreakMinutes / 60.0));

        return new AttendanceCalculationResult
        {
            LateMinutes = late,
            EarlyLeaveMinutes = early,
            OvertimeHours = Math.Round((decimal)overtimeHours, 2),
            TotalWorkingHours = Math.Round((decimal)totalHours, 2),
            Status = BuildFinalStatus(late, early)
        };
    }

    private static (DateTime ShiftStart, DateTime ShiftEnd) BuildShiftWindow(DateTime workDate, Shift shift)
    {
        var baseDate = workDate.Date;
        var shiftStart = baseDate.Add(shift.StartTime);
        var shiftEnd = baseDate.Add(shift.EndTime);

        if (shiftEnd <= shiftStart)
            shiftEnd = shiftEnd.AddDays(1);

        return (shiftStart, shiftEnd);
    }

    private static string BuildFinalStatus(int lateMinutes, int earlyLeaveMinutes)
    {
        if (lateMinutes > 0 && earlyLeaveMinutes > 0) return AttendanceStatuses.LateAndEarlyLeave;
        if (lateMinutes > 0) return AttendanceStatuses.Late;
        if (earlyLeaveMinutes > 0) return AttendanceStatuses.EarlyLeave;
        return AttendanceStatuses.Completed;
    }
}

public class AttendanceCalculationResult
{
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public decimal TotalWorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string Status { get; set; } = AttendanceStatuses.NotCheckedIn;
}

