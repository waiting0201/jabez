namespace Jabez.Api.Models.Entities;

public class LeaveRequest
{
    public int      Id             { get; set; }
    public Guid?    EmployeeId     { get; set; }
    public int?     ApprovalItemId { get; set; }
    public string   LeaveType      { get; set; } = string.Empty; // annual | personal | sick | compensatory | marriage | bereavement | official | maternity | miscarriage_3m | miscarriage_2to3m | miscarriage_under2m | prenatal_checkup | paternity | ceremonial_festival | senior_executive | menstrual | family_care | parental_leave | parental_leave_daily
    public DateTime StartDate      { get; set; }
    public DateTime EndDate        { get; set; }
    /// <summary>剩餘有效時數。銷假核准後遞減（見 LeaveRevocationService.ApplyAsync），下游扣薪 / 額度一律以此為準。</summary>
    public decimal  Hours          { get; set; }
    /// <summary>原始請假時數；僅在第一次銷假核准時寫入（null＝從未銷假）。供顯示「原 40h / 已銷 8h」與重算冪等。</summary>
    public decimal? OriginalHours  { get; set; }
    public string   Reason         { get; set; } = string.Empty;
    public string   ApprovalStatus   { get; set; } = "pending";  // pending | approved | rejected | returned | cancelled（全數銷假）
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById   { get; set; }
    public DateTime? ReviewedAt   { get; set; }
    public string?  ReviewNote     { get; set; }
    public string?  BereavementRelationship { get; set; }  // 喪假親屬關係
    /// <summary>子女出生日期（育嬰留停專用）。驗證「子女未滿 3 歲」，並作為「每名子女合計 2 年額度」的累計分組鍵。</summary>
    public DateTime? ChildBirthDate { get; set; }
    /// <summary>留停期間是否續保勞健保（育嬰留停專用）。僅記錄意願供人事作業參考，系統不算遞延帳、不自動扣款。</summary>
    public bool?    ContinueInsurance { get; set; }
    public Guid?    AgentUserId    { get; set; }  // 職務代理人（記錄 + 通知，不參與簽核）
    public DateTime CreatedAt      { get; set; }
    public DateTime? SubmittedAt   { get; set; }                  // 送簽日期；草稿為 null，送簽當下寫入，退回重送不改

    // Navigation
    public User?         Employee           { get; set; }
    public User?         ReviewedBy         { get; set; }
    public User?         AgentUser          { get; set; }
    public ApprovalItem? ApprovalItem       { get; set; }
    public ICollection<LeaveRevocation> Revocations { get; set; } = [];
}
