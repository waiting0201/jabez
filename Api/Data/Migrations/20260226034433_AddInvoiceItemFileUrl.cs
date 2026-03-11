using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceItemFileUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "InvoiceItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 1,
                column: "FileUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "FileUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 3,
                column: "FileUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 4,
                column: "FileUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 5,
                column: "FileUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "InvoiceItems");
        }
    }
}
