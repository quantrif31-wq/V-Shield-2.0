using System.ComponentModel.DataAnnotations;

namespace API.Models;

/// <summary>
/// Job đăng ký Face ID từ xa: khách gửi ảnh khuôn mặt qua VPS, job chờ local
/// node (có AI) xử lý rồi trả về template. Đồng bộ qua cơ chế sync hiện có.
/// </summary>
public sealed class RemoteFaceEnrollmentJob
{
    [Key]
    public Guid Id { get; set; }

    public int EmployeeId { get; set; }

    public int? CompanyId { get; set; }

    public int? SiteId { get; set; }

    /// <summary>Pending | Processing | Completed | Failed</summary>
    [MaxLength(24)]
    public string Status { get; set; } = RemoteFaceEnrollmentJobStatuses.Pending;

    /// <summary>Node local đang xử lý job này (null nếu chưa gán).</summary>
    [MaxLength(80)]
    public string? AssignedNodeId { get; set; }

    public DateTime? AssignedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(80)]
    public string? FailureCode { get; set; }

    [MaxLength(500)]
    public string? FailureMessage { get; set; }

    // Kết quả trả về sau khi local xử lý xong.
    [MaxLength(255)]
    public string? ResultModelFileName { get; set; }

    [MaxLength(64)]
    public string? ResultChecksum { get; set; }

    public int? ResultEncodingCount { get; set; }

    /// <summary>Nội dung template JSON (SFace) do local xử lý trả về, để VPS
    /// lưu và phát xuống các local khác.</summary>
    public string? TemplateContent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Employee Employee { get; set; } = null!;

    public ICollection<RemoteFaceEnrollmentFrame> Frames { get; set; } =
        new List<RemoteFaceEnrollmentFrame>();
}

public sealed class RemoteFaceEnrollmentFrame
{
    [Key]
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public int Ordinal { get; set; }

    /// <summary>Ảnh JPEG base64 (data URI).</summary>
    [Required]
    public string ImageData { get; set; } = null!;

    public RemoteFaceEnrollmentJob Job { get; set; } = null!;
}

public static class RemoteFaceEnrollmentJobStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
