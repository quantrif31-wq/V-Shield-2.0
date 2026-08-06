using API.Models;

namespace API.Services.FaceRecognition;

public interface IFaceCameraConfigurationService
{
    Task<FaceCameraConfigurationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);
    Task<FaceCameraConfigurationDto?> GetAsync(string runtimeCameraId, CancellationToken cancellationToken);
    Task<FaceCameraConfigurationDto> UpsertAsync(
        string runtimeCameraId,
        UpdateFaceCameraConfigurationRequest request,
        CancellationToken cancellationToken);
    Task<FaceCameraDesiredStateDto> StartAsync(string runtimeCameraId, CancellationToken cancellationToken);
    Task<FaceCameraDesiredStateDto> StopAsync(string runtimeCameraId, CancellationToken cancellationToken);
}

public interface IFaceCameraConfigurationStore
{
    Task<List<FaceCameraConfiguration>> LoadManagedAsync(CancellationToken cancellationToken);
    Task<FaceRuntimeInventory> GetRuntimeInventoryAsync(CancellationToken cancellationToken);
    Task RefreshConfigurationVersionAsync(FaceCameraConfiguration configuration, CancellationToken cancellationToken);
    Task MarkSyncedAsync(FaceCameraConfiguration configuration, CancellationToken cancellationToken);
    Task MarkFailureAsync(
        FaceCameraConfiguration configuration,
        string status,
        string message,
        CancellationToken cancellationToken);
    FaceCameraStartRequest CreateStartRequest(FaceCameraConfiguration configuration);
}
