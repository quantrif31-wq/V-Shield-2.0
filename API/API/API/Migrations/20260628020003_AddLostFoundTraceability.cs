using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddLostFoundTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReporterIdNumber",
                table: "LostItemReports",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReporterPhotoUrl",
                table: "LostItemReports",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinderPhotoUrl",
                table: "FoundItemReports",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FoundByIdNumber",
                table: "FoundItemReports",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FoundByPhone",
                table: "FoundItemReports",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ItemEvidenceId",
                table: "FoundItemReports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimantPhotoUrl",
                table: "ClaimRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedByUserId",
                table: "ClaimRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverNote",
                table: "ClaimRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemPhotoUrl",
                table: "ClaimRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ClaimRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnPhotoUrl",
                table: "ClaimRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "ClaimRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WitnessName",
                table: "ClaimRequests",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemReports_ItemEvidenceId",
                table: "FoundItemReports",
                column: "ItemEvidenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_FoundItemReports_EvidenceItems_ItemEvidenceId",
                table: "FoundItemReports",
                column: "ItemEvidenceId",
                principalTable: "EvidenceItems",
                principalColumn: "EvidenceItemId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundItemReports_EvidenceItems_ItemEvidenceId",
                table: "FoundItemReports");

            migrationBuilder.DropIndex(
                name: "IX_FoundItemReports_ItemEvidenceId",
                table: "FoundItemReports");

            migrationBuilder.DropColumn(
                name: "ReporterIdNumber",
                table: "LostItemReports");

            migrationBuilder.DropColumn(
                name: "ReporterPhotoUrl",
                table: "LostItemReports");

            migrationBuilder.DropColumn(
                name: "FinderPhotoUrl",
                table: "FoundItemReports");

            migrationBuilder.DropColumn(
                name: "FoundByIdNumber",
                table: "FoundItemReports");

            migrationBuilder.DropColumn(
                name: "FoundByPhone",
                table: "FoundItemReports");

            migrationBuilder.DropColumn(
                name: "ItemEvidenceId",
                table: "FoundItemReports");

            migrationBuilder.DropColumn(
                name: "ClaimantPhotoUrl",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "HandoverNote",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "ItemPhotoUrl",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "ReturnPhotoUrl",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "WitnessName",
                table: "ClaimRequests");
        }
    }
}
