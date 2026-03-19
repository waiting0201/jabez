using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestDesignatedReviewers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "DesignatedReviewerId",
                table: "AdvanceRequests");

            migrationBuilder.CreateTable(
                name: "RequestDesignatedReviewers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 8, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDesignatedReviewers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestDesignatedReviewers_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_ReviewerId",
                table: "RequestDesignatedReviewers",
                columns: new[] { "RequestType", "RequestId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestDesignatedReviewers_RequestType_RequestId_StepOrder",
                table: "RequestDesignatedReviewers",
                columns: new[] { "RequestType", "RequestId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestDesignatedReviewers_ReviewerId",
                table: "RequestDesignatedReviewers",
                column: "ReviewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestDesignatedReviewers");

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
    }
}
