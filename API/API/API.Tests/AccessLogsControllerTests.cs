using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public sealed class AccessLogsControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private static int _idCounter;

    private readonly SecurityWebApplicationFactory _factory;

    public AccessLogsControllerTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static int NextId() => System.Threading.Interlocked.Increment(ref _idCounter);

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));
        return client;
    }

    internal static string CreateJwtToken(int userId, string username, string role, int? employeeId = null)
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Role, role),
            new("token_version", "0"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (employeeId.HasValue)
        {
            claims.Add(new Claim("employeeId", employeeId.Value.ToString()));
        }
        var token = new JwtSecurityToken(
            issuer: "VShieldAPI",
            audience: "VShieldClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<(Gate gate, Employee employee, ExceptionReason reason)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var gate = new Gate { GateId = 9000 + NextId(), GateName = "Gate " + suffix };
        var employee = new Employee { EmployeeId = 7000 + NextId(), FullName = "A-Test " + suffix, Status = true };
        var reason = new ExceptionReason { ReasonId = 8000 + NextId(), ReasonCode = "RC" + suffix, Description = "Denied " + suffix };

        db.Gates.Add(gate);
        db.Employees.Add(employee);
        db.ExceptionReasons.Add(reason);
        await db.SaveChangesAsync();

        var today = DateTime.Today;
        var logBase = 100000 + NextId() * 3;
        db.AccessLogs.AddRange(
            new AccessLog
            {
                LogId = logBase,
                Timestamp = today.AddHours(9),
                Direction = "IN",
                GateId = gate.GateId,
                EmployeeId = employee.EmployeeId,
                ResultStatus = "APPROVED",
                CapturedLicensePlate = "29A00001",
                Note = "Ok-" + suffix
            },
            new AccessLog
            {
                LogId = logBase + 1,
                Timestamp = today.AddHours(10),
                Direction = "OUT",
                GateId = gate.GateId,
                EmployeeId = employee.EmployeeId,
                ResultStatus = "FAILED",
                ExceptionReasonId = reason.ReasonId,
                Note = "Bad-" + suffix
            },
            new AccessLog
            {
                LogId = logBase + 2,
                Timestamp = DateTime.UtcNow.AddDays(-5),
                Direction = "IN",
                EmployeeId = employee.EmployeeId,
                ResultStatus = "APPROVED",
                IsBypass = true,
                Note = "Old-bypass-" + suffix
            });
        db.Vehicles.Add(new Vehicle { VehicleId = 6000 + NextId(), LicensePlate = "30E" + suffix, ParkingStatus = "IN" });
        await db.SaveChangesAsync();
        return (gate, employee, reason);
    }

    [Fact]
    public async Task GetLogs_ReturnsSeededItems()
    {
        var (gate, _, _) = await SeedAsync();
        using var admin = AdminClient();

        var response = await admin.GetAsync($"/api/access-logs?gateId={gate.GateId}&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.True(root.GetProperty("total").GetInt32() >= 2);
        Assert.Equal(gate.GateId, root.GetProperty("items")[0].GetProperty("gateId").GetInt32());
    }

    [Fact]
    public async Task GetLogs_FilterByDirectionAndStatus()
    {
        var (_, _, _) = await SeedAsync();
        using var admin = AdminClient();

        var response = await admin.GetAsync("/api/access-logs?direction=OUT&resultStatus=FAILED");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.True(root.GetProperty("total").GetInt32() >= 1);
        foreach (var item in root.GetProperty("items").EnumerateArray())
        {
            Assert.Equal("OUT", item.GetProperty("direction").GetString());
            Assert.Equal("FAILED", item.GetProperty("resultStatus").GetString());
        }
    }

    [Fact]
    public async Task GetLogs_FilterByQuery_MatchesActorName()
    {
        var (_, employee, _) = await SeedAsync();
        using var admin = AdminClient();

        var response = await admin.GetAsync($"/api/access-logs?query={Uri.EscapeDataString(employee.FullName)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("total").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetLogs_PagingWorks()
    {
        var (_, _, _) = await SeedAsync();
        using var admin = AdminClient();

        var first = await admin.GetAsync("/api/access-logs?page=1&pageSize=1");
        var doc1 = await JsonDocument.ParseAsync(await first.Content.ReadAsStreamAsync());
        Assert.Equal(1, doc1.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(1, doc1.RootElement.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task GetSummary_ReturnsCounters()
    {
        var (gate, _, _) = await SeedAsync();
        using var admin = AdminClient();

        var response = await admin.GetAsync("/api/access-logs/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.True(root.GetProperty("totalToday").GetInt32() >= 2);
        Assert.True(root.GetProperty("entriesToday").GetInt32() >= 1);
        Assert.True(root.GetProperty("exitsToday").GetInt32() >= 1);
        Assert.True(root.GetProperty("exceptionsToday").GetInt32() >= 1);
        Assert.True(root.GetProperty("vehiclesInside").GetInt32() >= 1);
        Assert.True(root.GetProperty("successRate").GetInt32() >= 0);
    }

    [Fact]
    public async Task GetExceptions_ReturnsFailedAndByPass()
    {
        var (_, _, _) = await SeedAsync();
        using var admin = AdminClient();

        var response = await admin.GetAsync("/api/access-logs/exceptions?pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        Assert.True(root.GetProperty("total").GetInt32() >= 1);
        Assert.True(root.GetProperty("summaryByReason").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetDetail_ReturnsSingleLog()
    {
        var (_, _, _) = await SeedAsync();
        using var admin = AdminClient();

        var list = await admin.GetAsync("/api/access-logs?pageSize=1");
        var doc = await JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync());
        var id = doc.RootElement.GetProperty("items")[0].GetProperty("logId").GetInt32();

        var detail = await admin.GetAsync($"/api/access-logs/{id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailDoc = await JsonDocument.ParseAsync(await detail.Content.ReadAsStreamAsync());
        Assert.Equal(id, detailDoc.RootElement.GetProperty("logId").GetInt32());
    }

    [Fact]
    public async Task GetDetail_Missing_ReturnsNotFound()
    {
        using var admin = AdminClient();
        var response = await admin.GetAsync("/api/access-logs/99999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSystemAudit_ReturnsItems()
    {
        using var admin = AdminClient();

        // Trigger a request so the audit middleware writes a SystemAuditLog row.
        var warm = await admin.GetAsync("/api/access-logs");
        Assert.Equal(HttpStatusCode.OK, warm.StatusCode);

        var response = await admin.GetAsync("/api/access-logs/system-audit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("total").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetSystemAudit_StaffRole_IsForbidden()
    {
        using var staff = _factory.CreateClient();
        staff.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(1001, "staff.test", "Staff"));
        var response = await staff.GetAsync("/api/access-logs/system-audit");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}