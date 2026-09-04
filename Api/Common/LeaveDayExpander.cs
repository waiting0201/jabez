using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 某一天的請假時數與時段。
/// <para>
/// Start / End 為該日實際請假時段，一律 clamp 在 08:00–17:00 內（小時假可能填 07:00 / 19:00）。
/// 注意 Hours 沿用既有語意（整點差、不扣午休，故 09:00–13:00 ＝ 4 小時），
/// 與 End − Start 不必然等長，這是刻意保留的既有行為。
/// </para>
/// </summary>
public readonly record struct LeaveDay(
    DateTime Date, decimal Hours, string Segment, TimeOnly Start, TimeOnly End);

/// <summary>
/// 請假單「逐日展開」的單一真相 —— 把一張 LeaveRequest 攤成「哪一天、幾小時」的清單。
///
/// 消費點：
///   LeaveRevocationHandler  → 逐日勾選銷假（GET /leave-requests/{id}/revocable-dates）
///   LeaveRevocationService  → 銷假核准後重算 LeaveRequest.Hours
///   LeaveRequestHandler     → 共用假別分類常數（WorkingDayLeaveTypes / GetTimeUnit）
///   AttendanceLeaveMerger   → 出缺勤報表「打卡 ∪ 請假日」合併時算當日請假時數
///
/// 展開規則與 LeaveRequestHandler 送出時的權威重算完全一致，故 Σ Hours 應等於 LeaveRequest.Hours：
///   Day     → 每個工作日 8 小時
///   Hour    → 同日 end.Hour − start.Hour；跨日首日 Clamp(17 − start.Hour, 0, 8)、中間 8、末日 Clamp(end.Hour − 8, 0, 8)
///   HalfDay → 單一工作日 am→am 4 / am→pm 8 / pm→pm 4；多工作日 首日(am 8 / pm 4) + 中間 8 + 末日(pm 8 / am 4)
///   非工作日型假別（歲時祭儀假）→ 整段日曆天，每天 8 小時
/// 工作日判定一律走 <see cref="WorkCalendarHelper"/>（有行事曆用 CalendarDay.IsHoliday、無資料退回六日）；
/// 申請人為排班制（User.IsShiftWorker）時 ignoreHolidays=true，整段皆為工作日、不扣六日與國定假日。
/// </summary>
public static class LeaveDayExpander
{
    /// <summary>
    /// 工作日型假別：天數 / 時數以「扣除國定假日與六日後的實際工作日」計算。
    /// 不適用者為歲時祭儀假與育嬰留職停薪（依法／依語意為連續日曆天）。
    /// 前端 WORKING_DAY_LEAVE_TYPES 須與此保持同步。
    /// </summary>
    /// <remarks>
    /// parental_leave（長期留停）刻意不列入：留停整段期間都不在職（含六日與國定假日），
    /// 且工作日型假別在 Submit 時會強制要求區間橫跨的每個年度行事曆皆已匯入
    /// （見 LeaveRequestHandler.SubmitAsync），育嬰留停跨 1~2 年會因未來年度行事曆未匯入而無法送件。
    /// parental_leave_daily（彈性單日）則為一般工作日請假語意，仍列入。
    /// </remarks>
    public static readonly HashSet<string> WorkingDayLeaveTypes =
        ["annual", "personal", "sick", "compensatory", "official", "senior_executive",
         "marriage", "maternity", "bereavement",
         "miscarriage_3m", "miscarriage_2to3m", "miscarriage_under2m",
         "prenatal_checkup", "paternity", "menstrual", "family_care",
         "parental_leave_daily"];

    /// <summary>各假別時間單位對應</summary>
    public static readonly Dictionary<string, LeaveTimeUnit> TimeUnitMap = new()
    {
        ["personal"]            = LeaveTimeUnit.Hour,
        ["family_care"]         = LeaveTimeUnit.Hour,
        ["sick"]                = LeaveTimeUnit.Hour,
        ["prenatal_checkup"]    = LeaveTimeUnit.Hour,
        ["paternity"]           = LeaveTimeUnit.Hour,
        ["annual"]              = LeaveTimeUnit.HalfDay,
        ["compensatory"]        = LeaveTimeUnit.HalfDay,
        ["senior_executive"]    = LeaveTimeUnit.HalfDay,
        ["official"]            = LeaveTimeUnit.Day,
        ["marriage"]            = LeaveTimeUnit.Day,
        ["maternity"]           = LeaveTimeUnit.Day,
        ["bereavement"]         = LeaveTimeUnit.Day,
        ["ceremonial_festival"] = LeaveTimeUnit.Day,
        ["miscarriage_3m"]      = LeaveTimeUnit.Day,
        ["miscarriage_2to3m"]   = LeaveTimeUnit.Day,
        ["miscarriage_under2m"] = LeaveTimeUnit.Day,
        ["menstrual"]           = LeaveTimeUnit.Day,
        ["parental_leave"]       = LeaveTimeUnit.Day,
        ["parental_leave_daily"] = LeaveTimeUnit.Day,
    };

