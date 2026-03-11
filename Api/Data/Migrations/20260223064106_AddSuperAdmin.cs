using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AgentUserId", "Avatar", "BaseSalary", "CreatedAt", "DepartmentId", "Email", "HireDate", "IsSuperAdmin", "JobTitleId", "Name", "PasswordHash", "ResignDate", "Status", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, null, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "sa@system.local", null, true, null, "System Admin", "$2a$11$rBnRYkdyRVRmk5V.p8PoNuXHxUZIpXe.G/j.Q/rlOH0jMNJxqm5Fy", null, "active", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                table: "Users");
        }
    }
}
