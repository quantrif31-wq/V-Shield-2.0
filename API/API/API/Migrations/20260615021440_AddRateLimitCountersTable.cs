using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddRateLimitCountersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RateLimitCounters",
                columns: table => new
                {
                    RateLimitCounterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CounterKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WindowStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitCounters", x => x.RateLimitCounterId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitCounters_CreatedAt",
                table: "RateLimitCounters",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitCounters_Key_Window",
                table: "RateLimitCounters",
                columns: new[] { "CounterKey", "WindowStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateLimitCounters");
        }
    }
}
