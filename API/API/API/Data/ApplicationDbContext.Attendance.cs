using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }

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
    }
}
