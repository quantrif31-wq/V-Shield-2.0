using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public class GeoRoutingTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SecurityWebApplicationFactory _factory;

    public GeoRoutingTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RoutingEndpoint_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/routing", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoutingEndpoint_AuthenticatedUser_CanAccess()
    {
        using var authenticated = CreateAdminClient();
        var response = await authenticated.PostAsJsonAsync("/api/routing", new
        {
            fromLat = 21.0285,
            fromLng = 105.8048,
            toLat = 21.0275,
            toLng = 105.8058
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.True(body.RootElement.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task RoutingEndpoint_WithIndoorNodes_ReturnsIndoorSteps()
    {
        var buildingId = await SeedBuildingWithNodesAsync();

        using var authenticated = CreateAdminClient();
        var response = await authenticated.PostAsJsonAsync("/api/routing", new
        {
            fromLat = 21.0285,
            fromLng = 105.8048,
            toLat = 21.0275,
            toLng = 105.8058,
            buildingId,
            targetNodeId = (long?)3
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var data = body.RootElement.GetProperty("data");

        Assert.True(data.TryGetProperty("targetBuildingName", out var buildingName));
        Assert.Equal("Test Building", buildingName.GetString());

        Assert.True(data.TryGetProperty("indoorSteps", out var steps));
        Assert.True(steps.GetArrayLength() > 0);
    }

    [Fact]
    public async Task IndoorMapNodes_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/indoor-map/nodes?buildingId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IndoorMapNodes_WithBuildingId_ReturnsNodes()
    {
        var buildingId = await SeedBuildingWithNodesAsync();

        using var authenticated = CreateAdminClient();
        var response = await authenticated.GetAsync($"/api/indoor-map/nodes?buildingId={buildingId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);

        Assert.True(body.RootElement.TryGetProperty("data", out var nodes));
        Assert.True(nodes.GetArrayLength() > 0);

        var first = nodes[0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("x", out _));
        Assert.True(first.TryGetProperty("y", out _));
        Assert.True(first.TryGetProperty("nodeType", out _));
    }

    [Fact]
    public async Task SocAlarmList_IncludesLatLongFields()
    {
        using var authenticated = CreateAdminClient();

        var createResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/alarms", new
        {
            alarmType = "TestGeo",
            severity = "Medium",
            summary = "Geo-located alarm test",
            latitude = 21.0285m,
            longitude = 105.8048m
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var listResponse = await authenticated.GetAsync("/api/enterprise/soc/alarms?state=&severity=&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var body = await ReadJsonAsync(listResponse);
        Assert.True(body.RootElement.TryGetProperty("items", out var items));
        Assert.True(items.GetArrayLength() > 0);

        var alarm = items.EnumerateArray().First(a =>
            a.TryGetProperty("alarmType", out var t) && t.GetString() == "TestGeo");

        Assert.True(alarm.TryGetProperty("latitude", out var lat));
        Assert.Equal(21.0285m, lat.GetDecimal());
        Assert.True(alarm.TryGetProperty("longitude", out var lng));
        Assert.Equal(105.8048m, lng.GetDecimal());
    }

    [Fact]
    public async Task Notifications_IncludeLatLongFields()
    {
        using var authenticated = CreateAdminClient();

        // Create an alarm with location → the SOC controller also pushes a notification
        var alarmPayload = new
        {
            alarmType = "NotificationGeo",
            severity = "Medium",
            summary = "Notification location test",
            latitude = 21.0285m,
            longitude = 105.8048m
        };
        var createAlarmResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/alarms", alarmPayload);
        Assert.Equal(HttpStatusCode.OK, createAlarmResponse.StatusCode);

        var response = await authenticated.GetAsync("/api/notifications?skip=0&take=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync(response);
        Assert.True(body.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.GetArrayLength() > 0, "Notification data array should not be empty");

        var newest = data.EnumerateArray().First();
        Assert.True(newest.TryGetProperty("latitude", out var lat));
        Assert.Equal(21.0285m, lat.GetDecimal());
        Assert.True(newest.TryGetProperty("longitude", out var lng));
        Assert.Equal(105.8048m, lng.GetDecimal());
        Assert.True(newest.TryGetProperty("category", out var cat));
        Assert.Equal("Alarm", cat.GetString());
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private async Task<int> SeedBuildingWithNodesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!db.Sites.Any(s => s.SiteId == 1))
        {
            db.Sites.Add(new Site
            {
                SiteId = 1,
                Name = "Test Site",
                Latitude = 21.0285m,
                Longitude = 105.8048m
            });
        }

        if (!db.Buildings.Any(b => b.BuildingId == 1))
        {
            db.Buildings.Add(new Building
            {
                BuildingId = 1,
                SiteId = 1,
                Name = "Test Building",
                Code = "TB-01",
                Latitude = 21.0285m,
                Longitude = 105.8048m,
                IsActive = true
            });
        }

        if (!db.FacilityFloors.Any(f => f.FacilityFloorId == 1))
        {
            db.FacilityFloors.Add(new FacilityFloor
            {
                FacilityFloorId = 1,
                BuildingId = 1,
                Name = "Tầng 1",
                Code = "F1",
                SortOrder = 1,
                IsActive = true
            });
        }

        if (!db.IndoorPathNodes.Any(n => n.Id == 1))
        {
            db.IndoorPathNodes.AddRange(
                new IndoorPathNode
                {
                    Id = 1,
                    BuildingId = 1,
                    FacilityFloorId = 1,
                    Label = "Cổng vào",
                    NodeType = "Entrance",
                    X = 0, Y = 0, Z = 0,
                    IsEmergencyExit = false,
                    IsAccessible = true,
                    NeighborsJson = "[{\"NodeId\":2,\"Weight\":10.0}]"
                },
                new IndoorPathNode
                {
                    Id = 2,
                    BuildingId = 1,
                    FacilityFloorId = 1,
                    Label = "Hành lang chính",
                    NodeType = "Corridor",
                    X = 10, Y = 0, Z = 0,
                    IsEmergencyExit = false,
                    IsAccessible = true,
                    NeighborsJson = "[{\"NodeId\":1,\"Weight\":10.0},{\"NodeId\":3,\"Weight\":8.0}]"
                },
                new IndoorPathNode
                {
                    Id = 3,
                    BuildingId = 1,
                    FacilityFloorId = 1,
                    Label = "Phòng họp A",
                    NodeType = "Room",
                    X = 10, Y = 8, Z = 0,
                    IsEmergencyExit = false,
                    IsAccessible = true,
                    NeighborsJson = "[{\"NodeId\":2,\"Weight\":8.0}]"
                }
            );
        }

        await db.SaveChangesAsync();
        return 1;
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));
        return client;
    }

    private static string CreateJwtToken(int userId, string username, string role)
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
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

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }
}
