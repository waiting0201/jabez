namespace Jabez.Api.Models.Entities;

/// <summary>
/// 加班申請的關聯專案明細（一列一專案，含該專案的預估時數）。
/// 父表 OvertimeRequest.EstimatedHours = 本表 EstimatedHours 的合計快取，由 Handler 重算。
/// </summary>
public class OvertimeRequestProject
{
    public int     Id                { get; set; }
    public int     OvertimeRequestId { get; set; }
    public int     ProjectId         { get; set; }
    public decimal EstimatedHours    { get; set; }
    public int     SortOrder         { get; set; }

    // Navigation
    public OvertimeRequest? OvertimeRequest { get; set; }
    public Project?         Project         { get; set; }
}
