using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 「例外指定審核」步驟的限定職稱名單：申請人在此步驟自行指定審核者時，
/// 只能從這些職稱的人員中挑選（空名單＝不限職稱，維持原行為）。
/// 僅在該步驟有 <see cref="ApprovalStepException"/> 名單時有意義
/// （UseApplicantDesignated=true 的原生指定步驟一律清空，由 ApprovalHandler 守門）。
/// 註：本表與例外名單同屬【送單前 / 送單當下】的真相來源；送單完成後一律改看
/// RequestDesignatedReviewers 快照（見 DesignatedReviewerHelper）。
/// </summary>
public class ApprovalStepDesignatedJobTitle
{
    public int      Id             { get; set; }
    public int      ApprovalStepId { get; set; }
    public int      JobTitleId     { get; set; }
    public DateTime CreatedAt      { get; set; } = Clock.Now;

    // Navigation
    public ApprovalStep ApprovalStep { get; set; } = null!;
    public JobTitle     JobTitle     { get; set; } = null!;
}
