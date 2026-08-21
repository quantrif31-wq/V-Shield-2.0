using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentSystemAndEmployeeProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CCCD",
                table: "Employee",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Employee",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "Employee",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Employee",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentAuditLogs",
                columns: table => new
                {
                    AgentAuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    ToolName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ArgsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PromptTokens = table.Column<long>(type: "bigint", nullable: true),
                    CompletionTokens = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentAuditLogs", x => x.AgentAuditId);
                });

            migrationBuilder.CreateTable(
                name: "AgentDrafts",
                columns: table => new
                {
                    AgentDraftId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    To = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SendError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDrafts", x => x.AgentDraftId);
                });

            migrationBuilder.CreateTable(
                name: "AgentMessages",
                columns: table => new
                {
                    AgentMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PromptTokens = table.Column<long>(type: "bigint", nullable: true),
                    CompletionTokens = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMessages", x => x.AgentMessageId);
                });

            migrationBuilder.CreateTable(
                name: "AgentThreads",
                columns: table => new
                {
                    AgentThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FactBlob = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentThreads", x => x.AgentThreadId);
                });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 1,
                columns: new[] { "CCCD", "DateOfBirth", "EmployeeCode", "Gender" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 2,
                columns: new[] { "CCCD", "DateOfBirth", "EmployeeCode", "Gender" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 3,
                columns: new[] { "CCCD", "DateOfBirth", "EmployeeCode", "Gender" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 4,
                columns: new[] { "CCCD", "DateOfBirth", "EmployeeCode", "Gender" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Employee",
                keyColumn: "EmployeeId",
                keyValue: 5,
                columns: new[] { "CCCD", "DateOfBirth", "EmployeeCode", "Gender" },
                values: new object[] { null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuditLogs_AgentThreadId",
                table: "AgentAuditLogs",
                column: "AgentThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAuditLogs_CreatedAt",
                table: "AgentAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDrafts_AgentThreadId",
                table: "AgentDrafts",
                column: "AgentThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDrafts_UserId",
                table: "AgentDrafts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_AgentThreadId",
                table: "AgentMessages",
                column: "AgentThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_CreatedAt",
                table: "AgentMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentThreads_UpdatedAt",
                table: "AgentThreads",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentThreads_UserId",
                table: "AgentThreads",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentAuditLogs");

            migrationBuilder.DropTable(
                name: "AgentDrafts");

            migrationBuilder.DropTable(
                name: "AgentMessages");

            migrationBuilder.DropTable(
                name: "AgentThreads");

            migrationBuilder.DropColumn(
                name: "CCCD",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Employee");
        }
    }
}
