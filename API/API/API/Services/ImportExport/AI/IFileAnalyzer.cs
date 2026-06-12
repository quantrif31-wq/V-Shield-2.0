namespace API.Services.ImportExport.AI;

public interface IFileAnalyzer
{
    Task<FileAnalysisResult> AnalyzeAsync(Stream fileStream, string fileName, string? contentType);
    bool IsAISuggestedFormat(string format);
    bool NeedsOcr(string format);
}
