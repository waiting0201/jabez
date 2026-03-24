using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelWriteOffRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedById",
                table: "TravelRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "TravelRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "TravelRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "TravelRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TravelWriteOffRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: ""),
                    TravelRequestId = table.Column<int>(type: "int", nullable: false),
                    WriteOffNo = table.Column<int>(type: "int", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelWriteOffRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelWriteOffRecords_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TravelWriteOffRecords_TravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelWriteOffRecords_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TravelWriteOffRecords_Users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TravelWriteOffItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TravelWriteOffRecordId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelWriteOffItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelWriteOffItems_TravelWriteOffRecords_TravelWriteOffRecordId",
                        column: x => x.TravelWriteOffRecordId,
                        principalTable: "TravelWriteOffRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ApprovalItems",
                columns: new[] { "Id", "ApplicationType", "Code", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[] { 8, "travel_write_off", "travel_write_off_request", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "出差沖銷申請" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "57", "travel-write-off-requests:read", null, "出差沖銷申請", "瀏覽" },
                    { "58", "travel-write-off-requests:write", null, "出差沖銷申請", "新增/修改" },
                    { "59", "travel-write-off-requests:delete", null, "出差沖銷申請", "刪除" }
                });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder", "UseApplicantDepartment" },
                values: new object[] { 30, 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, "部門主管初核", 1, true });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder" },
                values: new object[,]
                {
                    { 31, 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 4, "取得紙本資料審核", 2 },
                    { 32, 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 4, "填入預計撥款日，核決及撥款後，填入撥款日", 3 },
                    { 33, 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 5, "最終核決", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_ClosedById",
                table: "TravelRequests",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_TravelWriteOffItems_TravelWriteOffRecordId",
                table: "TravelWriteOffItems",
                column: "TravelWriteOffRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelWriteOffRecords_ApprovalItemId",
                table: "TravelWriteOffRecords",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelWriteOffRecords_ReviewedById",
                table: "TravelWriteOffRecords",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TravelWriteOffRecords_SubmittedById",
                table: "TravelWriteOffRecords",
                column: "SubmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_TravelWriteOffRecords_TravelRequestId",
                table: "TravelWriteOffRecords",
                column: "TravelRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_TravelRequests_Users_ClosedById",
                table: "TravelRequests",
                column: "ClosedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_Users_ClosedById",
                table: "TravelRequests");

            migrationBuilder.DropTable(
                name: "TravelWriteOffItems");

            migrationBuilder.DropTable(
                name: "TravelWriteOffRecords");

            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_ClosedById",
                table: "TravelRequests");

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "57");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "58");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "59");

            migrationBuilder.DeleteData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "TravelRequests");
        }
    }
}