    /// <summary>取得指定假別的時間單位</summary>
    public static LeaveTimeUnit GetTimeUnit(string leaveType) =>
        TimeUnitMap.TryGetValue(leaveType, out var u) ? u : LeaveTimeUnit.Hour;

    /// <summary>時間單位轉字串（前端使用）</summary>
    public static string TimeUnitToString(LeaveTimeUnit unit) => unit switch
    {
        LeaveTimeUnit.Hour    => "hour",
        LeaveTimeUnit.HalfDay => "half_day",
        LeaveTimeUnit.Day     => "day",
        _                     => "hour",
    };

    /// <summary>
    /// Dapper 投影專用 overload：展開只讀假別與起訖三個欄位，不必為此撈出完整 LeaveRequest entity。
    /// 消費點：AttendanceLeaveMerger（出缺勤報表合併請假虛擬列）。
    /// </summary>
    public static Task<List<LeaveDay>> ExpandAsync(
        ICalendarDayReadService calendarReader, bool ignoreHolidays,
        string leaveType, DateTime startDate, DateTime endDate) =>
        ExpandAsync(calendarReader, ignoreHolidays, new LeaveRequest
        {
            LeaveType = leaveType,
            StartDate = startDate,
            EndDate   = endDate,
        });

    /// <summary>
    /// 把請假單攤成逐日清單（僅含實際請假的日子，假日不產生列）。
    /// 行事曆尚未匯入時退回六日判定，與 <see cref="WorkCalendarHelper"/> 同一規則。
    /// </summary>
    public static async Task<List<LeaveDay>> ExpandAsync(
        ICalendarDayReadService calendarReader, bool ignoreHolidays, LeaveRequest leave)
    {
        var start = leave.StartDate;
        var end   = leave.EndDate;

        // 非工作日型假別（連續日曆天，目前僅歲時祭儀假）→ 不扣假日，整段日曆天每天 8 小時
        if (!WorkingDayLeaveTypes.Contains(leave.LeaveType))
            return [.. WorkCalendarHelper.EnumerateDates(start, end).Select(FullDay)];

        var (_, _, working) = await WorkCalendarHelper.ComputeWorkingDatesAsync(calendarReader, ignoreHolidays, start, end);
        if (working.Count == 0) return [];

        return GetTimeUnit(leave.LeaveType) switch
        {
            LeaveTimeUnit.Day     => [.. working.Select(FullDay)],
            LeaveTimeUnit.Hour    => ExpandHourUnit(working, start, end),
            LeaveTimeUnit.HalfDay => ExpandHalfDayUnit(working, start, end),
            _                     => [.. working.Select(FullDay)],
        };
    }

    /// <summary>
    /// Hour 單位（事假 / 家庭照顧假 / 病假 / 產檢假 / 陪產假）：與 LeaveRequestHandler.ComputeHourUnitHoursAsync 同規則。
    /// 同日維持 end.Hour − start.Hour（不扣午休，沿用既有單日語意）。
    /// 時段：同日為原始起訖、跨日首日為「起 → 17:00」、末日為「08:00 → 訖」、中間日為全天。
    /// </summary>
    private static List<LeaveDay> ExpandHourUnit(List<DateTime> working, DateTime start, DateTime end)
    {
        if (start.Date == end.Date)
        {
            var sameDay = Math.Max(0, end.Hour - start.Hour);
            return sameDay > 0
                ? [new LeaveDay(start.Date, sameDay, LeaveDaySegments.Partial,
                                ClampToWorkday(start), ClampToWorkday(end))]
                : [];
        }

        var days = new List<LeaveDay>();
        foreach (var d in working)
        {
            if (d == start.Date)
            {
                decimal hours = Math.Clamp(WorkdayHours.EndHour - start.Hour, 0, 8);
                if (hours > 0)
                    days.Add(new LeaveDay(d, hours, LeaveDaySegments.Partial, ClampToWorkday(start), WorkdayEnd));
            }
            else if (d == end.Date)
            {
                decimal hours = Math.Clamp(end.Hour - WorkdayHours.StartHour, 0, 8);
                if (hours > 0)
                    days.Add(new LeaveDay(d, hours, LeaveDaySegments.Partial, WorkdayStart, ClampToWorkday(end)));
            }
            else
            {
                days.Add(FullDay(d));
            }
        }
        return days;
    }

