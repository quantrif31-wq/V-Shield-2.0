using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;

namespace API.Services.AccessCredentials;

public sealed class AccessCredentialDomainException(string code, string message, int statusCode = 400)
    : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class AccessCredentialService(
    ApplicationDbContext db,
    IAccessCredentialStateEvaluator stateEvaluator,
    IAccessCredentialIdentifierProtector protector,
    ICurrentUserContext currentUser) : IAccessCredentialService, IAccessCredentialContextResolver
{
    public async Task<IReadOnlyList<AccessCredentialDto>> ListAsync(
        int? employeeId, CancellationToken token)
    {
        var query = db.AccessCredentials.AsNoTracking().Include(x => x.Employee).AsQueryable();
        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId);
        var rows = await query.OrderBy(x => x.EmployeeId).ThenBy(x => x.Id).ToListAsync(token);
        return rows.Select(x => Map(x, DateTime.UtcNow)).ToList();
    }

    public async Task<AccessCredentialDto?> GetAsync(long id, CancellationToken token)
    {
        var row = await db.AccessCredentials.AsNoTracking().Include(x => x.Employee)
            .SingleOrDefaultAsync(x => x.Id == id, token);
        return row is null ? null : Map(row, DateTime.UtcNow);
    }

    public async Task<AccessCredentialDto> CreateAsync(
        CreateAccessCredentialRequest request, CancellationToken token)
    {
        var type = AccessCredentialTypes.Normalize(request.CredentialType)
            ?? throw Fail("CredentialTypeUnsupported", "Credential type is unsupported.");
        var now = DateTime.UtcNow;
        ValidateWindow(request.EffectiveFromUtc, request.ExpiresAtUtc, now);
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.EmployeeId == request.EmployeeId, token)
            ?? throw Fail("CredentialEmployeeMissing", "Employee does not exist.", 404);
        if (employee.Status == false ||
            !employee.LifecycleStatus.Equals(EmployeeLifecycleStates.Active, StringComparison.OrdinalIgnoreCase))
            throw Fail("CredentialEmployeeInactive", "Employee is inactive.", 409);

        string? hash = null;
        string? mask = null;
        if (type == AccessCredentialTypes.Card)
        {
            if (string.IsNullOrWhiteSpace(request.Identifier))
                throw Fail("CredentialIdentifierRequired", "Card identifier is required.");
            (hash, mask) = protector.Protect(type, request.Identifier);
            if (await db.AccessCredentials.AnyAsync(
                    x => x.CredentialType == type && x.IdentifierHash == hash, token))
                throw Fail("CredentialDuplicateIdentifier", "Credential identifier already exists.", 409);
        }
        else if (!string.IsNullOrEmpty(request.Identifier))
            throw Fail("CredentialIdentifierNotAllowed", "This credential type does not accept an identifier.");

        EmployeeDynamicQr? qr = null;
        if (type == AccessCredentialTypes.DynamicQr)
        {
            if (!request.EmployeeDynamicQrId.HasValue)
                throw Fail("CredentialQrRequired", "Dynamic QR reference is required.");
            qr = await db.EmployeeDynamicQrs.SingleOrDefaultAsync(
                x => x.Id == request.EmployeeDynamicQrId, token)
                ?? throw Fail("CredentialQrMissing", "Dynamic QR row does not exist.", 404);
            if (qr.EmployeeId != request.EmployeeId)
                throw Fail("CredentialOwnershipMismatch", "Dynamic QR belongs to another employee.", 409);
            if (!qr.IsActive && request.Activate)
                throw Fail("CredentialQrInactive", "Inactive Dynamic QR cannot create an active credential.", 409);
            if (await db.AccessCredentials.AnyAsync(
                    x => x.EmployeeDynamicQrId == request.EmployeeDynamicQrId, token))
                throw Fail("CredentialQrAlreadyLinked", "Dynamic QR is already linked.", 409);
            mask = $"QR-{qr.Id}";
        }
        else if (request.EmployeeDynamicQrId.HasValue)
            throw Fail("CredentialQrNotAllowed", "Only DynamicQr credentials may reference a QR row.");

        if (type == AccessCredentialTypes.FaceBiometric && request.Activate &&
            await db.AccessCredentials.AnyAsync(x => x.EmployeeId == request.EmployeeId &&
                x.CredentialType == AccessCredentialTypes.FaceBiometric &&
                x.Status == AccessCredentialStatuses.Active, token))
            throw Fail("CredentialActiveFaceDuplicate", "Employee already has an active face credential.", 409);

        var row = new AccessCredential
        {
            EmployeeId = request.EmployeeId,
            CredentialType = type,
            Status = request.Activate ? AccessCredentialStatuses.Active : AccessCredentialStatuses.Pending,
            EffectiveFromUtc = Utc(request.EffectiveFromUtc),
            ExpiresAtUtc = Utc(request.ExpiresAtUtc),
            IdentifierHash = hash,
            IdentifierHashVersion = hash is null ? null : "hmac-sha256-v1",
            MaskedIdentifier = mask,
            EmployeeDynamicQrId = qr?.Id,
            Description = Sanitize(request.Description),
            CreatedByUserId = currentUser.UserId,
            CreatedAtUtc = now,
            Employee = employee
        };
        db.AccessCredentials.Add(row);
        AddAudit("CredentialCreated", row, true, null);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException)
        {
            AddAudit("CredentialCreationRejected", row, false, "Database uniqueness or ownership constraint.");
            throw Fail("CredentialConflict", "Credential conflicts with an existing record.", 409);
        }
        return Map(row, now);
    }

    public async Task<AccessCredentialDto> TransitionAsync(
        long id, string targetStatus, string rowVersion, string? reason, CancellationToken token)
    {
        var normalizedTarget = AccessCredentialStatuses.Supported.FirstOrDefault(
            x => x.Equals(targetStatus, StringComparison.OrdinalIgnoreCase))
            ?? throw Fail("CredentialTransitionInvalid", "Target status is unsupported.");
        var row = await db.AccessCredentials.Include(x => x.Employee)
            .SingleOrDefaultAsync(x => x.Id == id, token)
            ?? throw Fail("CredentialMissing", "Credential does not exist.", 404);
        if (row.Status == normalizedTarget) return Map(row, DateTime.UtcNow);
        if (!Allowed(row.Status, normalizedTarget))
            throw Fail("CredentialTransitionInvalid",
                $"Transition {row.Status} to {normalizedTarget} is not allowed.", 409);
        byte[] version;
        try { version = Convert.FromBase64String(rowVersion ?? string.Empty); }
        catch (FormatException) { throw Fail("CredentialRowVersionInvalid", "RowVersion is invalid.", 409); }
        db.Entry(row).Property(x => x.RowVersion).OriginalValue = version;
        var now = DateTime.UtcNow;
        var effective = stateEvaluator.Evaluate(new(
            row.Status, row.EffectiveFromUtc, row.ExpiresAtUtc, row.RevokedAtUtc, now));
        if (normalizedTarget == AccessCredentialStatuses.Active &&
            effective.EffectiveStatus == EffectiveCredentialStatuses.Expired)
            throw Fail("CredentialExpired", "Expired credential cannot be activated.", 409);
        if (normalizedTarget == AccessCredentialStatuses.Active &&
            row.CredentialType == AccessCredentialTypes.DynamicQr &&
            !await db.EmployeeDynamicQrs.AnyAsync(x =>
                x.Id == row.EmployeeDynamicQrId && x.EmployeeId == row.EmployeeId && x.IsActive, token))
            throw Fail("CredentialQrInactive", "Dynamic QR is missing, inactive, or has different ownership.", 409);
        if (normalizedTarget == AccessCredentialStatuses.Active &&
            row.CredentialType == AccessCredentialTypes.FaceBiometric &&
            await db.AccessCredentials.AnyAsync(x => x.Id != row.Id &&
                x.EmployeeId == row.EmployeeId && x.CredentialType == AccessCredentialTypes.FaceBiometric &&
                x.Status == AccessCredentialStatuses.Active, token))
            throw Fail("CredentialActiveFaceDuplicate", "Employee already has an active face credential.", 409);

        row.Status = normalizedTarget;
        row.UpdatedAtUtc = now;
        if (normalizedTarget == AccessCredentialStatuses.Revoked)
        {
            row.RevokedAtUtc = now;
            row.RevokedByUserId = currentUser.UserId;
            row.RevocationReason = Sanitize(reason);
        }
        AddAudit($"Credential{normalizedTarget}", row, true, Sanitize(reason));
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            throw Fail("CredentialConcurrencyConflict", "Credential was changed by another request.", 409);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            throw Fail("CredentialConflict", "Credential transition conflicts with current state.", 409);
        }
        return Map(row, now);
    }

    public async Task<CredentialResolution> ResolveByCredentialIdAsync(
        long credentialId, int employeeId, DateTime occurredAtUtc, CancellationToken token)
    {
        var row = await db.AccessCredentials.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == credentialId, token);
        if (row is null) return new(null, "EnterpriseCredentialMissing");
        if (row.EmployeeId != employeeId) return new(null, "EnterpriseCredentialOwnershipMismatch");
        return Resolve(row, occurredAtUtc);
    }

    public async Task<CredentialResolution> ResolveActiveCredentialsForEmployeeAsync(
        int employeeId, string credentialType, DateTime occurredAtUtc, CancellationToken token)
    {
        var type = AccessCredentialTypes.Normalize(credentialType);
        if (type is null) return new(null, "EnterpriseCredentialTypeUnsupported");
        var rows = await db.AccessCredentials.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.CredentialType == type)
            .ToListAsync(token);
        var effective = rows.Select(x => (Row: x, Resolution: Resolve(x, occurredAtUtc)))
            .Where(x => x.Resolution.Context?.EffectiveStatus == EffectiveCredentialStatuses.Active)
            .ToList();
        if (effective.Count > 1) return new(null, "EnterpriseCredentialAmbiguous", true);
        if (effective.Count == 1) return effective[0].Resolution;
        return rows.Count == 1 ? Resolve(rows[0], occurredAtUtc)
            : new(null, rows.Count == 0 ? "EnterpriseCredentialMissing" : "EnterpriseCredentialAmbiguous",
                rows.Count > 1);
    }

    private CredentialResolution Resolve(AccessCredential row, DateTime occurredAtUtc)
    {
        occurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        var state = stateEvaluator.Evaluate(new(row.Status, row.EffectiveFromUtc,
            row.ExpiresAtUtc, row.RevokedAtUtc, occurredAtUtc));
        return new(new(row.Id, row.EmployeeId, row.CredentialType, row.Status,
            state.EffectiveStatus, row.EffectiveFromUtc, row.ExpiresAtUtc, occurredAtUtc,
            row.EmployeeDynamicQrId.HasValue ? "EmployeeDynamicQr" : "Canonical",
            row.EmployeeDynamicQrId), state.ReasonCode);
    }

    private AccessCredentialDto Map(AccessCredential row, DateTime at)
    {
        var state = stateEvaluator.Evaluate(new(row.Status, row.EffectiveFromUtc,
            row.ExpiresAtUtc, row.RevokedAtUtc, at));
        return new(row.Id, row.EmployeeId, row.Employee?.FullName ?? string.Empty,
            row.CredentialType, row.Status, state.EffectiveStatus, row.EffectiveFromUtc,
            row.ExpiresAtUtc, row.RevokedAtUtc, row.MaskedIdentifier, row.EmployeeDynamicQrId,
            row.CreatedAtUtc, row.UpdatedAtUtc, row.Description,
            Convert.ToBase64String(row.RowVersion));
    }

    private void AddAudit(string action, AccessCredential row, bool success, string? reason) =>
        db.SystemAuditLogs.Add(new SystemAuditLog
        {
            TimestampUtc = DateTime.UtcNow, UserId = currentUser.UserId,
            Username = currentUser.Username, EventCategory = "ACCESS_CREDENTIAL",
            ActionType = action, EntityName = nameof(AccessCredential),
            EntityId = row.Id == 0 ? null : row.Id.ToString(), IsSuccess = success,
            FailureReason = reason,
            NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                row.EmployeeId, row.CredentialType, row.Status
            })
        });

    private static void ValidateWindow(DateTime? from, DateTime? expires, DateTime now)
    {
        if (from.HasValue && from.Value.Kind == DateTimeKind.Local ||
            expires.HasValue && expires.Value.Kind == DateTimeKind.Local)
            throw Fail("CredentialUtcRequired", "Credential dates must be UTC.");
        if (from.HasValue && expires.HasValue && expires <= from)
            throw Fail("CredentialWindowInvalid", "ExpiresAtUtc must be after EffectiveFromUtc.");
        if (expires.HasValue && expires <= now)
            throw Fail("CredentialExpired", "A new credential cannot already be expired.");
    }

    private static DateTime? Utc(DateTime? value) => value.HasValue
        ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    private static string? Sanitize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 500)];
    private static bool Allowed(string from, string to) =>
        (from, to) switch
        {
            (AccessCredentialStatuses.Pending, AccessCredentialStatuses.Active) => true,
            (AccessCredentialStatuses.Pending, AccessCredentialStatuses.Revoked) => true,
            (AccessCredentialStatuses.Active, AccessCredentialStatuses.Inactive) => true,
            (AccessCredentialStatuses.Active, AccessCredentialStatuses.Revoked) => true,
            (AccessCredentialStatuses.Inactive, AccessCredentialStatuses.Active) => true,
            (AccessCredentialStatuses.Inactive, AccessCredentialStatuses.Revoked) => true,
            _ => false
        };
    private static AccessCredentialDomainException Fail(string code, string message, int status = 400) =>
        new(code, message, status);
}
