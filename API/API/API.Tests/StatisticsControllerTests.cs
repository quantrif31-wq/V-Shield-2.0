using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public sealed class StatisticsControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private static int _idCounter;

    private readonly SecurityWebApplicationFactory _factory;

    public StatisticsControllerTests(SecurityWebApplicationFactory factory)
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
    public async Task EmployeesSummary_ReturnsGroupedCounts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var deptId = 50000 + NextId();
        var posId = 60000 + NextId();

        db.Departments.Add(new Department { DepartmentId = deptId, Name = "StatsDept " + suffix });
        db.Positions.Add(new Position { PositionId = posId, Name = "StatsPos " + suffix });
        db.Employees.AddRange(
            new Employee { EmployeeId = 40000 + NextId(), FullName = "StatsA " + suffix, Status = true, DepartmentId = deptId, PositionId = posId },
            new Employee { EmployeeId = 40000 + NextId(), FullName = "StatsB " + suffix, Status = true, DepartmentId = deptId, PositionId = posId },
            new Employee { EmployeeId = 40000 + NextId(), FullName = "StatsC " + suffix, Status = false, DepartmentId = deptId });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.GetAsync("/api/Statistics/employees/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.True(root.GetProperty("totalEmployees").GetInt32() >= 3);
        Assert.True(root.GetProperty("activeEmployees").GetInt32() >= 2);
        Assert.True(root.GetProperty("inactiveEmployees").GetInt32() >= 1);
        Assert.True(root.GetProperty("byDepartment").GetArrayLength() >= 1);
        Assert.True(root.GetProperty("byPosition").GetArrayLength() >= 1);
    }
}