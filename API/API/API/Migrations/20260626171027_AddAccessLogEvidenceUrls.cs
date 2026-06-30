using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessLogEvidenceUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CapturedFaceCropURL",
                table: "Access_Log",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CapturedPlateCropURL",
                table: "Access_Log",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CapturedQrSnapshotURL",
                table: "Access_Log",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CapturedSnapshotURL",
                table: "Access_Log",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CapturedFaceCropURL",
                table: "Access_Log");

            migrationBuilder.DropColumn(
                name: "CapturedPlateCropURL",
                table: "Access_Log");

            migrationBuilder.DropColumn(
                name: "CapturedQrSnapshotURL",
                table: "Access_Log");

            migrationBuilder.DropColumn(
                name: "CapturedSnapshotURL",
                table: "Access_Log");
        }
    }
}
