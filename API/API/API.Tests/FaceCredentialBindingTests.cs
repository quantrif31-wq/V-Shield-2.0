using API.Data;
using API.Models;
using API.Services;
using API.Services.AccessCredentials;
using API.Services.Audit;
using API.Services.FaceCredentialBindings;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class FaceCredentialBindingTests
{
    private static readonly DateTime EventTime =
        new(2026, 7, 28, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_RequiresSameEmployeeFaceCredentialAndIsIdempotent()
    {
        await using var db = Database();
        db.Employees.AddRange(Employee(1), Employee(2));
        db.AccessCredentials.AddRange(
            Credential(10, 1, AccessCredentialTypes.FaceBiometric),
            Credential(11, 2, AccessCredentialTypes.FaceBiometric),
            Credential(12, 1, AccessCredentialTypes.DynamicQr),
            Credential(13, 1, AccessCredentialTypes.Card));
        await db.SaveChangesAsync();
        var service = Service(db);

        var created = await service.CreateAsync(new(1, 10, "approved"), default);
        var repeated = await service.CreateAsync(new(1, 10, "ignored"), default);
        Assert.Equal(created.Id, repeated.Id);
        Assert.Equal(EmployeeFaceCredentialBindingStatuses.Active, created.BindingStatus);

        var ownership = await Assert.ThrowsAsync<FaceCredentialBindingDomainException>(
            () => service.CreateAsync(new(1, 11, null), default));
        Assert.Equal("FaceCredentialOwnershipMismatch", ownership.Code);
        var qr = await Assert.ThrowsAsync<FaceCredentialBindingDomainException>(
            () => service.CreateAsync(new(1, 12, null), default));
        Assert.Equal("FaceCredentialTypeMismatch", qr.Code);
        var card = await Assert.ThrowsAsync<FaceCredentialBindingDomainException>(
            () => service.CreateAsync(new(1, 13, null), default));
        Assert.Equal("FaceCredentialTypeMismatch", card.Code);
        Assert.Empty(await db.AccessRules.ToListAsync());
        Assert.Empty(await db.EmployeeAccessPermissions.ToListAsync());
        Assert.Empty(await db.AccessDecisions.ToListAsync());
    }

    [Fact]
    public async Task Create_AssignsIdentityBeforeBuildingAuditAndPreservesExplicitActor()
    {
        await using var db = Database();
        db.Employees.Add(Employee(1));
        db.AccessCredentials.Add(Credential(10, 1, AccessCredentialTypes.FaceBiometric));
        await db.SaveChangesAsync();

        var created = await Service(db).CreateAsync(
            new(1, 10, "approved", 1, "admin"),
            default);

        var audit = await db.SystemAuditLogs.SingleAsync(
            x => x.ActionType == SystemAuditActions.FaceCredentialBindingCreated);
        Assert.True(created.Id > 0);
        Assert.Equal(created.Id.ToString(), audit.EntityId);
        Assert.Equal(1, audit.UserId);
        Assert.Equal("admin", audit.Username);
        Assert.Contains($"\"bindingId\":{created.Id}", audit.NewValuesJson);
    }

    [Theory]
    [InlineData(AccessCredentialStatuses.Pending, null, "FaceCredentialInactive")]
    [InlineData(AccessCredentialStatuses.Inactive, null, "FaceCredentialInactive")]
    [InlineData(AccessCredentialStatuses.Revoked, "2026-07-28T07:00:00Z", "FaceCredentialInactive")]
    public async Task Create_RejectsNonActiveCredential(
        string status, string? revokedAt, string expected)
    {
        await using var db = Database();
        db.Employees.Add(Employee(1));
        var credential = Credential(10, 1, AccessCredentialTypes.FaceBiometric);
        credential.Status = status;
        credential.RevokedAtUtc = revokedAt is null ? null : DateTime.Parse(revokedAt).ToUniversalTime();
        db.AccessCredentials.Add(credential);
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<FaceCredentialBindingDomainException>(
            () => Service(db).CreateAsync(new(1, 10, null), default));
        Assert.Equal(expected, error.Code);
    }

    [Fact]
    public async Task Create_RejectsExpiredAndMissingRows()
    {
        await using var db = Database();
        db.Employees.Add(Employee(1));
        var expired = Credential(10, 1, AccessCredentialTypes.FaceBiometric);
        expired.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        db.AccessCredentials.Add(expired);
        await db.SaveChangesAsync();
        Assert.Equal("FaceCredentialInactive",
            (await Assert.ThrowsAsync<FaceCredentialBindingDomainException>(
                () => Service(db).CreateAsync(new(1, 10, null), default))).Code);
        Assert.Equal("FaceCredentialBindingEmployeeMissing",
            (await Assert.ThrowsAsync<FaceCredentialBindingDomainException>(
                () => Service(db).CreateAsync(new(99, 10, null), default))).Code);
        Assert.Equal("FaceCredentialBindingCredentialMissing",
            (await Assert.ThrowsAsync<FaceCredentialBindingDomainException>(
                () => Service(db).CreateAsync(new(1, 99, null), default))).Code);
    }

    [Fact]
    public async Task Resolve_UsesOccurredAtAndPreservesPreRevokeHistory()
    {
        await using var db = Database();
        db.Employees.Add(Employee(1));
        db.AccessCredentials.Add(Credential(10, 1, AccessCredentialTypes.FaceBiometric));
        db.EmployeeFaceCredentialBindings.Add(new EmployeeFaceCredentialBinding
        {
            Id = 20, EmployeeId = 1, AccessCredentialId = 10,
            Status = EmployeeFaceCredentialBindingStatuses.Revoked,
            ActivatedAtUtc = EventTime, RevokedAtUtc = EventTime.AddHours(2),
            CreatedAtUtc = EventTime
        });
        await db.SaveChangesAsync();
        var service = Service(db);

        Assert.Equal("EnterpriseFaceCredentialBindingMissing",
            (await service.ResolveAsync(1, EventTime.AddTicks(-1), default)).ReasonCode);
        Assert.NotNull((await service.ResolveAsync(1, EventTime, default)).Context);
        Assert.NotNull((await service.ResolveAsync(1, EventTime.AddHours(1), default)).Context);
        Assert.Equal("EnterpriseFaceCredentialBindingRevoked",
            (await service.ResolveAsync(1, EventTime.AddHours(2), default)).ReasonCode);
    }

    [Fact]
    public async Task Resolve_DoesNotApplyCredentialCreatedAfterEventRetroactively()
    {
        await using var db = Database();
        db.Employees.Add(Employee(1));
        var credential = Credential(10, 1, AccessCredentialTypes.FaceBiometric);
        credential.CreatedAtUtc = EventTime.AddMinutes(1);
        db.AccessCredentials.Add(credential);
        db.EmployeeFaceCredentialBindings.Add(new EmployeeFaceCredentialBinding
        {
            Id = 20, EmployeeId = 1, AccessCredentialId = 10,
            Status = EmployeeFaceCredentialBindingStatuses.Active,
            ActivatedAtUtc = EventTime.AddMinutes(-1), CreatedAtUtc = EventTime.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var result = await Service(db).ResolveAsync(1, EventTime, default);
        Assert.Null(result.Context);
        Assert.Equal("EnterpriseCredentialInactive", result.ReasonCode);
    }

    [Fact]
    public async Task Revoke_DoesNotMutateCredentialOrModels()
    {
        await using var db = Database();
        db.Employees.Add(Employee(1));
        db.AccessCredentials.Add(Credential(10, 1, AccessCredentialTypes.FaceBiometric));
        db.EmployeeFaceModels.Add(new EmployeeFaceModel
        {
            Id = 1, EmployeeId = 1, Version = 1, Status = FaceModelLifecycleStatuses.Active,
            ModelFileName = "model.pkl", ModelPath = "active/model.pkl",
            ModelChecksum = new string('a', 64)
        });
        var binding = new EmployeeFaceCredentialBinding
        {
            Id = 20, EmployeeId = 1, AccessCredentialId = 10,
            Status = EmployeeFaceCredentialBindingStatuses.Active,
            ActivatedAtUtc = EventTime, CreatedAtUtc = EventTime,
            RowVersion = [1]
        };
        db.EmployeeFaceCredentialBindings.Add(binding);
        await db.SaveChangesAsync();
        var rowVersion = Convert.ToBase64String(binding.RowVersion);

        var result = await Service(db).RevokeAsync(20, new("admin revoke", rowVersion), default);
        Assert.Equal(EmployeeFaceCredentialBindingStatuses.Revoked, result.BindingStatus);
        Assert.Equal(AccessCredentialStatuses.Active, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Equal(FaceModelLifecycleStatuses.Active, (await db.EmployeeFaceModels.SingleAsync()).Status);
        Assert.Empty(await db.AccessDecisions.ToListAsync());
    }

    [Fact]
    public void Model_UsesNoActionRowVersionAndFilteredUniqueIndexes()
    {
        using var db = Database();
        var entity = db.Model.FindEntityType(typeof(EmployeeFaceCredentialBinding))!;
        Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior));
        Assert.True(entity.FindProperty(nameof(EmployeeFaceCredentialBinding.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique &&
            x.GetDatabaseName() == "UX_EmployeeFaceCredentialBindings_ActiveEmployee" &&
            x.GetFilter() == "[Status] = 'Active'");
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique &&
            x.GetDatabaseName() == "UX_EmployeeFaceCredentialBindings_ActiveCredential" &&
            x.GetFilter() == "[Status] = 'Active'");
    }

    private static Employee Employee(int id) => new()
    {
        EmployeeId = id, FullName = $"Employee {id}", Status = true,
        LifecycleStatus = EmployeeLifecycleStates.Active
    };

    private static AccessCredential Credential(long id, int employeeId, string type) => new()
    {
        Id = id, EmployeeId = employeeId, CredentialType = type,
        Status = AccessCredentialStatuses.Active, CreatedAtUtc = EventTime.AddDays(-1)
    };

    private static ApplicationDbContext Database() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"face-binding-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)).Options);

    private static FaceCredentialBindingService Service(ApplicationDbContext db) => new(
        db, new AccessCredentialStateEvaluator(), new TestUser());

    private sealed class TestUser : ICurrentUserContext
    {
        public int? UserId => null;
        public string? Username => "test";
    }
}
