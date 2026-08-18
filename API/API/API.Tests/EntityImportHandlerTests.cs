using API.Data;
using API.DTOs;
using API.Models;
using API.Services.ImportExport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class EntityImportHandlerTests
{
    private static int _databaseCounter;

    private static (ServiceProvider provider, DepartmentImportHandler dept, CompanyImportHandler company, PositionImportHandler position) Build()
    {
        var databaseName = $"entities-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName, root)
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return (provider,
            new DepartmentImportHandler(scopeFactory),
            new CompanyImportHandler(scopeFactory),
            new PositionImportHandler(scopeFactory));
    }

    private static ImportValidationContext Context() => new()
    {
        ServiceProvider = new ServiceCollection().BuildServiceProvider(),
        SkipDuplicates = true
    };

    [Fact]
    public async Task Department_ValidateAndCreate()
    {
        var (provider, dept, _, _) = Build();
        await using (provider)
        {
            var missing = await dept.ValidateRowAsync(
                new Dictionary<string, object?> { ["Name"] = "" }, 1, Context());
            Assert.Single(missing);

            var created = await dept.CreateEntityAsync(
                new Dictionary<string, object?> { ["Name"] = "Phòng Kế toán" }, Context());
            Assert.IsType<Department>(created);

            var valid = await dept.ValidateRowAsync(
                new Dictionary<string, object?> { ["Name"] = "Phòng Kế toán" }, 1, Context());
            var dup = Assert.Single(valid);
            Assert.Contains("đã tồn tại", dup.Message);

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Single(await db.Departments.ToListAsync());
        }
    }

    [Fact]
    public async Task Department_ExportAndDictionary()
    {
        var (provider, dept, _, _) = Build();
        await using (provider)
        {
            using var seedScope = provider.CreateScope();
            var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Departments.Add(new Department { Name = "Phòng CNTT" });
            await db.SaveChangesAsync();

            var exported = await dept.ExportDataAsync(new ExportRequest());
            var item = Assert.Single(exported);
            Assert.Equal("Phòng CNTT", item["Name"]);

            var dict = await dept.EntityToDictionaryAsync(new Department { Name = "X", DepartmentId = 5 });
            Assert.Equal(5, dict["DepartmentId"]);
            Assert.Equal("X", dict["Name"]);
        }
    }

    [Fact]
    public void Department_TemplateFieldsAndMetadata()
    {
        var (provider, dept, _, _) = Build();
        using (provider)
        {
            Assert.Equal("Department", dept.EntityType);
            Assert.Equal("Phòng ban", dept.DisplayName);
            var field = Assert.Single(dept.GetTemplateFields());
            Assert.Equal("Name", field.FieldName);
            Assert.True(field.IsRequired);
        }
    }

    [Fact]
    public async Task Company_ValidateAndCreate()
    {
        var (provider, _, company, _) = Build();
        await using (provider)
        {
            var missing = await company.ValidateRowAsync(
                new Dictionary<string, object?> { ["Name"] = "A", ["Code"] = "" }, 1, Context());
            Assert.Single(missing);

            var created = await company.CreateEntityAsync(
                new Dictionary<string, object?> { ["Name"] = "ACME", ["Code"] = "ACM", ["IsActive"] = "false" }, Context());
            var c = Assert.IsType<Company>(created);
            Assert.False(c.IsActive);

            var dup = await company.ValidateRowAsync(
                new Dictionary<string, object?> { ["Name"] = "ACME2", ["Code"] = "ACM" }, 1, Context());
            Assert.Contains(dup, e => e.Message.Contains("đã tồn tại"));

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Single(await db.Companies.ToListAsync());
        }
    }

    [Fact]
    public async Task Company_ExportAndDictionary()
    {
        var (provider, _, company, _) = Build();
        await using (provider)
        {
            using var seedScope = provider.CreateScope();
            var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Companies.Add(new Company { Name = "ACME", Code = "ACM" });
            await db.SaveChangesAsync();

            var exported = await company.ExportDataAsync(new ExportRequest());
            var item = Assert.Single(exported);
            Assert.Equal("ACM", item["Code"]);

            var dict = await company.EntityToDictionaryAsync(new Company { Name = "ACME", Code = "ACM", IsActive = true, CompanyId = 9 });
            Assert.Equal("ACME", dict["Name"]);
            Assert.Equal("ACM", dict["Code"]);
            Assert.Equal(true, dict["IsActive"]);
        }
    }

    [Fact]
    public void Company_TemplateFieldsAndMetadata()
    {
        var (provider, _, company, _) = Build();
        using (provider)
        {
            Assert.Equal("Company", company.EntityType);
            Assert.Equal("Công ty", company.DisplayName);
            var fields = company.GetTemplateFields();
            Assert.Contains(fields, f => f.FieldName == "Code" && f.IsRequired);
            Assert.Contains(fields, f => f.FieldName == "IsActive" && f.DataType == "bool");
        }
    }

    [Fact]
    public async Task Position_ValidateAndCreate()
    {
        var (provider, _, _, position) = Build();
        await using (provider)
        {
            var missing = await position.ValidateRowAsync(
                new Dictionary<string, object?> { ["Name"] = " " }, 1, Context());
            Assert.Single(missing);

            await position.CreateEntityAsync(
                new Dictionary<string, object?> { ["Name"] = "Nhân viên" }, Context());

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Single(await db.Positions.ToListAsync());
        }
    }

    [Fact]
    public void Position_TemplateFields()
    {
        var (provider, _, _, position) = Build();
        using (provider)
        {
            Assert.Equal("Position", position.EntityType);
            var field = Assert.Single(position.GetTemplateFields());
            Assert.Equal("Name", field.FieldName);
        }
    }
}
