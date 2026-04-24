using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelPaymentRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TravelPaymentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedPaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelPaymentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPaymentRequests_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TravelPaymentRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TravelPaymentRequests_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TravelPaymentRequests_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TravelPaymentRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TravelPaymentRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TravelPaymentRequestId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelPaymentRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPaymentRequestItems_TravelPaymentRequests_TravelPaymentRequestId",
                        column: x => x.TravelPaymentRequestId,
                        principalTable: "TravelPaymentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "出差預支申請");

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "出差預支沖銷申請");

            migrationBuilder.InsertData(
                table: "ApprovalItems",
                columns: new[] { "Id", "ApplicationType", "Code", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[] { 10, "travel_payment", "travel_payment_request", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "出差請款申請" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "68", "travel-payment-requests:read", null, "出差請款申請", "瀏覽" },
                    { "69", "travel-payment-requests:write", null, "出差請款申請", "新增/修改" },
                    { "70", "travel-payment-requests:delete", null, "出差請款申請", "刪除" }
                });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder", "UseApplicantDepartment" },
                values: new object[] { 50, 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, "部門主管初核", 1, true });

            migrationBuilder.InsertData(
                table: "ApprovalSteps",
                columns: new[] { "Id", "ApprovalItemId", "CreatedAt", "DepartmentId", "JobTitleId", "Note", "StepOrder" },
                values: new object[,]
                {
                    { 51, 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 5, "總監核決", 2 },
                    { 52, 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 11, "取得紙本資料審核", 3 },
                    { 53, 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 7, "填入預計撥款日，核決及撥款後，填入撥款日", 4 }
                });

            // 只對實際存在於 Roles 表的 RoleId 建立權限，避免不同環境（Azure SQL）因 Role GUID 差異觸發 FK 違反
            migrationBuilder.Sql("""
                INSERT INTO RolePermissions (PermissionId, RoleId)
                SELECT v.PermissionId, v.RoleId
                FROM (VALUES
                    ('68', '3afbfc1e-4caa-4a4e-af1e-ebdc0d9002b4'),
                    ('69', '3afbfc1e-4caa-4a4e-af1e-ebdc0d9002b4'),
                    ('70', '3afbfc1e-4caa-4a4e-af1e-ebdc0d9002b4'),
                    ('68', '44e48f58-1bef-441e-bb70-a624d4f97856'),
                    ('69', '44e48f58-1bef-441e-bb70-a624d4f97856'),
                    ('70', '44e48f58-1bef-441e-bb70-a624d4f97856'),
                    ('68', 'admin'),
                    ('69', 'admin'),
                    ('70', 'admin'),
                    ('68', 'fe015c41-d9a8-48fa-994d-5588b9c4a92b'),
                    ('69', 'fe015c41-d9a8-48fa-994d-5588b9c4a92b'),
                    ('70', 'fe015c41-d9a8-48fa-994d-5588b9c4a92b'),
                    ('68', 'manager'),
                    ('69', 'manager'),
                    ('70', 'manager'),
                    ('68', 'viewer'),
                    ('69', 'viewer'),
                    ('70', 'viewer')
                ) AS v(PermissionId, RoleId)
                WHERE EXISTS (SELECT 1 FROM Roles r WHERE r.Id = v.RoleId)
                  AND NOT EXISTS (SELECT 1 FROM RolePermissions rp WHERE rp.PermissionId = v.PermissionId AND rp.RoleId = v.RoleId);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequestItems_TravelPaymentRequestId",
                table: "TravelPaymentRequestItems",
                column: "TravelPaymentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequests_ApprovalItemId",
                table: "TravelPaymentRequests",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequests_EmployeeId",
                table: "TravelPaymentRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequests_PaidByUserId",
                table: "TravelPaymentRequests",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequests_ProjectId",
                table: "TravelPaymentRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPaymentRequests_ReviewedById",
                table: "TravelPaymentRequests",
                column: "ReviewedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TravelPaymentRequestItems");

            migrationBuilder.DropTable(
                name: "TravelPaymentRequests");

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "ApprovalSteps",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "68", "3afbfc1e-4caa-4a4e-af1e-ebdc0d9002b4" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "69", "3afbfc1e-4caa-4a4e-af1e-ebdc0d9002b4" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "70", "3afbfc1e-4caa-4a4e-af1e-ebdc0d9002b4" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "68", "44e48f58-1bef-441e-bb70-a624d4f97856" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "69", "44e48f58-1bef-441e-bb70-a624d4f97856" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "70", "44e48f58-1bef-441e-bb70-a624d4f97856" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "68", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "69", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "70", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "68", "fe015c41-d9a8-48fa-994d-5588b9c4a92b" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "69", "fe015c41-d9a8-48fa-994d-5588b9c4a92b" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "70", "fe015c41-d9a8-48fa-994d-5588b9c4a92b" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "68", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "69", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "70", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "68", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "69", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "70", "viewer" });

            migrationBuilder.DeleteData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "68");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "69");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "70");

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "出差申請");

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "出差沖銷申請");
        }
    }
}
