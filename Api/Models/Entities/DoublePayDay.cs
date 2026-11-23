namespace Jabez.Api.Models.Entities;

/// <summary>
/// 加倍工資日（例：選舉投票日，2026-11 新增）。
///
/// 與國定假日的差別：**不改變任何人的日別** —— 當天照個人排班走（上班日照常打卡、休假日需加班單、
/// 例假日全鎖），只在薪資上對「當天有上班打卡」者加發 1 日日薪，併入「國定假日出勤加倍工資」一行。
/// 休假日加班者的加班單**不計入**（照一般休假日計酬）。
///
/// ⚠ 刻意獨立於 <see cref="CalendarDay"/>：行事曆有名稱就會被 <c>PublicHolidayRule</c> 判成國定假日、
/// 蓋過個人排班；且 <c>ImportYearAsync</c> 是整年 RemoveRange 重建，寫在行事曆上會被匯入洗掉。
/// </summary>
public class DoublePayDay
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    /// <summary>建立者（不建 FK，免除刪使用者時的清洗）。</summary>
    public Guid? CreatedById { get; set; }
}
