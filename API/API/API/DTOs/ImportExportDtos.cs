using System.Text.Json;

namespace API.DTOs;

public sealed record ImportRequest
{
    public string? EntityType { get; init; }
    public bool SkipDuplicates { get; init; } = true;
    public bool UpdateExisting { get; init; } = false;
    public string? AdditionalOptions { get; init; }
}

public sealed record ImportResponse
{
    public Guid HistoryId { get; init; }
    public Guid? JobId { get; init; }
    public string Status { get; init; } = "Pending";
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public List<ImportErrorDetail>? Errors { get; init; }
    public List<ImportWarningDetail>? Warnings { get; init; }
    public bool HasErrors => ErrorCount > 0;
    public bool HasWarnings => WarningCount > 0;
}

public sealed record ImportErrorDetail
{
    public int Row { get; init; }
    public string? Column { get; init; }
    public string? Value { get; init; }
    public string Message { get; init; } = null!;
    public string? ErrorCode { get; init; }
}

public sealed record ImportWarningDetail
{
    public int Row { get; init; }
    public string? Column { get; init; }
    public string Message { get; init; } = null!;
}

public sealed record ExportRequest
{
    public string? EntityType { get; init; }
    public string Format { get; init; } = "csv";
    public List<string>? Columns { get; init; }
    public Dictionary<string, string>? Filters { get; init; }
    public bool IncludeHeaders { get; init; } = true;
}

public sealed record ExportResponse
{
    public Guid HistoryId { get; init; }
    public Guid? JobId { get; init; }
    public string Status { get; init; } = "Pending";
    public string FileName { get; init; } = null!;
    public string FileFormat { get; init; } = null!;
    public long FileSize { get; init; }
    public string? DownloadUrl { get; init; }
    public int TotalRows { get; init; }
}

public sealed record ImportExportHistoryResponse
{
    public Guid Id { get; init; }
    public string OperationType { get; init; } = null!;
    public string EntityType { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string FileFormat { get; init; } = null!;
    public long FileSize { get; init; }
    public string Status { get; init; } = null!;
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public DateTime PerformedAt { get; init; }
    public long? DurationMs { get; init; }
    public string? PerformedByName { get; init; }
}

public sealed record ImportExportFormatInfo
{
    public string Format { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string Extension { get; init; } = null!;
    public string MimeType { get; init; } = null!;
    public bool SupportsImport { get; init; }
    public bool SupportsExport { get; init; }
}

public sealed record EntityImportTemplateInfo
{
    public string EntityType { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public List<TemplateFieldInfo> Fields { get; init; } = [];
}

public sealed record TemplateFieldInfo
{
    public string FieldName { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string DataType { get; init; } = null!;
    public bool IsRequired { get; init; }
    public string? Description { get; init; }
    public List<string>? AllowedValues { get; init; }
    public string? ForeignKeyEntity { get; init; }
}

public sealed record ImportJobStatusResponse
{
    public Guid JobId { get; init; }
    public string Status { get; init; } = null!;
    public int ProgressPercent { get; init; }
    public int TotalRows { get; init; }
    public int ProcessedRows { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public List<ImportErrorDetail>? Errors { get; init; }
}

public static class ImportExportConstants
{
    public const string OperationImport = "Import";
    public const string OperationExport = "Export";
    public const string StatusPending = "Pending";
    public const string StatusProcessing = "Processing";
    public const string StatusCompleted = "Completed";
    public const string StatusFailed = "Failed";
    public const string StatusPartialSuccess = "PartialSuccess";
    public const string FormatCsv = "csv";
    public const string FormatExcel = "xlsx";
    public const string FormatJson = "json";
    public const string FormatXml = "xml";

    public static readonly List<ImportExportFormatInfo> SupportedFormats =
    [
        new() { Format = FormatCsv, DisplayName = "CSV", Extension = ".csv", MimeType = "text/csv", SupportsImport = true, SupportsExport = true },
        new() { Format = FormatExcel, DisplayName = "Excel", Extension = ".xlsx", MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", SupportsImport = true, SupportsExport = true },
        new() { Format = FormatJson, DisplayName = "JSON", Extension = ".json", MimeType = "application/json", SupportsImport = true, SupportsExport = true },
        new() { Format = FormatXml, DisplayName = "XML", Extension = ".xml", MimeType = "application/xml", SupportsImport = true, SupportsExport = true },
    ];
}
