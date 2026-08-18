using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <summary>
    /// 2026-08 移除職務加給 / 主管加給 / 外派加給（Users + SalaryAdjustmentRecords 共 6 欄）。
    ///
    /// ⚠ 這是一支**刻意留空的 no-op migration**，唯一作用是讓 ModelSnapshot 追上已移除欄位的 entity。
    /// 資料庫欄位刻意「保留不 DROP」：SalaryAdjustmentRecords 上的舊金額是員工歷次調薪的稽核紀錄，
    /// DROP 下去無法回復。六個欄位皆為 nullable，程式不再讀寫它們，留著不影響任何行為。
    ///
    /// EF 原本 scaffold 出的 DropColumn 已全數移除。日後若確定要真的清掉，
    /// 另開一支明確命名的 migration，不要改這一支。
    /// </summary>
    public partial class RemoveThreeAllowancesFromModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 無操作：僅同步 ModelSnapshot，DB 欄位保留（見類別註解）
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 無操作
        }
    }
}
