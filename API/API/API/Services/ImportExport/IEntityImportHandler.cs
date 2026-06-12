using API.DTOs;

namespace API.Services.ImportExport;

public interface IEntityImportHandler
{
    string EntityType { get; }
    string DisplayName { get; }
    List<TemplateFieldInfo> GetTemplateFields();
    Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context);
    Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context);
    Task<object?> UpdateEntityAsync(Dictionary<string, object?> row, object existingEntity, ImportValidationContext context);
    Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity);
    Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request);
    Func<object, bool>? GetExportFilter(ExportRequest request);
}

public sealed record ImportValidationContext
{
    public IServiceProvider ServiceProvider { get; init; } = null!;
    public bool SkipDuplicates { get; init; } = true;
    public bool UpdateExisting { get; init; }
    public List<ImportErrorDetail> Errors { get; init; } = new();
}
