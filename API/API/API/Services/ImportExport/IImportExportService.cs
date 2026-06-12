using API.DTOs;

namespace API.Services.ImportExport;

public interface IImportExportService
{
    Task<ImportResponse> ImportAsync(
        string entityType,
        Stream fileStream,
        string fileName,
        string? contentType,
        ImportRequest? options,
        int performedByUserId,
        CancellationToken ct = default);

    Task<ExportResponse> ExportAsync(
        string entityType,
        ExportRequest request,
        int performedByUserId,
        CancellationToken ct = default);

    Task<Stream> DownloadTemplateAsync(string entityType, string format);

    Task<List<ImportExportHistoryResponse>> GetHistoryAsync(string? entityType = null, string? operationType = null, int page = 1, int pageSize = 20);

    Task<ImportExportHistoryResponse?> GetHistoryByIdAsync(Guid id);

    Task<ImportJobStatusResponse?> GetJobStatusAsync(Guid jobId);

    Task<ImportResponse> PreviewImportAsync(
        string entityType,
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken ct = default);

    List<ImportExportFormatInfo> GetSupportedFormats();

    List<EntityImportTemplateInfo> GetSupportedEntities();
}
