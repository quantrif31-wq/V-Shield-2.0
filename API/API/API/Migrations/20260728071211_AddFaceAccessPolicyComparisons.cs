using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceAccessPolicyComparisons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaceAccessPolicyComparisons",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FaceRecognitionEventId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    CameraId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FaceCameraConfigurationId = table.Column<int>(type: "int", nullable: true),
                    LaneId = table.Column<int>(type: "int", nullable: true),
                    GateId = table.Column<int>(type: "int", nullable: true),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LegacyDecision = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    LegacyReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LegacyPermissionId = table.Column<int>(type: "int", nullable: true),
                    EnterpriseDecision = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    EnterpriseReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EnterprisePolicyVersionId = table.Column<int>(type: "int", nullable: true),
                    EnterpriseRuleId = table.Column<int>(type: "int", nullable: true),
                    EnterpriseScheduleId = table.Column<int>(type: "int", nullable: true),
                    ComparisonResult = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    MappingStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EvaluationVersion = table.Column<int>(type: "int", nullable: false),
                    LegacyInputFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EnterpriseInputFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScheduleTimeZoneId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceAccessPolicyComparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceAccessPolicyComparisons_FaceRecognitionEvents_FaceRecognitionEventId",
                        column: x => x.FaceRecognitionEventId,
                        principalTable: "FaceRecognitionEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessPolicyComparisons_AccessPointId",
                table: "FaceAccessPolicyComparisons",
                column: "AccessPointId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessPolicyComparisons_ComparisonResult",
                table: "FaceAccessPolicyComparisons",
                column: "ComparisonResult");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessPolicyComparisons_EmployeeId",
                table: "FaceAccessPolicyComparisons",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessPolicyComparisons_EnterpriseDecision",
                table: "FaceAccessPolicyComparisons",
                column: "EnterpriseDecision");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessPolicyComparisons_FaceRecognitionEventId",
                table: "FaceAccessPolicyComparisons",
                column: "FaceRecognitionEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessPolicyComparisons_GateId",
                table: "FaceAccessPolicyComparisons",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessPolicyComparisons_LegacyDecision",
                table: "FaceAccessPolicyComparisons",
                column: "LegacyDecision");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAccessPolicyComparisons_OccurredAtUtc",
                table: "FaceAccessPolicyComparisons",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceAccessPolicyComparisons");
        }
    }
}
