using API.Models;

namespace API.Services.Audit;

public static class SystemAuditActions
{
    public const string CredentialCreated = "CredentialCreated";
    public const string CredentialActivated = "CredentialActivated";
    public const string CredentialDeactivated = "CredentialDeactivated";
    public const string CredentialRevoked = "CredentialRevoked";
    public const string CredentialCreationRejected = "CredentialCreationRejected";

    public const string FaceCredentialBindingCreated = "FaceCredentialBindingCreated";
    public const string FaceCredentialBindingRevoked = "FaceCredentialBindingRevoked";
    public const string FaceCredentialBindingConflict = "FaceCredentialBindingConflict";
    public const string FaceCredentialBindingRevokedImmutable = "FaceCredentialBindingRevokedImmutable";
    public const string FaceCredentialOwnershipMismatch = "FaceCredentialOwnershipMismatch";
    public const string FaceCredentialTypeMismatch = "FaceCredentialTypeMismatch";
    public const string FaceCredentialInactive = "FaceCredentialInactive";
    public const string FaceCredentialBindingManifestApplied = "FaceCredentialBindingManifestApplied";
    public const string FaceCredentialBindingAuditReconciled = "FaceCredentialBindingAuditReconciled";

    public static IReadOnlyList<string> CredentialAndBindingActions { get; } =
    [
        CredentialCreated,
        CredentialActivated,
        CredentialDeactivated,
        CredentialRevoked,
        CredentialCreationRejected,
        FaceCredentialBindingCreated,
        FaceCredentialBindingRevoked,
        FaceCredentialBindingConflict,
        FaceCredentialBindingRevokedImmutable,
        FaceCredentialOwnershipMismatch,
        FaceCredentialTypeMismatch,
        FaceCredentialInactive,
        FaceCredentialBindingManifestApplied,
        FaceCredentialBindingAuditReconciled
    ];

    static SystemAuditActions()
    {
        if (CredentialAndBindingActions.Any(action => action.Length > SystemAuditLogLimits.ActionTypeMaxLength))
            throw new InvalidOperationException("A system audit action exceeds the canonical ActionType length.");
    }
}
