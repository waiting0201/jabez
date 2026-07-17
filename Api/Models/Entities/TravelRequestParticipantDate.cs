namespace Jabez.Api.Models.Entities;

/// <summary>假日執行活動參與人員的個別參與日期（可不連續；無任何列＝全程參與）</summary>
public class TravelRequestParticipantDate
{
    public int Id { get; set; }
    public int TravelRequestParticipantId { get; set; }
    public DateTime Date { get; set; }

    // Navigation
    public TravelRequestParticipant Participant { get; set; } = null!;
}
