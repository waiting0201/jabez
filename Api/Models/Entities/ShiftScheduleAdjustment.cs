namespace Jabez.Api.Models.Entities;

/// <summary>
/// 「活動日覆蓋個人排班」的異動紀錄 —— 同時是右上角鈴鐺通知的來源（2026-09-29 新增）。
///
/// 主管把活動日排在某位預定人力已排定的例假／休假上時，活動日優先：
/// 系統把該日改為上班日，並把少掉的例假／休假搬到當月另一個合法的日子
/// （<see cref="Jabez.Api.Services.ActivityScheduleOverrideService"/>）。每位同仁每筆覆蓋落一列，
/// <see cref="AcknowledgedAt"/> 為 null 即為鈴鐺上的未讀。
///
/// <see cref="RelocatedTo"/> 為 null ＝**待補排**：找不到合法的日子可搬，只把衝突日改成上班日，
/// 由同仁自行補排（開放期內直接改班表、期後走〈改班申請〉）。
/// </summary>
public class ShiftScheduleAdjustment
{
    public int  Id     { get; set; }
    public Guid UserId { get; set; }

    /// <summary>觸發覆蓋的活動日；活動日被刪除後設 NULL，通知紀錄保留。</summary>
    public int? ActivityDayId { get; set; }

    /// <summary>活動名稱快照（活動日改名或刪除後仍能顯示當初的原因）。</summary>
    public string ActivityTitle { get; set; } = string.Empty;

    /// <summary>被覆蓋為上班日的日子（＝活動日）。</summary>
    public DateTime Date { get; set; }

    /// <summary>覆蓋前的日別：statutory_off / rest_day。</summary>
    public string OriginalDayType { get; set; } = string.Empty;

    /// <summary>原例假／休假被搬到哪一天；null ＝ 待補排。</summary>
    public DateTime? RelocatedTo { get; set; }

    /// <summary>同仁按下「我知道了」的時間；null ＝ 未讀。</summary>
    public DateTime? AcknowledgedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public User?        User        { get; set; }
    public ActivityDay? ActivityDay { get; set; }
}
