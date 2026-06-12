using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseSecurityRenovationControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Vehicle",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessPointNameSnapshot",
                table: "SecurityEvents",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityZoneNameSnapshot",
                table: "SecurityEvents",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteNameSnapshot",
                table: "SecurityEvents",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentHashVerifiedAtUtc",
                table: "EvidenceItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastHashVerificationStatus",
                table: "EvidenceItems",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "NotVerified");

            migrationBuilder.AddColumn<string>(
                name: "PurgeReason",
                table: "EvidenceItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurgedAtUtc",
                table: "EvidenceItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurgedByUserId",
                table: "EvidenceItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessPolicyVersionId",
                table: "AccessRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "AccessPolicyVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "AccessPolicyVersions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangeSummary",
                table: "AccessPolicyVersions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetiredAtUtc",
                table: "AccessPolicyVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "AccessPolicyVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessPolicyVersionId",
                table: "AccessDecisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionMode",
                table: "AccessDecisions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Enforced");

            migrationBuilder.AddColumn<string>(
                name: "LegacyResult",
                table: "AccessDecisions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShadowMismatch",
                table: "AccessDecisions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AccessPointNameSnapshot",
                table: "Access_Log",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CameraNameSnapshot",
                table: "Access_Log",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GateNameSnapshot",
                table: "Access_Log",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LaneNameSnapshot",
                table: "Access_Log",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityZoneNameSnapshot",
                table: "Access_Log",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteNameSnapshot",
                table: "Access_Log",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_SiteId",
                table: "Vehicle",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceItems_RetentionCategory_PurgedAtUtc_IsLegalHold",
                table: "EvidenceItems",
                columns: new[] { "RetentionCategory", "PurgedAtUtc", "IsLegalHold" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_AccessPolicyVersionId",
                table: "AccessRules",
                column: "AccessPolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessPolicyVersions_ApprovedByUserId",
                table: "AccessPolicyVersions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessPolicyVersions_Status_CreatedAtUtc",
                table: "AccessPolicyVersions",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessDecisions_AccessPolicyVersionId",
                table: "AccessDecisions",
                column: "AccessPolicyVersionId");

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
                name: "IX_PrivilegedActionSessions_UserId_Action_Status_ExpiresAtUtc",
                table: "PrivilegedActionSessions",
                columns: new[] { "UserId", "Action", "Status", "ExpiresAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_AccessDecisions_AccessPolicyVersions_AccessPolicyVersionId",
                table: "AccessDecisions",
                column: "AccessPolicyVersionId",
                principalTable: "AccessPolicyVersions",
                principalColumn: "AccessPolicyVersionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessPolicyVersions_AppUsers_ApprovedByUserId",
                table: "AccessPolicyVersions",
                column: "ApprovedByUserId",
                principalTable: "AppUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessRules_AccessPolicyVersions_AccessPolicyVersionId",
                table: "AccessRules",
                column: "AccessPolicyVersionId",
                principalTable: "AccessPolicyVersions",
                principalColumn: "AccessPolicyVersionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicle_Sites_SiteId",
                table: "Vehicle",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "SiteId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessDecisions_AccessPolicyVersions_AccessPolicyVersionId",
                table: "AccessDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_AccessPolicyVersions_AppUsers_ApprovedByUserId",
                table: "AccessPolicyVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_AccessRules_AccessPolicyVersions_AccessPolicyVersionId",
                table: "AccessRules");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicle_Sites_SiteId",
                table: "Vehicle");

            migrationBuilder.DropTable(
                name: "MfaRecoveryCodes");

            migrationBuilder.DropTable(
                name: "PrivilegedActionSessions");

            migrationBuilder.DropIndex(
                name: "IX_Vehicle_SiteId",
                table: "Vehicle");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceItems_RetentionCategory_PurgedAtUtc_IsLegalHold",
                table: "EvidenceItems");

            migrationBuilder.DropIndex(
                name: "IX_AccessRules_AccessPolicyVersionId",
                table: "AccessRules");

            migrationBuilder.DropIndex(
                name: "IX_AccessPolicyVersions_ApprovedByUserId",
                table: "AccessPolicyVersions");

            migrationBuilder.DropIndex(
                name: "IX_AccessPolicyVersions_Status_CreatedAtUtc",
                table: "AccessPolicyVersions");

            migrationBuilder.DropIndex(
                name: "IX_AccessDecisions_AccessPolicyVersionId",
                table: "AccessDecisions");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "AccessPointNameSnapshot",
                table: "SecurityEvents");

            migrationBuilder.DropColumn(
                name: "SecurityZoneNameSnapshot",
                table: "SecurityEvents");

            migrationBuilder.DropColumn(
                name: "SiteNameSnapshot",
                table: "SecurityEvents");

            migrationBuilder.DropColumn(
                name: "CurrentHashVerifiedAtUtc",
                table: "EvidenceItems");

            migrationBuilder.DropColumn(
                name: "LastHashVerificationStatus",
                table: "EvidenceItems");

            migrationBuilder.DropColumn(
                name: "PurgeReason",
                table: "EvidenceItems");

            migrationBuilder.DropColumn(
                name: "PurgedAtUtc",
                table: "EvidenceItems");

            migrationBuilder.DropColumn(
                name: "PurgedByUserId",
                table: "EvidenceItems");

            migrationBuilder.DropColumn(
                name: "AccessPolicyVersionId",
                table: "AccessRules");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "AccessPolicyVersions");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "AccessPolicyVersions");

            migrationBuilder.DropColumn(
                name: "ChangeSummary",
                table: "AccessPolicyVersions");

            migrationBuilder.DropColumn(
                name: "RetiredAtUtc",
                table: "AccessPolicyVersions");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "AccessPolicyVersions");

            migrationBuilder.DropColumn(
                name: "AccessPolicyVersionId",
                table: "AccessDecisions");

            migrationBuilder.DropColumn(
                name: "DecisionMode",
                table: "AccessDecisions");

            migrationBuilder.DropColumn(
                name: "LegacyResult",
                table: "AccessDecisions");

            migrationBuilder.DropColumn(
                name: "ShadowMismatch",
                table: "AccessDecisions");

            migrationBuilder.DropColumn(
                name: "AccessPointNameSnapshot",
                table: "Access_Log");

            migrationBuilder.DropColumn(
                name: "CameraNameSnapshot",
                table: "Access_Log");

            migrationBuilder.DropColumn(
                name: "GateNameSnapshot",
                table: "Access_Log");

            migrationBuilder.DropColumn(
                name: "LaneNameSnapshot",
                table: "Access_Log");

            migrationBuilder.DropColumn(
                name: "SecurityZoneNameSnapshot",
                table: "Access_Log");

            migrationBuilder.DropColumn(
                name: "SiteNameSnapshot",
                table: "Access_Log");
        }
    }
}
