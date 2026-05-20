namespace Jabez.Api.Models.Entities;

public class TravelRequestInstallment
{
    public int       Id              { get; set; }
    public int       TravelRequestId { get; set; }
    public int       InstallmentNo   { get; set; }
    public DateTime  ExpectedDate    { get; set; }
    public DateTime? PaidAt          { get; set; }
    public decimal   Amount          { get; set; }
    public string?   Note            { get; set; }
    public Guid?     PaidByUserId    { get; set; }
    public DateTime  CreatedAt       { get; set; }
    public DateTime  UpdatedAt       { get; set; }

    // Navigation
    public TravelRequest TravelRequest { get; set; } = null!;
    public User?         PaidBy        { get; set; }
}
