using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveAndTravelRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalItemId",
                table: "PaymentRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationType",
                table: "ApprovalItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    LeaveType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Days = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "TravelRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelRequests_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TravelRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TravelRequests_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TravelRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 1,
                column: "ApplicationType",
                value: "leave");

            migrationBuilder.UpdateData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "ApplicationType",
                value: "payment_request");

            migrationBuilder.InsertData(
                table: "LeaveRequests",
                columns: new[] { "Id", "ApprovalItemId", "ApprovalStatus", "CreatedAt", "Days", "EmployeeId", "EndDate", "LeaveType", "Reason", "ReviewNote", "ReviewedAt", "ReviewedById", "StartDate" },
                values: new object[,]
                {
                    { 1, 1, "pending", new DateTime(2026, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc), 5m, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), "annual", "個人旅遊", null, null, null, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 1, "approved", new DateTime(2026, 2, 20, 7, 0, 0, 0, DateTimeKind.Utc), 2m, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "sick", "身體不適就醫", "核准", new DateTime(2026, 2, 20, 8, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 1, "rejected", new DateTime(2026, 2, 13, 0, 0, 0, 0, DateTimeKind.Utc), 1m, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), "compensatory", "補休加班時數", "補休時數不足，請確認後重新申請", new DateTime(2026, 2, 14, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 1,
                column: "ApprovalItemId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 2,
                column: "ApprovalItemId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 3,
                column: "ApprovalItemId",
                value: 2);

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "28", "leave-requests:read", null, "LeaveRequests", "View Leave Requests" },
                    { "29", "leave-requests:write", null, "LeaveRequests", "Create/Edit Leave Requests" },
                    { "30", "leave-requests:delete", null, "LeaveRequests", "Delete Leave Requests" },
                    { "31", "travel-requests:read", null, "TravelRequests", "View Travel Requests" },
                    { "32", "travel-requests:write", null, "TravelRequests", "Create/Edit Travel Requests" },
                    { "33", "travel-requests:delete", null, "TravelRequests", "Delete Travel Requests" }
                });

            migrationBuilder.InsertData(
                table: "TravelRequests",
                columns: new[] { "Id", "ApprovalItemId", "ApprovalStatus", "CreatedAt", "Destination", "EmployeeId", "EndDate", "EstimatedCost", "ProjectId", "Purpose", "ReviewNote", "ReviewedAt", "ReviewedById", "StartDate" },
                values: new object[,]
                {
                    { 1, null, "pending", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "台南", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), 3000m, 1, "客戶現場拜訪與需求確認", null, null, null, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, "approved", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), "台中", new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), 1500m, 2, "供應商工廠參訪", "核准", new DateTime(2026, 2, 24, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { "28", "admin" },
                    { "29", "admin" },
                    { "30", "admin" },
                    { "31", "admin" },
                    { "32", "admin" },
                    { "33", "admin" },
                    { "28", "manager" },
                    { "29", "manager" },
                    { "31", "manager" },
                    { "32", "manager" },
                    { "28", "viewer" },
                    { "31", "viewer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ApprovalItemId",
                table: "PaymentRequests",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalItems_ApplicationType",
                table: "ApprovalItems",
                column: "ApplicationType",
                unique: true,
                filter: "[ApplicationType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ApprovalItemId",
                table: "LeaveRequests",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ReviewedById",
                table: "LeaveRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_ApprovalItemId",
                table: "TravelRequests",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_EmployeeId",
                table: "TravelRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_ProjectId",
                table: "TravelRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_ReviewedById",
                table: "TravelRequests",
                column: "ReviewedById");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRequests_ApprovalItems_ApprovalItemId",
                table: "PaymentRequests",
                column: "ApprovalItemId",
                principalTable: "ApprovalItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_ApprovalItems_ApprovalItemId",
                table: "PaymentRequests");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_ApprovalItemId",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalItems_ApplicationType",
                table: "ApprovalItems");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "28", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "29", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "30", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "31", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "32", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "33", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "28", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "29", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "31", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "32", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "28", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "31", "viewer" });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "28");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "29");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "30");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "31");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "32");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "33");

            migrationBuilder.DropColumn(
                name: "ApprovalItemId",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "ApplicationType",
                table: "ApprovalItems");
        }
    }
}
