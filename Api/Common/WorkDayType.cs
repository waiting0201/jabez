namespace Jabez.Api.Common;

/// <summary>
/// 四週彈性工時的「某人某日的日別」—— 取代現行 <c>WorkCalendarHelper</c> 三個方法的
/// <c>bool ignoreHolidays</c> 二元旗標（語意是全有全無，表達不了個人排例／休）。
///
/// 四值互斥，涵蓋全部情形：
///   <list type="bullet">
///     <item><see cref="Work"/> 上班日 —— 應出勤，加班走平日級距</item>
///     <item><see cref="RestDay"/> 休假日（勞基法「休息日」）—— 免出勤，出勤須先有核准加班單，走假日級距</item>
///     <item><see cref="StatutoryOff"/> 例假日 —— **依法嚴禁出勤**，打卡與加班申請全鎖（罰鍰 2 萬～100 萬）</item>
///     <item><see cref="PublicHoliday"/> 國定假日 —— 員工不可勾選、不佔 4 例 4 休配額；
///           出勤前 8 小時加發 1 日日薪、第 9 小時起走**平日**級距</item>
///   </list>
///
/// ⚠ 「主管排定的活動日」**不是第 5 種日別**，是壓在日別之上的疊加旗標（見 ActivityDay）。
/// 做成第 5 種會讓自動排班的「跳過活動日」與配額計算互相打架。
///
/// 字面值直接存進 <c>ShiftScheduleDay.DayType</c>，故**不可任意改字串**。
/// </summary>
public static class WorkDayTypes
{
    /// <summary>上班日。</summary>
    public const string Work = "work";

    /// <summary>休假日（休息日）。員工每月自排 4 天（31 日曆月 5 天）。</summary>
    public const string RestDay = "rest_day";

    /// <summary>例假日。員工每月自排 4 天，依法嚴禁出勤。</summary>
    public const string StatutoryOff = "statutory_off";

    /// <summary>
    /// 國定假日。**不由員工勾選、不寫進 ShiftScheduleDay**，一律由公司行事曆解析。
    /// 判準見 <see cref="PublicHolidayRule"/>。
    /// </summary>
    public const string PublicHoliday = "public_holiday";

    /// <summary>員工可自行勾選的三種（國定假日唯讀，不在其中）。</summary>
    public static readonly string[] Selectable = [Work, RestDay, StatutoryOff];

    /// <summary>占用「4 例 4 休」配額的兩種。</summary>
    public static readonly string[] QuotaBearing = [StatutoryOff, RestDay];

    public static bool IsSelectable(string? value) =>
        value is not null && Array.IndexOf(Selectable, value) >= 0;

    /// <summary>未知值一律視為上班日（安全側：寧可要求出勤，不可誤放成例假而漏排）。</summary>
    public static string Normalize(string? value) =>
        IsSelectable(value) ? value! : Work;
}

/// <summary>
/// 「某個 CalendarDay 是不是**國定假日**」的單一真相。
///
/// ⚠ 判準是「<c>IsHoliday</c> **且** <c>Description</c> 非空」，不是只看 <c>IsHoliday</c>：
/// 公司行事曆（來源 ruyut/TaiwanCalendar）把**週六日也標成 IsHoliday = 1**，只是 Description 為空；
/// 有名稱的（補假／國慶日／臺灣光復…紀念日）才是真正的國定假日。
///
/// 只看旗標的後果：四週彈性工時下週末不再天然免出勤（該不該休由個人排班決定），
/// 每個週末都變唯讀格 → 員工**永遠排不滿 4 例 4 休**（實測 2026 年 10 月 31 天有 11 天被誤丟，配額停在 0/4）。
///
/// 衍生要求：必須用 <c>ICalendarDayReadService.GetByYearAsync</c>（有 Description），
/// **不可用 GetHolidayDatesAsync**（只回日期，分不出「國慶日」與「單純的週六」）。
/// 月曆讀取（唯讀格）、整月寫入（丟棄該日）、配額重算三個消費點**必須共用本方法**，各寫各的必然漂移。
/// </summary>
public static class PublicHolidayRule
{
    public static bool IsPublicHoliday(bool isHoliday, string? description) =>
        isHoliday && !string.IsNullOrWhiteSpace(description);
}

/// <summary>日別的中文名稱（通知文案、簽核摘要共用）。</summary>
public static class WorkDayTypeNames
{
    public static string GetZh(string? dayType) => dayType switch
    {
        WorkDayTypes.Work          => "上班日",
        WorkDayTypes.RestDay       => "休假日",
        WorkDayTypes.StatutoryOff  => "例假日",
        WorkDayTypes.PublicHoliday => "國定假日",
        _                          => dayType ?? "—",
    };
}
