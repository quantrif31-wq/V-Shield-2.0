using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.Models;
using API.Services;
using API.Services.AccessCredentials;
using API.Services.AccessPolicyComparison;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class AccessCredentialTests
{
    private static readonly DateTime Now = new(2026, 7, 28, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Pending", null, null, null, "Pending", "CredentialPending")]
    [InlineData("Inactive", null, null, null, "Inactive", "CredentialInactive")]
    [InlineData("Revoked", null, null, "2026-07-28T07:00:00Z", "Revoked", "CredentialRevoked")]
    [InlineData("Active", "2026-07-28T09:00:00Z", null, null, "NotYetEffective", "CredentialNotYetEffective")]
    [InlineData("Active", null, "2026-07-28T08:00:00Z", null, "Expired", "CredentialExpired")]
    [InlineData("Active", "2026-07-28T08:00:00Z", "2026-07-28T09:00:00Z", null, "Active", "CredentialActive")]
    public void StateEvaluator_IsDeterministicAndUsesDocumentedBoundaries(
        string stored, string? from, string? expires, string? revoked,
        string expectedStatus, string expectedReason)
    {
        var result = new AccessCredentialStateEvaluator().Evaluate(new(
            stored, Parse(from), Parse(expires), Parse(revoked), Now));
        Assert.Equal(expectedStatus, result.EffectiveStatus);
        Assert.Equal(expectedReason, result.ReasonCode);
    }

    [Fact]
    public void StateEvaluator_RejectsInvalidLifecycle()
    {
        var evaluator = new AccessCredentialStateEvaluator();
        Assert.Equal("CredentialInvalidLifecycle", evaluator.Evaluate(new(
            "Unknown", null, null, null, Now)).ReasonCode);
        Assert.Equal("CredentialInvalidLifecycle", evaluator.Evaluate(new(
            "Revoked", null, null, null, Now)).ReasonCode);
        Assert.Equal("CredentialInvalidLifecycle", evaluator.Evaluate(new(
            "Active", Now.AddHours(1), Now, null, Now)).ReasonCode);
    }

    [Fact]
    public void IdentifierProtector_UsesKeyedDomainSeparatedHashAndRedacts()
    {
        var protector = new AccessCredentialIdentifierProtector(new()
        {
            IdentifierHmacKey = "test-key-with-at-least-thirty-two-characters!"
        });
        var first = protector.Protect("Card", "ab-123456");
        var repeated = protector.Protect("Card", " AB-123456 ");
        Assert.Equal(first.Hash, repeated.Hash);
        Assert.EndsWith("3456", first.Mask);
        Assert.DoesNotContain("AB-12", first.Mask);
        Assert.NotEqual(Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes("AB-123456"))).ToLowerInvariant(), first.Hash);
        Assert.NotEqual(first.Hash, protector.Protect("FaceBiometric", "ab-123456").Hash);
    }

    [Fact]
    public void IdentifierProtector_FailsClosedWithoutLeakingKey()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new AccessCredentialIdentifierProtector(new()).Protect("Card", "secret-card"));
        Assert.DoesNotContain("secret-card", exception.Message);
    }

    [Fact]
    public async Task Service_CreatesRedactedCardWithoutPermissionsOrRules()
    {
        await using var db = Database();
        db.Employees.Add(Employee(1));
        await db.SaveChangesAsync();
        var service = Service(db);
        var result = await service.CreateAsync(new(
            1, "Card", "CARD-00001234", null, null, Now.AddDays(1), "Test"), default);
        var stored = await db.AccessCredentials.SingleAsync();
        Assert.Equal("Card", stored.CredentialType);
        Assert.NotNull(stored.IdentifierHash);
        Assert.DoesNotContain("CARD-00001234", stored.MaskedIdentifier);
        Assert.DoesNotContain("CARD-00001234",
            System.Text.Json.JsonSerializer.Serialize(result));
        Assert.Empty(await db.AccessRules.ToListAsync());
        Assert.Empty(await db.EmployeeAccessPermissions.ToListAsync());
        Assert.Empty(await db.AccessDecisions.ToListAsync());
    }

    [Fact]
    public async Task Service_RejectsDuplicateIdentifierAndWrongQrOwnership()
    {
        await using var db = Database();
        db.Employees.AddRange(Employee(1), Employee(2));
        db.EmployeeDynamicQrs.Add(new EmployeeDynamicQr
        {
            Id = 10, EmployeeId = 2, SecretKey = string.Empty, IsActive = true
        });
        await db.SaveChangesAsync();
        var service = Service(db);
        await service.CreateAsync(new(1, "Card", "same-card", null, null, null, null), default);
        var duplicate = await Assert.ThrowsAsync<AccessCredentialDomainException>(() =>
            service.CreateAsync(new(2, "Card", "same-card", null, null, null, null), default));
        Assert.Equal("CredentialDuplicateIdentifier", duplicate.Code);
        var ownership = await Assert.ThrowsAsync<AccessCredentialDomainException>(() =>
            service.CreateAsync(new(1, "DynamicQr", null, 10, null, null, null), default));
        Assert.Equal("CredentialOwnershipMismatch", ownership.Code);
        Assert.Null((await db.AccessCredentials.SingleAsync()).EmployeeDynamicQrId);
    }

    [Fact]
    public async Task Resolver_DoesNotGuessWhenMultipleCredentialsAreEffective()
    {
        await using var db = Database();
        db.Employees.Add(Employee(1));
        db.AccessCredentials.AddRange(
            new AccessCredential { EmployeeId = 1, CredentialType = "Card", Status = "Active" },
            new AccessCredential { EmployeeId = 1, CredentialType = "Card", Status = "Active" });
        await db.SaveChangesAsync();
        var result = await Service(db).ResolveActiveCredentialsForEmployeeAsync(
            1, "Card", Now, default);
        Assert.True(result.IsAmbiguous);
        Assert.Equal("EnterpriseCredentialAmbiguous", result.ReasonCode);
    }

    [Fact]
    public async Task CredentialAwareEvaluator_RejectsInactiveAndPreservesExplicitDeny()
    {
        await using var db = Database();
        db.AccessRules.AddRange(
            new AccessRule { AccessRuleId = 1, AccessLevelId = 1, SubjectType = "Employee",
                SubjectId = 1, AccessPointId = 10, CredentialType = "Card", AllowAccess = true },
            new AccessRule { AccessRuleId = 2, AccessLevelId = 1, SubjectType = "Employee",
                SubjectId = 1, AccessPointId = 10, CredentialType = "Card", AllowAccess = false });
        await db.SaveChangesAsync();
        var evaluator = new EnterpriseAccessPolicyEvaluator(db, new()
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh")
        });
        var input = new EnterprisePolicyEvaluationInput(1, 10, Now, null);
        var inactive = new AccessCredentialContext(
            1, 1, "Card", "Inactive", "Inactive", null, null, Now, "Canonical");
        Assert.Equal("EnterpriseCredentialInactive",
            (await evaluator.EvaluateAsync(input, inactive, default)).ReasonCode);
        var active = inactive with { StoredStatus = "Active", EffectiveStatus = "Active" };
        Assert.Equal("EnterpriseExplicitDenied",
            (await evaluator.EvaluateAsync(input, active, default)).ReasonCode);
        Assert.Equal("EnterpriseMissingCredentialContext",
            (await evaluator.EvaluateAsync(input, default)).ReasonCode);
    }

    [Fact]
    public void Model_HasRestrictOwnershipAndFilteredUniqueIndexes()
    {
        using var db = Database();
        var entity = db.Model.FindEntityType(typeof(AccessCredential))!;
        var employeeFk = entity.GetForeignKeys().Single(x =>
            x.Properties.Single().Name == nameof(AccessCredential.EmployeeId));
        Assert.Equal(DeleteBehavior.Restrict, employeeFk.DeleteBehavior);
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique &&
            x.GetFilter() == "[EmployeeDynamicQrId] IS NOT NULL");
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique &&
            x.GetDatabaseName() == "UX_AccessCredentials_ActiveFace_Employee");
    }

    private static DateTime? Parse(string? value) =>
        value is null ? null : DateTime.Parse(value).ToUniversalTime();
    private static Employee Employee(int id) => new()
    {
        EmployeeId = id, FullName = $"Employee {id}", Status = true,
        LifecycleStatus = EmployeeLifecycleStates.Active
    };
    private static ApplicationDbContext Database() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"credential-{Guid.NewGuid():N}").Options);
    private static AccessCredentialService Service(ApplicationDbContext db) => new(
        db, new AccessCredentialStateEvaluator(),
        new AccessCredentialIdentifierProtector(new()
        {
            IdentifierHmacKey = "test-key-with-at-least-thirty-two-characters!"
        }), new TestUser());

    private sealed class TestUser : ICurrentUserContext
    {
        public int? UserId => null;
        public string? Username => "test";
    }
}
