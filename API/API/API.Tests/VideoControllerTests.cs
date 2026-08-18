using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Controllers;
using API.Data;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace API.Tests;

public sealed class VideoControllerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FaceStoragePathResolver _storage;
    private readonly ApplicationDbContext _db;
    private readonly ClaimsPrincipal _adminPrincipal;
    private readonly ClaimsPrincipal _guardPrincipal;

    public VideoControllerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"vtest_{Guid.NewGuid():N}");
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(_tempRoot);
        env.SetupGet(e => e.WebRootPath).Returns(Path.Combine(_tempRoot, "wwwroot"));
        _storage = new FaceStoragePathResolver(
            Options.Create(new FaceStorageOptions { InputRoot = _tempRoot }),
            env.Object);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"video_{Guid.NewGuid():N}")
            .Options;
        _db = new ApplicationDbContext(options);

        _adminPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "admin")
        }, "test"));

        _guardPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "2"),
            new Claim(ClaimTypes.Role, "BaoVe"),
            new Claim(ClaimTypes.Name, "guard")
        }, "test"));
    }

    public void Dispose()
    {
        _db.Dispose();
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true); } catch { }
    }

    private VideoController Create(ClaimsPrincipal? principal = null)
    {
        var controller = new VideoController(_db, _storage)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal ?? _adminPrincipal
                }
            }
        };
        return controller;
    }

    private static IFormFile FormFile(string fileName, long size)
    {
        var stream = new MemoryStream(new byte[size]);
        return new FormFile(stream, 0, size, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "video/mp4"
        };
    }

    private async Task<Employee> SeedEmployeeAsync(int employeeId, string name = "Khoi")
    {
        var employee = new Employee { EmployeeId = employeeId, FullName = name };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return employee;
    }

    private async Task<AppUser> SeedAdminAsync()
    {
        var user = new AppUser { UserId = 1, Username = "admin", PasswordHash = "x", Role = "Admin" };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<AppUser> SeedGuardAsync(int employeeId)
    {
        var user = new AppUser
        {
            UserId = 2,
            Username = "guard",
            PasswordHash = "x",
            Role = "BaoVe",
            EmployeeId = employeeId
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task UploadVideo_NoFile_BadRequest()
    {
        await SeedAdminAsync();
        var result = await Create().UploadVideo(new UploadVideoRequest());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_EmptyFile_BadRequest()
    {
        await SeedAdminAsync();
        var result = await Create().UploadVideo(new UploadVideoRequest { File = FormFile("a.mp4", 0) });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_TooLarge_BadRequest()
    {
        await SeedAdminAsync();
        var result = await Create().UploadVideo(new UploadVideoRequest
        {
            File = FormFile("a.mp4", 50L * 1024 * 1024 + 1),
            EmployeeId = 10
        });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_WrongExtension_BadRequest()
    {
        await SeedAdminAsync();
        var result = await Create().UploadVideo(new UploadVideoRequest
        {
            File = FormFile("a.txt", 100),
            EmployeeId = 10
        });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_NoUserClaim_Unauthorized()
    {
        await SeedAdminAsync();
        var controller = Create(new ClaimsPrincipal(new ClaimsIdentity()));
        var result = await controller.UploadVideo(new UploadVideoRequest
        {
            File = FormFile("a.mp4", 100),
            EmployeeId = 10
        });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_UserNotFound_Unauthorized()
    {
        var result = await Create().UploadVideo(new UploadVideoRequest
        {
            File = FormFile("a.mp4", 100),
            EmployeeId = 10
        });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_AdminWithoutEmployee_BadRequest()
    {
        await SeedAdminAsync();
        await SeedEmployeeAsync(10);
        var result = await Create().UploadVideo(new UploadVideoRequest { File = FormFile("a.mp4", 100) });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_AdminEmployeeMissing_BadRequest()
    {
        await SeedAdminAsync();
        var result = await Create().UploadVideo(new UploadVideoRequest
        {
            File = FormFile("a.mp4", 100),
            EmployeeId = 999
        });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_NonAdminWithoutEmployee_BadRequest()
    {
        await SeedAdminAsync();
        await _db.AppUsers.AddAsync(new AppUser
        {
            UserId = 2,
            Username = "guard",
            PasswordHash = "x",
            Role = "BaoVe",
            EmployeeId = null
        });
        await _db.SaveChangesAsync();

        var result = await Create(_guardPrincipal).UploadVideo(new UploadVideoRequest
        {
            File = FormFile("a.mp4", 100)
        });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadVideo_Admin_Success()
    {
        await SeedAdminAsync();
        await SeedEmployeeAsync(10);

        var result = await Create().UploadVideo(new UploadVideoRequest
        {
            File = FormFile("clip.mp4", 1024),
            EmployeeId = 10
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var video = Assert.Single(_db.EmployeeFaceVideos);
        Assert.Equal(10, video.EmployeeId);
        Assert.True(video.FileSize > 0);
        Assert.StartsWith("emp_10_", video.FileName);
        Assert.True(System.IO.File.Exists(Path.Combine(_tempRoot, video.FilePath)));
    }

    [Fact]
    public async Task UploadVideo_NonAdminWithEmployee_Success()
    {
        await SeedAdminAsync();
        await SeedGuardAsync(10);
        _db.ChangeTracker.Clear();
        var guard = await _db.AppUsers.Include(u => u.Employee).SingleAsync(u => u.UserId == 2);
        guard.Employee = await SeedEmployeeAsync(10);

        var result = await Create(_guardPrincipal).UploadVideo(new UploadVideoRequest
        {
            File = FormFile("clip.mp4", 1024)
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(10, _db.EmployeeFaceVideos.Single().EmployeeId);
    }

    [Fact]
    public async Task GetVideosByEmployee_FiltersAndOrders()
    {
        _db.EmployeeFaceVideos.AddRange(
            new EmployeeFaceVideo { EmployeeId = 1, FileName = "a.mp4", FilePath = "v/a.mp4", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new EmployeeFaceVideo { EmployeeId = 1, FileName = "b.mp4", FilePath = "v/b.mp4", CreatedAt = DateTime.UtcNow },
            new EmployeeFaceVideo { EmployeeId = 2, FileName = "c.mp4", FilePath = "v/c.mp4", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await Create().GetVideosByEmployee(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var videos = Assert.IsAssignableFrom<List<EmployeeFaceVideo>>(ok.Value);
        Assert.Equal(2, videos.Count);
        Assert.Equal("b.mp4", videos[0].FileName);
    }

    [Fact]
    public async Task GetVideoContent_MissingVideo_NotFound()
    {
        var result = await Create().GetVideoContent(55);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetVideoContent_FileMissing_NotFound()
    {
        _db.EmployeeFaceVideos.Add(new EmployeeFaceVideo
        {
            Id = 1,
            EmployeeId = 10,
            FileName = "nope.mp4",
            FilePath = "video_notok/nope.mp4"
        });
        await _db.SaveChangesAsync();

        var result = await Create().GetVideoContent(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetVideoContent_InvalidFileName_NotFound()
    {
        _db.EmployeeFaceVideos.Add(new EmployeeFaceVideo
        {
            Id = 1,
            EmployeeId = 10,
            FileName = "bad/path.mp4",
            FilePath = "video_notok/bad"
        });
        await _db.SaveChangesAsync();

        var result = await Create().GetVideoContent(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetVideoContent_ExistingFile_ReturnsPhysicalFile()
    {
        var videoDir = Path.Combine(_tempRoot, "video_notok");
        Directory.CreateDirectory(videoDir);
        var fileName = "emp_10_20250101000000000.mp4";
        var fullPath = Path.Combine(videoDir, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, new byte[64]);

        _db.EmployeeFaceVideos.Add(new EmployeeFaceVideo
        {
            Id = 1,
            EmployeeId = 10,
            FileName = fileName,
            FilePath = $"video_notok/{fileName}"
        });
        await _db.SaveChangesAsync();

        var result = await Create().GetVideoContent(1);

        var file = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal("video/mp4", file.ContentType);
        Assert.Equal(fullPath, file.FileName);
        Assert.True(_db.SystemAuditLogs.Count(l => l.ActionType == "READ") == 1, "expected one READ audit log");
    }

    [Fact]
    public async Task DeleteVideo_Missing_NotFound()
    {
        var result = await Create().DeleteVideo(99);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteVideo_Success_RemovesRowAndFile()
    {
        var videoDir = Path.Combine(_tempRoot, "video_notok");
        Directory.CreateDirectory(videoDir);
        var fileName = "emp_10_20250101000000000.mp4";
        var fullPath = Path.Combine(videoDir, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, new byte[64]);

        _db.EmployeeFaceVideos.Add(new EmployeeFaceVideo
        {
            Id = 1,
            EmployeeId = 10,
            FileName = fileName,
            FilePath = $"video_notok/{fileName}"
        });
        await _db.SaveChangesAsync();

        var result = await Create().DeleteVideo(1);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(_db.EmployeeFaceVideos);
        Assert.False(System.IO.File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteVideo_InvalidFileName_NotFound()
    {
        _db.EmployeeFaceVideos.Add(new EmployeeFaceVideo
        {
            Id = 1,
            EmployeeId = 10,
            FileName = "bad/path.mp4",
            FilePath = "video_notok/bad"
        });
        await _db.SaveChangesAsync();

        var result = await Create().DeleteVideo(1);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Single(_db.EmployeeFaceVideos);
    }
}