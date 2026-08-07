using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddGuardZoneAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecurityZoneId",
                table: "OperationalInterventionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "OperationalInterventionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecurityZoneId",
                table: "EmergencyPasses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "EmergencyPasses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecurityZoneId",
                table: "DuressEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuardZoneAuthority",
                columns: table => new
                {
                    GuardZoneAuthorityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: false),
                    AuthorityLevel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CanOverride = table.Column<bool>(type: "bit", nullable: false),
                    CanManage = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrantedByUserId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardZoneAuthority", x => x.GuardZoneAuthorityId);
                    table.ForeignKey(
                        name: "FK_GuardZoneAuthority_GrantedByUser",
                        column: x => x.GrantedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GuardZoneAuthority_SecurityZone",
                        column: x => x.SecurityZoneId,
                        principalTable: "SecurityZones",
                        principalColumn: "SecurityZoneId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuardZoneAuthority_User",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalInterventionRequests_SecurityZoneId",
                table: "OperationalInterventionRequests",
                column: "SecurityZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalInterventionRequests_SiteId",
                table: "OperationalInterventionRequests",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyPasses_SecurityZoneId",
                table: "EmergencyPasses",
                column: "SecurityZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyPasses_SiteId",
                table: "EmergencyPasses",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_DuressEvents_SecurityZoneId",
                table: "DuressEvents",
                column: "SecurityZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DuressEvents_SiteId_OccurredAtUtc",
                table: "DuressEvents",
                columns: new[] { "SiteId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GuardZoneAuthority_GrantedByUserId",
                table: "GuardZoneAuthority",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuardZoneAuthority_SecurityZoneId",
                table: "GuardZoneAuthority",
                column: "SecurityZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_GuardZoneAuthority_UserId_SecurityZoneId",
                table: "GuardZoneAuthority",
                columns: new[] { "UserId", "SecurityZoneId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuardZoneAuthority_UserId_ValidTo",
                table: "GuardZoneAuthority",
                columns: new[] { "UserId", "ValidTo" });

            migrationBuilder.AddForeignKey(
                name: "FK_DuressEvents_SecurityZones_SecurityZoneId",
                table: "DuressEvents",
                column: "SecurityZoneId",
                principalTable: "SecurityZones",
                principalColumn: "SecurityZoneId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DuressEvents_Sites_SiteId",
                table: "DuressEvents",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "SiteId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyPasses_SecurityZones_SecurityZoneId",
                table: "EmergencyPasses",
                column: "SecurityZoneId",
                principalTable: "SecurityZones",
                principalColumn: "SecurityZoneId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyPasses_Sites_SiteId",
                table: "EmergencyPasses",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "SiteId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Intervention_SecurityZone",
                table: "OperationalInterventionRequests",
                column: "SecurityZoneId",
                principalTable: "SecurityZones",
                principalColumn: "SecurityZoneId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Intervention_Site",
                table: "OperationalInterventionRequests",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "SiteId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuressEvents_SecurityZones_SecurityZoneId",
                table: "DuressEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_DuressEvents_Sites_SiteId",
                table: "DuressEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_EmergencyPasses_SecurityZones_SecurityZoneId",
                table: "EmergencyPasses");

            migrationBuilder.DropForeignKey(
                name: "FK_EmergencyPasses_Sites_SiteId",
                table: "EmergencyPasses");

            migrationBuilder.DropForeignKey(
                name: "FK_Intervention_SecurityZone",
                table: "OperationalInterventionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Intervention_Site",
                table: "OperationalInterventionRequests");

            migrationBuilder.DropTable(
                name: "GuardZoneAuthority");

            migrationBuilder.DropIndex(
                name: "IX_OperationalInterventionRequests_SecurityZoneId",
                table: "OperationalInterventionRequests");

            migrationBuilder.DropIndex(
                name: "IX_OperationalInterventionRequests_SiteId",
                table: "OperationalInterventionRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmergencyPasses_SecurityZoneId",
                table: "EmergencyPasses");

            migrationBuilder.DropIndex(
                name: "IX_EmergencyPasses_SiteId",
                table: "EmergencyPasses");

            migrationBuilder.DropIndex(
                name: "IX_DuressEvents_SecurityZoneId",
                table: "DuressEvents");

            migrationBuilder.DropIndex(
                name: "IX_DuressEvents_SiteId_OccurredAtUtc",
                table: "DuressEvents");

            migrationBuilder.DropColumn(
                name: "SecurityZoneId",
                table: "OperationalInterventionRequests");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "OperationalInterventionRequests");

            migrationBuilder.DropColumn(
                name: "SecurityZoneId",
                table: "EmergencyPasses");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "EmergencyPasses");

            migrationBuilder.DropColumn(
                name: "SecurityZoneId",
                table: "DuressEvents");
        }
    }
}
