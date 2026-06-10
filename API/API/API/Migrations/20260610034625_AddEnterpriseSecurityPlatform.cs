using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseSecurityPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LifecycleStatus",
                table: "Employee",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTime>(
                name: "LifecycleUpdatedAtUtc",
                table: "Employee",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagerEmployeeId",
                table: "Employee",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimarySiteId",
                table: "Employee",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessDecisions",
                columns: table => new
                {
                    AccessDecisionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    CredentialType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvaluatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessDecisions", x => x.AccessDecisionId);
                });

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
                name: "AccessPolicyVersions",
                columns: table => new
                {
                    AccessPolicyVersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessPolicyVersions", x => x.AccessPolicyVersionId);
                    table.ForeignKey(
                        name: "FK_AccessPolicyVersions_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
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
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceItems", x => x.EvidenceItemId);
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
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "AccessRules",
                columns: table => new
                {
                    AccessRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 1,
                columns: new[] { "LifecycleStatus", "LifecycleUpdatedAtUtc", "ManagerEmployeeId", "PrimarySiteId" },
                values: new object[] { "Active", null, null, null });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 2,
                columns: new[] { "LifecycleStatus", "LifecycleUpdatedAtUtc", "ManagerEmployeeId", "PrimarySiteId" },
                values: new object[] { "Active", null, null, null });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 3,
                columns: new[] { "LifecycleStatus", "LifecycleUpdatedAtUtc", "ManagerEmployeeId", "PrimarySiteId" },
                values: new object[] { "Active", null, null, null });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 4,
                columns: new[] { "LifecycleStatus", "LifecycleUpdatedAtUtc", "ManagerEmployeeId", "PrimarySiteId" },
                values: new object[] { "Active", null, null, null });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 5,
                columns: new[] { "LifecycleStatus", "LifecycleUpdatedAtUtc", "ManagerEmployeeId", "PrimarySiteId" },
                values: new object[] { "Active", null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Employee_ManagerEmployeeId",
                table: "Employee",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_PrimarySiteId",
                table: "Employee",
                column: "PrimarySiteId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessControllerDevices_SecurityDeviceId",
                table: "AccessControllerDevices",
                column: "SecurityDeviceId",
                unique: true);

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
                name: "IX_AccessPolicyVersions_CreatedByUserId",
                table: "AccessPolicyVersions",
                column: "CreatedByUserId");

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
                name: "IX_RestoreDrills_BackupRunId",
                table: "RestoreDrills",
                column: "BackupRunId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionPolicies_EvidenceType_RetentionCategory_IsActive",
                table: "RetentionPolicies",
                columns: new[] { "EvidenceType", "RetentionCategory", "IsActive" });

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
                name: "IX_Sites_CompanyId_Code",
                table: "Sites",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

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
                name: "IX_VideoBookmarks_SecurityEventId_CameraId",
                table: "VideoBookmarks",
                columns: new[] { "SecurityEventId", "CameraId" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_Employee_Employee_ManagerEmployeeId",
                table: "Employee",
                column: "ManagerEmployeeId",
                principalTable: "Employee",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employee_Sites_PrimarySiteId",
                table: "Employee",
                column: "PrimarySiteId",
                principalTable: "Sites",
                principalColumn: "SiteId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employee_Employee_ManagerEmployeeId",
                table: "Employee");

            migrationBuilder.DropForeignKey(
                name: "FK_Employee_Sites_PrimarySiteId",
                table: "Employee");

            migrationBuilder.DropTable(
                name: "AccessDecisions");

            migrationBuilder.DropTable(
                name: "AccessPolicyVersions");

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
                name: "BarrierCommandAudits");

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
                name: "EmergencyMusterSnapshots");

            migrationBuilder.DropTable(
                name: "EmergencyStates");

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
                name: "LegalHolds");

            migrationBuilder.DropTable(
                name: "MapDevicePlacements");

            migrationBuilder.DropTable(
                name: "MusterPoints");

            migrationBuilder.DropTable(
                name: "OccupancySnapshots");

            migrationBuilder.DropTable(
                name: "OfflinePolicyPackages");

            migrationBuilder.DropTable(
                name: "ParkingPermits");

            migrationBuilder.DropTable(
                name: "ReaderDevices");

            migrationBuilder.DropTable(
                name: "RedactionRequests");

            migrationBuilder.DropTable(
                name: "RestoreDrills");

            migrationBuilder.DropTable(
                name: "RetentionPolicies");

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
                name: "TemporaryAccessGrants");

            migrationBuilder.DropTable(
                name: "VideoBookmarks");

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
                name: "AccessRecertificationCampaigns");

            migrationBuilder.DropTable(
                name: "AccessGroups");

            migrationBuilder.DropTable(
                name: "AccessLevels");

            migrationBuilder.DropTable(
                name: "AccessSchedules");

            migrationBuilder.DropTable(
                name: "Alarms");

            migrationBuilder.DropTable(
                name: "Barriers");

            migrationBuilder.DropTable(
                name: "EvidenceCollections");

            migrationBuilder.DropTable(
                name: "ExternalIdentityProviders");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "SiteMaps");

            migrationBuilder.DropTable(
                name: "ParkingAreas");

            migrationBuilder.DropTable(
                name: "AccessControllerDevices");

            migrationBuilder.DropTable(
                name: "EvidenceItems");

            migrationBuilder.DropTable(
                name: "BackupRuns");

            migrationBuilder.DropTable(
                name: "VisitorFormTemplates");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.DropTable(
                name: "WatchlistEntries");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions");

            migrationBuilder.DropTable(
                name: "Lanes");

            migrationBuilder.DropTable(
                name: "SecurityDevices");

            migrationBuilder.DropTable(
                name: "AccessPoints");

            migrationBuilder.DropTable(
                name: "SecurityZones");

            migrationBuilder.DropTable(
                name: "FacilityFloors");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Employee_ManagerEmployeeId",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_Employee_PrimarySiteId",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "LifecycleUpdatedAtUtc",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "ManagerEmployeeId",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "PrimarySiteId",
                table: "Employee");
        }
    }
}
