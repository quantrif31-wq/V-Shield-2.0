using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Controllers;
using API.Services;
using API.Services.FaceRecognition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public sealed class FaceCameraControllerContractTests
{
    [Theory]
    [InlineData("cameras/default/stop")]
    [InlineData("cameras/default/reset")]
    public async Task PostWithoutBody_ForwardsStatusAndPayload(string action)
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(
                HttpStatusCode.Accepted,
                """{"success":true,"message":"forwarded"}""",
                "application/json"));
        var controller = CreateController(client);

        var result = action.EndsWith("/stop", StringComparison.Ordinal)
            ? await controller.TurnOffCamera(CancellationToken.None)
            : await controller.ResetCameraState(CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal((int)HttpStatusCode.Accepted, content.StatusCode);
        Assert.Equal("application/json", content.ContentType);
        Assert.Equal("""{"success":true,"message":"forwarded"}""", content.Content);
        Assert.Equal(action, Assert.Single(client.Operations));
    }

    [Theory]
    [InlineData("status", "cameras/default/status")]
    [InlineData("result", "cameras/default/result")]
    [InlineData("locked-images", "cameras/default/locked-images")]
    public async Task Get_ForwardsStatusAndPayload(string action, string expectedOperation)
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(
                HttpStatusCode.OK,
                """{"success":true,"camera_enabled":false}""",
                "application/json"));
        var controller = CreateController(client);

        var result = action switch
        {
            "status" => await controller.GetCameraStatus(CancellationToken.None),
            "result" => await controller.GetCameraResult(CancellationToken.None),
            _ => await controller.GetLockedImages(CancellationToken.None)
        };

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal((int)HttpStatusCode.OK, content.StatusCode);
        Assert.Equal("""{"success":true,"camera_enabled":false}""", content.Content);
        Assert.Equal(expectedOperation, Assert.Single(client.Operations));
    }

    [Fact]
    public async Task CameraOn_ForwardsRequestWithoutChangingIp()
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(HttpStatusCode.OK, """{"success":true}""", "application/json"));
        var controller = CreateController(client);
        const string cameraUrl = "http://camera.local:8080/video?profile=main";

        var result = await controller.TurnOnCamera(
            new FaceCameraStartRequest { Ip = cameraUrl },
            CancellationToken.None);

        Assert.IsType<ContentResult>(result);
        Assert.Equal(cameraUrl, client.StartRequest?.Ip);
        Assert.Equal("cameras/default/start", Assert.Single(client.Operations));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, """{"success":false,"message":"python error"}""")]
    [InlineData(HttpStatusCode.OK, "not-json")]
    [InlineData(HttpStatusCode.OK, "")]
    public async Task PythonResponse_IsProxiedVerbatim(
        HttpStatusCode pythonStatus,
        string pythonPayload)
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(pythonStatus, pythonPayload, "text/plain"));
        var controller = CreateController(client);

        var result = await controller.GetCameraStatus(CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal((int)pythonStatus, content.StatusCode);
        Assert.Equal(pythonPayload, content.Content);
        Assert.Equal("text/plain", content.ContentType);
    }

    [Theory]
    [InlineData(FaceRuntimeFailureKind.ConnectionFailure, "simulated connection refused")]
    [InlineData(FaceRuntimeFailureKind.Timeout, "simulated timeout")]
    public async Task TransportFailure_Returns503WithCurrentErrorEnvelope(
        FaceRuntimeFailureKind kind,
        string message)
    {
        var client = StubClient.Throwing(
            new FaceRuntimeUnavailableException(kind, message, new Exception(message)));
        var controller = CreateController(client);

        var result = await controller.GetCameraStatus(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        var json = JsonSerializer.Serialize(objectResult.Value);
        Assert.Contains("Khong the ket noi toi Face camera service", json);
        Assert.Contains(message, json);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Models_ProxiesPythonStatusAndRawBody(HttpStatusCode status)
    {
        const string body = """{"version":4,"models":[]}""";
        var client = StubClient.Returning(new FaceRuntimeResponse(status, body, "application/json"));
        var controller = CreateController(client);

        var result = await controller.GetModels(CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal((int)status, content.StatusCode);
        Assert.Equal(body, content.Content);
        Assert.Equal("models", Assert.Single(client.Operations));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ReloadModels_ProxiesPythonStatusAndRawBody(HttpStatusCode status)
    {
        const string body = """{"success":false,"version":3}""";
        var client = StubClient.Returning(new FaceRuntimeResponse(status, body, "application/json"));
        var controller = CreateController(client);

        var result = await controller.ReloadModels(CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal((int)status, content.StatusCode);
        Assert.Equal(body, content.Content);
        Assert.Equal("models/reload", Assert.Single(client.Operations));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    public async Task ReloadModels_PreservesEmptyOrInvalidJsonBody(string body)
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(HttpStatusCode.OK, body, "application/json"));
        var controller = CreateController(client);

        var result = await controller.ReloadModels(CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(body, content.Content);
        Assert.Equal("application/json", content.ContentType);
        Assert.Equal("models/reload", Assert.Single(client.Operations));
    }

    [Fact]
    public async Task ReloadModels_WithRequestBody_Returns400WithoutCallingRuntime()
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(HttpStatusCode.OK, "{}", "application/json"));
        var controller = CreateController(client);
        controller.Request.ContentLength = 17;

        var result = await controller.ReloadModels(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.Empty(client.Operations);
    }

    [Fact]
    public async Task Controller_PassesCancellationTokenToClient()
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(HttpStatusCode.OK, "{}", "application/json"));
        var controller = CreateController(client);
        using var cancellation = new CancellationTokenSource();

        await controller.GetModels(cancellation.Token);

        Assert.Equal(cancellation.Token, client.LastCancellationToken);
    }

    [Fact]
    public async Task CallerCancellation_IsNotConvertedTo503()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = StubClient.Throwing(new OperationCanceledException(cancellation.Token));
        var controller = CreateController(client);

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => controller.GetModels(cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task DiscoverIpWebcam_UsesSharedDiscoveryService()
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(HttpStatusCode.OK, "{}", "application/json"));
        var controller = CreateController(client);
        var discovery = new StubCameraDiscoveryService(
        [
            new IpWebcamCandidate
            {
                Name = "Gate camera",
                IpAddress = "192.168.1.25",
                Port = 8080,
                BaseUrl = "http://192.168.1.25:8080"
            }
        ]);

        var result = await controller.DiscoverIpWebcam(
            discovery,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"count\":1", payload);
        Assert.Contains("192.168.1.25", payload);
        Assert.Equal(1, discovery.CallCount);
        Assert.Empty(client.Operations);
    }

    [Fact]
    public async Task CameraSpecificController_ForwardsIdLaneAndRawConflict()
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(
                HttpStatusCode.Conflict,
                """{"success":false,"errorCode":"CAMERA_CONFLICT"}""",
                "application/problem+json"));
        var controller = CreateController(client);
        var request = new FaceCameraStartRequest
        {
            Ip = "rtsp://camera.test/live",
            LaneId = "lane-01"
        };

        var result = await controller.StartCamera(
            "gate-01",
            request,
            CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal((int)HttpStatusCode.Conflict, content.StatusCode);
        Assert.Equal("application/problem+json", content.ContentType);
        Assert.Equal("lane-01", client.StartRequest?.LaneId);
        Assert.Equal("cameras/gate-01/start", Assert.Single(client.Operations));
    }

    [Theory]
    [InlineData("../camera")]
    [InlineData("camera id")]
    [InlineData("a..b")]
    public async Task InvalidCameraId_Returns400WithoutRuntimeCall(string cameraId)
    {
        var client = StubClient.Returning(
            new FaceRuntimeResponse(HttpStatusCode.OK, "{}", "application/json"));
        var controller = CreateController(client);

        var result = await controller.GetCameraStatus(
            cameraId,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(client.Operations);
    }

    [Fact]
    public async Task CameraSpecificTransportFailure_Returns503()
    {
        var client = StubClient.Throwing(
            new FaceRuntimeUnavailableException(
                FaceRuntimeFailureKind.ConnectionFailure,
                "runtime unavailable",
                new HttpRequestException("runtime unavailable")));
        var controller = CreateController(client);

        var result = await controller.GetCameraStatus(
            "gate-01",
            CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal("cameras/gate-01/status", Assert.Single(client.Operations));
    }

    private static FaceCameraController CreateController(StubClient client) =>
        new(client)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    internal sealed class StubClient : IFaceRecognitionClient
    {
        private readonly FaceRuntimeResponse? _response;
        private readonly Exception? _exception;

        private StubClient(FaceRuntimeResponse? response, Exception? exception)
        {
            _response = response;
            _exception = exception;
        }

        public List<string> Operations { get; } = [];

        public FaceCameraStartRequest? StartRequest { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public static StubClient Returning(FaceRuntimeResponse response) => new(response, null);

        public static StubClient Throwing(Exception exception) => new(null, exception);

        public Task<FaceRuntimeResponse> StartCameraAsync(
            FaceCameraStartRequest request,
            CancellationToken cancellationToken)
        {
            StartRequest = request;
            return StartCameraAsync("default", request, cancellationToken);
        }

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

        public Task<FaceRuntimeResponse> GetCamerasAsync(
            CancellationToken cancellationToken) =>
            Complete("cameras", cancellationToken);

        public Task<FaceRuntimeResponse> StartCameraAsync(
            string cameraId,
            FaceCameraStartRequest request,
            CancellationToken cancellationToken)
        {
            StartRequest = request;
            return Complete($"cameras/{cameraId}/start", cancellationToken);
        }

        public Task<FaceRuntimeResponse> StopCameraAsync(
            string cameraId,
            CancellationToken cancellationToken) =>
            Complete($"cameras/{cameraId}/stop", cancellationToken);

        public Task<FaceRuntimeResponse> ResetCameraAsync(
            string cameraId,
            CancellationToken cancellationToken) =>
            Complete($"cameras/{cameraId}/reset", cancellationToken);

        public Task<FaceRuntimeResponse> GetCameraStatusAsync(
            string cameraId,
            CancellationToken cancellationToken) =>
            Complete($"cameras/{cameraId}/status", cancellationToken);

        public Task<FaceRuntimeResponse> GetRecognitionResultAsync(
            string cameraId,
            CancellationToken cancellationToken) =>
            Complete($"cameras/{cameraId}/result", cancellationToken);

        public Task<FaceRuntimeResponse> GetLockedImagesAsync(
            string cameraId,
            CancellationToken cancellationToken) =>
            Complete($"cameras/{cameraId}/locked-images", cancellationToken);

        public Task<FaceRuntimeResponse> GetModelsAsync(CancellationToken cancellationToken) =>
            Complete("models", cancellationToken);

        public Task<FaceRuntimeResponse> ReloadModelsAsync(CancellationToken cancellationToken) =>
            Complete("models/reload", cancellationToken);

        private Task<FaceRuntimeResponse> Complete(
            string operation,
            CancellationToken cancellationToken)
        {
            Operations.Add(operation);
            LastCancellationToken = cancellationToken;
            return _exception == null
                ? Task.FromResult(_response!)
                : Task.FromException<FaceRuntimeResponse>(_exception);
        }
    }

    private sealed class StubCameraDiscoveryService : ILocalNetworkCameraDiscoveryService
    {
        private readonly IReadOnlyList<IpWebcamCandidate> _cameras;

        public StubCameraDiscoveryService(IReadOnlyList<IpWebcamCandidate> cameras)
        {
            _cameras = cameras;
        }

        public int CallCount { get; private set; }

        public Task<IReadOnlyList<IpWebcamCandidate>> DiscoverIpWebcamsAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_cameras);
        }
    }
}

public sealed class FaceCameraAuthorizationContractTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly SecurityWebApplicationFactory _factory;

    public FaceCameraAuthorizationContractTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/FaceCamera/camera/status")]
    [InlineData("/api/FaceCamera/cameras")]
    [InlineData("/api/FaceCamera/cameras/gate-01/status")]
    [InlineData("/api/FaceCamera/models")]
    [InlineData("/api/FaceCamera/models/reload")]
    [InlineData("/api/FaceCamera/discover-ipwebcam")]
    [InlineData("/api/FaceCameraConfigurations")]
    [InlineData("/api/FaceModels")]
    [InlineData("/api/FaceModels/health")]
    [InlineData("/api/Employees/1/face-models")]
    [InlineData("/api/FaceEnrollments")]
    [InlineData("/api/FaceRecognitionEvents")]
    [InlineData("/api/FaceAccessPolicyComparisons")]
    [InlineData("/api/FaceCredentialBindings")]
    public async Task AnonymousUser_IsRejected(string path)
    {
        using var client = CreateClientWithFakeFaceRuntime();

        var response = path.EndsWith("reload", StringComparison.Ordinal)
            ? await client.PostAsync(path, null)
            : await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/FaceCamera/camera/status")]
    [InlineData("/api/FaceCamera/cameras")]
    [InlineData("/api/FaceCamera/cameras/gate-01/status")]
    [InlineData("/api/FaceCamera/models")]
    [InlineData("/api/FaceCamera/models/reload")]
    [InlineData("/api/FaceCamera/discover-ipwebcam")]
    [InlineData("/api/FaceCameraConfigurations")]
    [InlineData("/api/FaceModels")]
    [InlineData("/api/FaceModels/health")]
    [InlineData("/api/Employees/1/face-models")]
    [InlineData("/api/FaceEnrollments")]
    [InlineData("/api/FaceRecognitionEvents")]
    [InlineData("/api/FaceAccessPolicyComparisons")]
    [InlineData("/api/FaceCredentialBindings")]
    public async Task UserWithoutMonitoringPermission_IsRejected(string path)
    {
        using var client = CreateClientWithFakeFaceRuntime();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1003, "staff.role", "Staff"));

        var response = path.EndsWith("reload", StringComparison.Ordinal)
            ? await client.PostAsync(path, null)
            : await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/FaceCamera/camera/status")]
    [InlineData("/api/FaceCamera/cameras")]
    [InlineData("/api/FaceCamera/cameras/gate-01/status")]
    [InlineData("/api/FaceCamera/models")]
    [InlineData("/api/FaceCamera/models/reload")]
    public async Task AdminWithMonitoringPermission_CanCallProxy(string path)
    {
        using var client = CreateClientWithFakeFaceRuntime();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));

        var response = path.EndsWith("reload", StringComparison.Ordinal)
            ? await client.PostAsync(path, null)
            : await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"success":true,"camera_enabled":false}""",
            await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/FaceModels")]
    [InlineData("/api/FaceModels/health")]
    [InlineData("/api/Employees/1/face-models")]
    [InlineData("/api/FaceEnrollments")]
    [InlineData("/api/FaceRecognitionEvents")]
    [InlineData("/api/FaceAccessPolicyComparisons")]
    [InlineData("/api/FaceCredentialBindings")]
    public async Task AdminWithIdentityManagementPermission_CanReadFaceModelMetadata(string path)
    {
        var runtime = FaceCameraControllerContractTests.StubClient.Returning(
            new FaceRuntimeResponse(
                HttpStatusCode.OK,
                """{"version":1,"successfulFileCount":0,"encodingCount":0,"errorCount":0,"models":[]}""",
                "application/json"));
        using var client = CreateClientWithFakeFaceRuntime(runtime);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("ModelPath", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ReloadModels_WithBody_IsRejectedBeforeRuntimeCall()
    {
        var runtime = FaceCameraControllerContractTests.StubClient.Returning(
            new FaceRuntimeResponse(HttpStatusCode.OK, "{}", "application/json"));
        using var client = CreateClientWithFakeFaceRuntime(runtime);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));

        var response = await client.PostAsync(
            "/api/FaceCamera/models/reload",
            new StringContent("""{"path":"forbidden.pkl"}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(runtime.Operations);
    }

    [Fact]
    public async Task LegacyFaceRecognitionRoute_IsNotMapped()
    {
        using var client = CreateClientWithFakeFaceRuntime();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));

        var response = await client.GetAsync("/api/face-recognition/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminWithMonitoringPermission_CanListPersistedConfigurations()
    {
        using var client = CreateClientWithFakeFaceRuntime(
            FaceCameraControllerContractTests.StubClient.Returning(
                new FaceRuntimeResponse(
                    HttpStatusCode.OK,
                    """{"sessions":[]}""",
                    "application/json")));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));

        var response = await client.GetAsync("/api/FaceCameraConfigurations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ManualReconcile_RequiresAdminAndAllowsAdmin()
    {
        var runtime = FaceCameraControllerContractTests.StubClient.Returning(
            new FaceRuntimeResponse(
                HttpStatusCode.OK,
                """{"sessions":[]}""",
                "application/json"));
        using var anonymous = CreateClientWithFakeFaceRuntime(runtime);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsync("/api/FaceCameraConfigurations/reconcile", null)).StatusCode);

        using var staff = CreateClientWithFakeFaceRuntime(runtime);
        staff.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1003, "staff.role", "Staff"));
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await staff.PostAsync("/api/FaceCameraConfigurations/reconcile", null)).StatusCode);

        using var admin = CreateClientWithFakeFaceRuntime(runtime);
        admin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));
        Assert.Equal(
            HttpStatusCode.OK,
            (await admin.PostAsync("/api/FaceCameraConfigurations/reconcile", null)).StatusCode);
    }

    [Fact]
    public async Task FaceCredentialBindingMutation_RequiresIdentityManagePermission()
    {
        const string payload = """{"employeeId":999,"accessCredentialId":999,"reason":"test"}""";

        using var anonymous = CreateClientWithFakeFaceRuntime();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsync("/api/FaceCredentialBindings",
                new StringContent(payload, Encoding.UTF8, "application/json"))).StatusCode);

        using var staff = CreateClientWithFakeFaceRuntime();
        staff.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1003, "staff.role", "Staff"));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await staff.PostAsync("/api/FaceCredentialBindings",
                new StringContent(payload, Encoding.UTF8, "application/json"))).StatusCode);

        using var admin = CreateClientWithFakeFaceRuntime();
        admin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.PostAsync("/api/FaceCredentialBindings",
                new StringContent(payload, Encoding.UTF8, "application/json"))).StatusCode);
    }

    private HttpClient CreateClientWithFakeFaceRuntime(
        FaceCameraControllerContractTests.StubClient? runtime = null)
    {
        runtime ??= FaceCameraControllerContractTests.StubClient.Returning(
            new FaceRuntimeResponse(
                HttpStatusCode.OK,
                """{"success":true,"camera_enabled":false}""",
                "application/json"));

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IFaceRecognitionClient>();
                services.AddSingleton<IFaceRecognitionClient>(runtime);
            });
        }).CreateClient();
    }

    private static string CreateJwtToken(int userId, string username, string role)
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("token_version", "0"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken(
            issuer: "VShieldAPI",
            audience: "VShieldClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
