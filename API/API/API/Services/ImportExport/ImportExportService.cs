using System.Collections.Concurrent;
using System.Diagnostics;
using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.ImportExport;

public class ImportExportService : IImportExportService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FileParserFactory _parserFactory;
    private readonly Dictionary<string, IEntityImportHandler> _handlers;
    private static readonly ConcurrentDictionary<Guid, ImportJobStatus> Jobs = new();

    public ImportExportService(
        IServiceScopeFactory scopeFactory,
        FileParserFactory parserFactory,
        IEnumerable<IEntityImportHandler> handlers)
    {
        _scopeFactory = scopeFactory;
        _parserFactory = parserFactory;
        _handlers = handlers.ToDictionary(h => h.EntityType, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<ApplicationDbContext> CreateDbContextAsync()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public List<ImportExportFormatInfo> GetSupportedFormats() => ImportExportConstants.SupportedFormats;

    public List<EntityImportTemplateInfo> GetSupportedEntities()
    {
        return _handlers.Values.Select(h => new EntityImportTemplateInfo
        {
            EntityType = h.EntityType,
            DisplayName = h.DisplayName,
            Fields = h.GetTemplateFields(),
        }).ToList();
    }

    public async Task<ImportResponse> ImportAsync(
        string entityType,
        Stream fileStream,
        string fileName,
        string? contentType,
        ImportRequest? options,
        int performedByUserId,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var format = _parserFactory.DetectFormat(fileName, contentType);
        var parser = _parserFactory.GetParser(format);

        if (!_handlers.TryGetValue(entityType, out var handler))
            throw new NotSupportedException($"Entity type '{entityType}' is not supported");

        var historyId = Guid.NewGuid();
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, ct);
        var fileBytes = memoryStream.ToArray();

        var parseResult = await parser.ParseAsync(new MemoryStream(fileBytes), new FileParseOptions());

        var context = new ImportValidationContext
        {
            SkipDuplicates = options?.SkipDuplicates ?? true,
            UpdateExisting = options?.UpdateExisting ?? false,
        };

        var allErrors = new List<ImportErrorDetail>();
        var allWarnings = new List<ImportWarningDetail>();
        var successCount = 0;

        for (int i = 0; i < parseResult.Rows.Count; i++)
        {
            var row = parseResult.Rows[i];
            var rowNum = i + 1;
            var rowErrors = await handler.ValidateRowAsync(row, rowNum, context);
            allErrors.AddRange(rowErrors);

            if (rowErrors.Count > 0) continue;

            try
            {
                await handler.CreateEntityAsync(row, context);
                successCount++;
            }
            catch (Exception ex)
            {
                allErrors.Add(new ImportErrorDetail
                {
                    Row = rowNum,
                    Column = null,
                    Message = $"Lỗi xử lý: {ex.Message}",
                    ErrorCode = "CREATE_FAILED",
                });
            }
        }

        sw.Stop();
        var status = allErrors.Count == 0
            ? ImportExportConstants.StatusCompleted
            : successCount > 0
                ? ImportExportConstants.StatusPartialSuccess
                : ImportExportConstants.StatusFailed;

        var errorJson = System.Text.Json.JsonSerializer.Serialize(allErrors.Take(100).ToList());

        await using var db = await CreateDbContextAsync();

        var history = new ImportExportHistory
        {
            Id = historyId,
            OperationType = ImportExportConstants.OperationImport,
            EntityType = entityType,
            FileName = fileName,
            FileFormat = format,
            FileSize = fileBytes.Length,
            Status = status,
            TotalRows = parseResult.TotalRows,
            SuccessCount = successCount,
            ErrorCount = allErrors.Count,
            WarningCount = allWarnings.Count,
            ErrorDetails = errorJson,
            OriginalFileContent = fileBytes,
            PerformedById = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            DurationMs = sw.ElapsedMilliseconds,
        };

        db.ImportExportHistories.Add(history);
        await db.SaveChangesAsync(ct);

        return new ImportResponse
        {
            HistoryId = historyId,
            Status = status,
            TotalRows = parseResult.TotalRows,
            SuccessCount = successCount,
            ErrorCount = allErrors.Count,
            WarningCount = allWarnings.Count,
            Errors = allErrors.Take(100).ToList(),
            Warnings = allWarnings.Take(100).ToList(),
        };
    }

    public async Task<ExportResponse> ExportAsync(
        string entityType,
        ExportRequest request,
        int performedByUserId,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        if (!_handlers.TryGetValue(entityType, out var handler))
            throw new NotSupportedException($"Entity type '{entityType}' is not supported");

        var parser = _parserFactory.GetParser(request.Format);

        var data = await handler.ExportDataAsync(request);
        var columns = request.Columns ?? (data.Count > 0 ? data[0].Keys.ToList() : []);

        var serializeOptions = new FileSerializeOptions
        {
            IncludeHeaders = request.IncludeHeaders,
            Columns = columns,
        };

        var resultStream = await parser.SerializeAsync(data, serializeOptions);
        var fileBytes = ((MemoryStream)resultStream).ToArray();

        sw.Stop();

        var historyId = Guid.NewGuid();
        await using var db = await CreateDbContextAsync();

        var history = new ImportExportHistory
        {
            Id = historyId,
            OperationType = ImportExportConstants.OperationExport,
            EntityType = entityType,
            FileName = $"export_{entityType}_{DateTime.UtcNow:yyyyMMddHHmmss}.{request.Format}",
            FileFormat = request.Format,
            FileSize = fileBytes.Length,
            Status = ImportExportConstants.StatusCompleted,
            TotalRows = data.Count,
            SuccessCount = data.Count,
            ResultFileContent = fileBytes,
            PerformedById = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            DurationMs = sw.ElapsedMilliseconds,
        };

        db.ImportExportHistories.Add(history);
        await db.SaveChangesAsync(ct);

        return new ExportResponse
        {
            HistoryId = historyId,
            Status = ImportExportConstants.StatusCompleted,
            FileName = history.FileName,
            FileFormat = request.Format,
            FileSize = fileBytes.Length,
            DownloadUrl = $"/api/import-export/download/{historyId}",
            TotalRows = data.Count,
        };
    }

    public async Task<Stream> DownloadTemplateAsync(string entityType, string format)
    {
        if (!_handlers.TryGetValue(entityType, out var handler))
            throw new NotSupportedException($"Entity type '{entityType}' is not supported");

        var parser = _parserFactory.GetParser(format);

        var fields = handler.GetTemplateFields();
        var headers = fields.Select(f => f.FieldName).ToList();

        var sampleRow = new Dictionary<string, object?>();
        foreach (var field in fields)
        {
            sampleRow[field.FieldName] = field.DataType switch
            {
                "bool" => true,
                "int" => 0,
                _ => field.IsRequired ? $"(required)" : null,
            };
        }

        var data = new List<Dictionary<string, object?>> { sampleRow };

        var serializeOptions = new FileSerializeOptions
        {
            IncludeHeaders = true,
            Columns = headers,
        };

        return await parser.SerializeAsync(data, serializeOptions);
    }

    public async Task<List<ImportExportHistoryResponse>> GetHistoryAsync(
        string? entityType = null,
        string? operationType = null,
        int page = 1,
        int pageSize = 20)
    {
        await using var db = await CreateDbContextAsync();

        var query = db.ImportExportHistories
            .Include(h => h.PerformedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(h => h.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(operationType))
            query = query.Where(h => h.OperationType == operationType);

        var list = await query
            .OrderByDescending(h => h.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return list.Select(h => new ImportExportHistoryResponse
        {
            Id = h.Id,
            OperationType = h.OperationType,
            EntityType = h.EntityType,
            FileName = h.FileName,
            FileFormat = h.FileFormat,
            FileSize = h.FileSize,
            Status = h.Status,
            TotalRows = h.TotalRows,
            SuccessCount = h.SuccessCount,
            ErrorCount = h.ErrorCount,
            WarningCount = h.WarningCount,
            PerformedAt = h.PerformedAt,
            DurationMs = h.DurationMs,
            PerformedByName = h.PerformedBy?.FullName ?? h.PerformedBy?.Username,
        }).ToList();
    }

    public async Task<ImportExportHistoryResponse?> GetHistoryByIdAsync(Guid id)
    {
        await using var db = await CreateDbContextAsync();
        var h = await db.ImportExportHistories
            .Include(x => x.PerformedBy)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (h == null) return null;

        return new ImportExportHistoryResponse
        {
            Id = h.Id,
            OperationType = h.OperationType,
            EntityType = h.EntityType,
            FileName = h.FileName,
            FileFormat = h.FileFormat,
            FileSize = h.FileSize,
            Status = h.Status,
            TotalRows = h.TotalRows,
            SuccessCount = h.SuccessCount,
            ErrorCount = h.ErrorCount,
            WarningCount = h.WarningCount,
            PerformedAt = h.PerformedAt,
            DurationMs = h.DurationMs,
            PerformedByName = h.PerformedBy?.FullName ?? h.PerformedBy?.Username,
        };
    }

    public async Task<ImportResponse> PreviewImportAsync(
        string entityType,
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken ct = default)
    {
        var format = _parserFactory.DetectFormat(fileName, contentType);
        var parser = _parserFactory.GetParser(format);

        if (!_handlers.TryGetValue(entityType, out var handler))
            throw new NotSupportedException($"Entity type '{entityType}' is not supported");

        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        var parseResult = await parser.ParseAsync(memoryStream, new FileParseOptions { MaxRows = 20 });

        var context = new ImportValidationContext
        {
            SkipDuplicates = true,
            UpdateExisting = false,
        };

        var errors = new List<ImportErrorDetail>();
        for (int i = 0; i < parseResult.Rows.Count; i++)
        {
            var rowErrors = await handler.ValidateRowAsync(parseResult.Rows[i], i + 1, context);
            errors.AddRange(rowErrors);
        }

        return new ImportResponse
        {
            Status = errors.Count == 0 ? "PreviewReady" : "PreviewWithErrors",
            TotalRows = parseResult.TotalRows,
            SuccessCount = 0,
            ErrorCount = errors.Count,
            Errors = errors.Take(100).ToList(),
        };
    }

    public Task<ImportJobStatusResponse?> GetJobStatusAsync(Guid jobId)
    {
        if (Jobs.TryGetValue(jobId, out var job))
        {
            return Task.FromResult<ImportJobStatusResponse?>(new ImportJobStatusResponse
            {
                JobId = jobId,
                Status = job.Status,
                ProgressPercent = job.TotalRows > 0 ? (job.ProcessedRows * 100 / job.TotalRows) : 0,
                TotalRows = job.TotalRows,
                ProcessedRows = job.ProcessedRows,
                SuccessCount = job.SuccessCount,
                ErrorCount = job.ErrorCount,
                Errors = job.Errors,
            });
        }

        return Task.FromResult<ImportJobStatusResponse?>(null);
    }

    internal static void TrackJob(Guid jobId, ImportJobStatus status) => Jobs[jobId] = status;
}

public class ImportJobStatus
{
    public string Status { get; set; } = "Pending";
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportErrorDetail>? Errors { get; set; }
}
