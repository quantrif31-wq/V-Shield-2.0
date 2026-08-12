using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class CompanyImportHandler : EntityImportHandlerBase
{
    public CompanyImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "Company";
    public override string DisplayName => "Công ty";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "Name", DisplayName = "Tên công ty", DataType = "string", IsRequired = true, Description = "Tên công ty" },
        new() { FieldName = "Code", DisplayName = "Mã công ty", DataType = "string", IsRequired = true, Description = "Mã duy nhất" },
        new() { FieldName = "IsActive", DisplayName = "Kích hoạt", DataType = "bool", Description = "true/false" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "Name");
        var code = GetString(row, "Code");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "Name", "Tên công ty không được để trống"));
        if (string.IsNullOrWhiteSpace(code))
            errors.Add(MakeError(rowIndex, "Code", "Mã công ty không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(code) && await db.Companies.AnyAsync(c => c.Code == code))
            errors.Add(MakeError(rowIndex, "Code", $"Mã công ty '{code}' đã tồn tại"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();
        var company = new Company
        {
            Name = GetString(row, "Name") ?? "",
            Code = GetString(row, "Code") ?? "",
            IsActive = GetBool(row, "IsActive") ?? true,
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var c = (Company)entity;
        return new Dictionary<string, object?>
        {
            ["CompanyId"] = c.CompanyId,
            ["Name"] = c.Name,
            ["Code"] = c.Code,
            ["IsActive"] = c.IsActive,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.Companies.AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
