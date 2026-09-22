namespace Jabez.Api.Common;

/// <summary>排班可編輯的四種情形。</summary>
public enum ShiftScheduleEditMode
{
    /// <summary>不可編輯。</summary>
    Closed,
    /// <summary>開放期內排次月（每月 10 日 00:00 ～ 25 日 23:59）。</summary>
    Open,
    /// <summary>當月到職者的寬限期（自帳號通知寄出起 3 個工作天，可排當月）。</summary>
    GracePeriod,
    /// <summary>僅能於當日 08:30 前調整「今天」一格（臨時調休）。</summary>
    SameDayOnly,
}

/// <param name="CanEdit">是否可編輯</param>
/// <param name="Mode">可編輯的情形</param>
/// <param name="Reason">不可編輯時的說明（可直接顯示給使用者）</param>
public sealed record ShiftScheduleEditability(bool CanEdit, ShiftScheduleEditMode Mode, string Reason);

/// <summary>
/// 「某人此刻能不能改某個月的班表」的單一真相（純函式、無 I/O）。
///
/// 規則（flexible-work-hours.md §3.5）：
///   <list type="bullet">
///     <item>每月 <b>10 日 00:00 ～ 25 日 23:59</b> 開放排定<b>次月</b>班表，期間內可修改及暫存</item>
///     <item>當月：允許於<b>當日早上 08:30 前</b>調整<b>當天</b>狀態（臨時調休），且仍受擋存判準約束</item>
///     <item>已過往之月份與日期<b>不可修改</b>，僅供歷史查詢</item>
///     <item>當月到職者另有寬限期（§3.5.3），自帳號通知寄出起 3 個<b>工作天</b></item>
///   </list>
///
/// 開放期結束後的異動一律走〈改班申請〉送簽（§3.5.2），核准後才寫入班表 —— 不在本判定範圍內。
///
/// ⚠ 「3 個工作天」需查公司行事曆，不是純運算，故 <c>graceDeadline</c> 由呼叫端算好傳入
/// （見 <c>ShiftScheduleHandler</c>）。工作天刻意以 <b>CalendarDay</b> 判定而非個人班表 ——
/// 新進同仁當月本來就沒有班表，那正是這條規則要解決的問題。
/// </summary>
public static class ShiftScheduleWindow
{
    /// <summary>開放期起始日（含）。</summary>
    public const int OpenFromDay = 10;

    /// <summary>開放期結束日（含，當日 23:59:59 截止）。</summary>
    public const int OpenToDay = 25;

    /// <summary>當日臨時調休的截止時刻。</summary>
    public static readonly TimeOnly SameDayCutoff = new(8, 30);

    /// <summary>當月到職者的寬限工作天數。</summary>
    public const int GraceWorkingDays = 3;

    public static ShiftScheduleEditability Evaluate(
        int year, int month, DateTime now, DateTime? graceDeadline = null)
    {
        var target       = new DateTime(year, month, 1);
        var currentMonth = new DateTime(now.Year, now.Month, 1);
        var nextMonth    = currentMonth.AddMonths(1);

        if (target < currentMonth)
            return new(false, ShiftScheduleEditMode.Closed, "已過往之月份不可修改，僅供歷史查詢。");

        if (target == currentMonth)
        {
            // 當月到職者的寬限期優先於「只能改今天」
            if (graceDeadline is { } deadline && now <= deadline)
                return new(true, ShiftScheduleEditMode.GracePeriod,
                    $"到職寬限期內，可排定當月班表（至 {deadline:M/d HH:mm} 止）。");

            return new(true, ShiftScheduleEditMode.SameDayOnly,
                $"當月班表已定案，僅可於當日 {SameDayCutoff:HH:mm} 前調整當天狀態；"
              + "其餘異動請提出〈改班申請〉。");
        }

        if (target == nextMonth)
        {
            if (now.Day is >= OpenFromDay and <= OpenToDay)
                return new(true, ShiftScheduleEditMode.Open,
                    $"次月排班開放中（每月 {OpenFromDay} 日 00:00 ～ {OpenToDay} 日 23:59）。");

            return new(false, ShiftScheduleEditMode.Closed,
                now.Day < OpenFromDay
                    ? $"次月排班尚未開放，將於 {now:yyyy/MM} 月 {OpenFromDay} 日開放。"
                    : $"次月排班已於 {OpenToDay} 日 23:59 截止，異動請提出〈改班申請〉。");
        }

        return new(false, ShiftScheduleEditMode.Closed, "僅開放排定次月班表。");
    }

    /// <summary>
    /// 某一格是否可改。<see cref="ShiftScheduleEditMode.SameDayOnly"/> 時只有「今天」那一格能動。
    /// </summary>
    public static bool CanEditDate(ShiftScheduleEditability editability, DateTime date, DateTime now) =>
        editability.Mode switch
        {
            ShiftScheduleEditMode.Open        => true,
            ShiftScheduleEditMode.GracePeriod => true,
            ShiftScheduleEditMode.SameDayOnly => date.Date == now.Date
                                              && TimeOnly.FromDateTime(now) < SameDayCutoff,
            _                                 => false,
        };
}
