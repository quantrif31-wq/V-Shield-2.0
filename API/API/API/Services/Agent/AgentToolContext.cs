using System.Text.Json.Nodes;
using API.Data;

namespace API.Services.Agent;

/// <summary>Bối cảnh cho một lời gọi tool của agent.</summary>
public sealed class AgentToolContext
{
    public required int UserId { get; init; }
    public int? EmployeeId { get; init; }
    public string? UserName { get; init; }
    public string? Role { get; init; }
    public required IReadOnlyList<string> OperationalTaskKeys { get; init; }
    public bool IsAdmin { get; init; }
    public required Guid ThreadId { get; init; }
    public required ApplicationDbContext Db { get; init; }
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Gửi sự kiện tiến trình lên client (SSE).</summary>
    public Func<string, Task>? EmitStatus { get; init; }
}

/// <summary>Định nghĩa một Skill/Tool của agent.</summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    JsonObject ParametersSchema { get; }

    Task<string> ExecuteAsync(AgentToolContext ctx, JsonObject args, CancellationToken cancellationToken);
}

public static class ToolAuthorizer
{
    /// <summary>
    /// Kiểm tra user hiện tại có được gọi tool không.
    /// Nguyên tắc: agent chỉ làm được những gì tài khoản đang đăng nhập được phép.
    /// </summary>
    public static bool CanUse(AgentToolContext ctx, string toolName)
    {
        switch (toolName)
        {
            case "get_me":
            case "save_note":
            case "get_note":
            case "draft_email":
                // Không chạm dữ liệu người khác; nháp email an toàn vì gửi cần xác nhận riêng.
                return true;
            case "search_people":
            case "get_person":
            case "get_org_relation":
            case "resolve_greeting":
                // Tra cứu danh bạ như chức năng Contacts/Chat mà user có quyền.
                return ctx.IsAdmin || ctx.OperationalTaskKeys.Count > 0;
            default:
                return false;
        }
    }

    /// <summary>Ẩn trường nhạy cảm (CCCD/DOB đầy đủ) chỉ dành cho Admin.</summary>
    public static bool CanSeeSensitiveProfile(AgentToolContext ctx) => ctx.IsAdmin;
}