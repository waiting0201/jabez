namespace Jabez.Api.Models.Entities;

public class OvertimeRequest
{
    public int      Id               { get; set; }
    public string?  RequestNo        { get; set; }               // OT-yyyyMMdd-NNN；送簽時取號，草稿為 null
    public Guid?    EmployeeId       { get; set; }
    public int?     ApprovalItemId   { get; set; }
    public DateTime OvertimeDate     { get; set; }
    public decimal  EstimatedHours   { get; set; }   // 合計快取 = SUM(Projects[].EstimatedHours)，由 Handler 重算

    /// <summary>
    /// 補償方式（整張單層級，二擇一）：compensatory＝補休 / pay＝加班費。
    /// 選 compensatory 才會計入補休池（見 LeaveRequestHandler.ComputeCompensatoryAsync）；
    /// 選 pay 則依勞基法分段累進倍率試算金額，隨「加班日次月」薪資發放。
    /// 預設 compensatory —— 同時是舊資料的 backfill 值，讓上線前所有已核准單原封不動留在補休池。
    /// 常數與正規化見 <see cref="Jabez.Api.Services.OvertimeCompensationService"/>。
    /// </summary>
    public string   CompensationType { get; set; } = "compensatory";

    /// <summary>
    /// 加班費金額快照（元，已捨入）。補休型恆為 null；查無底薪者寫 0（與「沒算過」的 null 區分）。
    /// 刻意存快照而非薪資端即時重算：薪資本身無月結快照表，若加班費也即時算，
    /// 一次調薪會回溯改動所有歷史月份的加班費且無從稽核。
    /// </summary>
    public decimal? OvertimePayAmount  { get; set; }

    /// <summary>時薪快照 = ROUND(BaseSalary / 240, 2)（月薪 ÷ 30 ÷ 8），供金額追溯。</summary>
    public decimal? HourlyRateSnapshot { get; set; }

    /// <summary>實際計酬時數 = min(EstimatedHours, 日別上限)。與 EstimatedHours 分離才看得出上限截斷。</summary>
    public decimal? PayableHours       { get; set; }

    /// <summary>
    /// 日別快照（true＝假日倍率、false＝平日倍率）。
    /// nullable 是刻意的：null＝尚未試算，false＝已算且為平日，兩者語意不同。
    /// </summary>
    public bool?    IsHolidayOvertime  { get; set; }

    public string   Reason           { get; set; } = string.Empty;
    public string   ApprovalStatus   { get; set; } = "pending";  // pending | approved | rejected | returned
    public int      CurrentStepOrder { get; set; } = 1;
    public Guid?    ReviewedById     { get; set; }
    public DateTime? ReviewedAt      { get; set; }
    public string?  ReviewNote       { get; set; }
    public DateTime CreatedAt        { get; set; }
    public DateTime? SubmittedAt     { get; set; }                // 送簽日期；草稿為 null，送簽當下寫入，退回重送不改

    // Navigation
    public User?         Employee           { get; set; }
    public User?         ReviewedBy         { get; set; }
    public ApprovalItem? ApprovalItem       { get; set; }

    /// <summary>關聯專案明細（一列一專案，含該專案預估時數）</summary>
    public List<OvertimeRequestProject> Projects { get; set; } = [];
}
