using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 勞基法加班費計算 —— 倍率、時薪、分段累進的**單一真相**（純函式，無 I/O 版本可直接單元驗算）。
///
/// 倍率（分段累進，不是整段套一個倍率）：
///   上班日（work）：1–2h ×1.34、第 3 小時起 ×1.67，上限 4 小時
///   休假日（rest_day / statutory_off）：1–2h ×1.34、第 3–8h ×1.67、第 9 小時起 ×2.67，上限 12 小時
///   國定假日（public_holiday）：**前 8 小時不走本計算器**（加發 1 日日薪，是薪資加項、不是加班費，
///     落點在 PayrollReadService），第 9 小時起**沿用上班日級距** —— 級距表因此只需要兩份。
///
/// ⚠ **三值日別是 2026-09 四週彈性工時的改版**（原為 <c>bool isHoliday</c>）：
///   國定假日在舊制會落到 <see cref="HolidayTiers"/>（×2.67、上限 12），費率算錯。
///   但**切換日之前必須維持舊行為**（舊單不遷移），故一律先經
///   <see cref="EffectiveDayType"/> 把切換日前的國定假日收斂回休假日。
///
/// 日別解析走 <c>IShiftScheduleReadService.ResolveDayTypeAsync</c>（個人排班 → 國定假日 → 舊制行事曆）。
/// 舊制退路仍是 <see cref="WorkCalendarHelper"/> 那套（行事曆有資料看 IsHoliday、沒資料退回六日），
/// **排班制員工（User.IsShiftWorker）** 的短路則已隨個人排班退場 —— 有個人班表者一律以班表為準。
///
/// 上限採「截斷計酬、不擋送出」：EstimatedHours 是預估值，擋件會讓員工無法如實登記加班事實；
/// 補休路徑本來就沒有上限，只在加班費側硬擋並不對稱。超出部分以 ExcessHours 全鏈路可見。
///
/// 消費點：
///   OvertimeRequestHandler.EstimateAsync        → 表單即時試算
///   OvertimeCompensationService.ApplyAsync      → 送簽 / 核准時寫入金額快照
///   PaymentRequestReadService（簽核任務詳情）    → 只取 SplitHourTiers 的級距，**不給金額**
/// </summary>
public static class OvertimePayCalculator
{
    /// <summary>平日延長工時計酬上限（小時）。</summary>
    public const decimal WeekdayCapHours = 4m;

    /// <summary>假日加班計酬上限（小時）。</summary>
    public const decimal HolidayCapHours = 12m;

    /// <summary>
    /// 時薪 = 月薪 ÷ 30 ÷ 8。分母直接寫 240m 而非除兩次，避免中間值二次捨入。
    /// 刻意不沿用 PayrollReadService 的 dailySalary（該值已先 ROUND 到整數元，再除 8 會繼承取整誤差）。
    /// </summary>
    public static decimal HourlyRate(decimal baseSalary) => Math.Round(baseSalary / 240m, 2);

    /// <summary>分段累進級距 (累進至第幾小時, 倍率)。最後一段的 UpToHour 即為該日別的計酬上限。</summary>
    private static readonly (decimal UpToHour, decimal Rate)[] WeekdayTiers = [(2m, 1.34m), (4m, 1.67m)];
    private static readonly (decimal UpToHour, decimal Rate)[] HolidayTiers = [(2m, 1.34m), (8m, 1.67m), (12m, 2.67m)];

    /// <summary>
    /// 國定假日「未排活動日」者的加班單時數＝**當日全部出勤時數**（§6.3.1 來源 B），
    /// 計酬前須先扣掉這 8 小時 —— 那 8 小時走薪資端的加發日薪，不是加班費。
    /// 已排活動日者（來源 A）打的是正常上下班卡，加班單本來就只填第 9 小時起，故不扣。
    /// </summary>
    public const decimal PublicHolidayFreeHours = 8m;

    /// <summary>
    /// 該日別套用哪一份級距表。國定假日與上班日共用平日級距（§6.3.1），
    /// 例假日雖然依法嚴禁出勤，萬一有資料仍以休假日級距計酬（安全側：寧可多給，不可少給）。
    /// </summary>
    private static (decimal UpToHour, decimal Rate)[] TiersFor(string dayType) => dayType switch
    {
        WorkDayTypes.RestDay or WorkDayTypes.StatutoryOff => HolidayTiers,
        _                                                 => WeekdayTiers,   // work / public_holiday
    };

