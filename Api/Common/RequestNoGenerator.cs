using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 申請單號取號單一真相 —— 格式 <c>{prefix}yyyyMMdd-NNN</c>（當日流水號，每日從 001 起算）。
///
/// **取號時機為「送簽當下」，不是建立草稿時**（2026-09 變更）：草稿階段 RequestNo 為 null，
/// 使用者在 SubmitAsync 送出申請的那一刻才配號，故單號日期＝送簽日，草稿刪除也不會留下缺號。
///
/// 呼叫端（7 個 Handler 的 SubmitAsync）必須遵守三條守則：
///   1. **只在 RequestNo 為空時取號** —— SubmitAsync 同時服務「草稿首次送簽」與「退回（returned）後重送」，
///      少了這個判斷，退回重送會把已流通的單號改成重送當天的新號。
///   2. **放在狀態閘門之後、Superadmin 自動核准早退分支之前** —— 早退分支會直接 return，
///      放在它後面會讓 Superadmin 送的單沒有單號。
///   3. **追加預支批次（isSupplementRound）不重新取號** —— 由守則 1 天然涵蓋（父單已有號）。
///
/// 併發（兩人同秒送簽同類申請）仍可能取到同號，靠各表的 RequestNo 唯一索引擋下第二筆，
/// 此風險與改動前的建單階段取號相同。
/// </summary>
public static class RequestNoGenerator
{
    /// <summary>
    /// 取當日下一個單號。<paramref name="existingNos"/> 傳該表的單號集合
    /// （例如 <c>db.PaymentRequests.Select(p =&gt; p.RequestNo)</c>）；
    /// 草稿的 null 不會匹配 LIKE 前綴，也會被 MAX 忽略，故不影響流水號。
    /// </summary>
    public static async Task<string> NextAsync(IQueryable<string?> existingNos, string prefix, DateTime today)
    {
        var full = $"{prefix}{today:yyyyMMdd}-";
        var maxNo = await existingNos
            .Where(no => no != null && no.StartsWith(full))
            .MaxAsync(no => no);

        int seq = 1;
        if (maxNo is not null)
        {
            var seqStr = maxNo[full.Length..];
            if (int.TryParse(seqStr, out var parsed))
                seq = parsed + 1;
        }
        return $"{full}{seq:D3}";
    }
}
