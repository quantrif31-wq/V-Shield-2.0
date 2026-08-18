using API.Controllers;
using API.Data;
using API.Models;
using API.Services.FaceRecognition;
using API.Services;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class FaceEnrollmentContractTests
{
    [Fact]
    public void JobStates_AreCompleteAndDistinct()
    {
        var states = new[] {
            FaceEnrollmentJobStatuses.Pending, FaceEnrollmentJobStatuses.Processing,
            FaceEnrollmentJobStatuses.Prepared, FaceEnrollmentJobStatuses.Activating,
            FaceEnrollmentJobStatuses.Completed, FaceEnrollmentJobStatuses.Failed,
            FaceEnrollmentJobStatuses.Cancelled, FaceEnrollmentJobStatuses.RecoveryRequired
        };
        Assert.Equal(8, states.Distinct().Count());
        Assert.DoesNotContain(FaceEnrollmentJobStatuses.Completed, FaceEnrollmentJobStatuses.NonTerminal);
        Assert.DoesNotContain(FaceEnrollmentJobStatuses.Failed, FaceEnrollmentJobStatuses.NonTerminal);
    }

    [Fact]
    public void PublicCreateRequest_AcceptsOnlyManagedIdentifiers()
    {
        var properties = typeof(CreateFaceEnrollmentRequest).GetProperties().Select(p => p.Name).ToArray();
        Assert.Equal(new[] { "EmployeeId", "EmployeeFaceVideoId" }, properties);
    }

    [Fact]
    public void JobDto_RedactsCandidateAndFilesystemMetadata()
    {
        var names = typeof(FaceEnrollmentJobDto).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("CandidateReference", names);
        Assert.DoesNotContain("CandidateChecksum", names);
        Assert.DoesNotContain("SourceReference", names);
        Assert.DoesNotContain("ExpectedModelFileName", names);
        Assert.DoesNotContain("ModelPath", names);
    }

    [Fact]
    public void DatabaseModel_HasConcurrencyAndSafetyIndexes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(local);Database=model-only;Trusted_Connection=True;TrustServerCertificate=True")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        using var db = new ApplicationDbContext(options);
        var job = db.Model.FindEntityType(typeof(FaceEnrollmentJob))!;
        Assert.True(job.FindProperty(nameof(FaceEnrollmentJob.RowVersion))!.IsConcurrencyToken);
        var activeIndex = job.GetIndexes().Single(i =>
            i.GetDatabaseName() == "UX_FaceEnrollmentJobs_NonTerminalEmployee");
        Assert.True(activeIndex.IsUnique);
        Assert.Contains("Prepared", activeIndex.GetFilter());
        var model = db.Model.FindEntityType(typeof(EmployeeFaceModel))!;
        Assert.True(model.GetIndexes().Single(i =>
            i.GetDatabaseName() == "UX_EmployeeFaceModels_SourceEnrollmentJobId").IsUnique);
    }

    [Fact]
    public void EnrollmentRelations_UseRestrictDelete()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(local);Database=model-only;Trusted_Connection=True;TrustServerCertificate=True")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        using var db = new ApplicationDbContext(options);
        var foreignKeys = db.Model.FindEntityType(typeof(FaceEnrollmentJob))!.GetForeignKeys();
        Assert.All(foreignKeys, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
        var sourceJob = db.Model.FindEntityType(typeof(EmployeeFaceModel))!.GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(EmployeeFaceModel.SourceEnrollmentJobId)));
        Assert.Equal(DeleteBehavior.Restrict, sourceJob.DeleteBehavior);
    }

    [Fact]
    public void Controller_DoesNotExposeRuntimeProxyOrPathInputs()
    {
        var actions = typeof(FaceEnrollmentsController).GetMethods()
            .Where(m => m.DeclaringType == typeof(FaceEnrollmentsController))
            .Select(m => m.Name).ToArray();
        Assert.DoesNotContain("Proxy", actions);
        Assert.DoesNotContain("UploadModel", actions);
        Assert.DoesNotContain("Train", actions);
    }

    [Fact]
    public async Task Recovery_FinalizesRuntimeActivationWithoutCreatingAnotherVersion()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)).Options;
        await using var db = new ApplicationDbContext(options);
        db.Employees.Add(new Employee { EmployeeId = 7, FullName = "Recovery Test" });
        db.EmployeeFaceModels.Add(new EmployeeFaceModel {
            Id = 1, EmployeeId = 7, Version = 1, Status = FaceModelLifecycleStatuses.Active,
            ModelFileName = "emp_7_v1_old.pkl", ModelPath = "emp_7_v1_old.pkl",
            ModelChecksum = "a", CreatedAt = DateTime.UtcNow
        });
        var jobId = Guid.NewGuid();
        var expected = $"emp_7_v2_{jobId:N}"[..^24] + ".pkl";
        var job = new FaceEnrollmentJob {
            Id = jobId, EmployeeId = 7, EmployeeFaceVideoId = 1, RequestedByUserId = 1,
            Status = FaceEnrollmentJobStatuses.Activating, CandidateChecksum = "checksum",
            ExpectedModelFileName = expected, TargetModelVersion = 2
        };
        db.FaceEnrollmentJobs.Add(job);
        db.EmployeeFaceModels.Add(new EmployeeFaceModel {
            Id = 2, EmployeeId = 7, Version = 2, Status = FaceModelLifecycleStatuses.Activating,
            ModelFileName = expected, ModelPath = expected, ModelChecksum = "checksum",
            SourceEnrollmentJobId = jobId, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var runtime = new RecoveryRuntime(expected);
        var service = new FaceEnrollmentService(db, runtime, new DummyStorage(),
            new FaceEnrollmentOptions());

        await service.RecoverAsync(CancellationToken.None);
        db.ChangeTracker.Clear();

        Assert.Equal(FaceEnrollmentJobStatuses.Completed,
            (await db.FaceEnrollmentJobs.SingleAsync()).Status);
        var models = await db.EmployeeFaceModels.OrderBy(m => m.Version).ToListAsync();
        Assert.Equal(FaceModelLifecycleStatuses.Archived, models[0].Status);
        Assert.Equal(FaceModelLifecycleStatuses.Active, models[1].Status);
        Assert.Equal(2, models.Count);
    }

    [Fact]
    public async Task Create_RequiresManagedVideoOwnershipAndBlocksSecondActiveJob()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)).Options;
        await using var db = new ApplicationDbContext(options);
        db.Employees.AddRange(
            new Employee { EmployeeId = 1, FullName = "One", Status = true },
            new Employee { EmployeeId = 2, FullName = "Two", Status = true });
        db.AppUsers.Add(new AppUser {
            UserId = 10, Username = "admin", PasswordHash = "x", Role = "Admin"
        });
        var filename = $"{Guid.NewGuid():N}.mp4";
        var path = Path.Combine(Path.GetTempPath(), filename);
        await File.WriteAllBytesAsync(path, [1]);
        try
        {
            db.EmployeeFaceVideos.Add(new EmployeeFaceVideo {
                Id = 20, EmployeeId = 1, FileName = filename,
                FilePath = $"video_notok/{filename}", FileSize = 1
            });
            await db.SaveChangesAsync();
            var service = new FaceEnrollmentService(db, new RecoveryRuntime("unused"),
                new DummyStorage(), new FaceEnrollmentOptions());
            var created = await service.CreateAsync(new(1, 20), 10, CancellationToken.None);
            Assert.Equal(FaceEnrollmentJobStatuses.Pending, created.Status);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateAsync(new(2, 20), 10, CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(new(1, 20), 10, CancellationToken.None));
        }
        finally { File.Delete(path); }
    }

    private sealed class RecoveryRuntime(string filename) : IFaceRecognitionClient
    {
        public Task<FaceRuntimeResponse> GetModelsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new FaceRuntimeResponse(HttpStatusCode.OK,
                $$"""{"models":[{"fileName":"{{filename}}","checksum":"checksum"}]}""",
                "application/json"));
        public Task<FaceRuntimeResponse> GetCamerasAsync(CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> StartCameraAsync(string id, FaceCameraStartRequest r, CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> StopCameraAsync(string id, CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> ResetCameraAsync(string id, CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> GetCameraStatusAsync(string id, CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> GetRecognitionResultAsync(string id, CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> GetLockedImagesAsync(string id, CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> StartCameraAsync(FaceCameraStartRequest r, CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> StopCameraAsync(CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> ResetCameraAsync(CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> GetCameraStatusAsync(CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> GetRecognitionResultAsync(CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> GetLockedImagesAsync(CancellationToken c) => throw new NotSupportedException();
        public Task<FaceRuntimeResponse> ReloadModelsAsync(CancellationToken c) => throw new NotSupportedException();
    }

    private sealed class DummyStorage : IFaceStoragePathResolver
    {
        public string InputRoot => Path.GetTempPath();
        public string ModelActiveDir => Path.Combine(Path.GetTempPath(), "models", "active");
        public string ResolveDirectory(string directoryName) => Path.GetTempPath();
        public string ResolveFile(string directoryName, string fileName) => Path.Combine(Path.GetTempPath(), fileName);
    }
}
