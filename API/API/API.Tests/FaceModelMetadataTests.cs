using System.Net;
using System.Text.Json;
using API.Data;
using API.Models;
using API.Services.FaceRecognition;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class FaceModelMetadataTests
{
    [Fact]
    public async Task DryRun_ValidatesFiveRowsWithoutChangingMetadata()
    {
        await using var db = CreateDatabase();
        Seed(db);
        var service = new FaceModelMetadataService(db, RegistryClient());

        var result = await service.BootstrapAsync(false, false, default);

        result.Success.Should().BeTrue();
        result.Status.Should().Be("Validated");
        result.DatabaseModelCount.Should().Be(5);
        result.RegistryModelCount.Should().Be(5);
        result.EncodingCount.Should().Be(665);
        (await db.EmployeeFaceModels.AllAsync(model => model.Version == null))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Apply_RequiresBothConfirmationFlags(bool apply, bool confirm)
    {
        await using var db = CreateDatabase();
        Seed(db);
        var service = new FaceModelMetadataService(db, RegistryClient());

        var result = await service.BootstrapAsync(apply, confirm, default);

        result.Success.Should().BeFalse();
        result.Status.Should().Be("ConfirmationRequired");
    }

    [Fact]
    public async Task Apply_SetsVersionOneAndActiveWithoutChangingCreatedAt_AndIsIdempotent()
    {
        await using var db = CreateDatabase();
        Seed(db);
        var original = await db.EmployeeFaceModels
            .OrderBy(model => model.Id)
            .Select(model => model.CreatedAt)
            .ToListAsync();
        var service = new FaceModelMetadataService(db, RegistryClient());

        var applied = await service.BootstrapAsync(true, true, default);
        var repeated = await service.BootstrapAsync(true, true, default);
        var rows = await db.EmployeeFaceModels.OrderBy(model => model.Id).ToListAsync();

        applied.Status.Should().Be("Bootstrapped");
        repeated.Status.Should().Be("AlreadyBootstrapped");
        rows.Should().OnlyContain(model =>
            model.Version == 1 &&
            model.Status == FaceModelLifecycleStatuses.Active &&
            model.ModelChecksum!.Length == 64 &&
            model.EncodingCount > 0 &&
            model.ActivatedAtUtc.HasValue);
        rows.Select(model => model.CreatedAt).Should().Equal(original);
        rows.Select(model => model.ActivatedAtUtc!.Value)
            .Should().Equal(original.Select(value => DateTime.SpecifyKind(value, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task SubjectMismatch_BlocksAllUpdates()
    {
        await using var db = CreateDatabase();
        Seed(db);
        var service = new FaceModelMetadataService(
            db,
            RegistryClient(subjectOverride: "99"));

        var result = await service.BootstrapAsync(true, true, default);

        result.Success.Should().BeFalse();
        result.Issues.Should().Contain(issue => issue.Contains("Subject mismatch"));
        (await db.EmployeeFaceModels.AnyAsync(model => model.Version != null))
            .Should().BeFalse();
    }

    [Fact]
    public async Task AdminDto_ExposesChecksumPrefixButNotPathOrFullChecksum()
    {
        await using var db = CreateDatabase();
        Seed(db);
        var service = new FaceModelMetadataService(db, RegistryClient());
        await service.BootstrapAsync(true, true, default);

        var models = await service.ListAsync(null, default);
        var json = JsonSerializer.Serialize(models);

        models.Should().HaveCount(5);
        models.Should().OnlyContain(model =>
            model.RegistrySyncState == "Synced" &&
            model.ChecksumPrefix!.Length == 12);
        json.Should().NotContain("models/active");
        json.Should().NotContain(new string('a', 64));
        json.Should().NotContain("ModelPath");
    }

    private static ApplicationDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void Seed(ApplicationDbContext db)
    {
        var createdAt = new DateTime(2026, 7, 28, 4, 6, 33, DateTimeKind.Utc);
        for (var id = 1; id <= 5; id++)
        {
            db.Employees.Add(new Employee { EmployeeId = id, FullName = $"Employee {id}" });
            db.EmployeeFaceModels.Add(new EmployeeFaceModel
            {
                Id = id,
                EmployeeId = id,
                ModelFileName = $"emp_{id}_model.pkl",
                ModelPath = $"models/active/emp_{id}_model.pkl",
                CreatedAt = createdAt
            });
        }
        db.SaveChanges();
    }

    private static IFaceRecognitionClient RegistryClient(string? subjectOverride = null)
    {
        var counts = new[] { 119, 117, 183, 164, 82 };
        var models = Enumerable.Range(1, 5).Select(index => new
        {
            subjectId = subjectOverride is not null && index == 1
                ? subjectOverride
                : index.ToString(),
            fileName = $"emp_{index}_model.pkl",
            checksum = new string((char)('a' + index - 1), 64),
            encodingCount = counts[index - 1],
            registryVersion = 7
        });
        return new StubFaceClient(JsonSerializer.Serialize(new
        {
            version = 7,
            successfulFileCount = 5,
            encodingCount = 665,
            errorCount = 0,
            models
        }));
    }

    private sealed class StubFaceClient(string modelsJson) : IFaceRecognitionClient
    {
        public Task<FaceRuntimeResponse> GetModelsAsync(CancellationToken token) =>
            Task.FromResult(new FaceRuntimeResponse(HttpStatusCode.OK, modelsJson, "application/json"));
        public Task<FaceRuntimeResponse> ReloadModelsAsync(CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> GetCamerasAsync(CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> StartCameraAsync(string id, FaceCameraStartRequest request, CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> StopCameraAsync(string id, CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> ResetCameraAsync(string id, CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> GetCameraStatusAsync(string id, CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> GetRecognitionResultAsync(string id, CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> GetLockedImagesAsync(string id, CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> StartCameraAsync(FaceCameraStartRequest request, CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> StopCameraAsync(CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> ResetCameraAsync(CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> GetCameraStatusAsync(CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> GetRecognitionResultAsync(CancellationToken token) => Throw();
        public Task<FaceRuntimeResponse> GetLockedImagesAsync(CancellationToken token) => Throw();
        private static Task<FaceRuntimeResponse> Throw() => throw new NotSupportedException();
    }
}
