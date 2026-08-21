using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Services;
using API.Services.Agent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
[Route("api/ai-chat")]
[Authorize]
public class AiChatController : ControllerBase
{
    private readonly AgentRunner _agentRunner;
    private readonly MemoryService _memory;
    private readonly IMailService _mailService;
    private readonly MailOptions _mailOptions;
    private readonly UserOperationalScopeService _scopeService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(
        AgentRunner agentRunner,
        MemoryService memory,
        IMailService mailService,
        IOptions<MailOptions> mailOptions,
        UserOperationalScopeService scopeService,
        ApplicationDbContext db,
        ILogger<AiChatController> logger)
    {
        _agentRunner = agentRunner;
        _memory = memory;
        _mailService = mailService;
        _mailOptions = mailOptions.Value;
        _scopeService = scopeService;
        _db = db;
        _logger = logger;
    }

    private async Task<AgentActor> ResolveActorAsync(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var userId = int.TryParse(userIdValue, out var uid) ? uid : 0;
        var employeeIdValue = User.FindFirst("employeeId")?.Value;
        var employeeId = int.TryParse(employeeIdValue, out var eid) ? eid : (int?)null;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var fullName = User.FindFirst("fullName")?.Value ?? User.Identity?.Name;
        var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        var taskKeys = new List<string>();
        try { taskKeys = await _scopeService.GetEffectiveTaskKeysAsync(userId, role, ct); } catch { }

        return new AgentActor
        {
            UserId = userId,
            EmployeeId = employeeId,
            UserName = fullName,
            Role = role,
            TaskKeys = taskKeys,
            IsAdmin = isAdmin
        };
    }

    private async Task WriteSseAsync(string payload)
    {
        await Response.WriteAsync($"data: {payload}\n\n");
        await Response.Body.FlushAsync();
    }

    /// <summary>Trò chuyện với agent (SSE: status/token/draft/done).</summary>
    [HttpPost("stream")]
    public async Task StreamChat([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        Guid? threadId = null;
        if (body.TryGetProperty("threadId", out var tid) && tid.ValueKind == JsonValueKind.String)
        {
            if (Guid.TryParse(tid.GetString(), out var g)) threadId = g;
        }
        var message = body.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
            ? m.GetString() ?? ""
            : "";

        var actor = await ResolveActorAsync(cancellationToken);
        await _agentRunner.RunAsync(
            new AgentRunRequest { ThreadId = threadId, Message = message },
            actor,
            WriteSseAsync,
            cancellationToken);
    }

    /// <summary>Cập nhật nội dung nháp sau khi người dùng chỉnh sửa.</summary>
    [HttpPut("drafts/{id:long}")]
    public async Task<IActionResult> UpdateDraft(long id, [FromBody] DraftEditRequest request)
    {
        var actor = await ResolveActorAsync(HttpContext.RequestAborted);
        var draft = await _db.AgentDrafts.FirstOrDefaultAsync(d => d.AgentDraftId == id, HttpContext.RequestAborted);
        if (draft == null || draft.UserId != actor.UserId)
            return NotFound(new { success = false, message = "Không tìm thấy nháp." });

        if (request.To is { Length: > 0 }) draft.To = string.Join(";", request.To.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (!string.IsNullOrWhiteSpace(request.Subject)) draft.Subject = request.Subject;
        if (!string.IsNullOrWhiteSpace(request.Body)) draft.Body = request.Body;
        await _db.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(new { success = true, draft = new { id = draft.AgentDraftId, to = ToArray(draft.To), draft.Subject, draft.Body } });
    }

    /// <summary>Gửi email (CHỈ được gọi từ nút Gửi do người dùng bấm — đây là điểm xác nhận của con người).</summary>
    [HttpPost("send-draft")]
    public async Task<IActionResult> SendDraft([FromBody] SendDraftRequest request)
    {
        var actor = await ResolveActorAsync(HttpContext.RequestAborted);
        var draft = await _db.AgentDrafts.FirstOrDefaultAsync(d => d.AgentDraftId == request.DraftId, HttpContext.RequestAborted);
        if (draft == null || draft.UserId != actor.UserId)
            return NotFound(new { success = false, message = "Không tìm thấy nháp." });

        if (draft.Status != "Draft")
            return BadRequest(new { success = false, message = $"Nháp đã ở trạng thái {draft.Status}." });

        // Cho phép nội dung người dùng sửa trước khi gửi
        if (request.To is { Length: > 0 }) draft.To = string.Join(";", request.To.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (!string.IsNullOrWhiteSpace(request.Subject)) draft.Subject = request.Subject;
        if (!string.IsNullOrWhiteSpace(request.Body)) draft.Body = request.Body;

        var to = ToArray(draft.To);
        if (to.Length == 0)
            return BadRequest(new { success = false, message = "Chưa có người nhận." });

        // Re-validate người nhận phía server (không tin nội dung AI)
        var invalid = await ValidateRecipientsAsync(to, HttpContext.RequestAborted);
        if (invalid.Count > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Không thể gửi tới: " + string.Join(", ", invalid)
                    + (invalid.Any(x => !IsInternal(x)) ? " (email ngoài hệ thống cần bật Mail:AllowExternal)" : "")
            });
        }

        var bodyHtml = ToHtml((draft.Subject ?? ""), (draft.Body ?? ""));
        string? fromEmail = null, fromName = null;
        if (actor.EmployeeId is int empId)
        {
            var senderEmp = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == empId, HttpContext.RequestAborted);
            if (senderEmp != null)
            {
                if (!string.IsNullOrWhiteSpace(senderEmp.CompanyEmail)) fromEmail = senderEmp.CompanyEmail;
                if (!string.IsNullOrWhiteSpace(senderEmp.FullName)) fromName = senderEmp.FullName;
            }
        }

        var result = await _mailService.SendAsync(
            new MailMessage(to, draft.Subject ?? "", bodyHtml, FromEmail: fromEmail, FromName: fromName),
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            draft.Status = "Failed";
            draft.SendError = result.Error;
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return StatusCode(502, new { success = false, message = result.Error });
        }

        draft.Status = "Sent";
        draft.MessageId = result.MessageId;
        draft.SentAt = DateTime.Now;
        await _db.SaveChangesAsync(HttpContext.RequestAborted);

        _logger.LogInformation("Agent gửi email #{DraftId} cho user {User} tới {To}", draft.AgentDraftId, actor.UserId, draft.To);
        return Ok(new { success = true, message = "Đã gửi email.", messageId = result.MessageId });
    }

