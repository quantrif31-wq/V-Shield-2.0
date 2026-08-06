using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class EstablishCanonicalEmployeeCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessCredentials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CredentialType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdentifierHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IdentifierHashVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    MaskedIdentifier = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EmployeeDynamicQrId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RevokedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessCredentials_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_AccessCredentials_AppUsers_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_AccessCredentials_EmployeeDynamicQrs_EmployeeDynamicQrId",
                        column: x => x.EmployeeDynamicQrId,
                        principalTable: "EmployeeDynamicQrs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessCredentials_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessCredentials_CreatedByUserId",
                table: "AccessCredentials",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessCredentials_CredentialType",
                table: "AccessCredentials",
                column: "CredentialType");

            migrationBuilder.CreateIndex(
                name: "IX_AccessCredentials_CredentialType_IdentifierHash",
                table: "AccessCredentials",
                columns: new[] { "CredentialType", "IdentifierHash" },
                unique: true,
                filter: "[IdentifierHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessCredentials_EmployeeDynamicQrId",
                table: "AccessCredentials",
                column: "EmployeeDynamicQrId",
                unique: true,
                filter: "[EmployeeDynamicQrId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessCredentials_EmployeeId_CredentialType",
                table: "AccessCredentials",
                columns: new[] { "EmployeeId", "CredentialType" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessCredentials_ExpiresAtUtc",
                table: "AccessCredentials",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AccessCredentials_RevokedByUserId",
                table: "AccessCredentials",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessCredentials_Status",
                table: "AccessCredentials",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_AccessCredentials_ActiveFace_Employee",
                table: "AccessCredentials",
                column: "EmployeeId",
                unique: true,
                filter: "[CredentialType] = 'FaceBiometric' AND [Status] = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessCredentials");
        }
    }
}
