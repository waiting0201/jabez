using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op：RolePermissions 已在 AddAttendancePermissions migration 中插入，
            // 此處原先重複插入導致 Error 2627（唯一鍵值衝突）
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op：對應的 Up 已無操作
        }
    }
}
