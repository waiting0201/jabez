using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShiftChangeRequestDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftChangeRequestId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    FromDayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToDayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftChangeRequestDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequestDates_ShiftChangeRequests_ShiftChangeRequestId",
                        column: x => x.ShiftChangeRequestId,
                        principalTable: "ShiftChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequestDates_ShiftChangeRequestId_Date",
                table: "ShiftChangeRequestDates",
                columns: new[] { "ShiftChangeRequestId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_ApprovalItemId",
                table: "ShiftChangeRequests",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_EmployeeId_Year_Month_ApprovalStatus",
                table: "ShiftChangeRequests",
                columns: new[] { "EmployeeId", "Year", "Month", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_RequestNo",
                table: "ShiftChangeRequests",
                column: "RequestNo",
                unique: true,
                filter: "[RequestNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_ReviewedById",
                table: "ShiftChangeRequests",
                column: "ReviewedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftChangeRequestDates");

            migrationBuilder.DropTable(
                name: "ShiftChangeRequests");
        }
    }
}
