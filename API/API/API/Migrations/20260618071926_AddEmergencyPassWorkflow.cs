using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyPassWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmergencyPasses",
                columns: table => new
                {
                    EmergencyPassId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SubjectName = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LaneReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LaneName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlarmId = table.Column<long>(type: "bigint", nullable: true),
                    LaneEventId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyPasses", x => x.EmergencyPassId);
                    table.ForeignKey(
                        name: "FK_EmergencyPasses_AppUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyPasses_ApprovedByUserId",
                table: "EmergencyPasses",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyPasses_CorrelationId",
                table: "EmergencyPasses",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyPasses_Status_ValidToUtc",
                table: "EmergencyPasses",
                columns: new[] { "Status", "ValidToUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyPasses");
        }
    }
}
