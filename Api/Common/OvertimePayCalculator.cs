using Jabez.Api.Models.Dtos;
using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 勞基法加班費計算 —— 倍率、時薪、分段累進的**單一真相**（純函式，無 I/O 版本可直接單元驗算）。
///
/// 倍率（分段累進，不是整段套一個倍率）：
///   平日：1–2h ×1.34、第 3 小時起 ×1.67，上限 4 小時
///   假日：1–2h ×1.34、第 3–8 小時 ×1.67、第 9 小時起 ×2.67，上限 12 小時
///
/// 日別判定沿用 <see cref="WorkCalendarHelper.IsHolidayAsync"/>（行事曆有資料看 IsHoliday、
/// 沒資料退回六日）。**排班制員工（User.IsShiftWorker）恆判為平日**，因為 WorkCalendarHelper
/// 對其短路回 false —— 這是與請假同源的既定語意，刻意不為加班開特例。
/// ⚠ 勞基法 §36 / §39 對排班制的例假 / 休息日另有規定，若業務端認為不符，
///   正解是給 CalendarDay 加排班制專屬日別，不是在本計算器裡挖洞。
///
/// 上限採「截斷計酬、不擋送出」：EstimatedHours 是預估值，擋件會讓員工無法如實登記加班事實；
/// 補休路徑本來就沒有上限，只在加班費側硬擋並不對稱。超出部分以 ExcessHours 全鏈路可見。
///
/// 消費點：
///   OvertimeRequestHandler.EstimateAsync        → 表單即時試算
///   OvertimeCompensationService.ApplyAsync      → 送簽 / 核准時寫入金額快照
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

    /// <summary>純計算版（無 I/O）。表單試算與核准寫快照共用同一支，杜絕兩套公式漂移。</summary>
    public static OvertimePayEstimateDto Calculate(decimal baseSalary, decimal hours, bool isHoliday, DateTime overtimeDate)
    {
        var rate   = HourlyRate(baseSalary);
        var tiers  = isHoliday ? HolidayTiers : WeekdayTiers;
        var cap    = isHoliday ? HolidayCapHours : WeekdayCapHours;
        var capped = Math.Max(0m, Math.Min(hours, cap));

        decimal prev = 0m, raw = 0m;
        var segments = new List<OvertimePaySegmentDto>();
        foreach (var (upTo, mult) in tiers)
        {
            var segHours = Math.Max(0m, Math.Min(capped, upTo) - prev);
            if (segHours > 0m)
            {
                var amount = rate * mult * segHours;
                raw += amount;
                segments.Add(new OvertimePaySegmentDto(mult, segHours, amount));
            }
            prev = upTo;
        }

        // 只在**總額**捨入一次（各分段保留原始小數；逐段捨入再加總會漂移）。
        // AwayFromZero 為專案慣例：Math.Round 預設銀行家捨入，落在 .5 會少 1 元（見 PayrollReadService 假日津貼）。
        var total = Math.Round(raw, 0, MidpointRounding.AwayFromZero);

        return new OvertimePayEstimateDto(
            OvertimeDate:   overtimeDate.Date,
            IsHoliday:      isHoliday,
            HourlyRate:     rate,
            RequestedHours: hours,
            PayableHours:   capped,
            ExcessHours:    Math.Max(0m, hours - cap),
            CapHours:       cap,
            Amount:         total,
            Segments:       [.. segments],
            HasBaseSalary:  baseSalary > 0m);
    }

    /// <summary>
    /// 查行事曆 / 排班制旗標後計算。
    /// <paramref name="ownerId"/> 必須是**加班單所有人**，不是呼叫者（比照 WorkCalendarHelper 的既定慣例）。
    /// </summary>
    public static async Task<OvertimePayEstimateDto> CalculateAsync(
        ICalendarDayReadService calendarReader, IWorkPatternReadService workPattern,
        decimal baseSalary, Guid ownerId, DateTime overtimeDate, decimal hours)
    {
        var isShiftWorker = await workPattern.IsShiftWorkerAsync(ownerId);
        var isHoliday     = await WorkCalendarHelper.IsHolidayAsync(calendarReader, isShiftWorker, overtimeDate);
        return Calculate(baseSalary, hours, isHoliday, overtimeDate);
    }
}
