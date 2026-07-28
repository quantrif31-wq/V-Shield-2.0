using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceRecognitionEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaceRecognitionCollectorCheckpoints",
                columns: table => new
                {
                    CameraId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RuntimeSessionGeneration = table.Column<long>(type: "bigint", nullable: false),
                    LastSequence = table.Column<long>(type: "bigint", nullable: false),
                    LastEventOccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPollAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSuccessAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GapDetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceRecognitionCollectorCheckpoints", x => x.CameraId);
                });

            migrationBuilder.CreateTable(
                name: "FaceRecognitionEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuntimeEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CameraId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FaceCameraConfigurationId = table.Column<int>(type: "int", nullable: true),
                    LaneId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    RuntimeSubjectId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RuntimeSequence = table.Column<long>(type: "bigint", nullable: false),
                    RuntimeSessionGeneration = table.Column<long>(type: "bigint", nullable: false),
                    RecognitionDistance = table.Column<double>(type: "float", nullable: true),
                    ModelRegistryVersion = table.Column<long>(type: "bigint", nullable: true),
                    EmployeeFaceModelId = table.Column<int>(type: "int", nullable: true),
                    ModelFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModelChecksumPrefix = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    MatchStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SyncRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceRecognitionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceRecognitionEvents_EmployeeFaceModels_EmployeeFaceModelId",
                        column: x => x.EmployeeFaceModelId,
                        principalTable: "EmployeeFaceModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FaceRecognitionEvents_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FaceRecognitionEvents_FaceCameraConfigurations_FaceCameraConfigurationId",
                        column: x => x.FaceCameraConfigurationId,
                        principalTable: "FaceCameraConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FaceRecognitionEvents_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "LaneId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_CameraId_OccurredAtUtc",
                table: "FaceRecognitionEvents",
                columns: new[] { "CameraId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_EmployeeFaceModelId",
                table: "FaceRecognitionEvents",
                column: "EmployeeFaceModelId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_EmployeeId_OccurredAtUtc",
                table: "FaceRecognitionEvents",
                columns: new[] { "EmployeeId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_FaceCameraConfigurationId_OccurredAtUtc",
                table: "FaceRecognitionEvents",
                columns: new[] { "FaceCameraConfigurationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_LaneId",
                table: "FaceRecognitionEvents",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_MatchStatus",
                table: "FaceRecognitionEvents",
                column: "MatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_OccurredAtUtc",
                table: "FaceRecognitionEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_RuntimeEventId",
                table: "FaceRecognitionEvents",
                column: "RuntimeEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceRecognitionCollectorCheckpoints");

            migrationBuilder.DropTable(
                name: "FaceRecognitionEvents");
        }
    }
}
