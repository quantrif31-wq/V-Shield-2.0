using System.Collections.Concurrent;
using API.DTOs;

namespace API.Services.ImportExport.AI;

public class AiImportService : IAiImportService
{
    private static readonly ConcurrentDictionary<Guid, AiSession> Sessions = new();

    private readonly IFileAnalyzer _fileAnalyzer;
    private readonly IOcrService _ocrService;
    private readonly IAiNormalizationService _aiService;
    private readonly IImportExportService _importExportService;
    private readonly FileParserFactory _parserFactory;
    private readonly Dictionary<string, IEntityImportHandler> _handlers;
    private readonly Validation.SynonymDetector _synonymDetector;
    private readonly Validation.IStructureValidator _structureValidator;

    public AiImportService(
        IFileAnalyzer fileAnalyzer,
        IOcrService ocrService,
        IAiNormalizationService aiService,
        IImportExportService importExportService,
        FileParserFactory parserFactory,
        IEnumerable<IEntityImportHandler> handlers,
        Validation.SynonymDetector synonymDetector,
        Validation.IStructureValidator structureValidator)
    {
        _fileAnalyzer = fileAnalyzer;
        _ocrService = ocrService;
        _aiService = aiService;
        _importExportService = importExportService;
        _parserFactory = parserFactory;
        _handlers = handlers.ToDictionary(h => h.EntityType, StringComparer.OrdinalIgnoreCase);
        _synonymDetector = synonymDetector;
        _structureValidator = structureValidator;
    }

    public AiSession CreateSession(string entityType, string fileName, string fileFormat, byte[]? fileContent)
    {
        var session = new AiSession
        {
            EntityType = entityType,
            FileName = fileName,
            FileFormat = fileFormat,
            OriginalFileContent = fileContent,
        };
        Sessions[session.SessionId] = session;
        return session;
    }

    public async Task<FileAnalysisResult> AnalyzeFileAsync(string entityType, Stream fileStream, string fileName, string? contentType)
    {
        return await _fileAnalyzer.AnalyzeAsync(fileStream, fileName, contentType);
    }

    public async Task<AiProcessingResult> ProcessOcrAsync(string entityType, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(entityType, out var handler))
            throw new NotSupportedException($"Entity type '{entityType}' not supported");

        var ocrResult = await _ocrService.ExtractTextAsync(fileStream, fileName, ct);
        if (!ocrResult.Success)
        {
            return new AiProcessingResult
            {
                Status = "failed",
                ErrorMessage = ocrResult.ErrorMessage ?? "OCR extraction failed",
            };
        }

        var parsedData = await _aiService.ParseOcrTextAsync(ocrResult.RawText, handler, ct);

        var analysis = new FileAnalysisResult
        {
            IsReadable = true,
            DetectedFormat = "ai_ocr",
            SuggestedAction = "normalize",
            ParsedData = parsedData,
        };

        var session = CreateSession(entityType, fileName, "ai_ocr", null);
        session.ParsedData = parsedData;
        session.AiWasUsed = true;
        session.Validation = _structureValidator.Validate(parsedData, handler);

        var issues = _synonymDetector.DetectIssues(parsedData, handler.GetTemplateFields());

        return new AiProcessingResult
        {
            Status = issues.Count > 0 ? "needs_normalization" : "ready",
            NormalizedData = parsedData,
            Changes = issues.Select(i => new SynonymChangeLog
            {
                Row = i.Row, Column = i.Column,
                OriginalValue = i.OriginalValue, NormalizedValue = i.SuggestedValue,
                Reason = i.Category,
            }).ToList(),
        };
    }

    public async Task<AiImportPreviewResponse> NormalizeAndPreviewAsync(string entityType, Guid sessionId, CancellationToken ct = default)
    {
        if (!Sessions.TryGetValue(sessionId, out var session))
            throw new KeyNotFoundException("Session not found");

        if (!_handlers.TryGetValue(entityType, out var handler))
            throw new NotSupportedException($"Entity type '{entityType}' not supported");

        var data = session.ParsedData ?? session.NormalizedData;
        if (data == null)
            throw new InvalidOperationException("No data to normalize");

        var issues = _synonymDetector.DetectIssues(data, handler.GetTemplateFields());

        AiProcessingResult normalizeResult;
        if (issues.Count > 0 && _aiService.IsAvailable())
        {
            normalizeResult = await _aiService.NormalizeAsync(data, handler, issues, ct);
        }
        else if (issues.Count > 0)
        {
            normalizeResult = await new AiNormalizationService(
                new Validation.SynonymRegistry(),
                null!,
                new ConfigurationBuilder().Build(),
                null!)!
                .NormalizeAsync(data, handler, issues, ct);
        }
        else
        {
            normalizeResult = new AiProcessingResult
            {
                Status = "success",
                NormalizedData = data,
                Changes = [],
            };
        }

        session.NormalizedData = normalizeResult.NormalizedData;
        session.Changes = normalizeResult.Changes;
        session.Validation = _structureValidator.Validate(normalizeResult.NormalizedData, handler);

        var validation = session.Validation;

        return new AiImportPreviewResponse
        {
            SessionId = sessionId,
            PreviewData = normalizeResult.NormalizedData,
            Changes = normalizeResult.Changes,
            Validation = validation,
            ReadyForImport = validation.IsValid,
            ChangeCount = normalizeResult.Changes.Count,
            TotalRows = normalizeResult.NormalizedData.TotalRows,
        };
    }

    public async Task<ImportResponse> ConfirmImportAsync(
        string entityType, Guid sessionId, AiImportRequest request,
        int performedByUserId, CancellationToken ct = default)
    {
        if (!Sessions.TryGetValue(sessionId, out var session))
            throw new KeyNotFoundException("Session not found");

        var data = session.NormalizedData ?? session.ParsedData;
        if (data == null)
            throw new InvalidOperationException("No data to import");

        var jsonBytes = System.Text.Json.JsonSerializer.Serialize(data.Rows);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonBytes));

        var result = await _importExportService.ImportAsync(
            entityType, stream, $"ai_import_{session.FileName}", "application/json",
            new DTOs.ImportRequest { SkipDuplicates = true },
            performedByUserId, ct);

        Sessions.TryRemove(sessionId, out _);
        return result;
    }

    public Task<AiImportPreviewResponse?> GetPreviewAsync(Guid sessionId)
    {
        if (Sessions.TryGetValue(sessionId, out var session) && session.Validation != null)
        {
            return Task.FromResult<AiImportPreviewResponse?>(new AiImportPreviewResponse
            {
                SessionId = sessionId,
                PreviewData = session.NormalizedData ?? session.ParsedData ?? new FileParseResult(),
                Changes = session.Changes,
                Validation = session.Validation,
                ReadyForImport = session.Validation.IsValid,
                ChangeCount = session.Changes.Count,
                TotalRows = (session.NormalizedData ?? session.ParsedData)?.TotalRows ?? 0,
            });
        }
        return Task.FromResult<AiImportPreviewResponse?>(null);
    }
}