    /// <summary>該日別的計酬上限。Calculate 與 SplitHourTiers 共用，避免三元式散在兩處。</summary>
    public static decimal CapHoursFor(string dayType) =>
        dayType is WorkDayTypes.RestDay or WorkDayTypes.StatutoryOff ? HolidayCapHours : WeekdayCapHours;

    /// <summary>
    /// 切換日前，**國定假日一律收斂回休假日** —— 舊制沒有第四種日別，
    /// <c>WorkCalendarHelper.IsHolidayAsync</c> 對國定假日回 true，走的就是 ×2.67 那套。
    /// 不收斂的話，地基上線當天所有歷史國定假日加班單的級距會靜默變動。
    /// </summary>
    /// ⚠ **不可套 <c>WorkDayTypes.Normalize</c>** —— 那支的語意是「員工可勾選的三值」，
    /// 會把 <c>public_holiday</c> 當成非法值打回 <c>work</c>（實測：切換日打開後國定假日加班
    /// 反而變回平日級距、前 8 小時也不扣）。此處的輸入來自 <c>ResolveDayTypeAsync</c>，本來就是四值之一。
    public static string EffectiveDayType(string dayType, DateTime date, DateTime? switchDate) =>
        dayType == WorkDayTypes.PublicHoliday && !WorkdayHours.IsFlexible(date, switchDate)
            ? WorkDayTypes.RestDay
            : dayType;

    /// <summary>
    /// 加班單時數 → 實際進級距的時數。國定假日來源 B 要先扣掉前 8 小時（見
    /// <see cref="PublicHolidayFreeHours"/>），其餘日別原值照舊。純函式，供試算與快照共用。
    /// </summary>
    public static decimal ToPayableTierHours(decimal requestedHours, string dayType, bool isActivityAssignee) =>
        dayType == WorkDayTypes.PublicHoliday && !isActivityAssignee
            ? Math.Max(0m, requestedHours - PublicHolidayFreeHours)
            : requestedHours;

    /// <summary>
    /// 舊快照列的日別還原。<c>OvertimeRequest.OvertimeDayType</c> 是 2026-09 才加的欄位，
    /// 既有列只有 <c>IsHolidayOvertime</c>（bool?）—— 刻意不 backfill（migration 只做 schema），
    /// 改由讀取端統一退回。true → 休假日、false → 上班日、null → 未算過。
    /// </summary>
    public static string? SnapshotDayType(string? dayType, bool? isHolidayOvertime) =>
        dayType ?? isHolidayOvertime switch
        {
            true  => WorkDayTypes.RestDay,
            false => WorkDayTypes.Work,
            null  => null,
        };

    /// <summary>
    /// 只切「分段時數」、不算錢：依日別級距把 hours 截斷至上限後拆成 (倍率, 該段時數)。
    ///
    /// 存在理由：簽核詳情頁必須讓審核者看懂計酬結構（「2h ×1.34 + 1h ×1.67」），
    /// 但**不能給金額** —— 金額 ÷ 時數 = 時薪 × 加權倍率 → 反推得出底薪，
    /// 與加班報表 reports-overtime:amount 是同一個顧慮。級距與 <see cref="Calculate"/>
    /// 共用同一份級距表與同一套截斷語意，杜絕「畫面切 2+1、實發按別的算」。
    ///
    /// ⚠ 呼叫端請傳 **PayableHours 快照**（已截斷）而非 EstimatedHours —— 時數側才吃得到快照保真度。
    /// ⚠ 級距表本身取的是**當下常數**、非快照：日後修法時舊單顯示的級距會跟著變，與 OvertimePayAmount
    ///   快照不同步。這是刻意取捨 —— 正解是修法時給級距表加生效日，不是在這裡再加第五個快照欄。
    /// </summary>
    public static OvertimeHourTierDto[] SplitHourTiers(decimal hours, string dayType)
    {
        var tiers  = TiersFor(dayType);
        var capped = Math.Max(0m, Math.Min(hours, CapHoursFor(dayType)));

        decimal prev = 0m;
        var result = new List<OvertimeHourTierDto>();
        foreach (var (upTo, mult) in tiers)
        {
            var segHours = Math.Max(0m, Math.Min(capped, upTo) - prev);
            if (segHours > 0m) result.Add(new OvertimeHourTierDto(mult, segHours));
            prev = upTo;
        }
        return [.. result];
    }

