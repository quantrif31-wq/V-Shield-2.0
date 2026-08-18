using System.Text;
using API.Data;
using API.DTOs;
using API.Models;
using API.Services.ImportExport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class ImportExportServiceTests
{
    private static int _databaseCounter;

    private static (ServiceProvider provider, ImportExportService service, EmployeeImportHandler handler) Build()
    {
        var databaseName = $"importsvc-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName, root)
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        var provider = services.BuildServiceProvider();
        var handler = new EmployeeImportHandler(provider.GetRequiredService<IServiceScopeFactory>());
        var parserFactory = new FileParserFactory(new IFileParser[] { new CsvFileParser() });
        var service = new ImportExportService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            parserFactory,
            new IEntityImportHandler[] { handler });
        return (provider, service, handler);
    }

    private static Stream Csv(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void GetSupportedEntities_ListsHandlers()
    {
        var (provider, service, _) = Build();
        using (provider)
        {
            var entities = service.GetSupportedEntities();
            var entity = Assert.Single(entities);
            Assert.Equal("Employee", entity.EntityType);
            Assert.NotEmpty(entity.Fields);
        }
    }

    [Fact]
    public async Task ImportAsync_CreatesEntitiesAndHistory()
    {
        var (provider, service, _) = Build();
        await using (provider)
        {
            var csv = Csv("FullName,Status\nNguyen Van A,true\nNguyen Van B,false\n");
            var response = await service.ImportAsync(
                "Employee", csv, "employees.csv", "text/csv", new ImportRequest(), 1);

            Assert.Equal(ImportExportConstants.StatusCompleted, response.Status);
            Assert.Equal(2, response.SuccessCount);
            Assert.Empty(response.Errors);

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(2, await db.Employees.CountAsync());
            Assert.Single(await db.ImportExportHistories.ToListAsync());
        }
    }

    [Fact]
    public async Task ImportAsync_ReportsRowErrors()
    {
        var (provider, service, _) = Build();
        await using (provider)
        {
            var csv = Csv("FullName\n   \nNguyen Van A\n");
            var response = await service.ImportAsync(
                "Employee", csv, "employees.csv", "text/csv", new ImportRequest(), 1);

            Assert.Equal(ImportExportConstants.StatusPartialSuccess, response.Status);
            Assert.Equal(1, response.SuccessCount);
            Assert.Single(response.Errors);
            Assert.Equal("FullName", response.Errors[0].Column);
        }
    }

    [Fact]
    public async Task ImportAsync_Throws_ForUnsupportedEntity()
    {
        var (provider, service, _) = Build();
        await using (provider)
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => service.ImportAsync(
                "Unknown", Csv("A\n1\n"), "x.csv", "text/csv", new ImportRequest(), 1));
        }
    }

    [Fact]
    public async Task ExportAsync_GeneratesFileAndHistory()
    {
        var (provider, service, _) = Build();
        await using (provider)
        {
            using var seedScope = provider.CreateScope();
            var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Employees.Add(new Employee { FullName = "Export Test", Status = true });
            await db.SaveChangesAsync();

            var response = await service.ExportAsync("Employee", new ExportRequest { Format = "csv" }, 1);

            Assert.Equal(ImportExportConstants.StatusCompleted, response.Status);
            Assert.Equal(1, response.TotalRows);
            Assert.EndsWith(".csv", response.FileName);
            Assert.Contains("/api/import-export/download/", response.DownloadUrl);
        }
    }

    [Fact]
    public async Task DownloadTemplateAsync_ReturnsCsvWithHeaders()
    {
        var (provider, service, _) = Build();
        await using (provider)
        {
            using var stream = await service.DownloadTemplateAsync("Employee", "csv");
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var text = await reader.ReadToEndAsync();
            Assert.Contains("FullName", text);
            Assert.Contains("Status", text);
        }
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersAndMaps()
    {
        var (provider, service, _) = Build();
        await using (provider)
        {
            using var seedScope = provider.CreateScope();
            var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ImportExportHistories.AddRange(
                new ImportExportHistory
                {
                    Id = Guid.NewGuid(),
                    OperationType = ImportExportConstants.OperationImport,
                    EntityType = "Employee",
                    FileName = "a.csv",
                    FileFormat = "csv",
                    Status = ImportExportConstants.StatusCompleted,
                    PerformedAt = DateTime.UtcNow.AddMinutes(-1),
                },
                new ImportExportHistory
                {
                    Id = Guid.NewGuid(),
                    OperationType = ImportExportConstants.OperationExport,
                    EntityType = "Department",
                    FileName = "b.csv",
                    FileFormat = "csv",
                    Status = ImportExportConstants.StatusCompleted,
                    PerformedAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();

            var filtered = await service.GetHistoryAsync(entityType: "Employee");
            var item = Assert.Single(filtered);
            Assert.Equal("a.csv", item.FileName);

            var all = await service.GetHistoryAsync();
            Assert.Equal(2, all.Count);
        }
    }

    [Fact]
    public async Task GetHistoryByIdAsync_ReturnsNull_WhenMissing()
    {
        var (provider, service, _) = Build();
        await using (provider)
        {
            Assert.Null(await service.GetHistoryByIdAsync(Guid.NewGuid()));
        }
    }

    [Fact]
    public async Task PreviewImportAsync_ValidatesWithoutCreating()
    {
        var (provider, service, _) = Build();
        await using (provider)
        {
            var csv = Csv("FullName\nNguyen Van A\n");
            var response = await service.PreviewImportAsync("Employee", csv, "x.csv", "text/csv");

            Assert.Equal("PreviewReady", response.Status);
            Assert.Equal(1, response.TotalRows);

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Empty(await db.Employees.ToListAsync());
        }
    }

    [Fact]
    public void GetJobStatusAsync_ReturnsNull_ForUnknownJob()
    {
        var (provider, service, _) = Build();
        using (provider)
        {
            Assert.Null(service.GetJobStatusAsync(Guid.NewGuid()).GetAwaiter().GetResult());
        }
    }
}
