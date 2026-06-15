using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class OperationalInterventionRequest
{
    public long OperationalInterventionRequestId { get; set; }

    /// <summary>Người yêu cầu (BaoVe)</summary>
    public int RequestedByUserId { get; set; }

    /// <summary>Làn / cổng nơi phát sinh yêu cầu</summary>
    [MaxLength(120)]
    public string? LaneId { get; set; }

    /// <summary>Tên làn</summary>
    [MaxLength(160)]
    public string? LaneName { get; set; }

    /// <summary>Loại can thiệp: temporary_grant, anti_passback_reset, emergency_override, policy_override, device_override, other</summary>
    [MaxLength(80)]
    public string InterventionType { get; set; } = "other";

    /// <summary>Thông tin đối tượng</summary>
    [MaxLength(240)]
    public string? SubjectName { get; set; }

    [MaxLength(80)]
    public string? SubjectId { get; set; }

    [MaxLength(40)]
    public string? SubjectType { get; set; } // EMPLOYEE, GUEST, VEHICLE

    [MaxLength(40)]
    public string? PlateNumber { get; set; }

    [MaxLength(500)]
    public string? QrPayload { get; set; }

    /// <summary>Lý do yêu cầu can thiệp</summary>
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Ghi chú bổ sung</summary>
    [MaxLength(2000)]
    public string? Note { get; set; }

    /// <summary>Trạng thái: Pending, Accepted, Rejected, Executed, Expired</summary>
    [MaxLength(40)]
    public string Status { get; set; } = "Pending";

    /// <summary>Mức độ ưu tiên: low, medium, high, critical</summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm Admin chấp nhận yêu cầu</summary>
    public DateTime? AcceptedAtUtc { get; set; }

    /// <summary>Admin đã chấp nhận</summary>
    public int? AcceptedByUserId { get; set; }

    /// <summary>Thời điểm Admin từ chối</summary>
    public DateTime? RejectedAtUtc { get; set; }

    /// <summary>Admin đã từ chối</summary>
    public int? RejectedByUserId { get; set; }

    /// <summary>Lý do từ chối</summary>
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    /// <summary>Thời điểm thực thi</summary>
    public DateTime? ExecutedAtUtc { get; set; }

    /// <summary>Người thực thi (Admin)</summary>
    public int? ExecutedByUserId { get; set; }

    /// <summary>Thời điểm hết hạn (tự động expired)</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>CorrelationId để truy vết audit</summary>
    [MaxLength(80)]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

    // Navigation properties
    public AppUser? RequestedByUser { get; set; }
    public AppUser? AcceptedByUser { get; set; }
    public AppUser? RejectedByUser { get; set; }
    public AppUser? ExecutedByUser { get; set; }
}
