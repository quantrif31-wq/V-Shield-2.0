using System.ComponentModel.DataAnnotations;

namespace API.Models;

public sealed class FaceAccessPolicyComparison
{
    public long Id { get; set; }
    public long FaceRecognitionEventId { get; set; }
    public int? EmployeeId { get; set; }
    [MaxLength(64)] public string CameraId { get; set; } = string.Empty;
    public int? FaceCameraConfigurationId { get; set; }
    public int? LaneId { get; set; }
    public int? GateId { get; set; }
    public int? AccessPointId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime EvaluatedAtUtc { get; set; }
    [MaxLength(24)] public string LegacyDecision { get; set; } = PolicyEvaluationDecisions.Indeterminate;
    [MaxLength(64)] public string LegacyReasonCode { get; set; } = string.Empty;
    public int? LegacyPermissionId { get; set; }
    [MaxLength(24)] public string EnterpriseDecision { get; set; } = PolicyEvaluationDecisions.Indeterminate;
    [MaxLength(64)] public string EnterpriseReasonCode { get; set; } = string.Empty;
    public int? EnterprisePolicyVersionId { get; set; }
    public int? EnterpriseRuleId { get; set; }
    public int? EnterpriseScheduleId { get; set; }
    [MaxLength(48)] public string ComparisonResult { get; set; } = PolicyComparisonResults.EvaluationError;
    [MaxLength(40)] public string MappingStatus { get; set; } = FacePolicyMappingStatuses.AmbiguousMapping;
    public int EvaluationVersion { get; set; }
    [MaxLength(64)] public string LegacyInputFingerprint { get; set; } = string.Empty;
    [MaxLength(64)] public string EnterpriseInputFingerprint { get; set; } = string.Empty;
    [MaxLength(80)] public string ScheduleTimeZoneId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public FaceRecognitionEvent FaceRecognitionEvent { get; set; } = null!;
}

public static class PolicyEvaluationDecisions
{
    public const string Allow = "Allow";
    public const string Deny = "Deny";
    public const string NotConfigured = "NotConfigured";
    public const string Indeterminate = "Indeterminate";
    public const string Error = "Error";
}

public static class PolicyComparisonResults
{
    public const string AgreeAllow = "AgreeAllow";
    public const string AgreeDeny = "AgreeDeny";
    public const string LegacyAllowEnterpriseDeny = "LegacyAllowEnterpriseDeny";
    public const string LegacyDenyEnterpriseAllow = "LegacyDenyEnterpriseAllow";
    public const string LegacyConfiguredEnterpriseMissing = "LegacyConfiguredEnterpriseMissing";
    public const string EnterpriseConfiguredLegacyMissing = "EnterpriseConfiguredLegacyMissing";
    public const string BothNotConfigured = "BothNotConfigured";
    public const string EnterpriseIndeterminate = "EnterpriseIndeterminate";
    public const string LegacyIndeterminate = "LegacyIndeterminate";
    public const string MappingUnavailable = "MappingUnavailable";
    public const string EvaluationError = "EvaluationError";
}

public static class FacePolicyMappingStatuses
{
    public const string Resolved = "Resolved";
    public const string CameraUnmanaged = "CameraUnmanaged";
    public const string LaneMissing = "LaneMissing";
    public const string GateMissing = "GateMissing";
    public const string AccessPointMissing = "AccessPointMissing";
    public const string AmbiguousMapping = "AmbiguousMapping";
}
