using System.Security.Claims;
using API.DTOs;
using API.Middleware;
using API.Services.ImportExport;
using API.Services.ImportExport.AI;
using API.Services.ImportExport.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/import-export/{entityType}/ai")]
[Authorize]
[RequireOperationalTask("device-mgmt")]
public class AiImportController : ControllerBase
{
    private readonly IAiImportService _aiService;
    private readonly IFileAnalyzer _fileAnalyzer;
    private readonly IOcrService _ocrService;
    private readonly IAiNormalizationService _aiNormalization;
    private readonly SynonymRegistry _synonymRegistry;
    private readonly FileParserFactory _parserFactory;
    private readonly Dictionary<string, IEntityImportHandler> _handlers;

    public AiImportController(
        IAiImportService aiService,
        IFileAnalyzer fileAnalyzer,
        IOcrService ocrService,
        IAiNormalizationService aiNormalization,
        SynonymRegistry synonymRegistry,
        FileParserFactory parserFactory,
        IEnumerable<IEntityImportHandler> handlers)
    {
        _aiService = aiService;
        _fileAnalyzer = fileAnalyzer;
        _ocrService = ocrService;
        _aiNormalization = aiNormalization;
        _synonymRegistry = synonymRegistry;
        _parserFactory = parserFactory;
        _handlers = handlers.ToDictionary(h => h.EntityType, StringComparer.OrdinalIgnoreCase);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

    [HttpPost("analyze")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Analyze()
    {
        var entityType = RouteData.Values["entityType"]?.ToString() ?? "";
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file" });

        await using var stream = file.OpenReadStream();
        var analysis = await _fileAnalyzer.AnalyzeAsync(stream, file.FileName, file.ContentType);

        if (analysis.IsReadable && analysis.ParsedData != null)
        {
            var session = _aiService.CreateSession(entityType, file.FileName, analysis.DetectedFormat, null);
            session.ParsedData = analysis.ParsedData;

            return Ok(new
            {
                sessionId = session.SessionId,
                isReadable = true,
                suggestedAction = analysis.SuggestedAction,
                message = analysis.Message,
                detectedFormat = analysis.DetectedFormat,
                totalRows = analysis.ParsedData.TotalRows,
                headers = analysis.ParsedData.Headers,
                preview = analysis.ParsedData.Rows.Take(5),
            });
        }

        var ocrSession = _aiService.CreateSession(entityType, file.FileName, analysis.DetectedFormat, null);

        return Ok(new
        {
            sessionId = ocrSession.SessionId,
            isReadable = false,
            suggestedAction = analysis.SuggestedAction ?? "ocr",
            message = analysis.Message ?? $"File '{file.FileName}' không thể đọc trực tiếp",
            detectedFormat = analysis.DetectedFormat,
            aiAvailable = _aiNormalization.IsAvailable(),
            ocrAvailable = _ocrService.CanHandle(file.FileName),
        });
    }

    [HttpPost("ocr")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> ProcessOcr()
    {
        var entityType = RouteData.Values["entityType"]?.ToString() ?? "";
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file" });

        await using var stream = file.OpenReadStream();
        var result = await _aiService.ProcessOcrAsync(entityType, stream, file.FileName);

        if (result.Status == "failed")
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new
        {
            status = result.Status,
            totalRows = result.NormalizedData.TotalRows,
            headers = result.NormalizedData.Headers,
            changes = result.Changes,
            changeCount = result.Changes.Count,
            preview = result.NormalizedData.Rows.Take(5),
        });
    }

    [HttpPost("normalize")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Normalize([FromBody] NormalizeRequest request)
    {
        var entityType = RouteData.Values["entityType"]?.ToString() ?? "";
        var preview = await _aiService.NormalizeAndPreviewAsync(entityType, request.SessionId);

        return Ok(new
        {
            sessionId = preview.SessionId,
            readyForImport = preview.ReadyForImport,
            totalRows = preview.TotalRows,
            changeCount = preview.ChangeCount,
            changes = preview.Changes,
            validation = new
            {
                isValid = preview.Validation.IsValid,
                hasSynonymIssues = preview.Validation.HasSynonymIssues,
                hasStructuralIssues = preview.Validation.HasStructuralIssues,
                errorCount = preview.Validation.Errors.Count,
                errors = preview.Validation.Errors.Take(20),
                warnings = preview.Validation.Warnings,
            },
            preview = preview.PreviewData.Rows.Take(5),
        });
    }

    [HttpPost("confirm/{sessionId}")]
    public async Task<IActionResult> Confirm(Guid sessionId, [FromBody] AiImportRequest request)
    {
        var entityType = RouteData.Values["entityType"]?.ToString() ?? "";

        try
        {
            var result = await _aiService.ConfirmImportAsync(entityType, sessionId, request, GetUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Session không tồn tại hoặc đã hết hạn" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("preview/{sessionId}")]
    public async Task<IActionResult> GetPreview(Guid sessionId)
    {
        var preview = await _aiService.GetPreviewAsync(sessionId);
        if (preview == null)
            return NotFound(new { message = "Session không tồn tại" });

        return Ok(new
        {
            sessionId = preview.SessionId,
            readyForImport = preview.ReadyForImport,
            totalRows = preview.TotalRows,
            changeCount = preview.ChangeCount,
            changes = preview.Changes,
            validation = new
            {
                isValid = preview.Validation.IsValid,
                errorCount = preview.Validation.Errors.Count,
                errors = preview.Validation.Errors.Take(20),
            },
            preview = preview.PreviewData.Rows.Take(5),
        });
    }

    [HttpGet("synonyms")]
    public IActionResult GetSynonyms()
    {
        return Ok(_synonymRegistry.ExportRegistry());
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            aiAvailable = _aiNormalization.IsAvailable(),
            ocrAvailable = true,
            supportedFormats = _parserFactory.GetSupportedFormats(),
        });
    }
}

public record NormalizeRequest
{
    public Guid SessionId { get; init; }
}
