using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jabez.Api.Services;

/// <summary>
/// 預支申請「關聯預審單」共用邏輯（static，不呼叫 SaveChanges，交易邊界由呼叫端負責，比照 AdvanceSupplementService）。
/// multipart 欄位 <c>preReviewRequestIds</c>（JSON int 陣列）：key 不存在＝不變更、<c>[]</c>＝清空該批次。
/// 可選範圍：已核准 ∧ 申請人本人送出（不限專案）。同一張預支單內同一預審單只能出現一次（跨批次）。
/// </summary>
public static class AdvancePreReviewLinker
{
    public const string FormKey = "preReviewRequestIds";

    /// <summary>讀取 multipart 欄位；key 不存在回 null（＝不變更），格式錯誤回 400。</summary>
    public static int[]? ParseIds(IFormCollection form)
    {
        if (!form.ContainsKey(FormKey)) return null;
        var raw = form[FormKey].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return [];
        try
        {
            var ids = JsonSerializer.Deserialize<int[]>(raw);
            return ids is null ? [] : ids.Distinct().ToArray();
        }
        catch (JsonException)
        {
            throw AppException.BadRequest("preReviewRequestIds 格式錯誤，須為整數陣列。");
        }
    }

    /// <summary>逐一驗證：存在 ∧ 已核准 ∧ 申請人本人送出；不符回 400 並點名單號（無單號者以 Id 表示）。</summary>
    public static async Task ValidateAsync(AppDbContext db, IReadOnlyCollection<int> ids, Guid? advanceSubmitterId)
    {
        if (ids.Count == 0) return;

        var rows = await db.PreReviewRequests.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.RequestNo, p.ApprovalStatus, p.SubmittedById })
            .ToListAsync();

        foreach (var id in ids)
        {
            var row = rows.FirstOrDefault(r => r.Id == id)
                      ?? throw AppException.BadRequest($"預審單 {id} 不存在。");
            var label = row.RequestNo ?? $"#{row.Id}";
            if (row.ApprovalStatus != "approved")
                throw AppException.BadRequest($"預審單 {label} 尚未核准，不可關聯。");
            if (advanceSubmitterId is null || row.SubmittedById != advanceSubmitterId)
                throw AppException.BadRequest($"預審單 {label} 非申請人本人送出，不可關聯。");
        }
    }

    /// <summary>
    /// 只替換該批次（RoundNo）的關聯列；已存在於其他批次者回 400「PRV-xxx 已於第 N 次預支引用」。
    /// 呼叫前須已通過 <see cref="ValidateAsync"/>。
    /// </summary>
    public static async Task ReplaceRoundAsync(AppDbContext db, int advanceRequestId, int roundNo, IReadOnlyCollection<int> ids)
    {
        var existing = await db.AdvanceRequestPreReviews
            .Where(l => l.AdvanceRequestId == advanceRequestId)
            .ToListAsync();

        var conflict = existing.FirstOrDefault(l => l.RoundNo != roundNo && ids.Contains(l.PreReviewRequestId));
        if (conflict is not null)
        {
            var no = await db.PreReviewRequests.AsNoTracking()
                .Where(p => p.Id == conflict.PreReviewRequestId).Select(p => p.RequestNo).FirstOrDefaultAsync();
            throw AppException.BadRequest($"{no ?? $"#{conflict.PreReviewRequestId}"} 已於第 {conflict.RoundNo} 次預支引用。");
        }

        var thisRound = existing.Where(l => l.RoundNo == roundNo).ToList();
        db.AdvanceRequestPreReviews.RemoveRange(thisRound.Where(l => !ids.Contains(l.PreReviewRequestId)));

        var have = thisRound.Select(l => l.PreReviewRequestId).ToHashSet();
        foreach (var id in ids.Where(i => !have.Contains(i)))
        {
            db.AdvanceRequestPreReviews.Add(new AdvanceRequestPreReview
            {
                AdvanceRequestId   = advanceRequestId,
                PreReviewRequestId = id,
                RoundNo            = roundNo,
                CreatedAt          = Clock.Now,
            });
        }
    }

    /// <summary>刪除某批次的全部關聯列（追加批次駁回 / 放棄時使用）。</summary>
    public static async Task RemoveRoundAsync(AppDbContext db, int advanceRequestId, int roundNo)
    {
        db.AdvanceRequestPreReviews.RemoveRange(
            await db.AdvanceRequestPreReviews
                .Where(l => l.AdvanceRequestId == advanceRequestId && l.RoundNo == roundNo)
                .ToListAsync());
    }
}
