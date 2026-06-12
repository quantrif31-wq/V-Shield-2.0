namespace API.Models;

public static class WorkScheduleStatuses
{
    public const string Scheduled = "Scheduled";
    public const string Worked = "Worked";
    public const string Leave = "Leave";
    public const string Absent = "Absent";
    public const string Cancelled = "Cancelled";
    public const string Changed = "Changed";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Scheduled,
        Worked,
        Leave,
        Absent,
        Cancelled,
        Changed
    };
}

public static class AttendanceStatuses
{
    public const string NotCheckedIn = "NotCheckedIn";
    public const string CheckedIn = "CheckedIn";
    public const string Completed = "Completed";
    public const string Late = "Late";
    public const string EarlyLeave = "EarlyLeave";
    public const string LateAndEarlyLeave = "LateAndEarlyLeave";
    public const string Absent = "Absent";
    public const string Leave = "Leave";
    public const string ForgotCheckout = "ForgotCheckout";
    public const string OutOfSchedule = "OutOfSchedule";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        NotCheckedIn,
        CheckedIn,
        Completed,
        Late,
        EarlyLeave,
        LateAndEarlyLeave,
        Absent,
        Leave,
        ForgotCheckout,
        OutOfSchedule
    };
}

public static class AttendanceSources
{
    public const string Manual = "Manual";
    public const string AccessLog = "AccessLog";
    public const string Qr = "QR";
    public const string FaceAi = "FaceAI";
    public const string Card = "Card";
    public const string ZoneTransit = "ZoneTransit";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Manual,
        AccessLog,
        Qr,
        FaceAi,
        Card,
        ZoneTransit
    };
}

public static class LeaveTypes
{
    public const string AnnualLeave = "AnnualLeave";
    public const string SickLeave = "SickLeave";
    public const string UnpaidLeave = "UnpaidLeave";
    public const string PersonalLeave = "PersonalLeave";
    public const string Other = "Other";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        AnnualLeave,
        SickLeave,
        UnpaidLeave,
        PersonalLeave,
        Other
    };
}

public static class LeaveRequestStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    };
}

