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

public sealed class DynamicQrControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private static int _idCounter;

    private readonly SecurityWebApplicationFactory _factory;

    public DynamicQrControllerTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static int NextId() => System.Threading.Interlocked.Increment(ref _idCounter);

    private static string TokenFor(int userId, int employeeId, string role = "Staff")
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, "user" + userId),
            new Claim(ClaimTypes.Role, role),
            new Claim("employeeId", employeeId.ToString()),
            new Claim("token_version", "0"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken("VShieldAPI", "VShieldClient", claims,
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (int userId, int employeeId) SeedEmployee()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var employeeId = 40000 + NextId();
        var userId = 30000 + NextId();
        db.Employees.Add(new Employee { EmployeeId = employeeId, FullName = "QrUser " + suffix, Status = true });
        db.AppUsers.Add(new AppUser
        {
            UserId = userId,
            Username = "qr" + suffix,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qr@12345"),
            FullName = "Qr User " + suffix,
            Role = "Staff",
            IsActive = true,
            TokenVersion = 0,
            EmployeeId = employeeId,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        return (userId, employeeId);
    }

    [Fact]
    public async Task GetMyDynamicQr_CreatesAndReturnsPayload()
    {
        var (userId, employeeId) = SeedEmployee();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(userId, employeeId));

        var response = await client.PostAsync("/api/dynamic-qr/my", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(employeeId, doc.RootElement.GetProperty("data").GetProperty("employeeId").GetInt32());
        var payload = doc.RootElement.GetProperty("data").GetProperty("qrPayload").GetString();
        Assert.StartsWith($"EMP:{employeeId}|TS:", payload);
        Assert.True(doc.RootElement.GetProperty("data").GetProperty("remainingSeconds").GetInt32() >= 0);
    }

    [Fact]
    public async Task GetMyDynamicQr_MissingEmployeeClaim_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("VShieldAPI", "VShieldClient",
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "1002"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("token_version", "0"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            },
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));

        var response = await client.PostAsync("/api/dynamic-qr/my", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyMobileQrBootstrap_ReturnsSecret()
    {
        var (userId, employeeId) = SeedEmployee();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(userId, employeeId));

        var response = await client.GetAsync("/api/dynamic-qr/mobile-bootstrap");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(employeeId, doc.RootElement.GetProperty("data").GetProperty("employeeId").GetInt32());
        Assert.NotNull(doc.RootElement.GetProperty("data").GetProperty("secretKey").GetString());
    }

    [Fact]
    public async Task GenerateDynamicQr_AsAdmin_ForOtherEmployee()
    {
        var (_, employeeId) = SeedEmployee();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(1002, employeeId, "Admin"));

        var response = await client.PostAsJsonAsync("/api/dynamic-qr/generate", new { employeeId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.StartsWith($"EMP:{employeeId}|", doc.RootElement.GetProperty("data").GetProperty("qrPayload").GetString());
    }

    [Fact]
    public async Task GenerateDynamicQr_InvalidEmployeeId_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(1002, 1, "Admin"));

        var response = await client.PostAsJsonAsync("/api/dynamic-qr/generate", new { employeeId = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}