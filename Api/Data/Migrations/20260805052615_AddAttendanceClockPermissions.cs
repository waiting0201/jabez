using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 出勤打卡納入權限管理：新增 attendances:read(37) / attendances:write(38) / reports-attendance:write(42)。
    ///
    /// Id 37/38 為重用的歷史空號（2026-03 由 SyncSeedDataWithDatabase 刪除）、42 原為已刪除的 reports-leave:read。
    /// 刻意不取 78+ —— PermissionHandler.CreateAsync 以 max(Id)+1 配號，78 起可能已被 UI 建立的權限占用。
    ///
    /// 本檔刻意「不用」scaffold 產生的 InsertData，全部改為 raw SQL，原因有二：
    ///   1. seed 的角色（如員工-測試 3afbfc1e…）在部分環境已被刪除，InsertData 會撞 FK。
    ///   2. 正式環境另有大量 UI 建立的自訂角色，HasData 涵蓋不到，必須 INSERT … SELECT FROM Roles 回填。
    /// 所有語句都以 IF NOT EXISTS / NOT EXISTS 包住 —— Program.cs 啟動時會自動 MigrateAsync，
    /// 這裡一拋例外整個 Function App 就起不來，寧可 no-op 也不要炸開機。
    ///
    /// 回填策略（上線後行為與現況完全相同，差別只在「從此可管理」）：
    ///   - attendances:read / write → 所有現有角色（打卡是全員功能）
    ///   - reports-attendance:write → 只給已擁有 reports-attendance:read 的角色（精準維持「能進報表頁就能編輯」）
    /// </summary>
    public partial class AddAttendanceClockPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ① 權限主檔
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Id = '37' OR Code = 'attendances:read')
                    INSERT INTO Permissions (Id, Code, Name, Module, Description)
                    VALUES ('37', 'attendances:read', N'瀏覽', N'出勤打卡', N'出勤打卡');

                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Id = '38' OR Code = 'attendances:write')
                    INSERT INTO Permissions (Id, Code, Name, Module, Description)
                    VALUES ('38', 'attendances:write', N'打卡', N'出勤打卡', N'出勤打卡');

                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Id = '42' OR Code = 'reports-attendance:write')
                    INSERT INTO Permissions (Id, Code, Name, Module, Description)
                    VALUES ('42', 'reports-attendance:write', N'出缺勤紀錄編輯', N'Reports', NULL);
                """);

            // ② attendances:read / write → 所有現有角色（含 UI 建立的自訂角色）
            //    以 Code 而非 Id 解析 PermissionId，即使 ① 因既存資料而 no-op 也能正確掛上
            migrationBuilder.Sql("""
                INSERT INTO RolePermissions (RoleId, PermissionId)
                SELECT r.Id, p.Id
                FROM Roles r
                CROSS JOIN Permissions p
                WHERE p.Code IN ('attendances:read', 'attendances:write')
                  AND NOT EXISTS (
                        SELECT 1 FROM RolePermissions rp
                        WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);
                """);

            // ③ reports-attendance:write → 只給已擁有 reports-attendance:read 的角色
            migrationBuilder.Sql("""
                INSERT INTO RolePermissions (RoleId, PermissionId)
                SELECT rp.RoleId, pw.Id
                FROM RolePermissions rp
                JOIN Permissions pr ON pr.Id = rp.PermissionId AND pr.Code = 'reports-attendance:read'
                CROSS JOIN Permissions pw
                WHERE pw.Code = 'reports-attendance:write'
                  AND NOT EXISTS (
                        SELECT 1 FROM RolePermissions x
                        WHERE x.RoleId = rp.RoleId AND x.PermissionId = pw.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 必須反向刪除：不刪的話回滾後 DB 與 snapshot 不一致，重新 Up 會撞 unique(Code)。
            // RolePermissions 先清（不倚賴 FK cascade），再刪 Permissions。
            migrationBuilder.Sql("""
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions
                    WHERE Code IN ('attendances:read', 'attendances:write', 'reports-attendance:write'));

                DELETE FROM Permissions
                WHERE Code IN ('attendances:read', 'attendances:write', 'reports-attendance:write');
                """);
        }
    }
}
