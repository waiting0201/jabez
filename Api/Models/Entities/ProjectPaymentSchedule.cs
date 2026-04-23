namespace Jabez.Api.Models.Entities;

/// <summary>專案請款期別明細（一期一筆，涵蓋請款 / 發票 / 入帳 / 扣款備註）</summary>
public class ProjectPaymentSchedule
{
    public Guid      Id             { get; set; }
    public int       ProjectId      { get; set; }
    public int       PeriodNo       { get; set; }   // 期別順序（1, 2, 3...）
    public DateTime? BillingDate    { get; set; }
    public decimal?  BillingAmount  { get; set; }
    public DateTime? InvoiceDate    { get; set; }
    public decimal?  InvoiceAmount  { get; set; }
    public DateTime? DepositDate    { get; set; }
    public decimal?  DepositAmount  { get; set; }
    public string?   DeductionNote  { get; set; }   // 扣款金額 = InvoiceAmount − DepositAmount，由前端/報表計算，不存 DB

    public Project?  Project        { get; set; }
}
