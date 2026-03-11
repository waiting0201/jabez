using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportPermissionsByType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "41",
                columns: new[] { "Code", "Name" },
                values: new object[] { "reports-attendance:read", "View Attendance Report" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "42", "reports-leave:read", null, "Reports", "View Leave Report" },
                    { "43", "reports-overtime:read", null, "Reports", "View Overtime Report" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "42");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "43");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "41",
                columns: new[] { "Code", "Name" },
                values: new object[] { "reports:read", "View Reports" });
        }
    }
}
