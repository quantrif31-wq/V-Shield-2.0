using Microsoft.EntityFrameworkCore;
using API.DTOs;
using API.Models;

namespace API.Services.ImportExport;

public class CameraImportHandler : EntityImportHandlerBase
{
    public CameraImportHandler(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    public override string EntityType => "Camera";
    public override string DisplayName => "Camera";

    public override List<TemplateFieldInfo> GetTemplateFields() =>
    [
        new() { FieldName = "CameraName", DisplayName = "Tên camera", DataType = "string", IsRequired = true, Description = "Tên camera" },
        new() { FieldName = "GateName", DisplayName = "Tên cổng", DataType = "string", Description = "Tên cổng (tự động map)" },
        new() { FieldName = "CameraType", DisplayName = "Loại camera", DataType = "string", Description = "Ví dụ: entrance / exit / plate" },
        new() { FieldName = "StreamUrl", DisplayName = "URL stream", DataType = "string", Description = "RTSP hoặc HTTP stream" },
        new() { FieldName = "UrlView", DisplayName = "URL xem", DataType = "string", Description = "URL xem trực tiếp" },
        new() { FieldName = "IsRecordingEnabled", DisplayName = "Cho phép ghi hình", DataType = "bool", Description = "true/false" },
        new() { FieldName = "RecordingRetentionDays", DisplayName = "Số ngày lưu", DataType = "int", Description = "Số ngày lưu video, mặc định 30" },
    ];

    public override async Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context)
    {
        var errors = new List<ImportErrorDetail>();
        var name = GetString(row, "CameraName");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(MakeError(rowIndex, "CameraName", "Tên camera không được để trống"));

        await using var db = await CreateDbContextAsync();
        if (!string.IsNullOrWhiteSpace(name) && await db.Cameras.AnyAsync(c => c.CameraName == name))
            errors.Add(MakeError(rowIndex, "CameraName", $"Camera '{name}' đã tồn tại"));

        return errors;
    }

    public override async Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context)
    {
        await using var db = await CreateDbContextAsync();

        var gateName = GetString(row, "GateName");
        int? gateId = null;
        if (!string.IsNullOrWhiteSpace(gateName))
        {
            var gate = await db.Gates.FirstOrDefaultAsync(g => g.GateName == gateName);
            gateId = gate?.GateId;
        }

        var camera = new Camera
        {
            CameraName = GetString(row, "CameraName") ?? "",
            GateId = gateId,
            CameraType = GetString(row, "CameraType"),
            StreamUrl = GetString(row, "StreamUrl"),
            UrlView = GetString(row, "UrlView"),
            IsRecordingEnabled = GetBool(row, "IsRecordingEnabled") ?? true,
            RecordingRetentionDays = GetInt(row, "RecordingRetentionDays") ?? 30,
        };
        db.Cameras.Add(camera);
        await db.SaveChangesAsync();
        return camera;
    }

    public override async Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity)
    {
        var c = (Camera)entity;
        return new Dictionary<string, object?>
        {
            ["CameraId"] = c.CameraId,
            ["CameraName"] = c.CameraName,
            ["GateName"] = c.Gate?.GateName,
            ["CameraType"] = c.CameraType,
            ["StreamUrl"] = c.StreamUrl,
            ["UrlView"] = c.UrlView,
            ["IsRecordingEnabled"] = c.IsRecordingEnabled,
            ["RecordingRetentionDays"] = c.RecordingRetentionDays,
        };
    }

    public override async Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request)
    {
        await using var db = await CreateDbContextAsync();
        var list = await db.Cameras.Include(c => c.Gate).AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in list)
            result.Add(await EntityToDictionaryAsync(item));
        return result;
    }
}
