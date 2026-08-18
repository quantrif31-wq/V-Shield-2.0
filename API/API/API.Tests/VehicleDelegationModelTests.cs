using API.Data;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public class VehicleDelegationModelTests
{
    [Fact]
    public void VehicleDelegation_EmployeeRelations_ShouldNotCascadeDelete()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        using var dbContext = new ApplicationDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(VehicleDelegation));
        entityType.Should().NotBeNull();

        var employeeForeignKeys = entityType!
            .GetForeignKeys()
            .Where(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Employee))
            .ToArray();

        employeeForeignKeys.Should().HaveCount(2);
        employeeForeignKeys.Should().OnlyContain(foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void VehicleDelegation_VehicleRelation_ShouldRemainCascadeDelete()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        using var dbContext = new ApplicationDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(VehicleDelegation));
        entityType.Should().NotBeNull();

        var vehicleForeignKey = entityType!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Vehicle));

        vehicleForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }
}
