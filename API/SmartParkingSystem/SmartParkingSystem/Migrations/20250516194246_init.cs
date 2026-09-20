using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parkings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Parkingtype = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    priceperhour = table.Column<double>(type: "float", nullable: false),
                    isOccupied = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parkings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CarParkings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parkingID = table.Column<int>(type: "int", nullable: false),
                    carNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    parkingStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    parkingEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    totalAmount = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarParkings", x => x.id);
                    table.ForeignKey(
                        name: "FK_CarParkings_Parkings_parkingID",
                        column: x => x.parkingID,
                        principalTable: "Parkings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarParkings_parkingID",
                table: "CarParkings",
                column: "parkingID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarParkings");

            migrationBuilder.DropTable(
                name: "Parkings");
        }
    }
}
