using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalRecordsAndCurrentStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStepOrder",
                table: "TravelRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStepOrder",
                table: "PaymentRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStepOrder",
                table: "LeaveRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ApprovalRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRecords_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "LeaveRequests",
                keyColumn: "Id",
                keyValue: 1,
                column: "CurrentStepOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "LeaveRequests",
                keyColumn: "Id",
                keyValue: 2,
                column: "CurrentStepOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "LeaveRequests",
                keyColumn: "Id",
                keyValue: 3,
                column: "CurrentStepOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 1,
                column: "CurrentStepOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 2,
                column: "CurrentStepOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 3,
                column: "CurrentStepOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "TravelRequests",
                keyColumn: "Id",
                keyValue: 1,
                column: "CurrentStepOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "TravelRequests",
                keyColumn: "Id",
                keyValue: 2,
                column: "CurrentStepOrder",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_ApplicationType_ApplicationId_StepOrder",
                table: "ApprovalRecords",
                columns: new[] { "ApplicationType", "ApplicationId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_ReviewedById",
                table: "ApprovalRecords",
                column: "ReviewedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRecords");

            migrationBuilder.DropColumn(
                name: "CurrentStepOrder",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "CurrentStepOrder",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "CurrentStepOrder",
                table: "LeaveRequests");
        }
    }
}
