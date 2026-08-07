using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeFaceCredentialBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeFaceCredentialBindings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AccessCredentialId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RevokedByUserId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeFaceCredentialBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFaceCredentialBindings_AccessCredentials_AccessCredentialId",
                        column: x => x.AccessCredentialId,
                        principalTable: "AccessCredentials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeFaceCredentialBindings_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_EmployeeFaceCredentialBindings_AppUsers_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_EmployeeFaceCredentialBindings_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFaceCredentialBindings_ActivatedAtUtc",
                table: "EmployeeFaceCredentialBindings",
                column: "ActivatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFaceCredentialBindings_CreatedByUserId",
                table: "EmployeeFaceCredentialBindings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFaceCredentialBindings_RevokedAtUtc",
                table: "EmployeeFaceCredentialBindings",
                column: "RevokedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFaceCredentialBindings_RevokedByUserId",
                table: "EmployeeFaceCredentialBindings",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFaceCredentialBindings_Status",
                table: "EmployeeFaceCredentialBindings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeFaceCredentialBindings_ActiveCredential",
                table: "EmployeeFaceCredentialBindings",
                column: "AccessCredentialId",
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeFaceCredentialBindings_ActiveEmployee",
                table: "EmployeeFaceCredentialBindings",
                column: "EmployeeId",
                unique: true,
                filter: "[Status] = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeFaceCredentialBindings");
        }
    }
}
