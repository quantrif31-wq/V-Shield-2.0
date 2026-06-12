using System.Security.Claims;
using API.Data;
using API.DTOs;
using API.Services.ImportExport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/import-export")]
[Authorize(Roles = "Admin")]
public class ImportExportController : ControllerBase
{
    private readonly IImportExportService _service;
    private readonly IServiceScopeFactory _scopeFactory;

    public ImportExportController(IImportExportService service, IServiceScopeFactory scopeFactory)
    {
        _service = service;
        _scopeFactory = scopeFactory;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

    private async Task<ApplicationDbContext> CreateDbContextAsync()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [HttpPost("{entityType}/import")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Import(string entityType, [FromForm] ImportRequest? options)
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file để import" });

        await using var stream = file.OpenReadStream();
        var result = await _service.ImportAsync(
            entityType, stream, file.FileName, file.ContentType, options, GetUserId());

        return Ok(result);
    }

    [HttpPost("{entityType}/import/preview")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> PreviewImport(string entityType)
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file để preview" });

        await using var stream = file.OpenReadStream();
        var result = await _service.PreviewImportAsync(entityType, stream, file.FileName, file.ContentType);

        return Ok(result);
    }

    [HttpGet("{entityType}/export")]
    public async Task<IActionResult> Export(string entityType, [FromQuery] ExportRequest request)
    {
        var result = await _service.ExportAsync(entityType, request, GetUserId());
        return Ok(result);
    }

    [HttpGet("{entityType}/template")]
    public async Task<IActionResult> DownloadTemplate(string entityType, [FromQuery] string format = "csv")
    {
        try
        {
            var stream = await _service.DownloadTemplateAsync(entityType, format);
            var ext = format.TrimStart('.');
            var mime = ext switch
            {
                "csv" => "text/csv",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "json" => "application/json",
                "xml" => "application/xml",
                _ => "application/octet-stream",
            };
            return File(stream, mime, $"template_{entityType}.{ext}");
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadResult(Guid id)
    {
        await using var db = await CreateDbContextAsync();
        var history = await db.ImportExportHistories.FindAsync(id);
        if (history == null)
            return NotFound(new { message = "Không tìm thấy bản ghi" });

        if (history.ResultFileContent == null)
            return BadRequest(new { message = "File không khả dụng" });

        var ext = history.FileFormat.TrimStart('.');
        var mime = ext switch
        {
            "csv" => "text/csv",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "json" => "application/json",
            "xml" => "application/xml",
            _ => "application/octet-stream",
        };

        return File(history.ResultFileContent, mime, history.FileName);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? entityType,
        [FromQuery] string? operationType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetHistoryAsync(entityType, operationType, page, pageSize);
        return Ok(result);
    }

    [HttpGet("history/{id}")]
    public async Task<IActionResult> GetHistoryById(Guid id)
    {
        var result = await _service.GetHistoryByIdAsync(id);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy bản ghi" });
        return Ok(result);
    }

    [HttpGet("formats")]
    public IActionResult GetFormats()
    {
        return Ok(_service.GetSupportedFormats());
    }

    [HttpGet("entities")]
    public IActionResult GetEntities()
    {
        return Ok(_service.GetSupportedEntities());
    }
}
