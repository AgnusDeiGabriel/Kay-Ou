using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayAyiti.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustSafetyVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdDocumentPath",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KycRejectionReason",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KycStatus",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateTime>(
                name: "KycSubmittedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KycVerifiedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KycVerifiedByAdminId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelfiePath",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdresseComplete",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Houses",
                type: "decimal(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Houses",
                type: "decimal(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipMandatePath",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipRejectionReason",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipStatus",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnershipSubmittedAt",
                table: "Houses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipTaxReceiptPath",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipTitleDeedPath",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnershipVerifiedAt",
                table: "Houses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnershipVerifiedByAdminId",
                table: "Houses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterId = table.Column<int>(type: "int", nullable: false),
                    HouseId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByAdminId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Houses_HouseId",
                        column: x => x.HouseId,
                        principalTable: "Houses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IdDocumentPath", "KycRejectionReason", "KycStatus", "KycSubmittedAt", "KycVerifiedAt", "KycVerifiedByAdminId", "SelfiePath" },
                values: new object[] { null, null, "None", null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_HouseId_Status",
                table: "Reports",
                columns: new[] { "HouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports",
                column: "ReporterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropColumn(
                name: "IdDocumentPath",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycRejectionReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycSubmittedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycVerifiedByAdminId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelfiePath",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdresseComplete",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OwnershipMandatePath",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OwnershipRejectionReason",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OwnershipStatus",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OwnershipSubmittedAt",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OwnershipTaxReceiptPath",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OwnershipTitleDeedPath",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OwnershipVerifiedAt",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OwnershipVerifiedByAdminId",
                table: "Houses");
        }
    }
}
