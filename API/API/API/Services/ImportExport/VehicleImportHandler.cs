using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class VehicleImportHandler : EntityImportHandlerBase
{
    public VehicleImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "Vehicle";
    public override string DisplayName => "Phương tiện";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "LicensePlate", DisplayName = "Biển số", DataType = "string", IsRequired = true, Description = "Biển số xe" },
        new() { FieldName = "VehicleTypeName", DisplayName = "Loại xe", DataType = "string", Description = "Tên loại xe (tự động map)" },
        new() { FieldName = "EmployeeCode", DisplayName = "Mã nhân viên", DataType = "string", Description = "Email/SĐT nhân viên sở hữu" },
        new() { FieldName = "Description", DisplayName = "Mô tả", DataType = "string" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var plate = GetString(row, "LicensePlate");
        if (string.IsNullOrWhiteSpace(plate))
            errors.Add(MakeError(rowIndex, "LicensePlate", "Biển số không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(plate) && await db.Vehicles.AnyAsync(v => v.LicensePlate == plate))
            errors.Add(MakeError(rowIndex, "LicensePlate", $"Biển số '{plate}' đã tồn tại"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var typeName = GetString(row, "VehicleTypeName");
        int? typeId = null;
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            var vt = await db.VehicleTypes.FirstOrDefaultAsync(t => t.TypeName == typeName);
            if (vt == null)
            {
                vt = new VehicleType { TypeName = typeName };
                db.VehicleTypes.Add(vt);
                await db.SaveChangesAsync();
            }
            typeId = vt.VehicleTypeId;
        }

        var empCode = GetString(row, "EmployeeCode");
        int? empId = null;
        if (!string.IsNullOrWhiteSpace(empCode))
        {
            var emp = await db.Employees.FirstOrDefaultAsync(e => e.Email == empCode || e.Phone == empCode);
            if (emp != null) empId = emp.EmployeeId;
        }

        var vehicle = new Vehicle
        {
            LicensePlate = GetString(row, "LicensePlate") ?? "",
            VehicleTypeId = typeId,
            EmployeeId = empId,
            Description = GetString(row, "Description"),
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var v = (Vehicle)entity;
        return new Dictionary<string, object?>
        {
            ["VehicleId"] = v.VehicleId,
            ["LicensePlate"] = v.LicensePlate,
            ["VehicleTypeName"] = v.VehicleType?.TypeName,
            ["EmployeeCode"] = v.Employee?.Email,
            ["Description"] = v.Description,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var query = db.Vehicles
            .Include(v => v.VehicleType)
            .Include(v => v.Employee)
            .AsQueryable();
        var list = await query.ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
