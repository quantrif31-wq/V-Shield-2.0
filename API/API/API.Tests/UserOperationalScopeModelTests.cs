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
}
