using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceAccessDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaceAccessDecisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FaceRecognitionEventId = table.Column<long>(type: "bigint", nullable: false),
                    FaceAccessPolicyComparisonId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    CameraId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LaneId = table.Column<int>(type: "int", nullable: true),
                    GateId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LegacyDecision = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    LegacyReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EnterpriseDecision = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    EnterpriseReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LegacyPermissionId = table.Column<int>(type: "int", nullable: true),
                    EnterprisePolicyVersionId = table.Column<int>(type: "int", nullable: true),
                    EnterpriseRuleId = table.Column<int>(type: "int", nullable: true),
                    EnterpriseScheduleId = table.Column<int>(type: "int", nullable: true),
                    EvaluationVersion = table.Column<int>(type: "int", nullable: false),
                    ScheduleTimeZoneId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InputFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PolicySnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceAccessDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceAccessDecisions_FaceAccessPolicyComparisons_FaceAccessPolicyComparisonId",
                        column: x => x.FaceAccessPolicyComparisonId,
                        principalTable: "FaceAccessPolicyComparisons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FaceAccessDecisions_FaceRecognitionEvents_FaceRecognitionEventId",
                        column: x => x.FaceRecognitionEventId,
                        principalTable: "FaceRecognitionEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessDecisions_AccessPointId",
                table: "FaceAccessDecisions",
                column: "AccessPointId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessDecisions_Decision",
                table: "FaceAccessDecisions",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessDecisions_EmployeeId",
                table: "FaceAccessDecisions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessDecisions_FaceAccessPolicyComparisonId",
                table: "FaceAccessDecisions",
                column: "FaceAccessPolicyComparisonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessDecisions_FaceRecognitionEventId",
                table: "FaceAccessDecisions",
                column: "FaceRecognitionEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessDecisions_GateId",
                table: "FaceAccessDecisions",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessDecisions_OccurredAtUtc",
                table: "FaceAccessDecisions",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceAccessDecisions");
        }
    }
}
