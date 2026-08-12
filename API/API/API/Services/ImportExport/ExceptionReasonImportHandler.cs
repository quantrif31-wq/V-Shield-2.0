using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class ExceptionReasonImportHandler : EntityImportHandlerBase
{
    public ExceptionReasonImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "ExceptionReason";
    public override string DisplayName => "Lý do ngoại lệ";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "ReasonCode", DisplayName = "Mã lý do", DataType = "string", IsRequired = true, Description = "Ví dụ: BYPASS_MANUAL" },
        new() { FieldName = "Description", DisplayName = "Mô tả", DataType = "string", IsRequired = true, Description = "Mô tả lý do ngoại lệ" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var code = GetString(row, "ReasonCode");
        var desc = GetString(row, "Description");
        if (string.IsNullOrWhiteSpace(code))
            errors.Add(MakeError(rowIndex, "ReasonCode", "Mã lý do không được để trống"));
        if (string.IsNullOrWhiteSpace(desc))
            errors.Add(MakeError(rowIndex, "Description", "Mô tả không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(code) && await db.ExceptionReasons.AnyAsync(r => r.ReasonCode == code))
            errors.Add(MakeError(rowIndex, "ReasonCode", $"Lý do '{code}' đã tồn tại"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();
        var reason = new ExceptionReason
        {
            ReasonCode = GetString(row, "ReasonCode") ?? "",
            Description = GetString(row, "Description") ?? "",
        };
        db.ExceptionReasons.Add(reason);
        await db.SaveChangesAsync();
        return reason;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var r = (ExceptionReason)entity;
        return new Dictionary<string, object?>
        {
            ["ReasonId"] = r.ReasonId,
            ["ReasonCode"] = r.ReasonCode,
            ["Description"] = r.Description,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.ExceptionReasons.AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
