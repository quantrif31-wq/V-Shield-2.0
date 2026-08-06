using System.Net;
using API.Data;
using API.Models;
using API.Services.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.Tests;

public sealed class FaceCameraConfigurationTests
{
    [Fact]
    public async Task Upsert_UsesCanonicalCameraAndMasksCredentials()
    {
        await using var db = CreateDb();
        db.Cameras.Add(Camera(1, "rtsp://operator:secret@10.0.0.8/live"));
        await db.SaveChangesAsync();
        var runtime = new FakeFaceRuntime();
        var service = new FaceCameraConfigurationService(db, runtime);

        var result = await service.UpsertAsync(
            "gate-01",
            new UpdateFaceCameraConfigurationRequest { CameraId = 1 },
            CancellationToken.None);

        Assert.Equal(1, result.ConfigurationVersion);
        Assert.DoesNotContain("operator", result.StreamUrlMasked);
        Assert.DoesNotContain("secret", result.StreamUrlMasked);
        Assert.Equal(
            "rtsp://operator:secret@10.0.0.8/live",
            (await db.Cameras.FindAsync(1))!.StreamUrl);
    }

    [Fact]
    public async Task Upsert_RejectsMissingCameraAndUnsupportedStream()
    {
        await using var db = CreateDb();
        db.Cameras.Add(Camera(1, "file:///camera.mp4"));
        await db.SaveChangesAsync();
        var service = new FaceCameraConfigurationService(db, new FakeFaceRuntime());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpsertAsync(
            "missing",
            new UpdateFaceCameraConfigurationRequest { CameraId = 404 },
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpsertAsync(
            "invalid-url",
            new UpdateFaceCameraConfigurationRequest { CameraId = 1 },
            CancellationToken.None));
    }

    [Fact]
    public async Task Upsert_RejectsSecondRuntimeForSameCamera()
    {
        await using var db = CreateDb();
        db.Cameras.Add(Camera(1, "rtsp://camera/live"));
        await db.SaveChangesAsync();
        var service = new FaceCameraConfigurationService(db, new FakeFaceRuntime());
        await service.UpsertAsync(
            "gate-01",
            new UpdateFaceCameraConfigurationRequest { CameraId = 1 },
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpsertAsync(
            "gate-02",
            new UpdateFaceCameraConfigurationRequest { CameraId = 1 },
            CancellationToken.None));
    }

    [Fact]
    public async Task Start_SavesDesiredStateAndAppliedVersionOnSuccess()
    {
        await using var db = CreateDb();
        db.Cameras.Add(Camera(1, "rtsp://camera/live"));
        await db.SaveChangesAsync();
        var runtime = new FakeFaceRuntime();
        var service = new FaceCameraConfigurationService(db, runtime);
        await service.UpsertAsync(
            "gate-01",
            new UpdateFaceCameraConfigurationRequest { CameraId = 1 },
            CancellationToken.None);

        var result = await service.StartAsync("gate-01", CancellationToken.None);

        Assert.True(result.RuntimeApplied);
        Assert.Equal(FaceCameraDesiredStates.Running, result.Configuration.DesiredState);
        Assert.Equal(
            result.Configuration.ConfigurationVersion,
            result.Configuration.LastAppliedVersion);
        Assert.Equal("rtsp://camera/live", runtime.StartRequests["gate-01"].Ip);
    }

    [Fact]
    public async Task RuntimeUnavailable_DoesNotRollBackStartOrStopDesiredState()
    {
        await using var db = CreateDb();
        db.Cameras.Add(Camera(1, "rtsp://camera/live"));
        await db.SaveChangesAsync();
        var runtime = new FakeFaceRuntime { ThrowUnavailable = true };
        var service = new FaceCameraConfigurationService(db, runtime);
        await service.UpsertAsync(
            "gate-01",
            new UpdateFaceCameraConfigurationRequest { CameraId = 1 },
            CancellationToken.None);

        var start = await service.StartAsync("gate-01", CancellationToken.None);
        var stop = await service.StopAsync("gate-01", CancellationToken.None);

        Assert.False(start.RuntimeApplied);
        Assert.Equal(FaceCameraDesiredStates.Running, start.Configuration.DesiredState);
        Assert.False(stop.RuntimeApplied);
        Assert.Equal(FaceCameraDesiredStates.Stopped, stop.Configuration.DesiredState);
        Assert.Equal(FaceCameraSyncStatuses.Unavailable, stop.Configuration.LastSyncStatus);
    }

