using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class GateImportHandler : EntityImportHandlerBase
{
    public GateImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "Gate";
    public override string DisplayName => "Cổng";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "GateName", DisplayName = "Tên cổng", DataType = "string", IsRequired = true, Description = "Tên cổng ra vào" },
        new() { FieldName = "Location", DisplayName = "Vị trí", DataType = "string", Description = "Mô tả vị trí" },
        new() { FieldName = "Latitude", DisplayName = "Vĩ độ", DataType = "decimal", Description = "Vĩ độ (nếu có)" },
        new() { FieldName = "Longitude", DisplayName = "Kinh độ", DataType = "decimal", Description = "Kinh độ (nếu có)" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "GateName");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "GateName", "Tên cổng không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(name) && await db.Gates.AnyAsync(g => g.GateName == name))
            errors.Add(MakeError(rowIndex, "GateName", $"Cổng '{name}' đã tồn tại"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();
        var gate = new Gate
        {
            GateName = GetString(row, "GateName") ?? "",
            Location = GetString(row, "Location"),
            Latitude = GetDecimal(row, "Latitude"),
            Longitude = GetDecimal(row, "Longitude"),
        };
        db.Gates.Add(gate);
        await db.SaveChangesAsync();
        return gate;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var g = (Gate)entity;
        return new Dictionary<string, object?>
        {
            ["GateId"] = g.GateId,
            ["GateName"] = g.GateName,
            ["Location"] = g.Location,
            ["Latitude"] = g.Latitude,
            ["Longitude"] = g.Longitude,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.Gates.AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
