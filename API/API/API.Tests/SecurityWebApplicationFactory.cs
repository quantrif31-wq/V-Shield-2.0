using API.Data;
using API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace API.Tests;

public sealed class SecurityWebApplicationFactory : WebApplicationFactory<API.Program>
{
    private readonly string _databaseName = $"vshield-security-tests-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

            if (!db.AppUsers.Any())
            {
                db.AppUsers.AddRange(
                    new AppUser
                    {
                        UserId = 1001,
                        Username = "staff.test",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@12345"),
                        FullName = "Staff Test",
                        Role = "Staff",
                        IsActive = true,
                        TokenVersion = 0,
                        CreatedAt = DateTime.UtcNow
                    },
                    new AppUser
                    {
                        UserId = 1002,
                        Username = "admin.test",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345"),
                        FullName = "Admin Test",
                        Role = "Admin",
                        IsActive = true,
                        TokenVersion = 0,
                        CreatedAt = DateTime.UtcNow
                    },
                    new AppUser
                    {
                        UserId = 1003,
                        Username = "staff.role",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@12345"),
                        FullName = "Staff Role Test",
                        Role = "Staff",
                        IsActive = true,
                        TokenVersion = 0,
                        CreatedAt = DateTime.UtcNow
                    });
                db.SaveChanges();
            }

            if (!db.NotificationRules.Any())
            {
                db.NotificationRules.AddRange(
                    new NotificationRule { EventType = "Alarm.Generic", SeverityMin = "Critical", RecipientRole = "Admin", NotifyWeb = true, NotifyMobile = true },
                    new NotificationRule { EventType = "Alarm.Generic", SeverityMin = "Critical", RecipientRole = "BaoVe", NotifyWeb = true, NotifyMobile = true });
                db.SaveChanges();
            }
        });
    }
}
