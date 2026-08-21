using System;
using System.ComponentModel.DataAnnotations;

namespace API.Models;

/// <summary>Hội thoại của AI Agent (bộ nhớ dài hạn: summary + sổ sự kiện).</summary>
public class AgentThread
{
    [Key]
    public Guid AgentThreadId { get; set; } = Guid.NewGuid();

    public int UserId { get; set; }

    public int? EmployeeId { get; set; }

    [StringLength(100)]
    public string? UserName { get; set; }

    [StringLength(50)]
    public string? Role { get; set; }

    public string? Summary { get; set; }

    public string? FactBlob { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>Tin nhắn thô trong thread (phục vụ audit + compaction).</summary>
public class AgentMessage
{
    [Key]
    public long AgentMessageId { get; set; }

    public Guid AgentThreadId { get; set; }

    [StringLength(20)]
    public string Role { get; set; } = "user";

    public string? Content { get; set; }

    public long? PromptTokens { get; set; }

    public long? CompletionTokens { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Email nháp do agent soạn (chưa gửi / đã gửi / lỗi).</summary>
public class AgentDraft
{
    [Key]
    public long AgentDraftId { get; set; }

    public Guid AgentThreadId { get; set; }

    public int UserId { get; set; }

    public int? EmployeeId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public string? To { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    [StringLength(200)]
    public string? MessageId { get; set; }

    public string? SendError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? SentAt { get; set; }
}

/// <summary>Nhật ký mọi lời gọi tool của agent (append-only).</summary>
public class AgentAuditLog
{
    [Key]
    public long AgentAuditId { get; set; }

    public Guid AgentThreadId { get; set; }

    public int UserId { get; set; }

    public int? EmployeeId { get; set; }

    [StringLength(60)]
    public string ToolName { get; set; } = "";

    public string? ArgsJson { get; set; }

    public string? ResultSummary { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Ok";

    public long? PromptTokens { get; set; }

    public long? CompletionTokens { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}