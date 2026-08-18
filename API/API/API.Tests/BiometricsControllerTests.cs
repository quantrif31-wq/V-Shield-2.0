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

public sealed class BiometricsControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private static int _idCounter;

    private readonly SecurityWebApplicationFactory _factory;

    public BiometricsControllerTests(SecurityWebApplicationFactory factory)
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
    public async Task Overview_ReturnsSummaryAndEmployees()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var empId = 40000 + NextId();
        var deptId = 50000 + NextId();
        var posId = 60000 + NextId();

        db.Departments.Add(new Department { DepartmentId = deptId, Name = "Dept " + suffix });
        db.Positions.Add(new Position { PositionId = posId, Name = "Pos " + suffix });
        var emp = new Employee
        {
            EmployeeId = empId,
            FullName = "Bio " + suffix,
            Email = "bio" + suffix + "@x.com",
            Phone = "0900000000",
            Status = true,
            DepartmentId = deptId,
            PositionId = posId
        };
        db.Employees.Add(emp);
        db.Vehicles.Add(new Vehicle { VehicleId = 70000 + NextId(), LicensePlate = "51F" + suffix, ParkingStatus = "OUT", EmployeeId = empId });
        db.EmployeeFaceVideos.Add(new EmployeeFaceVideo { Id = 80000 + NextId(), EmployeeId = empId, FileName = "vid.mp4", FilePath = "/videos/vid.mp4", FileSize = 100, CreatedAt = DateTime.UtcNow });
        db.EmployeeFaceModels.Add(new EmployeeFaceModel { Id = 90000 + NextId(), EmployeeId = empId, ModelFileName = "model.bin", ModelPath = "/models/model.bin", Status = "Active", CreatedAt = DateTime.UtcNow, Version = 1 });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.GetAsync("/api/biometrics/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.True(root.GetProperty("summary").GetProperty("totalEmployees").GetInt32() >= 1);
        Assert.True(root.GetProperty("summary").GetProperty("trainedEmployees").GetInt32() >= 1);
        Assert.True(root.GetProperty("employees").GetArrayLength() >= 1);
        Assert.True(root.GetProperty("recentVideos").GetArrayLength() >= 1);
        Assert.True(root.GetProperty("recentModels").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Overview_WithQuery_FiltersEmployees()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var empId = 40000 + NextId();
        db.Employees.Add(new Employee { EmployeeId = empId, FullName = "QueryTarget-" + suffix, Status = true });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());

        var response = await client.GetAsync($"/api/biometrics/overview?query={Uri.EscapeDataString("QueryTarget-" + suffix)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var employees = doc.RootElement.GetProperty("employees");
        Assert.Equal(1, employees.GetArrayLength());
        Assert.Equal(empId, employees[0].GetProperty("employeeId").GetInt32());
    }
}