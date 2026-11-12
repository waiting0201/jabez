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
    /// <summary>
    /// 結算上限（2026-11 加班超時認定）：一般為核准的申請時數；人事登錄「認定」後提高為認定時數。
    /// 不認定 / 未處理一律回申請時數。取 max 是防呆：認定時數不可能讓上限變得比申請時數還低。
    /// </summary>
    public static decimal ApprovedCap(OvertimeRequest ot) =>
        ot.ExtraRecognition == OvertimeRecognitionDecisions.Recognized && ot.RecognizedHours is { } h
            ? Math.Max(h, ot.EstimatedHours)
            : ot.EstimatedHours;

    /// <summary>
    /// 加班打卡時數（已扣正常上班時段重疊、無條件捨去到 0.1 小時），**不套用核准上限**。
    /// 供「打卡是否超過申請時數」的判定；與 <see cref="ComputeSettled"/> 共用同一套扣除與捨入。
    /// </summary>
    public static decimal PunchedHours(
        DateTime? start, DateTime? end, DateTime? regularStart = null, DateTime? regularEnd = null) =>
        ComputeSettled(decimal.MaxValue, start, end, regularStart, regularEnd);

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
    public static decimal ComputeSettled(
        decimal approvedHours, DateTime? start, DateTime? end,
        DateTime? regularStart = null, DateTime? regularEnd = null)
    {
        if (start is null || end is null || end.Value <= start.Value) return 0m;

        var minutes = (decimal)(end.Value - start.Value).TotalMinutes;

        // 扣掉與「正常上班時段」重疊的部分（2026-10 防灌工時）：上班時間不是加班，
        // 否則可在 00:01 打上下班卡、00:03 打加班卡就把整段正常工時領成加班費。
        // regularStart / regularEnd 只在「該日為員工的工作日」時由呼叫端傳入；休假日不傳＝不扣。
        if (regularStart is { } ws && regularEnd is { } we && we > ws)
        {
            var overlapStart = start.Value > ws ? start.Value : ws;
            var overlapEnd   = end.Value   < we ? end.Value   : we;
            if (overlapEnd > overlapStart)
                minutes -= (decimal)(overlapEnd - overlapStart).TotalMinutes;
        }

        var punched = Math.Floor(minutes / 60m / RoundingStepHours) * RoundingStepHours;
        return Math.Max(0m, Math.Min(approvedHours, punched));
    }

    /// <summary>
    /// 該加班日「正常上班時段」（工作日才有；其餘日別回 null）。
    /// 一律用標準時段全段（不依請假縮減）：寧可少給，不可多給。
    /// </summary>
    public static (DateTime Start, DateTime End)? RegularWindowFor(
        DateTime overtimeDate, string dayType, WorkdaySchedule schedule)
    {
        if (dayType != WorkDayTypes.Work) return null;
        var day = overtimeDate.Date;
        return (day.Add(schedule.Start.ToTimeSpan()), day.Add(schedule.End.ToTimeSpan()));
    }
}

/// <summary>加班超時認定結果（<see cref="OvertimeRequest.ExtraRecognition"/>）。null＝尚未處理。</summary>
public static class OvertimeRecognitionDecisions
{
    /// <summary>認定：結算上限提高為認定時數。</summary>
    public const string Recognized = "recognized";
    /// <summary>不認定：維持以申請時數為上限（留下公司未同意的紀錄）。</summary>
    public const string Rejected   = "rejected";

    public static bool IsValid(string? v) => v is Recognized or Rejected;
}
