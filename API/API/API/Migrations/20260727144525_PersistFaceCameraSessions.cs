using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class PersistFaceCameraSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaceCameraConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CameraId = table.Column<int>(type: "int", nullable: false),
                    RuntimeCameraId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LaneId = table.Column<int>(type: "int", nullable: true),
                    DesiredState = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AutoRestore = table.Column<bool>(type: "bit", nullable: false),
                    ConfigurationVersion = table.Column<long>(type: "bigint", nullable: false),
                    LastAppliedVersion = table.Column<long>(type: "bigint", nullable: false),
                    LastSyncStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastSyncError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastSyncAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfigurationFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceCameraConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceCameraConfigurations_Camera_CameraId",
                        column: x => x.CameraId,
                        principalTable: "Camera",
                        principalColumn: "CameraId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaceCameraConfigurations_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "LaneId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceCameraConfigurations_CameraId",
                table: "FaceCameraConfigurations",
                column: "CameraId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceCameraConfigurations_LaneId",
                table: "FaceCameraConfigurations",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceCameraConfigurations_RuntimeCameraId",
                table: "FaceCameraConfigurations",
                column: "RuntimeCameraId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceCameraConfigurations");
        }
    }
}
