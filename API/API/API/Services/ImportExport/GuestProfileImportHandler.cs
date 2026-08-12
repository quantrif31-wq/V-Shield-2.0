using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class GuestProfileImportHandler : EntityImportHandlerBase
{
    public GuestProfileImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "GuestProfile";
    public override string DisplayName => "Khách";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "FullName", DisplayName = "Họ và tên khách", DataType = "string", IsRequired = true, Description = "Tên khách" },
        new() { FieldName = "Phone", DisplayName = "Số điện thoại", DataType = "string", Description = "SĐT liên hệ" },
        new() { FieldName = "DefaultLicensePlate", DisplayName = "Biển số xe", DataType = "string", Description = "Biển số xe mặc định (nếu có)" },
        new() { FieldName = "FaceImageUrl", DisplayName = "URL ảnh khuôn mặt", DataType = "string", Description = "URL ảnh khuôn mặt (nếu có)" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "FullName");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "FullName", "Họ và tên khách không được để trống"));

        await using var db = await CreateDbContextAsync();
        var phone = GetString(row, "Phone");
        if (!string.IsNullOrWhiteSpace(phone) && await db.GuestProfiles.AnyAsync(g => g.Phone == phone))
            errors.Add(MakeError(rowIndex, "Phone", $"SĐT '{phone}' đã tồn tại"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();
        var guest = new GuestProfile
        {
            FullName = GetString(row, "FullName") ?? "",
            Phone = GetString(row, "Phone"),
            DefaultLicensePlate = GetString(row, "DefaultLicensePlate"),
            FaceImageUrl = GetString(row, "FaceImageUrl"),
        };
        db.GuestProfiles.Add(guest);
        await db.SaveChangesAsync();
        return guest;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var g = (GuestProfile)entity;
        return new Dictionary<string, object?>
        {
            ["GuestId"] = g.GuestId,
            ["FullName"] = g.FullName,
            ["Phone"] = g.Phone,
            ["DefaultLicensePlate"] = g.DefaultLicensePlate,
            ["FaceImageUrl"] = g.FaceImageUrl,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.GuestProfiles.AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
