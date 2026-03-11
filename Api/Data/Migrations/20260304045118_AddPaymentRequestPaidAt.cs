using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRequestPaidAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "PaymentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 1,
                column: "PaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 2,
                column: "PaidAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 3,
                column: "PaidAt",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "PaymentRequests");
        }
    }
}
