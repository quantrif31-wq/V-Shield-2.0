using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class VehicleTypeImportHandler : EntityImportHandlerBase
{
    public VehicleTypeImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "VehicleType";
    public override string DisplayName => "Loại phương tiện";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "TypeName", DisplayName = "Tên loại xe", DataType = "string", IsRequired = true, Description = "Ví dụ: Ô tô, Xe máy, Xe tải" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "TypeName");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "TypeName", "Tên loại xe không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(name) && await db.VehicleTypes.AnyAsync(v => v.TypeName == name))
            errors.Add(MakeError(rowIndex, "TypeName", $"Loại xe '{name}' đã tồn tại"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();
        var vt = new VehicleType { TypeName = GetString(row, "TypeName") ?? "" };
        db.VehicleTypes.Add(vt);
        await db.SaveChangesAsync();
        return vt;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var v = (VehicleType)entity;
        return new Dictionary<string, object?>
        {
            ["VehicleTypeId"] = v.VehicleTypeId,
            ["TypeName"] = v.TypeName,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.VehicleTypes.AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
