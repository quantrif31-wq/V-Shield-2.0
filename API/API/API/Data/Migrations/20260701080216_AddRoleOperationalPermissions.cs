using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleOperationalPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleOperationalPermissions",
                columns: table => new
                {
                    RoleOperationalPermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TaskKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleOperationalPermissions", x => x.RoleOperationalPermissionId);
                    table.ForeignKey(
                        name: "FK_RoleOperationalPermissions_AppUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleOperationalPermissions_Role_TaskKey",
                table: "RoleOperationalPermissions",
                columns: new[] { "Role", "TaskKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleOperationalPermissions_UpdatedByUserId",
                table: "RoleOperationalPermissions",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleOperationalPermissions");
        }
    }
}
