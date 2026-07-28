using System.ComponentModel.DataAnnotations;

namespace API.Models;

public sealed class FaceEnrollmentJob
{
    [Key] public Guid Id { get; set; }
    public int EmployeeId { get; set; }
    public int EmployeeFaceVideoId { get; set; }
    public int RequestedByUserId { get; set; }
    [MaxLength(24)] public string Status { get; set; } = FaceEnrollmentJobStatuses.Pending;
    public int AttemptCount { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? PreparedAtUtc { get; set; }
    public DateTime? ActivationRequestedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    [MaxLength(80)] public string? FailureCode { get; set; }
    [MaxLength(500)] public string? FailureMessage { get; set; }
    [MaxLength(120)] public string? CandidateReference { get; set; }
    [MaxLength(64)] public string? CandidateChecksum { get; set; }
    public int? CandidateEncodingCount { get; set; }
    public int? TotalInputFrames { get; set; }
    public int? ProcessedFrameCount { get; set; }
    public int? UsableFrameCount { get; set; }
    public int? NoFaceFrameCount { get; set; }
    public int? MultipleFaceFrameCount { get; set; }
    public int? InvalidFrameCount { get; set; }
    public double? QualityScore { get; set; }
    [MaxLength(40)] public string? DuplicateSubjectId { get; set; }
    public double? DuplicateDistance { get; set; }
    public int? TargetModelVersion { get; set; }
    [MaxLength(255)] public string? ExpectedModelFileName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Employee Employee { get; set; } = null!;
    public EmployeeFaceVideo EmployeeFaceVideo { get; set; } = null!;
    public AppUser RequestedByUser { get; set; } = null!;
    public EmployeeFaceModel? ResultModel { get; set; }
}

public static class FaceEnrollmentJobStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Prepared = "Prepared";
    public const string Activating = "Activating";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
    public const string RecoveryRequired = "RecoveryRequired";
    public static readonly string[] NonTerminal =
        [Pending, Processing, Prepared, Activating, RecoveryRequired];
}
