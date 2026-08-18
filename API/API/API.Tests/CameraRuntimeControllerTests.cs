using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public sealed class CameraRuntimeControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private static int _idCounter;

    private readonly SecurityWebApplicationFactory _factory;

    public CameraRuntimeControllerTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static int NextId() => System.Threading.Interlocked.Increment(ref _idCounter);

    private static string AdminToken()
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1002"),
            new Claim(JwtRegisteredClaimNames.UniqueName, "admin.test"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("token_version", "0"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken("VShieldAPI", "VShieldClient", claims,
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetAll_Create_Update_Toggle_Delete_Flow()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var list = await client.GetAsync("/api/camera-runtime");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var name = "Cam" + Guid.NewGuid().ToString("N")[..6];
        var create = await client.PostAsJsonAsync("/api/camera-runtime", new { cameraName = name, cameraType = "rtsp", streamUrl = "rtsp://demo.local/qr" });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var createDoc = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
        var camId = createDoc.RootElement.GetProperty("cameraId").GetInt32();
        Assert.NotNull(createDoc.RootElement.GetProperty("urlView").GetString());

        var byId = await client.GetAsync($"/api/camera-runtime/{camId}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        var update = await client.PutAsJsonAsync($"/api/camera-runtime/{camId}", new { cameraName = name + "2", isRecordingEnabled = true, recordingRetentionDays = 45 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var toggle = await client.PutAsJsonAsync($"/api/camera-runtime/{camId}/recording", new { enabled = false, retentionDays = 7 });
        Assert.Equal(HttpStatusCode.OK, toggle.StatusCode);

        var del = await client.DeleteAsync($"/api/camera-runtime/{camId}");
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var missing = await client.GetAsync($"/api/camera-runtime/{camId}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidGate_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.PostAsJsonAsync("/api/camera-runtime", new { cameraName = "X", gateId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.PostAsJsonAsync("/api/camera-runtime", new { cameraName = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Missing_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.PutAsJsonAsync("/api/camera-runtime/999999", new { cameraName = "X" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ArchiveSegments_And_RecordedSegments_ReturnPaged()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var gate = new Gate { GateId = 60000 + NextId(), GateName = "G " + suffix };
        var cam = new Camera { CameraId = 30000 + NextId(), CameraName = "Arch " + suffix, GateId = gate.GateId, IsRecordingEnabled = true };
        db.Gates.Add(gate);
        db.Cameras.Add(cam);
        db.RecordedSegments.AddRange(
            new RecordedSegment { SegmentId = 20000 + NextId(), CameraId = cam.CameraId, StartedAt = DateTime.UtcNow.AddMinutes(-10), EndedAt = DateTime.UtcNow.AddMinutes(-9), DurationSeconds = 60, FileSizeBytes = 1000, StorageUrl = "/s/1.mp4" },
            new RecordedSegment { SegmentId = 20000 + NextId(), CameraId = cam.CameraId, StartedAt = DateTime.UtcNow.AddMinutes(-30), EndedAt = DateTime.UtcNow.AddMinutes(-29), DurationSeconds = 60, FileSizeBytes = 1000, StorageUrl = "/s/2.mp4" });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var archive = await client.GetAsync($"/api/camera-runtime/archive/segments?cameraId={cam.CameraId}&search={Uri.EscapeDataString(suffix)}&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        var archiveDoc = await JsonDocument.ParseAsync(await archive.Content.ReadAsStreamAsync());
        Assert.True(archiveDoc.RootElement.GetProperty("total").GetInt32() >= 2);

        var recorded = await client.GetAsync($"/api/camera-runtime/{cam.CameraId}/recorded-segments?pageSize=50");
        Assert.Equal(HttpStatusCode.OK, recorded.StatusCode);
        var recDoc = await JsonDocument.ParseAsync(await recorded.Content.ReadAsStreamAsync());
        Assert.True(recDoc.RootElement.GetProperty("total").GetInt32() >= 2);

        var filterTo = await client.GetAsync($"/api/camera-runtime/archive/segments?cameraId={cam.CameraId}&to={DateTime.UtcNow.AddMinutes(-20):yyyy-MM-dd}T00:00:00Z");
        Assert.Equal(HttpStatusCode.OK, filterTo.StatusCode);
    }
}