using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class FacilityFloorImportHandler : EntityImportHandlerBase
{
    public FacilityFloorImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "FacilityFloor";
    public override string DisplayName => "Tầng";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "Name", DisplayName = "Tên tầng", DataType = "string", IsRequired = true, Description = "Tên tầng" },
        new() { FieldName = "Code", DisplayName = "Mã tầng", DataType = "string", IsRequired = true, Description = "Mã duy nhất" },
        new() { FieldName = "BuildingCode", DisplayName = "Mã tòa nhà", DataType = "string", IsRequired = true, Description = "Mã tòa nhà sở hữu" },
        new() { FieldName = "SortOrder", DisplayName = "Thứ tự", DataType = "int", Description = "Thứ tự sắp xếp" },
        new() { FieldName = "IsActive", DisplayName = "Kích hoạt", DataType = "bool", Description = "true/false" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "Name");
        var code = GetString(row, "Code");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "Name", "Tên tầng không được để trống"));
        if (string.IsNullOrWhiteSpace(code))
            errors.Add(MakeError(rowIndex, "Code", "Mã tầng không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(code) && await db.FacilityFloors.AnyAsync(f => f.Code == code))
            errors.Add(MakeError(rowIndex, "Code", $"Mã tầng '{code}' đã tồn tại"));

        var buildingCode = GetString(row, "BuildingCode");
        if (!string.IsNullOrWhiteSpace(buildingCode) && !await db.Buildings.AnyAsync(b => b.Code == buildingCode))
            errors.Add(MakeError(rowIndex, "BuildingCode", $"Không tìm thấy tòa nhà với mã '{buildingCode}'"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var buildingCode = GetString(row, "BuildingCode");
        int buildingId = 0;
        if (!string.IsNullOrWhiteSpace(buildingCode))
        {
            var building = await db.Buildings.FirstOrDefaultAsync(b => b.Code == buildingCode);
            buildingId = building?.BuildingId ?? 0;
        }

        var floor = new FacilityFloor
        {
            Name = GetString(row, "Name") ?? "",
            Code = GetString(row, "Code") ?? "",
            BuildingId = buildingId,
            SortOrder = GetInt(row, "SortOrder") ?? 0,
            IsActive = GetBool(row, "IsActive") ?? true,
        };
        db.FacilityFloors.Add(floor);
        await db.SaveChangesAsync();
        return floor;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var f = (FacilityFloor)entity;
        return new Dictionary<string, object?>
        {
            ["FacilityFloorId"] = f.FacilityFloorId,
            ["Name"] = f.Name,
            ["Code"] = f.Code,
            ["BuildingCode"] = f.Building?.Code,
            ["SortOrder"] = f.SortOrder,
            ["IsActive"] = f.IsActive,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.FacilityFloors.Include(f => f.Building).AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
