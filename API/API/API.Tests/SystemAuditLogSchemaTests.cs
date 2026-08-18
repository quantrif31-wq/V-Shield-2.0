using API.Data;
using API.Models;
using API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class SystemAuditLogSchemaTests
{
    [Fact]
    public void Model_UsesCanonicalActionTypeLength()
    {
        using var db = Database();
        var property = db.Model.FindEntityType(typeof(SystemAuditLog))!
            .FindProperty(nameof(SystemAuditLog.ActionType))!;

        Assert.Equal(SystemAuditLogLimits.ActionTypeMaxLength, property.GetMaxLength());
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void CredentialAndBindingActions_FitCanonicalLimitWithoutTruncation()
    {
        Assert.All(
            SystemAuditActions.CredentialAndBindingActions,
            action => Assert.InRange(action.Length, 1, SystemAuditLogLimits.ActionTypeMaxLength));
        Assert.Contains(SystemAuditActions.FaceCredentialBindingCreated,
            SystemAuditActions.CredentialAndBindingActions);
        Assert.Equal("FaceCredentialBindingCreated",
            SystemAuditActions.FaceCredentialBindingCreated);
    }

    private static ApplicationDbContext Database() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"audit-schema-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)).Options);
}
