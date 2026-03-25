using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RefundedByUserId",
                table: "TravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundedByUserId",
                table: "AdvanceRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_RefundedByUserId",
                table: "TravelRequests",
                column: "RefundedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_RefundedByUserId",
                table: "AdvanceRequests",
                column: "RefundedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdvanceRequests_Users_RefundedByUserId",
                table: "AdvanceRequests",
                column: "RefundedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TravelRequests_Users_RefundedByUserId",
                table: "TravelRequests",
                column: "RefundedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdvanceRequests_Users_RefundedByUserId",
                table: "AdvanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_Users_RefundedByUserId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_RefundedByUserId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequests_RefundedByUserId",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "RefundedByUserId",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "RefundedByUserId",
                table: "AdvanceRequests");
        }
    }
}
