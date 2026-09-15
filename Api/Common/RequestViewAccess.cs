using System.Security.Claims;
using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 申請單「單筆詳情」的檢視授權單一真相（申請人以外的人能不能讀這張單）。
///
/// 起因：簽核作業詳情頁的列印按鈕（2026-09 移到頁首、各階段皆可按）是走**申請單本身**的
/// <c>GET /{type}-requests/{id}</c> 取 PDF 原料，但該端點原本只放行「申請人本人 / Superadmin」，
/// 於是審核者一按列印就拿到 404 →「載入 XX 申請資料失敗，無法匯出 PDF」。
/// 五個同類 handler 當時各寫各的（travel / travel_payment 只認申請人、write_off 系列認「已審 ∪ 指定審核」、
/// advance 完全不判），故收斂於此。
///
/// 判準刻意與 <c>GET /approval-tasks/{appType}/{id}</c> 的存取控制一致 ——
/// 能開簽核詳情頁的人本來就看得到整張單的內容（明細、金額、指定審核者），
/// 故這裡不是放寬授權面，只是讓兩條取得同一份資料的路徑對齊。
/// </summary>
public static class RequestViewAccess
{
    /// <summary>
    /// 呼叫者是否可檢視此申請單：
    /// 申請人本人 ∪ Superadmin ∪ 持 approval-tasks:read（＝簽核台可見者）
    /// ∪ 曾審核過（ApprovalRecord）∪ 被指定為審核者 ∪ 被指派升級審核。
    /// </summary>
    /// <param name="applicationType">多型足跡表的申請類型（travel / holiday_travel / write_off …），須與送簽時寫入的值一致。</param>
    /// <param name="isApplicant">呼叫者是否為申請人本人（各表欄位不同，由呼叫端判定後傳入）。</param>
    public static async Task<bool> CanViewAsync(
        AppDbContext db, ClaimsPrincipal? principal, Guid callerId, string applicationType, int requestId, bool isApplicant)
    {
        if (isApplicant)
            return true;

        if (string.Equals(principal?.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
            return true;

        if (principal?.FindAll("permissions").Any(c => c.Value == PermissionCodes.ApprovalTasksRead) == true)
            return true;

        if (await db.ApprovalRecords.AsNoTracking()
                .AnyAsync(ar => ar.ApplicationType == applicationType && ar.ApplicationId == requestId && ar.ReviewedById == callerId))
            return true;

        if (await db.RequestDesignatedReviewers.AsNoTracking()
                .AnyAsync(r => r.RequestType == applicationType && r.RequestId == requestId && r.ReviewerId == callerId))
            return true;

        return await db.EscalationOverrides.AsNoTracking()
            .AnyAsync(e => e.ApplicationType == applicationType && e.ApplicationId == requestId && e.ReviewerId == callerId);
    }
}
