namespace Jabez.Api.Models.Entities;

/// <summary>
/// 使用者帳號異動稽核（2026-10 提權修補）：管理者經 <c>PUT /users/{id}</c> 修改帳號、設定他人密碼、
/// 寄送帳號通知（會重設密碼）時留一列「誰、對誰、做了什麼、改了哪些欄位」。只增不改不刪。
///
/// <see cref="TargetUserId"/> / <see cref="OperatorUserId"/> 刻意**不設 FK**：稽核紀錄必須比被稽核的帳號活得久
/// （帳號被硬刪除後仍要查得到誰動過），也不必加進 UserHandler 刪除時的 NO_ACTION 外鍵清洗清單。
/// <see cref="Changes"/> 只記欄位名與非敏感欄位的前後值；薪資欄只記「已變更」、密碼只記「已重設」，不落任何明文。
/// </summary>
public class UserAuditLog
{
    public Guid     Id             { get; set; }
    public Guid     TargetUserId   { get; set; }
    public Guid     OperatorUserId { get; set; }
    /// <summary>update / set_password / send_credentials</summary>
    public string   Action         { get; set; } = string.Empty;
    /// <summary>變更摘要（例：departmentId: 3 → 5; jobTitleId: 4 → 2; baseSalary: changed）</summary>
    public string?  Changes        { get; set; }
    public DateTime CreatedAt      { get; set; }
}
