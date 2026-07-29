using API.Services.AccessCredentials;

namespace API.Services.FaceCredentialBindings;

public sealed record FaceCredentialBindingDto(
    long Id,
    int EmployeeId,
    string EmployeeName,
    long AccessCredentialId,
    string CredentialType,
    string CredentialStoredStatus,
    string CredentialEffectiveStatus,
    string? MaskedIdentifier,
    string BindingStatus,
    DateTime? ActivatedAtUtc,
    DateTime? RevokedAtUtc,
    DateTime CreatedAtUtc,
    string? Reason,
    string RowVersion);

public sealed record FaceCredentialCandidateDto(
    long AccessCredentialId,
    int EmployeeId,
    string EmployeeName,
    string CredentialType,
    string CredentialStoredStatus,
    string CredentialEffectiveStatus,
    string? MaskedIdentifier,
    string CandidateClassification,
    string? BlockingReasonCode,
    DateTime? EffectiveFromUtc,
    DateTime? ExpiresAtUtc);

public sealed record CreateFaceCredentialBindingRequest(
    int EmployeeId,
    long AccessCredentialId,
    string? Reason,
    int? AuditActorUserId = null,
    string? AuditActorUsername = null);

public sealed record RevokeFaceCredentialBindingRequest(
    string? Reason,
    string RowVersion);

public sealed record FaceCredentialBindingResolution(
    EmployeeFaceCredentialBindingContext? Context,
    string ReasonCode,
    bool IsAmbiguous = false);

public sealed record EmployeeFaceCredentialBindingContext(
    long BindingId,
    int EmployeeId,
    long AccessCredentialId,
    string BindingStatus,
    DateTime ActivatedAtUtc,
    DateTime? RevokedAtUtc,
    DateTime OccurredAtUtc);

public interface IFaceCredentialBindingService
{
    Task<IReadOnlyList<FaceCredentialBindingDto>> ListAsync(CancellationToken token);
    Task<FaceCredentialBindingDto?> GetAsync(long id, CancellationToken token);
    Task<FaceCredentialBindingDto?> GetByEmployeeAsync(int employeeId, CancellationToken token);
    Task<FaceCredentialBindingDto?> GetByCredentialAsync(long accessCredentialId, CancellationToken token);
    Task<IReadOnlyList<FaceCredentialCandidateDto>> GetCandidatesAsync(int employeeId, CancellationToken token);
    Task<FaceCredentialBindingDto> CreateAsync(CreateFaceCredentialBindingRequest request, CancellationToken token);
    Task<FaceCredentialBindingDto> RevokeAsync(long id, RevokeFaceCredentialBindingRequest request, CancellationToken token);
    Task<FaceCredentialBindingResolution> ResolveAsync(int employeeId, DateTime occurredAtUtc, CancellationToken token);
}

public sealed class FaceCredentialBindingDomainException(string code, string message, int statusCode = 400)
    : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
