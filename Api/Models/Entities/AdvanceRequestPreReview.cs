using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 預支申請關聯的預審單（多對多連結表）。
/// 一張預支單可引用多張預審單；同一張預審單可被多張預支單引用，但同一張預支單內只能出現一次
/// （跨批次，唯一索引 AdvanceRequestId + PreReviewRequestId）。
/// RoundNo 記錄是哪一批預支加選的：1 = 原始預支，≥ 2 = 追加批次（批次被駁回 / 放棄時一併刪除）。
/// 預審單本身不因此被鎖定；僅「被引用中不可刪除」由 NoAction FK 把關。
/// </summary>
public class AdvanceRequestPreReview
{
    public int      Id                 { get; set; }
    public int      AdvanceRequestId   { get; set; }
    public int      PreReviewRequestId { get; set; }
    public int      RoundNo            { get; set; } = 1;
    public DateTime CreatedAt          { get; set; } = Clock.Now;

    // Navigation
    public AdvanceRequest   AdvanceRequest   { get; set; } = null!;
    public PreReviewRequest PreReviewRequest { get; set; } = null!;
}
