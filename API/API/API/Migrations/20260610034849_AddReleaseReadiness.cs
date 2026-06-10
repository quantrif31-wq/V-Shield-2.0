using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_QaTestRuns_TestType_Status_StartedAtUtc",
                table: "QaTestRuns",
                columns: new[] { "TestType", "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseCandidates_Version_Status",
                table: "ReleaseCandidates",
                columns: new[] { "Version", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseGateChecks_ReleaseCandidateId_GateName",
                table: "ReleaseGateChecks",
                columns: new[] { "ReleaseCandidateId", "GateName" });

            migrationBuilder.CreateIndex(
                name: "IX_RunbookAcknowledgements_RunbookName_RoleName_AcknowledgedAtUtc",
                table: "RunbookAcknowledgements",
                columns: new[] { "RunbookName", "RoleName", "AcknowledgedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QaTestRuns");

            migrationBuilder.DropTable(
                name: "ReleaseGateChecks");

            migrationBuilder.DropTable(
                name: "RunbookAcknowledgements");

            migrationBuilder.DropTable(
                name: "ReleaseCandidates");
        }
    }
}