    /// <summary>
    /// HalfDay 單位（年假 / 補休 / 高階主管假）：與前端 computeWorkingDayHours 同規則。
    /// 半天時段編碼於 datetime：起 08:00＝上午、13:00＝下午；訖 12:00＝上午、17:00＝下午。
    /// 補休的上午時段為 09:00–13:00（見前端 halfDayAmStartHour / halfDayAmEndHour），故一律以
    /// 「起 &lt; 13:00 ＝上午」「訖 &gt; 13:00 ＝下午」分類，不可改用「等於 08:00 / 12:00」判定
    /// （補休的訖 13:00 必須仍判為上午，否則單日 am→am 會被誤判成全日 8 小時）；
    /// 展開後的時段與時數仍取標準半天 08:00–12:00 / 4 小時。
    /// 首 / 末日以「工作日清單」的頭尾為準（非日曆起訖），與前端一致。
    /// </summary>
    private static List<LeaveDay> ExpandHalfDayUnit(List<DateTime> working, DateTime start, DateTime end)
    {
        bool startIsAm = start.Hour < WorkdayHours.LunchEndHour; // 08:00 / 09:00 → am、13:00 → pm
        bool endIsPm   = end.Hour   > WorkdayHours.LunchEndHour; // 17:00 → pm、12:00 / 13:00 → am

        if (working.Count == 1)
        {
            return (startIsAm, endIsPm) switch
            {
                (true,  false) => [AmHalfDay(working[0])],   // am → am
                (true,  true)  => [FullDay(working[0])],     // am → pm
                (false, true)  => [PmHalfDay(working[0])],   // pm → pm
                _              => [],                        // pm → am：單日無效
            };
        }

        var days = new List<LeaveDay>(working.Count);
        for (int i = 0; i < working.Count; i++)
        {
            var d = working[i];
            days.Add(
                i == 0                 ? (startIsAm ? FullDay(d) : PmHalfDay(d)) :
                i == working.Count - 1 ? (endIsPm   ? FullDay(d) : AmHalfDay(d)) :
                                         FullDay(d));
        }
        return days;
    }

    // ── 時段建構輔助 ──────────────────────────────────────────
    // 工作日邊界一律取自 WorkdayHours（Constants.cs），不在此重複硬編碼時分。

    private static readonly TimeOnly WorkdayStart = new(WorkdayHours.StartHour, 0);       // 08:00
    private static readonly TimeOnly LunchStart   = new(WorkdayHours.LunchStartHour, 0);  // 12:00
    private static readonly TimeOnly LunchEnd     = new(WorkdayHours.LunchEndHour, 0);    // 13:00
    private static readonly TimeOnly WorkdayEnd   = new(WorkdayHours.EndHour, 0);         // 17:00

    /// <summary>整個工作日 08:00–17:00（8 小時）</summary>
    private static LeaveDay FullDay(DateTime date) =>
        new(date, 8m, LeaveDaySegments.Full, WorkdayStart, WorkdayEnd);

    /// <summary>上半天 08:00–12:00（4 小時）。補休雖存 09:00–13:00，展開仍取標準上午時段（時數恆 4 小時）</summary>
    private static LeaveDay AmHalfDay(DateTime date) =>
        new(date, 4m, LeaveDaySegments.Am, WorkdayStart, LunchStart);

    /// <summary>下半天 13:00–17:00（4 小時）</summary>
    private static LeaveDay PmHalfDay(DateTime date) =>
        new(date, 4m, LeaveDaySegments.Pm, LunchEnd, WorkdayEnd);

    /// <summary>
    /// 把原始填寫時間夾在工作日邊界 08:00–17:00 內。
    /// 小時假的 Hours 端本來就 Math.Clamp(…, 0, 8)，時段端不夾會讓「應出勤時段」被推到 07:00 開工。
    /// </summary>
    private static TimeOnly ClampToWorkday(DateTime at)
    {
        var t = TimeOnly.FromDateTime(at);
        return t < WorkdayStart ? WorkdayStart
             : t > WorkdayEnd   ? WorkdayEnd
             : t;
    }
}
