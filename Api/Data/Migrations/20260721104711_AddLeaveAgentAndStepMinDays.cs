using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveAgentAndStepMinDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgentUserId",
                table: "LeaveRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinDays",
                table: "ApprovalSteps",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 3,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 4,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 5,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 6,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 7,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 8,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 9,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 10,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 11,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 12,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 13,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 20,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 21,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 22,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 23,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 30,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 31,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 32,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 33,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 40,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 41,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 42,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 43,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 50,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 51,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 52,
                column: "MinDays",
                value: null);

            migrationBuilder.UpdateData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 53,
                column: "MinDays",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_AgentUserId",
                table: "LeaveRequests",
                column: "AgentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_AgentUserId",
                table: "LeaveRequests",
                column: "AgentUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_AgentUserId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_AgentUserId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "AgentUserId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "MinDays",
                table: "ApprovalSteps");
        }
    }
}
