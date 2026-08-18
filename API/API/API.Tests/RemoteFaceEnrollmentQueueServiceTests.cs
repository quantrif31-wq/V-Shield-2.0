using API.Data;
using API.Models;
using API.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.Tests;

public sealed class RemoteFaceEnrollmentQueueServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"rfeq_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Employee SeedEmployee(ApplicationDbContext db, int id)
    {
        var employee = new Employee { EmployeeId = id, FullName = "Nguyen Van A", Email = $"e{id}@x.com", Status = true };
        db.Employees.Add(employee);
        db.SaveChanges();
        return employee;
    }

    private static RemoteFaceEnrollmentQueueService CreateService(ApplicationDbContext db) =>
        new(db, NullLogger<RemoteFaceEnrollmentQueueService>.Instance);

    private static RemoteFaceEnrollmentJob SeedJob(ApplicationDbContext db, int employeeId, string status, DateTime? completedAt = null)
    {
        var job = new RemoteFaceEnrollmentJob
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Status = status,
            CompletedAtUtc = completedAt,
            Employee = db.Employees.Find(employeeId)!
        };
        db.RemoteFaceEnrollmentJobs.Add(job);
        db.SaveChanges();
        return job;
    }

    [Fact]
    public async Task CompleteAsync_MarksJobCompleted_ArchivesOldAndCreatesNewModel()
    {
        var db = CreateDb();
        SeedEmployee(db, 1);
        var job = SeedJob(db, 1, RemoteFaceEnrollmentJobStatuses.Processing);
        db.EmployeeFaceModels.Add(new EmployeeFaceModel
        {
            EmployeeId = 1,
            ModelFileName = "old.bin",
            ModelPath = "models/active/old.bin",
            Status = FaceModelLifecycleStatuses.Active,
            Version = 1
        });
        db.SaveChanges();

        var service = CreateService(db);
        await service.CompleteAsync(job.Id, "node-a", "new.bin", "chk", 3, "{\"x\":1}", CancellationToken.None);

        var updatedJob = db.RemoteFaceEnrollmentJobs.Single();
        Assert.Equal(RemoteFaceEnrollmentJobStatuses.Completed, updatedJob.Status);
        Assert.Equal("new.bin", updatedJob.ResultModelFileName);
        Assert.Equal("{\"x\":1}", updatedJob.TemplateContent);

        Assert.Equal(FaceModelLifecycleStatuses.Archived, db.EmployeeFaceModels.Single(m => m.ModelFileName == "old.bin").Status);
        var fresh = db.EmployeeFaceModels.Single(m => m.ModelFileName == "new.bin");
        Assert.Equal(FaceModelLifecycleStatuses.Active, fresh.Status);
        Assert.Equal(2, fresh.Version);
        Assert.Equal("models/active/new.bin", fresh.ModelPath);
    }

    [Fact]
    public async Task CompleteAsync_JobNotFound_Throws()
    {
        var db = CreateDb();
        var service = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(Guid.NewGuid(), "node-a", "new.bin", null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task FailAsync_MarksJobFailed()
    {
        var db = CreateDb();
        SeedEmployee(db, 1);
        var job = SeedJob(db, 1, RemoteFaceEnrollmentJobStatuses.Processing);

        var service = CreateService(db);
        await service.FailAsync(job.Id, "node-a", "AI_TIMEOUT", "timeout", CancellationToken.None);

        var updated = db.RemoteFaceEnrollmentJobs.Single();
        Assert.Equal(RemoteFaceEnrollmentJobStatuses.Failed, updated.Status);
        Assert.Equal("AI_TIMEOUT", updated.FailureCode);
        Assert.NotNull(updated.CompletedAtUtc);
    }

    [Fact]
    public async Task FailAsync_JobNotFound_Throws()
    {
        var db = CreateDb();
        var service = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FailAsync(Guid.NewGuid(), "node-a", "X", "y", CancellationToken.None));
    }

    [Fact]
    public async Task GetActiveTemplatesAsync_ReturnsLatestPerEmployee()
    {
        var db = CreateDb();
        SeedEmployee(db, 1);
        var older = SeedJob(db, 1, RemoteFaceEnrollmentJobStatuses.Completed, DateTime.UtcNow.AddHours(-2));
        older.ResultModelFileName = "old.bin";
        older.TemplateContent = "{}";
        older.ResultChecksum = "c1";
        var newer = SeedJob(db, 1, RemoteFaceEnrollmentJobStatuses.Completed, DateTime.UtcNow);
        newer.ResultModelFileName = "new.bin";
        newer.TemplateContent = "{\"n\":2}";
        newer.ResultChecksum = "c2";
        db.SaveChanges();

        var service = CreateService(db);
        var templates = await service.GetActiveTemplatesAsync(CancellationToken.None);

        var template = Assert.Single(templates);
        Assert.Equal(1, template.EmployeeId);
        Assert.Equal("new.bin", template.ModelFileName);
    }

    [Fact]
    public async Task GetActiveTemplatesAsync_SkipsIncompleteJobs()
    {
        var db = CreateDb();
        SeedEmployee(db, 1);
        SeedJob(db, 1, RemoteFaceEnrollmentJobStatuses.Pending);
        var failed = SeedJob(db, 1, RemoteFaceEnrollmentJobStatuses.Failed);
        failed.ResultModelFileName = "f.bin";
        failed.TemplateContent = "{}";
        db.SaveChanges();

        var service = CreateService(db);
        var templates = await service.GetActiveTemplatesAsync(CancellationToken.None);

        Assert.Empty(templates);
    }
}