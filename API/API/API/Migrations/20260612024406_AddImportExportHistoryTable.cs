using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddImportExportHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportExportHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalFileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ResultFileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PerformedById = table.Column<int>(type: "int", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportExportHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportExportHistory_AppUsers_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportExportHistory_EntityType_OperationType",
                table: "ImportExportHistory",
                columns: new[] { "EntityType", "OperationType" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportExportHistory_PerformedAt",
                table: "ImportExportHistory",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportExportHistory_PerformedById",
                table: "ImportExportHistory",
                column: "PerformedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportExportHistory");
        }
    }
}
