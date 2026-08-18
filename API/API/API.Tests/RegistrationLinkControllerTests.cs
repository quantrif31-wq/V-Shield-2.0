using API.Controllers;
using API.Data;
using API.DTOs.PreRegistration;
using API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace API.Tests;

public sealed class RegistrationLinkControllerTests
{
    private static ApplicationDbContextWrapper Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"reglink_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContextWrapper(new ApplicationDbContext(options));
    }

    private sealed class ApplicationDbContextWrapper : IDisposable
    {
        public ApplicationDbContext Db { get; }
        public ApplicationDbContextWrapper(ApplicationDbContext db) => Db = db;
        public void Dispose() => Db.Dispose();
    }

    private static Employee SeedEmployee(ApplicationDbContext db, int id, string name)
    {
        var employee = new Employee { EmployeeId = id, FullName = name, Email = $"e{id}@x.com", Status = true };
        db.Employees.Add(employee);
        db.SaveChanges();
        return employee;
    }

    private static RegistrationLinkController CreateController(
        ApplicationDbContext db, Dictionary<string, string?>? configValues = null, string? origin = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues ?? new Dictionary<string, string?>())
            .Build();
        var controller = new RegistrationLinkController(db, config);
        var http = new DefaultHttpContext();
        if (origin != null)
            http.Request.Headers["Origin"] = origin;
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    [Fact]
    public async Task CreateLink_EmployeeNotFound_ReturnsNotFound()
    {
        using var wrapper = Create();
        var controller = CreateController(wrapper.Db);
        var result = await controller.CreateLink(new CreateLinkRequestDto { HostEmployeeId = 999, ExpiryHours = 24 });
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateLink_CreatesLinkAndUsesGuestPortalUrl()
    {
        using var wrapper = Create();
        var db = wrapper.Db;
        SeedEmployee(db, 1, "Nguyen Van A");
        var controller = CreateController(db, new Dictionary<string, string?>
        {
            ["AppSettings:GuestRegistrationPortalUrl"] = "https://guest.example.com/"
        });

        var result = await controller.CreateLink(new CreateLinkRequestDto { HostEmployeeId = 1, ExpiryHours = 24 });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CreateLinkResponseDto>(ok.Value);
        Assert.Equal(32, dto.Token.Length);
        Assert.StartsWith("https://guest.example.com/register/", dto.RegistrationUrl);
        Assert.Single(db.RegistrationLinks);
        Assert.Equal(1, db.RegistrationLinks.Single().HostEmployeeId);
    }

    [Fact]
    public async Task CreateLink_WithoutConfig_UsesOriginHeader()
    {
        using var wrapper = Create();
        var db = wrapper.Db;
        SeedEmployee(db, 1, "Nguyen Van A");
        var controller = CreateController(db, new Dictionary<string, string?>(), origin: "https://app.example.com");

        var result = await controller.CreateLink(new CreateLinkRequestDto { HostEmployeeId = 1, ExpiryHours = 1 });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CreateLinkResponseDto>(ok.Value);
        Assert.StartsWith("https://app.example.com/register/", dto.RegistrationUrl);
    }

    [Fact]
    public async Task GetLinks_FiltersByTokenQuery()
    {
        using var wrapper = Create();
        var db = wrapper.Db;
        SeedEmployee(db, 1, "Nguyen Van A");
        db.RegistrationLinks.Add(new RegistrationLink { Token = "token-abc-123", HostEmployeeId = 1, CreatedAt = DateTime.Now });
        db.RegistrationLinks.Add(new RegistrationLink { Token = "token-xyz-999", HostEmployeeId = 1, CreatedAt = DateTime.Now });
        db.SaveChanges();
        var controller = CreateController(db);

        var result = await controller.GetLinks(query: "abc");

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = (IEnumerable<object>)ok.Value!;
        Assert.Single(items);
    }

    [Fact]
    public async Task GetLinks_FiltersByEmployeeNameQuery()
    {
        using var wrapper = Create();
        var db = wrapper.Db;
        SeedEmployee(db, 1, "Tran Van B");
        db.RegistrationLinks.Add(new RegistrationLink { Token = "token-1", HostEmployeeId = 1, CreatedAt = DateTime.Now });
        db.SaveChanges();
        var controller = CreateController(db);

        var result = await controller.GetLinks(query: "Tran Van B");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Single((IEnumerable<object>)ok.Value!);
    }

    [Fact]
    public async Task GetLinks_NoQuery_ReturnsAll()
    {
        using var wrapper = Create();
        var db = wrapper.Db;
        SeedEmployee(db, 1, "Nguyen Van A");
        db.RegistrationLinks.Add(new RegistrationLink { Token = "token-1", HostEmployeeId = 1, CreatedAt = DateTime.Now });
        db.RegistrationLinks.Add(new RegistrationLink { Token = "token-2", HostEmployeeId = 1, CreatedAt = DateTime.Now });
        db.SaveChanges();
        var controller = CreateController(db);

        var result = await controller.GetLinks();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, ((IEnumerable<object>)ok.Value!).Count());
    }

    [Fact]
    public async Task GetLinks_MarksExpiredFlag()
    {
        using var wrapper = Create();
        var db = wrapper.Db;
        SeedEmployee(db, 1, "Nguyen Van A");
        db.RegistrationLinks.Add(new RegistrationLink { Token = "token-expired", HostEmployeeId = 1, ExpiredAt = DateTime.Now.AddHours(-2), CreatedAt = DateTime.Now });
        db.SaveChanges();
        var controller = CreateController(db);

        var result = await controller.GetLinks();

        var ok = Assert.IsType<OkObjectResult>(result);
        var item = ((IEnumerable<object>)ok.Value!).Cast<object>().Single();
        var isExpired = item.GetType().GetProperty("isExpired")!.GetValue(item);
        Assert.Equal(true, isExpired);
    }
}