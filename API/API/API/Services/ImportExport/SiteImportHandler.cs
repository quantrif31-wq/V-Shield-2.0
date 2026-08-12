using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class SiteImportHandler : EntityImportHandlerBase
{
    public SiteImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "Site";
    public override string DisplayName => "Khu vực / Site";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "Name", DisplayName = "Tên site", DataType = "string", IsRequired = true, Description = "Tên site/khu vực" },
        new() { FieldName = "Code", DisplayName = "Mã site", DataType = "string", IsRequired = true, Description = "Mã duy nhất" },
        new() { FieldName = "CompanyCode", DisplayName = "Mã công ty", DataType = "string", IsRequired = true, Description = "Mã công ty sở hữu" },
        new() { FieldName = "Address", DisplayName = "Địa chỉ", DataType = "string", Description = "Địa chỉ" },
        new() { FieldName = "TimeZoneId", DisplayName = "Múi giờ", DataType = "string", Description = "Mặc định Asia/Ho_Chi_Minh" },
        new() { FieldName = "IsActive", DisplayName = "Kích hoạt", DataType = "bool", Description = "true/false" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "Name");
        var code = GetString(row, "Code");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "Name", "Tên site không được để trống"));
        if (string.IsNullOrWhiteSpace(code))
            errors.Add(MakeError(rowIndex, "Code", "Mã site không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(code) && await db.Sites.AnyAsync(s => s.Code == code))
            errors.Add(MakeError(rowIndex, "Code", $"Mã site '{code}' đã tồn tại"));

        var companyCode = GetString(row, "CompanyCode");
        if (!string.IsNullOrWhiteSpace(companyCode) && !await db.Companies.AnyAsync(c => c.Code == companyCode))
            errors.Add(MakeError(rowIndex, "CompanyCode", $"Không tìm thấy công ty với mã '{companyCode}'"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var companyCode = GetString(row, "CompanyCode");
        int companyId = 0;
        if (!string.IsNullOrWhiteSpace(companyCode))
        {
            var company = await db.Companies.FirstOrDefaultAsync(c => c.Code == companyCode);
            companyId = company?.CompanyId ?? 0;
        }
        if (companyId == 0)
        {
            var first = await db.Companies.OrderBy(c => c.CompanyId).FirstOrDefaultAsync();
            companyId = first?.CompanyId ?? 0;
        }

        var site = new Site
        {
            Name = GetString(row, "Name") ?? "",
            Code = GetString(row, "Code") ?? "",
            CompanyId = companyId,
            Address = GetString(row, "Address"),
            Latitude = GetDecimal(row, "Latitude"),
            Longitude = GetDecimal(row, "Longitude"),
            TimeZoneId = GetString(row, "TimeZoneId") ?? "Asia/Ho_Chi_Minh",
            IsActive = GetBool(row, "IsActive") ?? true,
        };
        db.Sites.Add(site);
        await db.SaveChangesAsync();
        return site;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var s = (Site)entity;
        return new Dictionary<string, object?>
        {
            ["SiteId"] = s.SiteId,
            ["Name"] = s.Name,
            ["Code"] = s.Code,
            ["CompanyCode"] = s.Company?.Code,
            ["Address"] = s.Address,
            ["TimeZoneId"] = s.TimeZoneId,
            ["IsActive"] = s.IsActive,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.Sites.Include(s => s.Company).AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
