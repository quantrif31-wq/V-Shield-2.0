using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Agent;

public sealed class AgentAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AgentAuditService> _logger;

    public AgentAuditService(ApplicationDbContext db, ILogger<AgentAuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        AgentToolContext ctx,
        string toolName,
        string? argsJson,
        string? resultSummary,
        string status,
        long? promptTokens = null,
        long? completionTokens = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _db.AgentAuditLogs.Add(new Models.AgentAuditLog
            {
                AgentThreadId = ctx.ThreadId,
                UserId = ctx.UserId,
                EmployeeId = ctx.EmployeeId,
                ToolName = toolName,
                ArgsJson = argsJson,
                ResultSummary = resultSummary,
                Status = status,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ghi audit agent thất bại");
        }
    }
}

/// <summary>Bộ nhớ của agent: thread + summary + sổ sự kiện (tiết kiệm quota).</summary>
public sealed class MemoryService
{
    private readonly ApplicationDbContext _db;

    public MemoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Models.AgentThread> GetOrCreateThreadAsync(
        Guid threadId, int userId, int? employeeId, string? userName, string? role,
        CancellationToken cancellationToken = default)
    {
        var thread = await _db.AgentThreads.FirstOrDefaultAsync(t => t.AgentThreadId == threadId, cancellationToken);
        if (thread == null)
        {
            thread = new Models.AgentThread
            {
                AgentThreadId = threadId,
                UserId = userId,
                EmployeeId = employeeId,
                UserName = userName,
                Role = role,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.AgentThreads.Add(thread);
            await _db.SaveChangesAsync(cancellationToken);
        }
        return thread;
    }

    public async Task AddMessageAsync(Guid threadId, string role, string content, CancellationToken cancellationToken = default)
    {
        _db.AgentMessages.Add(new Models.AgentMessage
        {
            AgentThreadId = threadId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TouchAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        var thread = await _db.AgentThreads.FirstOrDefaultAsync(t => t.AgentThreadId == threadId, cancellationToken);
        if (thread != null)
        {
            thread.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<Models.AgentMessage>> GetRecentAsync(Guid threadId, int take, CancellationToken cancellationToken = default)
    {
        return await _db.AgentMessages
            .Where(m => m.AgentThreadId == threadId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.AgentMessageId)
            .Take(take)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.AgentMessageId)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveSummaryAsync(Guid threadId, string summary, CancellationToken cancellationToken = default)
    {
        var thread = await _db.AgentThreads.FirstOrDefaultAsync(t => t.AgentThreadId == threadId, cancellationToken);
        if (thread != null)
        {
            thread.Summary = summary;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string> ReadFactBlobAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        var thread = await _db.AgentThreads.FirstOrDefaultAsync(t => t.AgentThreadId == threadId, cancellationToken);
        return thread?.FactBlob ?? "";
    }

    public async Task WriteFactAsync(Guid threadId, string key, string value, CancellationToken cancellationToken = default)
    {
        var thread = await _db.AgentThreads.FirstOrDefaultAsync(t => t.AgentThreadId == threadId, cancellationToken);
        if (thread == null) return;

        var facts = ParseFactBlob(thread.FactBlob);
        facts[key] = value;
        thread.FactBlob = System.Text.Json.JsonSerializer.Serialize(facts);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, string> ParseFactBlob(string? blob)
    {
        if (string.IsNullOrWhiteSpace(blob)) return new Dictionary<string, string>();
        try { return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(blob) ?? new(); }
        catch { return new Dictionary<string, string>(); }
    }
}