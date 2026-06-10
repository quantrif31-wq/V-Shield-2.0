using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

public class SecurityBoundaryTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SecurityWebApplicationFactory _factory;

    public SecurityBoundaryTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health/degraded")]
    [InlineData("/api/pre-registrations/validate/unknown-token")]
    [InlineData("/api/pre-registrations/visitor-pass/unknown-token")]
    public async Task PublicGetEndpoints_DoNotRequireAuthentication(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/employees")]
    [InlineData("/api/access-permissions")]
    [InlineData("/api/camera-runtime")]
    [InlineData("/api/vehicles")]
    [InlineData("/api/QrAccess/verify-camera-auth")]
    [InlineData("/api/dynamic-qr/generate")]
    [InlineData("/api/video/1/content")]
    public async Task PrivilegedEndpoints_RejectAnonymousRequests(string path)
    {
        using var request = path.Contains("verify-camera-auth", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains("dynamic-qr/generate", StringComparison.OrdinalIgnoreCase)
            ? new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new { })
            }
            : new HttpRequestMessage(HttpMethod.Get, path);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Uploads_AreNotPublic()
    {
        var response = await _client.GetAsync("/uploads/faces/anything.jpg");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CorrelationId_IsReturnedAndAccepted()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-123");

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values));
        Assert.Equal("test-correlation-123", values.Single());
    }

    [Fact]
    public async Task ReadinessHealth_ReturnsDependencyChecks()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ready\"", body);
        Assert.Contains("\"database\"", body);
    }

    [Fact]
    public async Task DegradedHealth_ReturnsRuntimeChecks()
    {
        var response = await _client.GetAsync("/health/degraded");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"runtime\"", body);
        Assert.Contains("\"database\"", body);
    }

    [Fact]
    public async Task SafeExceptionEnvelope_DoesNotLeakExceptionDetails()
    {
        var response = await _client.GetAsync("/__test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("correlationId", body);
        Assert.DoesNotContain("Sensitive test exception detail", body);
    }

    [Fact]
    public async Task StaffLogin_IssuesRefreshToken_AndLogoutRevokesAccessToken()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = "staff.test",
            password = "Staff@12345"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await ReadJsonAsync(loginResponse);
        var token = GetString(login, "token");
        var refreshToken = GetString(login, "refreshToken");
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        using var authenticated = CreateClientWithBearer(token);
        var meResponse = await authenticated.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var logoutResponse = await authenticated.PostAsJsonAsync("/api/Auth/logout", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var revokedMeResponse = await authenticated.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedMeResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RotatesRefreshToken()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = "staff.test",
            password = "Staff@12345"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await ReadJsonAsync(loginResponse);
        var refreshToken = GetString(login, "refreshToken");

        var refreshResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refresh = await ReadJsonAsync(refreshResponse);
        Assert.NotEqual(refreshToken, GetString(refresh, "refreshToken"));
        Assert.False(string.IsNullOrWhiteSpace(GetString(refresh, "token")));
    }

    [Fact]
    public async Task StaffToken_CannotAccessAdminOnlyUsersController()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StaffToken_CannotOperateFaceCameraController()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/FaceCamera/camera/reset", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DemoWeatherForecastEndpoint_IsNotExposed()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));
        var response = await authenticated.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void ControllerActions_HaveExplicitTrustBoundary()
    {
        var controllerTypes = typeof(API.Program).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) &&
                           !type.IsAbstract &&
                           type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .OrderBy(type => type.FullName)
            .ToList();

        var missing = new List<string>();

        foreach (var controllerType in controllerTypes)
        {
            var controllerHasBoundary = HasTrustBoundary(controllerType);
            var actions = controllerType.GetMethods()
                .Where(method => method.DeclaringType == controllerType &&
                                 method.IsPublic &&
                                 !method.IsSpecialName &&
                                 method.GetCustomAttributes(typeof(HttpMethodAttribute), true).Any())
                .OrderBy(method => method.Name);

            foreach (var action in actions)
            {
                if (!controllerHasBoundary && !HasTrustBoundary(action))
                {
                    missing.Add($"{controllerType.Name}.{action.Name}");
                }
            }
        }

        Assert.True(missing.Count == 0, "Controller actions missing explicit [Authorize] or [AllowAnonymous]: " + string.Join(", ", missing));
    }

    [Fact]
    public async Task StaffToken_CannotAdministerEnterpriseFoundation()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/foundation/companies", new
        {
            name = "V-Shield Corp",
            code = "VSHIELD"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanCreateEnterpriseFoundationHierarchy()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));

        var companyResponse = await authenticated.PostAsJsonAsync("/api/enterprise/foundation/companies", new
        {
            name = "V-Shield Corp",
            code = "VSHIELD"
        });
        Assert.Equal(HttpStatusCode.Created, companyResponse.StatusCode);
        var company = await ReadJsonAsync(companyResponse);
        var companyId = company.RootElement.GetProperty("companyId").GetInt32();

        var siteResponse = await authenticated.PostAsJsonAsync("/api/enterprise/foundation/sites", new
        {
            companyId,
            name = "Headquarters",
            code = "HQ",
            address = "Main security site",
            timeZoneId = "Asia/Ho_Chi_Minh"
        });
        Assert.Equal(HttpStatusCode.OK, siteResponse.StatusCode);
        var site = await ReadJsonAsync(siteResponse);
        var siteId = site.RootElement.GetProperty("siteId").GetInt32();

        var buildingResponse = await authenticated.PostAsJsonAsync("/api/enterprise/foundation/buildings", new
        {
            siteId,
            name = "Main Building",
            code = "MAIN"
        });
        Assert.Equal(HttpStatusCode.OK, buildingResponse.StatusCode);
        var building = await ReadJsonAsync(buildingResponse);
        var buildingId = building.RootElement.GetProperty("buildingId").GetInt32();

        var floorResponse = await authenticated.PostAsJsonAsync("/api/enterprise/foundation/floors", new
        {
            buildingId,
            name = "Ground Floor",
            code = "G",
            sortOrder = 0
        });
        Assert.Equal(HttpStatusCode.OK, floorResponse.StatusCode);
        var floor = await ReadJsonAsync(floorResponse);
        var floorId = floor.RootElement.GetProperty("facilityFloorId").GetInt32();

        var zoneResponse = await authenticated.PostAsJsonAsync("/api/enterprise/foundation/zones", new
        {
            siteId,
            buildingId,
            facilityFloorId = floorId,
            name = "Lobby",
            code = "LOBBY",
            securityLevel = "Normal",
            isRestricted = false
        });
        Assert.Equal(HttpStatusCode.OK, zoneResponse.StatusCode);
        var zone = await ReadJsonAsync(zoneResponse);
        var zoneId = zone.RootElement.GetProperty("securityZoneId").GetInt32();

        var accessPointResponse = await authenticated.PostAsJsonAsync("/api/enterprise/foundation/access-points", new
        {
            siteId,
            securityZoneId = zoneId,
            name = "Lobby Door",
            type = "Door",
            directionMode = "Bidirectional"
        });
        Assert.Equal(HttpStatusCode.OK, accessPointResponse.StatusCode);

        var overviewResponse = await authenticated.GetAsync("/api/enterprise/foundation/overview");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
        var overview = await overviewResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"companies\":1", overview, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"sites\":1", overview, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminToken_CanRecordEmployeeLifecycleChange()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));
        var response = await authenticated.PatchAsJsonAsync("/api/enterprise/foundation/employees/1/lifecycle", new
        {
            newState = "Suspended",
            reason = "Security review",
            primarySiteId = (int?)null,
            managerEmployeeId = (int?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Suspended", body);
    }

    [Fact]
    public async Task StaffToken_CannotAdministerAccessPolicy()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/access-policy/access-levels", new
        {
            name = "Restricted",
            code = "RESTRICTED",
            description = "Restricted access",
            requiresApproval = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanEvaluateAccessPolicyWithExplainableDecision()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));

        var levelResponse = await authenticated.PostAsJsonAsync("/api/enterprise/access-policy/access-levels", new
        {
            name = "Office Access",
            code = "OFFICE",
            description = "Office hours access",
            requiresApproval = false
        });
        Assert.Equal(HttpStatusCode.OK, levelResponse.StatusCode);
        var level = await ReadJsonAsync(levelResponse);
        var accessLevelId = level.RootElement.GetProperty("accessLevelId").GetInt32();

        var scheduleResponse = await authenticated.PostAsJsonAsync("/api/enterprise/access-policy/schedules", new
        {
            name = "All Day",
            startTime = new TimeSpan(0, 0, 0),
            endTime = new TimeSpan(23, 59, 59),
            daysOfWeek = "Mon,Tue,Wed,Thu,Fri,Sat,Sun",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, scheduleResponse.StatusCode);
        var schedule = await ReadJsonAsync(scheduleResponse);
        var accessScheduleId = schedule.RootElement.GetProperty("accessScheduleId").GetInt32();

        var ruleResponse = await authenticated.PostAsJsonAsync("/api/enterprise/access-policy/rules", new
        {
            accessLevelId,
            accessGroupId = (int?)null,
            siteId = (int?)null,
            securityZoneId = (int?)null,
            accessPointId = (int?)null,
            accessScheduleId,
            subjectType = "Employee",
            subjectId = 1,
            credentialType = "Any",
            allowAccess = true,
            validFromUtc = (DateTime?)null,
            validToUtc = (DateTime?)null,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, ruleResponse.StatusCode);

        var allowResponse = await authenticated.PostAsJsonAsync("/api/enterprise/access-policy/evaluate", new
        {
            subjectType = "Employee",
            subjectId = 1,
            siteId = (int?)null,
            securityZoneId = (int?)null,
            accessPointId = (int?)null,
            credentialType = "QR",
            allowHolidayAccess = false,
            evaluatedAtUtc = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc)
        });
        Assert.Equal(HttpStatusCode.OK, allowResponse.StatusCode);
        var allowBody = await allowResponse.Content.ReadAsStringAsync();
        Assert.Contains("Allow", allowBody);
        Assert.Contains("allowed access", allowBody);

        var emergencyResponse = await authenticated.PostAsJsonAsync("/api/enterprise/access-policy/emergency-states", new
        {
            state = "FullLockdown",
            siteId = (int?)null,
            securityZoneId = (int?)null,
            accessPointId = (int?)null,
            reason = "Drill"
        });
        Assert.Equal(HttpStatusCode.OK, emergencyResponse.StatusCode);

        var denyResponse = await authenticated.PostAsJsonAsync("/api/enterprise/access-policy/evaluate", new
        {
            subjectType = "Employee",
            subjectId = 1,
            siteId = (int?)null,
            securityZoneId = (int?)null,
            accessPointId = (int?)null,
            credentialType = "QR",
            allowHolidayAccess = false,
            evaluatedAtUtc = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc)
        });
        Assert.Equal(HttpStatusCode.OK, denyResponse.StatusCode);
        var denyBody = await denyResponse.Content.ReadAsStringAsync();
        Assert.Contains("Deny", denyBody);
        Assert.Contains("Emergency state active", denyBody);
    }

    [Fact]
    public async Task StaffToken_CannotAdministerVisitorVehicleOperations()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/visitor-vehicle/watchlist", new
        {
            entityType = "Person",
            displayName = "Blocked Visitor",
            identifier = "0900000000",
            severity = "High",
            reason = "Test"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanRunVisitorVehicleWorkflow()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));

        var watchlistResponse = await authenticated.PostAsJsonAsync("/api/enterprise/visitor-vehicle/watchlist", new
        {
            entityType = "Person",
            displayName = "Visitor Watch",
            identifier = "watch@example.com",
            severity = "High",
            reason = "Watchlist test"
        });
        Assert.Equal(HttpStatusCode.OK, watchlistResponse.StatusCode);

        var visitResponse = await authenticated.PostAsJsonAsync("/api/enterprise/visitor-vehicle/visits", new
        {
            siteId = (int?)null,
            hostEmployeeId = 1,
            visitorName = "Visitor Watch",
            visitorType = "Visitor",
            visitorPhone = "0900000000",
            visitorEmail = "watch@example.com",
            expectedInUtc = DateTime.UtcNow.AddMinutes(-5),
            expectedOutUtc = DateTime.UtcNow.AddHours(2),
            escortRequired = true,
            ndaRequired = true,
            safetyBriefingRequired = true
        });
        Assert.Equal(HttpStatusCode.OK, visitResponse.StatusCode);
        var visit = await ReadJsonAsync(visitResponse);
        var visitId = visit.RootElement.GetProperty("visitId").GetInt32();

        var blockedCheckIn = await authenticated.PostAsJsonAsync($"/api/enterprise/visitor-vehicle/visits/{visitId}/check-in", new
        {
            idDocumentType = "ID",
            idDocumentReference = "ID-123",
            verificationStatus = "Verified"
        });
        Assert.Equal(HttpStatusCode.BadRequest, blockedCheckIn.StatusCode);

        var formResponse = await authenticated.PostAsJsonAsync("/api/enterprise/visitor-vehicle/forms", new
        {
            name = "Default NDA",
            formType = "NDA",
            version = 1,
            body = "NDA text"
        });
        Assert.Equal(HttpStatusCode.OK, formResponse.StatusCode);
        var form = await ReadJsonAsync(formResponse);
        var templateId = form.RootElement.GetProperty("visitorFormTemplateId").GetInt32();

        var acceptResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/visitor-vehicle/visits/{visitId}/form-acceptances", new
        {
            templateId,
            acceptedByName = "Visitor Watch"
        });
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var checkInResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/visitor-vehicle/visits/{visitId}/check-in", new
        {
            idDocumentType = "ID",
            idDocumentReference = "ID-123",
            verificationStatus = "Verified"
        });
        Assert.Equal(HttpStatusCode.OK, checkInResponse.StatusCode);
        var checkInBody = await checkInResponse.Content.ReadAsStringAsync();
        Assert.Contains("CheckedIn", checkInBody);

        var parkingAreaResponse = await authenticated.PostAsJsonAsync("/api/enterprise/visitor-vehicle/parking-areas", new
        {
            siteId = (int?)null,
            name = "Visitor Parking",
            capacity = 10
        });
        Assert.Equal(HttpStatusCode.OK, parkingAreaResponse.StatusCode);
        var parkingArea = await ReadJsonAsync(parkingAreaResponse);
        var parkingAreaId = parkingArea.RootElement.GetProperty("parkingAreaId").GetInt32();

        var permitResponse = await authenticated.PostAsJsonAsync("/api/enterprise/visitor-vehicle/parking-permits", new
        {
            parkingAreaId,
            vehicleId = (int?)null,
            visitId,
            permitType = "Visitor",
            validFromUtc = DateTime.UtcNow.AddMinutes(-5),
            validToUtc = DateTime.UtcNow.AddHours(2)
        });
        Assert.Equal(HttpStatusCode.OK, permitResponse.StatusCode);

        var barrierResponse = await authenticated.PostAsJsonAsync("/api/enterprise/visitor-vehicle/barriers", new
        {
            laneId = (int?)null,
            name = "Visitor Barrier",
            state = "Closed"
        });
        Assert.Equal(HttpStatusCode.OK, barrierResponse.StatusCode);
        var barrier = await ReadJsonAsync(barrierResponse);
        var barrierId = barrier.RootElement.GetProperty("barrierId").GetInt32();

        var commandResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/visitor-vehicle/barriers/{barrierId}/commands", new
        {
            command = "Open",
            reason = "Visitor approved"
        });
        Assert.Equal(HttpStatusCode.OK, commandResponse.StatusCode);

        var checkOutResponse = await authenticated.PostAsync($"/api/enterprise/visitor-vehicle/visits/{visitId}/check-out", null);
        Assert.Equal(HttpStatusCode.OK, checkOutResponse.StatusCode);
        var checkOutBody = await checkOutResponse.Content.ReadAsStringAsync();
        Assert.Contains("CheckedOut", checkOutBody);
    }

    [Fact]
    public async Task StaffToken_CannotAdministerEnterpriseDevices()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/devices", new
        {
            siteId = (int?)null,
            accessPointId = (int?)null,
            deviceType = "Controller",
            name = "Panel 1",
            vendor = "Generic",
            model = "Sim",
            serialNumber = "SIM-1",
            firmwareVersion = "1.0",
            configurationVersion = "1"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanRegisterDeviceHealthAndOfflinePolicyPackage()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));

        var deviceResponse = await authenticated.PostAsJsonAsync("/api/enterprise/devices", new
        {
            siteId = (int?)null,
            accessPointId = (int?)null,
            deviceType = "Controller",
            name = "Panel 1",
            vendor = "Generic",
            model = "Sim",
            serialNumber = "SIM-1",
            firmwareVersion = "1.0",
            configurationVersion = "1"
        });
        Assert.Equal(HttpStatusCode.OK, deviceResponse.StatusCode);
        var device = await ReadJsonAsync(deviceResponse);
        var securityDeviceId = device.RootElement.GetProperty("securityDeviceId").GetInt32();

        var controllerResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/devices/{securityDeviceId}/controllers", new
        {
            protocol = "OSDP",
            supportsOfflineDecision = true,
            maxCredentials = 10000
        });
        Assert.Equal(HttpStatusCode.OK, controllerResponse.StatusCode);

        var healthResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/devices/{securityDeviceId}/health", new
        {
            status = "Ok",
            message = "Simulated heartbeat",
            latencyMs = 12
        });
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var packageResponse = await authenticated.PostAsJsonAsync("/api/enterprise/devices/offline-policy-packages", new
        {
            securityDeviceId,
            packageVersion = "v1",
            payloadJson = "{\"credentials\":[]}",
            payloadHash = "hash",
            status = "Published"
        });
        Assert.Equal(HttpStatusCode.OK, packageResponse.StatusCode);

        var connectorsResponse = await authenticated.GetAsync("/api/enterprise/devices/connectors/status");
        Assert.Equal(HttpStatusCode.OK, connectorsResponse.StatusCode);
        var connectorsBody = await connectorsResponse.Content.ReadAsStringAsync();
        Assert.Contains("API wrapper only", connectorsBody);
        Assert.Contains("OSDP-compatible", connectorsBody);
    }

    [Fact]
    public async Task StaffToken_CannotAdministerSituationalAwarenessMaps()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/situational-awareness/maps", new
        {
            siteId = (int?)null,
            name = "HQ Map",
            assetReference = "/maps/hq.png",
            coordinateSystem = "Normalized"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanCreateCorrelatedEventAndAiReviewWorkflow()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));
        var correlationId = Guid.NewGuid().ToString("N");

        var eventResponse = await authenticated.PostAsJsonAsync("/api/enterprise/situational-awareness/events", new
        {
            sourceType = "Access",
            sourceId = "scan-1",
            eventType = "AccessDenied",
            severity = "High",
            siteId = (int?)null,
            securityZoneId = (int?)null,
            accessPointId = (int?)null,
            subjectType = "Employee",
            subjectId = 1,
            vehicleId = (int?)null,
            plateText = (string?)null,
            confidence = 0.95m,
            correlationId,
            summary = "Denied access with high confidence",
            occurredAtUtc = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, eventResponse.StatusCode);
        var securityEvent = await ReadJsonAsync(eventResponse);
        var securityEventId = securityEvent.RootElement.GetProperty("securityEventId").GetInt64();

        var bookmarkResponse = await authenticated.PostAsJsonAsync("/api/enterprise/situational-awareness/video-bookmarks", new
        {
            securityEventId,
            cameraId = (int?)null,
            artifactReference = "/evidence/video/clip-1",
            startUtc = DateTime.UtcNow.AddSeconds(-10),
            endUtc = DateTime.UtcNow.AddSeconds(10),
            note = "Linked clip"
        });
        Assert.Equal(HttpStatusCode.OK, bookmarkResponse.StatusCode);

        var mapResponse = await authenticated.PostAsJsonAsync("/api/enterprise/situational-awareness/maps", new
        {
            siteId = (int?)null,
            name = "HQ Map",
            assetReference = "/maps/hq.png",
            coordinateSystem = "Normalized"
        });
        Assert.Equal(HttpStatusCode.OK, mapResponse.StatusCode);
        var map = await ReadJsonAsync(mapResponse);
        var siteMapId = map.RootElement.GetProperty("siteMapId").GetInt32();

        var placementResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/situational-awareness/maps/{siteMapId}/placements", new
        {
            securityDeviceId = (int?)null,
            cameraId = (int?)null,
            x = 0.5m,
            y = 0.5m,
            iconType = "Alarm"
        });
        Assert.Equal(HttpStatusCode.OK, placementResponse.StatusCode);

        var aiResponse = await authenticated.PostAsJsonAsync("/api/enterprise/situational-awareness/ai-adjudications", new
        {
            securityEventId,
            aiSource = "Face",
            modelVersion = "runtime-wrapper",
            confidence = 0.92m
        });
        Assert.Equal(HttpStatusCode.OK, aiResponse.StatusCode);
        var ai = await ReadJsonAsync(aiResponse);
        var aiItemId = ai.RootElement.GetProperty("aiAdjudicationItemId").GetInt32();

        var reviewResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/situational-awareness/ai-adjudications/{aiItemId}/review", new
        {
            outcome = "FalsePositive",
            reviewNote = "Operator reviewed"
        });
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        var overviewResponse = await authenticated.GetAsync("/api/enterprise/situational-awareness/overview");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
        var overview = await overviewResponse.Content.ReadAsStringAsync();
        Assert.Contains("correlations", overview, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaffToken_CannotAdministerSoc()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/soc/alarm-rules", new
        {
            name = "Critical denied access",
            eventType = "AccessDenied",
            severity = "Critical",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanRunSocIncidentCommandWorkflow()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));

        var ruleResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/alarm-rules", new
        {
            name = "Critical denied access",
            eventType = "AccessDenied",
            severity = "Critical",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, ruleResponse.StatusCode);

        var alarmResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/alarms", new
        {
            securityEventId = (long?)null,
            alarmType = "AccessDenied",
            severity = "Critical",
            summary = "Denied entry at main lobby",
            siteId = (int?)null
        });
        Assert.Equal(HttpStatusCode.OK, alarmResponse.StatusCode);
        var alarm = await ReadJsonAsync(alarmResponse);
        var alarmId = alarm.RootElement.GetProperty("alarmId").GetInt64();

        var acknowledgeResponse = await authenticated.PatchAsync($"/api/enterprise/soc/alarms/{alarmId}/acknowledge", null);
        Assert.Equal(HttpStatusCode.OK, acknowledgeResponse.StatusCode);

        var assignResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/soc/alarms/{alarmId}/assign", new
        {
            assignedToUserId = 1002,
            note = "SOC triage started"
        });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var sopTemplateResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/sop-templates", new
        {
            name = "Critical alarm SOP",
            alarmType = "AccessDenied",
            version = 1,
            checklistJson = "[\"verify camera\",\"dispatch guard\"]"
        });
        Assert.Equal(HttpStatusCode.OK, sopTemplateResponse.StatusCode);
        var sopTemplate = await ReadJsonAsync(sopTemplateResponse);
        var sopTemplateId = sopTemplate.RootElement.GetProperty("sopTemplateId").GetInt32();

        var sopExecutionResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/sop-executions", new
        {
            alarmId,
            incidentId = (long?)null,
            sopTemplateId
        });
        Assert.Equal(HttpStatusCode.OK, sopExecutionResponse.StatusCode);
        var sopExecution = await ReadJsonAsync(sopExecutionResponse);
        var sopExecutionId = sopExecution.RootElement.GetProperty("sopExecutionId").GetInt64();

        var incidentResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/incidents", new
        {
            title = "Unauthorized lobby attempt",
            severity = "Critical",
            primaryAlarmId = alarmId,
            ownerUserId = 1002
        });
        Assert.Equal(HttpStatusCode.OK, incidentResponse.StatusCode);
        var incident = await ReadJsonAsync(incidentResponse);
        var incidentId = incident.RootElement.GetProperty("incidentId").GetInt64();

        var timelineResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/soc/incidents/{incidentId}/timeline", new
        {
            itemType = "Triage",
            text = "Camera checked and guard dispatched"
        });
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);

        var dispatchResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/dispatch-tasks", new
        {
            alarmId,
            incidentId,
            siteId = (int?)null,
            locationText = "Main lobby",
            priority = "Critical",
            assignedGuardUserId = 1002,
            instructions = "Inspect entrance and confirm identity"
        });
        Assert.Equal(HttpStatusCode.OK, dispatchResponse.StatusCode);
        var dispatch = await ReadJsonAsync(dispatchResponse);
        var dispatchTaskId = dispatch.RootElement.GetProperty("dispatchTaskId").GetInt64();

        var musterResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/emergency-muster-snapshots", new
        {
            siteId = (int?)null,
            musterPointId = (int?)null,
            knownOnsite = 20,
            accountedFor = 18,
            visitorsOnsite = 3
        });
        Assert.Equal(HttpStatusCode.OK, musterResponse.StatusCode);
        var musterBody = await musterResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"unaccountedFor\":5", musterBody, StringComparison.OrdinalIgnoreCase);

        var handoverResponse = await authenticated.PostAsJsonAsync("/api/enterprise/soc/shift-handovers", new
        {
            siteId = (int?)null,
            fromUserId = 1002,
            toUserId = 1002,
            summary = "Critical lobby alarm handled; watchlist follow-up pending."
        });
        Assert.Equal(HttpStatusCode.OK, handoverResponse.StatusCode);

        var completeDispatchResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/soc/dispatch-tasks/{dispatchTaskId}/complete", new
        {
            note = "Guard confirmed false tailgate attempt."
        });
        Assert.Equal(HttpStatusCode.OK, completeDispatchResponse.StatusCode);

        var completeSopResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/soc/sop-executions/{sopExecutionId}/complete", new
        {
            completedStepsJson = "[\"verify camera\",\"dispatch guard\"]"
        });
        Assert.Equal(HttpStatusCode.OK, completeSopResponse.StatusCode);

        var closeAlarmResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/soc/alarms/{alarmId}/close", new
        {
            note = "Closed after operator review."
        });
        Assert.Equal(HttpStatusCode.OK, closeAlarmResponse.StatusCode);

        var closeIncidentResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/soc/incidents/{incidentId}/close", new
        {
            note = "Resolved with dispatch confirmation."
        });
        Assert.Equal(HttpStatusCode.OK, closeIncidentResponse.StatusCode);

        var overviewResponse = await authenticated.GetAsync("/api/enterprise/soc/overview");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
        var overview = await overviewResponse.Content.ReadAsStringAsync();
        Assert.Contains("shiftHandovers", overview, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("musterSnapshots", overview, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaffToken_CannotAdministerEvidenceGovernance()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/evidence/retention-policies", new
        {
            name = "Biometric evidence",
            evidenceType = "Video",
            retentionCategory = "Biometric",
            retentionDays = 365,
            purgeMode = "ReviewRequired",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanGovernEvidencePrivacyExportAndCompliance()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));

        var retentionResponse = await authenticated.PostAsJsonAsync("/api/enterprise/evidence/retention-policies", new
        {
            name = "Video biometric evidence",
            evidenceType = "Video",
            retentionCategory = "Biometric",
            retentionDays = 730,
            purgeMode = "ReviewRequired",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, retentionResponse.StatusCode);

        var itemResponse = await authenticated.PostAsJsonAsync("/api/enterprise/evidence/items", new
        {
            evidenceType = "Video",
            sourceType = "Camera",
            sourceReference = "camera-1",
            securityEventId = (long?)null,
            alarmId = (long?)null,
            incidentId = (long?)null,
            storageReference = "/evidence/video/clip-1.mp4",
            hashSha256 = "abc123",
            privacyLabel = "Biometric",
            retentionCategory = "Biometric",
            siteId = (int?)null,
            isImmutable = true
        });
        Assert.Equal(HttpStatusCode.OK, itemResponse.StatusCode);
        var item = await ReadJsonAsync(itemResponse);
        var evidenceItemId = item.RootElement.GetProperty("evidenceItemId").GetInt64();

        var readResponse = await authenticated.GetAsync($"/api/enterprise/evidence/items/{evidenceItemId}?purpose=IncidentReview");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        var collectionResponse = await authenticated.PostAsJsonAsync("/api/enterprise/evidence/collections", new
        {
            name = "Lobby incident evidence",
            purpose = "Investigation",
            incidentId = (long?)null
        });
        Assert.Equal(HttpStatusCode.OK, collectionResponse.StatusCode);
        var collection = await ReadJsonAsync(collectionResponse);
        var evidenceCollectionId = collection.RootElement.GetProperty("evidenceCollectionId").GetInt64();

        var collectionItemResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/evidence/collections/{evidenceCollectionId}/items", new
        {
            evidenceItemId
        });
        Assert.Equal(HttpStatusCode.OK, collectionItemResponse.StatusCode);
        var collectionBody = await collectionItemResponse.Content.ReadAsStringAsync();
        Assert.Contains("bundleHash", collectionBody, StringComparison.OrdinalIgnoreCase);

        var custodyResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/evidence/items/{evidenceItemId}/custody", new
        {
            action = "Transferred",
            fromCustodian = "SOC",
            toCustodian = "Compliance",
            hashBefore = "abc123",
            hashAfter = "abc123",
            note = "Case handoff"
        });
        Assert.Equal(HttpStatusCode.OK, custodyResponse.StatusCode);

        var holdResponse = await authenticated.PostAsJsonAsync("/api/enterprise/evidence/legal-holds", new
        {
            evidenceItemId,
            evidenceCollectionId = (long?)null,
            reason = "Investigation hold"
        });
        Assert.Equal(HttpStatusCode.OK, holdResponse.StatusCode);
        var hold = await ReadJsonAsync(holdResponse);
        var legalHoldId = hold.RootElement.GetProperty("legalHoldId").GetInt64();

        var exportResponse = await authenticated.PostAsJsonAsync("/api/enterprise/evidence/export-requests", new
        {
            evidenceItemId = (long?)null,
            evidenceCollectionId,
            purpose = "Regulatory review",
            recipient = "auditor@example.com"
        });
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var export = await ReadJsonAsync(exportResponse);
        var exportRequestId = export.RootElement.GetProperty("evidenceExportRequestId").GetInt64();

        var approveExportResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/evidence/export-requests/{exportRequestId}/approve", new
        {
            watermark = "V-Shield approved export",
            signatureReference = "sig-1"
        });
        Assert.Equal(HttpStatusCode.OK, approveExportResponse.StatusCode);
        var approveExportBody = await approveExportResponse.Content.ReadAsStringAsync();
        Assert.Contains("exportHash", approveExportBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Approved", approveExportBody);

        var redactionResponse = await authenticated.PostAsJsonAsync("/api/enterprise/evidence/redaction-requests", new
        {
            evidenceItemId,
            reason = "Mask visitor face for disclosure",
            privacyLabel = "Biometric"
        });
        Assert.Equal(HttpStatusCode.OK, redactionResponse.StatusCode);
        var redaction = await ReadJsonAsync(redactionResponse);
        var redactionRequestId = redaction.RootElement.GetProperty("redactionRequestId").GetInt64();

        var approveRedactionResponse = await authenticated.PatchAsync($"/api/enterprise/evidence/redaction-requests/{redactionRequestId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveRedactionResponse.StatusCode);

        var performRedactionResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/evidence/redaction-requests/{redactionRequestId}/perform", new
        {
            redactedStorageReference = "/evidence/video/clip-1-redacted.mp4"
        });
        Assert.Equal(HttpStatusCode.OK, performRedactionResponse.StatusCode);

        var verifyRedactionResponse = await authenticated.PatchAsync($"/api/enterprise/evidence/redaction-requests/{redactionRequestId}/verify", null);
        Assert.Equal(HttpStatusCode.OK, verifyRedactionResponse.StatusCode);
        var verifyRedactionBody = await verifyRedactionResponse.Content.ReadAsStringAsync();
        Assert.Contains("Verified", verifyRedactionBody);

        var complianceResponse = await authenticated.PostAsJsonAsync("/api/enterprise/evidence/compliance-reports", new
        {
            reportType = "EvidenceAccess",
            periodStartUtc = DateTime.UtcNow.AddDays(-1),
            periodEndUtc = DateTime.UtcNow,
            outputReference = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, complianceResponse.StatusCode);

        var releaseHoldResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/evidence/legal-holds/{legalHoldId}/release", new
        {
            note = "Investigation complete"
        });
        Assert.Equal(HttpStatusCode.OK, releaseHoldResponse.StatusCode);

        var overviewResponse = await authenticated.GetAsync("/api/enterprise/evidence/overview");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
        var overview = await overviewResponse.Content.ReadAsStringAsync();
        Assert.Contains("complianceReports", overview, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("accessLogs", overview, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaffToken_CannotAdministerOperationsResilience()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/operations/webhook-subscriptions", new
        {
            name = "SIEM webhook",
            targetUrl = "https://siem.example.test/events",
            secretReference = "secret://test",
            eventTypes = "Alarm,EvidenceExport",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanRunOperationsResilienceWorkflow()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));
        var correlationId = Guid.NewGuid().ToString("N");

        var healthResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/dependency-health", new
        {
            dependencyName = "face-runtime-wrapper",
            dependencyType = "Runtime",
            status = "Ok",
            latencyMs = 24,
            message = "Wrapper heartbeat accepted"
        });
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var outboxResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/outbox-events", new
        {
            eventType = "AlarmRaised",
            aggregateType = "Alarm",
            aggregateId = "alarm-1",
            payloadJson = "{\"severity\":\"Critical\"}",
            correlationId
        });
        Assert.Equal(HttpStatusCode.OK, outboxResponse.StatusCode);
        var outbox = await ReadJsonAsync(outboxResponse);
        var outboxEventId = outbox.RootElement.GetProperty("outboxEventId").GetInt64();

        var subscriptionResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/webhook-subscriptions", new
        {
            name = "SIEM webhook",
            targetUrl = "https://siem.example.test/events",
            secretReference = "secret://vshield/siem",
            eventTypes = "AlarmRaised,SiemExport",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, subscriptionResponse.StatusCode);
        var subscription = await ReadJsonAsync(subscriptionResponse);
        var webhookSubscriptionId = subscription.RootElement.GetProperty("webhookSubscriptionId").GetInt32();

        var deliveryResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/webhook-deliveries", new
        {
            webhookSubscriptionId,
            outboxEventId
        });
        Assert.Equal(HttpStatusCode.OK, deliveryResponse.StatusCode);
        var delivery = await ReadJsonAsync(deliveryResponse);
        var webhookDeliveryId = delivery.RootElement.GetProperty("webhookDeliveryId").GetInt64();
        Assert.False(string.IsNullOrWhiteSpace(delivery.RootElement.GetProperty("signature").GetString()));

        var deliveryResultResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/operations/webhook-deliveries/{webhookDeliveryId}/result", new
        {
            status = "Delivered",
            attemptCount = 1,
            responseStatusCode = 200,
            responseBody = "ok"
        });
        Assert.Equal(HttpStatusCode.OK, deliveryResultResponse.StatusCode);

        var dispatchResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/operations/outbox-events/{outboxEventId}/dispatch", new
        {
            status = "Dispatched",
            retryCount = 0
        });
        Assert.Equal(HttpStatusCode.OK, dispatchResponse.StatusCode);

        var siemResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/siem-exports", new
        {
            source = "SecurityEvent",
            correlationId,
            payloadJson = "{\"event\":\"EvidenceExportApproved\"}"
        });
        Assert.Equal(HttpStatusCode.OK, siemResponse.StatusCode);

        var backupResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/backup-runs", new
        {
            profile = "MediumCompany",
            targetRpoMinutes = 15,
            targetRtoMinutes = 60,
            notes = "Nightly backup"
        });
        Assert.Equal(HttpStatusCode.OK, backupResponse.StatusCode);
        var backup = await ReadJsonAsync(backupResponse);
        var backupRunId = backup.RootElement.GetProperty("backupRunId").GetInt64();

        var completeBackupResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/operations/backup-runs/{backupRunId}/complete", new
        {
            status = "Completed",
            backupReference = "backup://medium-company/20260610",
            sizeBytes = 12345678L,
            verified = true,
            notes = "Checksum verified"
        });
        Assert.Equal(HttpStatusCode.OK, completeBackupResponse.StatusCode);

        var restoreResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/restore-drills", new
        {
            backupRunId,
            profile = "MediumCompany"
        });
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        var restore = await ReadJsonAsync(restoreResponse);
        var restoreDrillId = restore.RootElement.GetProperty("restoreDrillId").GetInt64();

        var completeRestoreResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/operations/restore-drills/{restoreDrillId}/complete", new
        {
            measuredRpoMinutes = 10,
            measuredRtoMinutes = 45,
            passed = true,
            findings = "Restore completed inside target"
        });
        Assert.Equal(HttpStatusCode.OK, completeRestoreResponse.StatusCode);

        var secretCheckResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/security-checks", new
        {
            checkType = "SecretsRotation",
            name = "JWT and webhook secret rotation procedure",
            status = "Passed",
            evidence = "Runbook documented and no code edit required"
        });
        Assert.Equal(HttpStatusCode.OK, secretCheckResponse.StatusCode);

        var vulnerabilityCheckResponse = await authenticated.PostAsJsonAsync("/api/enterprise/operations/security-checks", new
        {
            checkType = "DependencyVulnerability",
            name = "Critical dependency scan",
            status = "Passed",
            evidence = "No known critical vulnerability gate failures"
        });
        Assert.Equal(HttpStatusCode.OK, vulnerabilityCheckResponse.StatusCode);

        var runtimeStatusResponse = await authenticated.GetAsync("/api/enterprise/operations/runtime-dependencies/status");
        Assert.Equal(HttpStatusCode.OK, runtimeStatusResponse.StatusCode);
        var runtimeStatusBody = await runtimeStatusResponse.Content.ReadAsStringAsync();
        Assert.Contains("API wrappers", runtimeStatusBody);

        var metricsResponse = await authenticated.GetAsync("/api/enterprise/operations/metrics/summary");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
        var metricsBody = await metricsResponse.Content.ReadAsStringAsync();
        Assert.Contains("latestMeasuredRtoMinutes", metricsBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("45", metricsBody);

        var overviewResponse = await authenticated.GetAsync("/api/enterprise/operations/overview");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
        var overview = await overviewResponse.Content.ReadAsStringAsync();
        Assert.Contains("backupRuns", overview, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("restoreDrills", overview, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaffToken_CannotAdministerReleaseReadiness()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1003, "staff.role", "Staff"));
        var response = await authenticated.PostAsJsonAsync("/api/enterprise/release-readiness/qa-test-runs", new
        {
            testType = "Load",
            profile = "MediumCompany",
            evidenceReference = "/qa/load/run-1",
            notes = "Load test"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanRecordQaReleaseGatesAndApproveCandidate()
    {
        using var authenticated = CreateClientWithBearer(CreateJwtToken(1002, "admin.test", "Admin"));

        var qaResponse = await authenticated.PostAsJsonAsync("/api/enterprise/release-readiness/qa-test-runs", new
        {
            testType = "LoadStressSoakChaos",
            profile = "MediumCompany",
            evidenceReference = "/qa/enterprise/load-stress-soak-chaos",
            notes = "Commercial readiness QA run"
        });
        Assert.Equal(HttpStatusCode.OK, qaResponse.StatusCode);
        var qa = await ReadJsonAsync(qaResponse);
        var qaTestRunId = qa.RootElement.GetProperty("qaTestRunId").GetInt64();

        var qaCompleteResponse = await authenticated.PatchAsJsonAsync($"/api/enterprise/release-readiness/qa-test-runs/{qaTestRunId}/complete", new
        {
            passedCount = 12,
            failedCount = 0,
            evidenceReference = "/qa/enterprise/load-stress-soak-chaos/passed",
            notes = "All required simulated gates passed"
        });
        Assert.Equal(HttpStatusCode.OK, qaCompleteResponse.StatusCode);

        var candidateResponse = await authenticated.PostAsJsonAsync("/api/enterprise/release-readiness/release-candidates", new
        {
            version = "2.0-enterprise-readiness",
            migrationId = "AddEnterpriseSecurityPlatform",
            buildReference = "local-build"
        });
        Assert.Equal(HttpStatusCode.OK, candidateResponse.StatusCode);
        var candidate = await ReadJsonAsync(candidateResponse);
        var releaseCandidateId = candidate.RootElement.GetProperty("releaseCandidateId").GetInt64();

        var requiredGates = new[]
        {
            "API tests",
            "Frontend build",
            "No-touch verification",
            "Migration reviewed",
            "Runbooks updated",
            "Security checks",
            "Load/stress/soak/chaos evidence"
        };

        foreach (var gate in requiredGates)
        {
            var gateResponse = await authenticated.PostAsJsonAsync($"/api/enterprise/release-readiness/release-candidates/{releaseCandidateId}/gate-checks", new
            {
                gateName = gate,
                status = "Passed",
                required = true,
                evidenceReference = $"/release-gates/{gate.Replace("/", "-").Replace(" ", "-").ToLowerInvariant()}",
                notes = "Verified by automated or documented acceptance evidence"
            });
            Assert.Equal(HttpStatusCode.OK, gateResponse.StatusCode);
        }

        var runbookResponse = await authenticated.PostAsJsonAsync("/api/enterprise/release-readiness/runbook-acknowledgements", new
        {
            runbookName = "Company-wide security operations runbook",
            roleName = "SecurityAdmin",
            evidenceReference = "/docs/company-wide-security-platform-runbooks.md"
        });
        Assert.Equal(HttpStatusCode.OK, runbookResponse.StatusCode);

        var approveResponse = await authenticated.PatchAsync($"/api/enterprise/release-readiness/release-candidates/{releaseCandidateId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approveBody = await approveResponse.Content.ReadAsStringAsync();
        Assert.Contains("Approved", approveBody);

        var overviewResponse = await authenticated.GetAsync("/api/enterprise/release-readiness/overview");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
        var overview = await overviewResponse.Content.ReadAsStringAsync();
        Assert.Contains("approvedReleaseCandidates", overview, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("runbookAcknowledgements", overview, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminLogin_RequiresTotpSetup_ThenAcceptsTotp()
    {
        var setupResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = "admin.test",
            password = "Admin@12345"
        });

        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);
        var setup = await ReadJsonAsync(setupResponse);
        Assert.True(setup.RootElement.GetProperty("requiresMfa").GetBoolean());
        Assert.True(setup.RootElement.GetProperty("requiresMfaSetup").GetBoolean());
        var secret = GetString(setup, "mfaSetupSecret");
        Assert.False(string.IsNullOrWhiteSpace(secret));

        var code = GenerateTotpCode(secret);
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = "admin.test",
            password = "Admin@12345",
            mfaCode = code
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await ReadJsonAsync(loginResponse);
        Assert.False(login.RootElement.GetProperty("requiresMfa").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(GetString(login, "token")));
    }

    private HttpClient CreateClientWithBearer(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static bool HasTrustBoundary(MemberInfo member) =>
        member.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any() ||
        member.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    private static string GetString(JsonDocument document, string propertyName) =>
        document.RootElement.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static string GenerateTotpCode(string secret)
    {
        var secretBytes = Base32Decode(secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D6");
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

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sanitized = input.Trim().Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var c in sanitized)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0)
                throw new FormatException("Invalid base32 value.");

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 255));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
    }
}
