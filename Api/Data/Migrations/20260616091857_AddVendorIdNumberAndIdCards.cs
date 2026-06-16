using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorIdNumberAndIdCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdCardBackUrl",
                table: "Vendors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCardFrontUrl",
                table: "Vendors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdNumber",
                table: "Vendors",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_IdNumber",
                table: "Vendors",
                column: "IdNumber",
                unique: true,
                filter: "[IdNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendors_IdNumber",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IdCardBackUrl",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IdCardFrontUrl",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IdNumber",
                table: "Vendors");
        }
    }
}
