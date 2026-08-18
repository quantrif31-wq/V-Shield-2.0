using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

public sealed class AttendanceAnomalyServiceTests
{
    private static int _databaseCounter;

    private static ApplicationDbContext CreateDatabase(out ServiceProvider provider)
    {
        var databaseName = $"attendance-anomaly-{System.Threading.Interlocked.Increment(ref _databaseCounter)}";
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
    public async Task DetectAnomaliesAsync_DetectsSuspiciousShortShift()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var today = DateTime.Today;
            db.Employees.Add(Employee(1));
            db.Attendances.Add(new Attendance
            {
                EmployeeId = 1,
                WorkDate = today,
                CheckIn = today.AddHours(8),
                CheckOut = today.AddHours(8).AddMinutes(30),
                Status = AttendanceStatuses.Completed
            });
            await db.SaveChangesAsync();

            var service = new AttendanceAnomalyService(db);
            var anomalies = await service.DetectAnomaliesAsync();

            Assert.Contains(anomalies, a => a.AnomalyType == AttendanceAnomalyTypes.SuspiciousTime);
            Assert.Contains(anomalies, a => a.Description.Contains("cach nhau"));
        }
    }

    [Fact]
    public async Task DetectAnomaliesAsync_DetectsMissingCheckOut()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var today = DateTime.Today;
            db.Employees.Add(Employee(1));
            db.Attendances.Add(new Attendance
            {
                EmployeeId = 1,
                WorkDate = today,
                CheckIn = today.AddHours(8),
                Status = AttendanceStatuses.CheckedIn
            });
            await db.SaveChangesAsync();

            var service = new AttendanceAnomalyService(db);
            var anomalies = await service.DetectAnomaliesAsync();

            Assert.Contains(anomalies, a => a.AnomalyType == AttendanceAnomalyTypes.MissingCheckOut);
        }
    }

    [Fact]
    public async Task DetectAnomaliesAsync_DetectsDuplicateCheckIn()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var today = DateTime.Today;
            db.Employees.Add(Employee(1));
            db.Attendances.AddRange(
                new Attendance { EmployeeId = 1, WorkDate = today, CheckIn = today.AddHours(8), Status = AttendanceStatuses.CheckedIn },
                new Attendance { EmployeeId = 1, WorkDate = today, CheckIn = today.AddHours(9), Status = AttendanceStatuses.CheckedIn });
            await db.SaveChangesAsync();

            var service = new AttendanceAnomalyService(db);
            var anomalies = await service.DetectAnomaliesAsync();

            Assert.Contains(anomalies, a => a.AnomalyType == AttendanceAnomalyTypes.DuplicateCheckIn);
        }
    }

    [Fact]
    public async Task GetAnomaliesAsync_FiltersAndOrders()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Employees.AddRange(Employee(1), Employee(2));
            db.Set<AttendanceAnomaly>().AddRange(
                new AttendanceAnomaly { EmployeeId = 1, AnomalyType = AttendanceAnomalyTypes.SuspiciousTime, Severity = AnomalySeverities.High, Status = AnomalyStatuses.Open, WorkDate = DateTime.Today },
                new AttendanceAnomaly { EmployeeId = 2, AnomalyType = AttendanceAnomalyTypes.MissingCheckOut, Severity = AnomalySeverities.Medium, Status = AnomalyStatuses.Resolved, WorkDate = DateTime.Today });
            await db.SaveChangesAsync();

            var service = new AttendanceAnomalyService(db);
            var byEmployee = await service.GetAnomaliesAsync(employeeId: 1);
            Assert.Single(byEmployee);
            Assert.Equal(1, byEmployee[0].EmployeeId);

            var byStatus = await service.GetAnomaliesAsync(status: AnomalyStatuses.Resolved);
            Assert.Single(byStatus);

            var byType = await service.GetAnomaliesAsync(type: AttendanceAnomalyTypes.MissingCheckOut);
            Assert.Single(byType);
        }
    }

    [Fact]
    public async Task ResolveAnomalyAsync_MarksResolvedAndPersists()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Set<AttendanceAnomaly>().Add(new AttendanceAnomaly
            {
                EmployeeId = 1,
                AnomalyType = AttendanceAnomalyTypes.SuspiciousTime,
                Severity = AnomalySeverities.Medium,
                Status = AnomalyStatuses.Open
            });
            await db.SaveChangesAsync();

            var service = new AttendanceAnomalyService(db);
            await service.ResolveAnomalyAsync(1, "Reviewed", 7);

            var stored = await db.Set<AttendanceAnomaly>().SingleAsync();
            Assert.Equal(AnomalyStatuses.Resolved, stored.Status);
            Assert.Equal("Reviewed", stored.Resolution);
            Assert.Equal(7, stored.ResolvedBy);
            Assert.NotNull(stored.ResolvedAt);
        }
    }

    [Fact]
    public async Task ResolveAnomalyAsync_Throws_WhenMissing()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var service = new AttendanceAnomalyService(db);
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.ResolveAnomalyAsync(999, "x", 1));
        }
    }

    [Fact]
    public async Task MarkFalsePositiveAsync_SetsStatus()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            db.Set<AttendanceAnomaly>().Add(new AttendanceAnomaly
            {
                EmployeeId = 1,
                AnomalyType = AttendanceAnomalyTypes.SuspiciousTime,
                Severity = AnomalySeverities.Medium,
                Status = AnomalyStatuses.Open
            });
            await db.SaveChangesAsync();

            var service = new AttendanceAnomalyService(db);
            await service.MarkFalsePositiveAsync(1, 3);

            var stored = await db.Set<AttendanceAnomaly>().SingleAsync();
            Assert.Equal(AnomalyStatuses.FalsePositive, stored.Status);
            Assert.Equal(3, stored.ResolvedBy);
        }
    }

    [Fact]
    public async Task PredictAbsencesAsync_ReturnsPredictionsForScheduledDays()
    {
        await using var db = CreateDatabase(out var provider);
        await using (provider)
        {
            var today = DateTime.Today;
            db.Employees.Add(Employee(1));
            var shift = new Shift { ShiftName = "Ca sáng", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 0, 0) };
            db.Shifts.Add(shift);
            await db.SaveChangesAsync();

            for (var i = 1; i <= 3; i++)
            {
                db.WorkSchedules.Add(new WorkSchedule
                {
                    EmployeeId = 1,
                    ShiftId = shift.ShiftId,
                    WorkDate = today.AddDays(i),
                    Status = WorkScheduleStatuses.Scheduled
                });
            }
            // Historical absences on the same weekday.
            db.Attendances.Add(new Attendance
            {
                EmployeeId = 1,
                WorkDate = today.AddDays(-7),
                Status = AttendanceStatuses.Absent
            });
            await db.SaveChangesAsync();

            var service = new AttendanceAnomalyService(db);
            var predictions = await service.PredictAbsencesAsync(1, lookAheadDays: 7);

            Assert.Equal(3, predictions.Count);
            var first = predictions[0];
            var risk = first.GetType().GetProperty("risk")!.GetValue(first)!.ToString();
            Assert.NotNull(first.GetType().GetProperty("shiftName")!.GetValue(first));
            Assert.NotEmpty(risk);
        }
    }
}
