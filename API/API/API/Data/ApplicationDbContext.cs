using System;
using System.Collections.Generic;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using API.Services.Sync;

namespace API.Data;

public partial class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly ISyncExecutionContext? _syncExecutionContext;
    private readonly SyncEntityEventFactory? _syncEntityEventFactory;
    private bool _isWritingAudit;

    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    [ActivatorUtilitiesConstructor]
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserContext currentUserContext,
        ISyncExecutionContext? syncExecutionContext = null,
        SyncEntityEventFactory? syncEntityEventFactory = null)
        : base(options)
    {
        _currentUserContext = currentUserContext;
        _syncExecutionContext = syncExecutionContext;
        _syncEntityEventFactory = syncEntityEventFactory;
    }

    public virtual DbSet<AccessLog> AccessLogs { get; set; }

    public virtual DbSet<Camera> Cameras { get; set; }
    public virtual DbSet<FaceCameraConfiguration> FaceCameraConfigurations { get; set; }
    public DbSet<RecordedSegment> RecordedSegments { get; set; }
    public virtual DbSet<CameraPlate> CameraPlates { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<ExceptionReason> ExceptionReasons { get; set; }

    public virtual DbSet<Gate> Gates { get; set; }

    public virtual DbSet<GuestProfile> GuestProfiles { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<PreRegistration> PreRegistrations { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleType> VehicleTypes { get; set; }

    public virtual DbSet<AppUser> AppUsers { get; set; }
    // Thêm vào ApplicationDbContext.cs
    public virtual DbSet<RegistrationLink> RegistrationLinks { get; set; }
    public virtual DbSet<VisitorDetail> VisitorDetails { get; set; }
    public virtual DbSet<EmployeeFaceVideo> EmployeeFaceVideos { get; set; }
    public virtual DbSet<EmployeeFaceModel> EmployeeFaceModels { get; set; }
    public virtual DbSet<FaceEnrollmentJob> FaceEnrollmentJobs { get; set; }
    public virtual DbSet<FaceRecognitionEvent> FaceRecognitionEvents { get; set; }
    public virtual DbSet<FaceRecognitionCollectorCheckpoint> FaceRecognitionCollectorCheckpoints { get; set; }
    public virtual DbSet<EmployeeDynamicQr> EmployeeDynamicQrs { get; set; }
    public virtual DbSet<DynamicQrScanLog> DynamicQrScanLogs { get; set; }
    public DbSet<EmployeeAccessPermission> EmployeeAccessPermissions { get; set; }
    public DbSet<VisitorAccessPermission> VisitorAccessPermissions { get; set; }
    public DbSet<SystemAuditLog> SystemAuditLogs { get; set; }
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
    public DbSet<RateLimitCounter> RateLimitCounters { get; set; }
    public DbSet<VehicleDelegation> VehicleDelegations { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }

    public override int SaveChanges()
    {
        if (_isWritingAudit)
            return base.SaveChanges();

        var auditCandidates = BuildAuditCandidates();
        var syncCandidates = BuildSyncCandidates();
        if (auditCandidates.Count == 0 && syncCandidates.Count == 0)
            return base.SaveChanges();

        try
        {
            _isWritingAudit = true;
            var result = base.SaveChanges();

            foreach (var candidate in auditCandidates)
            {
                candidate.Log.IsSuccess = true;
                candidate.Log.FailureReason = null;
                candidate.Log.EntityId = BuildEntityId(candidate.Entry);
            }

            OutboxEvents.AddRange(syncCandidates.Select(x => x.OutboxEvent));
            SystemAuditLogs.AddRange(auditCandidates.Select(x => x.Log));
            base.SaveChanges();
            return result;
        }
        catch (Exception ex)
        {
            try
            {
                foreach (var candidate in auditCandidates)
                {
                    candidate.Log.IsSuccess = false;
                    candidate.Log.FailureReason = ex.Message;
                    candidate.Log.EntityId = BuildEntityId(candidate.Entry);
                }
                SystemAuditLogs.AddRange(auditCandidates.Select(x => x.Log));
                base.SaveChanges();
            }
            catch
            {
            }
            throw;
        }
        finally
        {
            _isWritingAudit = false;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isWritingAudit)
            return await base.SaveChangesAsync(cancellationToken);

        var auditCandidates = BuildAuditCandidates();
        var syncCandidates = BuildSyncCandidates();
        if (auditCandidates.Count == 0 && syncCandidates.Count == 0)
            return await base.SaveChangesAsync(cancellationToken);

        try
        {
            _isWritingAudit = true;
            var result = await base.SaveChangesAsync(cancellationToken);

            foreach (var candidate in auditCandidates)
            {
                candidate.Log.IsSuccess = true;
                candidate.Log.FailureReason = null;
                candidate.Log.EntityId = BuildEntityId(candidate.Entry);
            }

            OutboxEvents.AddRange(syncCandidates.Select(x => x.OutboxEvent));
            SystemAuditLogs.AddRange(auditCandidates.Select(x => x.Log));
            await base.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            try
            {
                foreach (var candidate in auditCandidates)
                {
                    candidate.Log.IsSuccess = false;
                    candidate.Log.FailureReason = ex.Message;
                    candidate.Log.EntityId = BuildEntityId(candidate.Entry);
                }
                SystemAuditLogs.AddRange(auditCandidates.Select(x => x.Log));
                await base.SaveChangesAsync(cancellationToken);
            }
            catch
            {
            }
            throw;
        }
        finally
        {
            _isWritingAudit = false;
        }
    }

    private List<(EntityEntry Entry, SystemAuditLog Log)> BuildAuditCandidates()
    {
        var candidates = new List<(EntityEntry, SystemAuditLog)>();

        foreach (var entry in ChangeTracker.Entries().Where(e =>
                     e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                     e.Entity is not SystemAuditLog))
        {
            var action = entry.State switch
            {
                EntityState.Added => "CREATE",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "UNKNOWN"
            };

            var oldValues = entry.State == EntityState.Added ? null : SerializeValues(entry.OriginalValues);
            var newValues = entry.State == EntityState.Deleted ? null : SerializeValues(entry.CurrentValues);

            var log = new SystemAuditLog
            {
                TimestampUtc = DateTime.UtcNow,
                UserId = _currentUserContext?.UserId,
                Username = _currentUserContext?.Username,
                EventCategory = "DATA_CHANGE",
                Severity = "INFO",
                ActionType = action,
                EntityName = entry.Metadata.ClrType.Name,
                OldValuesJson = oldValues,
                NewValuesJson = newValues,
                IsSuccess = false
            };

            candidates.Add((entry, log));
        }

        return candidates;
    }

    private IReadOnlyList<PendingSyncEventCandidate> BuildSyncCandidates()
    {
        if (_syncExecutionContext?.SuppressOutboxPublishing == true)
        {
            return [];
        }

        return _syncEntityEventFactory?.BuildCandidates(ChangeTracker) ?? [];
    }

    private static string SerializeValues(PropertyValues values)
    {
        var dict = values.Properties.ToDictionary(p => p.Name, p => values[p.Name]);
        return JsonSerializer.Serialize(dict);
    }

    private static string? BuildEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key == null) return null;
        var parts = new List<string>();
        foreach (var k in key.Properties)
        {
            var value = entry.Property(k.Name).CurrentValue ?? entry.Property(k.Name).OriginalValue;
            parts.Add($"{k.Name}:{value}");
        }
        return string.Join("|", parts);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Access_L__5E548648F597543A");

            entity.Property(e => e.IsBypass).HasDefaultValue(false);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Camera).WithMany(p => p.AccessLogs).HasConstraintName("FK_AccessLog_Camera");

            entity.HasOne(d => d.Employee).WithMany(p => p.AccessLogs).HasConstraintName("FK_AccessLog_Employee");

            entity.HasOne(d => d.EntryLog).WithMany(p => p.InverseEntryLog).HasConstraintName("FK_AccessLog_EntryLog");

            entity.HasOne(d => d.ExceptionReason).WithMany(p => p.AccessLogs).HasConstraintName("FK_AccessLog_ExceptionReason");

            entity.HasOne(d => d.Gate).WithMany(p => p.AccessLogs).HasConstraintName("FK_AccessLog_Gate");

            entity.HasOne(d => d.Registration).WithMany(p => p.AccessLogs).HasConstraintName("FK_AccessLog_PreRegistration");
        });

        modelBuilder.Entity<Camera>(entity =>
        {
            entity.HasKey(e => e.CameraId).HasName("PK__Camera__F971E0C89B981B26");

            entity.HasOne(d => d.Gate).WithMany(p => p.Cameras).HasConstraintName("FK_Camera_Gate");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED22FBEE13");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F1101CCCAF2");

            entity.Property(e => e.Status).HasDefaultValue(true);

            entity.HasOne(d => d.Department).WithMany(p => p.Employees).HasConstraintName("FK_Employee_Department");

            entity.HasOne(d => d.Position).WithMany(p => p.Employees).HasConstraintName("FK_Employee_Position");
        });

        modelBuilder.Entity<ExceptionReason>(entity =>
        {
            entity.HasKey(e => e.ReasonId).HasName("PK__Exceptio__A4F8C0E71D19C4D0");
        });

        modelBuilder.Entity<Gate>(entity =>
        {
            entity.HasKey(e => e.GateId).HasName("PK__Gate__9582C65020BEEB76");
            entity.Property(e => e.Latitude).HasColumnType("decimal(18,12)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(18,12)");
        });

        modelBuilder.Entity<GuestProfile>(entity =>
        {
            entity.HasKey(e => e.GuestId).HasName("PK__GuestPro__0C423C12B547B8BB");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__Position__60BB9A79F338CF82");
        });

        modelBuilder.Entity<PreRegistration>(entity =>
        {
            entity.HasKey(e => e.RegistrationId).HasName("PK__Pre_Regi__6EF58810D0B7AD86");

            entity.Property(e => e.Status).HasDefaultValue("PENDING");

            entity.HasOne(d => d.Guest).WithMany(p => p.PreRegistrations).HasConstraintName("FK_PreReg_Guest");

            entity.HasOne(d => d.HostEmployee).WithMany(p => p.PreRegistrations).HasConstraintName("FK_PreReg_Employee");
            entity.HasMany(e => e.VisitorDetails)
          .WithOne(v => v.Registration)
          .HasForeignKey(v => v.RegistrationId)
          .OnDelete(DeleteBehavior.Cascade)
          .HasConstraintName("FK_VisitorDetail_PreRegistration");
            entity.Property(e => e.NumberOfVisitors).HasDefaultValue(1);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });
        modelBuilder.Entity<RegistrationLink>(entity =>
{
    entity.HasKey(e => e.LinkId);
    entity.HasIndex(e => e.Token).IsUnique();
    entity.Property(e => e.IsUsed).HasDefaultValue(false);
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

    entity.HasOne(e => e.HostEmployee)
          .WithMany()
          .HasForeignKey(e => e.HostEmployeeId)
          .OnDelete(DeleteBehavior.Restrict)
          .HasConstraintName("FK_RegistrationLink_Employee");
});

        // THÊM: config VisitorDetail
        modelBuilder.Entity<VisitorDetail>(entity =>
        {
            entity.HasKey(e => e.VisitorDetailId);

            entity.HasOne(e => e.Registration)
                  .WithMany(r => r.VisitorDetails)
                  .HasForeignKey(e => e.RegistrationId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_VisitorDetail_PreRegistration");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__Vehicle__476B54920FBE48B7");

            entity.HasOne(d => d.Employee).WithMany(p => p.Vehicles).HasConstraintName("FK_Vehicle_Employee");

            entity.HasOne(d => d.VehicleType).WithMany(p => p.Vehicles).HasConstraintName("FK_Vehicle_Type");
            entity.Property(e => e.ParkingStatus)
          .HasMaxLength(10)
          .HasDefaultValue("OUT");
        });

        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.HasKey(e => e.VehicleTypeId).HasName("PK__VehicleT__9F449643A4120859");
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Role).HasDefaultValue("LeTan");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.TokenVersion).HasDefaultValue(0);
            entity.Property(e => e.MfaEnabled).HasDefaultValue(false);

            // Quan hệ 1-1: một AppUser gắn với tối đa một Employee
            entity.HasOne(u => u.Employee)
                  .WithOne(e => e.AppUser)
                  .HasForeignKey<AppUser>(u => u.EmployeeId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_AppUser_Employee");
        });

        modelBuilder.Entity<UserRefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ExpiresAtUtc });
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_UserRefreshTokens_AppUser");
        });

        modelBuilder.Entity<SystemAuditLog>(entity =>
        {
            entity.Property(e => e.EventCategory).HasDefaultValue("APPLICATION");
            entity.Property(e => e.Severity).HasDefaultValue("INFO");
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => new { e.EventCategory, e.TimestampUtc });
        });

        modelBuilder.Entity<EmployeeFaceVideo>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                  .HasMaxLength(255);

            entity.Property(e => e.FilePath)
                  .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("(getdate())");

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_EmployeeFaceVideo_Employee");
        });
        modelBuilder.Entity<FaceEnrollmentJob>(entity =>
        {
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.Property(e => e.QualityScore).HasPrecision(9, 6);
            entity.Property(e => e.DuplicateDistance).HasPrecision(9, 6);
            entity.HasIndex(e => e.EmployeeId)
                .IsUnique()
                .HasFilter("[Status] IN ('Pending','Processing','Prepared','Activating','RecoveryRequired')")
                .HasDatabaseName("UX_FaceEnrollmentJobs_NonTerminalEmployee");
            entity.HasIndex(e => new { e.Status, e.CreatedAtUtc })
                .HasDatabaseName("IX_FaceEnrollmentJobs_Status_CreatedAtUtc");
            entity.HasOne(e => e.Employee).WithMany()
                .HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.EmployeeFaceVideo).WithMany(e => e.EnrollmentJobs)
                .HasForeignKey(e => e.EmployeeFaceVideoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RequestedByUser).WithMany()
                .HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FaceRecognitionEvent>(entity =>
        {
            entity.HasIndex(e => e.RuntimeEventId).IsUnique();
            entity.HasIndex(e => e.OccurredAtUtc);
            entity.HasIndex(e => new { e.EmployeeId, e.OccurredAtUtc });
            entity.HasIndex(e => new { e.CameraId, e.OccurredAtUtc });
            entity.HasIndex(e => new { e.FaceCameraConfigurationId, e.OccurredAtUtc });
            entity.HasIndex(e => e.MatchStatus);
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.EmployeeFaceModel).WithMany().HasForeignKey(e => e.EmployeeFaceModelId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.FaceCameraConfiguration).WithMany()
                .HasForeignKey(e => e.FaceCameraConfigurationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Lane).WithMany().HasForeignKey(e => e.LaneId)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<FaceRecognitionCollectorCheckpoint>(entity =>
        {
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });
        modelBuilder.Entity<EmployeeFaceModel>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ModelFileName)
                  .HasMaxLength(255);

            entity.Property(e => e.ModelPath)
                  .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.ModelChecksum).HasMaxLength(64);
            entity.Property(e => e.FailureCode).HasMaxLength(80);
            entity.Property(e => e.FailureMessage).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();

            entity.HasIndex(e => e.ModelFileName)
                  .IsUnique()
                  .HasDatabaseName("UX_EmployeeFaceModels_ModelFileName");
            entity.HasIndex(e => new { e.EmployeeId, e.Version })
                  .IsUnique()
                  .HasFilter("[Version] IS NOT NULL")
                  .HasDatabaseName("UX_EmployeeFaceModels_EmployeeId_Version");
            entity.HasIndex(e => e.EmployeeId)
                  .HasDatabaseName("IX_EmployeeFaceModels_EmployeeId");
            entity.HasIndex(e => new { e.EmployeeId, e.Status })
                  .IsUnique()
                  .HasFilter("[Status] = 'Active'")
                  .HasDatabaseName("UX_EmployeeFaceModels_ActiveEmployee");
            entity.HasIndex(e => e.SourceEnrollmentJobId)
                  .IsUnique()
                  .HasFilter("[SourceEnrollmentJobId] IS NOT NULL")
                  .HasDatabaseName("UX_EmployeeFaceModels_SourceEnrollmentJobId");

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_EmployeeFaceModel_Employee");
            entity.HasOne(e => e.SourceEnrollmentJob)
                  .WithOne(e => e.ResultModel)
                  .HasForeignKey<EmployeeFaceModel>(e => e.SourceEnrollmentJobId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED22FBEE13");

            entity.HasData(
                new Department { DepartmentId = 1, Name = "Phòng Kỹ thuật" },
                new Department { DepartmentId = 2, Name = "Phòng Nhân sự" },
                new Department { DepartmentId = 3, Name = "Phòng Bảo vệ" }
            );
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__Position__60BB9A79F338CF82");

            entity.HasData(
                new Position { PositionId = 1, Name = "Nhân viên" },
                new Position { PositionId = 2, Name = "Trưởng nhóm" },
                new Position { PositionId = 3, Name = "Bảo vệ" }
            );
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F1101CCCAF2");

            entity.Property(e => e.Status).HasDefaultValue(true);

            entity.HasOne(d => d.Department)
                  .WithMany(p => p.Employees)
                  .HasConstraintName("FK_Employee_Department");

            entity.HasOne(d => d.Position)
                  .WithMany(p => p.Employees)
                  .HasConstraintName("FK_Employee_Position");

            entity.HasData(
                new Employee
                {
                    EmployeeId = 1,
                    DepartmentId = 1,
                    PositionId = 1,
                    FullName = "Phạm Ngọc Hoài Anh",
                    Phone = "0900000001",
                    Email = "a@company.local",
                    FaceImageUrl = "/images/employees/a.jpg",
                    Status = true
                },
                new Employee
                {
                    EmployeeId = 2,
                    DepartmentId = 1,
                    PositionId = 2,
                    FullName = "Phạm Văn Thành",
                    Phone = "0900000002",
                    Email = "b@company.local",
                    FaceImageUrl = "/images/employees/b.jpg",
                    Status = true
                },
                new Employee
                {
                    EmployeeId = 3,
                    DepartmentId = 2,
                    PositionId = 1,
                    FullName = "Hà Mạnh Hùng",
                    Phone = "0900000003",
                    Email = "c@company.local",
                    FaceImageUrl = "/images/employees/c.jpg",
                    Status = true
                },
                new Employee
                {
                    EmployeeId = 4,
                    DepartmentId = 3,
                    PositionId = 3,
                    FullName = "Vũ Tiến Đạt",
                    Phone = "0900000004",
                    Email = "d@company.local",
                    FaceImageUrl = "/images/employees/d.jpg",
                    Status = true
                },
                new Employee
                {
                    EmployeeId = 5,
                    DepartmentId = 3,
                    PositionId = 3,
                    FullName = "Nguyễn Quốc Việt",
                    Phone = "0900000005",
                    Email = "e@company.local",
                    FaceImageUrl = "/images/employees/e.jpg",
                    Status = true
                }
            );
        });
        modelBuilder.Entity<EmployeeDynamicQr>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EmployeeId).IsUnique();

            entity.Property(e => e.SecretKey)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(e => e.TimeStepSeconds)
                  .HasDefaultValue(30);

            entity.Property(e => e.Digits)
                  .HasDefaultValue(6);

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("(getutcdate())");

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_EmployeeDynamicQr_Employee");
        });

        modelBuilder.Entity<DynamicQrScanLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.QrPayload)
                  .HasMaxLength(500)
                  .IsRequired();

            entity.Property(e => e.Message)
                  .HasMaxLength(500);

            entity.Property(e => e.ScannerDevice)
                  .HasMaxLength(200);

            entity.Property(e => e.ScannedAt)
                  .HasDefaultValueSql("(getutcdate())");
        });
        OnModelCreatingPartial(modelBuilder);
        ConfigureChatModels(modelBuilder);
        ConfigureNotificationModels(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
