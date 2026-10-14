using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 員工管理的薪資欄位改用獨立權限碼 users:salary-read（原借用 payroll:read）。
    ///
    /// 回填：目前持有 payroll:read 的角色一律補上新碼 —— 上線前後行為不變，
    /// 之後由 Superadmin 依需要在角色管理頁取消勾選。
    ///
    /// 全為 raw SQL 且冪等（IF / NOT EXISTS）：Program.cs 啟動時會自動 MigrateAsync，
    /// 這裡拋例外會讓 Function App 起不來。
    /// Id：2~77 已全滿，優先取 78；若已被 UI 建立的權限占用則取 max(數字 Id)+1。
    /// 之後一律以 Code 解析 PermissionId，不倚賴 Id。
    /// Module 沿用同 DB 內既有 users:read 那筆的值，確保與「員工管理」同卡片。
    /// </summary>
    public partial class AddUsersSalaryReadPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = 'users:salary-read')
                BEGIN
                    DECLARE @NewId nvarchar(450) = '78';
                    IF EXISTS (SELECT 1 FROM Permissions WHERE Id = @NewId)
                        SELECT @NewId = CAST(ISNULL(MAX(TRY_CAST(Id AS int)), 0) + 1 AS nvarchar(450)) FROM Permissions;

                    INSERT INTO Permissions (Id, Code, Name, Module, Description)
                    SELECT @NewId, 'users:salary-read', N'薪資欄位檢視',
                           ISNULL((SELECT TOP 1 Module FROM Permissions WHERE Code = 'users:read'), N'員工管理'),
                           N'員工管理薪資欄位檢視';
                END
                """);

            migrationBuilder.Sql("""
                INSERT INTO RolePermissions (RoleId, PermissionId)
                SELECT rp.RoleId, pn.Id
                FROM RolePermissions rp
                JOIN Permissions po ON po.Id = rp.PermissionId AND po.Code = 'payroll:read'
                CROSS JOIN Permissions pn
                WHERE pn.Code = 'users:salary-read'
                  AND NOT EXISTS (
                        SELECT 1 FROM RolePermissions x
                        WHERE x.RoleId = rp.RoleId AND x.PermissionId = pn.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM RolePermissions
                WHERE PermissionId IN (SELECT Id FROM Permissions WHERE Code = 'users:salary-read');

                DELETE FROM Permissions WHERE Code = 'users:salary-read';
                """);
        }
    }
}
