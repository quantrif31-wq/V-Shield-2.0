using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class BuildingImportHandler : EntityImportHandlerBase
{
    public BuildingImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "Building";
    public override string DisplayName => "Tòa nhà";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "Name", DisplayName = "Tên tòa nhà", DataType = "string", IsRequired = true, Description = "Tên tòa nhà" },
        new() { FieldName = "Code", DisplayName = "Mã tòa nhà", DataType = "string", IsRequired = true, Description = "Mã duy nhất" },
        new() { FieldName = "SiteCode", DisplayName = "Mã site", DataType = "string", IsRequired = true, Description = "Mã site sở hữu" },
        new() { FieldName = "TotalFloors", DisplayName = "Số tầng", DataType = "int", Description = "Tổng số tầng" },
        new() { FieldName = "IsActive", DisplayName = "Kích hoạt", DataType = "bool", Description = "true/false" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "Name");
        var code = GetString(row, "Code");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "Name", "Tên tòa nhà không được để trống"));
        if (string.IsNullOrWhiteSpace(code))
            errors.Add(MakeError(rowIndex, "Code", "Mã tòa nhà không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(code) && await db.Buildings.AnyAsync(b => b.Code == code))
            errors.Add(MakeError(rowIndex, "Code", $"Mã tòa nhà '{code}' đã tồn tại"));

        var siteCode = GetString(row, "SiteCode");
        if (!string.IsNullOrWhiteSpace(siteCode) && !await db.Sites.AnyAsync(s => s.Code == siteCode))
            errors.Add(MakeError(rowIndex, "SiteCode", $"Không tìm thấy site với mã '{siteCode}'"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var siteCode = GetString(row, "SiteCode");
        int siteId = 0;
        if (!string.IsNullOrWhiteSpace(siteCode))
        {
            var site = await db.Sites.FirstOrDefaultAsync(s => s.Code == siteCode);
            siteId = site?.SiteId ?? 0;
        }

        var building = new Building
        {
            Name = GetString(row, "Name") ?? "",
            Code = GetString(row, "Code") ?? "",
            SiteId = siteId,
            Latitude = GetDecimal(row, "Latitude"),
            Longitude = GetDecimal(row, "Longitude"),
            TotalFloors = GetInt(row, "TotalFloors"),
            IsActive = GetBool(row, "IsActive") ?? true,
        };
        db.Buildings.Add(building);
        await db.SaveChangesAsync();
        return building;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var b = (Building)entity;
        return new Dictionary<string, object?>
        {
            ["BuildingId"] = b.BuildingId,
            ["Name"] = b.Name,
            ["Code"] = b.Code,
            ["SiteCode"] = b.Site?.Code,
            ["TotalFloors"] = b.TotalFloors,
            ["IsActive"] = b.IsActive,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.Buildings.Include(b => b.Site).AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
