using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 補休「逐筆 lot」帳務（四週彈性工時 §7）—— 取代現行純聚合 SUM 的補休池。
///
/// <b>現況（切換前）</b>補休池是三個聚合相減：期初 <c>User.CompensatoryOpeningHours</c>
/// ＋ 已核准補休制加班單的 <c>SUM(EstimatedHours)</c> − 補休假的 <c>SUM(Hours)</c>。
/// FIFO 只是 <c>Math.Min(used, opening)</c> 的算術模擬，**沒有到期日、沒有加班單↔補休單對應**，
/// 也就無從依「原始加班費率」換算到期津貼。
///
/// <b>新制</b>每筆加班選「換取補休」時開一個 lot（帶原始費率快照與到期日），
/// 請補休假時 FIFO 扣抵最早未逾期的 lot 並寫 <see cref="CompensatoryUsage"/>。
///
/// <b>兩制的切線＝<c>SystemSetting.FlexibleWorkStartDate</c></b>，且判準是**加班日 / 請假日自己的日期**，
/// 不是「今天」。切換日之前的加班**刻意不開 lot** —— 那段餘額由切換當下的一次性腳本
/// （<c>Api/Data/Scripts/12</c>）整批做成一筆期初 lot，開了就會重複計算。
///
/// 靜態、**不呼叫 SaveChanges**（比照 <see cref="LeaveRevocationService"/> / <see cref="OvertimeCompensationService"/>），
/// 交易邊界交給呼叫端。
/// </summary>
public static class CompensatoryLotService
{
    /// <summary>
    /// 到期日（2026-09-16 客戶修訂：比產生期間多留一個月）。
    ///   1–6 月產生 → 用至當年 7/31 → 8 月薪資結算
    ///   7–12 月產生 → 用至隔年 1/31 → 隔年 2 月薪資結算
    ///
    /// ⚠ 原規格「1–6 月產生者用至 6 月底」會讓 6 月下旬產生的補休幾乎無法執行，已被推翻，舊數字不可沿用。
    /// 一律回傳**當日 23:59:59**，寫 00:00 會讓到期當天整天不能用。
    /// </summary>
    public static DateTime ExpiresAtFor(DateTime earnedDate) =>
        earnedDate.Month <= 6
            ? new DateTime(earnedDate.Year,     7, 31, 23, 59, 59)
            : new DateTime(earnedDate.Year + 1, 1, 31, 23, 59, 59);

    /// <summary>
    /// 該到期日的結算薪資月份（到期月的**次月**）。薪資端用來圈出「本月要結算哪些 lot」。
    /// 7/31 → 8 月；隔年 1/31 → 隔年 2 月。
    /// </summary>
    public static (int Year, int Month) SettlementMonthFor(DateTime expiresAt)
    {
        var next = new DateTime(expiresAt.Year, expiresAt.Month, 1).AddMonths(1);
        return (next.Year, next.Month);
    }

    /// <summary>
    /// 到期結算在薪資單上的項目名稱（民國年）。
    ///
    /// 依 <see cref="ExpiresAtFor"/>，一般 lot 的到期日只會是 7/31 或 1/31；
    /// **期初 lot 是例外**（沿用舊制的 2027-06-30），故第三條退路不可省。
    /// </summary>
    public static string SettlementLabel(DateTime expiresAt) => expiresAt switch
    {
        { Month:  7, Day: 31 } => $"{expiresAt.Year - 1911}年1-6月補休時數未完畢津貼",
        { Month:  1, Day: 31 } => $"{expiresAt.Year - 1912}年7-12月補休時數未完畢津貼",
        _                      => $"{expiresAt:yyyy/MM/dd} 到期補休時數未完畢津貼",
    };

    // ── 產生（加班核准） ────────────────────────────────────────────────────

