using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Projects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.CreateTable(
                name: "OvertimeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalItemId = table.Column<int>(type: "int", nullable: true),
                    OvertimeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProjectIds = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OvertimeRequests_ApprovalItems_ApprovalItemId",
                        column: x => x.ApprovalItemId,
                        principalTable: "ApprovalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OvertimeRequests_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OvertimeRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "date", nullable: false),
                    ClockInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockInLatitude = table.Column<double>(type: "float", nullable: true),
                    ClockInLongitude = table.Column<double>(type: "float", nullable: true),
                    ClockOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockOutLatitude = table.Column<double>(type: "float", nullable: true),
                    ClockOutLongitude = table.Column<double>(type: "float", nullable: true),
                    OvertimeStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OvertimeStartLatitude = table.Column<double>(type: "float", nullable: true),
                    OvertimeStartLongitude = table.Column<double>(type: "float", nullable: true),
                    OvertimeEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OvertimeEndLatitude = table.Column<double>(type: "float", nullable: true),
                    OvertimeEndLongitude = table.Column<double>(type: "float", nullable: true),
                    OvertimeRequestId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_OvertimeRequests_OvertimeRequestId",
                        column: x => x.OvertimeRequestId,
                        principalTable: "OvertimeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ApprovalItems",
                columns: new[] { "Id", "ApplicationType", "Code", "CreatedAt", "Description", "IsActive", "Name" },
                values: new object[] { 3, "overtime", "overtime_request", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "加班申請" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "34", "overtime-requests:read", null, "OvertimeRequests", "View Overtime Requests" },
                    { "35", "overtime-requests:write", null, "OvertimeRequests", "Create/Edit Overtime Requests" },
                    { "36", "overtime-requests:delete", null, "OvertimeRequests", "Delete Overtime Requests" },
                    { "37", "attendances:read", null, "Attendances", "View Attendance Records" },
                    { "38", "attendances:write", null, "Attendances", "Clock In/Out" }
                });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                column: "Status",
                value: "closed");

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 2,
                column: "Status",
                value: "active");

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 3,
                column: "Status",
                value: "active");

            migrationBuilder.InsertData(
                table: "OvertimeRequests",
                columns: new[] { "Id", "ApprovalItemId", "ApprovalStatus", "CreatedAt", "CurrentStepOrder", "EmployeeId", "EstimatedHours", "OvertimeDate", "ProjectIds", "Reason", "ReviewNote", "ReviewedAt", "ReviewedById" },
                values: new object[,]
                {
                    { 1, 3, "approved", new DateTime(2026, 2, 24, 10, 0, 0, 0, DateTimeKind.Utc), 1, new Guid("22222222-2222-2222-2222-222222222222"), 3m, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), "1,3", "專案趕工，需加班完成模組開發", "核准", new DateTime(2026, 2, 24, 16, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111") },
                    { 2, 3, "pending", new DateTime(2026, 2, 25, 9, 0, 0, 0, DateTimeKind.Utc), 1, new Guid("33333333-3333-3333-3333-333333333333"), 2m, new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "2", "客戶報告截止日前需完成", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { "34", "admin" },
                    { "35", "admin" },
                    { "36", "admin" },
                    { "37", "admin" },
                    { "38", "admin" },
                    { "34", "manager" },
                    { "35", "manager" },
                    { "37", "manager" },
                    { "38", "manager" },
                    { "34", "viewer" },
                    { "37", "viewer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_OvertimeRequestId",
                table: "AttendanceRecords",
                column: "OvertimeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_UserId_RecordDate",
                table: "AttendanceRecords",
                columns: new[] { "UserId", "RecordDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_ApprovalItemId",
                table: "OvertimeRequests",
                column: "ApprovalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_EmployeeId",
                table: "OvertimeRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_ReviewedById",
                table: "OvertimeRequests",
                column: "ReviewedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "OvertimeRequests");

            migrationBuilder.DeleteData(
                table: "ApprovalItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "34", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "35", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "36", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "37", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "38", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "34", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "35", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "37", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "38", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "34", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "37", "viewer" });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "34");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "35");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "36");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "37");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "38");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Projects");
        }
    }
}
