using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class SecurityZoneImportHandler : EntityImportHandlerBase
{
    public SecurityZoneImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "SecurityZone";
    public override string DisplayName => "Vùng an ninh";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "Name", DisplayName = "Tên vùng", DataType = "string", IsRequired = true, Description = "Tên vùng an ninh" },
        new() { FieldName = "Code", DisplayName = "Mã vùng", DataType = "string", IsRequired = true, Description = "Mã duy nhất" },
        new() { FieldName = "SiteCode", DisplayName = "Mã site", DataType = "string", IsRequired = true, Description = "Mã site sở hữu" },
        new() { FieldName = "SecurityLevel", DisplayName = "Cấp độ an ninh", DataType = "string", Description = "Normal / Restricted" },
        new() { FieldName = "IsRestricted", DisplayName = "Vùng hạn chế", DataType = "bool", Description = "true/false" },
        new() { FieldName = "IsActive", DisplayName = "Kích hoạt", DataType = "bool", Description = "true/false" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "Name");
        var code = GetString(row, "Code");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "Name", "Tên vùng an ninh không được để trống"));
        if (string.IsNullOrWhiteSpace(code))
            errors.Add(MakeError(rowIndex, "Code", "Mã vùng an ninh không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(code) && await db.SecurityZones.AnyAsync(z => z.Code == code))
            errors.Add(MakeError(rowIndex, "Code", $"Mã vùng '{code}' đã tồn tại"));

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

        var zone = new SecurityZone
        {
            Name = GetString(row, "Name") ?? "",
            Code = GetString(row, "Code") ?? "",
            SiteId = siteId,
            SecurityLevel = GetString(row, "SecurityLevel") ?? "Normal",
            IsRestricted = GetBool(row, "IsRestricted") ?? false,
            IsActive = GetBool(row, "IsActive") ?? true,
        };
        db.SecurityZones.Add(zone);
        await db.SaveChangesAsync();
        return zone;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var z = (SecurityZone)entity;
        return new Dictionary<string, object?>
        {
            ["SecurityZoneId"] = z.SecurityZoneId,
            ["Name"] = z.Name,
            ["Code"] = z.Code,
            ["SiteCode"] = z.Site?.Code,
            ["SecurityLevel"] = z.SecurityLevel,
            ["IsRestricted"] = z.IsRestricted,
            ["IsActive"] = z.IsActive,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.SecurityZones.Include(z => z.Site).AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
