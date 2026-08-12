using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class AccessLogImportHandler : EntityImportHandlerBase
{
    public AccessLogImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "AccessLog";
    public override string DisplayName => "Lịch sử ra vào";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "LogId", DisplayName = "Mã log", DataType = "int", Description = "Mã nhật ký" },
        new() { FieldName = "Timestamp", DisplayName = "Thời gian", DataType = "datetime", Description = "Thời điểm sự kiện" },
        new() { FieldName = "ActorName", DisplayName = "Đối tượng", DataType = "string", Description = "Tên nhân viên/khách" },
        new() { FieldName = "ActorType", DisplayName = "Loại đối tượng", DataType = "string", Description = "Employee / Guest / Vehicle / Unknown" },
        new() { FieldName = "Direction", DisplayName = "Chiều", DataType = "string", Description = "In / Out" },
        new() { FieldName = "GateName", DisplayName = "Tên cổng", DataType = "string", Description = "Cổng ra vào" },
        new() { FieldName = "CameraName", DisplayName = "Tên camera", DataType = "string", Description = "Camera ghi nhận" },
        new() { FieldName = "CapturedLicensePlate", DisplayName = "Biển số", DataType = "string", Description = "Biển số nhận diện" },
        new() { FieldName = "Method", DisplayName = "Phương thức", DataType = "string", Description = "QR / Plate / Face / Manual" },
        new() { FieldName = "ResultStatus", DisplayName = "Kết quả", DataType = "string", Description = "GrantedAccess / DeniedAccess / Bypass..." },
        new() { FieldName = "Note", DisplayName = "Ghi chú", DataType = "string", Description = "Ghi chú" },
    ];

    public override Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        return Task.FromResult(new List<ImportErrorDetail>
        {
            MakeError(rowIndex, null, "AccessLog chỉ hỗ trợ xuất dữ liệu, không hỗ trợ nhập", "IMPORT_NOT_SUPPORTED"),
        });
    }

    public override Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        throw new NotSupportedException("AccessLog chỉ hỗ trợ xuất dữ liệu, không hỗ trợ nhập");
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var log = (AccessLog)entity;
        string actorName = log.Employee?.FullName ?? log.Registration?.Guest?.FullName ?? log.CapturedLicensePlate ?? "";
        string actorType = log.EmployeeId != null ? "Employee"
            : log.RegistrationId != null ? "Guest"
            : !string.IsNullOrEmpty(log.CapturedLicensePlate) ? "Vehicle"
            : "Unknown";
        return new Dictionary<string, object?>
        {
            ["LogId"] = log.LogId,
            ["Timestamp"] = log.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss"),
            ["ActorName"] = actorName,
            ["ActorType"] = actorType,
            ["Direction"] = log.Direction,
            ["GateName"] = log.Gate?.GateName ?? log.GateNameSnapshot,
            ["CameraName"] = log.Camera?.CameraName ?? log.CameraNameSnapshot,
            ["CapturedLicensePlate"] = log.CapturedLicensePlate,
            ["Method"] = "QR",
            ["ResultStatus"] = log.ResultStatus,
            ["Note"] = log.Note,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var query = db.AccessLogs
            .Include(l => l.Employee)
            .Include(l => l.Registration).ThenInclude(r => r!.Guest)
            .Include(l => l.Gate)
            .Include(l => l.Camera)
            .AsNoTracking()
            .AsQueryable();

        if (request.Filters?.TryGetValue("status", out var status) == true && !string.IsNullOrWhiteSpace(status))
            query = query.Where(l => l.ResultStatus == status);
        if (request.Filters?.TryGetValue("direction", out var dir) == true && !string.IsNullOrWhiteSpace(dir))
            query = query.Where(l => l.Direction == dir);
        if (request.Filters?.TryGetValue("from", out var from) == true && DateTime.TryParse(from, out var fromDate))
            query = query.Where(l => l.Timestamp >= fromDate);
        if (request.Filters?.TryGetValue("to", out var to) == true && DateTime.TryParse(to, out var toDate))
            query = query.Where(l => l.Timestamp <= toDate);

        var list = await query.OrderByDescending(l => l.Timestamp).Take(20000).ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
