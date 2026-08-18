using API.Data;
using API.DTOs;
using API.Models;
using API.Services.ImportExport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class EmployeeImportHandlerTests
{
    private static int _databaseCounter;

    private static (ServiceProvider provider, EmployeeImportHandler handler) Build()
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = $"import-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName, root)
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        var provider = services.BuildServiceProvider();
        var handler = new EmployeeImportHandler(provider.GetRequiredService<IServiceScopeFactory>());
        return (provider, handler);
    }

    private static ImportValidationContext Context() => new()
    {
        ServiceProvider = new ServiceCollection().BuildServiceProvider(),
        SkipDuplicates = true
    };

    [Fact]
    public async Task ValidateRowAsync_RequiresFullName()
    {
        var (provider, handler) = Build();
        await using (provider)
        {
            var errors = await handler.ValidateRowAsync(
                new Dictionary<string, object?> { ["FullName"] = "" }, 1, Context());
            var error = Assert.Single(errors);
            Assert.Equal("FullName", error.Column);
            Assert.Contains("không được để trống", error.Message);
        }
    }

    [Fact]
    public async Task ValidateRowAsync_AcceptsValidRow()
    {
        var (provider, handler) = Build();
        await using (provider)
        {
            var errors = await handler.ValidateRowAsync(
                new Dictionary<string, object?> { ["FullName"] = "Nguyen Van A" }, 1, Context());
            Assert.Empty(errors);
        }
    }

    [Fact]
    public async Task CreateEntityAsync_CreatesEmployeeWithMappedDepartmentAndPosition()
    {
        var (provider, handler) = Build();
        await using (provider)
        {
            var entity = await handler.CreateEntityAsync(new Dictionary<string, object?>
            {
                ["FullName"] = "Nguyen Van B",
                ["Email"] = "b@test.com",
                ["Phone"] = "0901234567",
                ["DepartmentName"] = "Phòng IT",
                ["PositionName"] = "Dev",
                ["Status"] = "true",
            }, Context());

            var employee = Assert.IsType<Employee>(entity);
            Assert.Equal("Nguyen Van B", employee.FullName);
            Assert.True(employee.Status);

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dept = await db.Departments.SingleAsync(d => d.Name == "Phòng IT");
            var pos = await db.Positions.SingleAsync(p => p.Name == "Dev");
            Assert.Equal(dept.DepartmentId, employee.DepartmentId);
            Assert.Equal(pos.PositionId, employee.PositionId);
        }
    }

    [Fact]
    public async Task CreateEntityAsync_ReusesExistingDepartment()
    {
        var (provider, handler) = Build();
        await using (provider)
        {
            using var seedScope = provider.CreateScope();
            var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Departments.Add(new Department { Name = "Phòng IT" });
            await db.SaveChangesAsync();

            await handler.CreateEntityAsync(new Dictionary<string, object?>
            {
                ["FullName"] = "Nguyen Van C",
                ["DepartmentName"] = "Phòng IT",
            }, Context());

            var deptCount = await db.Departments.CountAsync(d => d.Name == "Phòng IT");
            Assert.Equal(1, deptCount);
        }
    }

    [Fact]
    public async Task ExportDataAsync_FiltersByStatusAndDepartment()
    {
        var (provider, handler) = Build();
        await using (provider)
        {
            using var seedScope = provider.CreateScope();
            var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dept = new Department { Name = "Phòng IT" };
            db.Departments.Add(dept);
            db.Employees.AddRange(
                new Employee { FullName = "A", Status = true, Department = dept },
                new Employee { FullName = "B", Status = false, Department = dept });
            await db.SaveChangesAsync();

            var byStatus = await handler.ExportDataAsync(new ExportRequest
            {
                Filters = new Dictionary<string, string> { ["status"] = "true" }
            });
            var onlyTrue = Assert.Single(byStatus);
            Assert.Equal("A", onlyTrue["FullName"]);

            var byDept = await handler.ExportDataAsync(new ExportRequest
            {
                Filters = new Dictionary<string, string> { ["departmentName"] = "IT" }
            });
            Assert.Equal(2, byDept.Count);
        }
    }

    [Fact]
    public async Task EntityToDictionaryAsync_ExportsAllFields()
    {
        var (provider, handler) = Build();
        await using (provider)
        {
            var employee = new Employee
            {
                EmployeeId = 1,
                FullName = "Nguyen Van D",
                Email = "d@test.com",
                Phone = "0987654321",
                Status = true,
                Department = new Department { Name = "Phòng KD" },
                Position = new Position { Name = "Sales" }
            };
            var dict = await handler.EntityToDictionaryAsync(employee);
            Assert.Equal("Nguyen Van D", dict["FullName"]);
            Assert.Equal("Phòng KD", dict["DepartmentName"]);
            Assert.Equal("Sales", dict["PositionName"]);
            Assert.Equal(true, dict["Status"]);
        }
    }

    [Fact]
    public async Task TemplateFields_ExposeExpectedContract()
    {
        var (provider, handler) = Build();
        await using (provider)
        {
            var fields = handler.GetTemplateFields();
            Assert.Contains(fields, f => f.FieldName == "FullName" && f.IsRequired);
            Assert.Contains(fields, f => f.FieldName == "Status" && f.DataType == "bool");
            Assert.Equal("Employee", handler.EntityType);
            Assert.Equal("Nhân viên", handler.DisplayName);
        }
    }
}
