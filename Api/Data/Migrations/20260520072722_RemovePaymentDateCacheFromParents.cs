using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentDateCacheFromParents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdvanceRequests_Users_PaidByUserId",
                table: "AdvanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_Users_PaidByUserId",
                table: "PaymentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TravelPaymentRequests_Users_PaidByUserId",
                table: "TravelPaymentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_Users_PaidByUserId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_PaidByUserId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TravelPaymentRequests_PaidByUserId",
                table: "TravelPaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_PaidByUserId",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequests_PaidByUserId",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedPaymentDate",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedPaymentDate",
                table: "TravelPaymentRequests");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "TravelPaymentRequests");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "TravelPaymentRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedPaymentDate",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedPaymentDate",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "AdvanceRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPaymentDate",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidByUserId",
                table: "TravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPaymentDate",
                table: "TravelPaymentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "TravelPaymentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidByUserId",
                table: "TravelPaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPaymentDate",
                table: "PaymentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "PaymentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidByUserId",
                table: "PaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedPaymentDate",
                table: "AdvanceRequests",
                type: "datetime2",
                nullable: true);

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
                name: "IX_TravelRequests_PaidByUserId",
                table: "TravelRequests",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequests_PaidByUserId",
                table: "TravelPaymentRequests",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_PaidByUserId",
                table: "PaymentRequests",
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
                name: "FK_PaymentRequests_Users_PaidByUserId",
                table: "PaymentRequests",
                column: "PaidByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TravelPaymentRequests_Users_PaidByUserId",
                table: "TravelPaymentRequests",
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
    }
}
