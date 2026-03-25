using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidByUserIdToAdvanceAndTravel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaidByUserId",
                table: "TravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidByUserId",
                table: "AdvanceRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_PaidByUserId",
                table: "TravelRequests",
                column: "PaidByUserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_TravelRequests_Users_PaidByUserId",
                table: "TravelRequests",
                column: "PaidByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdvanceRequests_Users_PaidByUserId",
                table: "AdvanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_Users_PaidByUserId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_PaidByUserId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequests_PaidByUserId",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "AdvanceRequests");
        }
    }
}
