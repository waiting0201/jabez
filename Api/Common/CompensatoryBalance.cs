namespace Jabez.Api.Common;

/// <summary>
/// 補休餘額公式的**單一真相**（純函式）：期初匯入 + 系統核准補休加班 − 已送出補休假。
///
/// FIFO：補休先消耗期初餘額，期初到期（<see cref="OpeningExpiry"/>）後其未用部分作廢，
/// 只剩系統加班可補休（系統加班不到期）。
///
/// 消費點：
///   LeaveRequestHandler.ComputeCompensatoryAsync   → 請假表單 / 送簽擋件 / GET /me/compensatory-hours
///   CompensatoryReportReadService                  → 加班補休時數總表（逐人批次計算）
/// 兩處各自取 opening / earned / used 三個原料，**公式只在這裡**，避免報表與個人頁對不起來。
/// </summary>
public static class CompensatoryBalance
{
    /// <summary>
    /// 期初補休時數（User.CompensatoryOpeningHours）到期日：系統上線前累計的補休須於此日前休完，
    /// 未休完即歸零作廢；此後系統內加班核准產生的補休不受此限制。全員一致故採固定常數。
    /// </summary>
    public static readonly DateTime OpeningExpiry = new(2027, 6, 30, 23, 59, 59);

    public readonly record struct Result(
        decimal OpeningHours,      // 期初匯入（系統上線前累計）
        decimal OpeningRemaining,  // 舊補休剩餘（期初未消耗部分；到期後為 0）
        decimal OvertimeHours,     // 系統核准加班可補休時數
        decimal UsedHours,         // 已送出（pending/approved）補休
        decimal AvailableHours,    // 合計可用
        bool    OpeningExpired);   // 期初是否已到期

    public static Result Compute(decimal opening, decimal earned, decimal used, DateTime now)
    {
        bool expired = now > OpeningExpiry;

        // 期初剩餘（未消耗部分）；到期後作廢為 0
        var openingRemaining = expired ? 0m : Math.Max(0m, opening - Math.Min(used, opening));

        // 合計可用：到期前 = 期初 + 加班 - 已用；到期後 = 加班 - 超出期初的已用部分（期初未用作廢）
        var available = expired
            ? earned - Math.Max(0m, used - opening)
            : opening + earned - used;
        available = available < 0 ? 0m : available;

        return new Result(opening, openingRemaining, earned, used, available, expired);
    }
}