    /// <summary>
    /// 純計算版（無 I/O）。表單試算與核准寫快照共用同一支，杜絕兩套公式漂移。
    ///
    /// <paramref name="dayType"/> 必須是**已經過 <see cref="EffectiveDayType"/> 收斂**的值。
    /// <paramref name="isActivityAssignee"/> 只在國定假日有意義（決定要不要扣前 8 小時）。
    /// </summary>
    public static OvertimePayEstimateDto Calculate(
        decimal baseSalary, decimal hours, string dayType, DateTime overtimeDate,
        bool isActivityAssignee = false)
    {
        var rate   = HourlyRate(baseSalary);
        var cap    = CapHoursFor(dayType);
        // 國定假日來源 B：申請時數含前 8 小時，計酬前先扣掉
        var tierHours = ToPayableTierHours(hours, dayType, isActivityAssignee);
        var capped = Math.Max(0m, Math.Min(tierHours, cap));

        // 分段「時數」的單一真相＝SplitHourTiers；本方法只負責乘上時薪與總額捨入。
        decimal raw = 0m;
        var segments = new List<OvertimePaySegmentDto>();
        foreach (var (mult, segHours) in SplitHourTiers(tierHours, dayType))
        {
            var amount = rate * mult * segHours;
            raw += amount;
            segments.Add(new OvertimePaySegmentDto(mult, segHours, amount));
        }

        // 只在**總額**捨入一次（各分段保留原始小數；逐段捨入再加總會漂移）。
        // AwayFromZero 為專案慣例：Math.Round 預設銀行家捨入，落在 .5 會少 1 元（見 PayrollReadService 假日津貼）。
        var total = Math.Round(raw, 0, MidpointRounding.AwayFromZero);

        return new OvertimePayEstimateDto(
            OvertimeDate:   overtimeDate.Date,
            DayType:        dayType,
            HourlyRate:     rate,
            RequestedHours: hours,
            PayableHours:   capped,
            ExcessHours:    Math.Max(0m, tierHours - cap),
            CapHours:       cap,
            Amount:         total,
            Segments:       [.. segments],
            HasBaseSalary:  baseSalary > 0m);
    }

    /// <summary>
    /// 解析日別（個人排班 → 國定假日 → 舊制行事曆）後計算。
    /// <paramref name="ownerId"/> 必須是**加班單所有人**，不是呼叫者（比照 WorkCalendarHelper 的既定慣例）。
    ///
    /// 三件事一次做完，讓三個消費點不必各自拼裝：
    ///   ① 解析日別　② 依切換日收斂國定假日（<see cref="EffectiveDayType"/>）
    ///   ③ 查當日是否為活動日預定人力（只在國定假日影響結果，故其餘日別短路不查）
    /// </summary>
    public static async Task<OvertimePayEstimateDto> CalculateAsync(
        IShiftScheduleReadService shiftSchedule, IWorkdayScheduleProvider scheduleProvider,
        decimal baseSalary, Guid ownerId, DateTime overtimeDate, decimal hours)
    {
        var (dayType, isAssignee) = await ResolveDayContextAsync(
            shiftSchedule, scheduleProvider, ownerId, overtimeDate);
        return Calculate(baseSalary, hours, dayType, overtimeDate, isAssignee);
    }

    /// <summary>
    /// 日別 ＋ 活動日預定人力的解析（三個消費點共用；快照寫入亦走這支，確保試算與落地一致）。
    /// </summary>
    public static async Task<(string DayType, bool IsActivityAssignee)> ResolveDayContextAsync(
        IShiftScheduleReadService shiftSchedule, IWorkdayScheduleProvider scheduleProvider,
        Guid ownerId, DateTime overtimeDate)
    {
        var raw       = await shiftSchedule.ResolveDayTypeAsync(ownerId, overtimeDate);
        var switchDay = await scheduleProvider.GetSwitchDateAsync();
        var dayType   = EffectiveDayType(raw, overtimeDate, switchDay);

        // 非國定假日時 isActivityAssignee 不影響任何結果，省一次 DB 往返
        var isAssignee = dayType == WorkDayTypes.PublicHoliday
                      && await shiftSchedule.IsActivityAssigneeAsync(ownerId, overtimeDate);

        return (dayType, isAssignee);
    }
}
