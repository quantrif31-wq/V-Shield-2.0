using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class EstablishFaceModelLifecycleMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAtUtc",
                table: "EmployeeFaceModels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "EmployeeFaceModels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EncodingCount",
                table: "EmployeeFaceModels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "EmployeeFaceModels",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureMessage",
                table: "EmployeeFaceModels",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelChecksum",
                table: "EmployeeFaceModels",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAtUtc",
                table: "EmployeeFaceModels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmployeeFaceModels",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceEnrollmentJobId",
                table: "EmployeeFaceModels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "EmployeeFaceModels",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "EmployeeFaceModels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeFaceModels_ActiveEmployee",
                table: "EmployeeFaceModels",
                columns: new[] { "EmployeeId", "Status" },
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeFaceModels_EmployeeId_Version",
                table: "EmployeeFaceModels",
                columns: new[] { "EmployeeId", "Version" },
                unique: true,
                filter: "[Version] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeFaceModels_ModelFileName",
                table: "EmployeeFaceModels",
                column: "ModelFileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeFaceModels_SourceEnrollmentJobId",
                table: "EmployeeFaceModels",
                column: "SourceEnrollmentJobId",
                unique: true,
                filter: "[SourceEnrollmentJobId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_EmployeeFaceModels_ActiveEmployee",
                table: "EmployeeFaceModels");

            migrationBuilder.DropIndex(
                name: "UX_EmployeeFaceModels_EmployeeId_Version",
                table: "EmployeeFaceModels");

            migrationBuilder.DropIndex(
                name: "UX_EmployeeFaceModels_ModelFileName",
                table: "EmployeeFaceModels");

            migrationBuilder.DropIndex(
                name: "UX_EmployeeFaceModels_SourceEnrollmentJobId",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "ActivatedAtUtc",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "EncodingCount",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "FailureMessage",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "ModelChecksum",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "SourceEnrollmentJobId",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EmployeeFaceModels");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "EmployeeFaceModels");
        }
    }
}
