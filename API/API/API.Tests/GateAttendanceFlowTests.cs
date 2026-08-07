using API.Data;
using API.Models;
using API.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests;

public sealed class GateAttendanceFlowTests
{
    [Fact]
    public async Task SuccessfulEntry_CreatesZoneTransitAndAttendance()
    {
        await using var db = CreateContext();
        var timestamp = new DateTime(2026, 8, 7, 8, 5, 0);
        var log = await SeedFoundationAndLog(db, "IN", "SUCCESS", timestamp);
        var service = CreateService(db);

        await service.ProcessAccessLogAsync(log.LogId);

        var transit = await db.Set<ZoneTransit>().SingleAsync();
        transit.AccessLogId.Should().Be(log.LogId);
        transit.Direction.Should().Be("IN");

        var attendance = await db.Attendances.SingleAsync();
        attendance.EmployeeId.Should().Be(101);
        attendance.CheckIn.Should().Be(timestamp);
        attendance.CheckOut.Should().BeNull();
        attendance.IsZoneDerived.Should().BeTrue();
    }

    [Fact]
    public async Task SuccessfulExit_UpdatesCheckoutWithoutChangingFirstEntry()
    {
        await using var db = CreateContext();
        var entryAt = new DateTime(2026, 8, 7, 8, 0, 0);
        var exitAt = new DateTime(2026, 8, 7, 17, 0, 0);
        var entry = await SeedFoundationAndLog(db, "IN", "SUCCESS", entryAt);
        var service = CreateService(db);
        await service.ProcessAccessLogAsync(entry.LogId);

        var exit = new AccessLog
        {
            Timestamp = exitAt,
            EmployeeId = 101,
            GateId = 1,
            Direction = "OUT",
            ResultStatus = "SUCCESS"
        };
        db.AccessLogs.Add(exit);
        await db.SaveChangesAsync();

        await service.ProcessAccessLogAsync(exit.LogId);

        var attendance = await db.Attendances.SingleAsync();
        attendance.CheckIn.Should().Be(entryAt);
        attendance.CheckOut.Should().Be(exitAt);
        attendance.TotalWorkingHours.Should().Be(9m);
        attendance.ZoneTransitCount.Should().Be(2);
    }

    [Fact]
    public async Task DeniedAccess_DoesNotCreateTransitOrAttendance()
    {
        await using var db = CreateContext();
        var log = await SeedFoundationAndLog(
            db,
            "IN",
            "FAILED",
            new DateTime(2026, 8, 7, 8, 5, 0));
        var service = CreateService(db);

        await service.ProcessAccessLogAsync(log.LogId);

        (await db.Set<ZoneTransit>().CountAsync()).Should().Be(0);
        (await db.Attendances.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReprocessingSameAccessLog_IsIdempotent()
    {
        await using var db = CreateContext();
        var log = await SeedFoundationAndLog(
            db,
            "IN",
            "SUCCESS",
            new DateTime(2026, 8, 7, 8, 5, 0));
        var service = CreateService(db);

        await service.ProcessAccessLogAsync(log.LogId);
        await service.ProcessAccessLogAsync(log.LogId);

        (await db.Set<ZoneTransit>().CountAsync()).Should().Be(1);
        (await db.Attendances.CountAsync()).Should().Be(1);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"gate-attendance-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ZoneTransitService CreateService(ApplicationDbContext db)
    {
        var attendance = new AttendanceZoneService(db, new AttendanceCalculationService());
        return new ZoneTransitService(db, attendance);
    }

    private static async Task<AccessLog> SeedFoundationAndLog(
        ApplicationDbContext db,
        string direction,
        string status,
        DateTime timestamp)
    {
        db.Employees.Add(new Employee { EmployeeId = 101, FullName = "Nhân viên kiểm thử", Status = true });
        db.Gates.Add(new Gate { GateId = 1, GateName = "Cổng chính" });
        db.Set<Site>().Add(new Site { SiteId = 1, CompanyId = 1, Name = "Trụ sở", Code = "HQ" });
        db.Set<SecurityZone>().Add(new SecurityZone
        {
            SecurityZoneId = 1,
            SiteId = 1,
            Name = "Khu vực làm việc",
            Code = "WORK"
        });
        db.Set<AccessPoint>().Add(new AccessPoint
        {
            AccessPointId = 1,
            SiteId = 1,
            SecurityZoneId = 1,
            Name = "Điểm kiểm soát chính"
        });
        db.Set<Lane>().AddRange(
            new Lane { LaneId = 1, SiteId = 1, GateId = 1, AccessPointId = 1, Name = "Làn vào", Direction = "Entry" },
            new Lane { LaneId = 2, SiteId = 1, GateId = 1, AccessPointId = 1, Name = "Làn ra", Direction = "Exit" });

        var log = new AccessLog
        {
            Timestamp = timestamp,
            EmployeeId = 101,
            GateId = 1,
            Direction = direction,
            ResultStatus = status
        };
        db.AccessLogs.Add(log);
        await db.SaveChangesAsync();
        return log;
    }
}
