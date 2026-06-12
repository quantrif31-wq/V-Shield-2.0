using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class EmployeeImportHandler : EntityImportHandlerBase
{
    public EmployeeImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "Employee";
    public override string DisplayName => "Nhân viên";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "FullName", DisplayName = "Họ và tên", DataType = "string", IsRequired = true, Description = "Tên đầy đủ của nhân viên" },
        new() { FieldName = "Email", DisplayName = "Email", DataType = "string", Description = "Địa chỉ email" },
        new() { FieldName = "Phone", DisplayName = "Số điện thoại", DataType = "string", Description = "Số điện thoại liên hệ" },
        new() { FieldName = "DepartmentName", DisplayName = "Phòng ban", DataType = "string", Description = "Tên phòng ban (tự động map)" },
        new() { FieldName = "PositionName", DisplayName = "Chức vụ", DataType = "string", Description = "Tên chức vụ (tự động map)" },
        new() { FieldName = "Status", DisplayName = "Trạng thái", DataType = "bool", Description = "true = Đang hoạt động, false = Ngừng HĐ" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var fullName = GetString(row, "FullName");
        if (string.IsNullOrWhiteSpace(fullName))
            errors.Add(MakeError(rowIndex, "FullName", "Họ và tên không được để trống"));
        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var fullName = GetString(row, "FullName") ?? "";
        var deptName = GetString(row, "DepartmentName");
        var posName = GetString(row, "PositionName");

        int? deptId = null;
        if (!string.IsNullOrWhiteSpace(deptName))
        {
            var dept = await db.Departments.FirstOrDefaultAsync(d => d.Name == deptName);
            if (dept == null)
            {
                dept = new Department { Name = deptName };
                db.Departments.Add(dept);
                await db.SaveChangesAsync();
            }
            deptId = dept.DepartmentId;
        }

        int? posId = null;
        if (!string.IsNullOrWhiteSpace(posName))
        {
            var pos = await db.Positions.FirstOrDefaultAsync(p => p.Name == posName);
            if (pos == null)
            {
                pos = new Position { Name = posName };
                db.Positions.Add(pos);
                await db.SaveChangesAsync();
            }
            posId = pos.PositionId;
        }

        var employee = new Employee
        {
            FullName = fullName,
            Email = GetString(row, "Email"),
            Phone = GetString(row, "Phone"),
            DepartmentId = deptId,
            PositionId = posId,
            Status = GetBool(row, "Status") ?? true,
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var emp = (Employee)entity;
        return new Dictionary<string, object?>
        {
            ["EmployeeId"] = emp.EmployeeId,
            ["FullName"] = emp.FullName,
            ["Email"] = emp.Email,
            ["Phone"] = emp.Phone,
            ["DepartmentName"] = emp.Department?.Name,
            ["PositionName"] = emp.Position?.Name,
            ["Status"] = emp.Status,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var query = db.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsQueryable();

        if (request.Filters?.TryGetValue("status", out var statusVal) == true && bool.TryParse(statusVal, out var status))
            query = query.Where(e => e.Status == status);

        if (request.Filters?.TryGetValue("departmentName", out var deptFilter) == true && !string.IsNullOrWhiteSpace(deptFilter))
            query = query.Where(e => e.Department != null && e.Department.Name.Contains(deptFilter));

        var employees = await query.ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var emp in employees)
            result.Add(await EntityToDictionaryAsync(emp));
        return result;
    }
}
