using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameWriteOffToAdvanceWriteOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "預支沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "54",
                column: "Module",
                value: "預支沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "55",
                column: "Module",
                value: "預支沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "56",
                column: "Module",
                value: "預支沖銷申請");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "54",
                column: "Module",
                value: "沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "55",
                column: "Module",
                value: "沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "56",
                column: "Module",
                value: "沖銷申請");
        }
    }
}
