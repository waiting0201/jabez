namespace Jabez.Api.Models.Entities;

/// <summary>
/// 個人排班的「某人某日日別」—— 四週彈性工時下「該日要不要出勤」的單一真相，
/// 取代現行以 <c>User.IsShiftWorker</c> ＋ 公司行事曆推導的全公司一致判定。
///
/// **國定假日不寫入本表**：它唯讀、不佔 4 例 4 休配額，一律由 CalendarDay 解析
/// （判準見 <see cref="Jabez.Api.Common.PublicHolidayRule"/>）。本表只存員工自己勾的三種
/// （上班日／例假日／休假日，見 <see cref="Jabez.Api.Common.WorkDayTypes"/>）。
///
/// 查無該日紀錄時的解析退回順序見 ShiftScheduleReadService：個人排班 → 國定假日 → 舊制行事曆判定。
/// </summary>
public class ShiftScheduleDay
{
    public int      Id        { get; set; }
    public Guid     UserId    { get; set; }
    public DateTime Date      { get; set; }

    /// <summary>work / statutory_off / rest_day，見 <c>WorkDayTypes</c>。</summary>
    public string   DayType   { get; set; } = Common.WorkDayTypes.Work;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
