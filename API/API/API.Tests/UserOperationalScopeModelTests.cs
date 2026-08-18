using API.Data;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace API.Tests;

public class UserOperationalScopeModelTests
{
    [Fact]
    public void UserOperationalScope_UserRelation_ShouldNotCascadeDelete()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        using var dbContext = new ApplicationDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(UserOperationalScope));
        entityType.Should().NotBeNull();

        var userForeignKey = entityType!
            .GetForeignKeys()
            .Single(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(AppUser) &&
                foreignKey.Properties.Any(property => property.Name == nameof(UserOperationalScope.UserId)));

        userForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void UserOperationalScope_AuditAndLocationRelations_ShouldKeepSafeDeleteBehavior()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        using var dbContext = new ApplicationDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(UserOperationalScope));
        entityType.Should().NotBeNull();

        var foreignKeys = entityType!.GetForeignKeys().ToArray();

        foreignKeys.Single(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(AppUser) &&
                foreignKey.Properties.Any(property => property.Name == nameof(UserOperationalScope.CreatedByUserId)))
            .DeleteBehavior.Should().Be(DeleteBehavior.SetNull);

        foreignKeys.Where(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(Site) ||
                foreignKey.PrincipalEntityType.ClrType == typeof(Gate) ||
                foreignKey.PrincipalEntityType.ClrType == typeof(Lane) ||
                foreignKey.PrincipalEntityType.ClrType == typeof(SecurityZone))
            .Should()
            .OnlyContain(foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }
}
