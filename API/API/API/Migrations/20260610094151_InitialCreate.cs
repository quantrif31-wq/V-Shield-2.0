using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessGroups",
                columns: table => new
                {
                    AccessGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroups", x => x.AccessGroupId);
                });

            migrationBuilder.CreateTable(
                name: "AccessLevels",
                columns: table => new
                {
                    AccessLevelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLevels", x => x.AccessLevelId);
                });

            migrationBuilder.CreateTable(
                name: "AccessSchedules",
                columns: table => new
                {
                    AccessScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DaysOfWeek = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessSchedules", x => x.AccessScheduleId);
                });

            migrationBuilder.CreateTable(
                name: "AiAdjudicationItems",
                columns: table => new
                {
                    AiAdjudicationItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityEventId = table.Column<long>(type: "bigint", nullable: true),
                    AiSource = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAdjudicationItems", x => x.AiAdjudicationItemId);
                });

            migrationBuilder.CreateTable(
                name: "AiPerformanceMetrics",
                columns: table => new
                {
                    AiPerformanceMetricId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiSource = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MetricValue = table.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPerformanceMetrics", x => x.AiPerformanceMetricId);
                });

            migrationBuilder.CreateTable(
                name: "AlarmRules",
                columns: table => new
                {
                    AlarmRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmRules", x => x.AlarmRuleId);
                });

            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    AlarmId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityEventId = table.Column<long>(type: "bigint", nullable: true),
                    AlarmType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    State = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.AlarmId);
                });

            migrationBuilder.CreateTable(
                name: "AntiPassbackStates",
                columns: table => new
                {
                    AntiPassbackStateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsViolated = table.Column<bool>(type: "bit", nullable: false),
                    ResetReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiPassbackStates", x => x.AntiPassbackStateId);
                });

            migrationBuilder.CreateTable(
                name: "BackupRuns",
                columns: table => new
                {
                    BackupRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Profile = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BackupReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    TargetRpoMinutes = table.Column<int>(type: "int", nullable: false),
                    TargetRtoMinutes = table.Column<int>(type: "int", nullable: false),
                    Verified = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupRuns", x => x.BackupRunId);
                });

            migrationBuilder.CreateTable(
                name: "CameraPlates",
                columns: table => new
                {
                    CameraIP = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    X1 = table.Column<int>(type: "int", nullable: false),
                    Y1 = table.Column<int>(type: "int", nullable: false),
                    X2 = table.Column<int>(type: "int", nullable: false),
                    Y2 = table.Column<int>(type: "int", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CameraPlates", x => x.CameraIP);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.CompanyId);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceReportRuns",
                columns: table => new
                {
                    ComplianceReportRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OutputReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceReportRuns", x => x.ComplianceReportRunId);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Departme__B2079BED22FBEE13", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "DispatchTasks",
                columns: table => new
                {
                    DispatchTaskId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmId = table.Column<long>(type: "bigint", nullable: true),
                    IncidentId = table.Column<long>(type: "bigint", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    LocationText = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AssignedGuardUserId = table.Column<int>(type: "int", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchTasks", x => x.DispatchTaskId);
                });

            migrationBuilder.CreateTable(
                name: "DynamicQrScanLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    QrPayload = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ScannerDevice = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicQrScanLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyMusterSnapshots",
                columns: table => new
                {
                    EmergencyMusterSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    MusterPointId = table.Column<int>(type: "int", nullable: true),
                    KnownOnsite = table.Column<int>(type: "int", nullable: false),
                    AccountedFor = table.Column<int>(type: "int", nullable: false),
                    VisitorsOnsite = table.Column<int>(type: "int", nullable: false),
                    UnaccountedFor = table.Column<int>(type: "int", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyMusterSnapshots", x => x.EmergencyMusterSnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyStates",
                columns: table => new
                {
                    EmergencyStateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    State = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyStates", x => x.EmergencyStateId);
                });

            migrationBuilder.CreateTable(
                name: "Employee_Access_Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    GateId = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee_Access_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventCorrelations",
                columns: table => new
                {
                    EventCorrelationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCorrelations", x => x.EventCorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceCollections",
                columns: table => new
                {
                    EvidenceCollectionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IncidentId = table.Column<long>(type: "bigint", nullable: true),
                    BundleHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceCollections", x => x.EvidenceCollectionId);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceExportRequests",
                columns: table => new
                {
                    EvidenceExportRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceItemId = table.Column<long>(type: "bigint", nullable: true),
                    EvidenceCollectionId = table.Column<long>(type: "bigint", nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExportHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Watermark = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    SignatureReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceExportRequests", x => x.EvidenceExportRequestId);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceItems",
                columns: table => new
                {
                    EvidenceItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    SecurityEventId = table.Column<long>(type: "bigint", nullable: true),
                    AlarmId = table.Column<long>(type: "bigint", nullable: true),
                    IncidentId = table.Column<long>(type: "bigint", nullable: true),
                    StorageReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HashSha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrivacyLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RetentionCategory = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    IsImmutable = table.Column<bool>(type: "bit", nullable: false),
                    IsLegalHold = table.Column<bool>(type: "bit", nullable: false),
                    LastHashVerificationStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "NotVerified"),
                    CurrentHashVerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurgedByUserId = table.Column<int>(type: "int", nullable: true),
                    PurgeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceItems", x => x.EvidenceItemId);
                });

            migrationBuilder.CreateTable(
                name: "Exception_Reason",
                columns: table => new
                {
                    ReasonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReasonCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Exceptio__A4F8C0E71D19C4D0", x => x.ReasonId);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIdentityProviders",
                columns: table => new
                {
                    ExternalIdentityProviderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Authority = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIdentityProviders", x => x.ExternalIdentityProviderId);
                });

            migrationBuilder.CreateTable(
                name: "Gate",
                columns: table => new
                {
                    GateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Gate__9582C65020BEEB76", x => x.GateId);
                });

            migrationBuilder.CreateTable(
                name: "GuestProfile",
                columns: table => new
                {
                    GuestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DefaultLicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FaceImageURL = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GuestPro__0C423C12B547B8BB", x => x.GuestId);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    IncidentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrimaryAlarmId = table.Column<long>(type: "bigint", nullable: true),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.IncidentId);
                });

            migrationBuilder.CreateTable(
                name: "LegalHolds",
                columns: table => new
                {
                    LegalHoldId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceItemId = table.Column<long>(type: "bigint", nullable: true),
                    EvidenceCollectionId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AppliedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReleasedByUserId = table.Column<int>(type: "int", nullable: true),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalHolds", x => x.LegalHoldId);
                });

            migrationBuilder.CreateTable(
                name: "OccupancySnapshots",
                columns: table => new
                {
                    OccupancySnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    Count = table.Column<int>(type: "int", nullable: false),
                    MaxAllowed = table.Column<int>(type: "int", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccupancySnapshots", x => x.OccupancySnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    OutboxEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AggregateId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.OutboxEventId);
                });

            migrationBuilder.CreateTable(
                name: "Position",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Position__60BB9A79F338CF82", x => x.PositionId);
                });

            migrationBuilder.CreateTable(
                name: "QaTestRuns",
                columns: table => new
                {
                    QaTestRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Profile = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PassedCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaTestRuns", x => x.QaTestRunId);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseCandidates",
                columns: table => new
                {
                    ReleaseCandidateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MigrationId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    BuildReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseCandidates", x => x.ReleaseCandidateId);
                });

            migrationBuilder.CreateTable(
                name: "RetentionPolicies",
                columns: table => new
                {
                    RetentionPolicyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RetentionCategory = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    PurgeMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionPolicies", x => x.RetentionPolicyId);
                });

            migrationBuilder.CreateTable(
                name: "RunbookAcknowledgements",
                columns: table => new
                {
                    RunbookAcknowledgementId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunbookName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AcknowledgedByUserId = table.Column<int>(type: "int", nullable: true),
                    EvidenceReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunbookAcknowledgements", x => x.RunbookAcknowledgementId);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeDependencyHealths",
                columns: table => new
                {
                    RuntimeDependencyHealthId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DependencyName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DependencyType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeDependencyHealths", x => x.RuntimeDependencyHealthId);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    SecurityEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    PlateText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SiteNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    SecurityZoneNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    AccessPointNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.SecurityEventId);
                });

            migrationBuilder.CreateTable(
                name: "SecurityOperationsChecks",
                columns: table => new
                {
                    SecurityOperationsCheckId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityOperationsChecks", x => x.SecurityOperationsCheckId);
                });

            migrationBuilder.CreateTable(
                name: "ShiftHandovers",
                columns: table => new
                {
                    ShiftHandoverId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    FromUserId = table.Column<int>(type: "int", nullable: true),
                    ToUserId = table.Column<int>(type: "int", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftHandovers", x => x.ShiftHandoverId);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    ShiftId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    BreakMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AllowedLateMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AllowedEarlyLeaveMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.ShiftId);
                });

            migrationBuilder.CreateTable(
                name: "SiteMaps",
                columns: table => new
                {
                    SiteMapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AssetReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CoordinateSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteMaps", x => x.SiteMapId);
                });

            migrationBuilder.CreateTable(
                name: "SopExecutions",
                columns: table => new
                {
                    SopExecutionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmId = table.Column<long>(type: "bigint", nullable: true),
                    IncidentId = table.Column<long>(type: "bigint", nullable: true),
                    SopTemplateId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CompletedStepsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExecutedByUserId = table.Column<int>(type: "int", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SopExecutions", x => x.SopExecutionId);
                });

            migrationBuilder.CreateTable(
                name: "SopTemplates",
                columns: table => new
                {
                    SopTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AlarmType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ChecklistJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SopTemplates", x => x.SopTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "SystemAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EventCategory = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, defaultValue: "APPLICATION"),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "INFO"),
                    ClientIp = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleType",
                columns: table => new
                {
                    VehicleTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VehicleT__9F449643A4120859", x => x.VehicleTypeId);
                });

            migrationBuilder.CreateTable(
                name: "VideoBookmarks",
                columns: table => new
                {
                    VideoBookmarkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityEventId = table.Column<long>(type: "bigint", nullable: true),
                    CameraId = table.Column<int>(type: "int", nullable: true),
                    ArtifactReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoBookmarks", x => x.VideoBookmarkId);
                });

            migrationBuilder.CreateTable(
                name: "Visitor_Access_Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitorDetailId = table.Column<int>(type: "int", nullable: false),
                    GateId = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitor_Access_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitorFormTemplates",
                columns: table => new
                {
                    VisitorFormTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FormType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorFormTemplates", x => x.VisitorFormTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "WatchlistEntries",
                columns: table => new
                {
                    WatchlistEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistEntries", x => x.WatchlistEntryId);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                columns: table => new
                {
                    WebhookSubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SecretReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    EventTypes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.WebhookSubscriptionId);
                });

            migrationBuilder.CreateTable(
                name: "AlarmComments",
                columns: table => new
                {
                    AlarmCommentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmComments", x => x.AlarmCommentId);
                    table.ForeignKey(
                        name: "FK_AlarmComments_Alarms_AlarmId",
                        column: x => x.AlarmId,
                        principalTable: "Alarms",
                        principalColumn: "AlarmId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestoreDrills",
                columns: table => new
                {
                    RestoreDrillId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BackupRunId = table.Column<long>(type: "bigint", nullable: true),
                    Profile = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeasuredRpoMinutes = table.Column<int>(type: "int", nullable: true),
                    MeasuredRtoMinutes = table.Column<int>(type: "int", nullable: true),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestoreDrills", x => x.RestoreDrillId);
                    table.ForeignKey(
                        name: "FK_RestoreDrills_BackupRuns_BackupRunId",
                        column: x => x.BackupRunId,
                        principalTable: "BackupRuns",
                        principalColumn: "BackupRunId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    SiteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.SiteId);
                    table.ForeignKey(
                        name: "FK_Sites_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChainOfCustodyEntries",
                columns: table => new
                {
                    ChainOfCustodyEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceItemId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    FromCustodian = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ToCustodian = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    HashBefore = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    HashAfter = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainOfCustodyEntries", x => x.ChainOfCustodyEntryId);
                    table.ForeignKey(
                        name: "FK_ChainOfCustodyEntries_EvidenceItems_EvidenceItemId",
                        column: x => x.EvidenceItemId,
                        principalTable: "EvidenceItems",
                        principalColumn: "EvidenceItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceAccessLogs",
                columns: table => new
                {
                    EvidenceAccessLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceItemId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    AccessType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceAccessLogs", x => x.EvidenceAccessLogId);
                    table.ForeignKey(
                        name: "FK_EvidenceAccessLogs_EvidenceItems_EvidenceItemId",
                        column: x => x.EvidenceItemId,
                        principalTable: "EvidenceItems",
                        principalColumn: "EvidenceItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceCollectionItems",
                columns: table => new
                {
                    EvidenceCollectionItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceCollectionId = table.Column<long>(type: "bigint", nullable: false),
                    EvidenceItemId = table.Column<long>(type: "bigint", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceCollectionItems", x => x.EvidenceCollectionItemId);
                    table.ForeignKey(
                        name: "FK_EvidenceCollectionItems_EvidenceCollections_EvidenceCollectionId",
                        column: x => x.EvidenceCollectionId,
                        principalTable: "EvidenceCollections",
                        principalColumn: "EvidenceCollectionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvidenceCollectionItems_EvidenceItems_EvidenceItemId",
                        column: x => x.EvidenceItemId,
                        principalTable: "EvidenceItems",
                        principalColumn: "EvidenceItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RedactionRequests",
                columns: table => new
                {
                    RedactionRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceItemId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PrivacyLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    PerformedByUserId = table.Column<int>(type: "int", nullable: true),
                    VerifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PerformedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedactedStorageReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedactionRequests", x => x.RedactionRequestId);
                    table.ForeignKey(
                        name: "FK_RedactionRequests_EvidenceItems_EvidenceItemId",
                        column: x => x.EvidenceItemId,
                        principalTable: "EvidenceItems",
                        principalColumn: "EvidenceItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Camera",
                columns: table => new
                {
                    CameraId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CameraName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GateId = table.Column<int>(type: "int", nullable: true),
                    CameraType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    StreamUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UrlView = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Camera__F971E0C89B981B26", x => x.CameraId);
                    table.ForeignKey(
                        name: "FK_Camera_Gate",
                        column: x => x.GateId,
                        principalTable: "Gate",
                        principalColumn: "GateId");
                });

            migrationBuilder.CreateTable(
                name: "IncidentTimelineItems",
                columns: table => new
                {
                    IncidentTimelineItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentId = table.Column<long>(type: "bigint", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTimelineItems", x => x.IncidentTimelineItemId);
                    table.ForeignKey(
                        name: "FK_IncidentTimelineItems_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "IncidentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseGateChecks",
                columns: table => new
                {
                    ReleaseGateCheckId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReleaseCandidateId = table.Column<long>(type: "bigint", nullable: false),
                    GateName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VerifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseGateChecks", x => x.ReleaseGateCheckId);
                    table.ForeignKey(
                        name: "FK_ReleaseGateChecks_ReleaseCandidates_ReleaseCandidateId",
                        column: x => x.ReleaseCandidateId,
                        principalTable: "ReleaseCandidates",
                        principalColumn: "ReleaseCandidateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                columns: table => new
                {
                    WebhookDeliveryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebhookSubscriptionId = table.Column<int>(type: "int", nullable: false),
                    OutboxEventId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.WebhookDeliveryId);
                    table.ForeignKey(
                        name: "FK_WebhookDeliveries_OutboxEvents_OutboxEventId",
                        column: x => x.OutboxEventId,
                        principalTable: "OutboxEvents",
                        principalColumn: "OutboxEventId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WebhookDeliveries_WebhookSubscriptions_WebhookSubscriptionId",
                        column: x => x.WebhookSubscriptionId,
                        principalTable: "WebhookSubscriptions",
                        principalColumn: "WebhookSubscriptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessRecertificationCampaigns",
                columns: table => new
                {
                    AccessRecertificationCampaignId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRecertificationCampaigns", x => x.AccessRecertificationCampaignId);
                    table.ForeignKey(
                        name: "FK_AccessRecertificationCampaigns_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    BuildingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.BuildingId);
                    table.ForeignKey(
                        name: "FK_Buildings_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    PositionId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FaceImageURL = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    LifecycleStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Active"),
                    PrimarySiteId = table.Column<int>(type: "int", nullable: true),
                    ManagerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    LifecycleUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Employee__7AD04F1101CCCAF2", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employee_Department",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK_Employee_Employee_ManagerEmployeeId",
                        column: x => x.ManagerEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Position",
                        column: x => x.PositionId,
                        principalTable: "Position",
                        principalColumn: "PositionId");
                    table.ForeignKey(
                        name: "FK_Employee_Sites_PrimarySiteId",
                        column: x => x.PrimarySiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HolidayCalendars",
                columns: table => new
                {
                    HolidayCalendarId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    HolidayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayCalendars", x => x.HolidayCalendarId);
                    table.ForeignKey(
                        name: "FK_HolidayCalendars_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MusterPoints",
                columns: table => new
                {
                    MusterPointId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    LocationNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusterPoints", x => x.MusterPointId);
                    table.ForeignKey(
                        name: "FK_MusterPoints_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParkingAreas",
                columns: table => new
                {
                    ParkingAreaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingAreas", x => x.ParkingAreaId);
                    table.ForeignKey(
                        name: "FK_ParkingAreas_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FacilityFloors",
                columns: table => new
                {
                    FacilityFloorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityFloors", x => x.FacilityFloorId);
                    table.ForeignKey(
                        name: "FK_FacilityFloors_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "BuildingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Staff"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    TokenVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MfaEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MfaSecretProtected = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    MfaConfiguredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPasswordChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_AppUser_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDynamicQrs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SecretKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TimeStepSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    Digits = table.Column<int>(type: "int", nullable: false, defaultValue: 6),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastUsedCounter = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDynamicQrs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDynamicQr_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFaceModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ModelFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ModelPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeFaceModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFaceModel_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFaceVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeFaceVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFaceVideo_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pre_Registration",
                columns: table => new
                {
                    RegistrationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuestId = table.Column<int>(type: "int", nullable: true),
                    HostEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ExpectedTimeIn = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExpectedTimeOut = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "PENDING"),
                    NumberOfVisitors = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Pre_Regi__6EF58810D0B7AD86", x => x.RegistrationId);
                    table.ForeignKey(
                        name: "FK_PreReg_Employee",
                        column: x => x.HostEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_PreReg_Guest",
                        column: x => x.GuestId,
                        principalTable: "GuestProfile",
                        principalColumn: "GuestId");
                });

            migrationBuilder.CreateTable(
                name: "Registration_Links",
                columns: table => new
                {
                    LinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    HostEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registration_Links", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_RegistrationLink_Employee",
                        column: x => x.HostEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    VisitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    HostEmployeeId = table.Column<int>(type: "int", nullable: true),
                    VisitorName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    VisitorType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VisitorPhone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    VisitorEmail = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExpectedInUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedOutUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EscortRequired = table.Column<bool>(type: "bit", nullable: false),
                    NdaRequired = table.Column<bool>(type: "bit", nullable: false),
                    SafetyBriefingRequired = table.Column<bool>(type: "bit", nullable: false),
                    HostNotified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.VisitId);
                    table.ForeignKey(
                        name: "FK_Visits_Employee_HostEmployeeId",
                        column: x => x.HostEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Visits_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SecurityZones",
                columns: table => new
                {
                    SecurityZoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    BuildingId = table.Column<int>(type: "int", nullable: true),
                    FacilityFloorId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SecurityLevel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsRestricted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityZones", x => x.SecurityZoneId);
                    table.ForeignKey(
                        name: "FK_SecurityZones_FacilityFloors_FacilityFloorId",
                        column: x => x.FacilityFloorId,
                        principalTable: "FacilityFloors",
                        principalColumn: "FacilityFloorId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityZones_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessPolicyVersions",
                columns: table => new
                {
                    AccessPolicyVersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessPolicyVersions", x => x.AccessPolicyVersionId);
                    table.ForeignKey(
                        name: "FK_AccessPolicyVersions_AppUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessPolicyVersions_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AccessRecertificationDecisions",
                columns: table => new
                {
                    AccessRecertificationDecisionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessRecertificationCampaignId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ReviewerUserId = table.Column<int>(type: "int", nullable: true),
                    Decision = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRecertificationDecisions", x => x.AccessRecertificationDecisionId);
                    table.ForeignKey(
                        name: "FK_AccessRecertificationDecisions_AccessRecertificationCampaigns_AccessRecertificationCampaignId",
                        column: x => x.AccessRecertificationCampaignId,
                        principalTable: "AccessRecertificationCampaigns",
                        principalColumn: "AccessRecertificationCampaignId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessRecertificationDecisions_AppUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessRecertificationDecisions_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLifecycleEvents",
                columns: table => new
                {
                    EmployeeLifecycleEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PreviousState = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NewState = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLifecycleEvents", x => x.EmployeeLifecycleEventId);
                    table.ForeignKey(
                        name: "FK_EmployeeLifecycleEvents_AppUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmployeeLifecycleEvents_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIdentityMappings",
                columns: table => new
                {
                    ExternalIdentityMappingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalIdentityProviderId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    ExternalSubject = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    ExternalUsername = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIdentityMappings", x => x.ExternalIdentityMappingId);
                    table.ForeignKey(
                        name: "FK_ExternalIdentityMappings_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExternalIdentityMappings_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExternalIdentityMappings_ExternalIdentityProviders_ExternalIdentityProviderId",
                        column: x => x.ExternalIdentityProviderId,
                        principalTable: "ExternalIdentityProviders",
                        principalColumn: "ExternalIdentityProviderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ApproverId = table.Column<int>(type: "int", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.LeaveRequestId);
                    table.ForeignKey(
                        name: "FK_AttendanceModule_LeaveRequest_ApproverUser",
                        column: x => x.ApproverId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceModule_LeaveRequest_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MfaRecoveryCodes",
                columns: table => new
                {
                    MfaRecoveryCodeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaRecoveryCodes", x => x.MfaRecoveryCodeId);
                    table.ForeignKey(
                        name: "FK_MfaRecoveryCodes_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrivilegedActionSessions",
                columns: table => new
                {
                    PrivilegedActionSessionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ChallengeNonce = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivilegedActionSessions", x => x.PrivilegedActionSessionId);
                    table.ForeignKey(
                        name: "FK_PrivilegedActionSessions_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    JwtId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokens_AppUser",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Scheduled"),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_AttendanceModule_WorkSchedule_CreatedByUser",
                        column: x => x.CreatedBy,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceModule_WorkSchedule_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceModule_WorkSchedule_Shift",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visitor_Details",
                columns: table => new
                {
                    VisitorDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdCardNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExpectedFaceImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QrSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QrPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QrIssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsQrActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitor_Details", x => x.VisitorDetailId);
                    table.ForeignKey(
                        name: "FK_VisitorDetail_PreRegistration",
                        column: x => x.RegistrationId,
                        principalTable: "Pre_Registration",
                        principalColumn: "RegistrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorCheckIns",
                columns: table => new
                {
                    VisitorCheckInId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedOutAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedInByUserId = table.Column<int>(type: "int", nullable: true),
                    CheckedOutByUserId = table.Column<int>(type: "int", nullable: true),
                    IdDocumentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IdDocumentReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorCheckIns", x => x.VisitorCheckInId);
                    table.ForeignKey(
                        name: "FK_VisitorCheckIns_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorCredentials",
                columns: table => new
                {
                    VisitorCredentialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    CredentialType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CredentialReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorCredentials", x => x.VisitorCredentialId);
                    table.ForeignKey(
                        name: "FK_VisitorCredentials_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorFormAcceptances",
                columns: table => new
                {
                    VisitorFormAcceptanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    VisitorFormTemplateId = table.Column<int>(type: "int", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedByName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorFormAcceptances", x => x.VisitorFormAcceptanceId);
                    table.ForeignKey(
                        name: "FK_VisitorFormAcceptances_VisitorFormTemplates_VisitorFormTemplateId",
                        column: x => x.VisitorFormTemplateId,
                        principalTable: "VisitorFormTemplates",
                        principalColumn: "VisitorFormTemplateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitorFormAcceptances_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessPoints",
                columns: table => new
                {
                    AccessPointId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DirectionMode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessPoints", x => x.AccessPointId);
                    table.ForeignKey(
                        name: "FK_AccessPoints_SecurityZones_SecurityZoneId",
                        column: x => x.SecurityZoneId,
                        principalTable: "SecurityZones",
                        principalColumn: "SecurityZoneId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessPoints_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessDecisions",
                columns: table => new
                {
                    AccessDecisionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessPolicyVersionId = table.Column<int>(type: "int", nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    CredentialType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DecisionMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Enforced"),
                    LegacyResult = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ShadowMismatch = table.Column<bool>(type: "bit", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvaluatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessDecisions", x => x.AccessDecisionId);
                    table.ForeignKey(
                        name: "FK_AccessDecisions_AccessPolicyVersions_AccessPolicyVersionId",
                        column: x => x.AccessPolicyVersionId,
                        principalTable: "AccessPolicyVersions",
                        principalColumn: "AccessPolicyVersionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ScheduleId = table.Column<int>(type: "int", nullable: true),
                    WorkDate = table.Column<DateTime>(type: "date", nullable: false),
                    CheckIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyLeaveMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalWorkingHours = table.Column<decimal>(type: "decimal(8,2)", nullable: false, defaultValue: 0m),
                    OvertimeHours = table.Column<decimal>(type: "decimal(8,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "NotCheckedIn"),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Manual"),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.AttendanceId);
                    table.ForeignKey(
                        name: "FK_AttendanceModule_Attendance_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceModule_Attendance_WorkSchedule",
                        column: x => x.ScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "ScheduleId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Access_Log",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Direction = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    GateId = table.Column<int>(type: "int", nullable: true),
                    CameraId = table.Column<int>(type: "int", nullable: true),
                    CapturedLicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CapturedFaceImageURL = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    RegistrationId = table.Column<int>(type: "int", nullable: true),
                    ResultStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IsBypass = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ExceptionReasonId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SiteNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    SecurityZoneNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    AccessPointNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    LaneNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    GateNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    CameraNameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    EntryLogId = table.Column<int>(type: "int", nullable: true),
                    GuestId = table.Column<int>(type: "int", nullable: true),
                    VisitorDetailId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Access_L__5E548648F597543A", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_AccessLog_Camera",
                        column: x => x.CameraId,
                        principalTable: "Camera",
                        principalColumn: "CameraId");
                    table.ForeignKey(
                        name: "FK_AccessLog_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_AccessLog_EntryLog",
                        column: x => x.EntryLogId,
                        principalTable: "Access_Log",
                        principalColumn: "LogId");
                    table.ForeignKey(
                        name: "FK_AccessLog_ExceptionReason",
                        column: x => x.ExceptionReasonId,
                        principalTable: "Exception_Reason",
                        principalColumn: "ReasonId");
                    table.ForeignKey(
                        name: "FK_AccessLog_Gate",
                        column: x => x.GateId,
                        principalTable: "Gate",
                        principalColumn: "GateId");
                    table.ForeignKey(
                        name: "FK_AccessLog_PreRegistration",
                        column: x => x.RegistrationId,
                        principalTable: "Pre_Registration",
                        principalColumn: "RegistrationId");
                    table.ForeignKey(
                        name: "FK_Access_Log_Visitor_Details_VisitorDetailId",
                        column: x => x.VisitorDetailId,
                        principalTable: "Visitor_Details",
                        principalColumn: "VisitorDetailId");
                });

            migrationBuilder.CreateTable(
                name: "Vehicle",
                columns: table => new
                {
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ParkingStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "OUT"),
                    VisitorDetailId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Vehicle__476B54920FBE48B7", x => x.VehicleId);
                    table.ForeignKey(
                        name: "FK_Vehicle_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_Vehicle_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vehicle_Type",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleType",
                        principalColumn: "VehicleTypeId");
                    table.ForeignKey(
                        name: "FK_Vehicle_Visitor_Details_VisitorDetailId",
                        column: x => x.VisitorDetailId,
                        principalTable: "Visitor_Details",
                        principalColumn: "VisitorDetailId");
                });

            migrationBuilder.CreateTable(
                name: "AccessRules",
                columns: table => new
                {
                    AccessRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessPolicyVersionId = table.Column<int>(type: "int", nullable: true),
                    AccessLevelId = table.Column<int>(type: "int", nullable: false),
                    AccessGroupId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    AccessScheduleId = table.Column<int>(type: "int", nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    CredentialType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AllowAccess = table.Column<bool>(type: "bit", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRules", x => x.AccessRuleId);
                    table.ForeignKey(
                        name: "FK_AccessRules_AccessGroups_AccessGroupId",
                        column: x => x.AccessGroupId,
                        principalTable: "AccessGroups",
                        principalColumn: "AccessGroupId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessRules_AccessLevels_AccessLevelId",
                        column: x => x.AccessLevelId,
                        principalTable: "AccessLevels",
                        principalColumn: "AccessLevelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessRules_AccessPoints_AccessPointId",
                        column: x => x.AccessPointId,
                        principalTable: "AccessPoints",
                        principalColumn: "AccessPointId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessRules_AccessPolicyVersions_AccessPolicyVersionId",
                        column: x => x.AccessPolicyVersionId,
                        principalTable: "AccessPolicyVersions",
                        principalColumn: "AccessPolicyVersionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessRules_AccessSchedules_AccessScheduleId",
                        column: x => x.AccessScheduleId,
                        principalTable: "AccessSchedules",
                        principalColumn: "AccessScheduleId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessRules_SecurityZones_SecurityZoneId",
                        column: x => x.SecurityZoneId,
                        principalTable: "SecurityZones",
                        principalColumn: "SecurityZoneId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessRules_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Doors",
                columns: table => new
                {
                    DoorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessPointId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DoorMode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doors", x => x.DoorId);
                    table.ForeignKey(
                        name: "FK_Doors_AccessPoints_AccessPointId",
                        column: x => x.AccessPointId,
                        principalTable: "AccessPoints",
                        principalColumn: "AccessPointId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lanes",
                columns: table => new
                {
                    LaneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    GateId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lanes", x => x.LaneId);
                    table.ForeignKey(
                        name: "FK_Lanes_AccessPoints_AccessPointId",
                        column: x => x.AccessPointId,
                        principalTable: "AccessPoints",
                        principalColumn: "AccessPointId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Lanes_Gate_GateId",
                        column: x => x.GateId,
                        principalTable: "Gate",
                        principalColumn: "GateId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Lanes_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityDevices",
                columns: table => new
                {
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Vendor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ConfigurationVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityDevices", x => x.SecurityDeviceId);
                    table.ForeignKey(
                        name: "FK_SecurityDevices_AccessPoints_AccessPointId",
                        column: x => x.AccessPointId,
                        principalTable: "AccessPoints",
                        principalColumn: "AccessPointId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityDevices_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TemporaryAccessGrants",
                columns: table => new
                {
                    TemporaryAccessGrantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryAccessGrants", x => x.TemporaryAccessGrantId);
                    table.ForeignKey(
                        name: "FK_TemporaryAccessGrants_AccessPoints_AccessPointId",
                        column: x => x.AccessPointId,
                        principalTable: "AccessPoints",
                        principalColumn: "AccessPointId");
                    table.ForeignKey(
                        name: "FK_TemporaryAccessGrants_AppUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TemporaryAccessGrants_SecurityZones_SecurityZoneId",
                        column: x => x.SecurityZoneId,
                        principalTable: "SecurityZones",
                        principalColumn: "SecurityZoneId");
                    table.ForeignKey(
                        name: "FK_TemporaryAccessGrants_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId");
                });

            migrationBuilder.CreateTable(
                name: "ParkingPermits",
                columns: table => new
                {
                    ParkingPermitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParkingAreaId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    VisitId = table.Column<int>(type: "int", nullable: true),
                    PermitType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingPermits", x => x.ParkingPermitId);
                    table.ForeignKey(
                        name: "FK_ParkingPermits_ParkingAreas_ParkingAreaId",
                        column: x => x.ParkingAreaId,
                        principalTable: "ParkingAreas",
                        principalColumn: "ParkingAreaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingPermits_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParkingPermits_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WatchlistMatches",
                columns: table => new
                {
                    WatchlistMatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WatchlistEntryId = table.Column<int>(type: "int", nullable: false),
                    VisitId = table.Column<int>(type: "int", nullable: true),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistMatches", x => x.WatchlistMatchId);
                    table.ForeignKey(
                        name: "FK_WatchlistMatches_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WatchlistMatches_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WatchlistMatches_WatchlistEntries_WatchlistEntryId",
                        column: x => x.WatchlistEntryId,
                        principalTable: "WatchlistEntries",
                        principalColumn: "WatchlistEntryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Barriers",
                columns: table => new
                {
                    BarrierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaneId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    State = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barriers", x => x.BarrierId);
                    table.ForeignKey(
                        name: "FK_Barriers_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "LaneId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LaneEvents",
                columns: table => new
                {
                    LaneEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaneId = table.Column<int>(type: "int", nullable: true),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlateText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaneEvents", x => x.LaneEventId);
                    table.ForeignKey(
                        name: "FK_LaneEvents_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "LaneId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LaneEvents_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AccessControllerDevices",
                columns: table => new
                {
                    AccessControllerDeviceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SupportsOfflineDecision = table.Column<bool>(type: "bit", nullable: false),
                    MaxCredentials = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessControllerDevices", x => x.AccessControllerDeviceId);
                    table.ForeignKey(
                        name: "FK_AccessControllerDevices_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceConfigurationVersions",
                columns: table => new
                {
                    DeviceConfigurationVersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceConfigurationVersions", x => x.DeviceConfigurationVersionId);
                    table.ForeignKey(
                        name: "FK_DeviceConfigurationVersions_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCredentials",
                columns: table => new
                {
                    DeviceCredentialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: false),
                    CredentialType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CredentialReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RotatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCredentials", x => x.DeviceCredentialId);
                    table.ForeignKey(
                        name: "FK_DeviceCredentials_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceHealthSnapshots",
                columns: table => new
                {
                    DeviceHealthSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceHealthSnapshots", x => x.DeviceHealthSnapshotId);
                    table.ForeignKey(
                        name: "FK_DeviceHealthSnapshots_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceProvisioningRequests",
                columns: table => new
                {
                    DeviceProvisioningRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RequestedName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ApprovalNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceProvisioningRequests", x => x.DeviceProvisioningRequestId);
                    table.ForeignKey(
                        name: "FK_DeviceProvisioningRequests_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeviceRelays",
                columns: table => new
                {
                    DeviceRelayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    State = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceRelays", x => x.DeviceRelayId);
                    table.ForeignKey(
                        name: "FK_DeviceRelays_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceSensors",
                columns: table => new
                {
                    DeviceSensorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: false),
                    SensorType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    State = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsTamperSensor = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSensors", x => x.DeviceSensorId);
                    table.ForeignKey(
                        name: "FK_DeviceSensors_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapDevicePlacements",
                columns: table => new
                {
                    MapDevicePlacementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteMapId = table.Column<int>(type: "int", nullable: false),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: true),
                    CameraId = table.Column<int>(type: "int", nullable: true),
                    X = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    Y = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    IconType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapDevicePlacements", x => x.MapDevicePlacementId);
                    table.ForeignKey(
                        name: "FK_MapDevicePlacements_Camera_CameraId",
                        column: x => x.CameraId,
                        principalTable: "Camera",
                        principalColumn: "CameraId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MapDevicePlacements_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MapDevicePlacements_SiteMaps_SiteMapId",
                        column: x => x.SiteMapId,
                        principalTable: "SiteMaps",
                        principalColumn: "SiteMapId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfflinePolicyPackages",
                columns: table => new
                {
                    OfflinePolicyPackageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: true),
                    PackageVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflinePolicyPackages", x => x.OfflinePolicyPackageId);
                    table.ForeignKey(
                        name: "FK_OfflinePolicyPackages_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BarrierCommandAudits",
                columns: table => new
                {
                    BarrierCommandAuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarrierId = table.Column<int>(type: "int", nullable: false),
                    Command = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarrierCommandAudits", x => x.BarrierCommandAuditId);
                    table.ForeignKey(
                        name: "FK_BarrierCommandAudits_Barriers_BarrierId",
                        column: x => x.BarrierId,
                        principalTable: "Barriers",
                        principalColumn: "BarrierId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReaderDevices",
                columns: table => new
                {
                    ReaderDeviceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityDeviceId = table.Column<int>(type: "int", nullable: false),
                    AccessControllerDeviceId = table.Column<int>(type: "int", nullable: true),
                    ReaderProtocol = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CredentialFormats = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderDevices", x => x.ReaderDeviceId);
                    table.ForeignKey(
                        name: "FK_ReaderDevices_AccessControllerDevices_AccessControllerDeviceId",
                        column: x => x.AccessControllerDeviceId,
                        principalTable: "AccessControllerDevices",
                        principalColumn: "AccessControllerDeviceId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReaderDevices_SecurityDevices_SecurityDeviceId",
                        column: x => x.SecurityDeviceId,
                        principalTable: "SecurityDevices",
                        principalColumn: "SecurityDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Department",
                columns: new[] { "DepartmentId", "Name" },
                values: new object[,]
                {
                    { 1, "Phòng Kỹ thuật" },
                    { 2, "Phòng Nhân sự" },
                    { 3, "Phòng Bảo vệ" }
                });

            migrationBuilder.InsertData(
                table: "Position",
                columns: new[] { "PositionId", "Name" },
                values: new object[,]
                {
                    { 1, "Nhân viên" },
                    { 2, "Trưởng nhóm" },
                    { 3, "Bảo vệ" }
                });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "ShiftId", "AllowedEarlyLeaveMinutes", "AllowedLateMinutes", "BreakMinutes", "CreatedAt", "EndTime", "IsActive", "ShiftName", "StartTime", "UpdatedAt" },
                values: new object[] { 1, 5, 5, 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 17, 0, 0, 0), true, "Ca hành chính", new TimeSpan(0, 8, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "ShiftId", "AllowedEarlyLeaveMinutes", "AllowedLateMinutes", "CreatedAt", "EndTime", "IsActive", "ShiftName", "StartTime", "UpdatedAt" },
                values: new object[,]
                {
                    { 2, 5, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 11, 0, 0, 0), true, "Ca sáng", new TimeSpan(0, 7, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 5, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 17, 0, 0, 0), true, "Ca chiều", new TimeSpan(0, 13, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 5, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 22, 0, 0, 0), true, "Ca tối", new TimeSpan(0, 18, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Employee",
                columns: new[] { "EmployeeId", "DepartmentId", "Email", "FaceImageURL", "FullName", "LifecycleStatus", "LifecycleUpdatedAtUtc", "ManagerEmployeeId", "Phone", "PositionId", "PrimarySiteId", "Status" },
                values: new object[,]
                {
                    { 1, 1, "a@company.local", "/images/employees/a.jpg", "Phạm Ngọc Hoài Anh", "Active", null, null, "0900000001", 1, null, true },
                    { 2, 1, "b@company.local", "/images/employees/b.jpg", "Phạm Văn Thành", "Active", null, null, "0900000002", 2, null, true },
                    { 3, 2, "c@company.local", "/images/employees/c.jpg", "Hà Mạnh Hùng", "Active", null, null, "0900000003", 1, null, true },
                    { 4, 3, "d@company.local", "/images/employees/d.jpg", "Vũ Tiến Đạt", "Active", null, null, "0900000004", 3, null, true },
                    { 5, 3, "e@company.local", "/images/employees/e.jpg", "Nguyễn Quốc Việt", "Active", null, null, "0900000005", 3, null, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Access_Log_CameraId",
                table: "Access_Log",
                column: "CameraId");

            migrationBuilder.CreateIndex(
                name: "IX_Access_Log_EmployeeId",
                table: "Access_Log",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Access_Log_EntryLogId",
                table: "Access_Log",
                column: "EntryLogId");

            migrationBuilder.CreateIndex(
                name: "IX_Access_Log_ExceptionReasonId",
                table: "Access_Log",
                column: "ExceptionReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Access_Log_GateId",
                table: "Access_Log",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_Access_Log_RegistrationId",
                table: "Access_Log",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Access_Log_VisitorDetailId",
                table: "Access_Log",
                column: "VisitorDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessControllerDevices_SecurityDeviceId",
                table: "AccessControllerDevices",
                column: "SecurityDeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessDecisions_AccessPolicyVersionId",
                table: "AccessDecisions",
                column: "AccessPolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDecisions_SubjectType_SubjectId_EvaluatedAtUtc",
                table: "AccessDecisions",
                columns: new[] { "SubjectType", "SubjectId", "EvaluatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroups_Code",
                table: "AccessGroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessLevels_Code",
                table: "AccessLevels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessPoints_SecurityZoneId",
                table: "AccessPoints",
                column: "SecurityZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessPoints_SiteId_Name",
                table: "AccessPoints",
                columns: new[] { "SiteId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessPolicyVersions_ApprovedByUserId",
                table: "AccessPolicyVersions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessPolicyVersions_CreatedByUserId",
                table: "AccessPolicyVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessPolicyVersions_Status_CreatedAtUtc",
                table: "AccessPolicyVersions",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRecertificationCampaigns_SiteId",
                table: "AccessRecertificationCampaigns",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRecertificationDecisions_AccessRecertificationCampaignId_EmployeeId",
                table: "AccessRecertificationDecisions",
                columns: new[] { "AccessRecertificationCampaignId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessRecertificationDecisions_EmployeeId",
                table: "AccessRecertificationDecisions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRecertificationDecisions_ReviewerUserId",
                table: "AccessRecertificationDecisions",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_AccessGroupId",
                table: "AccessRules",
                column: "AccessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_AccessLevelId",
                table: "AccessRules",
                column: "AccessLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_AccessPointId",
                table: "AccessRules",
                column: "AccessPointId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_AccessPolicyVersionId",
                table: "AccessRules",
                column: "AccessPolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_AccessScheduleId",
                table: "AccessRules",
                column: "AccessScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_SecurityZoneId",
                table: "AccessRules",
                column: "SecurityZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_SiteId",
                table: "AccessRules",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_SubjectType_SubjectId_SiteId_SecurityZoneId_AccessPointId",
                table: "AccessRules",
                columns: new[] { "SubjectType", "SubjectId", "SiteId", "SecurityZoneId", "AccessPointId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiAdjudicationItems_AiSource_Status_CreatedAtUtc",
                table: "AiAdjudicationItems",
                columns: new[] { "AiSource", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiPerformanceMetrics_AiSource_MetricName_CapturedAtUtc",
                table: "AiPerformanceMetrics",
                columns: new[] { "AiSource", "MetricName", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmComments_AlarmId",
                table: "AlarmComments",
                column: "AlarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_State_Severity_CreatedAtUtc",
                table: "Alarms",
                columns: new[] { "State", "Severity", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AntiPassbackStates_SubjectType_SubjectId_SecurityZoneId",
                table: "AntiPassbackStates",
                columns: new[] { "SubjectType", "SubjectId", "SecurityZoneId" },
                unique: true,
                filter: "[SecurityZoneId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_EmployeeId",
                table: "AppUsers",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId_WorkDate_ScheduleId",
                table: "Attendances",
                columns: new[] { "EmployeeId", "WorkDate", "ScheduleId" },
                unique: true,
                filter: "[ScheduleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ScheduleId",
                table: "Attendances",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupRuns_Profile_StartedAtUtc",
                table: "BackupRuns",
                columns: new[] { "Profile", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BarrierCommandAudits_BarrierId",
                table: "BarrierCommandAudits",
                column: "BarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Barriers_LaneId",
                table: "Barriers",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_SiteId_Code",
                table: "Buildings",
                columns: new[] { "SiteId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Camera_GateId",
                table: "Camera",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_ChainOfCustodyEntries_EvidenceItemId",
                table: "ChainOfCustodyEntries",
                column: "EvidenceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                table: "Companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceReportRuns_ReportType_CreatedAtUtc",
                table: "ComplianceReportRuns",
                columns: new[] { "ReportType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConfigurationVersions_SecurityDeviceId",
                table: "DeviceConfigurationVersions",
                column: "SecurityDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCredentials_SecurityDeviceId_CredentialReference",
                table: "DeviceCredentials",
                columns: new[] { "SecurityDeviceId", "CredentialReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHealthSnapshots_SecurityDeviceId_CapturedAtUtc",
                table: "DeviceHealthSnapshots",
                columns: new[] { "SecurityDeviceId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceProvisioningRequests_SecurityDeviceId",
                table: "DeviceProvisioningRequests",
                column: "SecurityDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRelays_SecurityDeviceId",
                table: "DeviceRelays",
                column: "SecurityDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSensors_SecurityDeviceId",
                table: "DeviceSensors",
                column: "SecurityDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchTasks_Status_Priority_CreatedAtUtc",
                table: "DispatchTasks",
                columns: new[] { "Status", "Priority", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Doors_AccessPointId",
                table: "Doors",
                column: "AccessPointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyMusterSnapshots_SiteId_CapturedAtUtc",
                table: "EmergencyMusterSnapshots",
                columns: new[] { "SiteId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyStates_SiteId_SecurityZoneId_AccessPointId_IsActive",
                table: "EmergencyStates",
                columns: new[] { "SiteId", "SecurityZoneId", "AccessPointId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Employee_DepartmentId",
                table: "Employee",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_ManagerEmployeeId",
                table: "Employee",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_PositionId",
                table: "Employee",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_PrimarySiteId",
                table: "Employee",
                column: "PrimarySiteId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDynamicQrs_EmployeeId",
                table: "EmployeeDynamicQrs",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFaceModels_EmployeeId",
                table: "EmployeeFaceModels",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFaceVideos_EmployeeId",
                table: "EmployeeFaceVideos",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLifecycleEvents_ChangedByUserId",
                table: "EmployeeLifecycleEvents",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLifecycleEvents_EmployeeId_EffectiveAtUtc",
                table: "EmployeeLifecycleEvents",
                columns: new[] { "EmployeeId", "EffectiveAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventCorrelations_CorrelationId",
                table: "EventCorrelations",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceAccessLogs_EvidenceItemId_AccessedAtUtc",
                table: "EvidenceAccessLogs",
                columns: new[] { "EvidenceItemId", "AccessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceCollectionItems_EvidenceCollectionId_EvidenceItemId",
                table: "EvidenceCollectionItems",
                columns: new[] { "EvidenceCollectionId", "EvidenceItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceCollectionItems_EvidenceItemId",
                table: "EvidenceCollectionItems",
                column: "EvidenceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceExportRequests_Status_RequestedAtUtc",
                table: "EvidenceExportRequests",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceItems_EvidenceType_PrivacyLabel_CreatedAtUtc",
                table: "EvidenceItems",
                columns: new[] { "EvidenceType", "PrivacyLabel", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceItems_HashSha256",
                table: "EvidenceItems",
                column: "HashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceItems_RetentionCategory_PurgedAtUtc_IsLegalHold",
                table: "EvidenceItems",
                columns: new[] { "RetentionCategory", "PurgedAtUtc", "IsLegalHold" });

            migrationBuilder.CreateIndex(
                name: "UQ__Exceptio__A6278DA348D14177",
                table: "Exception_Reason",
                column: "ReasonCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityMappings_EmployeeId",
                table: "ExternalIdentityMappings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityMappings_ExternalIdentityProviderId_ExternalSubject",
                table: "ExternalIdentityMappings",
                columns: new[] { "ExternalIdentityProviderId", "ExternalSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityMappings_UserId",
                table: "ExternalIdentityMappings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityProviders_Name",
                table: "ExternalIdentityProviders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacilityFloors_BuildingId_Code",
                table: "FacilityFloors",
                columns: new[] { "BuildingId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendars_SiteId_HolidayDate",
                table: "HolidayCalendars",
                columns: new[] { "SiteId", "HolidayDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status_Severity_OpenedAtUtc",
                table: "Incidents",
                columns: new[] { "Status", "Severity", "OpenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTimelineItems_IncidentId",
                table: "IncidentTimelineItems",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_LaneEvents_LaneId_OccurredAtUtc",
                table: "LaneEvents",
                columns: new[] { "LaneId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LaneEvents_VehicleId",
                table: "LaneEvents",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_AccessPointId",
                table: "Lanes",
                column: "AccessPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_GateId",
                table: "Lanes",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_SiteId",
                table: "Lanes",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ApproverId",
                table: "LeaveRequests",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalHolds_Status_AppliedAtUtc",
                table: "LegalHolds",
                columns: new[] { "Status", "AppliedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MapDevicePlacements_CameraId",
                table: "MapDevicePlacements",
                column: "CameraId");

            migrationBuilder.CreateIndex(
                name: "IX_MapDevicePlacements_SecurityDeviceId",
                table: "MapDevicePlacements",
                column: "SecurityDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_MapDevicePlacements_SiteMapId",
                table: "MapDevicePlacements",
                column: "SiteMapId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaRecoveryCodes_CodeHash",
                table: "MfaRecoveryCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaRecoveryCodes_UserId_UsedAtUtc_ExpiresAtUtc",
                table: "MfaRecoveryCodes",
                columns: new[] { "UserId", "UsedAtUtc", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MusterPoints_SiteId",
                table: "MusterPoints",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancySnapshots_SiteId_SecurityZoneId_CapturedAtUtc",
                table: "OccupancySnapshots",
                columns: new[] { "SiteId", "SecurityZoneId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflinePolicyPackages_SecurityDeviceId",
                table: "OfflinePolicyPackages",
                column: "SecurityDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status_NextAttemptAtUtc_CreatedAtUtc",
                table: "OutboxEvents",
                columns: new[] { "Status", "NextAttemptAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingAreas_SiteId",
                table: "ParkingAreas",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingPermits_ParkingAreaId_ValidToUtc",
                table: "ParkingPermits",
                columns: new[] { "ParkingAreaId", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingPermits_VehicleId",
                table: "ParkingPermits",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingPermits_VisitId",
                table: "ParkingPermits",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_Pre_Registration_GuestId",
                table: "Pre_Registration",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Pre_Registration_HostEmployeeId",
                table: "Pre_Registration",
                column: "HostEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivilegedActionSessions_UserId_Action_Status_ExpiresAtUtc",
                table: "PrivilegedActionSessions",
                columns: new[] { "UserId", "Action", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_QaTestRuns_TestType_Status_StartedAtUtc",
                table: "QaTestRuns",
                columns: new[] { "TestType", "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReaderDevices_AccessControllerDeviceId",
                table: "ReaderDevices",
                column: "AccessControllerDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaderDevices_SecurityDeviceId",
                table: "ReaderDevices",
                column: "SecurityDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RedactionRequests_EvidenceItemId",
                table: "RedactionRequests",
                column: "EvidenceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_Links_HostEmployeeId",
                table: "Registration_Links",
                column: "HostEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_Links_Token",
                table: "Registration_Links",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseCandidates_Version_Status",
                table: "ReleaseCandidates",
                columns: new[] { "Version", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseGateChecks_ReleaseCandidateId_GateName",
                table: "ReleaseGateChecks",
                columns: new[] { "ReleaseCandidateId", "GateName" });

            migrationBuilder.CreateIndex(
                name: "IX_RestoreDrills_BackupRunId",
                table: "RestoreDrills",
                column: "BackupRunId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionPolicies_EvidenceType_RetentionCategory_IsActive",
                table: "RetentionPolicies",
                columns: new[] { "EvidenceType", "RetentionCategory", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RunbookAcknowledgements_RunbookName_RoleName_AcknowledgedAtUtc",
                table: "RunbookAcknowledgements",
                columns: new[] { "RunbookName", "RoleName", "AcknowledgedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeDependencyHealths_DependencyName_ObservedAtUtc",
                table: "RuntimeDependencyHealths",
                columns: new[] { "DependencyName", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityDevices_AccessPointId",
                table: "SecurityDevices",
                column: "AccessPointId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityDevices_SiteId_DeviceType_Name",
                table: "SecurityDevices",
                columns: new[] { "SiteId", "DeviceType", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CorrelationId_OccurredAtUtc",
                table: "SecurityEvents",
                columns: new[] { "CorrelationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_SiteId_SecurityZoneId_Severity_OccurredAtUtc",
                table: "SecurityEvents",
                columns: new[] { "SiteId", "SecurityZoneId", "Severity", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityZones_FacilityFloorId",
                table: "SecurityZones",
                column: "FacilityFloorId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityZones_SiteId_Code",
                table: "SecurityZones",
                columns: new[] { "SiteId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_ShiftName",
                table: "Shifts",
                column: "ShiftName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_CompanyId_Code",
                table: "Sites",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_CorrelationId",
                table: "SystemAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_EventCategory_TimestampUtc",
                table: "SystemAuditLogs",
                columns: new[] { "EventCategory", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAccessGrants_AccessPointId",
                table: "TemporaryAccessGrants",
                column: "AccessPointId");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAccessGrants_ApprovedByUserId",
                table: "TemporaryAccessGrants",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAccessGrants_SecurityZoneId",
                table: "TemporaryAccessGrants",
                column: "SecurityZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAccessGrants_SiteId",
                table: "TemporaryAccessGrants",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAccessGrants_SubjectType_SubjectId_ValidToUtc",
                table: "TemporaryAccessGrants",
                columns: new[] { "SubjectType", "SubjectId", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_TokenHash",
                table: "UserRefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId_ExpiresAtUtc",
                table: "UserRefreshTokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_EmployeeId",
                table: "Vehicle",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_SiteId",
                table: "Vehicle",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_VehicleTypeId",
                table: "Vehicle",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_VisitorDetailId",
                table: "Vehicle",
                column: "VisitorDetailId");

            migrationBuilder.CreateIndex(
                name: "UQ__Vehicle__026BC15CB8D416A0",
                table: "Vehicle",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoBookmarks_SecurityEventId_CameraId",
                table: "VideoBookmarks",
                columns: new[] { "SecurityEventId", "CameraId" });

            migrationBuilder.CreateIndex(
                name: "IX_Visitor_Details_RegistrationId",
                table: "Visitor_Details",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorCheckIns_VisitId",
                table: "VisitorCheckIns",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorCredentials_CredentialReference",
                table: "VisitorCredentials",
                column: "CredentialReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorCredentials_VisitId",
                table: "VisitorCredentials",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorFormAcceptances_VisitId",
                table: "VisitorFormAcceptances",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorFormAcceptances_VisitorFormTemplateId",
                table: "VisitorFormAcceptances",
                column: "VisitorFormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_HostEmployeeId",
                table: "Visits",
                column: "HostEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_SiteId_Status_ExpectedInUtc",
                table: "Visits",
                columns: new[] { "SiteId", "Status", "ExpectedInUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistEntries_EntityType_Identifier_IsActive",
                table: "WatchlistEntries",
                columns: new[] { "EntityType", "Identifier", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistMatches_VehicleId",
                table: "WatchlistMatches",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistMatches_VisitId",
                table: "WatchlistMatches",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistMatches_WatchlistEntryId",
                table: "WatchlistMatches",
                column: "WatchlistEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_OutboxEventId",
                table: "WebhookDeliveries",
                column: "OutboxEventId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_WebhookSubscriptionId",
                table: "WebhookDeliveries",
                column: "WebhookSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_CreatedBy",
                table: "WorkSchedules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_EmployeeId_ShiftId_WorkDate",
                table: "WorkSchedules",
                columns: new[] { "EmployeeId", "ShiftId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_ShiftId",
                table: "WorkSchedules",
                column: "ShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Access_Log");

            migrationBuilder.DropTable(
                name: "AccessDecisions");

            migrationBuilder.DropTable(
                name: "AccessRecertificationDecisions");

            migrationBuilder.DropTable(
                name: "AccessRules");

            migrationBuilder.DropTable(
                name: "AiAdjudicationItems");

            migrationBuilder.DropTable(
                name: "AiPerformanceMetrics");

            migrationBuilder.DropTable(
                name: "AlarmComments");

            migrationBuilder.DropTable(
                name: "AlarmRules");

            migrationBuilder.DropTable(
                name: "AntiPassbackStates");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "BarrierCommandAudits");

            migrationBuilder.DropTable(
                name: "CameraPlates");

            migrationBuilder.DropTable(
                name: "ChainOfCustodyEntries");

            migrationBuilder.DropTable(
                name: "ComplianceReportRuns");

            migrationBuilder.DropTable(
                name: "DeviceConfigurationVersions");

            migrationBuilder.DropTable(
                name: "DeviceCredentials");

            migrationBuilder.DropTable(
                name: "DeviceHealthSnapshots");

            migrationBuilder.DropTable(
                name: "DeviceProvisioningRequests");

            migrationBuilder.DropTable(
                name: "DeviceRelays");

            migrationBuilder.DropTable(
                name: "DeviceSensors");

            migrationBuilder.DropTable(
                name: "DispatchTasks");

            migrationBuilder.DropTable(
                name: "Doors");

            migrationBuilder.DropTable(
                name: "DynamicQrScanLogs");

            migrationBuilder.DropTable(
                name: "EmergencyMusterSnapshots");

            migrationBuilder.DropTable(
                name: "EmergencyStates");

            migrationBuilder.DropTable(
                name: "Employee_Access_Permissions");

            migrationBuilder.DropTable(
                name: "EmployeeDynamicQrs");

            migrationBuilder.DropTable(
                name: "EmployeeFaceModels");

            migrationBuilder.DropTable(
                name: "EmployeeFaceVideos");

            migrationBuilder.DropTable(
                name: "EmployeeLifecycleEvents");

            migrationBuilder.DropTable(
                name: "EventCorrelations");

            migrationBuilder.DropTable(
                name: "EvidenceAccessLogs");

            migrationBuilder.DropTable(
                name: "EvidenceCollectionItems");

            migrationBuilder.DropTable(
                name: "EvidenceExportRequests");

            migrationBuilder.DropTable(
                name: "ExternalIdentityMappings");

            migrationBuilder.DropTable(
                name: "HolidayCalendars");

            migrationBuilder.DropTable(
                name: "IncidentTimelineItems");

            migrationBuilder.DropTable(
                name: "LaneEvents");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LegalHolds");

            migrationBuilder.DropTable(
                name: "MapDevicePlacements");

            migrationBuilder.DropTable(
                name: "MfaRecoveryCodes");

            migrationBuilder.DropTable(
                name: "MusterPoints");

            migrationBuilder.DropTable(
                name: "OccupancySnapshots");

            migrationBuilder.DropTable(
                name: "OfflinePolicyPackages");

            migrationBuilder.DropTable(
                name: "ParkingPermits");

            migrationBuilder.DropTable(
                name: "PrivilegedActionSessions");

            migrationBuilder.DropTable(
                name: "QaTestRuns");

            migrationBuilder.DropTable(
                name: "ReaderDevices");

            migrationBuilder.DropTable(
                name: "RedactionRequests");

            migrationBuilder.DropTable(
                name: "Registration_Links");

            migrationBuilder.DropTable(
                name: "ReleaseGateChecks");

            migrationBuilder.DropTable(
                name: "RestoreDrills");

            migrationBuilder.DropTable(
                name: "RetentionPolicies");

            migrationBuilder.DropTable(
                name: "RunbookAcknowledgements");

            migrationBuilder.DropTable(
                name: "RuntimeDependencyHealths");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "SecurityOperationsChecks");

            migrationBuilder.DropTable(
                name: "ShiftHandovers");

            migrationBuilder.DropTable(
                name: "SopExecutions");

            migrationBuilder.DropTable(
                name: "SopTemplates");

            migrationBuilder.DropTable(
                name: "SystemAuditLogs");

            migrationBuilder.DropTable(
                name: "TemporaryAccessGrants");

            migrationBuilder.DropTable(
                name: "UserRefreshTokens");

            migrationBuilder.DropTable(
                name: "VideoBookmarks");

            migrationBuilder.DropTable(
                name: "Visitor_Access_Permissions");

            migrationBuilder.DropTable(
                name: "VisitorCheckIns");

            migrationBuilder.DropTable(
                name: "VisitorCredentials");

            migrationBuilder.DropTable(
                name: "VisitorFormAcceptances");

            migrationBuilder.DropTable(
                name: "WatchlistMatches");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries");

            migrationBuilder.DropTable(
                name: "Exception_Reason");

            migrationBuilder.DropTable(
                name: "AccessRecertificationCampaigns");

            migrationBuilder.DropTable(
                name: "AccessGroups");

            migrationBuilder.DropTable(
                name: "AccessLevels");

            migrationBuilder.DropTable(
                name: "AccessPolicyVersions");

            migrationBuilder.DropTable(
                name: "AccessSchedules");

            migrationBuilder.DropTable(
                name: "Alarms");

            migrationBuilder.DropTable(
                name: "WorkSchedules");

            migrationBuilder.DropTable(
                name: "Barriers");

            migrationBuilder.DropTable(
                name: "EvidenceCollections");

            migrationBuilder.DropTable(
                name: "ExternalIdentityProviders");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Camera");

            migrationBuilder.DropTable(
                name: "SiteMaps");

            migrationBuilder.DropTable(
                name: "ParkingAreas");

            migrationBuilder.DropTable(
                name: "AccessControllerDevices");

            migrationBuilder.DropTable(
                name: "EvidenceItems");

            migrationBuilder.DropTable(
                name: "ReleaseCandidates");

            migrationBuilder.DropTable(
                name: "BackupRuns");

            migrationBuilder.DropTable(
                name: "VisitorFormTemplates");

            migrationBuilder.DropTable(
                name: "Vehicle");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.DropTable(
                name: "WatchlistEntries");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Lanes");

            migrationBuilder.DropTable(
                name: "SecurityDevices");

            migrationBuilder.DropTable(
                name: "VehicleType");

            migrationBuilder.DropTable(
                name: "Visitor_Details");

            migrationBuilder.DropTable(
                name: "Gate");

            migrationBuilder.DropTable(
                name: "AccessPoints");

            migrationBuilder.DropTable(
                name: "Pre_Registration");

            migrationBuilder.DropTable(
                name: "SecurityZones");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "GuestProfile");

            migrationBuilder.DropTable(
                name: "FacilityFloors");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "Position");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
