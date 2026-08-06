using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceEnrollmentJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeFaceVideo_Employee",
                table: "EmployeeFaceVideos");

            migrationBuilder.CreateTable(
                name: "FaceEnrollmentJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeFaceVideoId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreparedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivationRequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CandidateReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CandidateChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CandidateEncodingCount = table.Column<int>(type: "int", nullable: true),
                    TotalInputFrames = table.Column<int>(type: "int", nullable: true),
                    ProcessedFrameCount = table.Column<int>(type: "int", nullable: true),
                    UsableFrameCount = table.Column<int>(type: "int", nullable: true),
                    NoFaceFrameCount = table.Column<int>(type: "int", nullable: true),
                    MultipleFaceFrameCount = table.Column<int>(type: "int", nullable: true),
                    InvalidFrameCount = table.Column<int>(type: "int", nullable: true),
                    QualityScore = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: true),
                    DuplicateSubjectId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DuplicateDistance = table.Column<double>(type: "float(9)", precision: 9, scale: 6, nullable: true),
                    TargetModelVersion = table.Column<int>(type: "int", nullable: true),
                    ExpectedModelFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceEnrollmentJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceEnrollmentJobs_AppUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaceEnrollmentJobs_EmployeeFaceVideos_EmployeeFaceVideoId",
                        column: x => x.EmployeeFaceVideoId,
                        principalTable: "EmployeeFaceVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaceEnrollmentJobs_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceEnrollmentJobs_EmployeeFaceVideoId",
                table: "FaceEnrollmentJobs",
                column: "EmployeeFaceVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceEnrollmentJobs_RequestedByUserId",
                table: "FaceEnrollmentJobs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceEnrollmentJobs_Status_CreatedAtUtc",
                table: "FaceEnrollmentJobs",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_FaceEnrollmentJobs_NonTerminalEmployee",
                table: "FaceEnrollmentJobs",
                column: "EmployeeId",
                unique: true,
                filter: "[Status] IN ('Pending','Processing','Prepared','Activating','RecoveryRequired')");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeFaceModels_FaceEnrollmentJobs_SourceEnrollmentJobId",
                table: "EmployeeFaceModels",
                column: "SourceEnrollmentJobId",
                principalTable: "FaceEnrollmentJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeFaceVideo_Employee",
                table: "EmployeeFaceVideos",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeFaceModels_FaceEnrollmentJobs_SourceEnrollmentJobId",
                table: "EmployeeFaceModels");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeFaceVideo_Employee",
                table: "EmployeeFaceVideos");

            migrationBuilder.DropTable(
                name: "FaceEnrollmentJobs");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeFaceVideo_Employee",
                table: "EmployeeFaceVideos",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
