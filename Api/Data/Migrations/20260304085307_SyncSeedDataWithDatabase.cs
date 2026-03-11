using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncSeedDataWithDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "InvoiceItems",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "LeaveRequests",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "LeaveRequests",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "LeaveRequests",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "OvertimeRequests",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "OvertimeRequests",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "10", "admin" });

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
                keyValues: new object[] { "5", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "6", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "7", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "8", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "9", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "11", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "14", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "17", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "20", "manager" });

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
                keyValues: new object[] { "39", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "41", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "47", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "48", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "49", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "5", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "50", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "11", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "14", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "17", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "2", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "20", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "26", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "37", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "5", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "8", "viewer" });

            migrationBuilder.DeleteData(
                table: "TravelRequests",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TravelRequests",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PaymentRequests",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "37");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "38");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "10",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "權限管理", "權限管理", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "11",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "部門管理", "部門管理", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "12",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "部門管理", "部門管理", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "13",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "部門管理", "部門管理", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "14",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "職稱管理", "職稱管理", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "15",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "職稱管理", "職稱管理", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "16",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "職稱管理", "職稱管理", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "17",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "簽核管理", "簽核管理", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "18",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "簽核管理", "簽核管理", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "19",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "簽核管理", "簽核管理", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "2",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "員工管理", "員工管理", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "20",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "專案管理", "專案管理", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "21",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "專案管理", "專案管理", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "22",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "專案管理", "專案管理", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "23",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "請款申請", "請款申請", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "24",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "請款申請", "請款申請", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "25",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "請款申請", "請款申請", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "26",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "簽核作業", "簽核作業", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "27",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "簽核作業", "簽核作業", "審核" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "28",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "請假申請", "請假申請", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "29",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "請假申請", "請假申請", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "3",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "員工管理", "員工管理", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "30",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "請假申請", "請假申請", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "31",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "出差申請", "出差申請", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "32",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "出差申請", "出差申請", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "33",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "出差申請", "出差申請", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "34",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "加班申請", "加班申請", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "35",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "加班申請", "加班申請", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "36",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "加班申請", "加班申請", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "39",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "系統設定", "系統設定", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "4",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "員工管理", "員工管理", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "40",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "系統設定", "系統設定", "編輯" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "41",
                column: "Name",
                value: "出缺勤紀錄");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "44",
                columns: new[] { "Module", "Name" },
                values: new object[] { "勞健保級距", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "45",
                columns: new[] { "Module", "Name" },
                values: new object[] { "勞健保級距", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "46",
                columns: new[] { "Module", "Name" },
                values: new object[] { "勞健保級距", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "47",
                columns: new[] { "Module", "Name" },
                values: new object[] { "人事薪資", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "48",
                column: "Name",
                value: "加班紀錄");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "49",
                column: "Name",
                value: "請款統計");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "5",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "角色管理", "角色管理", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "50",
                column: "Name",
                value: "專案水位表");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "6",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "角色管理", "角色管理", "新增/修改" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "7",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "角色管理", "角色管理", "刪除" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "8",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "權限管理", "權限管理", "瀏覽" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "9",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { "權限管理", "權限管理", "新增/修改" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { "25", "manager" },
                    { "30", "manager" },
                    { "33", "manager" },
                    { "36", "manager" },
                    { "24", "viewer" },
                    { "25", "viewer" },
                    { "29", "viewer" },
                    { "30", "viewer" },
                    { "32", "viewer" },
                    { "33", "viewer" },
                    { "35", "viewer" },
                    { "36", "viewer" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "BaseSalary", "HireDate" },
                values: new object[] { 60000m, new DateTime(2023, 12, 28, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "HireDate",
                value: new DateTime(2024, 2, 9, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "HireDate", "Status" },
                values: new object[] { new DateTime(2024, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), "active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "25", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "30", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "33", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "36", "manager" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "24", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "25", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "29", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "30", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "32", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "33", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "35", "viewer" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "36", "viewer" });

            migrationBuilder.InsertData(
                table: "LeaveRequests",
                columns: new[] { "Id", "ApprovalItemId", "ApprovalStatus", "CreatedAt", "CurrentStepOrder", "Days", "EmployeeId", "EndDate", "LeaveType", "Reason", "ReviewNote", "ReviewedAt", "ReviewedById", "StartDate" },
                values: new object[,]
                {
                    { 1, 1, "pending", new DateTime(2026, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, 5m, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), "annual", "個人旅遊", null, null, null, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 1, "approved", new DateTime(2026, 2, 20, 7, 0, 0, 0, DateTimeKind.Utc), 1, 2m, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 2, 21, 0, 0, 0, 0, DateTimeKind.Utc), "sick", "身體不適就醫", "核准", new DateTime(2026, 2, 20, 8, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 1, "rejected", new DateTime(2026, 2, 13, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1m, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), "compensatory", "補休加班時數", "補休時數不足，請確認後重新申請", new DateTime(2026, 2, 14, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "OvertimeRequests",
                columns: new[] { "Id", "ApprovalItemId", "ApprovalStatus", "CreatedAt", "CurrentStepOrder", "EmployeeId", "EstimatedHours", "OvertimeDate", "ProjectIds", "Reason", "ReviewNote", "ReviewedAt", "ReviewedById" },
                values: new object[,]
                {
                    { 1, 3, "approved", new DateTime(2026, 2, 24, 10, 0, 0, 0, DateTimeKind.Utc), 1, new Guid("22222222-2222-2222-2222-222222222222"), 3m, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), "1,3", "專案趕工，需加班完成模組開發", "核准", new DateTime(2026, 2, 24, 16, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111") },
                    { 2, 3, "pending", new DateTime(2026, 2, 25, 9, 0, 0, 0, DateTimeKind.Utc), 1, new Guid("33333333-3333-3333-3333-333333333333"), 2m, new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), "2", "客戶報告截止日前需完成", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "PaymentRequests",
                columns: new[] { "Id", "ApprovalItemId", "ApprovalStatus", "CreatedAt", "CurrentStepOrder", "PaidAt", "ProjectId", "ReviewNote", "ReviewedAt", "ReviewedById", "SubmittedById", "TotalAmount", "Type" },
                values: new object[,]
                {
                    { 1, 2, "approved", new DateTime(2024, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, 1, "符合請款規定", new DateTime(2024, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), 23500m, "vendor" },
                    { 2, 2, "pending", new DateTime(2024, 7, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, 2, null, null, null, new Guid("33333333-3333-3333-3333-333333333333"), 5180m, "travel" },
                    { 3, 2, "rejected", new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, 3, "金額超出預算上限，請重新提交", new DateTime(2025, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), 20000m, "advance" }
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "10",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Permissions", "Delete Permissions" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "11",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Departments", "View Departments" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "12",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Departments", "Create/Edit Departments" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "13",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Departments", "Delete Departments" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "14",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "JobTitles", "View Job Titles" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "15",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "JobTitles", "Create/Edit Job Titles" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "16",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "JobTitles", "Delete Job Titles" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "17",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Approvals", "View Approvals" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "18",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Approvals", "Create/Edit Approvals" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "19",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Approvals", "Delete Approvals" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "2",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Users", "View Users" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "20",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Projects", "View Projects" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "21",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Projects", "Create/Edit Projects" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "22",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Projects", "Delete Projects" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "23",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "PaymentRequests", "View Payment Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "24",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "PaymentRequests", "Create/Edit Payment Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "25",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "PaymentRequests", "Delete Payment Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "26",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "ApprovalTasks", "View Approval Tasks" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "27",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "ApprovalTasks", "Review Approval Tasks" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "28",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "LeaveRequests", "View Leave Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "29",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "LeaveRequests", "Create/Edit Leave Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "3",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Users", "Create/Edit Users" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "30",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "LeaveRequests", "Delete Leave Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "31",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "TravelRequests", "View Travel Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "32",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "TravelRequests", "Create/Edit Travel Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "33",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "TravelRequests", "Delete Travel Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "34",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "OvertimeRequests", "View Overtime Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "35",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "OvertimeRequests", "Create/Edit Overtime Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "36",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "OvertimeRequests", "Delete Overtime Requests" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "39",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Settings", "View Settings" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "4",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Users", "Delete Users" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "40",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Settings", "Edit Settings" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "41",
                column: "Name",
                value: "View Attendance Report");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "44",
                columns: new[] { "Module", "Name" },
                values: new object[] { "InsuranceBrackets", "View Insurance Brackets" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "45",
                columns: new[] { "Module", "Name" },
                values: new object[] { "InsuranceBrackets", "Create/Edit Insurance Brackets" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "46",
                columns: new[] { "Module", "Name" },
                values: new object[] { "InsuranceBrackets", "Delete Insurance Brackets" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "47",
                columns: new[] { "Module", "Name" },
                values: new object[] { "Payroll", "View Payroll" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "48",
                column: "Name",
                value: "View Overtime Report");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "49",
                column: "Name",
                value: "View Payment Report");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "5",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Roles", "View Roles" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "50",
                column: "Name",
                value: "View Project Water Level Report");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "6",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Roles", "Create/Edit Roles" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "7",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Roles", "Delete Roles" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "8",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Permissions", "View Permissions" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "9",
                columns: new[] { "Description", "Module", "Name" },
                values: new object[] { null, "Permissions", "Create/Edit Permissions" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "37", "attendances:read", null, "Attendances", "View Attendances" },
                    { "38", "attendances:write", null, "Attendances", "Clock In/Out" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { "10", "admin" },
                    { "5", "admin" },
                    { "6", "admin" },
                    { "7", "admin" },
                    { "8", "admin" },
                    { "9", "admin" },
                    { "11", "manager" },
                    { "14", "manager" },
                    { "17", "manager" },
                    { "20", "manager" },
                    { "39", "manager" },
                    { "41", "manager" },
                    { "47", "manager" },
                    { "48", "manager" },
                    { "49", "manager" },
                    { "5", "manager" },
                    { "50", "manager" },
                    { "11", "viewer" },
                    { "14", "viewer" },
                    { "17", "viewer" },
                    { "2", "viewer" },
                    { "20", "viewer" },
                    { "26", "viewer" },
                    { "5", "viewer" },
                    { "8", "viewer" }
                });

            migrationBuilder.InsertData(
                table: "TravelRequests",
                columns: new[] { "Id", "ApprovalItemId", "ApprovalStatus", "CreatedAt", "CurrentStepOrder", "Destination", "EmployeeId", "EndDate", "EstimatedCost", "ProjectId", "Purpose", "ReviewNote", "ReviewedAt", "ReviewedById", "StartDate" },
                values: new object[,]
                {
                    { 1, null, "pending", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "台南", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), 3000m, 1, "客戶現場拜訪與需求確認", null, null, null, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, "approved", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), 1, "台中", new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc), 1500m, 2, "供應商工廠參訪", "核准", new DateTime(2026, 2, 24, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "BaseSalary", "HireDate" },
                values: new object[] { 80000m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "HireDate",
                value: new DateTime(2024, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "HireDate", "Status" },
                values: new object[] { new DateTime(2024, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), "inactive" });

            migrationBuilder.InsertData(
                table: "InvoiceItems",
                columns: new[] { "Id", "Amount", "FileName", "FileUrl", "InvoiceNo", "PaymentRequestId" },
                values: new object[,]
                {
                    { 1, 15000m, "invoice_001.jpg", null, "AB-12345678", 1 },
                    { 2, 8500m, "invoice_002.jpg", null, "CD-87654321", 1 },
                    { 3, 4200m, "receipt_hotel.jpg", null, "EF-11223344", 2 },
                    { 4, 980m, "receipt_train.jpg", null, "GH-55667788", 2 },
                    { 5, 20000m, "advance_001.jpg", null, "IJ-99887766", 3 }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { "37", "admin" },
                    { "38", "admin" },
                    { "37", "manager" },
                    { "38", "manager" },
                    { "37", "viewer" }
                });
        }
    }
}
