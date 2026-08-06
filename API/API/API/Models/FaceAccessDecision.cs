using System.ComponentModel.DataAnnotations;

namespace API.Models;

public sealed class FaceAccessDecision
{
    public long Id { get; set; }
    public long FaceRecognitionEventId { get; set; }
    public long FaceAccessPolicyComparisonId { get; set; }
    public int? EmployeeId { get; set; }
    [MaxLength(64)] public string CameraId { get; set; } = string.Empty;
    public int? LaneId { get; set; }
    public int? GateId { get; set; }
    public int? AccessPointId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime DecidedAtUtc { get; set; }
    [MaxLength(24)] public string Decision { get; set; } = FaceAccessDecisionStatuses.Indeterminate;
    [MaxLength(80)] public string ReasonCode { get; set; } = string.Empty;
    [MaxLength(24)] public string LegacyDecision { get; set; } = PolicyEvaluationDecisions.Indeterminate;
    [MaxLength(64)] public string LegacyReasonCode { get; set; } = string.Empty;
    [MaxLength(24)] public string EnterpriseDecision { get; set; } = PolicyEvaluationDecisions.Indeterminate;
    [MaxLength(64)] public string EnterpriseReasonCode { get; set; } = string.Empty;
    public int? LegacyPermissionId { get; set; }
    public int? EnterprisePolicyVersionId { get; set; }
    public int? EnterpriseRuleId { get; set; }
    public int? EnterpriseScheduleId { get; set; }
    public int EvaluationVersion { get; set; }
    [MaxLength(80)] public string ScheduleTimeZoneId { get; set; } = string.Empty;
    [MaxLength(64)] public string InputFingerprint { get; set; } = string.Empty;
    public string PolicySnapshotJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }
    public FaceRecognitionEvent FaceRecognitionEvent { get; set; } = null!;
    public FaceAccessPolicyComparison FaceAccessPolicyComparison { get; set; } = null!;
}

public static class FaceAccessDecisionStatuses
{
    public const string Allowed = "Allowed";
    public const string Denied = "Denied";
    public const string ReviewRequired = "ReviewRequired";
    public const string Indeterminate = "Indeterminate";
}

public static class FaceAccessDecisionReasons
{
    public const string BothEnginesAllowed = "BothEnginesAllowed";
    public const string LegacyDenied = "LegacyDenied";
    public const string EnterpriseDenied = "EnterpriseDenied";
    public const string BothEnginesDenied = "BothEnginesDenied";
    public const string EvaluationIncomplete = "EvaluationIncomplete";
    public const string PolicyNotConfigured = "PolicyNotConfigured";
    public const string EvaluationError = "EvaluationError";
    public const string ExplicitDeny = "ExplicitDeny";
    public const string MappingInvalid = "MappingInvalid";
}
