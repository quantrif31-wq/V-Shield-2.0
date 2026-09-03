using System;
using API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260903153000_AddUserMonitoringPreferences")]
public partial class AddUserMonitoringPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserMonitoringPreferences",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false),
                SelectedCameraIdsJson = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "[]"),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserMonitoringPreferences", x => x.UserId);
                table.ForeignKey(
                    name: "FK_UserMonitoringPreferences_AppUsers",
                    column: x => x.UserId,
                    principalTable: "AppUsers",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserMonitoringPreferences");
    }
}
