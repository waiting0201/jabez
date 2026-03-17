using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceRequestAndWriteOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvanceRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    ActivityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActivityPeriod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdvanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CashTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubmittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedPaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvanceRequests_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdvanceRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdvanceRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AdvanceRequests_Users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AdvanceRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdvanceRequestId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvanceRequestItems_AdvanceRequests_AdvanceRequestId",
                        column: x => x.AdvanceRequestId,
                        principalTable: "AdvanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WriteOffRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdvanceRequestId = table.Column<int>(type: "int", nullable: false),
                    WriteOffNo = table.Column<int>(type: "int", nullable: false),
                    CashTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriteOffRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WriteOffRecords_AdvanceRequests_AdvanceRequestId",
                        column: x => x.AdvanceRequestId,
                        principalTable: "AdvanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WriteOffRecords_Users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WriteOffItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WriteOffRecordId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriteOffItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WriteOffItems_WriteOffRecords_WriteOffRecordId",
                        column: x => x.WriteOffRecordId,
                        principalTable: "WriteOffRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ApprovalItems",
                columns: new[] { "Id", "ApplicationType", "Code", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[] { 6, "advance", "advance_request", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "預支申請" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "51", "advance-requests:read", null, "預支申請", "瀏覽" },
                    { "52", "advance-requests:write", null, "預支申請", "新增/修改" },
                    { "53", "advance-requests:delete", null, "預支申請", "刪除" }
                });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder", "UseApplicantDepartment" },
                values: new object[] { 10, 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, "部門主管核准", 1, true });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder" },
                values: new object[] { 11, 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 4, "財務部確認撥款", 2 });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequestItems_AdvanceRequestId",
                table: "AdvanceRequestItems",
                column: "AdvanceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_ApprovalItemId",
                table: "AdvanceRequests",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_ProjectId",
                table: "AdvanceRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_ReviewedById",
                table: "AdvanceRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequests_SubmittedById",
                table: "AdvanceRequests",
                column: "SubmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffItems_WriteOffRecordId",
                table: "WriteOffItems",
                column: "WriteOffRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffRecords_AdvanceRequestId",
                table: "WriteOffRecords",
                column: "AdvanceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WriteOffRecords_SubmittedById",
                table: "WriteOffRecords",
                column: "SubmittedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvanceRequestItems");

            migrationBuilder.DropTable(
                name: "WriteOffItems");

            migrationBuilder.DropTable(
                name: "WriteOffRecords");

            migrationBuilder.DropTable(
                name: "AdvanceRequests");

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "51");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "52");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "53");

            migrationBuilder.DeleteData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
