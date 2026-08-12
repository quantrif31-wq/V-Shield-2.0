using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class WorkScheduleImportHandler : EntityImportHandlerBase
{
    public WorkScheduleImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "WorkSchedule";
    public override string DisplayName => "Lịch làm việc";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "EmployeeEmail", DisplayName = "Email nhân viên", DataType = "string", IsRequired = true, Description = "Email để map nhân viên" },
        new() { FieldName = "ShiftName", DisplayName = "Tên ca", DataType = "string", IsRequired = true, Description = "Tên ca làm việc (tự động map)" },
        new() { FieldName = "WorkDate", DisplayName = "Ngày làm việc", DataType = "date", IsRequired = true, Description = "Ví dụ: 2026-08-10" },
        new() { FieldName = "Status", DisplayName = "Trạng thái", DataType = "string", Description = "Scheduled / Confirmed / Completed / Cancelled" },
        new() { FieldName = "Note", DisplayName = "Ghi chú", DataType = "string", Description = "Ghi chú (nếu có)" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var email = GetString(row, "EmployeeEmail");
        var shiftName = GetString(row, "ShiftName");
        var date = GetDateTime(row, "WorkDate");

        if (string.IsNullOrWhiteSpace(email))
            errors.Add(MakeError(rowIndex, "EmployeeEmail", "Email nhân viên không được để trống"));
        if (string.IsNullOrWhiteSpace(shiftName))
            errors.Add(MakeError(rowIndex, "ShiftName", "Tên ca không được để trống"));
        if (date == null)
            errors.Add(MakeError(rowIndex, "WorkDate", "Ngày làm việc không hợp lệ (vd: 2026-08-10)"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(email) && !await db.Employees.AnyAsync(e => e.Email == email))
            errors.Add(MakeError(rowIndex, "EmployeeEmail", $"Không tìm thấy nhân viên với email '{email}'"));
        if (!string.IsNullOrWhiteSpace(shiftName) && !await db.Shifts.AnyAsync(s => s.ShiftName == shiftName))
            errors.Add(MakeError(rowIndex, "ShiftName", $"Không tìm thấy ca '{shiftName}'"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var email = GetString(row, "EmployeeEmail");
        var shiftName = GetString(row, "ShiftName");
        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Email == email);
        var shift = await db.Shifts.FirstOrDefaultAsync(s => s.ShiftName == shiftName);
        if (emp == null || shift == null) return null;

        var schedule = new WorkSchedule
        {
            EmployeeId = emp.EmployeeId,
            ShiftId = shift.ShiftId,
            WorkDate = GetDateTime(row, "WorkDate") ?? DateTime.UtcNow.Date,
            Status = GetString(row, "Status") ?? WorkScheduleStatuses.Scheduled,
            Note = GetString(row, "Note"),
        };
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var s = (WorkSchedule)entity;
        return new Dictionary<string, object?>
        {
            ["ScheduleId"] = s.ScheduleId,
            ["EmployeeEmail"] = s.Employee?.Email,
            ["EmployeeName"] = s.Employee?.FullName,
            ["ShiftName"] = s.Shift?.ShiftName,
            ["WorkDate"] = s.WorkDate.ToString("yyyy-MM-dd"),
            ["Status"] = s.Status,
            ["Note"] = s.Note,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.WorkSchedules.Include(s => s.Employee).Include(s => s.Shift).AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
