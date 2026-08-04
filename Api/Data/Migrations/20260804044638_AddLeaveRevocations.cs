using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRevocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OriginalHours",
                table: "LeaveRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeaveRevocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RevokedHours = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRevocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRevocations_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LeaveRevocations_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveRevocations_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveRevocations_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeaveRevocationDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRevocationId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(4,1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRevocationDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRevocationDates_LeaveRevocations_LeaveRevocationId",
                        column: x => x.LeaveRevocationId,
                        principalTable: "LeaveRevocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRevocationDates_Date",
                table: "LeaveRevocationDates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRevocationDates_LeaveRevocationId_Date",
                table: "LeaveRevocationDates",
                columns: new[] { "LeaveRevocationId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRevocations_ApprovalItemId",
                table: "LeaveRevocations",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRevocations_EmployeeId",
                table: "LeaveRevocations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRevocations_LeaveRequestId_ApprovalStatus",
                table: "LeaveRevocations",
                columns: new[] { "LeaveRequestId", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRevocations_ReviewedById",
                table: "LeaveRevocations",
                column: "ReviewedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveRevocationDates");

            migrationBuilder.DropTable(
                name: "LeaveRevocations");

            migrationBuilder.DropColumn(
                name: "OriginalHours",
                table: "LeaveRequests");
        }
    }
}
