using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendancePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "37", "attendances:read", null, "Attendances", "View Attendances" },
                    { "38", "attendances:write", null, "Attendances", "Clock In/Out" }
                });

            // 補充被 CASCADE DELETE 連帶刪除的 RolePermissions
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                values: new object[,]
                {
                    { "admin",   "37" },
                    { "admin",   "38" },
                    { "manager", "37" },
                    { "manager", "38" },
                    { "viewer",  "37" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // RolePermissions 會被 CASCADE DELETE 自動刪除，但為明確起見也手動移除
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "PermissionId" },
                keyValues: new object[] { "admin", "37" });
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "PermissionId" },
                keyValues: new object[] { "admin", "38" });
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "PermissionId" },
                keyValues: new object[] { "manager", "37" });
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "PermissionId" },
                keyValues: new object[] { "manager", "38" });
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "PermissionId" },
                keyValues: new object[] { "viewer", "37" });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "37");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "38");
        }
    }
}
