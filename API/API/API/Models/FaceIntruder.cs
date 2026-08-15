using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

/// <summary>
/// Kẻ xâm nhập / truy cập không được phép phát hiện qua Face ID.
/// Lưu ảnh chụp (snapshot + crop), lý do, và thông tin nhân viên nếu khớp
/// với một người đã biết nhưng không có quyền (hoặc trong danh sách đen).
/// </summary>
[Table("FaceIntruders")]
public class FaceIntruder
{
    [Key]
    public int Id { get; set; }

    /// <summary>Camera runtime đã phát hiện (runtimeCameraId).</summary>
    [MaxLength(80)]
    public string? CameraId { get; set; }

    [MaxLength(80)]
    public string? GateName { get; set; }

    public int? GateId { get; set; }

    public int? EmployeeId { get; set; }

    [MaxLength(150)]
    public string? EmployeeName { get; set; }

    /// <summary>unknown | denied | blacklist</summary>
    [MaxLength(30)]
    public string Reason { get; set; } = "unknown";

    [MaxLength(200)]
    public string? ReasonDetail { get; set; }

    public double? Distance { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? SnapshotBase64 { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? FaceCropBase64 { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class FaceIntruderReasons
{
    public const string Unknown = "unknown";
    public const string Denied = "denied";
    public const string Blacklist = "blacklist";
}
