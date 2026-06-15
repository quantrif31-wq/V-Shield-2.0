using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<CampusMapLayout> CampusMapLayouts { get; set; }
    public DbSet<ZoneTransit> ZoneTransits { get; set; }
    public DbSet<AttendanceAnomaly> AttendanceAnomalies { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.ShiftId);
            entity.Property(e => e.ShiftName).IsRequired().HasMaxLength(120);
            entity.HasIndex(e => e.ShiftName).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.BreakMinutes).HasDefaultValue(0);
            entity.Property(e => e.AllowedLateMinutes).HasDefaultValue(0);
            entity.Property(e => e.AllowedEarlyLeaveMinutes).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasData(
                new Shift
                {
                    ShiftId = 1,
                    ShiftName = "Ca hành chính",
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    BreakMinutes = 60,
                    AllowedLateMinutes = 5,
                    AllowedEarlyLeaveMinutes = 5,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Shift
                {
                    ShiftId = 2,
                    ShiftName = "Ca sáng",
                    StartTime = new TimeSpan(7, 0, 0),
                    EndTime = new TimeSpan(11, 0, 0),
                    BreakMinutes = 0,
                    AllowedLateMinutes = 5,
                    AllowedEarlyLeaveMinutes = 5,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Shift
                {
                    ShiftId = 3,
                    ShiftName = "Ca chiều",
                    StartTime = new TimeSpan(13, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    BreakMinutes = 0,
                    AllowedLateMinutes = 5,
                    AllowedEarlyLeaveMinutes = 5,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Shift
                {
                    ShiftId = 4,
                    ShiftName = "Ca tối",
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(22, 0, 0),
                    BreakMinutes = 0,
                    AllowedLateMinutes = 5,
                    AllowedEarlyLeaveMinutes = 5,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });

        modelBuilder.Entity<WorkSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);
            entity.Property(e => e.WorkDate).HasColumnType("date");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue(WorkScheduleStatuses.Scheduled);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.EmployeeId, e.ShiftId, e.WorkDate }).IsUnique();

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AttendanceModule_WorkSchedule_Employee");

            entity.HasOne(e => e.Shift)
                .WithMany(s => s.WorkSchedules)
                .HasForeignKey(e => e.ShiftId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AttendanceModule_WorkSchedule_Shift");

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AttendanceModule_WorkSchedule_CreatedByUser");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId);
            entity.Property(e => e.WorkDate).HasColumnType("date");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40).HasDefaultValue(AttendanceStatuses.NotCheckedIn);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(20).HasDefaultValue(AttendanceSources.Manual);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.TotalWorkingHours).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate, e.ScheduleId }).IsUnique();

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AttendanceModule_Attendance_Employee");

            entity.HasOne(e => e.Schedule)
                .WithMany(s => s.Attendances)
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AttendanceModule_Attendance_WorkSchedule");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.LeaveRequestId);
            entity.Property(e => e.StartDate).HasColumnType("date");
            entity.Property(e => e.EndDate).HasColumnType("date");
            entity.Property(e => e.LeaveType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue(LeaveRequestStatuses.Pending);
            entity.Property(e => e.RejectReason).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AttendanceModule_LeaveRequest_Employee");

            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApproverId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AttendanceModule_LeaveRequest_ApproverUser");
        });

        modelBuilder.Entity<CampusMapLayout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GateId).IsUnique();
            entity.Property(e => e.X).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Y).HasColumnType("decimal(10,2)");
            entity.Property(e => e.W).HasColumnType("decimal(10,2)");
            entity.Property(e => e.H).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ZIndex).HasDefaultValue(1);
            entity.Property(e => e.Color).HasMaxLength(30);
            entity.Property(e => e.Icon).HasMaxLength(80);
            entity.Property(e => e.IsVisible).HasDefaultValue(true);
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(e => e.Gate)
                .WithMany()
                .HasForeignKey(e => e.GateId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CampusMapLayouts_Gate");

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CampusMapLayouts_AppUser");
        });

        modelBuilder.Entity<ZoneTransit>(entity =>
        {
            entity.HasKey(e => e.ZoneTransitId);
            entity.Property(e => e.Direction).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.EmployeeId, e.Timestamp });

            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ZoneTransit_Employee");

            entity.HasOne(e => e.SecurityZone)
                .WithMany()
                .HasForeignKey(e => e.SecurityZoneId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ZoneTransit_SecurityZone");

            entity.HasOne(e => e.AccessPoint)
                .WithMany()
                .HasForeignKey(e => e.AccessPointId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ZoneTransit_AccessPoint");

            entity.HasOne(e => e.AccessLog)
                .WithMany()
                .HasForeignKey(e => e.AccessLogId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ZoneTransit_AccessLog");

            entity.HasMany(e => e.Attendances)
                .WithMany(a => a.ZoneTransits)
                .UsingEntity(j => j.ToTable("AttendanceZoneTransits"));
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.Property(e => e.ZoneDwellTime).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            entity.Property(e => e.ZoneTransitCount).HasDefaultValue(0);
            entity.Property(e => e.IsZoneDerived).HasDefaultValue(false);
        });

        ConfigureCompanySecurityFoundation(modelBuilder);
        ConfigureAccessPolicyEngine(modelBuilder);
        ConfigureVisitorVehicleOperations(modelBuilder);
        ConfigureDeviceProtocolOperations(modelBuilder);
        ConfigureSituationalAwareness(modelBuilder);
        ConfigureSocIncidentOperations(modelBuilder);
        ConfigureEvidenceComplianceGovernance(modelBuilder);
        ConfigureOperationsResilience(modelBuilder);
        ConfigureReleaseReadiness(modelBuilder);
        ConfigurePrivilegedOperations(modelBuilder);
        ConfigureMfaRecovery(modelBuilder);
        ConfigureImportExport(modelBuilder);
        ConfigureUeba(modelBuilder);
        ConfigureAiCore(modelBuilder);
        ConfigureRateLimiting(modelBuilder);
    }

    private static void ConfigureRateLimiting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RateLimitCounter>(entity =>
        {
            entity.HasIndex(e => new { e.CounterKey, e.WindowStart }).HasDatabaseName("IX_RateLimitCounters_Key_Window");
            entity.HasIndex(e => e.CreatedAtUtc).HasDatabaseName("IX_RateLimitCounters_CreatedAt");
        });
    }
}
