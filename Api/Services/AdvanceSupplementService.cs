using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 追加預支批次共用邏輯。
/// 「駁回」與「主動放棄追加」兩個入口共用同一份回滾：把父單還原成送出追加之前的已核准狀態。
/// </summary>
public static class AdvanceSupplementService
{
    public const string ContainerName = "advance-files";

    /// <summary>
    /// 解析某申請單目前的簽核批次。僅 advance 會 &gt; 1，其餘申請類型恆為 1。
    ///
    /// 「跨步驟同人去重 / 自動跳過 / 通知去重」三處都以 ApprovalRecords 判斷「此人是否已審過」，
    /// 追加預支時前一輪紀錄仍在，若不限定批次會造成：
    /// 　· 總監審過第 1 輪 → 第 2 輪被擋（看不到、也不能審）
    /// 　· 所有步驟被自動跳過 → 追加未經審核就核准
    /// 　· 應收通知的審核者被跳過
    /// 因此四處判定一律以本批次為範圍。
    /// </summary>
    public static async Task<int> ResolveCurrentRoundAsync(AppDbContext db, string? applicationType, int? applicationId)
    {
        if (applicationType != "advance" || applicationId is null) return 1;
        var round = await db.AdvanceRequests.AsNoTracking()
            .Where(a => a.Id == applicationId.Value)
            .Select(a => (int?)a.CurrentRoundNo)
            .FirstOrDefaultAsync();
        return round is > 0 ? round.Value : 1;
    }

    /// <summary>
    /// 回滾最新的追加批次（不呼叫 SaveChanges，交易邊界由呼叫端負責）。
    /// 回傳需在 SaveChanges 之後刪除的 blob 名稱清單。
    /// </summary>
    public static async Task<List<string>> RollbackAsync(
        AppDbContext db, IBlobStorageService blob, AdvanceRequest ar)
    {
        var roundNo = ar.CurrentRoundNo;
        if (roundNo <= 1) return [];

        var supplement = await db.AdvanceRequestSupplements
            .FirstOrDefaultAsync(s => s.AdvanceRequestId == ar.Id && s.RoundNo == roundNo);

        // 1. 刪除該批次明細（含 blob）
        var roundItems = await db.AdvanceRequestItems
            .Where(i => i.AdvanceRequestId == ar.Id && i.RoundNo == roundNo)
            .ToListAsync();
        var blobNames = roundItems
            .Select(i => blob.ExtractBlobName(i.FileUrl, ContainerName))
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();
        db.AdvanceRequestItems.RemoveRange(roundItems);

        // 2. 刪除該批次的簽核足跡（第 1 輪紀錄必須保留）
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords
                .Where(r => r.ApplicationType == "advance" && r.ApplicationId == ar.Id && r.RoundNo == roundNo)
                .ToListAsync());

        // 由「駁回」進來時，本次駁回的紀錄還在 change tracker 尚未寫入 DB，上面的查詢抓不到，
        // 需另外 Detach，否則 SaveChanges 會留下一筆指向已刪除批次的孤兒紀錄。
        foreach (var entry in db.ChangeTracker.Entries<ApprovalRecord>()
                     .Where(e => e.State == EntityState.Added
                              && e.Entity.ApplicationType == "advance"
                              && e.Entity.ApplicationId == ar.Id
                              && e.Entity.RoundNo == roundNo)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides
                .Where(o => o.ApplicationType == "advance" && o.ApplicationId == ar.Id)
                .ToListAsync());

        // 3. 還原父單狀態（快照存於 supplement 的 Prev* 欄位）
        ar.ApprovalStatus   = "approved";
        ar.CurrentStepOrder = supplement?.PrevCurrentStepOrder ?? ar.CurrentStepOrder;
        ar.ReviewedAt       = supplement?.PrevReviewedAt;
        ar.ReviewedById     = supplement?.PrevReviewedById;
        ar.ReviewNote       = supplement?.PrevReviewNote;

        // 4. 依剩餘明細重算總額
        var remaining = await db.AdvanceRequestItems
            .Where(i => i.AdvanceRequestId == ar.Id && i.RoundNo != roundNo)
            .ToListAsync();
        ar.CashTotal   = remaining.Sum(i => i.CashAmount);
        ar.CheckTotal  = remaining.Sum(i => i.CheckAmount);
        ar.GrandTotal  = remaining.Sum(i => i.TotalPrice);
        ar.CurrentRoundNo = roundNo - 1;

        // 5. 還原指定審核者狀態：依「留存的前幾輪簽核紀錄」回推，不憑空捏造
        await RestoreDesignatedReviewersAsync(db, ar.Id, roundNo);

        if (supplement is not null)
            db.AdvanceRequestSupplements.Remove(supplement);

        return blobNames;
    }

    /// <summary>
    /// 追加送簽時把指定審核者整組重置為 pending（比照退回重送）。
    /// </summary>
    public static async Task ResetDesignatedReviewersAsync(AppDbContext db, int advanceRequestId)
    {
        var rdrs = await db.RequestDesignatedReviewers
            .Where(r => r.RequestType == "advance" && r.RequestId == advanceRequestId)
            .ToListAsync();
        foreach (var rdr in rdrs)
        {
            rdr.Status     = "pending";
            rdr.ReviewedAt = null;
            rdr.Comment    = null;
        }
    }

    public static async Task DeleteBlobsAsync(IBlobStorageService blob, IEnumerable<string> blobNames)
    {
        foreach (var name in blobNames)
            await blob.DeleteAsync(ContainerName, name);
    }

    private static async Task RestoreDesignatedReviewersAsync(AppDbContext db, int advanceRequestId, int rolledBackRound)
    {
        var rdrs = await db.RequestDesignatedReviewers
            .Where(r => r.RequestType == "advance" && r.RequestId == advanceRequestId)
            .ToListAsync();
        if (rdrs.Count == 0) return;

        var survivingRecords = await db.ApprovalRecords.AsNoTracking()
            .Where(r => r.ApplicationType == "advance" && r.ApplicationId == advanceRequestId
                     && r.RoundNo < rolledBackRound && r.Action == "approved")
            .ToListAsync();

        foreach (var rdr in rdrs)
        {
            var rec = survivingRecords
                .Where(r => r.ReviewedById == rdr.ReviewerId && r.StepOrder == rdr.ApprovalStepOrder)
                .OrderByDescending(r => r.ReviewedAt)
                .FirstOrDefault();
            if (rec is null) continue;
            rdr.Status     = "approved";
            rdr.ReviewedAt = rec.ReviewedAt;
            rdr.Comment    = rec.ReviewNote;
        }
    }
}