    [Fact]
    public async Task Reconcile_RestoresMissingAndStopsOnlyManagedStoppedSession()
    {
        await using var db = CreateDb();
        db.Cameras.AddRange(
            Camera(1, "rtsp://camera/one"),
            Camera(2, "rtsp://camera/two"));
        await db.SaveChangesAsync();
        var runtime = new FakeFaceRuntime();
        var service = new FaceCameraConfigurationService(db, runtime);
        await service.UpsertAsync("running", new() { CameraId = 1 }, CancellationToken.None);
        await service.UpsertAsync("stopped", new() { CameraId = 2 }, CancellationToken.None);
        var running = await db.FaceCameraConfigurations.SingleAsync(x => x.RuntimeCameraId == "running");
        running.DesiredState = FaceCameraDesiredStates.Running;
        await db.SaveChangesAsync();
        runtime.Inventory =
            """{"sessions":[{"cameraId":"stopped","enabled":true,"connected":true,"status":"running"},{"cameraId":"default","enabled":true,"connected":true,"status":"running"}]}""";
        var cycle = new FaceCameraReconciliationCycle(service, runtime);

        var result = await cycle.RunAsync(CancellationToken.None);

        Assert.Equal(1, result.StartedCount);
        Assert.Equal(1, result.StoppedCount);
        Assert.Equal(1, result.UnmanagedCount);
        Assert.Contains("running", runtime.StartRequests.Keys);
        Assert.Contains("stopped", runtime.StopCalls);
        Assert.DoesNotContain("default", runtime.StopCalls);
    }

