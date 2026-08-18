using System.Net;
using System.Text.Json;
using API.Data;
using API.Models;
using API.Services.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class FaceRecognitionEventCollectorTests
{
    [Fact]
    public async Task RepeatedBatch_IsIdempotentAndCheckpointAdvancesWithInsert()
    {
        var eventId = Guid.NewGuid();
        var runtimeEvent = new FaceRuntimeRecognitionEvent(
            eventId.ToString(), "gate-1", "runtime-lane", 1, 2, "Recognized", "9999",
            DateTime.UtcNow, 0.28, 4, "emp_9999.pkl", "abcdef123456");
        var runtime = RuntimeWith(
            new FaceCameraEventsResponse("gate-1", 2, 1, 1, [runtimeEvent], false, false));
        await using var provider = BuildServices(runtime.Object);
        var collector = provider.GetRequiredService<IFaceRecognitionEventCollector>();

        await collector.RunCycleAsync(CancellationToken.None);
        await collector.RunCycleAsync(CancellationToken.None);
        runtime.Verify(item => item.GetCameraEventsAsync(
            "gate-1", It.IsAny<long>(), It.IsAny<long?>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = Assert.Single(await db.FaceRecognitionEvents.ToListAsync());
        Assert.Equal(eventId, stored.RuntimeEventId);
        Assert.Equal(FaceRecognitionMatchStatuses.EmployeeMissing, stored.MatchStatus);
        var checkpoint = Assert.Single(await db.FaceRecognitionCollectorCheckpoints.ToListAsync());
        Assert.Equal(1, checkpoint.LastSequence);
        Assert.Equal(2, checkpoint.RuntimeSessionGeneration);
    }

    [Fact]
    public async Task UnknownEvent_IsSkippedByDefaultButCheckpointStillAdvances()
    {
        var runtimeEvent = new FaceRuntimeRecognitionEvent(
            Guid.NewGuid().ToString(), "gate-2", null, 5, 1, "Unknown", null,
            DateTime.UtcNow, 0.8, null, null, null);
        var runtime = RuntimeWith(
            new FaceCameraEventsResponse("gate-2", 1, 5, 5, [runtimeEvent], false, false));
        await using var provider = BuildServices(runtime.Object);

        await provider.GetRequiredService<IFaceRecognitionEventCollector>()
            .RunCycleAsync(CancellationToken.None);
        runtime.Verify(item => item.GetCameraEventsAsync(
            "gate-2", It.IsAny<long>(), It.IsAny<long?>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.FaceRecognitionEvents.ToListAsync());
        Assert.Equal(5, Assert.Single(await db.FaceRecognitionCollectorCheckpoints.ToListAsync()).LastSequence);
    }

    private static Mock<IFaceRecognitionClient> RuntimeWith(FaceCameraEventsResponse payload)
    {
        var runtime = new Mock<IFaceRecognitionClient>();
        runtime.Setup(item => item.GetCamerasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FaceRuntimeResponse(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(new {
                    sessions = new[] { new { cameraId = payload.CameraId } }
                }),
                "application/json"));
        runtime.Setup(item => item.GetCameraEventsAsync(
                payload.CameraId, It.IsAny<long>(), It.IsAny<long?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FaceCameraEventsRuntimeResult(HttpStatusCode.OK, payload));
        return runtime;
    }

    private static ServiceProvider BuildServices(IFaceRecognitionClient runtime)
    {
        var services = new ServiceCollection();
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"recognition-events-{Guid.NewGuid():N}";
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName, databaseRoot)
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        services.AddSingleton(runtime);
        services.AddSingleton(new FaceRecognitionEventOptions());
        services.AddSingleton<FaceRecognitionEventCollector>();
        services.AddSingleton<IFaceRecognitionEventCollector>(provider =>
            provider.GetRequiredService<FaceRecognitionEventCollector>());
        return services.BuildServiceProvider();
    }

}
