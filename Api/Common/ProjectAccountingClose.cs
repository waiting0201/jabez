using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 專案「關帳日」判定的單一真相（2026-11-25）。
///
/// 規則：關帳日<b>當天仍可用</b>，<b>隔天起</b>帳務類申請看不到也不能選該專案 ——
/// 請款 / 預審 / 預支（含追加）/ 預支沖銷 / 出差 / 出差請款 / 出差沖銷。
/// 加班申請、下班打卡專案工時佔比、工時總表不受影響（走 <c>/projects/active?all=true</c>）。
/// 關帳日可改、可清空：延續案跨年切帳後改日期，帳務功能即恢復。
///
/// 以<b>今天</b>判定（不是單據日期）：問的是「現在還能不能用這個專案建帳」。
/// 只擋建立 / 修改 / 送簽；已送簽中的單照常簽核，已核准的單不受影響。
/// </summary>
public static class ProjectAccountingClose
{
    /// <summary>Dapper WHERE 片段（別名 p、參數 @AccountingToday）：未關帳者。</summary>
    public const string OpenSqlClause = "(p.AccountingCloseDate IS NULL OR p.AccountingCloseDate >= @AccountingToday)";

    public static bool IsClosed(DateTime? closeDate, DateTime today)
        => closeDate.HasValue && today.Date > closeDate.Value.Date;

    /// <summary>專案已過關帳日即丟 400。projectId 為 null（出差未選專案）直接通過。</summary>
    public static async Task EnsureOpenAsync(AppDbContext db, int? projectId)
    {
        if (projectId is not { } id) return;
        var p = await db.Projects.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Code, x.Name, x.AccountingCloseDate })
            .FirstOrDefaultAsync();
        if (p is null) return;   // 存在性由各 Handler 既有檢查負責
        if (IsClosed(p.AccountingCloseDate, Clock.Now))
            throw AppException.BadRequest(
                $"專案「{p.Code} {p.Name}」已於 {p.AccountingCloseDate:yyyy/MM/dd} 關帳，無法再建立或送出帳務申請；如需繼續使用請洽專案管理調整關帳日。");
    }

    /// <summary>出差沖銷：專案取自母出差單。</summary>
    public static async Task EnsureTravelOpenAsync(AppDbContext db, int travelRequestId)
        => await EnsureOpenAsync(db, await db.TravelRequests.AsNoTracking()
            .Where(t => t.Id == travelRequestId).Select(t => t.ProjectId).FirstOrDefaultAsync());
}
