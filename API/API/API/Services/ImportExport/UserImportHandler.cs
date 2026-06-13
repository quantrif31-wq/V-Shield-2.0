using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class UserImportHandler : EntityImportHandlerBase
{
    public UserImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "AppUser";
    public override string DisplayName => "Người dùng";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "Username", DisplayName = "Tên đăng nhập", DataType = "string", IsRequired = true, Description = "Tên tài khoản" },
        new() { FieldName = "FullName", DisplayName = "Họ tên", DataType = "string", Description = "Tên đầy đủ" },
        new() { FieldName = "Role", DisplayName = "Vai trò", DataType = "string", Description = "Admin / Staff / BaoVe / QuanLy", AllowedValues = ["Admin", "Staff", "BaoVe", "QuanLy"] },
        new() { FieldName = "IsActive", DisplayName = "Kích hoạt", DataType = "bool", Description = "true = Kích hoạt, false = Vô hiệu" },
        new() { FieldName = "EmployeeEmail", DisplayName = "Email nhân viên", DataType = "string", Description = "Gắn với nhân viên qua Email" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var username = GetString(row, "Username");
        if (string.IsNullOrWhiteSpace(username))
            errors.Add(MakeError(rowIndex, "Username", "Tên đăng nhập không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(username) && await db.AppUsers.AnyAsync(u => u.Username == username))
            errors.Add(MakeError(rowIndex, "Username", $"Tên đăng nhập '{username}' đã tồn tại"));

        var role = GetString(row, "Role");
        if (!string.IsNullOrWhiteSpace(role) && role is not ("Admin" or "Staff" or "BaoVe" or "QuanLy"))
            errors.Add(MakeError(rowIndex, "Role", $"Vai trò không hợp lệ: '{role}'. Chấp nhận: Admin, Staff, BaoVe, QuanLy"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var empEmail = GetString(row, "EmployeeEmail");
        int? empId = null;
        if (!string.IsNullOrWhiteSpace(empEmail))
        {
            var emp = await db.Employees.FirstOrDefaultAsync(e => e.Email == empEmail);
            if (emp != null) empId = emp.EmployeeId;
        }

        var defaultPassword = Environment.GetEnvironmentVariable("VSHIELD_IMPORT_DEFAULT_PASSWORD") ?? "VShield@123";

        var user = new AppUser
        {
            Username = GetString(row, "Username") ?? "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
            FullName = GetString(row, "FullName"),
            Role = GetString(row, "Role") ?? "Staff",
            IsActive = GetBool(row, "IsActive") ?? true,
            EmployeeId = empId,
        };

        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var u = (AppUser)entity;
        return new Dictionary<string, object?>
        {
            ["UserId"] = u.UserId,
            ["Username"] = u.Username,
            ["FullName"] = u.FullName,
            ["Role"] = u.Role,
            ["IsActive"] = u.IsActive,
            ["EmployeeEmail"] = u.Employee?.Email,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var query = db.AppUsers.Include(u => u.Employee).AsQueryable();
        var list = await query.ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