    [Fact]
    public async Task Reconcile_OneFailureDoesNotPreventOtherCameraRecovery()
    {
        await using var db = CreateDb();
        db.Cameras.AddRange(
            Camera(1, "rtsp://camera/one"),
            Camera(2, "rtsp://camera/two"));
        await db.SaveChangesAsync();
        var runtime = new FakeFaceRuntime { FailingCameraId = "broken" };
        var service = new FaceCameraConfigurationService(db, runtime);
        await service.UpsertAsync("broken", new() { CameraId = 1 }, CancellationToken.None);
        await service.UpsertAsync("healthy", new() { CameraId = 2 }, CancellationToken.None);
        foreach (var item in db.FaceCameraConfigurations)
        {
            item.DesiredState = FaceCameraDesiredStates.Running;
        }
        await db.SaveChangesAsync();
        var cycle = new FaceCameraReconciliationCycle(service, runtime);

        var result = await cycle.RunAsync(CancellationToken.None);

        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, result.StartedCount);
        Assert.Contains("healthy", runtime.StartRequests.Keys);
    }

    [Fact]
    public async Task ConfigurationChange_RestartsOnceAndNextCycleIsIdempotent()
    {
        await using var db = CreateDb();
        db.Cameras.Add(Camera(1, "rtsp://camera/old"));
        await db.SaveChangesAsync();
        var runtime = new FakeFaceRuntime
        {
            Inventory =
                """{"sessions":[{"cameraId":"gate-01","enabled":true,"connected":true,"status":"running"}]}"""
        };
        var service = new FaceCameraConfigurationService(db, runtime);
        await service.UpsertAsync("gate-01", new() { CameraId = 1 }, CancellationToken.None);
        var configuration = await db.FaceCameraConfigurations.SingleAsync();
        configuration.DesiredState = FaceCameraDesiredStates.Running;
        configuration.LastAppliedVersion = configuration.ConfigurationVersion;
        (await db.Cameras.FindAsync(1))!.StreamUrl = "rtsp://camera/new";
        await db.SaveChangesAsync();
        var cycle = new FaceCameraReconciliationCycle(service, runtime);

        var first = await cycle.RunAsync(CancellationToken.None);
        var second = await cycle.RunAsync(CancellationToken.None);

        Assert.Equal(1, first.RestartedCount);
        Assert.Equal(0, second.RestartedCount);
        Assert.Single(runtime.StopCalls);
        Assert.Equal(1, runtime.StartCalls);
    }

    [Fact]
    public void Model_HasRequiredUniqueIndexesAndConcurrencyToken()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(FaceCameraConfiguration))!;
        Assert.True(entity.FindProperty(nameof(FaceCameraConfiguration.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(FaceCameraConfiguration.RuntimeCameraId));
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(FaceCameraConfiguration.CameraId));
    }

    [Fact]
    public async Task Reconciler_PreventsOverlappingManualCycles()
    {
        var blockingStore = new BlockingStore();
        var runtime = new FakeFaceRuntime();
        var services = new ServiceCollection()
            .AddSingleton<IFaceCameraConfigurationStore>(blockingStore)
            .AddSingleton<IFaceRecognitionClient>(runtime)
            .AddScoped<FaceCameraReconciliationCycle>()
            .BuildServiceProvider();
        var reconciler = new FaceCameraSessionReconciler(
            services.GetRequiredService<IServiceScopeFactory>(),
            new FaceCameraReconcileOptions(),
            NullLogger<FaceCameraSessionReconciler>.Instance);

        var first = reconciler.ReconcileAsync(CancellationToken.None);
        await blockingStore.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var overlapping = await reconciler.ReconcileAsync(CancellationToken.None);
        blockingStore.Release.TrySetResult();
        await first;

        Assert.True(overlapping.SkippedBecauseRunning);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"face-config-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Camera Camera(int id, string streamUrl) =>
        new()
        {
            CameraId = id,
            CameraName = $"Camera {id}",
            CameraType = "Face",
            StreamUrl = streamUrl
        };

    private sealed class FakeFaceRuntime : IFaceRecognitionClient
    {
        public string Inventory { get; set; } = """{"sessions":[]}""";
        public bool ThrowUnavailable { get; set; }
        public string? FailingCameraId { get; set; }
        public Dictionary<string, FaceCameraStartRequest> StartRequests { get; } = [];
        public List<string> StopCalls { get; } = [];
        public int StartCalls { get; private set; }

        public Task<FaceRuntimeResponse> GetCamerasAsync(CancellationToken cancellationToken)
        {
            ThrowIfUnavailable();
            return Response(HttpStatusCode.OK, Inventory);
        }

        public Task<FaceRuntimeResponse> StartCameraAsync(
            string cameraId,
            FaceCameraStartRequest request,
            CancellationToken cancellationToken)
        {
            ThrowIfUnavailable();
            StartCalls++;
            if (cameraId == FailingCameraId)
            {
                return Response(HttpStatusCode.InternalServerError, "{}");
            }
            StartRequests[cameraId] = request;
            return Response(HttpStatusCode.OK, "{}");
        }

        public Task<FaceRuntimeResponse> StopCameraAsync(
            string cameraId,
            CancellationToken cancellationToken)
        {
            ThrowIfUnavailable();
            StopCalls.Add(cameraId);
            return Response(HttpStatusCode.OK, "{}");
        }

        public Task<FaceRuntimeResponse> ResetCameraAsync(string cameraId, CancellationToken cancellationToken) =>
            Response(HttpStatusCode.OK, "{}");
        public Task<FaceRuntimeResponse> GetCameraStatusAsync(string cameraId, CancellationToken cancellationToken) =>
            Response(HttpStatusCode.OK, "{}");
        public Task<FaceRuntimeResponse> GetRecognitionResultAsync(string cameraId, CancellationToken cancellationToken) =>
            Response(HttpStatusCode.OK, "{}");
        public Task<FaceRuntimeResponse> GetLockedImagesAsync(string cameraId, CancellationToken cancellationToken) =>
            Response(HttpStatusCode.OK, "{}");
        public Task<FaceRuntimeResponse> StartCameraAsync(FaceCameraStartRequest request, CancellationToken cancellationToken) =>
            StartCameraAsync("default", request, cancellationToken);
        public Task<FaceRuntimeResponse> StopCameraAsync(CancellationToken cancellationToken) =>
            StopCameraAsync("default", cancellationToken);
        public Task<FaceRuntimeResponse> ResetCameraAsync(CancellationToken cancellationToken) =>
            ResetCameraAsync("default", cancellationToken);
        public Task<FaceRuntimeResponse> GetCameraStatusAsync(CancellationToken cancellationToken) =>
            GetCameraStatusAsync("default", cancellationToken);
        public Task<FaceRuntimeResponse> GetRecognitionResultAsync(CancellationToken cancellationToken) =>
            GetRecognitionResultAsync("default", cancellationToken);
        public Task<FaceRuntimeResponse> GetLockedImagesAsync(CancellationToken cancellationToken) =>
            GetLockedImagesAsync("default", cancellationToken);
        public Task<FaceRuntimeResponse> GetModelsAsync(CancellationToken cancellationToken) =>
            Response(HttpStatusCode.OK, "{}");
        public Task<FaceRuntimeResponse> ReloadModelsAsync(CancellationToken cancellationToken) =>
            Response(HttpStatusCode.OK, "{}");

        private void ThrowIfUnavailable()
        {
            if (ThrowUnavailable)
            {
                throw new FaceRuntimeUnavailableException(
                    FaceRuntimeFailureKind.ConnectionFailure,
                    "secret upstream detail",
                    new HttpRequestException());
            }
        }

        private static Task<FaceRuntimeResponse> Response(HttpStatusCode status, string body) =>
            Task.FromResult(new FaceRuntimeResponse(status, body, "application/json"));
    }

    private sealed class BlockingStore : IFaceCameraConfigurationStore
    {
        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<List<FaceCameraConfiguration>> LoadManagedAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new List<FaceCameraConfiguration>());

        public async Task<FaceRuntimeInventory> GetRuntimeInventoryAsync(
            CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new FaceRuntimeInventory(
                true,
                new Dictionary<string, FaceRuntimeSession>());
        }

        public Task RefreshConfigurationVersionAsync(
            FaceCameraConfiguration configuration,
            CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MarkSyncedAsync(
            FaceCameraConfiguration configuration,
            CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MarkFailureAsync(
            FaceCameraConfiguration configuration,
            string status,
            string message,
            CancellationToken cancellationToken) => Task.CompletedTask;
        public FaceCameraStartRequest CreateStartRequest(FaceCameraConfiguration configuration) =>
            new();
    }
}
