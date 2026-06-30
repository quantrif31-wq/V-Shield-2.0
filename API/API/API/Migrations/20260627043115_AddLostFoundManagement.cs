using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddLostFoundManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LockerCabinets",
                columns: table => new
                {
                    LockerCabinetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockerCabinets", x => x.LockerCabinetId);
                });

            migrationBuilder.CreateTable(
                name: "LostItemReports",
                columns: table => new
                {
                    LostItemReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReporterPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReporterEmail = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LastSeenLocation = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    LostAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostItemReports", x => x.LostItemReportId);
                });

            migrationBuilder.CreateTable(
                name: "LockerCompartments",
                columns: table => new
                {
                    LockerCompartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LockerCabinetId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EvidenceItemId = table.Column<long>(type: "bigint", nullable: true),
                    OccupiedByUserId = table.Column<int>(type: "int", nullable: true),
                    OccupiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockerCompartments", x => x.LockerCompartmentId);
                    table.ForeignKey(
                        name: "FK_LockerCompartments_EvidenceItems_EvidenceItemId",
                        column: x => x.EvidenceItemId,
                        principalTable: "EvidenceItems",
                        principalColumn: "EvidenceItemId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LockerCompartments_LockerCabinets_LockerCabinetId",
                        column: x => x.LockerCabinetId,
                        principalTable: "LockerCabinets",
                        principalColumn: "LockerCabinetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoundItemReports",
                columns: table => new
                {
                    FoundItemReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FoundByName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FoundLocation = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    FoundAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StorageLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LockerCompartmentId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoundItemReports", x => x.FoundItemReportId);
                    table.ForeignKey(
                        name: "FK_FoundItemReports_LockerCompartments_LockerCompartmentId",
                        column: x => x.LockerCompartmentId,
                        principalTable: "LockerCompartments",
                        principalColumn: "LockerCompartmentId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LockerAccessLogs",
                columns: table => new
                {
                    LockerAccessLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LockerCompartmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockerAccessLogs", x => x.LockerAccessLogId);
                    table.ForeignKey(
                        name: "FK_LockerAccessLogs_LockerCompartments_LockerCompartmentId",
                        column: x => x.LockerCompartmentId,
                        principalTable: "LockerCompartments",
                        principalColumn: "LockerCompartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimRequests",
                columns: table => new
                {
                    ClaimRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FoundItemReportId = table.Column<long>(type: "bigint", nullable: false),
                    LostItemReportId = table.Column<long>(type: "bigint", nullable: true),
                    ClaimantName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ClaimantIdNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ClaimantPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProofDocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimRequests", x => x.ClaimRequestId);
                    table.ForeignKey(
                        name: "FK_ClaimRequests_FoundItemReports_FoundItemReportId",
                        column: x => x.FoundItemReportId,
                        principalTable: "FoundItemReports",
                        principalColumn: "FoundItemReportId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimRequests_LostItemReports_LostItemReportId",
                        column: x => x.LostItemReportId,
                        principalTable: "LostItemReports",
                        principalColumn: "LostItemReportId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemMatches",
                columns: table => new
                {
                    ItemMatchId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LostItemReportId = table.Column<long>(type: "bigint", nullable: false),
                    FoundItemReportId = table.Column<long>(type: "bigint", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    MatchedByUserId = table.Column<int>(type: "int", nullable: true),
                    MatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemMatches", x => x.ItemMatchId);
                    table.ForeignKey(
                        name: "FK_ItemMatches_FoundItemReports_FoundItemReportId",
                        column: x => x.FoundItemReportId,
                        principalTable: "FoundItemReports",
                        principalColumn: "FoundItemReportId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemMatches_LostItemReports_LostItemReportId",
                        column: x => x.LostItemReportId,
                        principalTable: "LostItemReports",
                        principalColumn: "LostItemReportId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRequests_FoundItemReportId",
                table: "ClaimRequests",
                column: "FoundItemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRequests_LostItemReportId",
                table: "ClaimRequests",
                column: "LostItemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRequests_Status_RequestedAtUtc",
                table: "ClaimRequests",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemReports_LockerCompartmentId",
                table: "FoundItemReports",
                column: "LockerCompartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemReports_Status_CreatedAtUtc",
                table: "FoundItemReports",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemMatches_FoundItemReportId",
                table: "ItemMatches",
                column: "FoundItemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMatches_LostItemReportId",
                table: "ItemMatches",
                column: "LostItemReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMatches_Status_MatchedAtUtc",
                table: "ItemMatches",
                columns: new[] { "Status", "MatchedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LockerAccessLogs_LockerCompartmentId_Timestamp",
                table: "LockerAccessLogs",
                columns: new[] { "LockerCompartmentId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LockerCompartments_EvidenceItemId",
                table: "LockerCompartments",
                column: "EvidenceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LockerCompartments_LockerCabinetId_Code",
                table: "LockerCompartments",
                columns: new[] { "LockerCabinetId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LockerCompartments_Status_LockerCabinetId",
                table: "LockerCompartments",
                columns: new[] { "Status", "LockerCabinetId" });

            migrationBuilder.CreateIndex(
                name: "IX_LostItemReports_Status_CreatedAtUtc",
                table: "LostItemReports",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimRequests");

            migrationBuilder.DropTable(
                name: "ItemMatches");

            migrationBuilder.DropTable(
                name: "LockerAccessLogs");

            migrationBuilder.DropTable(
                name: "FoundItemReports");

            migrationBuilder.DropTable(
                name: "LostItemReports");

            migrationBuilder.DropTable(
                name: "LockerCompartments");

            migrationBuilder.DropTable(
                name: "LockerCabinets");
        }
    }
}
