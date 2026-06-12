using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneTransitTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsZoneDerived",
                table: "Attendances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ZoneDwellTime",
                table: "Attendances",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ZoneTransitCount",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ZoneTransits",
                columns: table => new
                {
                    ZoneTransitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SecurityZoneId = table.Column<int>(type: "int", nullable: false),
                    AccessPointId = table.Column<int>(type: "int", nullable: true),
                    AccessLogId = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsAutoDerived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneTransits", x => x.ZoneTransitId);
                    table.ForeignKey(
                        name: "FK_ZoneTransit_AccessLog",
                        column: x => x.AccessLogId,
                        principalTable: "Access_Log",
                        principalColumn: "LogId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ZoneTransit_AccessPoint",
                        column: x => x.AccessPointId,
                        principalTable: "AccessPoints",
                        principalColumn: "AccessPointId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ZoneTransit_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZoneTransit_SecurityZone",
                        column: x => x.SecurityZoneId,
                        principalTable: "SecurityZones",
                        principalColumn: "SecurityZoneId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceZoneTransits",
                columns: table => new
                {
                    AttendancesAttendanceId = table.Column<int>(type: "int", nullable: false),
                    ZoneTransitsZoneTransitId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceZoneTransits", x => new { x.AttendancesAttendanceId, x.ZoneTransitsZoneTransitId });
                    table.ForeignKey(
                        name: "FK_AttendanceZoneTransits_Attendances_AttendancesAttendanceId",
                        column: x => x.AttendancesAttendanceId,
                        principalTable: "Attendances",
                        principalColumn: "AttendanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceZoneTransits_ZoneTransits_ZoneTransitsZoneTransitId",
                        column: x => x.ZoneTransitsZoneTransitId,
                        principalTable: "ZoneTransits",
                        principalColumn: "ZoneTransitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceZoneTransits_ZoneTransitsZoneTransitId",
                table: "AttendanceZoneTransits",
                column: "ZoneTransitsZoneTransitId");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneTransits_AccessLogId",
                table: "ZoneTransits",
                column: "AccessLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneTransits_AccessPointId",
                table: "ZoneTransits",
                column: "AccessPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneTransits_EmployeeId_Timestamp",
                table: "ZoneTransits",
                columns: new[] { "EmployeeId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ZoneTransits_SecurityZoneId",
                table: "ZoneTransits",
                column: "SecurityZoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceZoneTransits");

            migrationBuilder.DropTable(
                name: "ZoneTransits");

            migrationBuilder.DropColumn(
                name: "IsZoneDerived",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ZoneDwellTime",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ZoneTransitCount",
                table: "Attendances");
        }
    }
}
