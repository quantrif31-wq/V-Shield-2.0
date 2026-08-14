using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoteFaceEnrollmentJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemoteFaceEnrollmentJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    AssignedNodeId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResultModelFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResultChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ResultEncodingCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteFaceEnrollmentJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemoteFaceEnrollmentJobs_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemoteFaceEnrollmentFrames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    ImageData = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteFaceEnrollmentFrames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemoteFaceEnrollmentFrames_RemoteFaceEnrollmentJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "RemoteFaceEnrollmentJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemoteFaceEnrollmentFrames_JobId",
                table: "RemoteFaceEnrollmentFrames",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_RemoteFaceEnrollmentJobs_EmployeeId",
                table: "RemoteFaceEnrollmentJobs",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemoteFaceEnrollmentFrames");

            migrationBuilder.DropTable(
                name: "RemoteFaceEnrollmentJobs");
        }
    }
}
