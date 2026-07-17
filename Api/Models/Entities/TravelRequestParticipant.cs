namespace Jabez.Api.Models.Entities;

public class TravelRequestParticipant
{
    public int Id { get; set; }
    public int TravelRequestId { get; set; }
    public Guid UserId { get; set; }
    public int SortOrder { get; set; }
    public int? HolidayDays { get; set; }   // 個人假日天數（Submit 時依勾選參與日期計算；NULL=全程參與，沿用整單 HolidayDays）

    // Navigation
    public TravelRequest TravelRequest { get; set; } = null!;
    public User User { get; set; } = null!;
    public List<TravelRequestParticipantDate> Dates { get; set; } = new();
}
