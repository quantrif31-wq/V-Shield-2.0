using API.Models;

namespace API.Services.AccessCredentials;

public sealed class AccessCredentialStateEvaluator : IAccessCredentialStateEvaluator
{
    public AccessCredentialState Evaluate(AccessCredentialStateInput input)
    {
        if (!AccessCredentialStatuses.Supported.Contains(input.StoredStatus) ||
            input.EvaluationTimeUtc.Kind != DateTimeKind.Utc ||
            (input.EffectiveFromUtc.HasValue && input.ExpiresAtUtc.HasValue &&
             input.ExpiresAtUtc <= input.EffectiveFromUtc) ||
            (input.StoredStatus.Equals(AccessCredentialStatuses.Revoked,
                 StringComparison.OrdinalIgnoreCase) != input.RevokedAtUtc.HasValue))
            return new(EffectiveCredentialStatuses.Invalid, "CredentialInvalidLifecycle");

        if (input.StoredStatus.Equals(AccessCredentialStatuses.Revoked, StringComparison.OrdinalIgnoreCase))
            return new(EffectiveCredentialStatuses.Revoked, "CredentialRevoked");
        if (input.StoredStatus.Equals(AccessCredentialStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return new(EffectiveCredentialStatuses.Pending, "CredentialPending");
        if (input.StoredStatus.Equals(AccessCredentialStatuses.Inactive, StringComparison.OrdinalIgnoreCase))
            return new(EffectiveCredentialStatuses.Inactive, "CredentialInactive");
        if (input.EffectiveFromUtc.HasValue && input.EvaluationTimeUtc < input.EffectiveFromUtc)
            return new(EffectiveCredentialStatuses.NotYetEffective, "CredentialNotYetEffective");
        // EffectiveFrom is inclusive; ExpiresAt is exclusive.
        if (input.ExpiresAtUtc.HasValue && input.EvaluationTimeUtc >= input.ExpiresAtUtc)
            return new(EffectiveCredentialStatuses.Expired, "CredentialExpired");
        return new(EffectiveCredentialStatuses.Active, "CredentialActive");
    }
}
