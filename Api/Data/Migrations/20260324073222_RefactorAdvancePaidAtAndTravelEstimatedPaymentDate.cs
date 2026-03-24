using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAdvancePaidAtAndTravelEstimatedPaymentDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdvanceRequests_Users_PaidByUserId",
                table: "AdvanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequests_PaidByUserId",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "AdvanceRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPaymentDate",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedPaymentDate",
                table: "TravelRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "AdvanceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidByUserId",
                table: "AdvanceRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_PaidByUserId",
                table: "AdvanceRequests",
                column: "PaidByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdvanceRequests_Users_PaidByUserId",
                table: "AdvanceRequests",
                column: "PaidByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
