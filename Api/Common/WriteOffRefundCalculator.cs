using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 預支沖銷「本次應撥差額（RefundDue）」共用計算。
/// 沖銷金額累計超過預支總額時，超出的部分由公司補撥給員工；
/// 以「增額」而非「總超支」計算，讓每張沖銷單各自算得出、彼此不重疊，
/// 加總即等於整張預支單的超支總額，不需等到結案。
/// </summary>
public static class WriteOffRefundCalculator
{
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
    /// 「本單之前已沖銷金額」的單一真相（EF 版）：同一預支單、已核准、且**核准時間早於本單**
    /// （本單尚未核准 → 全部已核准者；同時間以 Id 較小者為前序；舊資料 ReviewedAt 為 null 視為更早）。
    ///
    /// 刻意**不以 Id 排序**：沖銷單的建立順序與核准順序未必一致，若以 Id 判定前序，
    /// 較晚建立卻先核准的單會把同一段超支算成自己的增額，之後較早建立的單再算一次 → 重複撥款。
    /// Dapper 版（供列表 / 詳情顯示）為 WriteOffRequestReadService.BaseSql 的 AdvanceWrittenOffTotal 子查詢，
    /// 兩者條件必須保持一致。
    /// </summary>
    public static async Task<decimal> PriorApprovedTotalAsync(AppDbContext db, WriteOffRecord wo)
        => await db.WriteOffRecords
            .Where(w => w.AdvanceRequestId == wo.AdvanceRequestId
                     && w.ApprovalStatus == "approved"
                     && w.Id != wo.Id
                     && (wo.ReviewedAt == null
                      || w.ReviewedAt == null
                      || w.ReviewedAt < wo.ReviewedAt
                      || (w.ReviewedAt == wo.ReviewedAt && w.Id < wo.Id)))
            .SumAsync(w => (decimal?)w.GrandTotal) ?? 0m;

    /// <summary>
    /// 本張沖銷單造成的超支增額（公司應補撥給員工的金額）。
    /// 供 WriteOffRequestHandler（核准後修改撥款明細）與 ApprovalTaskHandler（財務核准當下）共用。
    /// </summary>
    public static async Task<decimal> CalculateAsync(AppDbContext db, WriteOffRecord wo)
    {
        var advanceGrandTotal = await db.AdvanceRequests
            .Where(a => a.Id == wo.AdvanceRequestId)
            .Select(a => a.GrandTotal)
            .FirstOrDefaultAsync();

        var priorTotal = await PriorApprovedTotalAsync(db, wo);
        return Calculate(advanceGrandTotal, priorTotal, wo.GrandTotal);
    }
}
