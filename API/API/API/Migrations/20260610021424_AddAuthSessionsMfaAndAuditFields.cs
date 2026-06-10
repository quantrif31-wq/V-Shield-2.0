using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthSessionsMfaAndAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientIp",
                table: "SystemAuditLogs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "SystemAuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventCategory",
                table: "SystemAuditLogs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "APPLICATION");

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "SystemAuditLogs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "INFO");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "SystemAuditLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "AppUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedAtUtc",
                table: "AppUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfaConfiguredAtUtc",
                table: "AppUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MfaEnabled",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MfaSecretProtected",
                table: "AppUsers",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "AppUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    JwtId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokens_AppUser",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_CorrelationId",
                table: "SystemAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_EventCategory_TimestampUtc",
                table: "SystemAuditLogs",
                columns: new[] { "EventCategory", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_TokenHash",
                table: "UserRefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId_ExpiresAtUtc",
                table: "UserRefreshTokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_SystemAuditLogs_CorrelationId",
                table: "SystemAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_SystemAuditLogs_EventCategory_TimestampUtc",
                table: "SystemAuditLogs");

            migrationBuilder.DropColumn(
                name: "ClientIp",
                table: "SystemAuditLogs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "SystemAuditLogs");

            migrationBuilder.DropColumn(
                name: "EventCategory",
                table: "SystemAuditLogs");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "SystemAuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "SystemAuditLogs");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangedAtUtc",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "MfaConfiguredAtUtc",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "MfaEnabled",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "MfaSecretProtected",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "AppUsers");
        }
    }
}
