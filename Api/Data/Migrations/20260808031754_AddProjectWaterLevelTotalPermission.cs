using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 專案水位表的「總專案水位」欄改為欄位級權限：新增 reports-project-water-level:total(43)。
    ///
    /// 總水位 = (已動支 + 導入前已使用) ÷ 契約金額，分母含公司保留的 40%，屬管理層資訊，
    /// 與「能否進入報表頁」(reports-project-water-level:read) 刻意分離。
    /// Id 43 為重用的歷史空號（介於 42 / 44 之間）。刻意不取 78+ ——
    /// PermissionHandler.CreateAsync 以 max(Id)+1 配號，78 起可能已被 UI 建立的權限占用。
    ///
    /// 本檔刻意「不用」scaffold 產生的 InsertData，全部改為 raw SQL，原因同
    /// 20260805052615_AddAttendanceClockPermissions：正式環境有大量 UI 建立的自訂角色，
    /// HasData 涵蓋不到，必須 INSERT … SELECT 回填。
    /// 所有語句都以 IF NOT EXISTS / NOT EXISTS 包住 —— Program.cs 啟動時會自動 MigrateAsync，
    /// 這裡一拋例外整個 Function App 就起不來，寧可 no-op 也不要炸開機。
    ///
    /// 回填策略：只給已擁有 reports-project-water-level:read 的角色 ——
    /// 上線後行為與現況完全相同（看得到水位表的人照樣看得到總水位欄），
    /// 之後由管理員在角色管理頁逐一取消，才開始真正收斂。
    /// </summary>
    public partial class AddProjectWaterLevelTotalPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ① 權限主檔
            //    Module 刻意「沿用 reports-project-water-level:read 那筆的值」而非寫死 N'Reports' ——
            //    權限管理頁與角色編輯頁都以 Module 分組，各環境的 DB 早已把報表類 Module 改為 N'統計報表'
            //    （HasData 仍是 'Reports'，屬既有漂移）。跟著抄才能保證新碼與專案水位表落在同一張卡片裡。
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Id = '43' OR Code = 'reports-project-water-level:total')
                    INSERT INTO Permissions (Id, Code, Name, Module, Description)
                    SELECT '43', 'reports-project-water-level:total', N'專案水位表－總水位',
                           ISNULL((SELECT TOP 1 Module FROM Permissions
                                   WHERE Code = 'reports-project-water-level:read'), N'Reports'),
                           NULL;
                """);

            // ② 回填：只給已擁有 reports-project-water-level:read 的角色（含 UI 建立的自訂角色）
            //    以 Code 而非 Id 解析 PermissionId，即使 ① 因既存資料而 no-op 也能正確掛上
            migrationBuilder.Sql("""
                INSERT INTO RolePermissions (RoleId, PermissionId)
                SELECT rp.RoleId, pt.Id
                FROM RolePermissions rp
                JOIN Permissions pr ON pr.Id = rp.PermissionId AND pr.Code = 'reports-project-water-level:read'
                CROSS JOIN Permissions pt
                WHERE pt.Code = 'reports-project-water-level:total'
                  AND NOT EXISTS (
                        SELECT 1 FROM RolePermissions x
                        WHERE x.RoleId = rp.RoleId AND x.PermissionId = pt.Id);
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
                    SELECT Id FROM Permissions WHERE Code = 'reports-project-water-level:total');

                DELETE FROM Permissions
                WHERE Code = 'reports-project-water-level:total';
                """);
        }
    }
}