    /// <summary>AI viết lại nháp theo yêu cầu (trang trọng hơn, ngắn gọn hơn, giữ nguyên...).</summary>
    [HttpPost("refine-draft")]
    public async Task<IActionResult> RefineDraft([FromBody] RefineDraftRequest request)
    {
        var actor = await ResolveActorAsync(HttpContext.RequestAborted);
        var draft = await _db.AgentDrafts.FirstOrDefaultAsync(d => d.AgentDraftId == request.DraftId, HttpContext.RequestAborted);
        if (draft == null || draft.UserId != actor.UserId)
            return NotFound(new { success = false, message = "Không tìm thấy nháp." });

        var instruction = string.IsNullOrWhiteSpace(request.Instruction)
            ? "Viết lại cho chuẩn chuyên nghiệp, trang trọng."
            : request.Instruction;

        var messages = new List<object>
        {
            new { role = "system", content =
                "Bạn là trợ lý soạn email tiếng Việt. Người dùng đưa một email nháp + yêu cầu chỉnh sửa. " +
                "Chỉ trả về JSON đúng định dạng {\"subject\":\"...\",\"body\":\"...\"}, không giải thích thêm. " +
                "Giữ người nhận, giữ ý, giữ lời chào và chữ ký phù hợp." },
            new { role = "user", content = $"EMAIL NHÁP:\nChủ đề: {draft.Subject}\nNội dung:\n{draft.Body}\n\nYÊU CẦU: {instruction}" }
        };

        var llm = HttpContext.RequestServices.GetRequiredService<AgentLlmClient>();
        var resp = await llm.CompleteAsync(messages, null, maxTokens: 1200, cancellationToken: HttpContext.RequestAborted);
        if (resp.IsError || string.IsNullOrWhiteSpace(resp.Content))
            return StatusCode(502, new { success = false, message = resp.Error ?? "AI không phản hồi." });

        try
        {
            var parsed = JsonDocument.Parse(resp.Content).RootElement;
            var newSubject = parsed.TryGetProperty("subject", out var s) ? s.GetString() : draft.Subject;
            var newBody = parsed.TryGetProperty("body", out var b) ? b.GetString() : draft.Body;
            if (!string.IsNullOrWhiteSpace(newSubject)) draft.Subject = newSubject;
            if (!string.IsNullOrWhiteSpace(newBody)) draft.Body = newBody;
            await _db.SaveChangesAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                success = true,
                draft = new { id = draft.AgentDraftId, to = ToArray(draft.To), draft.Subject, draft.Body }
            });
        }
        catch (JsonException)
        {
            return StatusCode(502, new { success = false, message = "AI trả về định dạng không hợp lệ." });
        }
    }

    private async Task<List<string>> ValidateRecipientsAsync(IReadOnlyList<string> to, CancellationToken ct)
    {
        var domain = (_mailOptions.Domain ?? "").Trim().TrimStart('@').ToLowerInvariant();
        var invalid = new List<string>();
        foreach (var email in to)
        {
            var norm = (email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(norm)) continue;

            var isCompany = !string.IsNullOrWhiteSpace(domain)
                && norm.ToLowerInvariant().EndsWith("@" + domain, StringComparison.Ordinal);
            var inDirectory = await _db.Employees.AnyAsync(
                e => e.Status != false && e.Email != null && e.Email.ToLower() == norm.ToLower(), ct)
                || await _db.Employees.AnyAsync(
                e => e.Status != false && e.CompanyEmail != null && e.CompanyEmail.ToLower() == norm.ToLower(), ct);

            if (!isCompany && !inDirectory && !_mailOptions.AllowExternal)
                invalid.Add(norm);
        }
        return invalid;
    }

    private static bool IsInternal(string email) => true; // chỗ này chỉ dùng cho thông điệp; logic thực ở ValidateRecipients

    private static string[] ToArray(string? joined)
        => (joined ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();

    private static string ToHtml(string subject, string body)
    {
        var escaped = System.Net.WebUtility.HtmlEncode(body).Replace("\n", "<br/>");
        return $"<html><body>{escaped}</body></html>";
    }
}

public sealed class DraftEditRequest
{
    public string[]? To { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
}

public sealed class SendDraftRequest
{
    public long DraftId { get; set; }
    public string[]? To { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
}

public sealed class RefineDraftRequest
{
    public long DraftId { get; set; }
    public string? Instruction { get; set; }
}