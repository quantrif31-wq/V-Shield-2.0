using System.Text.Json;
using API.Data;
using API.Models;
using API.Services.AccessCredentials;
using Microsoft.EntityFrameworkCore;

namespace API.Services.FaceCredentialBindings;

public sealed class FaceCredentialBindingManifestService(
    ApplicationDbContext db,
    IFaceCredentialBindingService bindingService,
    ICurrentUserContext currentUser)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<FaceCredentialBindingManifestTemplate> GenerateTemplateAsync(CancellationToken token)
    {
        var employees = await db.Employees.AsNoTracking()
            .Where(x => x.EmployeeId >= 1 && x.EmployeeId <= 5)
            .OrderBy(x => x.EmployeeId)
            .Select(x => new FaceCredentialBindingManifestItem(x.EmployeeId, null))
            .ToListAsync(token);

        var manifest = new FaceCredentialBindingManifestTemplate(
            1,
            false,
            string.Empty,
            null,
            employees);

        await WriteJsonAsync(GetDefaultManifestPath(), manifest, token);
        return manifest;
    }

    public async Task<FaceCredentialBindingValidationResult> ValidateManifestAsync(
        string manifestPath,
        bool requireApproval,
        CancellationToken token)
    {
        var manifest = await ReadManifestAsync(manifestPath, token);
        var findings = new List<FaceCredentialBindingValidationFinding>();

        if (manifest.SchemaVersion != 1)
            findings.Add(new("SchemaVersionInvalid", "schemaVersion must equal 1."));
        if (requireApproval && !manifest.Approved)
            findings.Add(new("ManifestNotApproved", "Manifest must be approved before apply."));
        if (manifest.Approved && string.IsNullOrWhiteSpace(manifest.ApprovedBy))
            findings.Add(new("ApprovedByMissing", "approvedBy is required when approved=true."));
        if (manifest.Approved && !manifest.ApprovedAtUtc.HasValue)
            findings.Add(new("ApprovedAtUtcMissing", "approvedAtUtc is required when approved=true."));
        if (manifest.ApprovedAtUtc.HasValue && manifest.ApprovedAtUtc.Value.Kind != DateTimeKind.Utc)
            findings.Add(new("ApprovedAtUtcNotUtc", "approvedAtUtc must be ISO 8601 UTC."));

        var duplicateEmployees = manifest.Bindings
            .GroupBy(x => x.EmployeeId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (duplicateEmployees.Length > 0)
            findings.Add(new("DuplicateEmployee", $"Manifest contains duplicate employeeId values: {string.Join(", ", duplicateEmployees)}"));

        var duplicateCredentials = manifest.Bindings
            .Where(x => x.AccessCredentialId.HasValue)
            .GroupBy(x => x.AccessCredentialId!.Value)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (duplicateCredentials.Length > 0)
            findings.Add(new("DuplicateCredential", $"Manifest contains duplicate accessCredentialId values: {string.Join(", ", duplicateCredentials)}"));

        var bindingChecks = new List<object>();
        foreach (var item in manifest.Bindings)
        {
            if (!item.AccessCredentialId.HasValue)
            {
                findings.Add(new("CredentialIdMissing", $"Employee {item.EmployeeId} has null accessCredentialId."));
                bindingChecks.Add(new
                {
                    employeeId = item.EmployeeId,
                    accessCredentialId = (long?)null,
                    valid = false,
                    reasonCode = "CredentialIdMissing"
                });
                continue;
            }

            try
            {
                var candidates = await bindingService.GetCandidatesAsync(item.EmployeeId, token);
                var candidate = candidates.SingleOrDefault(x => x.AccessCredentialId == item.AccessCredentialId.Value);
                if (candidate is null)
                {
                    findings.Add(new("CredentialMissing", $"Credential {item.AccessCredentialId.Value} is not a candidate for employee {item.EmployeeId}."));
                    bindingChecks.Add(new
                    {
                        employeeId = item.EmployeeId,
                        accessCredentialId = item.AccessCredentialId.Value,
                        valid = false,
                        reasonCode = "CredentialMissing"
                    });
                    continue;
                }

                var valid = candidate.CandidateClassification is "Ready" or "AlreadyBound";
                if (!valid)
                    findings.Add(new(candidate.BlockingReasonCode ?? "InvalidCandidate", $"Credential {candidate.AccessCredentialId} is not bindable for employee {candidate.EmployeeId}."));

                bindingChecks.Add(new
                {
                    employeeId = candidate.EmployeeId,
                    accessCredentialId = candidate.AccessCredentialId,
                    valid,
                    reasonCode = candidate.BlockingReasonCode,
                    candidateClassification = candidate.CandidateClassification,
                    credentialType = candidate.CredentialType,
                    credentialStoredStatus = candidate.CredentialStoredStatus,
                    credentialEffectiveStatus = candidate.CredentialEffectiveStatus,
                    maskedIdentifier = candidate.MaskedIdentifier
                });
            }
            catch (FaceCredentialBindingDomainException ex)
            {
                findings.Add(new(ex.Code, ex.Message));
                bindingChecks.Add(new
                {
                    employeeId = item.EmployeeId,
                    accessCredentialId = item.AccessCredentialId.Value,
                    valid = false,
                    reasonCode = ex.Code
                });
            }
        }

        var inventory = await BuildInventoryReportAsync(token);
        await WriteJsonAsync(GetDefaultReportPath(), inventory, token);

        return new FaceCredentialBindingValidationResult(
            findings.Count == 0,
            manifestPath,
            manifest.Approved,
            findings,
            bindingChecks,
            inventory);
    }

    public async Task<FaceCredentialBindingApplyResult> ApplyManifestAsync(
        string manifestPath,
        bool apply,
        bool confirmBindings,
        CancellationToken token)
    {
        if (!apply || !confirmBindings)
            return new(false, "ApplyRequiresConfirmation", []);

        var validation = await ValidateManifestAsync(manifestPath, requireApproval: true, token);
        if (!validation.Success)
            return new(false, "ManifestValidationFailed", []);

        var manifest = await ReadManifestAsync(manifestPath, token);
        var applied = new List<object>();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        try
        {
            foreach (var item in manifest.Bindings)
            {
                var result = await bindingService.CreateAsync(
                    new CreateFaceCredentialBindingRequest(item.EmployeeId, item.AccessCredentialId!.Value, "Manifest approved face credential binding"),
                    token);
                applied.Add(new
                {
                    result.EmployeeId,
                    result.AccessCredentialId,
                    result.Id,
                    result.BindingStatus
                });
            }

            db.SystemAuditLogs.Add(new SystemAuditLog
            {
                TimestampUtc = DateTime.UtcNow,
                UserId = currentUser.UserId,
                Username = currentUser.Username,
                EventCategory = "FACE_CREDENTIAL_BINDING",
                ActionType = "FaceCredentialBindingManifestApplied",
                EntityName = nameof(EmployeeFaceCredentialBinding),
                IsSuccess = true,
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    manifestPath,
                    approvedBy = manifest.ApprovedBy,
                    approvedAtUtc = manifest.ApprovedAtUtc,
                    appliedCount = applied.Count
                })
            });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return new(true, null, applied);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            return new(false, ex.Message, applied);
        }
    }

    public async Task<object> BuildInventoryReportAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var credentials = await db.AccessCredentials.AsNoTracking()
            .OrderBy(x => x.EmployeeId).ThenBy(x => x.Id)
            .ToListAsync(token);
        var bindings = await db.EmployeeFaceCredentialBindings.AsNoTracking()
            .OrderBy(x => x.EmployeeId).ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(token);
        var employees = await db.Employees.AsNoTracking()
            .Where(x => x.EmployeeId >= 1 && x.EmployeeId <= 5)
            .OrderBy(x => x.EmployeeId)
            .Select(x => new { x.EmployeeId, x.FullName })
            .ToListAsync(token);
        var models = await db.EmployeeFaceModels.AsNoTracking()
            .Where(x => x.EmployeeId >= 1 && x.EmployeeId <= 5 && x.Status == FaceModelLifecycleStatuses.Active)
            .OrderByDescending(x => x.ActivatedAtUtc ?? x.CreatedAt)
            .ToListAsync(token);

        var faceCredentials = credentials
            .Where(x => string.Equals(x.CredentialType, AccessCredentialTypes.FaceBiometric, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var perEmployeeCounts = faceCredentials.GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => x.Count());

        var invalidOwnership = bindings.Count(x => credentials.Any(c => c.Id == x.AccessCredentialId && c.EmployeeId != x.EmployeeId));
        var invalidType = bindings.Count(x => credentials.Any(c => c.Id == x.AccessCredentialId && !string.Equals(c.CredentialType, AccessCredentialTypes.FaceBiometric, StringComparison.OrdinalIgnoreCase)));

        var employeeReports = new List<object>();
        foreach (var employee in employees)
        {
            var employeeCredentialRows = faceCredentials.Where(x => x.EmployeeId == employee.EmployeeId).ToList();
            var existingBinding = bindings.FirstOrDefault(x => x.EmployeeId == employee.EmployeeId && x.Status == EmployeeFaceCredentialBindingStatuses.Active);
            var activeModel = models.FirstOrDefault(x => x.EmployeeId == employee.EmployeeId);
            var candidates = await bindingService.GetCandidatesAsync(employee.EmployeeId, token);
            var readyCandidates = candidates.Where(x => x.CandidateClassification == "Ready").ToList();
            var classification = existingBinding is not null
                ? "AlreadyBound"
                : readyCandidates.Count switch
                {
                    0 => "NoCandidate",
                    1 => "SingleCandidate",
                    _ => "MultipleCandidates"
                };

            employeeReports.Add(new
            {
                employeeId = employee.EmployeeId,
                employeeName = employee.FullName,
                activeFaceModelVersion = activeModel?.Version,
                faceBiometricCredentialCount = employeeCredentialRows.Count,
                credentialIds = employeeCredentialRows.Select(x => x.Id).ToArray(),
                credentialStates = employeeCredentialRows.Select(x => new
                {
                    accessCredentialId = x.Id,
                    storedStatus = x.Status,
                    effectiveStatus = EvaluateCredentialState(x, now).EffectiveStatus,
                    maskedIdentifier = x.MaskedIdentifier
                }).ToArray(),
                existingBinding = existingBinding is null ? null : new
                {
                    bindingId = existingBinding.Id,
                    bindingStatus = existingBinding.Status,
                    activatedAtUtc = existingBinding.ActivatedAtUtc,
                    revokedAtUtc = existingBinding.RevokedAtUtc
                },
                candidateClassification = classification,
                candidates = candidates.Select(x => new
                {
                    x.AccessCredentialId,
                    x.CredentialStoredStatus,
                    x.CredentialEffectiveStatus,
                    x.CandidateClassification,
                    x.BlockingReasonCode,
                    x.MaskedIdentifier
                }).ToArray()
            });
        }

        return new
        {
            generatedAtUtc = now,
            accessCredentials = new
            {
                total = credentials.Count,
                byType = credentials.GroupBy(x => x.CredentialType).ToDictionary(x => x.Key, x => x.Count()),
                byStoredStatus = credentials.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count()),
                faceBiometricCredentialCount = faceCredentials.Count,
                employeesWithZeroFaceBiometricCredentials = employees.Count(x => !perEmployeeCounts.ContainsKey(x.EmployeeId)),
                employeesWithOneFaceBiometricCredential = employees.Count(x => perEmployeeCounts.GetValueOrDefault(x.EmployeeId) == 1),
                employeesWithMultipleFaceBiometricCredentials = employees.Count(x => perEmployeeCounts.GetValueOrDefault(x.EmployeeId) > 1),
                invalidOwnershipBindings = invalidOwnership,
                invalidTypeBindings = invalidType
            },
            employeeFaceCredentialBindings = new
            {
                total = bindings.Count,
                active = bindings.Count(x => x.Status == EmployeeFaceCredentialBindingStatuses.Active),
                pending = bindings.Count(x => x.Status == EmployeeFaceCredentialBindingStatuses.Pending),
                revoked = bindings.Count(x => x.Status == EmployeeFaceCredentialBindingStatuses.Revoked)
            },
            employees1To5 = employeeReports
        };
    }

    public string GetDefaultManifestPath() => Path.Combine(GetFaceDataRoot(), "manifests", "face-credential-bindings.json");
    public string GetDefaultReportPath() => Path.Combine(GetFaceDataRoot(), "manifests", "face-credential-binding-report.json");

    private async Task<FaceCredentialBindingManifestTemplate> ReadManifestAsync(string path, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        var manifest = await JsonSerializer.DeserializeAsync<FaceCredentialBindingManifestTemplate>(stream, JsonOptions, token);
        return manifest ?? throw new InvalidOperationException("Manifest content is invalid.");
    }

    private static async Task WriteJsonAsync<T>(string path, T payload, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, JsonOptions), token);
    }

    private string GetFaceDataRoot()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (root.Parent is not null &&
               !Directory.Exists(Path.Combine(root.FullName, "runtime", "face-data")))
            root = root.Parent;
        return Path.Combine(root.FullName, "runtime", "face-data");
    }

    private static AccessCredentialState EvaluateCredentialState(AccessCredential credential, DateTime atUtc)
    {
        var evaluator = new AccessCredentialStateEvaluator();
        return evaluator.Evaluate(new(
            credential.Status,
            credential.EffectiveFromUtc,
            credential.ExpiresAtUtc,
            credential.RevokedAtUtc,
            atUtc));
    }
}

public sealed record FaceCredentialBindingManifestTemplate(
    int SchemaVersion,
    bool Approved,
    string ApprovedBy,
    DateTime? ApprovedAtUtc,
    IReadOnlyList<FaceCredentialBindingManifestItem> Bindings);

public sealed record FaceCredentialBindingManifestItem(
    int EmployeeId,
    long? AccessCredentialId);

public sealed record FaceCredentialBindingValidationFinding(string Code, string Message);

public sealed record FaceCredentialBindingValidationResult(
    bool Success,
    string ManifestPath,
    bool Approved,
    IReadOnlyList<FaceCredentialBindingValidationFinding> Findings,
    IReadOnlyList<object> BindingChecks,
    object InventoryReport);

public sealed record FaceCredentialBindingApplyResult(
    bool Success,
    string? Error,
    IReadOnlyList<object> AppliedBindings);
