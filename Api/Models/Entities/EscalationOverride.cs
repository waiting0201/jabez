using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 升級審核指派：當申請人即審核者（自審）時，記錄被指派的升級審核者。
/// 用於 Dapper 查詢篩選待審核任務，以及 AuthorizeStep 驗證。
/// </summary>
public class EscalationOverride
{
    public int      Id               { get; set; }
    public string   ApplicationType  { get; set; } = string.Empty;
    public int      ApplicationId    { get; set; }
    public int      StepOrder        { get; set; }
    public Guid     ReviewerId       { get; set; }
    public Guid?    OnBehalfOfUserId { get; set; }
    public DateTime CreatedAt        { get; set; } = Clock.Now;

    // Navigation
    public User  Reviewer       { get; set; } = null!;
    public User? OnBehalfOfUser { get; set; }
}