    /// <summary>
    /// 依加班單建立／更新補休 lot（冪等，可重複呼叫）。呼叫端負責 SaveChangesAsync。
    ///
    /// 只在「**終局核准** ＋ 補償方式為補休 ＋ 加班日已套用新制」三者同時成立時開 lot；
    /// 任一不成立即走 <see cref="RevokeAsync"/> 收掉既有 lot（退回 / 拒絕 / 改成領加班費時）。
    ///
    /// <b>時數沿用 <c>EstimatedHours</c>（未截斷）</b>，不是加班費那條路徑的 <c>PayableHours</c> ——
    /// 現行補休池本來就沒有計酬上限，轉 lot 時改用截斷值會把既有餘額追溯砍掉。
    /// </summary>
    public static async Task ApplyAsync(
        AppDbContext db,
        IShiftScheduleReadService shiftSchedule,
        IWorkdayScheduleProvider scheduleProvider,
        OvertimeRequest ot)
    {
        var switchDate = await scheduleProvider.GetSwitchDateAsync();

        bool shouldHaveLot = ot.ApprovalStatus == "approved"
                          && OvertimeCompensationService.Normalize(ot.CompensationType)
                             == OvertimeCompensationService.Compensatory
                          && ot.EmployeeId is not null
                          && ot.EstimatedHours > 0m
                          && WorkdayHours.IsFlexible(ot.OvertimeDate, switchDate);

        if (!shouldHaveLot)
        {
            await RevokeAsync(db, ot.Id);
            return;
        }

        var (dayType, isAssignee) = await OvertimePayCalculator.ResolveDayContextAsync(
            shiftSchedule, scheduleProvider, ot.EmployeeId!.Value, ot.OvertimeDate);

        var lot = await db.CompensatoryLots
            .FirstOrDefaultAsync(l => l.SourceOvertimeRequestId == ot.Id);

        var hours = ot.EstimatedHours;
        var rate  = WeightedRate(hours, dayType, isAssignee);

        if (lot is null)
        {
            db.CompensatoryLots.Add(new CompensatoryLot
            {
                UserId                  = ot.EmployeeId.Value,
                SourceOvertimeRequestId = ot.Id,
                EarnedDate              = ot.OvertimeDate.Date,
                Hours                   = hours,
                RemainingHours          = hours,
                RateSnapshot            = rate,
                ExpiresAt               = ExpiresAtFor(ot.OvertimeDate),
                IsOpening               = false,
                CreatedAt               = Clock.Now,
            });
            return;
        }

        // 已存在（重跑核准 / 核准後改時數）：等比調整剩餘時數，已扣抵的部分不動。
        // 直接覆蓋 RemainingHours 會把使用者已經請掉的補休憑空還回去。
        var used = await db.CompensatoryUsages
            .Where(u => u.LotId == lot.Id)
            .SumAsync(u => (decimal?)u.Hours) ?? 0m;

        lot.EarnedDate     = ot.OvertimeDate.Date;
        lot.Hours          = hours;
        lot.RemainingHours = Math.Max(0m, hours - used);
        lot.RateSnapshot   = rate;
        lot.ExpiresAt      = ExpiresAtFor(ot.OvertimeDate);
    }

    /// <summary>
    /// 收掉某張加班單的 lot（退回 / 拒絕 / 改領加班費 / 刪單）。呼叫端負責 SaveChangesAsync。
    ///
    /// ⚠ **已被扣抵過的 lot 不刪、只把剩餘時數歸零**：扣抵紀錄指向已核准的補休假單，
    /// 刪掉 lot 會讓那張假單變成「憑空來的補休」，且 <c>CompensatoryUsage.LotId</c> 是 Cascade，
    /// 會連扣抵紀錄一起消失而無從稽核。
    /// </summary>
    public static async Task RevokeAsync(AppDbContext db, int overtimeRequestId)
    {
        var lot = await db.CompensatoryLots
            .FirstOrDefaultAsync(l => l.SourceOvertimeRequestId == overtimeRequestId);
        if (lot is null) return;

        bool consumed = await db.CompensatoryUsages.AnyAsync(u => u.LotId == lot.Id);
        if (consumed) lot.RemainingHours = 0m;
        else          db.CompensatoryLots.Remove(lot);
    }

    // ── 消耗（補休假） ──────────────────────────────────────────────────────

