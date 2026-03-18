using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicantDesignatedReviewer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DesignatedReviewerId",
                table: "TravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DesignatedReviewerId",
                table: "PaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DesignatedReviewerId",
                table: "OvertimeRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DesignatedReviewerId",
                table: "LeaveRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseApplicantDesignated",
                table: "ApprovalSteps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DesignatedReviewerId",
                table: "AdvanceRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_DesignatedReviewerId",
                table: "TravelRequests",
                column: "DesignatedReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_DesignatedReviewerId",
                table: "PaymentRequests",
                column: "DesignatedReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_DesignatedReviewerId",
                table: "OvertimeRequests",
                column: "DesignatedReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_DesignatedReviewerId",
                table: "LeaveRequests",
                column: "DesignatedReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_DesignatedReviewerId",
                table: "AdvanceRequests",
                column: "DesignatedReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdvanceRequests_Users_DesignatedReviewerId",
                table: "AdvanceRequests",
                column: "DesignatedReviewerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_DesignatedReviewerId",
                table: "LeaveRequests",
                column: "DesignatedReviewerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OvertimeRequests_Users_DesignatedReviewerId",
                table: "OvertimeRequests",
                column: "DesignatedReviewerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRequests_Users_DesignatedReviewerId",
                table: "PaymentRequests",
                column: "DesignatedReviewerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TravelRequests_Users_DesignatedReviewerId",
                table: "TravelRequests",
                column: "DesignatedReviewerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdvanceRequests_Users_DesignatedReviewerId",
                table: "AdvanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_DesignatedReviewerId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_OvertimeRequests_Users_DesignatedReviewerId",
                table: "OvertimeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_Users_DesignatedReviewerId",
                table: "PaymentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_Users_DesignatedReviewerId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_DesignatedReviewerId",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_DesignatedReviewerId",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_OvertimeRequests_DesignatedReviewerId",
                table: "OvertimeRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_DesignatedReviewerId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequests_DesignatedReviewerId",
                table: "AdvanceRequests");

            migrationBuilder.DropColumn(
                name: "DesignatedReviewerId",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "DesignatedReviewerId",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "DesignatedReviewerId",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "DesignatedReviewerId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "UseApplicantDesignated",
                table: "ApprovalSteps");

            migrationBuilder.DropColumn(
                name: "DesignatedReviewerId",
                table: "AdvanceRequests");
        }
    }
}
