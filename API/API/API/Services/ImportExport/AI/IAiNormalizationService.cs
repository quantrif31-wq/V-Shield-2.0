namespace API.Services.ImportExport.AI;

public interface IAiNormalizationService
{
    Task<AiProcessingResult> NormalizeAsync(
        FileParseResult data,
        IEntityImportHandler handler,
        List<SynonymIssue> detectedIssues,
        CancellationToken ct = default);

    Task<FileParseResult> ParseOcrTextAsync(
        string rawText,
        IEntityImportHandler handler,
        CancellationToken ct = default);

    bool IsAvailable();
}
