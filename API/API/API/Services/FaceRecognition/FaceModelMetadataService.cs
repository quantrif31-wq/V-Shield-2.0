using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Services.FaceRecognition;

public sealed record FaceRegistryModel(
    string SubjectId,
    string FileName,
    string Checksum,
    int EncodingCount,
    int RegistryVersion);

public sealed record FaceRegistrySnapshot(
    int Version,
    int SuccessfulFileCount,
    int EncodingCount,
    int ErrorCount,
    IReadOnlyList<FaceRegistryModel> Models);

public sealed record FaceModelBootstrapResult(
    bool Success,
    string Status,
    int DatabaseModelCount,
    int RegistryModelCount,
    int EncodingCount,
    IReadOnlyList<string> Issues);

public sealed record FaceModelAdminDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string ModelFileName,
    int? Version,
    string? Status,
    int? EncodingCount,
    string? ChecksumPrefix,
    DateTime? ActivatedAtUtc,
    DateTime? ArchivedAtUtc,
    DateTime? RevokedAtUtc,
    string RegistrySyncState,
    int? RegistryVersion);

public interface IFaceModelMetadataService
{
    Task<FaceModelBootstrapResult> BootstrapAsync(
        bool apply,
        bool confirmBootstrap,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FaceModelAdminDto>> ListAsync(
        int? employeeId,
        CancellationToken cancellationToken);
}

public sealed class FaceModelMetadataService : IFaceModelMetadataService
{
    private const int ExpectedModelCount = 5;
    private const int ExpectedEncodingCount = 665;
    private static readonly Regex Sha256Pattern =
        new("^[0-9a-f]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ApplicationDbContext _db;
    private readonly IFaceRecognitionClient _runtime;

    public FaceModelMetadataService(
        ApplicationDbContext db,
        IFaceRecognitionClient runtime)
    {
        _db = db;
        _runtime = runtime;
    }

    public async Task<FaceModelBootstrapResult> BootstrapAsync(
        bool apply,
        bool confirmBootstrap,
        CancellationToken cancellationToken)
    {
        if (apply != confirmBootstrap)
        {
            return Failed("ConfirmationRequired",
                "--apply and --confirm-bootstrap must be supplied together.");
        }

        var validation = await ValidateAsync(cancellationToken);
        if (!validation.Result.Success ||
            !apply ||
            validation.Result.Status == "AlreadyBootstrapped")
        {
            return validation.Result;
        }

        IDbContextTransaction? transaction = null;
        if (_db.Database.IsRelational())
        {
            transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);
        }
        try
        {
            // Validate again inside the transaction and update the tracked rows
            // selected from the authoritative database state.
            validation = await ValidateAsync(cancellationToken);
            if (!validation.Result.Success ||
                validation.Result.Status == "AlreadyBootstrapped")
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);
                return validation.Result;
            }

