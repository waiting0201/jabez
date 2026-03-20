using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceRequestClosedAndRefundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "AdvanceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedById",
                table: "AdvanceRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "AdvanceRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "AdvanceRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "AdvanceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_ClosedById",
                table: "AdvanceRequests",
                column: "ClosedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AdvanceRequests_Users_ClosedById",
                table: "AdvanceRequests",
                column: "ClosedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdvanceRequests_Users_ClosedById",
                table: "AdvanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequests_ClosedById",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "AdvanceRequests");
        }
    }
}
