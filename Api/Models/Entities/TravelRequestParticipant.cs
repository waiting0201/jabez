namespace Jabez.Api.Models.Entities;

public class TravelRequestParticipant
{
    public int Id { get; set; }
    public int TravelRequestId { get; set; }
    public Guid UserId { get; set; }
    public int SortOrder { get; set; }
    // 個人假日天數（Submit 時依勾選參與日期 × 時段權重計算；NULL=全程參與，沿用整單 HolidayDays）
    // 半天（am / pm）以 0.5 計，故為 decimal(5,1)
    public decimal? HolidayDays { get; set; }

    // Navigation
    public TravelRequest TravelRequest { get; set; } = null!;
    public User User { get; set; } = null!;
    public List<TravelRequestParticipantDate> Dates { get; set; } = new();
}
