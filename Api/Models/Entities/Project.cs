namespace Jabez.Api.Models.Entities;

public class Project
{
    public int       Id              { get; set; }
    public string    Code            { get; set; } = string.Empty;
    public string    Name            { get; set; } = string.Empty;
    public string    Status          { get; set; } = "active";   // active | closed
    public DateTime  StartDate       { get; set; }
    public DateTime? EndDate         { get; set; }
    public int?      DepartmentId    { get; set; }
    public decimal?  ReceivedAmount  { get; set; }   // 實收金額（原 BudgetAmount）
    public decimal?  ContractAmount  { get; set; }   // 契約金額（原 ActualAmount）
    public decimal?  BusinessAmount  { get; set; }
    public string?   GoogleDriveUrl  { get; set; }
    public DateTime  CreatedAt       { get; set; }

    // Navigation
    public Department?                         Department       { get; set; }
    public ICollection<PaymentRequest>         PaymentRequests  { get; set; } = [];
    public ICollection<AdvanceRequest>         AdvanceRequests  { get; set; } = [];
    public ICollection<ProjectPaymentSchedule> PaymentSchedules { get; set; } = [];
}
