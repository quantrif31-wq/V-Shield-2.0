namespace API.Services.ImportExport.AI;

public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    bool CanHandle(string fileName);
}

public class OcrResult
{
    public bool Success { get; set; }
    public string RawText { get; set; } = null!;
    public string? ErrorMessage { get; set; }
    public List<OcrPageResult> Pages { get; set; } = [];
}

public class OcrPageResult
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = null!;
    public double Confidence { get; set; }
}
