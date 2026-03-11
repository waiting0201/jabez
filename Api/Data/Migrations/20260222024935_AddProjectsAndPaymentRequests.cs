using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsAndPaymentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BusinessAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GoogleDriveUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    SubmittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Users_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentRequestId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_PaymentRequests_PaymentRequestId",
                        column: x => x.PaymentRequestId,
                        principalTable: "PaymentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "20", "projects:read", null, "Projects", "View Projects" },
                    { "21", "projects:write", null, "Projects", "Create/Edit Projects" },
                    { "22", "projects:delete", null, "Projects", "Delete Projects" },
                    { "23", "payment-requests:read", null, "PaymentRequests", "View Payment Requests" },
                    { "24", "payment-requests:write", null, "PaymentRequests", "Create/Edit Payment Requests" },
                    { "25", "payment-requests:delete", null, "PaymentRequests", "Delete Payment Requests" },
                    { "26", "approval-tasks:read", null, "ApprovalTasks", "View Approval Tasks" },
                    { "27", "approval-tasks:write", null, "ApprovalTasks", "Review Approval Tasks" }
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "ActualAmount", "BudgetAmount", "BusinessAmount", "Code", "CreatedAt", "DepartmentId", "GoogleDriveUrl" },
                values: new object[,]
                {
                    { 1, 480000m, 500000m, 450000m, "P2024-001", new DateTime(2024, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "https://drive.google.com/drive/folders/example1" },
                    { 2, 0m, 1200000m, 1100000m, "P2024-002", new DateTime(2024, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), 2, "https://drive.google.com/drive/folders/example2" },
                    { 3, 280000m, 300000m, 250000m, "P2025-001", new DateTime(2025, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, "https://drive.google.com/drive/folders/example3" }
                });

            migrationBuilder.InsertData(
                table: "PaymentRequests",
                columns: new[] { "Id", "ApprovalStatus", "CreatedAt", "ProjectId", "ReviewNote", "ReviewedAt", "ReviewedById", "SubmittedById", "TotalAmount", "Type" },
                values: new object[,]
                {
                    { 1, "approved", new DateTime(2024, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, "符合請款規定", new DateTime(2024, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), 23500m, "vendor" },
                    { 2, "pending", new DateTime(2024, 7, 5, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, null, new Guid("33333333-3333-3333-3333-333333333333"), 5180m, "travel" },
                    { 3, "rejected", new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "金額超出預算上限，請重新提交", new DateTime(2025, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), 20000m, "advance" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { "20", "admin" },
                    { "21", "admin" },
                    { "22", "admin" },
                    { "23", "admin" },
                    { "24", "admin" },
                    { "25", "admin" },
                    { "26", "admin" },
                    { "27", "admin" },
                    { "20", "manager" },
                    { "23", "manager" },
                    { "24", "manager" },
                    { "26", "manager" },
                    { "27", "manager" },
                    { "20", "viewer" },
                    { "23", "viewer" },
                    { "26", "viewer" }
                });

            migrationBuilder.InsertData(
                table: "InvoiceItems",
                columns: new[] { "Id", "Amount", "FileName", "InvoiceNo", "PaymentRequestId" },
                values: new object[,]
                {
                    { 1, 15000m, "invoice_001.jpg", "AB-12345678", 1 },
                    { 2, 8500m, "invoice_002.jpg", "CD-87654321", 1 },
                    { 3, 4200m, "receipt_hotel.jpg", "EF-11223344", 2 },
                    { 4, 980m, "receipt_train.jpg", "GH-55667788", 2 },
                    { 5, 20000m, "advance_001.jpg", "IJ-99887766", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_PaymentRequestId",
                table: "InvoiceItems",
                column: "PaymentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ProjectId",
                table: "PaymentRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ReviewedById",
                table: "PaymentRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_SubmittedById",
                table: "PaymentRequests",
                column: "SubmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DepartmentId",
                table: "Projects",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "PaymentRequests");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "20", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "21", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "22", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "23", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "24", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "25", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "26", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "27", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "20", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "23", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "24", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "26", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "27", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "20", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "23", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "26", "viewer" });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "20");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "21");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "22");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "23");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "24");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "25");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "26");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "27");
        }
    }
}
