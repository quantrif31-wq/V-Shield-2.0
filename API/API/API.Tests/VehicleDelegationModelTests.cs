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
}
