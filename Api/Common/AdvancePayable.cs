using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 預支單撥款明細的「應撥總額」單一真相（2026-11-24）。
///
/// 應撥總額 ＝ 現金預支 ＋ 月結應撥：
/// · **現金**：一律用預支金額（GrandTotal − CheckTotal）。現金實支與預支的差額另走
///   「同仁繳回」（少花）或「沖銷差額撥款」（超支），不在預支撥款明細內調整，否則超支會重複撥款。
/// · **月結**：公司直接付給廠商，財務照廠商發票的實際金額付款。
///   預支單**只要有已核准的沖銷單**，月結應撥就改為「全部已核准沖銷的月結合計」（隨每張沖銷核准累加）；
///   尚無已核准沖銷時用預支的月結預算。只沖銷一部分時，月結應撥會暫時低於預算，屬預期行為。
///
/// 撥款明細的 SUM 驗證（審核當下 / 核准後修改兩入口）與各頁顯示的應撥總額皆以此為準，前端不得自行重算。
/// 沖銷簽核中（尚未核准）的月結金額不計入，與金額歷程「只計已核准沖銷」同一基準。
/// </summary>
public static class AdvancePayable
{
    /// <summary>月結應撥：有已核准沖銷 → 已核准沖銷的月結合計；否則 → 預支的月結預算。</summary>
    public static decimal CheckOf(decimal advanceCheck, int approvedWriteOffCount, decimal approvedWriteOffCheck)
        => approvedWriteOffCount > 0 ? approvedWriteOffCheck : advanceCheck;

    /// <summary>應撥總額 ＝ 現金預支 ＋ 月結應撥。</summary>
    public static decimal TotalOf(decimal advanceTotal, decimal advanceCheck, int approvedWriteOffCount, decimal approvedWriteOffCheck)
        => WriteOffRefundCalculator.CashOf(advanceTotal, advanceCheck)
         + CheckOf(advanceCheck, approvedWriteOffCount, approvedWriteOffCheck);

    /// <summary>Dapper 讀取端用：<paramref name="approved"/> 為 <see cref="ApprovedWriteOffSql"/> 彙總結果（查無＝default）。</summary>
    public static decimal CheckOf(decimal advanceCheck, (int Count, decimal Check) approved)
        => CheckOf(advanceCheck, approved.Count, approved.Check);

    /// <inheritdoc cref="CheckOf(decimal, ValueTuple{int, decimal})"/>
    public static decimal TotalOf(decimal advanceTotal, decimal advanceCheck, (int Count, decimal Check) approved)
        => TotalOf(advanceTotal, advanceCheck, approved.Count, approved.Check);

    /// <summary>EF 版：撥款明細寫入前取得應撥總額（AdvanceRequestHandler / ApprovalTaskHandler 共用）。</summary>
    public static async Task<decimal> TotalAsync(AppDbContext db, int advanceRequestId, decimal advanceTotal, decimal advanceCheck)
    {
        var approved = await db.WriteOffRecords
            .Where(w => w.AdvanceRequestId == advanceRequestId && w.ApprovalStatus == "approved")
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Check = g.Sum(w => w.CheckTotal) })
            .FirstOrDefaultAsync();
        return TotalOf(advanceTotal, advanceCheck, approved?.Count ?? 0, approved?.Check ?? 0m);
    }

    /// <summary>
    /// Dapper 版的原料 SQL：依預支單 Id 彙總已核准沖銷的張數與月結合計（參數 @AdvanceIds）。
    /// 回傳欄位 AdvanceRequestId / Cnt / CheckSum，由 <see cref="TotalOf"/> / <see cref="CheckOf"/> 組合。
    /// </summary>
    public const string ApprovedWriteOffSql = """
        SELECT AdvanceRequestId, COUNT(*) AS Cnt, ISNULL(SUM(CheckTotal), 0) AS CheckSum
        FROM WriteOffRecords
        WHERE AdvanceRequestId IN @AdvanceIds AND ApprovalStatus = 'approved'
        GROUP BY AdvanceRequestId
        """;

    /// <summary>Dapper 版：一次撈回多張預支單的已核准沖銷彙總（AdvanceRequestId → (張數, 月結合計)）。</summary>
    public static async Task<Dictionary<int, (int Count, decimal Check)>> LoadApprovedWriteOffsAsync(
        System.Data.IDbConnection db, IEnumerable<int> advanceIds)
    {
        var ids = advanceIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var rows = await Dapper.SqlMapper.QueryAsync<dynamic>(db, ApprovedWriteOffSql, new { AdvanceIds = ids });
        return rows.ToDictionary(r => (int)r.AdvanceRequestId, r => ((int)r.Cnt, (decimal)r.CheckSum));
    }
}
