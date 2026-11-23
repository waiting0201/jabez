using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 預支沖銷「本次應撥差額（RefundDue）」與「同仁應繳回 / 公司應補撥給同仁」的共用計算。
///
/// **2026-11-23 起現金與月結完全分開、不互抵**：
///
/// · **現金側 ＝ 同仁與公司之間的帳**：現金預支 − 現金核銷 − 同仁繳回 ＋ 公司補撥。
///   現金有剩 → 同仁應繳回（走「現金・匯款繳回」）；現金超支 → 公司應補撥同仁（走差額分期）。
///   兩者是同一個數字，只是正負號不同。
/// · **月結側 ＝ 公司與廠商之間的帳**：月結預支 − 月結核銷，只列示「月結差額」，
///   不併入應繳回 / 應補撥，也不進差額分期（月結超支的廠商款以明細的「支票已支付」註記處理）。
///
/// 原 2026-11-12 的「保守互抵」（現金差額與總額差額同方向取較小者）與「RefundDue ＝ 總額超支增額」
/// 會讓月結超支吃掉現金剩餘（例：WO-20261012-002 現金剩 667、月結超支 133,865，
/// 舊算法應繳回 0、應撥差額 133,198 ＝ 月結超支內含現金），已廢止。
///
/// 皆以「增額」計算，讓每張沖銷單各自算得出、彼此不重疊，不需等到結案。
/// 現金一律以「總額 − 月結」推導（<see cref="CashOf"/>），不直接讀 CashTotal。
/// </summary>
public static class WriteOffRefundCalculator
{
    /// <summary>現金部分 ＝ 總額 − 月結（支票）。全站「現金側」金額的唯一推導方式。</summary>
    public static decimal CashOf(decimal grandTotal, decimal checkTotal) => grandTotal - checkTotal;

    /// <summary>
    /// 本張沖銷單造成的**現金**超支增額（公司應補撥同仁、財務應排的差額撥款；月結不計）。
    /// </summary>
    /// <param name="advanceTotal">關聯預支單的總額（含追加批次）</param>
    /// <param name="advanceCheck">關聯預支單的月結（支票）合計</param>
    /// <param name="priorTotal">本單之前（先核准）已核准沖銷單的金額加總</param>
    /// <param name="priorCheck">同範圍的月結合計</param>
    /// <param name="currentTotal">本張沖銷單的金額</param>
    /// <param name="currentCheck">本張沖銷單的月結合計</param>
    public static decimal Calculate(decimal advanceTotal, decimal advanceCheck,
                                    decimal priorTotal, decimal priorCheck,
                                    decimal currentTotal, decimal currentCheck)
    {
        var advanceCash = CashOf(advanceTotal, advanceCheck);
        var priorCash   = CashOf(priorTotal, priorCheck);
        var currentCash = CashOf(currentTotal, currentCheck);
        var before = Math.Max(0m, priorCash - advanceCash);
        var after  = Math.Max(0m, priorCash + currentCash - advanceCash);
        return after - before;
    }

    /// <summary>
    /// 本張沖銷單造成的現金超支增額（財務應排的差額撥款）。
    /// 供 WriteOffRequestHandler（核准後修改撥款明細）與 ApprovalTaskHandler（財務核准當下）共用。
    /// </summary>
    public static async Task<decimal> CalculateAsync(AppDbContext db, WriteOffRecord wo)
    {
        var adv = await db.AdvanceRequests
            .Where(a => a.Id == wo.AdvanceRequestId)
            .Select(a => new { a.GrandTotal, a.CheckTotal })
            .FirstOrDefaultAsync();

        var (priorTotal, priorCheck) = await PriorApprovedTotalsAsync(db, wo);
        return Calculate(adv?.GrandTotal ?? 0m, adv?.CheckTotal ?? 0m, priorTotal, priorCheck, wo.GrandTotal, wo.CheckTotal);
    }

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
}
