using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 新增〈活動日〉的 2 個權限碼（四週彈性工時 §3.2）。
    ///
    /// 做法與 <c>AddShiftSchedulePermissions</c> 完全相同、理由亦同，重點重述：
    /// <list type="bullet">
    ///   <item><b>Id 不寫死</b>，於執行當下取 <c>max(Id)+1</c> 並逐筆重算 ——
    ///         正式站的 Id 可能已被 UI 建立的權限占用，撞 PK 會讓啟動時的
    ///         <c>MigrateAsync()</c> 拋例外、**整個 Function App 起不來**。</item>
    ///   <item><b>全部 <c>IF NOT EXISTS</c> 包住</b>，可重複執行。</item>
    ///   <item><b>Module 沿用同群組既有那筆的值</b>（正式站的報表類 Module 已漂移為 N'統計報表'）。</item>
    ///   <item><b>刻意不回填任何角色</b>：活動日只開給各部門協理，由 Superadmin 到角色管理指派，
    ///         指派後請該員重新登入（權限在 JWT claim 內）。</item>
    /// </list>
    ///
    /// ⚠ `activity-days:read` 刻意開給**全員**（排班月曆要顯示活動日標記），
    /// 真正受限的是 `:write`；能排定哪些部門另由 ProjectAccessScope 在 Handler 內把關。
    /// </summary>
    public partial class AddActivityDayPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @AttendanceModule nvarchar(100) =
                    ISNULL((SELECT TOP 1 Module FROM Permissions WHERE Code = 'shift-schedule:read'),
                           ISNULL((SELECT TOP 1 Module FROM Permissions WHERE Code = 'attendances:read'), N'出勤打卡'));

                DECLARE @NewPerms TABLE (Seq int, Code nvarchar(200), Name nvarchar(200), Module nvarchar(100), Descr nvarchar(1000));
                INSERT INTO @NewPerms (Seq, Code, Name, Module, Descr) VALUES
                    (1, 'activity-days:read',  N'活動日瀏覽', @AttendanceModule, N'檢視各部門已排定的活動日'),
                    (2, 'activity-days:write', N'活動日排定', @AttendanceModule, N'排定 / 改期活動日並勾選預定人力（各部門協理）');

                DECLARE @Seq int, @Code nvarchar(200), @Name nvarchar(200), @Module nvarchar(100), @Descr nvarchar(1000);
                DECLARE perm_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT Seq, Code, Name, Module, Descr FROM @NewPerms ORDER BY Seq;
                OPEN perm_cursor;
                FETCH NEXT FROM perm_cursor INTO @Seq, @Code, @Name, @Module, @Descr;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = @Code)
                    BEGIN
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
            migrationBuilder.Sql("""
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Code IN ('activity-days:read', 'activity-days:write'));

                DELETE FROM Permissions
                WHERE Code IN ('activity-days:read', 'activity-days:write');
                """);
        }
    }
}
