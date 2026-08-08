using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>某一天的請假時數</summary>
public readonly record struct LeaveDay(DateTime Date, decimal Hours);

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
/// 工作日判定一律走 <see cref="WorkCalendarHelper"/>（有行事曆用 CalendarDay.IsHoliday、無資料退回六日）。
/// </summary>
public static class LeaveDayExpander
{
    /// <summary>
    /// 工作日型假別：天數 / 時數以「扣除國定假日與六日後的實際工作日」計算。
    /// 除歲時祭儀假（依法為連續日曆天）外皆適用。
    /// 前端 WORKING_DAY_LEAVE_TYPES 須與此保持同步。
    /// </summary>
    public static readonly HashSet<string> WorkingDayLeaveTypes =
        ["annual", "personal", "sick", "compensatory", "official", "senior_executive",
         "marriage", "maternity", "bereavement",
         "miscarriage_3m", "miscarriage_2to3m", "miscarriage_under2m",
         "prenatal_checkup", "paternity", "menstrual", "family_care"];

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
        ICalendarDayReadService calendarReader, string leaveType, DateTime startDate, DateTime endDate) =>
        ExpandAsync(calendarReader, new LeaveRequest
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
        ICalendarDayReadService calendarReader, LeaveRequest leave)
    {
        var start = leave.StartDate;
        var end   = leave.EndDate;

        // 非工作日型假別（連續日曆天，目前僅歲時祭儀假）→ 不扣假日，整段日曆天每天 8 小時
        if (!WorkingDayLeaveTypes.Contains(leave.LeaveType))
            return [.. WorkCalendarHelper.EnumerateDates(start, end).Select(d => new LeaveDay(d, 8m))];

        var (_, _, working) = await WorkCalendarHelper.ComputeWorkingDatesAsync(calendarReader, start, end);
        if (working.Count == 0) return [];

        return GetTimeUnit(leave.LeaveType) switch
        {
            LeaveTimeUnit.Day     => [.. working.Select(d => new LeaveDay(d, 8m))],
            LeaveTimeUnit.Hour    => ExpandHourUnit(working, start, end),
            LeaveTimeUnit.HalfDay => ExpandHalfDayUnit(working, start, end),
            _                     => [.. working.Select(d => new LeaveDay(d, 8m))],
        };
    }

    /// <summary>
    /// Hour 單位（事假 / 家庭照顧假 / 病假 / 產檢假 / 陪產假）：與 LeaveRequestHandler.ComputeHourUnitHoursAsync 同規則。
    /// 同日維持 end.Hour − start.Hour（不扣午休，沿用既有單日語意）。
    /// </summary>
    private static List<LeaveDay> ExpandHourUnit(List<DateTime> working, DateTime start, DateTime end)
    {
        if (start.Date == end.Date)
        {
            var sameDay = Math.Max(0, end.Hour - start.Hour);
            return sameDay > 0 ? [new LeaveDay(start.Date, sameDay)] : [];
        }

        var days = new List<LeaveDay>();
        foreach (var d in working)
        {
            decimal hours =
                d == start.Date ? Math.Clamp(WorkdayHours.EndHour - start.Hour, 0, 8) :
                d == end.Date   ? Math.Clamp(end.Hour - WorkdayHours.StartHour, 0, 8) :
                                  8m;
            if (hours > 0) days.Add(new LeaveDay(d, hours));
        }
        return days;
    }

    /// <summary>
    /// HalfDay 單位（年假 / 補休 / 高階主管假）：與前端 computeWorkingDayHours 同規則。
    /// 半天時段編碼於 datetime：起 08:00＝上午、13:00＝下午；訖 12:00＝上午、17:00＝下午。
    /// 首 / 末日以「工作日清單」的頭尾為準（非日曆起訖），與前端一致。
    /// </summary>
    private static List<LeaveDay> ExpandHalfDayUnit(List<DateTime> working, DateTime start, DateTime end)
    {
        bool startIsAm = start.Hour < WorkdayHours.LunchEndHour;   // 08:00 → am、13:00 → pm
        bool endIsPm   = end.Hour   > WorkdayHours.LunchStartHour; // 17:00 → pm、12:00 → am

        if (working.Count == 1)
        {
            decimal single = (startIsAm, endIsPm) switch
            {
                (true,  false) => 4m,  // am → am
                (true,  true)  => 8m,  // am → pm
                (false, true)  => 4m,  // pm → pm
                _              => 0m,  // pm → am：單日無效
            };
            return single > 0 ? [new LeaveDay(working[0], single)] : [];
        }

        var days = new List<LeaveDay>(working.Count);
        for (int i = 0; i < working.Count; i++)
        {
            decimal hours =
                i == 0                  ? (startIsAm ? 8m : 4m) :
                i == working.Count - 1  ? (endIsPm   ? 8m : 4m) :
                                          8m;
            days.Add(new LeaveDay(working[i], hours));
        }
        return days;
    }
}
