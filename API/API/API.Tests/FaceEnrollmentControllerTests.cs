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

public sealed class FaceEnrollmentControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private static int _idCounter;

    private readonly SecurityWebApplicationFactory _factory;

    public FaceEnrollmentControllerTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static int NextId() => System.Threading.Interlocked.Increment(ref _idCounter);

    private static string TokenFor(int userId, int employeeId)
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, "face" + userId),
            new Claim(ClaimTypes.Role, "Staff"),
            new Claim("employeeId", employeeId.ToString()),
            new Claim("token_version", "0"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken("VShieldAPI", "VShieldClient", claims,
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int SeedEmployeeWithUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var employeeId = 40000 + NextId();
        var userId = 30000 + NextId();
        db.Employees.Add(new Employee { EmployeeId = employeeId, FullName = "Face " + suffix, Status = true });
        db.AppUsers.Add(new AppUser
        {
            UserId = userId,
            Username = "face" + suffix,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Face@12345"),
            FullName = "Face User " + suffix,
            Role = "Staff",
            IsActive = true,
            TokenVersion = 0,
            EmployeeId = employeeId,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        return employeeId;
    }

    [Fact]
    public async Task GetMyFaceStatus_NoModel_ReturnsHasFaceIdFalse()
    {
        var employeeId = SeedEmployeeWithUser();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = db.AppUsers.Single(u => u.EmployeeId == employeeId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user.UserId, employeeId));

        var response = await client.GetAsync("/api/FaceEnrollment/my-status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("hasEmployee").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("hasFaceId").GetBoolean());
    }

    [Fact]
    public async Task GetMyFaceStatus_ActiveModel_ReturnsHasFaceIdTrue()
    {
        var employeeId = SeedEmployeeWithUser();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = db.AppUsers.Single(u => u.EmployeeId == employeeId);
        db.EmployeeFaceModels.Add(new EmployeeFaceModel
        {
            Id = 90000 + NextId(),
            EmployeeId = employeeId,
            ModelFileName = "m" + Guid.NewGuid().ToString("N")[..8] + ".bin",
            ModelPath = "/models/m.bin",
            Status = FaceModelLifecycleStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            Version = 1,
            EncodingCount = 32,
            ModelChecksum = "abc123"
        });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user.UserId, employeeId));

        var response = await client.GetAsync("/api/FaceEnrollment/my-status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("hasFaceId").GetBoolean());
        Assert.Equal(32, doc.RootElement.GetProperty("encodingCount").GetInt32());
    }

    [Fact]
    public async Task SubmitRemote_CreatesJobAndFrames()
    {
        var employeeId = SeedEmployeeWithUser();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = db.AppUsers.Single(u => u.EmployeeId == employeeId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user.UserId, employeeId));

        var response = await client.PostAsJsonAsync("/api/FaceEnrollment/submit-remote",
            new { images = new[] { "data:image/jpeg;base64,AAAA", "data:image/jpeg;base64,BBBB" } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        var jobId = doc.RootElement.GetProperty("jobId").GetGuid();

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var job = await db2.RemoteFaceEnrollmentJobs.FindAsync(jobId);
        Assert.NotNull(job);
        Assert.Equal("Pending", job!.Status);
        Assert.Equal(2, db2.RemoteFaceEnrollmentFrames.Count(f => f.JobId == jobId));
    }

    [Fact]
    public async Task SubmitRemote_NoImages_ReturnsBadRequest()
    {
        var employeeId = SeedEmployeeWithUser();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = db.AppUsers.Single(u => u.EmployeeId == employeeId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user.UserId, employeeId));

        var response = await client.PostAsJsonAsync("/api/FaceEnrollment/submit-remote", new { images = new string[0] });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitRemote_UnlinkedAccount_ReturnsBadRequest()
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("VShieldAPI", "VShieldClient",
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "1001"),
                new Claim(ClaimTypes.Role, "Staff"),
                new Claim("token_version", "0"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            },
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));

        var response = await client.PostAsJsonAsync("/api/FaceEnrollment/submit-remote", new { images = new[] { "AAA" } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}