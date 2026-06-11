using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayAyiti.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HouseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseLikes_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HouseLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseLikes_HouseId",
                table: "HouseLikes",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseLikes_UserId_HouseId",
                table: "HouseLikes",
                columns: new[] { "UserId", "HouseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseLikes");
        }
    }
}
