using API.Data;
using API.Models;
using API.Services.AccessCredentials;
using API.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace API.Services.FaceCredentialBindings;

public sealed class FaceCredentialBindingService(
    ApplicationDbContext db,
    IAccessCredentialStateEvaluator stateEvaluator,
    ICurrentUserContext currentUser) : IFaceCredentialBindingService
{
    public async Task<IReadOnlyList<FaceCredentialBindingDto>> ListAsync(CancellationToken token)
    {
        var rows = await QueryBindings()
            .OrderBy(x => x.EmployeeId)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(token);
        var now = DateTime.UtcNow;
        return rows.Select(x => Map(x, now)).ToList();
    }

    public async Task<FaceCredentialBindingDto?> GetAsync(long id, CancellationToken token)
    {
        var row = await QueryBindings().SingleOrDefaultAsync(x => x.Id == id, token);
        return row is null ? null : Map(row, DateTime.UtcNow);
    }

    public async Task<FaceCredentialBindingDto?> GetByEmployeeAsync(int employeeId, CancellationToken token)
    {
        var row = await QueryBindings()
            .OrderByDescending(x => x.Status == EmployeeFaceCredentialBindingStatuses.Active)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId, token);
        return row is null ? null : Map(row, DateTime.UtcNow);
    }

    public async Task<FaceCredentialBindingDto?> GetByCredentialAsync(long accessCredentialId, CancellationToken token)
    {
        var row = await QueryBindings()
            .OrderByDescending(x => x.Status == EmployeeFaceCredentialBindingStatuses.Active)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.AccessCredentialId == accessCredentialId, token);
        return row is null ? null : Map(row, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<FaceCredentialCandidateDto>> GetCandidatesAsync(int employeeId, CancellationToken token)
    {
        var employee = await db.Employees.AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmployeeId == employeeId, token)
            ?? throw Fail("FaceCredentialBindingEmployeeMissing", "Employee does not exist.", 404);

        var now = DateTime.UtcNow;
        var credentials = await db.AccessCredentials.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Id)
            .ToListAsync(token);

        var activeEmployeeBinding = await db.EmployeeFaceCredentialBindings.AsNoTracking()
            .Include(x => x.AccessCredential)
            .Where(x => x.EmployeeId == employeeId && x.Status == EmployeeFaceCredentialBindingStatuses.Active)
            .OrderByDescending(x => x.ActivatedAtUtc)
            .ToListAsync(token);

        var activeCredentialBindings = await db.EmployeeFaceCredentialBindings.AsNoTracking()
            .Where(x => x.Status == EmployeeFaceCredentialBindingStatuses.Active)
            .ToDictionaryAsync(x => x.AccessCredentialId, token);

        return credentials.Select(credential =>
        {
            var state = EvaluateCredential(credential, now);
            var classification = "Ready";
            string? blockingReason = null;

            if (!string.Equals(credential.CredentialType, AccessCredentialTypes.FaceBiometric, StringComparison.OrdinalIgnoreCase))
            {
                classification = "InvalidCandidate";
                blockingReason = "EnterpriseFaceCredentialTypeMismatch";
            }
            else if (state.EffectiveStatus != EffectiveCredentialStatuses.Active)
            {
                classification = "InvalidCandidate";
                blockingReason = ToEnterpriseCredentialReason(state.EffectiveStatus);
            }
            else if (activeCredentialBindings.TryGetValue(credential.Id, out var binding) && binding.EmployeeId != employeeId)
            {
                classification = "InvalidCandidate";
                blockingReason = "EnterpriseFaceCredentialOwnershipMismatch";
            }
            else if (activeEmployeeBinding.Any(x => x.AccessCredentialId == credential.Id))
            {
                classification = "AlreadyBound";
            }
            else if (activeEmployeeBinding.Count > 0)
            {
                classification = "AlreadyBound";
                blockingReason = "FaceCredentialBindingConflict";
            }
            else if (credentials.Count(x => string.Equals(x.CredentialType, AccessCredentialTypes.FaceBiometric, StringComparison.OrdinalIgnoreCase) &&
                                            EvaluateCredential(x, now).EffectiveStatus == EffectiveCredentialStatuses.Active) > 1)
            {
                classification = "MultipleCandidates";
                blockingReason = "EnterpriseCredentialAmbiguous";
            }

            return new FaceCredentialCandidateDto(
                credential.Id,
                employee.EmployeeId,
                employee.FullName,
                credential.CredentialType,
                credential.Status,
                state.EffectiveStatus,
                credential.MaskedIdentifier,
                classification,
                blockingReason,
                credential.EffectiveFromUtc,
                credential.ExpiresAtUtc);
        }).ToList();
    }

    public async Task<FaceCredentialBindingDto> CreateAsync(CreateFaceCredentialBindingRequest request, CancellationToken token)
    {
        var ownsTransaction = db.Database.IsRelational() && db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await db.Database.BeginTransactionAsync(token)
            : null;
        var now = DateTime.UtcNow;
        var employee = await db.Employees.AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmployeeId == request.EmployeeId, token)
            ?? throw Fail("FaceCredentialBindingEmployeeMissing", "Employee does not exist.", 404);
        if (employee.Status != true || !string.Equals(employee.LifecycleStatus, EmployeeLifecycleStates.Active, StringComparison.OrdinalIgnoreCase))
            throw Fail("FaceCredentialBindingEmployeeInactive", "Employee is inactive.", 409);

        var credential = await db.AccessCredentials.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.AccessCredentialId, token)
            ?? throw Fail("FaceCredentialBindingCredentialMissing", "Access credential does not exist.", 404);

        ValidateCredentialOwnership(employee.EmployeeId, credential);
        ValidateCredentialType(credential);
        EnsureCredentialActiveAt(credential, now);

        var existingSame = await db.EmployeeFaceCredentialBindings
            .Include(x => x.Employee)
            .Include(x => x.AccessCredential)
            .ThenInclude(x => x!.Employee)
            .SingleOrDefaultAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.AccessCredentialId == request.AccessCredentialId &&
                x.Status == EmployeeFaceCredentialBindingStatuses.Active, token);
        if (existingSame is not null)
            return Map(existingSame, now);

        var employeeActiveBinding = await db.EmployeeFaceCredentialBindings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.Status == EmployeeFaceCredentialBindingStatuses.Active, token);
        if (employeeActiveBinding is not null)
            throw Fail("FaceCredentialBindingConflict", "Employee already has an active face credential binding.", 409);

        var credentialActiveBinding = await db.EmployeeFaceCredentialBindings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccessCredentialId == request.AccessCredentialId && x.Status == EmployeeFaceCredentialBindingStatuses.Active, token);
        if (credentialActiveBinding is not null)
            throw Fail("FaceCredentialBindingConflict", "Credential is already bound to another employee.", 409);

        var revokedSameBinding = await db.EmployeeFaceCredentialBindings.AsNoTracking()
            .AnyAsync(x => x.EmployeeId == request.EmployeeId &&
                           x.AccessCredentialId == request.AccessCredentialId &&
                           x.Status == EmployeeFaceCredentialBindingStatuses.Revoked, token);
        if (revokedSameBinding)
            throw Fail("FaceCredentialBindingRevokedImmutable", "Revoked binding cannot be activated again.", 409);

        var row = new EmployeeFaceCredentialBinding
        {
            EmployeeId = request.EmployeeId,
            AccessCredentialId = request.AccessCredentialId,
            Status = EmployeeFaceCredentialBindingStatuses.Active,
            ActivatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedByUserId = request.AuditActorUserId ?? currentUser.UserId,
            Reason = Sanitize(request.Reason)
        };

        db.EmployeeFaceCredentialBindings.Add(row);

        try
        {
            // Keep the transaction open: SQL Server must assign the identity before
            // the authoritative business audit is constructed.
            await db.SaveChangesAsync(token);
            AddAudit(
                SystemAuditActions.FaceCredentialBindingCreated,
                row,
                true,
                row.Reason,
                request.AuditActorUserId,
                request.AuditActorUsername);
            await db.SaveChangesAsync(token);
            if (transaction is not null)
                await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(token);
            db.ChangeTracker.Clear();
            throw Fail("FaceCredentialBindingConflict", "Binding conflicts with an existing active employee or credential binding.", 409);
        }

        var created = await QueryBindings().SingleAsync(x => x.Id == row.Id, token);
        return Map(created, now);
    }

    public async Task<FaceCredentialBindingDto> RevokeAsync(long id, RevokeFaceCredentialBindingRequest request, CancellationToken token)
    {
        var row = await db.EmployeeFaceCredentialBindings
            .Include(x => x.Employee)
            .Include(x => x.AccessCredential)
            .ThenInclude(x => x!.Employee)
            .SingleOrDefaultAsync(x => x.Id == id, token)
            ?? throw Fail("FaceCredentialBindingMissing", "Binding does not exist.", 404);

        if (string.IsNullOrWhiteSpace(request.RowVersion))
            throw Fail("FaceCredentialBindingRowVersionMissing", "RowVersion is required.", 409);

        if (row.Status == EmployeeFaceCredentialBindingStatuses.Revoked)
            return Map(row, DateTime.UtcNow);

        byte[] version;
        try
        {
            version = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            throw Fail("FaceCredentialBindingRowVersionInvalid", "RowVersion is invalid.", 409);
        }

        db.Entry(row).Property(x => x.RowVersion).OriginalValue = version;
        var now = DateTime.UtcNow;
        row.Status = EmployeeFaceCredentialBindingStatuses.Revoked;
        row.RevokedAtUtc = now;
        row.RevokedByUserId = currentUser.UserId;
        row.Reason = Sanitize(request.Reason) ?? row.Reason;
        AddAudit(SystemAuditActions.FaceCredentialBindingRevoked, row, true, row.Reason);

        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            throw Fail("FaceCredentialBindingConcurrencyConflict", "Binding was changed by another request.", 409);
        }

        return Map(row, now);
    }

    public async Task<FaceCredentialBindingResolution> ResolveAsync(int employeeId, DateTime occurredAtUtc, CancellationToken token)
    {
        occurredAtUtc = EnsureUtc(occurredAtUtc);
        var bindings = await db.EmployeeFaceCredentialBindings.AsNoTracking()
            .Include(x => x.AccessCredential)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.ActivatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(token);

        if (bindings.Count == 0)
            return new(null, "EnterpriseFaceCredentialBindingMissing");

        var activeAtTime = bindings
            .Where(x => x.ActivatedAtUtc.HasValue &&
                        x.ActivatedAtUtc.Value <= occurredAtUtc &&
                        (x.RevokedAtUtc == null || occurredAtUtc < x.RevokedAtUtc.Value))
            .ToList();

        if (activeAtTime.Count > 1)
            return new(null, "EnterpriseCredentialAmbiguous", true);

        var binding = activeAtTime.SingleOrDefault();
        if (binding is null)
        {
            var revoked = bindings
                .Where(x => x.RevokedAtUtc.HasValue && x.RevokedAtUtc.Value <= occurredAtUtc)
                .OrderByDescending(x => x.RevokedAtUtc)
                .FirstOrDefault();
            return revoked is null
                ? new(null, "EnterpriseFaceCredentialBindingMissing")
                : new(null, "EnterpriseFaceCredentialBindingRevoked");
        }

        // Revocation closes the binding's effective interval; it must not erase
        // the policy context for recognition events that occurred before revoke.
        if (!string.Equals(binding.Status, EmployeeFaceCredentialBindingStatuses.Active, StringComparison.OrdinalIgnoreCase) &&
            !(string.Equals(binding.Status, EmployeeFaceCredentialBindingStatuses.Revoked, StringComparison.OrdinalIgnoreCase) &&
              binding.RevokedAtUtc.HasValue &&
              occurredAtUtc < binding.RevokedAtUtc.Value))
            return new(null, "EnterpriseFaceCredentialBindingMissing");

        var credential = binding.AccessCredential;
        if (credential is null)
            return new(null, "EnterpriseFaceCredentialBindingMissing");
        if (credential.EmployeeId != employeeId)
            return new(null, "EnterpriseFaceCredentialOwnershipMismatch");
        if (!string.Equals(credential.CredentialType, AccessCredentialTypes.FaceBiometric, StringComparison.OrdinalIgnoreCase))
            return new(null, "EnterpriseFaceCredentialTypeMismatch");
        if (credential.CreatedAtUtc > occurredAtUtc)
            return new(null, "EnterpriseCredentialInactive");

        var state = EvaluateCredential(credential, occurredAtUtc);
        if (state.EffectiveStatus != EffectiveCredentialStatuses.Active)
            return new(null, ToEnterpriseCredentialReason(state.EffectiveStatus));

        return new(new EmployeeFaceCredentialBindingContext(
            binding.Id,
            binding.EmployeeId,
            binding.AccessCredentialId,
            binding.Status,
            binding.ActivatedAtUtc!.Value,
            binding.RevokedAtUtc,
            occurredAtUtc), "CredentialActive");
    }

    private IQueryable<EmployeeFaceCredentialBinding> QueryBindings() =>
        db.EmployeeFaceCredentialBindings.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.AccessCredential)
            .ThenInclude(x => x!.Employee);

    private FaceCredentialBindingDto Map(EmployeeFaceCredentialBinding binding, DateTime atUtc)
    {
        var credential = binding.AccessCredential ?? new AccessCredential
        {
            EmployeeId = binding.EmployeeId,
            CredentialType = AccessCredentialTypes.FaceBiometric,
            Status = AccessCredentialStatuses.Inactive
        };
        var state = EvaluateCredential(credential, EnsureUtc(atUtc));
        return new FaceCredentialBindingDto(
            binding.Id,
            binding.EmployeeId,
            binding.Employee?.FullName ?? credential.Employee?.FullName ?? string.Empty,
            binding.AccessCredentialId,
            credential.CredentialType,
            credential.Status,
            state.EffectiveStatus,
            credential.MaskedIdentifier,
            binding.Status,
            binding.ActivatedAtUtc,
            binding.RevokedAtUtc,
            binding.CreatedAtUtc,
            binding.Reason,
            Convert.ToBase64String(binding.RowVersion));
    }

    private AccessCredentialState EvaluateCredential(AccessCredential credential, DateTime atUtc) =>
        stateEvaluator.Evaluate(new(
            credential.Status,
            credential.EffectiveFromUtc,
            credential.ExpiresAtUtc,
            credential.RevokedAtUtc,
            EnsureUtc(atUtc)));

    private void EnsureCredentialActiveAt(AccessCredential credential, DateTime atUtc)
    {
        var state = EvaluateCredential(credential, atUtc);
        if (state.EffectiveStatus != EffectiveCredentialStatuses.Active)
        {
            AddRejectedAudit(credential.EmployeeId, credential.Id, "FaceCredentialInactive", state.ReasonCode);
            throw Fail("FaceCredentialInactive", $"Credential is not active at {atUtc:o}.", 409);
        }
    }

    private void ValidateCredentialOwnership(int employeeId, AccessCredential credential)
    {
        if (credential.EmployeeId == employeeId) return;
        AddRejectedAudit(employeeId, credential.Id, "FaceCredentialOwnershipMismatch", "Credential belongs to another employee.");
        throw Fail("FaceCredentialOwnershipMismatch", "Credential belongs to another employee.", 409);
    }

    private void ValidateCredentialType(AccessCredential credential)
    {
        if (string.Equals(credential.CredentialType, AccessCredentialTypes.FaceBiometric, StringComparison.OrdinalIgnoreCase))
            return;
        AddRejectedAudit(credential.EmployeeId, credential.Id, "FaceCredentialTypeMismatch", "Credential type must be FaceBiometric.");
        throw Fail("FaceCredentialTypeMismatch", "Only FaceBiometric credentials can be bound to face identity.", 409);
    }

    private void AddRejectedAudit(int employeeId, long credentialId, string action, string? reason)
    {
        db.SystemAuditLogs.Add(new SystemAuditLog
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = currentUser.UserId,
            Username = currentUser.Username,
            EventCategory = "FACE_CREDENTIAL_BINDING",
            ActionType = action,
            EntityName = nameof(EmployeeFaceCredentialBinding),
            EntityId = null,
            IsSuccess = false,
            FailureReason = reason,
            NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                employeeId,
                accessCredentialId = credentialId,
                actor = currentUser.Username
            })
        });
    }

    private void AddAudit(
        string action,
        EmployeeFaceCredentialBinding binding,
        bool success,
        string? reason,
        int? actorUserId = null,
        string? actorUsername = null)
    {
        db.SystemAuditLogs.Add(new SystemAuditLog
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = actorUserId ?? currentUser.UserId,
            Username = actorUsername ?? currentUser.Username,
            EventCategory = "FACE_CREDENTIAL_BINDING",
            ActionType = action,
            EntityName = nameof(EmployeeFaceCredentialBinding),
            EntityId = binding.Id == 0 ? null : binding.Id.ToString(),
            IsSuccess = success,
            FailureReason = reason,
            NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                actor = actorUsername ?? currentUser.Username,
                bindingId = binding.Id,
                employeeId = binding.EmployeeId,
                accessCredentialId = binding.AccessCredentialId,
                status = binding.Status,
                reason
            })
        });
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static string ToEnterpriseCredentialReason(string effectiveStatus) => effectiveStatus switch
    {
        EffectiveCredentialStatuses.Pending => "EnterpriseCredentialPending",
        EffectiveCredentialStatuses.Inactive => "EnterpriseCredentialInactive",
        EffectiveCredentialStatuses.Expired => "EnterpriseCredentialExpired",
        EffectiveCredentialStatuses.Revoked => "EnterpriseCredentialRevoked",
        EffectiveCredentialStatuses.NotYetEffective => "EnterpriseCredentialInactive",
        _ => "EnterpriseCredentialInactive"
    };

    private static string? Sanitize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 500)];

    private static FaceCredentialBindingDomainException Fail(string code, string message, int status = 400) =>
        new(code, message, status);
}
