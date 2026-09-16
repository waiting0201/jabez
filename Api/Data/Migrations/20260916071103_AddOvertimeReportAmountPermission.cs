using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 加班紀錄報表的「加班費」欄改為欄位級權限：新增 reports-overtime:amount(1)。
    ///
    /// 加班費金額依核准當下底薪試算，屬薪資性資訊，與「能否進入報表頁」(reports-overtime:read)
    /// 刻意分離 —— 只開給財務承辦與總監。GET /reports/overtime 是唯一能以「全公司逐筆」形式
    /// 看到他人加班費的端點（其餘皆為看自己 / 看指派給自己的單，或已受 payroll:read 管制）。
    ///
    /// Id 1 為重用的歷史空號（原 admin-access，已由 20260226113509_RemoveAdminAccessAddSettings
    /// 的 DeleteData 從 Permissions 與 RolePermissions 刪除，該 migration 早已在所有環境套用）。
    /// 目前 2~77 全滿，1 是唯一空號。刻意不取 78+ ——
    /// PermissionHandler.CreateAsync 以 max(Id)+1 配號，78 起可能已被 UI 建立的權限占用。
    ///
    /// 本檔刻意「不用」scaffold 產生的 InsertData，全部改為 raw SQL，原因同
    /// 20260808031754_AddProjectWaterLevelTotalPermission：正式環境有大量 UI 建立的自訂角色，
    /// HasData 涵蓋不到。所有語句都以 IF NOT EXISTS 包住 —— Program.cs 啟動時會自動 MigrateAsync，
    /// 這裡一拋例外整個 Function App 就起不來，寧可 no-op 也不要炸開機。
    ///
    /// ⚠ 回填策略與水位表先例「刻意相反」：本檔「不」回填 RolePermissions。
    /// 水位表當時的考量是「上線行為不變、之後再逐一取消」，但本需求的出發點就是
    /// 「金額預設沒人看得到」—— 回填等於上線後零收斂，還要靠人記得回頭取消。
    /// 故上線後由 Superadmin 在角色管理頁手動勾給財務承辦與總監的角色
    /// （不寫死角色名稱：各環境角色名不保證一致，寫死的 SQL 很可能靜默 no-op 而讓人誤以為已生效）。
    /// Superadmin 不受影響（OvertimeReportHandler.CanSeeAmount 對 is_superadmin 直接放行），
    /// 上線後仍有人能驗證金額資料正確。
    /// </summary>
    public partial class AddOvertimeReportAmountPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 權限主檔。Module 刻意「沿用 reports-overtime:read 那筆的值」而非寫死 N'Reports' ——
            // 權限管理頁與角色編輯頁都以 Module 分組，各環境的 DB 早已把報表類 Module 改為 N'統計報表'
            // （HasData 仍是 'Reports'，屬既有漂移）。跟著抄才能保證新碼與「加班紀錄」落在同一張卡片裡。
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Id = '1' OR Code = 'reports-overtime:amount')
                    INSERT INTO Permissions (Id, Code, Name, Module, Description)
                    SELECT '1', 'reports-overtime:amount', N'加班紀錄－加班費金額',
                           ISNULL((SELECT TOP 1 Module FROM Permissions
                                   WHERE Code = 'reports-overtime:read'), N'Reports'),
                           NULL;
                """);

            // 刻意「不」回填 RolePermissions，理由見上方 class doc。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 必須反向刪除：不刪的話回滾後 DB 與 snapshot 不一致，重新 Up 會撞 unique(Code)。
            // RolePermissions 先清（不倚賴 FK cascade，且上線後已由人工勾選產生資料），再刪 Permissions。
            migrationBuilder.Sql("""
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Code = 'reports-overtime:amount');

                DELETE FROM Permissions
                WHERE Code = 'reports-overtime:amount';
                """);
        }
    }
}
