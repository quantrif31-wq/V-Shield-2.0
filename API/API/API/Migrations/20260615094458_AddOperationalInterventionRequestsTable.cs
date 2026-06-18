using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalInterventionRequestsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalInterventionRequests",
                columns: table => new
                {
                    OperationalInterventionRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    LaneId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LaneName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    InterventionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    SubjectId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PlateNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    QrPayload = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByUserId = table.Column<int>(type: "int", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedByUserId = table.Column<int>(type: "int", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedByUserId = table.Column<int>(type: "int", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalInterventionRequests", x => x.OperationalInterventionRequestId);
                    table.ForeignKey(
                        name: "FK_Intervention_AcceptedByUser",
                        column: x => x.AcceptedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Intervention_ExecutedByUser",
                        column: x => x.ExecutedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Intervention_RejectedByUser",
                        column: x => x.RejectedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Intervention_RequestedByUser",
                        column: x => x.RequestedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalInterventionRequests_AcceptedByUserId",
                table: "OperationalInterventionRequests",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalInterventionRequests_ExecutedByUserId",
                table: "OperationalInterventionRequests",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalInterventionRequests_RejectedByUserId",
                table: "OperationalInterventionRequests",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalInterventionRequests_RequestedByUserId",
                table: "OperationalInterventionRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalInterventionRequests_Status_Priority_CreatedAtUtc",
                table: "OperationalInterventionRequests",
                columns: new[] { "Status", "Priority", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationalInterventionRequests");
        }
    }
}