            foreach (var row in validation.Rows)
            {
                var descriptor = validation.Registry.Models.Single(
                    model => model.FileName == row.ModelFileName);
                row.Version = 1;
                row.Status = FaceModelLifecycleStatuses.Active;
                row.ModelChecksum = descriptor.Checksum;
                row.EncodingCount = descriptor.EncodingCount;
                row.ActivatedAtUtc = DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc);
                row.ArchivedAtUtc = null;
                row.RevokedAtUtc = null;
                row.FailureCode = null;
                row.FailureMessage = null;
                row.SourceEnrollmentJobId = null;
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return validation.Result with { Status = "Bootstrapped" };
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return Failed("ConcurrencyConflict", "Face model metadata changed concurrently.");
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<FaceModelAdminDto>> ListAsync(
        int? employeeId,
        CancellationToken cancellationToken)
    {
        FaceRegistrySnapshot? registry = null;
        try
        {
            registry = await GetRegistryAsync(cancellationToken);
        }
        catch (FaceRuntimeUnavailableException)
        {
            // RuntimeUnavailable is a safe read-only reconciliation state.
        }

        var query = _db.EmployeeFaceModels
            .AsNoTracking()
            .Include(model => model.Employee)
            .AsQueryable();
        if (employeeId.HasValue)
        {
            query = query.Where(model => model.EmployeeId == employeeId.Value);
        }

        var rows = await query
            .OrderBy(model => model.EmployeeId)
            .ThenByDescending(model => model.Version)
            .ToListAsync(cancellationToken);
        var runtimeByName = registry?.Models.ToDictionary(
            model => model.FileName,
            StringComparer.Ordinal);

        var result = rows.Select(row =>
        {
            var state = "RuntimeUnavailable";
            FaceRegistryModel? runtimeModel = null;
            if (runtimeByName is not null)
            {
                runtimeByName.TryGetValue(row.ModelFileName, out runtimeModel);
                state = Reconcile(row, runtimeModel);
            }

            return new FaceModelAdminDto(
                row.Id,
                row.EmployeeId,
                row.Employee.FullName,
                row.ModelFileName,
                row.Version,
                row.Status,
                row.EncodingCount,
                row.ModelChecksum is { Length: >= 12 }
                    ? row.ModelChecksum[..12]
                    : null,
                row.ActivatedAtUtc,
                row.ArchivedAtUtc,
                row.RevokedAtUtc,
                state,
                registry?.Version);
        }).ToList();

        if (registry is not null)
        {
            var databaseNames = rows.Select(row => row.ModelFileName)
                .ToHashSet(StringComparer.Ordinal);
            result.AddRange(registry.Models
                .Where(model => !databaseNames.Contains(model.FileName))
                .Select(model => new FaceModelAdminDto(
                    0,
                    0,
                    "Unexpected runtime model",
                    model.FileName,
                    null,
                    null,
                    model.EncodingCount,
                    model.Checksum[..12],
                    null,
                    null,
                    null,
                    "UnexpectedInRuntime",
                    registry.Version)));
        }

        return result;
    }

    private async Task<ValidationState> ValidateAsync(CancellationToken cancellationToken)
    {
        FaceRegistrySnapshot registry;
        try
        {
            registry = await GetRegistryAsync(cancellationToken);
        }
        catch (FaceRuntimeUnavailableException)
        {
            return new ValidationState(
                Failed("RuntimeUnavailable", "Face Runtime is unavailable."),
                [],
                EmptyRegistry());
        }

        var rows = await _db.EmployeeFaceModels
            .OrderBy(model => model.Id)
            .ToListAsync(cancellationToken);
        var issues = new List<string>();

        if (rows.Count != ExpectedModelCount)
            issues.Add($"Expected {ExpectedModelCount} database models.");
        if (registry.SuccessfulFileCount != ExpectedModelCount ||
            registry.Models.Count != ExpectedModelCount)
            issues.Add($"Expected {ExpectedModelCount} registry models.");
        if (registry.EncodingCount != ExpectedEncodingCount)
            issues.Add($"Expected {ExpectedEncodingCount} registry encodings.");
        if (registry.ErrorCount != 0)
            issues.Add("Registry contains model load errors.");
        if (rows.Select(row => row.ModelFileName).Distinct(StringComparer.Ordinal).Count() != rows.Count)
            issues.Add("Database contains duplicate model filenames.");
        if (rows.Select(row => row.EmployeeId).Distinct().Count() != rows.Count)
            issues.Add("Database contains duplicate employee models.");
        if (registry.Models.Select(model => model.FileName).Distinct(StringComparer.Ordinal).Count() != registry.Models.Count)
            issues.Add("Registry contains duplicate model filenames.");

        var runtimeByName = registry.Models
            .GroupBy(model => model.FileName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!runtimeByName.TryGetValue(row.ModelFileName, out var matches) ||
                matches.Count != 1)
            {
                issues.Add($"Model {row.ModelFileName} does not match exactly one runtime descriptor.");
                continue;
            }

            var model = matches[0];
            if (model.SubjectId != row.EmployeeId.ToString(
                    System.Globalization.CultureInfo.InvariantCulture))
                issues.Add($"Subject mismatch for {row.ModelFileName}.");
            if (!Sha256Pattern.IsMatch(model.Checksum))
                issues.Add($"Invalid checksum for {row.ModelFileName}.");
            if (model.EncodingCount <= 0)
                issues.Add($"Invalid encoding count for {row.ModelFileName}.");
        }

