using API.DTOs;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.ImportExport;

public class UserImportHandler : EntityImportHandlerBase
{
    public UserImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "AppUser";
    public override string DisplayName => "Nguoi dung";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "Username", DisplayName = "Ten dang nhap", DataType = "string", IsRequired = true, Description = "Ten tai khoan" },
        new() { FieldName = "FullName", DisplayName = "Ho ten", DataType = "string", Description = "Ten day du" },
        new() { FieldName = "Role", DisplayName = "Vai tro", DataType = "string", Description = "Admin / Staff / BaoVe / QuanLy / LeTan", AllowedValues = ["Admin", "Staff", "BaoVe", "QuanLy", "LeTan"] },
        new() { FieldName = "IsActive", DisplayName = "Kich hoat", DataType = "bool", Description = "true = Kich hoat, false = Vo hieu" },
        new() { FieldName = "EmployeeEmail", DisplayName = "Email nhan vien", DataType = "string", Description = "Gan voi nhan vien qua Email" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var username = GetString(row, "Username");
        if (string.IsNullOrWhiteSpace(username))
            errors.Add(MakeError(rowIndex, "Username", "Ten dang nhap khong duoc de trong"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(username) && await db.AppUsers.AnyAsync(u => u.Username == username))
            errors.Add(MakeError(rowIndex, "Username", $"Ten dang nhap '{username}' da ton tai"));

        var role = GetString(row, "Role");
        if (!string.IsNullOrWhiteSpace(role) && role is not ("Admin" or "Staff" or "BaoVe" or "QuanLy" or "LeTan"))
            errors.Add(MakeError(rowIndex, "Role", $"Vai tro khong hop le: '{role}'. Chap nhan: Admin, Staff, BaoVe, QuanLy, LeTan"));

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
            if (emp != null)
                empId = emp.EmployeeId;
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

    public override Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var user = (AppUser)entity;
        return Task.FromResult(new Dictionary<string, object?>
        {
            ["UserId"] = user.UserId,
            ["Username"] = user.Username,
            ["FullName"] = user.FullName,
            ["Role"] = user.Role,
            ["IsActive"] = user.IsActive,
            ["EmployeeEmail"] = user.Employee?.Email,
        });
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var users = await db.AppUsers.Include(u => u.Employee).ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var user in users)
            result.Add(await EntityToDictionaryAsync(user));
        return result;
    }
}
