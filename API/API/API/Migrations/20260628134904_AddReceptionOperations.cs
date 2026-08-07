using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceptionInteractions",
                columns: table => new
                {
                    ReceptionInteractionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: true),
                    LostItemReportId = table.Column<long>(type: "bigint", nullable: true),
                    FoundItemReportId = table.Column<long>(type: "bigint", nullable: true),
                    InteractionType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DetailNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContactPersonName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    ContactPersonPhone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RelatedVehiclePlate = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SecurityRequested = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptionInteractions", x => x.ReceptionInteractionId);
                    table.ForeignKey(
                        name: "FK_ReceptionInteractions_FoundItemReports_FoundItemReportId",
                        column: x => x.FoundItemReportId,
                        principalTable: "FoundItemReports",
                        principalColumn: "FoundItemReportId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReceptionInteractions_LostItemReports_LostItemReportId",
                        column: x => x.LostItemReportId,
                        principalTable: "LostItemReports",
                        principalColumn: "LostItemReportId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReceptionInteractions_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionInteractions_FoundItemReportId",
                table: "ReceptionInteractions",
                column: "FoundItemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionInteractions_LostItemReportId",
                table: "ReceptionInteractions",
                column: "LostItemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionInteractions_Status_CreatedAtUtc",
                table: "ReceptionInteractions",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionInteractions_VisitId_CreatedAtUtc",
                table: "ReceptionInteractions",
                columns: new[] { "VisitId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceptionInteractions");
        }
    }
}