        foreach (var runtimeModel in registry.Models)
        {
            if (!rows.Any(row => row.ModelFileName == runtimeModel.FileName))
                issues.Add($"Unexpected runtime model {runtimeModel.FileName}.");
        }

        if (issues.Count > 0)
        {
            return new ValidationState(
                new FaceModelBootstrapResult(
                    false,
                    "Conflict",
                    rows.Count,
                    registry.Models.Count,
                    registry.EncodingCount,
                    issues),
                rows,
                registry);
        }

        var alreadyBootstrapped = rows.All(row =>
        {
            var runtimeModel = runtimeByName[row.ModelFileName][0];
            return row.Version == 1 &&
                   row.Status == FaceModelLifecycleStatuses.Active &&
                   row.ModelChecksum == runtimeModel.Checksum &&
                   row.EncodingCount == runtimeModel.EncodingCount &&
                   row.ActivatedAtUtc.HasValue;
        });
        var anyMetadata = rows.Any(row =>
            row.Version.HasValue || row.Status is not null ||
            row.ModelChecksum is not null || row.EncodingCount.HasValue ||
            row.ActivatedAtUtc.HasValue);
        if (anyMetadata && !alreadyBootstrapped)
        {
            return new ValidationState(
                new FaceModelBootstrapResult(
                    false, "DatabaseMetadataConflict", rows.Count,
                    registry.Models.Count, registry.EncodingCount,
                    ["Existing lifecycle metadata differs from the registry."]),
                rows,
                registry);
        }

        return new ValidationState(
            new FaceModelBootstrapResult(
                true,
                alreadyBootstrapped ? "AlreadyBootstrapped" : "Validated",
                rows.Count,
                registry.Models.Count,
                registry.EncodingCount,
                []),
            rows,
            registry);
    }

    private async Task<FaceRegistrySnapshot> GetRegistryAsync(
        CancellationToken cancellationToken)
    {
        var response = await _runtime.GetModelsAsync(cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new FaceRuntimeUnavailableException(
                FaceRuntimeFailureKind.UnexpectedFailure,
                "Face Runtime registry request failed.",
                new InvalidOperationException("Non-success registry response."));
        try
        {
            var payload = JsonSerializer.Deserialize<FaceRegistrySnapshot>(
                response.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return payload ?? throw new JsonException();
        }
        catch (JsonException ex)
        {
            throw new FaceRuntimeUnavailableException(
                FaceRuntimeFailureKind.UnexpectedFailure,
                "Face Runtime registry response is invalid.",
                ex);
        }
    }

    private static string Reconcile(
        EmployeeFaceModel row,
        FaceRegistryModel? runtimeModel)
    {
        if (runtimeModel is null) return "MissingInRuntime";
        if (runtimeModel.SubjectId != row.EmployeeId.ToString(
                System.Globalization.CultureInfo.InvariantCulture))
            return "SubjectMismatch";
        if (row.ModelChecksum is null || !row.EncodingCount.HasValue)
            return "DatabaseMetadataMissing";
        if (row.ModelChecksum != runtimeModel.Checksum) return "ChecksumMismatch";
        if (row.EncodingCount != runtimeModel.EncodingCount) return "EncodingCountMismatch";
        return "Synced";
    }

    private static FaceModelBootstrapResult Failed(string status, string issue) =>
        new(false, status, 0, 0, 0, [issue]);

    private static FaceRegistrySnapshot EmptyRegistry() =>
        new(0, 0, 0, 0, []);

    private sealed record ValidationState(
        FaceModelBootstrapResult Result,
        List<EmployeeFaceModel> Rows,
        FaceRegistrySnapshot Registry);
}
