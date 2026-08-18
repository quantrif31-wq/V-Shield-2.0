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

public sealed class LicensePlateControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private static int _idCounter;

    private readonly SecurityWebApplicationFactory _factory;

    public LicensePlateControllerTests(SecurityWebApplicationFactory factory)
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
    public async Task Cameras_Plates_And_CameraPlates_ReturnSeededData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var cameraIp = "192.168." + NextId() % 200 + "." + NextId() % 200;
        var cameraId = 30000 + NextId();
        db.CameraPlates.Add(new CameraPlate { CameraIP = cameraIp, PlateNumber = "29A" + suffix, X1 = 1, Y1 = 2, X2 = 3, Y2 = 4, LastUpdate = DateTime.UtcNow });
        db.Cameras.Add(new Camera { CameraId = cameraId, CameraName = cameraIp, CameraType = "plate" });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var cams = await client.GetAsync("/api/license-plates/cameras");
        Assert.Equal(HttpStatusCode.OK, cams.StatusCode);
        var camsDoc = await JsonDocument.ParseAsync(await cams.Content.ReadAsStreamAsync());
        Assert.Contains(camsDoc.RootElement.EnumerateArray(), e => e.GetProperty("cameraIP").GetString() == cameraIp);

        var plates = await client.GetAsync("/api/license-plates/plates");
        Assert.Equal(HttpStatusCode.OK, plates.StatusCode);
        var platesDoc = await JsonDocument.ParseAsync(await plates.Content.ReadAsStreamAsync());
        Assert.Contains(platesDoc.RootElement.EnumerateArray(), e => e.GetProperty("cameraIP").GetString() == cameraIp);

        var single = await client.GetAsync($"/api/license-plates/plate?ip={Uri.EscapeDataString(cameraIp)}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);

        var missing = await client.GetAsync("/api/license-plates/plate?ip=10.9.8.7");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var cameraPlates = await client.GetAsync("/api/license-plates/camera-plates");
        Assert.Equal(HttpStatusCode.OK, cameraPlates.StatusCode);
        var cpDoc = await JsonDocument.ParseAsync(await cameraPlates.Content.ReadAsStreamAsync());
        Assert.Contains(cpDoc.RootElement.EnumerateArray(), e => e.GetProperty("cameraId").GetInt32() == cameraId);
    }

    [Fact]
    public async Task FuzzyMatch_FindsSimilarVehicle()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var empId = 40000 + NextId();
        db.Employees.Add(new Employee { EmployeeId = empId, FullName = "Owner " + suffix, Status = true });
        var plate = "29A-" + suffix[..4] + "5";
        db.Vehicles.Add(new Vehicle { VehicleId = 50000 + NextId(), LicensePlate = plate, ParkingStatus = "OUT", EmployeeId = empId });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.PostAsJsonAsync("/api/license-plates/fuzzy-match", new { plate = plate.Replace("-", ""), minScore = 0.6, maxResults = 5 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("results").GetArrayLength() >= 1);
        Assert.Equal(plate, doc.RootElement.GetProperty("results")[0].GetProperty("licensePlate").GetString());

        var empty = await client.PostAsJsonAsync("/api/license-plates/fuzzy-match", new { plate = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task Timeline_And_Anomalies_ReturnEntries()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var plate = "30E" + suffix[..4] + "7";
        var gate = new Gate { GateId = 60000 + NextId(), GateName = "Gate " + suffix };
        db.Gates.Add(gate);
        db.AccessLogs.AddRange(
            new AccessLog { LogId = 70000 + NextId(), Timestamp = DateTime.UtcNow.AddMinutes(-1), Direction = "IN", GateId = gate.GateId, ResultStatus = "APPROVED", CapturedLicensePlate = plate },
            new AccessLog { LogId = 70000 + NextId(), Timestamp = DateTime.UtcNow.AddMinutes(-30), Direction = "IN", GateId = gate.GateId, ResultStatus = "APPROVED", CapturedLicensePlate = plate });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var timeline = await client.GetAsync($"/api/license-plates/{plate}/timeline?hours=24");
        Assert.Equal(HttpStatusCode.OK, timeline.StatusCode);
        var tlDoc = await JsonDocument.ParseAsync(await timeline.Content.ReadAsStreamAsync());
        Assert.True(tlDoc.RootElement.GetProperty("entries").GetArrayLength() >= 2);

        var anomalies = await client.GetAsync($"/api/license-plates/{plate}/anomalies?hours=24");
        Assert.Equal(HttpStatusCode.OK, anomalies.StatusCode);

        var emptyPlate = await client.GetAsync("/api/license-plates/%20/timeline");
        Assert.Equal(HttpStatusCode.BadRequest, emptyPlate.StatusCode);
    }

    [Fact]
    public async Task SuggestCorrection_Works()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Vehicles.Add(new Vehicle { VehicleId = 50000 + NextId(), LicensePlate = "29A-123.45", ParkingStatus = "OUT" });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.PostAsJsonAsync("/api/license-plates/suggest-correction", new { rawOcr = "29A12345" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("29A12345", doc.RootElement.GetProperty("normalized").GetString());

        var empty = await client.PostAsJsonAsync("/api/license-plates/suggest-correction", new { rawOcr = "" });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }
}