using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuranceBrackets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsuranceBrackets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaryBracket = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    LaborInsuranceEmployee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    HealthInsuranceEmployee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceBrackets", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "InsuranceBrackets",
                columns: new[] { "Id", "CreatedAt", "HealthInsuranceEmployee", "LaborInsuranceEmployee", "SalaryBracket" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 458m, 738m, 29500m },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 470m, 758m, 30300m },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 493m, 795m, 31800m },
                    { 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 516m, 833m, 33300m },
                    { 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 540m, 870m, 34800m },
                    { 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 563m, 908m, 36300m },
                    { 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 592m, 955m, 38200m },
                    { 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 622m, 1002m, 40100m },
                    { 9, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 651m, 1050m, 42000m },
                    { 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 681m, 1098m, 43900m },
                    { 11, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 710m, 1145m, 45800m },
                    { 12, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 748m, 1145m, 48200m },
                    { 13, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 785m, 1145m, 50600m },
                    { 14, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 822m, 1145m, 53000m },
                    { 15, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 859m, 1145m, 55400m },
                    { 16, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 896m, 1145m, 57800m },
                    { 17, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 943m, 1145m, 60800m },
                    { 18, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 990m, 1145m, 63800m },
                    { 19, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1036m, 1145m, 66800m },
                    { 20, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1083m, 1145m, 69800m },
                    { 21, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1129m, 1145m, 72800m },
                    { 22, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1187m, 1145m, 76500m },
                    { 23, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1244m, 1145m, 80200m },
                    { 24, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1301m, 1145m, 83900m },
                    { 25, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1359m, 1145m, 87600m },
                    { 26, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1428m, 1145m, 92100m },
                    { 27, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1498m, 1145m, 96600m },
                    { 28, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1568m, 1145m, 101100m },
                    { 29, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1638m, 1145m, 105600m },
                    { 30, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1708m, 1145m, 110100m },
                    { 31, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1791m, 1145m, 115500m },
                    { 32, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1875m, 1145m, 120900m },
                    { 33, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1959m, 1145m, 126300m },
                    { 34, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2043m, 1145m, 131700m },
                    { 35, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2126m, 1145m, 137100m },
                    { 36, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2210m, 1145m, 142500m },
                    { 37, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2294m, 1145m, 147900m },
                    { 38, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2327m, 1145m, 150000m },
                    { 39, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2426m, 1145m, 156400m },
                    { 40, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2525m, 1145m, 162800m },
                    { 41, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2624m, 1145m, 169200m },
                    { 42, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2724m, 1145m, 175600m },
                    { 43, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2823m, 1145m, 182000m },
                    { 44, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2939m, 1145m, 189500m },
                    { 45, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3055m, 1145m, 197000m },
                    { 46, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3172m, 1145m, 204500m },
                    { 47, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3288m, 1145m, 212000m },
                    { 48, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3404m, 1145m, 219500m },
                    { 49, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3539m, 1145m, 228200m },
                    { 50, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3674m, 1145m, 236900m },
                    { 51, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3809m, 1145m, 245600m },
                    { 52, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3944m, 1145m, 254300m },
                    { 53, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4079m, 1145m, 263000m },
                    { 54, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4234m, 1145m, 273000m },
                    { 55, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4389m, 1145m, 283000m },
                    { 56, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4544m, 1145m, 293000m },
                    { 57, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4700m, 1145m, 303000m },
                    { 58, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4855m, 1145m, 313000m }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { "44", "insurance-brackets:read", null, "InsuranceBrackets", "View Insurance Brackets" },
                    { "45", "insurance-brackets:write", null, "InsuranceBrackets", "Create/Edit Insurance Brackets" },
                    { "46", "insurance-brackets:delete", null, "InsuranceBrackets", "Delete Insurance Brackets" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { "41", "admin" },
                    { "44", "admin" },
                    { "45", "admin" },
                    { "46", "admin" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceBrackets_SalaryBracket",
                table: "InsuranceBrackets",
                column: "SalaryBracket",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceBrackets");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "41", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "44", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "45", "admin" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { "46", "admin" });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "44");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "45");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "46");
        }
    }
}
