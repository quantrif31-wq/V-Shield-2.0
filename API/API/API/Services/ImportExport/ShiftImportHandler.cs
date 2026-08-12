using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class ShiftImportHandler : EntityImportHandlerBase
{
    public ShiftImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "Shift";
    public override string DisplayName => "Ca làm việc";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "ShiftName", DisplayName = "Tên ca", DataType = "string", IsRequired = true, Description = "Ví dụ: Ca sáng" },
        new() { FieldName = "StartTime", DisplayName = "Giờ bắt đầu", DataType = "time", IsRequired = true, Description = "Ví dụ: 08:00" },
        new() { FieldName = "EndTime", DisplayName = "Giờ kết thúc", DataType = "time", IsRequired = true, Description = "Ví dụ: 17:00" },
        new() { FieldName = "BreakMinutes", DisplayName = "Nghỉ giải lao (phút)", DataType = "int", Description = "Số phút nghỉ, mặc định 0" },
        new() { FieldName = "AllowedLateMinutes", DisplayName = "Cho phép đi muộn (phút)", DataType = "int", Description = "Mặc định 0" },
        new() { FieldName = "AllowedEarlyLeaveMinutes", DisplayName = "Cho phép về sớm (phút)", DataType = "int", Description = "Mặc định 0" },
        new() { FieldName = "IsActive", DisplayName = "Kích hoạt", DataType = "bool", Description = "true/false" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "ShiftName");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "ShiftName", "Tên ca không được để trống"));

        if (GetTimeSpan(row, "StartTime") == null)
            errors.Add(MakeError(rowIndex, "StartTime", "Giờ bắt đầu không hợp lệ (vd: 08:00)"));
        if (GetTimeSpan(row, "EndTime") == null)
            errors.Add(MakeError(rowIndex, "EndTime", "Giờ kết thúc không hợp lệ (vd: 17:00)"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(name) && await db.Shifts.AnyAsync(s => s.ShiftName == name))
            errors.Add(MakeError(rowIndex, "ShiftName", $"Ca '{name}' đã tồn tại"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();
        var shift = new Shift
        {
            ShiftName = GetString(row, "ShiftName") ?? "",
            StartTime = GetTimeSpan(row, "StartTime") ?? TimeSpan.Zero,
            EndTime = GetTimeSpan(row, "EndTime") ?? TimeSpan.Zero,
            BreakMinutes = GetInt(row, "BreakMinutes") ?? 0,
            AllowedLateMinutes = GetInt(row, "AllowedLateMinutes") ?? 0,
            AllowedEarlyLeaveMinutes = GetInt(row, "AllowedEarlyLeaveMinutes") ?? 0,
            IsActive = GetBool(row, "IsActive") ?? true,
        };
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();
        return shift;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var s = (Shift)entity;
        return new Dictionary<string, object?>
        {
            ["ShiftId"] = s.ShiftId,
            ["ShiftName"] = s.ShiftName,
            ["StartTime"] = s.StartTime.ToString(),
            ["EndTime"] = s.EndTime.ToString(),
            ["BreakMinutes"] = s.BreakMinutes,
            ["AllowedLateMinutes"] = s.AllowedLateMinutes,
            ["AllowedEarlyLeaveMinutes"] = s.AllowedEarlyLeaveMinutes,
            ["IsActive"] = s.IsActive,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.Shifts.AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
