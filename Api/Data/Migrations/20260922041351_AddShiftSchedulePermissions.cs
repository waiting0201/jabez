using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 新增四週彈性工時〈個人排班〉的 4 個權限碼。
    ///
    /// ── 為何用 raw SQL 而不是 scaffold 的 InsertData / PermissionConfiguration.HasData ──
    ///
    /// ① **Id 不能寫死**。Permissions.Id 是 nvarchar，現行 1–77 已全滿、無歷史空號可重用
    ///    （1 與 43 已分別被 reports-overtime:amount 與 reports-project-water-level:total 用掉）。
    ///    而 PermissionHandler.CreateAsync 以 max(Id)+1 配號，正式站的 78+ 很可能已被
    ///    Superadmin 從 UI 建立的權限占用 —— 寫死就撞 PK，
    ///    而 Program.cs 啟動時無條件 MigrateAsync()，**這裡一拋例外整個 Function App 就起不來**。
    ///    故改以 Code 為準、Id 於執行當下取 max+1，並逐筆重算（一次插 4 筆，不能共用同一個 max）。
    ///    代價：這 4 筆不受 EF seed 管理（與 UI 建立的權限相同），刻意不加進 HasData 以免 snapshot 對不上。
    ///
    /// ② **全部語句 IF NOT EXISTS 包住**，理由同上：失敗即全站掛掉，寧可 no-op。本檔可重複執行。
    ///
    /// ③ **Module 不寫死**。正式站 DB 的報表類 Module 已被改成 N'統計報表'，
    ///    而 HasData 仍是 'Reports' —— 一律沿用同群組既有那筆的值，避免在後台分組裡多出一個孤兒群組。
    ///
    /// ── 回填策略：**刻意不回填任何角色**（預設全關）──
    /// 與水位表先例（20260808031754 回填 :read 持有者）相反，比照加班費金額碼
    /// （20260916071103）的做法：排班是新制度，上線時誰該看得到由 Superadmin 到
    /// 角色管理逐一指派，指派後請該員**重新登入**（權限在 JWT claim 內）。
    /// 也不寫死角色名稱 —— 各環境角色名不保證一致，寫死的 SQL 很可能靜默 no-op。
    /// </summary>
    public partial class AddShiftSchedulePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- 出勤管理群組（沿用 attendances:read 的 Module；查不到才退回字面值）
                DECLARE @AttendanceModule nvarchar(100) =
                    ISNULL((SELECT TOP 1 Module FROM Permissions WHERE Code = 'attendances:read'), N'出勤打卡');
                -- 報表群組（沿用 reports-overtime:read 的 Module，正式站可能是 N'統計報表'）
                DECLARE @ReportModule nvarchar(100) =
                    ISNULL((SELECT TOP 1 Module FROM Permissions WHERE Code = 'reports-overtime:read'), N'Reports');

                DECLARE @NewPerms TABLE (Seq int, Code nvarchar(200), Name nvarchar(200), Module nvarchar(100), Descr nvarchar(1000));
                INSERT INTO @NewPerms (Seq, Code, Name, Module, Descr) VALUES
                    (1, 'shift-schedule:read',         N'瀏覽',           @AttendanceModule, N'檢視個人排班月曆'),
                    (2, 'shift-schedule:write',        N'排班',           @AttendanceModule, N'排定 / 修改自己的班表（例假日、休假日）'),
                    (3, 'shift-schedule:view-all',     N'檢視全公司排班', @AttendanceModule, N'不受部門可見性限制，可檢視全公司排班狀態'),
                    (4, 'reports-shift-schedule:read', N'出勤排休總覽表', @ReportModule,     N'檢視全員當月預排出勤狀態表');

                DECLARE @Seq int, @Code nvarchar(200), @Name nvarchar(200), @Module nvarchar(100), @Descr nvarchar(1000);
                DECLARE perm_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT Seq, Code, Name, Module, Descr FROM @NewPerms ORDER BY Seq;
                OPEN perm_cursor;
                FETCH NEXT FROM perm_cursor INTO @Seq, @Code, @Name, @Module, @Descr;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = @Code)
                    BEGIN
                        -- 逐筆重算 max+1：一次插 4 筆不能共用同一個 max，否則第 2 筆起撞 PK。
                        -- TRY_CAST 讓非數字 Id（若有人手動建過）不會讓整句爆掉。
                        DECLARE @NextId int =
                            ISNULL((SELECT MAX(TRY_CAST(Id AS int)) FROM Permissions), 0) + 1;

                        INSERT INTO Permissions (Id, Code, Name, Module, Description)
                        VALUES (CAST(@NextId AS nvarchar(20)), @Code, @Name, @Module, @Descr);
                    END
                    FETCH NEXT FROM perm_cursor INTO @Seq, @Code, @Name, @Module, @Descr;
                END
                CLOSE perm_cursor;
                DEALLOCATE perm_cursor;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 必須反向刪兩張表：不刪則回滾後 DB 與 snapshot 不一致，重新 Up 會撞 unique(Code)。
            migrationBuilder.Sql("""
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions
                    WHERE Code IN ('shift-schedule:read', 'shift-schedule:write',
                                   'shift-schedule:view-all', 'reports-shift-schedule:read'));

                DELETE FROM Permissions
                WHERE Code IN ('shift-schedule:read', 'shift-schedule:write',
                               'shift-schedule:view-all', 'reports-shift-schedule:read');
                """);
        }
    }
}
