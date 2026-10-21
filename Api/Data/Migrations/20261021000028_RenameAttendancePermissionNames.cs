using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 角色權限表單「出勤打卡」群組原有兩個同名的「瀏覽」（打卡頁 attendances:read / 個人排班 shift-schedule:read），
    /// 改名為「打卡頁瀏覽」/「排班瀏覽」以資區別。
    ///
    /// 以 Code 定位、不用 scaffold 的 UpdateData（以 Id 定位）：shift-schedule:read 的 Id 是執行當下 max+1 配的，
    /// 各環境不同；attendances:read 一併以 Code 更新，寫法一致。查無者自然 no-op，可重複執行。
    /// </summary>
    public partial class RenameAttendancePermissionNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Permissions SET Name = N'打卡頁瀏覽' WHERE Code = 'attendances:read';
                UPDATE Permissions SET Name = N'排班瀏覽'   WHERE Code = 'shift-schedule:read';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Permissions SET Name = N'瀏覽' WHERE Code IN ('attendances:read', 'shift-schedule:read');
                """);
        }
    }
}
