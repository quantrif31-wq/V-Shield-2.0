using System.Globalization;
using API.Data;
using API.DTOs;

namespace API.Services.ImportExport;

public abstract class EntityImportHandlerBase : IEntityImportHandler
{
    protected readonly IServiceScopeFactory ScopeFactory;

    protected EntityImportHandlerBase(IServiceScopeFactory scopeFactory)
    {
        ScopeFactory = scopeFactory;
    }

    public abstract string EntityType { get; }
    public abstract string DisplayName { get; }
    public abstract List<TemplateFieldInfo> GetTemplateFields();
    public abstract Task<List<ImportErrorDetail>> ValidateRowAsync(Dictionary<string, object?> row, int rowIndex, ImportValidationContext context);
    public abstract Task<object?> CreateEntityAsync(Dictionary<string, object?> row, ImportValidationContext context);
    public virtual Task<object?> UpdateEntityAsync(Dictionary<string, object?> row, object existingEntity, ImportValidationContext context) => Task.FromResult<object?>(existingEntity);
    public abstract Task<Dictionary<string, object?>> EntityToDictionaryAsync(object entity);
    public abstract Task<List<Dictionary<string, object?>>> ExportDataAsync(ExportRequest request);
    public virtual Func<object, bool>? GetExportFilter(ExportRequest request) => null;

    protected async Task<ApplicationDbContext> CreateDbContextAsync()
    {
        var scope = ScopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    protected static string? GetString(Dictionary<string, object?> row, string key)
    {
        if (row.TryGetValue(key, out var val) && val != null)
            return val.ToString()?.Trim();
        return null;
    }

    protected static int? GetInt(Dictionary<string, object?> row, string key)
    {
        var val = GetString(row, key);
        if (string.IsNullOrEmpty(val)) return null;
        if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    protected static bool? GetBool(Dictionary<string, object?> row, string key)
    {
        var val = GetString(row, key);
        if (string.IsNullOrEmpty(val)) return null;
        if (bool.TryParse(val, out var b)) return b;
        if (val is "1" or "yes" or "Yes" or "YES") return true;
        if (val is "0" or "no" or "No" or "NO") return false;
        return null;
    }

    protected static decimal? GetDecimal(Dictionary<string, object?> row, string key)
    {
        var val = GetString(row, key);
        if (string.IsNullOrEmpty(val)) return null;
        if (decimal.TryParse(val, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    protected static DateTime? GetDateTime(Dictionary<string, object?> row, string key)
    {
        var val = GetString(row, key);
        if (string.IsNullOrEmpty(val)) return null;
        if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result;
        return null;
    }

    protected static TimeSpan? GetTimeSpan(Dictionary<string, object?> row, string key)
    {
        var val = GetString(row, key);
        if (string.IsNullOrEmpty(val)) return null;
        if (TimeSpan.TryParse(val, CultureInfo.InvariantCulture, out var result))
            return result;
        if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.TimeOfDay;
        return null;
    }

    protected static ImportErrorDetail MakeError(int rowIndex, string? column, string message, string? errorCode = null)
    {
        return new ImportErrorDetail
        {
            Row = rowIndex,
            Column = column,
            Message = message,
            ErrorCode = errorCode,
        };
    }
}
