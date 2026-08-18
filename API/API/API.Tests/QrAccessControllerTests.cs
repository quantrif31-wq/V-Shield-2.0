using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Controllers;
using API.Data;
using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class QrAccessControllerTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qraccess_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ClaimsPrincipal Principal(int userId, string role) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "test"));

    private static QrAccessController Create(
        ApplicationDbContext db,
        ClaimsPrincipal? principal = null,
        StaticVisitorQrService? visitorQr = null,
        UserOperationalScopeService? scopeService = null)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(Path.GetTempPath());
        env.SetupGet(e => e.WebRootPath).Returns(Path.GetTempPath());
        var evidence = new EvidenceCaptureService(db, env.Object);

        var controller = new QrAccessController(
            db,
            visitorQr ?? new StaticVisitorQrService(),
            new Mock<IZoneTransitService>().Object,
            evidence,
            scopeService ?? new UserOperationalScopeService(db))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal ?? Principal(1, "Admin") }
            }
        };
        return controller;
    }

    private static async Task SeedCameraAndGateAsync(ApplicationDbContext db, int cameraId = 5, int gateId = 1)
    {
        db.Gates.Add(new Gate { GateId = gateId, GateName = "Cổng A" });
        db.Cameras.Add(new Camera { CameraId = cameraId, GateId = gateId, CameraName = "Cam" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedActiveUserAsync(ApplicationDbContext db, int userId = 1, string role = "Admin")
    {
        db.AppUsers.Add(new AppUser { UserId = userId, Username = "u", PasswordHash = "x", Role = role, IsActive = true });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ScanAccess_NullRequest_BadRequest()
    {
        var db = CreateDb();
        var result = await Create(db).ScanAccess(null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ScanAccess_InvalidCameraId_BadRequest()
    {
        var db = CreateDb();
        var result = await Create(db).ScanAccess(new QrScanAccessRequest { CameraId = 0 });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ScanAccess_CameraMissing_Unauthorized()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);
        var result = await Create(db).ScanAccess(new QrScanAccessRequest { CameraId = 99 });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ScanAccess_GateMismatch_BadRequest()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);
        await SeedCameraAndGateAsync(db, 5, 1);

        var result = await Create(db).ScanAccess(new QrScanAccessRequest
        {
            CameraId = 5,
            GateId = 9,
            EmployeeId = 10
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ScanAccess_NoIdentity_BadRequest()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);
        await SeedCameraAndGateAsync(db, 5, 1);

        var result = await Create(db).ScanAccess(new QrScanAccessRequest
        {
            CameraId = 5,
            QrPayload = "   "
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ScanAccess_UserInactive_Unauthorized()
    {
        var db = CreateDb();
        db.AppUsers.Add(new AppUser { UserId = 1, Username = "u", PasswordHash = "x", Role = "Admin", IsActive = false });
        await db.SaveChangesAsync();
        await SeedCameraAndGateAsync(db, 5, 1);

        var result = await Create(db).ScanAccess(new QrScanAccessRequest
        {
            CameraId = 5,
            EmployeeId = 10
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ScanAccess_EmployeeInDynamicPayload_NoValidConfig_BadRequest()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);
        await SeedCameraAndGateAsync(db, 5, 1);

        var result = await Create(db).ScanAccess(new QrScanAccessRequest
        {
            CameraId = 5,
            QrPayload = "EMP:10|TS:1700000000|OTP:000000"
        });

        // dynamic validation fails -> falls back to identity check -> no visitor -> bad request
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task VerifyCameraAuth_NullRequest_BadRequest()
    {
        var db = CreateDb();
        var result = await Create(db).VerifyCameraAuth(null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task VerifyCameraAuth_CameraMissing_Unauthorized()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);
        var result = await Create(db).VerifyCameraAuth(new QrScanAccessRequest { CameraId = 77 });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task VerifyCameraAuth_Success_ReturnsOk()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);
        await SeedCameraAndGateAsync(db, 5, 1);

        var result = await Create(db).VerifyCameraAuth(new QrScanAccessRequest { CameraId = 5 });

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GateTransitApiResponse>(ok.Value);
        Assert.True(payload.Success);
    }

    [Fact]
    public async Task VerifyCameraAuth_GateMismatch_BadRequest()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);
        await SeedCameraAndGateAsync(db, 5, 1);

        var result = await Create(db).VerifyCameraAuth(new QrScanAccessRequest { CameraId = 5, GateId = 3 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ManualAccess_NullRequest_BadRequest()
    {
        var db = CreateDb();
        var result = await Create(db).ManualAccess(null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ManualAccess_Anonymous_Unauthorized()
    {
        var db = CreateDb();
        var controller = Create(db, new ClaimsPrincipal(new ClaimsIdentity()));
        var result = await controller.ManualAccess(new ManualAccessRequest { GateId = 1 });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ManualAccess_InactiveUser_Unauthorized()
    {
        var db = CreateDb();
        db.AppUsers.Add(new AppUser { UserId = 1, Username = "u", PasswordHash = "x", Role = "Admin", IsActive = false });
        await db.SaveChangesAsync();

        var result = await Create(db).ManualAccess(new ManualAccessRequest { GateId = 1 });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ManualAccess_NotAdminRole_Forbidden()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db, 1, "LeTan");

        var result = await Create(db).ManualAccess(new ManualAccessRequest { GateId = 1 });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ManualAccess_GateMissing_NotFound()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);

        var result = await Create(db).ManualAccess(new ManualAccessRequest { GateId = 99, EmployeeId = 10 });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ManualAccess_NoIdentity_BadRequest()
    {
        var db = CreateDb();
        await SeedActiveUserAsync(db);
        await SeedCameraAndGateAsync(db, 5, 1);

        var result = await Create(db).ManualAccess(new ManualAccessRequest { GateId = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}