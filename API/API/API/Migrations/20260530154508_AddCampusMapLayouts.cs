using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddCampusMapLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampusMapLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GateId = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Y = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    W = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    H = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ZIndex = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Color = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampusMapLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampusMapLayouts_AppUser",
                        column: x => x.UpdatedBy,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CampusMapLayouts_Gate",
                        column: x => x.GateId,
                        principalTable: "Gate",
                        principalColumn: "GateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampusMapLayouts_GateId",
                table: "CampusMapLayouts",
                column: "GateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampusMapLayouts_UpdatedBy",
                table: "CampusMapLayouts",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampusMapLayouts");
        }
    }
}
