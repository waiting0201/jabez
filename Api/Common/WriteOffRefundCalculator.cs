using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 預支沖銷「本次應撥差額（RefundDue）」與「同仁應繳回 / 公司應補撥給同仁」的共用計算。
///
/// 兩個數字、兩種用途（2026-11-12 拆開）：
///
/// 1. **RefundDue ＝ 財務要排的差額撥款**（差額分期 WriteOffInstallments 的 SUM）：
///    沖銷累計超過預支**總額**的增額，算法自 2026-07 起不變。它同時涵蓋「補撥給同仁」與
///    「月結超支、付給廠商」兩種款 —— 財務實務上就是用這組分期排月結超支的廠商付款
///    （備註寫「支付廠商」），同仁手上剩餘的現金則直接抵掉一部分。
///
/// 2. **同仁與公司之間的差額**（<see cref="Settle"/> / <see cref="DueToEmployee"/>，保守互抵規則）：
///    只認「現金差額」與「總額差額」**同方向**的部分、取絕對值較小者。
///      · 月結（支票）由公司直接支付廠商 → 月結超支不算欠同仁、月結短支不要求同仁繳回；
///      · 現金與月結互相抵銷的部分不算（同一筆費用在預支與沖銷常填在不同欄，
///        只看現金欄會憑空多出應補撥金額）。
///    用於金額歷程的「尚未結清」、繳回上限，以及 RefundDue 中「補撥同仁」的拆分顯示。
///    現金一律以「總額 − 月結」推導（<see cref="CashOf"/>），不直接讀 CashTotal。
///
/// 皆以「增額」而非「總超支」計算，讓每張沖銷單各自算得出、彼此不重疊，不需等到結案。
/// </summary>
public static class WriteOffRefundCalculator
{
    /// <summary>現金部分 ＝ 總額 − 月結（支票）。全站「現金側」金額的唯一推導方式。</summary>
    public static decimal CashOf(decimal grandTotal, decimal checkTotal) => grandTotal - checkTotal;

    /// <summary>
    /// 本張沖銷單造成的超支增額（財務應排的差額撥款，含補撥同仁與月結超支付廠商）。
    /// </summary>
    /// <param name="advanceGrandTotal">關聯預支單的總額（含追加批次）</param>
    /// <param name="otherWrittenOffTotal">本單之前（先核准）已核准沖銷單的金額加總</param>
    /// <param name="currentGrandTotal">本張沖銷單的金額</param>
    public static decimal Calculate(decimal advanceGrandTotal, decimal otherWrittenOffTotal, decimal currentGrandTotal)
    {
        var before = Math.Max(0m, otherWrittenOffTotal - advanceGrandTotal);
        var after  = Math.Max(0m, otherWrittenOffTotal + currentGrandTotal - advanceGrandTotal);
        return after - before;
    }

    /// <summary>
    /// 同仁與公司之間的結算差額（&gt;0 同仁應繳回，&lt;0 公司應補撥給同仁）。
    /// 兩個差額皆為「預支 − 核銷」方向；同號取絕對值較小者，異號或任一為 0 則為 0。
    /// 全站「尚未結清 / 同仁應繳回 / 應補撥同仁」的唯一判準（金額歷程、繳回上限、RefundDue 拆分共用）。
    /// </summary>
    public static decimal Settle(decimal cashDiff, decimal totalDiff)
    {
        if (cashDiff > 0m && totalDiff > 0m) return Math.Min(cashDiff, totalDiff);
        if (cashDiff < 0m && totalDiff < 0m) return Math.Max(cashDiff, totalDiff);
        return 0m;
    }

    /// <summary>累計到某個時點，公司應補撥給同仁的金額（≥ 0，恆 ≤ 總額超支）。</summary>
    public static decimal DueToEmployee(decimal advanceTotal, decimal advanceCheck, decimal writtenOffTotal, decimal writtenOffCheck)
        => Math.Max(0m, -Settle(
            CashOf(advanceTotal, advanceCheck) - CashOf(writtenOffTotal, writtenOffCheck),
            advanceTotal - writtenOffTotal));

    /// <summary>
    /// RefundDue 之中「補撥給同仁」的部分（本單增額）；其餘（RefundDue − 本值）為月結超支、付給廠商。
    /// 現金沖銷與總額沖銷皆只增不減，故增額恆 ≥ 0。
    /// </summary>
    public static decimal CalculateToEmployee(decimal advanceTotal, decimal advanceCheck,
                                              decimal priorTotal, decimal priorCheck,
                                              decimal currentTotal, decimal currentCheck)
        => DueToEmployee(advanceTotal, advanceCheck, priorTotal + currentTotal, priorCheck + currentCheck)
         - DueToEmployee(advanceTotal, advanceCheck, priorTotal, priorCheck);

    /// <summary>
    /// 「本單之前已沖銷金額」的單一真相（EF 版）：同一預支單、已核准、且**核准時間早於本單**
    /// （本單尚未核准 → 全部已核准者；同時間以 Id 較小者為前序；舊資料 ReviewedAt 為 null 視為更早）。
    ///
    /// 刻意**不以 Id 排序**：沖銷單的建立順序與核准順序未必一致，若以 Id 判定前序，
    /// 較晚建立卻先核准的單會把同一段超支算成自己的增額，之後較早建立的單再算一次 → 重複撥款。
    /// Dapper 版（供列表 / 詳情顯示）為 WriteOffRequestReadService.BaseSql 的 AdvanceWrittenOffTotal /
    /// AdvanceWrittenOffCheckTotal 子查詢，兩者條件必須保持一致。
    /// </summary>
    public static async Task<(decimal Total, decimal Check)> PriorApprovedTotalsAsync(AppDbContext db, WriteOffRecord wo)
    {
        var sums = await db.WriteOffRecords
            .Where(w => w.AdvanceRequestId == wo.AdvanceRequestId
                     && w.ApprovalStatus == "approved"
                     && w.Id != wo.Id
                     && (wo.ReviewedAt == null
                      || w.ReviewedAt == null
                      || w.ReviewedAt < wo.ReviewedAt
                      || (w.ReviewedAt == wo.ReviewedAt && w.Id < wo.Id)))
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(w => w.GrandTotal), Check = g.Sum(w => w.CheckTotal) })
            .FirstOrDefaultAsync();
        return (sums?.Total ?? 0m, sums?.Check ?? 0m);
    }

    /// <summary>
    /// 本張沖銷單造成的超支增額（財務應排的差額撥款）。
    /// 供 WriteOffRequestHandler（核准後修改撥款明細）與 ApprovalTaskHandler（財務核准當下）共用。
    /// </summary>
    public static async Task<decimal> CalculateAsync(AppDbContext db, WriteOffRecord wo)
    {
        var advanceGrandTotal = await db.AdvanceRequests
            .Where(a => a.Id == wo.AdvanceRequestId)
            .Select(a => a.GrandTotal)
            .FirstOrDefaultAsync();

        var (priorTotal, _) = await PriorApprovedTotalsAsync(db, wo);
        return Calculate(advanceGrandTotal, priorTotal, wo.GrandTotal);
    }
}