    /// <summary>
    /// 把某張補休假單的扣抵紀錄**整組重算**（冪等）。呼叫端負責 SaveChangesAsync。
    ///
    /// 刻意做成「先全額歸還、再重新 FIFO 扣抵」而不是逐事件增量調整 ——
    /// 送簽 / 核准 / 退回 / 拒絕 / 刪單 / **銷假使 Hours 遞減**共六個入口都只要呼叫這一支，
    /// 增量寫法則每個入口都要各自算差額，漏一個就會出現對不起來的餘額。
    ///
    /// 佔用時機比照現行補休池：**pending 與 approved 都佔用**（送出即扣，避免同一批時數被重複申請）。
    /// 其餘狀態（draft / returned / rejected）一律歸還。
    ///
    /// FIFO：只取**該假單起始日當下尚未逾期**且仍有剩餘的 lot，依 EarnedDate → Id 排序。
    /// 以假單起始日而非今天判定，補登過去日期的補休才不會被「今天已過期」誤擋。
    /// </summary>
    /// <returns>無法配到 lot 的時數（0 ＝ 全數配足）。呼叫端可據此提示餘額不足。</returns>
    public static async Task<decimal> SyncUsageAsync(
        AppDbContext db, IWorkdayScheduleProvider scheduleProvider, LeaveRequest leave)
    {
        var switchDate = await scheduleProvider.GetSwitchDateAsync();

        // 切換日之前的補休假仍走舊的聚合池，不寫 lot 扣抵
        if (leave.LeaveType != "compensatory"
            || !WorkdayHours.IsFlexible(leave.StartDate, switchDate))
            return 0m;

        // ① 全額歸還既有扣抵
        var existing = await db.CompensatoryUsages
            .Where(u => u.LeaveRequestId == leave.Id)
            .ToListAsync();

        if (existing.Count > 0)
        {
            var lotIds = existing.Select(u => u.LotId).Distinct().ToList();
            var lots   = await db.CompensatoryLots.Where(l => lotIds.Contains(l.Id)).ToListAsync();
            foreach (var u in existing)
            {
                var l = lots.FirstOrDefault(x => x.Id == u.LotId);
                if (l is not null) l.RemainingHours += u.Hours;
            }
            db.CompensatoryUsages.RemoveRange(existing);
        }

        var target = leave.ApprovalStatus is "pending" or "approved" ? leave.Hours : 0m;
        if (target <= 0m) return 0m;

        // ② 重新 FIFO 扣抵
        var available = await db.CompensatoryLots
            .Where(l => l.UserId == leave.EmployeeId
                     && l.RemainingHours > 0m
                     && l.ExpiresAt >= leave.StartDate)
            .OrderBy(l => l.EarnedDate).ThenBy(l => l.Id)
            .ToListAsync();

        var remaining = target;
        foreach (var lot in available)
        {
            if (remaining <= 0m) break;
            var take = Math.Min(lot.RemainingHours, remaining);
            lot.RemainingHours -= take;
            remaining          -= take;
            db.CompensatoryUsages.Add(new CompensatoryUsage
            {
                LotId          = lot.Id,
                LeaveRequestId = leave.Id,
                Hours          = take,
                CreatedAt      = Clock.Now,
            });
        }

        return remaining;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>補休餘額明細（lot 制）。欄位語意刻意對齊舊的聚合版，讓前端不必分兩套。</summary>
    public readonly record struct LotBalance(
        decimal OpeningHours,      // 期初 lot 的原始時數
        decimal OpeningRemaining,  // 期初 lot 的剩餘（已到期則為 0）
        decimal OvertimeHours,     // 非期初 lot 的原始時數合計
        decimal UsedHours,         // 已扣抵合計
        decimal AvailableHours,    // 尚未逾期且仍有剩餘的合計
        bool    OpeningExpired,
        DateTime? OpeningExpiry);

    public static async Task<LotBalance> GetBalanceAsync(AppDbContext db, Guid userId)
    {
        var now  = Clock.Now;
        var lots = await db.CompensatoryLots.AsNoTracking()
            .Where(l => l.UserId == userId)
            .ToListAsync();

        var opening = lots.FirstOrDefault(l => l.IsOpening);

        return new LotBalance(
            OpeningHours:     opening?.Hours ?? 0m,
            OpeningRemaining: opening is not null && opening.ExpiresAt >= now ? opening.RemainingHours : 0m,
            OvertimeHours:    lots.Where(l => !l.IsOpening).Sum(l => l.Hours),
            UsedHours:        lots.Sum(l => l.Hours - l.RemainingHours),
            AvailableHours:   lots.Where(l => l.ExpiresAt >= now).Sum(l => l.RemainingHours),
            OpeningExpired:   opening is not null && opening.ExpiresAt < now,
            OpeningExpiry:    opening?.ExpiresAt);
    }

    // ── 內部 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 原始加班費率快照 —— 到期未休完時依此換算津貼，故必須是**當時**的費率，事後重算會拿到改版後的數字。
    ///
    /// 一張加班單可能橫跨多個級距（例：休假日 3 小時 ＝ 2h ×1.34 ＋ 1h ×1.67），
    /// 而 <c>CompensatoryLot.SourceOvertimeRequestId</c> 有唯一索引（一單一 lot），
    /// 故取**加權平均**：Σ(該段時數 × 該段倍率) ÷ 總時數。加權平均讓
    /// 「剩餘時數 × 費率 × 時薪」在全額未休完時與逐段計算完全相等。
    ///
    /// 超出計酬上限的時數（補休路徑沒有上限，時數可能大於 cap）一律套**最後一段**的倍率。
    /// </summary>
    private static decimal WeightedRate(decimal hours, string dayType, bool isActivityAssignee)
    {
        if (hours <= 0m) return 0m;

        var tierHours = OvertimePayCalculator.ToPayableTierHours(hours, dayType, isActivityAssignee);
        var tiers     = OvertimePayCalculator.SplitHourTiers(tierHours, dayType);

        // 國定假日的前 8 小時不進級距（走薪資加項），此處以最低倍率計 ——
        // 那段時數換成補休時本來就沒有「加班費率」可言，取最低是安全側（津貼不會溢發）。
        if (tiers.Length == 0) return OvertimePayCalculator.LowestMultiplier;

        decimal weighted = 0m, counted = 0m;
        foreach (var t in tiers) { weighted += t.Hours * t.Multiplier; counted += t.Hours; }

        var lastRate = tiers[^1].Multiplier;
        var overflow = Math.Max(0m, hours - counted);
        weighted += overflow * lastRate;
        counted  += overflow;

        return counted > 0m ? Math.Round(weighted / counted, 2, MidpointRounding.AwayFromZero) : lastRate;
    }
}
