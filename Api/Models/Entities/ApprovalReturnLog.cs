namespace Jabez.Api.Models.Entities;

/// <summary>
/// 退回 / 拒絕歷程（2026-11）：審核者每次「退回修改」或「拒絕」留一列，只增不改。
///
/// 為何不直接讀 <see cref="ApprovalRecord"/>：退回後重送時各申請 Handler 的 SubmitAsync 會把
/// ApprovalRecords 整批清掉（2026-10 安全修正，殘留的舊 approved 會被誤判為「此人已審過」而自動代簽），
/// 於是重送後完全看不出「誰、為何退回」。本表與簽核判定完全無關，故重送時**不清**，
/// 已簽過的人再收到同一張單時可在簽核流程卡下方看到歷次退回原因。
///
/// 關聯：多型（ApplicationType + ApplicationId，同 ApprovalRecord）無 FK，刪申請單時由各 Handler 一併清除。
/// <see cref="ReviewedById"/> 刻意**不設 FK**、另存姓名 / 部門 / 職稱快照：審核者離職或被刪後歷程仍看得懂，
/// 也不必加進 UserHandler 的硬刪除清洗清單。
/// </summary>
public class ApprovalReturnLog
{
    public long      Id              { get; set; }
    public string    ApplicationType { get; set; } = string.Empty;
    public int       ApplicationId   { get; set; }
    public int       StepOrder       { get; set; }
    /// <summary>簽核批次（僅 advance 追加預支會 &gt; 1）</summary>
    public int       RoundNo         { get; set; } = 1;
    /// <summary>returned | rejected</summary>
    public string    Action          { get; set; } = string.Empty;
    public Guid?     ReviewedById    { get; set; }
    public string    ReviewerName    { get; set; } = string.Empty;
    public string?   ReviewerJobTitle       { get; set; }
    public string?   ReviewerDepartmentName { get; set; }
    /// <summary>代理審核：代替誰（姓名快照）</summary>
    public string?   OnBehalfOfName  { get; set; }
    public bool      IsEscalated     { get; set; }
    public DateTime  ReviewedAt      { get; set; }
    public string?   ReviewNote      { get; set; }
}
