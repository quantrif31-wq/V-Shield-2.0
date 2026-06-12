namespace API.Services.ImportExport.AI;

public class FileAnalyzer : IFileAnalyzer
{
    private readonly FileParserFactory _parserFactory;
    private static readonly HashSet<string> ReadableFormats = ["csv", "xlsx", "json", "xml", "tsv"];
    private static readonly HashSet<string> OcrFormats = ["pdf", "png", "jpg", "jpeg", "tiff", "tif", "bmp", "gif"];
    private static readonly HashSet<string> DocFormats = ["doc", "docx", "odt", "rtf"];

    private static readonly Dictionary<string, string> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["%PDF"] = "pdf",
        ["‰PNG"] = "png",
        ["ÿØÿà"] = "jpg",
        ["ÿØÿá"] = "jpg",
        ["ÿØÿÂ"] = "jpg",
        ["GIF8"] = "gif",
        ["II*"] = "tiff",
        ["MM\x00*"] = "tiff",
        ["BM"] = "bmp",
        ["PK"] = "zip_or_docx_or_xlsx",
    };

    public FileAnalyzer(FileParserFactory parserFactory)
    {
        _parserFactory = parserFactory;
    }

    public async Task<FileAnalysisResult> AnalyzeAsync(Stream fileStream, string fileName, string? contentType)
    {
        var result = new FileAnalysisResult();
        var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant() ?? "";
        var format = _parserFactory.DetectFormat(fileName, contentType);

        result.DetectedFormat = format;

        if (_parserFactory.IsFormatSupported(format))
        {
            fileStream.Position = 0;
            try
            {
                var parseResult = await _parserFactory.GetParser(format).ParseAsync(fileStream, new FileParseOptions { MaxRows = 100 });
                result.IsReadable = true;
                result.ParsedData = parseResult;
                result.SuggestedAction = "proceed";
                result.Warnings = parseResult.Warnings;
                return result;
            }
            catch
            {
            }
        }

        if (OcrFormats.Contains(ext) || OcrFormats.Contains(format))
        {
            result.IsReadable = false;
            result.SuggestedAction = "ocr";
            result.Message = $"File {ext.ToUpperInvariant()} không thể đọc trực tiếp. Bạn có muốn dùng AI OCR để trích xuất dữ liệu?";
            return result;
        }

        if (DocFormats.Contains(ext))
        {
            result.IsReadable = false;
            result.SuggestedAction = "ocr";
            result.Message = $"File {ext.ToUpperInvariant()} cần được trích xuất văn bản. Bạn có muốn dùng AI để xử lý?";
            return result;
        }

        result.IsReadable = false;
        result.SuggestedAction = "ai_assist";
        result.Message = $"Định dạng '{ext}' không được hỗ trợ trực tiếp. Bạn có muốn thử dùng AI để đọc file?";
        return result;
    }

    public bool IsAISuggestedFormat(string format)
    {
        var f = format.TrimStart('.').ToLowerInvariant();
        return OcrFormats.Contains(f) || DocFormats.Contains(f) || !_parserFactory.IsFormatSupported(f);
    }

    public bool NeedsOcr(string format)
    {
        var f = format.TrimStart('.').ToLowerInvariant();
        return OcrFormats.Contains(f);
    }
}
