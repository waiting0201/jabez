using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameHolidayTravelToHolidayActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "假日執行活動申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "60",
                column: "Module",
                value: "假日執行活動申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "61",
                column: "Module",
                value: "假日執行活動申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "62",
                column: "Module",
                value: "假日執行活動申請");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "假日出差申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "60",
                column: "Module",
                value: "假日出差申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "61",
                column: "Module",
                value: "假日出差申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "62",
                column: "Module",
                value: "假日出差申請");
        }
    }
}
