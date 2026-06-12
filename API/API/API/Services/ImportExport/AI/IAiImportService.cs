using API.DTOs;

namespace API.Services.ImportExport.AI;

public interface IAiImportService
{
    Task<FileAnalysisResult> AnalyzeFileAsync(string entityType, Stream fileStream, string fileName, string? contentType);
    Task<AiProcessingResult> ProcessOcrAsync(string entityType, Stream fileStream, string fileName, CancellationToken ct = default);
    Task<AiImportPreviewResponse> NormalizeAndPreviewAsync(string entityType, Guid sessionId, CancellationToken ct = default);
    Task<ImportResponse> ConfirmImportAsync(string entityType, Guid sessionId, AiImportRequest request, int performedByUserId, CancellationToken ct = default);
    Task<AiImportPreviewResponse?> GetPreviewAsync(Guid sessionId);
    AiSession CreateSession(string entityType, string fileName, string fileFormat, byte[]? fileContent);
}
