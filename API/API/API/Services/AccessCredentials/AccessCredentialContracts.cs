namespace API.Services.AccessCredentials;

public sealed record AccessCredentialStateInput(
    string StoredStatus, DateTime? EffectiveFromUtc, DateTime? ExpiresAtUtc,
    DateTime? RevokedAtUtc, DateTime EvaluationTimeUtc);
public sealed record AccessCredentialState(string EffectiveStatus, string ReasonCode);

public sealed record AccessCredentialContext(
    long AccessCredentialId, int EmployeeId, string CredentialType,
    string StoredStatus, string EffectiveStatus, DateTime? EffectiveFromUtc,
    DateTime? ExpiresAtUtc, DateTime OccurredAtUtc, string SourceType,
    int? EmployeeDynamicQrId = null);

public sealed record AccessCredentialDto(
    long Id, int EmployeeId, string EmployeeName, string CredentialType,
    string StoredStatus, string EffectiveStatus, DateTime? EffectiveFromUtc,
    DateTime? ExpiresAtUtc, DateTime? RevokedAtUtc, string? MaskedIdentifier,
    int? EmployeeDynamicQrId, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
    string? Description, string RowVersion);

public sealed record CreateAccessCredentialRequest(
    int EmployeeId, string CredentialType, string? Identifier,
    int? EmployeeDynamicQrId, DateTime? EffectiveFromUtc,
    DateTime? ExpiresAtUtc, string? Description, bool Activate = false);

public sealed record AccessCredentialTransitionRequest(string RowVersion, string? Reason);

public sealed record CredentialResolution(
    AccessCredentialContext? Context, string ReasonCode, bool IsAmbiguous = false);

public interface IAccessCredentialStateEvaluator
{
    AccessCredentialState Evaluate(AccessCredentialStateInput input);
}

public interface IAccessCredentialContextResolver
{
    Task<CredentialResolution> ResolveByCredentialIdAsync(
        long credentialId, int employeeId, DateTime occurredAtUtc, CancellationToken token);
    Task<CredentialResolution> ResolveActiveCredentialsForEmployeeAsync(
        int employeeId, string credentialType, DateTime occurredAtUtc, CancellationToken token);
}

public interface IAccessCredentialService
{
    Task<IReadOnlyList<AccessCredentialDto>> ListAsync(int? employeeId, CancellationToken token);
    Task<AccessCredentialDto?> GetAsync(long id, CancellationToken token);
    Task<AccessCredentialDto> CreateAsync(CreateAccessCredentialRequest request, CancellationToken token);
    Task<AccessCredentialDto> TransitionAsync(
        long id, string targetStatus, string rowVersion, string? reason, CancellationToken token);
}
