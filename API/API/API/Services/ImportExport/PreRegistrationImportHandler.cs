using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class PreRegistrationImportHandler : EntityImportHandlerBase
{
    public PreRegistrationImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "PreRegistration";
    public override string DisplayName => "Đăng ký khách trước";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "GuestPhone", DisplayName = "SĐT khách", DataType = "string", Description = "SĐT để map hồ sơ khách (tự động tạo nếu chưa có)" },
        new() { FieldName = "GuestFullName", DisplayName = "Tên khách", DataType = "string", IsRequired = true, Description = "Tên khách (nếu chưa có hồ sơ)" },
        new() { FieldName = "HostEmployeeEmail", DisplayName = "Email NV đăng ký", DataType = "string", Description = "Email nhân viên chủ trì" },
        new() { FieldName = "ExpectedTimeIn", DisplayName = "Giờ vào dự kiến", DataType = "datetime", IsRequired = true, Description = "Ví dụ: 2026-08-10 09:00" },
        new() { FieldName = "ExpectedTimeOut", DisplayName = "Giờ ra dự kiến", DataType = "datetime", IsRequired = true, Description = "Ví dụ: 2026-08-10 17:00" },
        new() { FieldName = "NumberOfVisitors", DisplayName = "Số khách", DataType = "int", Description = "Số lượng khách, mặc định 1" },
        new() { FieldName = "Status", DisplayName = "Trạng thái", DataType = "string", Description = "Pending / Approved / CheckedIn / Completed / Cancelled" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var guestName = GetString(row, "GuestFullName");
        var hostEmail = GetString(row, "HostEmployeeEmail");

        if (string.IsNullOrWhiteSpace(guestName))
            errors.Add(MakeError(rowIndex, "GuestFullName", "Tên khách không được để trống"));
        if (GetDateTime(row, "ExpectedTimeIn") == null)
            errors.Add(MakeError(rowIndex, "ExpectedTimeIn", "Giờ vào dự kiến không hợp lệ"));
        if (GetDateTime(row, "ExpectedTimeOut") == null)
            errors.Add(MakeError(rowIndex, "ExpectedTimeOut", "Giờ ra dự kiến không hợp lệ"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(hostEmail) && !await db.Employees.AnyAsync(e => e.Email == hostEmail))
            errors.Add(MakeError(rowIndex, "HostEmployeeEmail", $"Không tìm thấy nhân viên với email '{hostEmail}'"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var guestName = GetString(row, "GuestFullName") ?? "";
        var guestPhone = GetString(row, "GuestPhone");
        var guest = await db.GuestProfiles.FirstOrDefaultAsync(g =>
            (!string.IsNullOrEmpty(guestPhone) && g.Phone == guestPhone) || g.FullName == guestName);

        if (guest == null)
        {
            guest = new GuestProfile
            {
                FullName = guestName,
                Phone = guestPhone,
            };
            db.GuestProfiles.Add(guest);
            await db.SaveChangesAsync();
        }

        var hostEmail = GetString(row, "HostEmployeeEmail");
        int? hostId = null;
        if (!string.IsNullOrWhiteSpace(hostEmail))
        {
            var host = await db.Employees.FirstOrDefaultAsync(e => e.Email == hostEmail);
            hostId = host?.EmployeeId;
        }

        var reg = new PreRegistration
        {
            GuestId = guest.GuestId,
            HostEmployeeId = hostId,
            ExpectedTimeIn = GetDateTime(row, "ExpectedTimeIn") ?? DateTime.Now,
            ExpectedTimeOut = GetDateTime(row, "ExpectedTimeOut") ?? DateTime.Now.AddHours(1),
            NumberOfVisitors = GetInt(row, "NumberOfVisitors") ?? 1,
            Status = GetString(row, "Status") ?? "Pending",
        };
        db.PreRegistrations.Add(reg);
        await db.SaveChangesAsync();
        return reg;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var r = (PreRegistration)entity;
        return new Dictionary<string, object?>
        {
            ["RegistrationId"] = r.RegistrationId,
            ["GuestFullName"] = r.Guest?.FullName,
            ["GuestPhone"] = r.Guest?.Phone,
            ["HostEmployeeEmail"] = r.HostEmployee?.Email,
            ["ExpectedTimeIn"] = r.ExpectedTimeIn.ToString("yyyy-MM-dd HH:mm"),
            ["ExpectedTimeOut"] = r.ExpectedTimeOut.ToString("yyyy-MM-dd HH:mm"),
            ["NumberOfVisitors"] = r.NumberOfVisitors,
            ["Status"] = r.Status,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.PreRegistrations
            .Include(r => r.Guest)
            .Include(r => r.HostEmployee)
            .AsNoTracking()
            .ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
