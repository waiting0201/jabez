using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTravelPermissionModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "31",
                columns: new[] { "Description", "Module" },
                values: new object[] { "出差預支申請", "出差預支申請" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "32",
                columns: new[] { "Description", "Module" },
                values: new object[] { "出差預支申請", "出差預支申請" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "33",
                columns: new[] { "Description", "Module" },
                values: new object[] { "出差預支申請", "出差預支申請" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "57",
                column: "Module",
                value: "出差預支沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "58",
                column: "Module",
                value: "出差預支沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "59",
                column: "Module",
                value: "出差預支沖銷申請");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "31",
                columns: new[] { "Description", "Module" },
                values: new object[] { "出差申請", "出差申請" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "32",
                columns: new[] { "Description", "Module" },
                values: new object[] { "出差申請", "出差申請" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "33",
                columns: new[] { "Description", "Module" },
                values: new object[] { "出差申請", "出差申請" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "57",
                column: "Module",
                value: "出差沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "58",
                column: "Module",
                value: "出差沖銷申請");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "59",
                column: "Module",
                value: "出差沖銷申請");
        }
    }
}
