using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public sealed class CrudControllerTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly SecurityWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CrudControllerTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(1002, "admin.test", "Admin"));
        return client;
    }

    private static string CreateJwtToken(int userId, string username, string role)
    {
        const string secret = "LOCAL_DEVELOPMENT_ONLY_CHANGE_ME_32CHARS!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
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

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    [Fact]
    public async Task Departments_CrudFlow()
    {
        using var admin = AdminClient();

        var list = await admin.GetAsync("/api/departments");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var create = await admin.PostAsJsonAsync("/api/departments", new { name = "Dept " + Guid.NewGuid().ToString("N")[..8] });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("departmentId").GetInt32();

        var byId = await admin.GetAsync($"/api/departments/{id}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        var update = await admin.PutAsJsonAsync($"/api/departments/{id}", new { name = "Renamed " + Guid.NewGuid().ToString("N")[..8] });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await admin.DeleteAsync($"/api/departments/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Departments_NotFoundAndDuplicate()
    {
        using var admin = AdminClient();

        var missing = await admin.GetAsync("/api/departments/99999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var name = "Unique-Dept-" + Guid.NewGuid().ToString("N")[..8];
        var first = await admin.PostAsJsonAsync("/api/departments", new { name });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var dup = await admin.PostAsJsonAsync("/api/departments", new { name });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task Positions_CrudFlow()
    {
        using var admin = AdminClient();

        var create = await admin.PostAsJsonAsync("/api/positions", new { name = "Pos " + Guid.NewGuid().ToString("N")[..8] });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("positionId").GetInt32();

        var list = await admin.GetAsync("/api/positions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var update = await admin.PutAsJsonAsync($"/api/positions/{id}", new { name = "Renamed Pos" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await admin.DeleteAsync($"/api/positions/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Positions_NotFound()
    {
        using var admin = AdminClient();
        var missing = await admin.GetAsync("/api/positions/99999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Shifts_CrudAndDeactivate()
    {
        using var admin = AdminClient();

        var create = await admin.PostAsJsonAsync("/api/shifts", new
        {
            shiftName = "Shift " + Guid.NewGuid().ToString("N")[..8],
            startTime = new TimeSpan(8, 0, 0),
            endTime = new TimeSpan(17, 0, 0),
            breakMinutes = 60,
            allowedLateMinutes = 10,
            allowedEarlyLeaveMinutes = 10
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("shiftId").GetInt32();

        var list = await admin.GetAsync("/api/shifts");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var deactivate = await admin.PatchAsync($"/api/shifts/{id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var delete = await admin.DeleteAsync($"/api/shifts/{id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task Employees_CreateGetAndList()
    {
        using var admin = AdminClient();

        var create = await admin.PostAsJsonAsync("/api/employees", new
        {
            fullName = "Nguyen Test " + Guid.NewGuid().ToString("N")[..6],
            status = true,
            email = $"test{Guid.NewGuid():N}@test.com"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("employeeId").GetInt32();

        var byId = await admin.GetAsync($"/api/employees/{id}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        var list = await admin.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var filtered = await admin.GetAsync("/api/employees?status=true&search=Nguyen");
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
    }

    [Fact]
    public async Task Employees_AnonymousRejected()
    {
        var response = await _client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Vehicles_GetTypesAndCrud()
    {
        using var admin = AdminClient();

        var types = await admin.GetAsync("/api/vehicles/types");
        Assert.Equal(HttpStatusCode.OK, types.StatusCode);

        var createEmp = await admin.PostAsJsonAsync("/api/employees", new
        {
            fullName = "Vehicle Owner " + Guid.NewGuid().ToString("N")[..6],
            status = true
        });
        var empDoc = await ReadJsonAsync(createEmp);
        var employeeId = empDoc.RootElement.GetProperty("employeeId").GetInt32();

        var create = await admin.PostAsJsonAsync("/api/vehicles", new
        {
            licensePlate = $"29A-{Guid.NewGuid().ToString("N")[..5].ToUpper()}",
            employeeId,
            vehicleTypeId = (int?)null
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var vehicleId = created.RootElement.GetProperty("vehicleId").GetInt32();

        var list = await admin.GetAsync("/api/vehicles");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var byEmployee = await admin.GetAsync($"/api/vehicles/employee/{employeeId}");
        Assert.Equal(HttpStatusCode.OK, byEmployee.StatusCode);

        var delete = await admin.DeleteAsync($"/api/vehicles/{vehicleId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_PrivilegedEndpoints_Rejected()
    {
        var paths = new[] { "/api/departments", "/api/positions", "/api/shifts", "/api/vehicles/types" };
        foreach (var path in paths)
        {
            var response = await _client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task Reports_AttendanceEndpoints_ReturnOk()
    {
        using var admin = AdminClient();

        var daily = await admin.GetAsync("/api/reports/attendance/daily");
        Assert.Equal(HttpStatusCode.OK, daily.StatusCode);

        var monthly = await admin.GetAsync("/api/reports/attendance/monthly");
        Assert.Equal(HttpStatusCode.OK, monthly.StatusCode);

        var department = await admin.GetAsync("/api/reports/attendance/department");
        Assert.Equal(HttpStatusCode.OK, department.StatusCode);

        var late = await admin.GetAsync("/api/reports/attendance/late");
        Assert.Equal(HttpStatusCode.OK, late.StatusCode);

        var overtime = await admin.GetAsync("/api/reports/attendance/overtime");
        Assert.Equal(HttpStatusCode.OK, overtime.StatusCode);

        var leave = await admin.GetAsync("/api/reports/leave/monthly");
        Assert.Equal(HttpStatusCode.OK, leave.StatusCode);
    }

    [Fact]
    public async Task Reports_WithSeededData_ReturnsCounts()
    {
        var today = DateTime.Today;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var employee = new Employee
            {
                EmployeeId = 9001,
                FullName = "Report Employee",
                Status = true,
                LifecycleStatus = EmployeeLifecycleStates.Active
            };
            if (!db.Employees.Any(e => e.EmployeeId == 9001))
            {
                db.Employees.Add(employee);
                var shift = new Shift { ShiftName = "Ca hành chính", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 0, 0) };
                db.Shifts.Add(shift);
                await db.SaveChangesAsync();
                db.WorkSchedules.Add(new WorkSchedule { EmployeeId = 9001, ShiftId = shift.ShiftId, WorkDate = today, Status = WorkScheduleStatuses.Scheduled });
                db.Attendances.Add(new Attendance { EmployeeId = 9001, WorkDate = today, CheckIn = today.AddHours(8), CheckOut = today.AddHours(17), Status = AttendanceStatuses.Completed });
                await db.SaveChangesAsync();
            }
        }

        using var admin = AdminClient();
        var response = await admin.GetAsync("/api/reports/attendance/daily?date=" + today.ToString("yyyy-MM-dd"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await ReadJsonAsync(response);
        Assert.True(doc.RootElement.GetProperty("scheduledEmployees").GetInt32() >= 1);
    }

    [Fact]
    public async Task Users_ReadEndpoints_ReturnOk()
    {
        using var admin = AdminClient();

        var all = await admin.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        var doc = await ReadJsonAsync(all);
        Assert.True(doc.RootElement.GetArrayLength() >= 3);

        var scopeRef = await admin.GetAsync("/api/users/scope-reference");
        Assert.Equal(HttpStatusCode.OK, scopeRef.StatusCode);

        var gateRef = await admin.GetAsync("/api/users/gate-access-reference");
        Assert.Equal(HttpStatusCode.OK, gateRef.StatusCode);
    }

    [Fact]
    public async Task Users_GetById_ReturnsSeedAdmin()
    {
        using var admin = AdminClient();
        var response = await admin.GetAsync("/api/users/1002");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await ReadJsonAsync(response);
        Assert.Equal("admin.test", doc.RootElement.GetProperty("username").GetString());
    }

    [Fact]
    public async Task Users_GetById_NotFound()
    {
        using var admin = AdminClient();
        var response = await admin.GetAsync("/api/users/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeviceManagement_CameraAndGateCrud()
    {
        using var admin = AdminClient();

        var overview = await admin.GetAsync("/api/device-management/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var cameras = await admin.GetAsync("/api/device-management/cameras");
        Assert.Equal(HttpStatusCode.OK, cameras.StatusCode);

        var createGate = await admin.PostAsJsonAsync("/api/device-management/gates", new
        {
            gateName = "Gate " + Guid.NewGuid().ToString("N")[..8],
            location = "Lobby"
        });
        Assert.Equal(HttpStatusCode.Created, createGate.StatusCode);
        var gateDoc = await ReadJsonAsync(createGate);
        var gateId = gateDoc.RootElement.GetProperty("gateId").GetInt32();

        var createCamera = await admin.PostAsJsonAsync("/api/device-management/cameras", new
        {
            cameraName = "Cam " + Guid.NewGuid().ToString("N")[..6],
            cameraType = "IP",
            gateId,
            streamUrl = "rtsp://test.local/stream"
        });
        Assert.Equal(HttpStatusCode.Created, createCamera.StatusCode);
        var cameraDoc = await ReadJsonAsync(createCamera);
        var cameraId = cameraDoc.RootElement.GetProperty("cameraId").GetInt32();

        var updateCamera = await admin.PutAsJsonAsync($"/api/device-management/cameras/{cameraId}", new
        {
            cameraName = "Renamed Cam",
            cameraType = "IP",
            gateId,
            streamUrl = "rtsp://test.local/stream"
        });
        Assert.Equal(HttpStatusCode.OK, updateCamera.StatusCode);

        var deleteCamera = await admin.DeleteAsync($"/api/device-management/cameras/{cameraId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCamera.StatusCode);

        var deleteGate = await admin.DeleteAsync($"/api/device-management/gates/{gateId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteGate.StatusCode);
    }

    [Fact]
    public async Task DeviceManagement_CameraValidationErrors()
    {
        using var admin = AdminClient();

        var empty = await admin.PostAsJsonAsync("/api/device-management/cameras", new { cameraName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        var badGate = await admin.PostAsJsonAsync("/api/device-management/cameras", new
        {
            cameraName = "Cam X",
            gateId = 999999
        });
        Assert.Equal(HttpStatusCode.BadRequest, badGate.StatusCode);
    }

    [Fact]
    public async Task CampusMap_LayoutAndScene3d()
    {
        int siteId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var site = await db.Sites.FirstOrDefaultAsync();
            if (site == null)
            {
                var company = new Company { Name = "Map Co", Code = "MAPCO" };
                db.Companies.Add(company);
                await db.SaveChangesAsync();
                site = new Site { CompanyId = company.CompanyId, Name = "Map Site", Code = "MAPSITE" };
                db.Sites.Add(site);
                await db.SaveChangesAsync();
            }
            siteId = site.SiteId;
        }

        using var admin = AdminClient();

        var layout = await admin.GetAsync("/api/campus-map/layout");
        Assert.Equal(HttpStatusCode.OK, layout.StatusCode);

        var scene3d = await admin.GetAsync("/api/campus-map/scene3d");
        Assert.Equal(HttpStatusCode.OK, scene3d.StatusCode);

        var createObject = await admin.PostAsJsonAsync("/api/campus-map/scene3d/objects", new
        {
            siteId,
            objectType = "building",
            label = "Building A",
            positionX = 1.0m,
            positionY = 2.0m,
            positionZ = 0.0m,
            width = 10m,
            length = 20m,
            height = 15m,
            floors = 5,
            rotation = 0.0m,
            color = "#cccccc",
            propertiesJson = (string?)null,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, createObject.StatusCode);
        var objDoc = await ReadJsonAsync(createObject);
        var objectId = objDoc.RootElement.GetProperty("id").GetInt32();

        var deleteObject = await admin.DeleteAsync($"/api/campus-map/scene3d/objects/{objectId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteObject.StatusCode);
    }

    [Fact]
    public async Task FaceGate_GatesAndCheckAccess()
    {
        using var admin = AdminClient();

        var gates = await admin.GetAsync("/api/face-gate/gates");
        Assert.Equal(HttpStatusCode.OK, gates.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!db.Employees.Any(e => e.EmployeeId == 7001))
            {
                db.Employees.Add(new Employee
                {
                    EmployeeId = 7001,
                    FullName = "Face Gate Employee",
                    Status = true,
                    LifecycleStatus = EmployeeLifecycleStates.Active
                });
                await db.SaveChangesAsync();
            }
        }

        var unknown = await admin.GetAsync("/api/face-gate/check-access?employeeId=999999");
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        var unknownDoc = await ReadJsonAsync(unknown);
        Assert.Equal("unknown-employee", unknownDoc.RootElement.GetProperty("reason").GetString());

        var known = await admin.GetAsync("/api/face-gate/check-access?employeeId=7001");
        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        var knownDoc = await ReadJsonAsync(known);
        Assert.True(knownDoc.RootElement.GetProperty("known").GetBoolean());
    }

    [Fact]
    public async Task FaceGate_VerifyPassword_ValidatesCredentials()
    {
        using var admin = AdminClient();

        var wrong = await admin.PostAsJsonAsync("/api/face-gate/verify-password", new { password = "WrongPass!" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        var empty = await admin.PostAsJsonAsync("/api/face-gate/verify-password", new { password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task FaceGate_RecordUnknown_StoresIntruder()
    {
        using var admin = AdminClient();

        var record = await admin.PostAsJsonAsync("/api/face-gate/record", new
        {
            employeeId = 0,
            decision = "unknown",
            gateId = 1,
            gateName = "Gate 1",
            cameraId = "cam-1",
            direction = "IN"
        });
        Assert.Equal(HttpStatusCode.OK, record.StatusCode);

        var intruders = await admin.GetAsync("/api/face-gate/intruders");
        Assert.Equal(HttpStatusCode.OK, intruders.StatusCode);
    }

    [Fact]
    public async Task AccessPermission_MatrixEndpoints()
    {
        using var admin = AdminClient();

        var employeeMatrix = await admin.GetAsync("/api/access-permissions/employee-matrix");
        Assert.Equal(HttpStatusCode.OK, employeeMatrix.StatusCode);

        var positionMatrix = await admin.GetAsync("/api/access-permissions/position-matrix");
        Assert.Equal(HttpStatusCode.OK, positionMatrix.StatusCode);

        var visitorMatrix = await admin.GetAsync("/api/access-permissions/visitor-matrix");
        Assert.Equal(HttpStatusCode.OK, visitorMatrix.StatusCode);
    }

    [Fact]
    public async Task Dashboard_OverviewIntelligenceReports()
    {
        using var admin = AdminClient();

        var overview = await admin.GetAsync("/api/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var intelligence = await admin.GetAsync("/api/dashboard/intelligence");
        Assert.Equal(HttpStatusCode.OK, intelligence.StatusCode);

        var reports = await admin.GetAsync("/api/dashboard/reports");
        Assert.Equal(HttpStatusCode.OK, reports.StatusCode);
    }

    [Fact]
    public async Task GuestProfiles_CrudFlow()
    {
        using var admin = AdminClient();

        var list = await admin.GetAsync("/api/guest-profiles");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var create = await admin.PostAsJsonAsync("/api/guest-profiles", new
        {
            fullName = "Guest " + Guid.NewGuid().ToString("N")[..6],
            phone = "0901234567",
            defaultLicensePlate = "30A-12345"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("guestId").GetInt32();

        var byId = await admin.GetAsync($"/api/guest-profiles/{id}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        var update = await admin.PutAsJsonAsync($"/api/guest-profiles/{id}", new
        {
            fullName = "Renamed Guest",
            phone = "0912345678"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await admin.DeleteAsync($"/api/guest-profiles/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task GuestProfiles_NotFoundAndValidation()
    {
        using var admin = AdminClient();

        var missing = await admin.GetAsync("/api/guest-profiles/99999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var empty = await admin.PostAsJsonAsync("/api/guest-profiles", new { fullName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task LeaveRequests_ListAndValidation()
    {
        using var admin = AdminClient();

        var all = await admin.GetAsync("/api/leave-requests");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);

        var invalidType = await admin.PostAsJsonAsync("/api/leave-requests", new
        {
            leaveType = "NotARealType",
            startDate = DateTime.Today,
            endDate = DateTime.Today.AddDays(1),
            reason = "Test"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidType.StatusCode);

        var reversedDates = await admin.PostAsJsonAsync("/api/leave-requests", new
        {
            leaveType = LeaveTypes.AnnualLeave,
            startDate = DateTime.Today.AddDays(2),
            endDate = DateTime.Today,
            reason = "Test"
        });
        Assert.Equal(HttpStatusCode.BadRequest, reversedDates.StatusCode);
    }

    [Fact]
    public async Task EnterpriseLostFound_OverviewAndLostItemCrud()
    {
        using var admin = AdminClient();

        var overview = await admin.GetAsync("/api/enterprise/lost-found/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var create = await admin.PostAsJsonAsync("/api/enterprise/lost-found/lost-items", new
        {
            reporterName = "Nguyen Van A",
            reporterPhone = "0901234567",
            reporterIdNumber = "001202012345",
            reporterPhotoUrl = "http://x/photo.jpg",
            itemDescription = "Túi xách màu đen",
            lastSeenLocation = "Tầng 1",
            lostAtUtc = DateTime.UtcNow,
            photoUrl = "http://x/item.jpg"
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var lostItemId = created.RootElement.GetProperty("lostItemReportId").GetInt64();

        var list = await admin.GetAsync("/api/enterprise/lost-found/lost-items");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var byId = await admin.GetAsync($"/api/enterprise/lost-found/lost-items/{lostItemId}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        var close = await admin.PatchAsync($"/api/enterprise/lost-found/lost-items/{lostItemId}/close", null);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
    }

    [Fact]
    public async Task EnterpriseLostFound_LostItemValidation()
    {
        using var admin = AdminClient();

        var missing = await admin.GetAsync("/api/enterprise/lost-found/lost-items/99999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var invalid = await admin.PostAsJsonAsync("/api/enterprise/lost-found/lost-items", new
        {
            reporterName = "",
            reporterPhone = "",
            itemDescription = ""
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task EnterpriseLostFound_FoundItemListAndOverview()
    {
        using var admin = AdminClient();

        var overview = await admin.GetAsync("/api/enterprise/lost-found/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var foundList = await admin.GetAsync("/api/enterprise/lost-found/found-items");
        Assert.Equal(HttpStatusCode.OK, foundList.StatusCode);
    }

    [Fact]
    public async Task PreRegistration_ValidateToken_HandlesMissingAndUsed()
    {
        var missing = await _client.GetAsync("/api/pre-registrations/validate/nonexistent-token");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emp = new Employee
            {
                EmployeeId = 8001,
                FullName = "PreReg Host",
                Status = true,
                LifecycleStatus = EmployeeLifecycleStates.Active
            };
            if (!db.Employees.Any(e => e.EmployeeId == 8001))
            {
                db.Employees.Add(emp);
                await db.SaveChangesAsync();
            }
            if (!db.RegistrationLinks.Any(l => l.Token == "used-token-123"))
            {
                db.RegistrationLinks.Add(new RegistrationLink
                {
                    Token = "used-token-123",
                    HostEmployeeId = 8001,
                    ExpiredAt = DateTime.Now.AddDays(7),
                    IsUsed = true
                });
                await db.SaveChangesAsync();
            }
        }

        var used = await _client.GetAsync("/api/pre-registrations/validate/used-token-123");
        Assert.Equal(HttpStatusCode.BadRequest, used.StatusCode);
    }

    [Fact]
    public async Task PreRegistration_ValidateValidToken_ReturnsHostInfo()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emp = new Employee
            {
                EmployeeId = 8002,
                FullName = "PreReg Host 2",
                Phone = "0900000000",
                Email = "host2@test.com",
                Status = true,
                LifecycleStatus = EmployeeLifecycleStates.Active
            };
            if (!db.Employees.Any(e => e.EmployeeId == 8002))
            {
                db.Employees.Add(emp);
                await db.SaveChangesAsync();
            }
            if (!db.RegistrationLinks.Any(l => l.Token == "valid-token-456"))
            {
                db.RegistrationLinks.Add(new RegistrationLink
                {
                    Token = "valid-token-456",
                    HostEmployeeId = 8002,
                    ExpiredAt = DateTime.Now.AddDays(7),
                    IsUsed = false
                });
                await db.SaveChangesAsync();
            }
        }

        var response = await _client.GetAsync("/api/pre-registrations/validate/valid-token-456");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await ReadJsonAsync(response);
        Assert.Equal("PreReg Host 2", doc.RootElement.GetProperty("hostEmployeeName").GetString());
    }

    [Fact]
    public async Task PreRegistration_AdminListEndpoints()
    {
        using var admin = AdminClient();

        var all = await admin.GetAsync("/api/pre-registrations");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
    }

    [Fact]
    public async Task NotificationRules_CrudFlow()
    {
        using var admin = AdminClient();

        var list = await admin.GetAsync("/api/notification-rules");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var create = await admin.PostAsJsonAsync("/api/notification-rules", new
        {
            eventType = "Alarm.Test",
            severityMin = "High",
            recipientRole = "Admin",
            notifyWeb = true,
            notifyMobile = true,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var update = await admin.PutAsJsonAsync($"/api/notification-rules/{id}", new
        {
            eventType = "Alarm.Test",
            severityMin = "Critical",
            recipientRole = "Admin",
            notifyWeb = true,
            notifyMobile = true,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var suggestions = await admin.GetAsync("/api/notification-rules/suggestions");
        Assert.Equal(HttpStatusCode.OK, suggestions.StatusCode);

        var delete = await admin.DeleteAsync($"/api/notification-rules/{id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task Notifications_ListAndReadFlow()
    {
        using var admin = AdminClient();

        var list = await admin.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var unread = await admin.GetAsync("/api/notifications/unread-count");
        Assert.Equal(HttpStatusCode.OK, unread.StatusCode);

        var readAll = await admin.PostAsJsonAsync("/api/notifications/read-all", new { });
        Assert.Equal(HttpStatusCode.OK, readAll.StatusCode);
    }

    [Fact]
    public async Task ImportExport_GetFormatsAndEntities()
    {
        using var admin = AdminClient();

        var formats = await admin.GetAsync("/api/import-export/formats");
        Assert.Equal(HttpStatusCode.OK, formats.StatusCode);

        var entities = await admin.GetAsync("/api/import-export/entities");
        Assert.Equal(HttpStatusCode.OK, entities.StatusCode);

        var history = await admin.GetAsync("/api/import-export/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
    }

    [Fact]
    public async Task ImportExport_DownloadTemplate()
    {
        using var admin = AdminClient();

        var template = await admin.GetAsync("/api/import-export/Employee/template?format=csv");
        Assert.Equal(HttpStatusCode.OK, template.StatusCode);
        Assert.Equal("text/csv", template.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ImportExport_ExportAndDownload()
    {
        using var admin = AdminClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!db.Employees.Any(e => e.EmployeeId == 8100))
            {
                db.Employees.Add(new Employee
                {
                    EmployeeId = 8100,
                    FullName = "Export Employee",
                    Status = true,
                    LifecycleStatus = EmployeeLifecycleStates.Active
                });
                await db.SaveChangesAsync();
            }
        }

        var export = await admin.GetAsync("/api/import-export/Employee/export?format=csv");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        var doc = await ReadJsonAsync(export);
        Assert.True(doc.RootElement.GetProperty("historyId").GetGuid() != Guid.Empty);

        var history = await admin.GetAsync("/api/import-export/history?entityType=Employee");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
    }

    [Fact]
    public async Task ExceptionReasons_CrudFlow()
    {
        using var admin = AdminClient();

        var list = await admin.GetAsync("/api/exception-reasons");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var create = await admin.PostAsJsonAsync("/api/exception-reasons", new
        {
            reasonCode = "TEST-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            description = "Test reason"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("reasonId").GetInt32();

        var update = await admin.PutAsJsonAsync($"/api/exception-reasons/{id}", new
        {
            reasonCode = "TEST-UPDATED",
            description = "Updated"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await admin.DeleteAsync($"/api/exception-reasons/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task SystemConfig_UpsertAndGet()
    {
        using var admin = AdminClient();

        var key = "test-key-" + Guid.NewGuid().ToString("N")[..6];
        var upsert = await admin.PutAsJsonAsync($"/api/system-config/{key}", new { value = "some-value" });
        Assert.Equal(HttpStatusCode.OK, upsert.StatusCode);

        var get = await admin.GetAsync($"/api/system-config/{key}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task SecurityAlerts_ActiveAlerts()
    {
        using var admin = AdminClient();
        var response = await admin.GetAsync("/api/security-alerts/active");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EnterpriseSoc_AlarmRuleAndAlarmFlow()
    {
        using var admin = AdminClient();

        var overview = await admin.GetAsync("/api/enterprise/soc/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var createRule = await admin.PostAsJsonAsync("/api/enterprise/soc/alarm-rules", new
        {
            name = "Rule " + Guid.NewGuid().ToString("N")[..8],
            eventType = "Alarm.Generic",
            severity = "Critical",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, createRule.StatusCode);

        var createAlarm = await admin.PostAsJsonAsync("/api/enterprise/soc/alarms", new
        {
            alarmType = "Generic",
            severity = "High",
            summary = "Test alarm for SOC",
            siteId = (int?)null
        });
        Assert.Equal(HttpStatusCode.OK, createAlarm.StatusCode);
        var alarmDoc = await ReadJsonAsync(createAlarm);
        var alarmId = alarmDoc.RootElement.GetProperty("alarmId").GetInt64();

        var ack = await admin.PatchAsJsonAsync($"/api/enterprise/soc/alarms/{alarmId}/acknowledge", new { });
        Assert.Equal(HttpStatusCode.OK, ack.StatusCode);

        var close = await admin.PatchAsJsonAsync($"/api/enterprise/soc/alarms/{alarmId}/close", new { note = "Closed" });
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
    }

    [Fact]
    public async Task EnterpriseSoc_SopTemplateAndExecution()
    {
        using var admin = AdminClient();

        var createSop = await admin.PostAsJsonAsync("/api/enterprise/soc/sop-templates", new
        {
            name = "SOP " + Guid.NewGuid().ToString("N")[..8],
            alarmType = "Generic",
            version = 1,
            checklistJson = """["step1","step2"]"""
        });
        Assert.Equal(HttpStatusCode.OK, createSop.StatusCode);
        int sopId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await db.SopTemplates.FirstOrDefaultAsync(t => t.Name.StartsWith("SOP "));
            Assert.NotNull(stored);
            Assert.True(stored!.IsActive);
            sopId = stored.SopTemplateId;
        }

        var createAlarm = await admin.PostAsJsonAsync("/api/enterprise/soc/alarms", new
        {
            alarmType = "Generic",
            severity = "High",
            summary = "SOP test alarm",
            siteId = (int?)null
        });
        Assert.Equal(HttpStatusCode.OK, createAlarm.StatusCode);
        var alarmDoc = await ReadJsonAsync(createAlarm);
        var alarmId = alarmDoc.RootElement.GetProperty("alarmId").GetInt64();

        var start = await admin.PostAsJsonAsync("/api/enterprise/soc/sop-executions", new
        {
            alarmId,
            sopTemplateId = sopId
        });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var execDoc = await ReadJsonAsync(start);
        var execId = execDoc.RootElement.GetProperty("sopExecutionId").GetInt64();

        var complete = await admin.PatchAsJsonAsync($"/api/enterprise/soc/sop-executions/{execId}/complete", new
        {
            completedStepsJson = """["step1","step2"]"""
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
    }

    [Fact]
    public async Task Attendances_ListEndpoints()
    {
        using var admin = AdminClient();

        var all = await admin.GetAsync("/api/attendances");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);

        var zoneTransits = await admin.GetAsync("/api/attendances/zone-transits");
        Assert.Equal(HttpStatusCode.OK, zoneTransits.StatusCode);

        var missing = await admin.GetAsync("/api/attendances/99999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task EnterpriseDevice_ReadAndHealthFlow()
    {
        using var admin = AdminClient();

        var healthInsights = await admin.GetAsync("/api/enterprise/devices/health-insights");
        Assert.Equal(HttpStatusCode.OK, healthInsights.StatusCode);

        var overview = await admin.GetAsync("/api/enterprise/devices/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var devices = await admin.GetAsync("/api/enterprise/devices");
        Assert.Equal(HttpStatusCode.OK, devices.StatusCode);

        var create = await admin.PostAsJsonAsync("/api/enterprise/devices", new
        {
            siteId = (int?)null,
            accessPointId = (int?)null,
            deviceType = "Controller",
            name = "Device " + Guid.NewGuid().ToString("N")[..8],
            vendor = "TestVendor",
            model = "ModelX",
            serialNumber = "SN-" + Guid.NewGuid().ToString("N")[..8],
            firmwareVersion = "1.0",
            configurationVersion = "v1"
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var deviceDoc = await ReadJsonAsync(create);
        var deviceId = deviceDoc.RootElement.GetProperty("securityDeviceId").GetInt32();

        var health = await admin.PostAsJsonAsync($"/api/enterprise/devices/{deviceId}/health", new
        {
            status = "Healthy",
            message = "All good",
            latencyMs = 10
        });
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        var diagnose = await admin.PostAsJsonAsync($"/api/enterprise/devices/{deviceId}/ai-diagnose", new { });
        Assert.Equal(HttpStatusCode.OK, diagnose.StatusCode);
    }

    [Fact]
    public async Task EnterpriseIdentity_ReadEndpoints()
    {
        using var admin = AdminClient();

        var overview = await admin.GetAsync("/api/enterprise/identity/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var providers = await admin.GetAsync("/api/enterprise/identity/providers");
        Assert.Equal(HttpStatusCode.OK, providers.StatusCode);
    }

    [Fact]
    public async Task EnterpriseVisitorVehicle_ReadEndpoints()
    {
        using var admin = AdminClient();

        var overview = await admin.GetAsync("/api/enterprise/visitor-vehicle/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);

        var receptionOverview = await admin.GetAsync("/api/enterprise/visitor-vehicle/reception/overview");
        Assert.Equal(HttpStatusCode.OK, receptionOverview.StatusCode);

        var receptionBoard = await admin.GetAsync("/api/enterprise/visitor-vehicle/reception/board");
        Assert.Equal(HttpStatusCode.OK, receptionBoard.StatusCode);

        var visits = await admin.GetAsync("/api/enterprise/visitor-vehicle/visits");
        Assert.Equal(HttpStatusCode.OK, visits.StatusCode);

        var overstays = await admin.GetAsync("/api/enterprise/visitor-vehicle/visits/overstays");
        Assert.Equal(HttpStatusCode.OK, overstays.StatusCode);

        var watchlist = await admin.GetAsync("/api/enterprise/visitor-vehicle/watchlist-entries");
        Assert.Equal(HttpStatusCode.OK, watchlist.StatusCode);
    }

    [Fact]
    public async Task AccessPermission_SetAndToggleGate()
    {
        using var admin = AdminClient();

        int gateId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var gate = await db.Gates.FirstOrDefaultAsync();
            if (gate == null)
            {
                gate = new Gate { GateName = "Perm Gate " + Guid.NewGuid().ToString("N")[..6] };
                db.Gates.Add(gate);
                await db.SaveChangesAsync();
            }
            gateId = gate.GateId;
        }

        var missing = await admin.PostAsJsonAsync("/api/access-permissions/set-permission", new
        {
            employeeId = (int?)null,
            visitorDetailId = (int?)null,
            gateId,
            isAllowed = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    [Fact]
    public async Task AccessPermission_EmployeeToggle_RequiresEmployee()
    {
        using var admin = AdminClient();

        var response = await admin.PostAsJsonAsync("/api/access-permissions/employee/toggle-gate", new
        {
            employeeId = 999999,
            gateId = 1,
            enabled = true
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WorkSchedules_ListEndpoints()
    {
        using var admin = AdminClient();

        var all = await admin.GetAsync("/api/work-schedules");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);

        var byEmployee = await admin.GetAsync("/api/work-schedules/employee/1");
        Assert.Equal(HttpStatusCode.OK, byEmployee.StatusCode);

        var missing = await admin.GetAsync("/api/work-schedules/99999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task VehicleDelegations_ListEndpoints()
    {
        using var admin = AdminClient();

        var all = await admin.GetAsync("/api/vehicle-delegations");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
    }
}
