using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaNodeSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxEvents_Status_NextAttemptAtUtc_CreatedAtUtc",
                table: "OutboxEvents");

            migrationBuilder.AddColumn<string>(
                name: "AreaNodeId",
                table: "OutboxEvents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "OutboxEvents",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "OutboxEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCanonical",
                table: "OutboxEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAtUtc",
                table: "OutboxEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "SchemaVersion",
                table: "OutboxEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScopeId",
                table: "OutboxEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeType",
                table: "OutboxEvents",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "OutboxEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "OutboxEvents",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SyncAreaNodes",
                columns: table => new
                {
                    AreaNodeId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NodeSecretHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Mode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncAreaNodes", x => x.AreaNodeId);
                });

            migrationBuilder.CreateTable(
                name: "SyncInboundEvents",
                columns: table => new
                {
                    SyncInboundEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AreaNodeId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    ScopeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ScopeId = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AggregateId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    AppliedAggregateId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncInboundEvents", x => x.SyncInboundEventId);
                });

            migrationBuilder.CreateTable(
                name: "SyncOutboundCheckpoints",
                columns: table => new
                {
                    AreaNodeId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LastDeliveredOutboxEventId = table.Column<long>(type: "bigint", nullable: false),
                    LastAcknowledgedOutboxEventId = table.Column<long>(type: "bigint", nullable: false),
                    LastPulledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOutboundCheckpoints", x => x.AreaNodeId);
                });

            migrationBuilder.CreateTable(
                name: "SyncAreaAssignments",
                columns: table => new
                {
                    SyncAreaAssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AreaNodeId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ScopeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncAreaAssignments", x => x.SyncAreaAssignmentId);
                    table.ForeignKey(
                        name: "FK_SyncAreaAssignments_SyncAreaNodes_AreaNodeId",
                        column: x => x.AreaNodeId,
                        principalTable: "SyncAreaNodes",
                        principalColumn: "AreaNodeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Channel_AreaNodeId_CorrelationId",
                table: "OutboxEvents",
                columns: new[] { "Channel", "AreaNodeId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Channel_SiteId_ScopeType_ScopeId_OutboxEventId",
                table: "OutboxEvents",
                columns: new[] { "Channel", "SiteId", "ScopeType", "ScopeId", "OutboxEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Channel_Status_NextAttemptAtUtc_CreatedAtUtc",
                table: "OutboxEvents",
                columns: new[] { "Channel", "Status", "NextAttemptAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncAreaAssignments_AreaNodeId_ScopeType_ScopeId",
                table: "SyncAreaAssignments",
                columns: new[] { "AreaNodeId", "ScopeType", "ScopeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncInboundEvents_AreaNodeId_AggregateType_AggregateId",
                table: "SyncInboundEvents",
                columns: new[] { "AreaNodeId", "AggregateType", "AggregateId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncInboundEvents_AreaNodeId_CorrelationId",
                table: "SyncInboundEvents",
                columns: new[] { "AreaNodeId", "CorrelationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncAreaAssignments");

            migrationBuilder.DropTable(
                name: "SyncInboundEvents");

            migrationBuilder.DropTable(
                name: "SyncOutboundCheckpoints");

            migrationBuilder.DropTable(
                name: "SyncAreaNodes");

            migrationBuilder.DropIndex(
                name: "IX_OutboxEvents_Channel_AreaNodeId_CorrelationId",
                table: "OutboxEvents");

            migrationBuilder.DropIndex(
                name: "IX_OutboxEvents_Channel_SiteId_ScopeType_ScopeId_OutboxEventId",
                table: "OutboxEvents");

            migrationBuilder.DropIndex(
                name: "IX_OutboxEvents_Channel_Status_NextAttemptAtUtc_CreatedAtUtc",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "AreaNodeId",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "IsCanonical",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "OccurredAtUtc",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "ScopeId",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "OutboxEvents");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status_NextAttemptAtUtc_CreatedAtUtc",
                table: "OutboxEvents",
                columns: new[] { "Status", "NextAttemptAtUtc", "CreatedAtUtc" });
        }
    }
}
