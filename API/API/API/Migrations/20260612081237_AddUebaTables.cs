using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddUebaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UEBAAnomalies",
                columns: table => new
                {
                    AnomalyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AccessLogId = table.Column<int>(type: "int", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    EventTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnomalyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SupportingData = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    Resolution = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolvedBy = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UEBAAnomalies", x => x.AnomalyId);
                    table.ForeignKey(
                        name: "FK_UEBAAnomaly_AccessLog",
                        column: x => x.AccessLogId,
                        principalTable: "Access_Log",
                        principalColumn: "LogId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UEBAAnomaly_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UEBAAnomaly_ResolvedByUser",
                        column: x => x.ResolvedBy,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UEBAProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TotalAccessCount = table.Column<int>(type: "int", nullable: false),
                    DaysSinceLastAccess = table.Column<int>(type: "int", nullable: false),
                    AvgAccessPerDay = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    TypicalStartHour = table.Column<int>(type: "int", nullable: false),
                    TypicalEndHour = table.Column<int>(type: "int", nullable: false),
                    WeekendAccessRatio = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    InOutRatio = table.Column<double>(type: "float", nullable: false),
                    BypassRate = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    LastBuiltAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    CommonGatesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnusualHoursJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UEBAProfiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_UEBAProfile_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UEBAAnomalies_AccessLogId",
                table: "UEBAAnomalies",
                column: "AccessLogId");

            migrationBuilder.CreateIndex(
                name: "IX_UEBAAnomalies_EmployeeId_Status",
                table: "UEBAAnomalies",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UEBAAnomalies_EventTimestamp_AnomalyType",
                table: "UEBAAnomalies",
                columns: new[] { "EventTimestamp", "AnomalyType" });

            migrationBuilder.CreateIndex(
                name: "IX_UEBAAnomalies_ResolvedBy",
                table: "UEBAAnomalies",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UEBAProfiles_EmployeeId",
                table: "UEBAProfiles",
                column: "EmployeeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UEBAAnomalies");

            migrationBuilder.DropTable(
                name: "UEBAProfiles");
        }
    }
}
