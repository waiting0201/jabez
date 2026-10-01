namespace Jabez.Api.Common;

/// <summary>排班可編輯的四種情形。</summary>
public enum ShiftScheduleEditMode
{
    /// <summary>不可編輯。</summary>
    Closed,
    /// <summary>開放期內排未來月份（某月的開放期＝前兩個月 10 日 00:00 ～ 前一個月 25 日 23:59）。</summary>
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
///     <item>每個月份的開放期＝<b>前兩個月 10 日 00:00 ～ 前一個月 25 日 23:59</b>，期間內可修改及暫存
///           （2026-10-01 由「每月 10–25 日只開次月」放寬）。故每月 10–25 日可排次月＋下下月、
///           26 日至次月 9 日只可排下下月（屆時已成為次月）</item>
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
    /// <summary>開放期起始日（含）：目標月份前兩個月的這一天起開放。</summary>
    public const int OpenFromDay = 10;

    /// <summary>開放期結束日（含，當日 23:59:59 截止）：目標月份前一個月的這一天截止。</summary>
    public const int OpenToDay = 25;

    /// <summary>當日臨時調休的截止時刻。</summary>
    public static readonly TimeOnly SameDayCutoff = new(8, 30);

    /// <summary>當月到職者的寬限工作天數。</summary>
    public const int GraceWorkingDays = 3;

    /// <summary>
    /// 設定鍵 <c>App:ShiftScheduleOpenAllFutureMonths</c>（Azure App Setting <c>App__ShiftScheduleOpenAllFutureMonths</c>）。
    /// </summary>
    public const string OpenAllFutureMonthsConfigKey = "App:ShiftScheduleOpenAllFutureMonths";

    /// <summary>
    /// <b>測試用開關</b>：為 true 時，<b>所有月份</b>（過往月份、當月、未來月份）一律視為開放期，
    /// 不受「過往唯讀」「當月僅能改當天」「開放期」任何限制（供教育訓練以不同月份示範）。
    /// 設定鍵名稱沿用舊名（當初只開未來月份）以免測試站重設 App Setting。
    /// 由 <c>Program.cs</c> 於啟動時寫入；<b>正式站不得設定</b>。
    /// </summary>
    public static bool OpenAllFutureMonths { get; set; }

    public static ShiftScheduleEditability Evaluate(
        int year, int month, DateTime now, DateTime? graceDeadline = null)
    {
        var target       = new DateTime(year, month, 1);
        var currentMonth = new DateTime(now.Year, now.Month, 1);

        if (OpenAllFutureMonths)
            return new(true, ShiftScheduleEditMode.Open, "【測試模式】所有月份一律開放排班（正式站不適用）。");

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

        // 未來月份：每個月份的開放期 ＝ 前兩個月 10 日 00:00 ～ 前一個月 25 日 23:59（2026-10-01 由「只開次月」放寬）。
        // 以「提前幾個月」分流：次月開放至本月 25 日、下下月自本月 10 日起開放、更遠的月份尚未開放。
        var monthsAhead = (target.Year - currentMonth.Year) * 12 + target.Month - currentMonth.Month;
        var openFrom    = target.AddMonths(-2).AddDays(OpenFromDay - 1);   // 前兩個月 10 日
        var closeAt     = target.AddMonths(-1).AddDays(OpenToDay - 1);     // 前一個月 25 日（當日 23:59 截止）

        if (monthsAhead == 1)
        {
            if (now.Day <= OpenToDay)
                return new(true, ShiftScheduleEditMode.Open,
                    $"{target:yyyy 年 M 月}排班開放中（{openFrom:M/d} 00:00 ～ {closeAt:M/d} 23:59）。");

            return new(false, ShiftScheduleEditMode.Closed,
                $"{target:yyyy 年 M 月}排班已於 {closeAt:M/d} 23:59 截止，異動請提出〈改班申請〉。");
        }

        if (monthsAhead == 2 && now.Day >= OpenFromDay)
            return new(true, ShiftScheduleEditMode.Open,
                $"{target:yyyy 年 M 月}排班開放中（{openFrom:M/d} 00:00 ～ {closeAt:M/d} 23:59）。");

        return new(false, ShiftScheduleEditMode.Closed,
            $"{target:yyyy 年 M 月}排班尚未開放，將於 {openFrom:yyyy/M/d} 開放。");
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
