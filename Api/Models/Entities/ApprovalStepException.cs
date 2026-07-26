using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 簽核步驟的「例外指定審核」名單：名單內的申請人送單時，此步驟改為「由申請人自行指定審核者」，
/// 不在名單內的申請人仍照步驟原設定（部門／職稱、上層級、申請人部門）走。
/// 僅在 ApprovalStep.UseApplicantDesignated == false 時有意義（互斥，由 ApprovalHandler 守門）。
/// 註：本表僅為【送單前 / 送單當下】的真相來源；送單完成後一律改看
/// RequestDesignatedReviewers 快照（見 DesignatedReviewerHelper）。
/// </summary>
public class ApprovalStepException
{
    public int      Id             { get; set; }
    public int      ApprovalStepId { get; set; }
    public Guid     UserId         { get; set; }
    public DateTime CreatedAt      { get; set; } = Clock.Now;

    // Navigation
    public ApprovalStep ApprovalStep { get; set; } = null!;
    public User         User         { get; set; } = null!;
}
