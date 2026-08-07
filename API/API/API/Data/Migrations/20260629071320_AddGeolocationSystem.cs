using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeolocationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Sites",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Sites",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Notifications",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationLabel",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Notifications",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Gate",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Gate",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "EmergencyPasses",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "EmergencyPasses",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "DuressEvents",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "DuressEvents",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "DispatchTasks",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "DispatchTasks",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Buildings",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Buildings",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalFloors",
                table: "Buildings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Accuracy",
                table: "Alarms",
                type: "decimal(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Altitude",
                table: "Alarms",
                type: "decimal(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Alarms",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Alarms",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDeviceId",
                table: "Alarms",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "AccessPoints",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "AccessPoints",
                type: "decimal(18,12)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IndoorPathNodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<int>(type: "int", nullable: false),
                    FacilityFloorId = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NodeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    X = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Y = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Z = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    IsEmergencyExit = table.Column<bool>(type: "bit", nullable: false),
                    IsAccessible = table.Column<bool>(type: "bit", nullable: false),
                    NeighborsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndoorPathNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndoorPathNodes_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "BuildingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndoorPathNodes_FacilityFloors_FacilityFloorId",
                        column: x => x.FacilityFloorId,
                        principalTable: "FacilityFloors",
                        principalColumn: "FacilityFloorId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndoorPathNodes_BuildingId_FacilityFloorId",
                table: "IndoorPathNodes",
                columns: new[] { "BuildingId", "FacilityFloorId" });

            migrationBuilder.CreateIndex(
                name: "IX_IndoorPathNodes_FacilityFloorId",
                table: "IndoorPathNodes",
                column: "FacilityFloorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndoorPathNodes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "LocationLabel",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Gate");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Gate");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "EmergencyPasses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "EmergencyPasses");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "DuressEvents");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "DuressEvents");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "DispatchTasks");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "DispatchTasks");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "TotalFloors",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "Accuracy",
                table: "Alarms");

            migrationBuilder.DropColumn(
                name: "Altitude",
                table: "Alarms");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Alarms");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Alarms");

            migrationBuilder.DropColumn(
                name: "SourceDeviceId",
                table: "Alarms");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AccessPoints");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AccessPoints");
        }
    }
}
