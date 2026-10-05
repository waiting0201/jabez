using Jabez.Api.Models.Entities;

namespace Jabez.Api.Common;

/// <summary>
/// 加班「給付基準時數」的**單一真相**（純函式、無 I/O，比照 <see cref="OvertimePayCalculator"/>）。
///
/// 2026-10 防灌工時：加班給付（加班費快照 / 補休 lot / 補休餘額 / 補休總表 / 國定假日出勤加倍工資）
/// 不再信任申請單上的預估時數，改依實際加班打卡結算：
/// <c>SettledHours = min(核准的 EstimatedHours, 實際加班打卡時數)</c>，沒有加班打卡＝0。
///
/// <b>SettledHours 為 null ＝ 舊單</b>：給付基準沿用 EstimatedHours。本系統薪資即時重算、沒有月結快照，
/// 舊單若被套用新規則就等於改寫歷史月份薪資，故舊列永遠維持 null（migration 不 backfill）。
/// SQL 端等價寫法：<c>ISNULL(o.SettledHours, o.EstimatedHours)</c>。
///
/// 純顯示（通知摘要、申請時數欄）與擋件（單日上限、月上限）仍用 EstimatedHours，不走這裡。
/// </summary>
public static class OvertimeSettlement
{
    /// <summary>結算時數的最小單位（小時）＝ <c>decimal(5,1)</c> 欄位精度，打卡時數無條件捨去到此單位。</summary>
    public const decimal RoundingStepHours = 0.1m;

    /// <summary>給付基準時數：SettledHours ?? EstimatedHours。</summary>
    public static decimal BillableHours(OvertimeRequest ot) => ot.SettledHours ?? ot.EstimatedHours;

    public static decimal BillableHours(decimal estimatedHours, decimal? settledHours) =>
        settledHours ?? estimatedHours;

    /// <summary>
    /// 依加班打卡起訖結算：min(核准時數, 打卡時數)。起或訖缺一、或訖 ≤ 起 → 0。
    /// 打卡時數無條件捨去到 <see cref="RoundingStepHours"/>（寧可少給，不可多給）。
    /// 跨日（結束卡在隔天）以 DateTime 直接相減，不受日期邊界影響。
    /// </summary>
    public static decimal ComputeSettled(decimal approvedHours, DateTime? start, DateTime? end)
    {
        if (start is null || end is null || end.Value <= start.Value) return 0m;

        var minutes = (decimal)(end.Value - start.Value).TotalMinutes;
        var punched = Math.Floor(minutes / 60m / RoundingStepHours) * RoundingStepHours;
        return Math.Max(0m, Math.Min(approvedHours, punched));
    }
}
