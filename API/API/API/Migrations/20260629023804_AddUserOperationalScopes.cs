using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOperationalScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "AppUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "LeTan",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Staff");

            migrationBuilder.CreateTable(
                name: "UserOperationalScopes",
                columns: table => new
                {
                    UserOperationalScopeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TaskKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    GateId = table.Column<int>(type: "int", nullable: true),
                    LaneId = table.Column<int>(type: "int", nullable: true),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: true),
                    CanView = table.Column<bool>(type: "bit", nullable: false),
                    CanManage = table.Column<bool>(type: "bit", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOperationalScopes", x => x.UserOperationalScopeId);
                    table.ForeignKey(
                        name: "FK_UserOperationalScopes_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserOperationalScopes_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOperationalScopes_Gate_GateId",
                        column: x => x.GateId,
                        principalTable: "Gate",
                        principalColumn: "GateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOperationalScopes_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "LaneId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOperationalScopes_SecurityZones_SecurityZoneId",
                        column: x => x.SecurityZoneId,
                        principalTable: "SecurityZones",
                        principalColumn: "SecurityZoneId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOperationalScopes_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationalScopes_CreatedByUserId",
                table: "UserOperationalScopes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationalScopes_GateId",
                table: "UserOperationalScopes",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationalScopes_LaneId",
                table: "UserOperationalScopes",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationalScopes_SecurityZoneId",
                table: "UserOperationalScopes",
                column: "SecurityZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationalScopes_SiteId",
                table: "UserOperationalScopes",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationalScopes_UserId_SiteId_GateId_LaneId_SecurityZoneId",
                table: "UserOperationalScopes",
                columns: new[] { "UserId", "SiteId", "GateId", "LaneId", "SecurityZoneId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationalScopes_UserId_TaskKey",
                table: "UserOperationalScopes",
                columns: new[] { "UserId", "TaskKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOperationalScopes");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "AppUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Staff",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "LeTan");
        }
    }
}
