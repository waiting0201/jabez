namespace Jabez.Api.Models.Entities;

public class Project
{
    public int       Id              { get; set; }
    public string    Code            { get; set; } = string.Empty;
    public string    Name            { get; set; } = string.Empty;
    public string    Status          { get; set; } = "active";   // active | closed
    public DateTime  StartDate       { get; set; }
    public DateTime? EndDate         { get; set; }
    public int       DepartmentId    { get; set; }
    public decimal?  ContractAmount  { get; set; }   // 契約金額
    public decimal?  BusinessAmount  { get; set; }   // 業務執行金額
    public decimal?  RemainingAmount { get; set; }   // 剩餘金額（系統導入時的契約剩餘預算）
    public string?   GoogleDriveUrl  { get; set; }
    /// <summary>
    /// 關帳日（2026-11-25，可改、可清空）：**隔天起**帳務類申請（請款 / 預審 / 預支含追加 / 預支沖銷 / 出差 / 出差請款 / 出差沖銷）
    /// 看不到也不能選此專案；加班與下班打卡工時佔比不受影響。判定單一真相 Common/ProjectAccountingClose。
    /// </summary>
    public DateTime? AccountingCloseDate { get; set; }
    public DateTime  CreatedAt       { get; set; }

    // Navigation
    public Department?                         Department       { get; set; }
    public ICollection<PaymentRequest>         PaymentRequests  { get; set; } = [];
    public ICollection<AdvanceRequest>         AdvanceRequests  { get; set; } = [];
    public ICollection<ProjectPaymentSchedule> PaymentSchedules { get; set; } = [];
}
