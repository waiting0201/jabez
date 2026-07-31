using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class ApprovalStep
{
    public int      Id             { get; set; }
    public int      ApprovalItemId { get; set; }
    public int      StepOrder      { get; set; }
    public int?     DepartmentId   { get; set; }
    public int?     JobTitleId     { get; set; }
    public bool     UseApplicantDepartment { get; set; } = false;
    public bool     UseDirectSupervisor    { get; set; } = false;
    public bool     UseApplicantDesignated { get; set; } = false;
    public bool     DesignatedRequiresDepartment { get; set; } = false; // 僅 UseApplicantDesignated=true 時有意義：此步驟需先選部門再選人
    public int?     MinDays        { get; set; }                        // 適用天數門檻：null＝一律納入；有值時僅當申請天數 >= MinDays 才納入此步驟（目前供請假依天數分流）
    public string?  Note           { get; set; }
    public DateTime CreatedAt      { get; set; } = Clock.Now;

    // Navigation
    public ApprovalItem  ApprovalItem { get; set; } = null!;
    public Department?   Department   { get; set; }
    public JobTitle?     JobTitle     { get; set; }

    /// <summary>例外指定審核名單：名單內的申請人送單時，此步驟改為「由申請人自行指定審核者」（與 UseApplicantDesignated 互斥）。</summary>
    public ICollection<ApprovalStepException> Exceptions { get; set; } = [];

    /// <summary>例外指定審核的限定職稱：申請人只能從這些職稱的人員中指定審核者（空＝不限職稱）。僅例外步驟有意義。</summary>
    public ICollection<ApprovalStepDesignatedJobTitle> DesignatedJobTitles { get; set; } = [];
}
