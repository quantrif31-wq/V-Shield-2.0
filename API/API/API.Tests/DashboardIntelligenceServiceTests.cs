using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class DashboardIntelligenceServiceTests
{
    private static int _databaseCounter;

    private static ApplicationDbContext CreateDatabase(out ServiceProvider provider)
    {
        var databaseName = $"dashboard-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName, new InMemoryDatabaseRoot())
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));
        provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ApplicationDbContext>();
    }

    private static Employee Employee(int id) => new()
    {
        EmployeeId = id,
        FullName = $"Employee {id}",
        Status = true,
        LifecycleStatus = EmployeeLifecycleStates.Active
    };

    [Fact]
    public async Task GetIntelligenceAsync_ReturnsSummaryWithEmptyData()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new DashboardIntelligenceService(db);
            var result = await service.GetIntelligenceAsync();

            var summary = result.GetType().GetProperty("summary")!.GetValue(result)!.ToString();
            Assert.Contains("Khong phat hien bat thuong", summary);
            Assert.Contains("Do phu nhan dien AI dat 0%", summary);

            var insights = (IEnumerable<object>)result.GetType().GetProperty("insights")!.GetValue(result)!;
            Assert.Empty(insights);
        }
    }

    [Fact]
    public async Task GetIntelligenceAsync_ReflectsAttendanceAndTraffic()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var today = DateTime.Today;
            db.Employees.AddRange(Employee(1), Employee(2), Employee(3), Employee(4), Employee(5));
            db.Attendances.Add(new Attendance
            {
                EmployeeId = 1,
                WorkDate = today,
                CheckIn = today.AddHours(8),
                CheckOut = today.AddHours(17),
                LateMinutes = 0,
                Status = AttendanceStatuses.Completed
            });
            db.Attendances.AddRange(
                new Attendance
                {
                    EmployeeId = 2,
                    WorkDate = today,
                    CheckIn = today.AddHours(9),
                    LateMinutes = 30,
                    Status = AttendanceStatuses.Late
                },
                new Attendance
                {
                    EmployeeId = 3,
                    WorkDate = today,
                    CheckIn = today.AddHours(9),
                    LateMinutes = 30,
                    Status = AttendanceStatuses.Late
                },
                new Attendance
                {
                    EmployeeId = 4,
                    WorkDate = today,
                    CheckIn = today.AddHours(9),
                    LateMinutes = 30,
                    Status = AttendanceStatuses.Late
                },
                new Attendance
                {
                    EmployeeId = 5,
                    WorkDate = today,
                    CheckIn = today.AddHours(9),
                    LateMinutes = 30,
                    Status = AttendanceStatuses.Late
                });
            for (var i = 1; i <= 5; i++)
            {
                db.WorkSchedules.Add(new WorkSchedule
                {
                    EmployeeId = i,
                    ShiftId = 1,
                    WorkDate = today,
                    Status = WorkScheduleStatuses.Scheduled
                });
            }
            db.AccessLogs.AddRange(
                AccessLog(1, today.AddHours(8), "IN", 1),
                AccessLog(2, today.AddHours(9), "IN", 2),
                AccessLog(3, today.AddHours(18), "OUT", 1));
            db.EmployeeFaceModels.Add(new EmployeeFaceModel { EmployeeId = 1 });
            await db.SaveChangesAsync();

            var service = new DashboardIntelligenceService(db);
            var result = await service.GetIntelligenceAsync();

            var summary = result.GetType().GetProperty("summary")!.GetValue(result)!.ToString();
            Assert.Contains("5 nhan su duoc phan cong lam viec", summary);
            Assert.Contains("4 nguoi di tre", summary);
            Assert.Contains("Do phu nhan dien AI dat 20%", summary);

            var trends = (IEnumerable<object>)result.GetType().GetProperty("trends")!.GetValue(result)!;
            Assert.Equal(7, trends.Count());

            var insights = (IEnumerable<object>)result.GetType().GetProperty("insights")!.GetValue(result)!;
            var insightList = insights.ToList();
            Assert.Contains(insightList, i => i.GetType().GetProperty("title")!.GetValue(i)!.ToString()!.Contains("di tre"));
            Assert.Contains(insightList, i => i.GetType().GetProperty("title")!.GetValue(i)!.ToString()!.Contains("cao diem"));
        }
    }

    [Fact]
    public async Task GetIntelligenceAsync_WithNoAccessLogsToday_StillReturnsTrends()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Employees.Add(Employee(1));
            db.Attendances.Add(new Attendance
            {
                EmployeeId = 1,
                WorkDate = DateTime.Today.AddDays(-1),
                CheckIn = DateTime.Today.AddDays(-1).AddHours(8),
                Status = AttendanceStatuses.Completed
            });
            db.AccessLogs.Add(AccessLog(1, DateTime.Today.AddDays(-1).AddHours(8), "IN", 1));
            await db.SaveChangesAsync();

            var service = new DashboardIntelligenceService(db);
            var result = await service.GetIntelligenceAsync();

            var comparison = result.GetType().GetProperty("comparison")!.GetValue(result)!;
            Assert.NotNull(comparison);
            Assert.NotNull(result.GetType().GetProperty("generatedAt")!.GetValue(result));
        }
    }

    private static AccessLog AccessLog(int id, DateTime timestamp, string direction, int? employeeId)
    {
        return new AccessLog
        {
            LogId = id,
            EmployeeId = employeeId,
            Timestamp = timestamp,
            Direction = direction,
            ResultStatus = "Approved"
        };
    }
}
