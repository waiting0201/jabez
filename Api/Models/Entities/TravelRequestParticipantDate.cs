using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>假日執行活動參與人員的個別參與日期（可不連續；無任何列＝全程參與）</summary>
public class TravelRequestParticipantDate
{
    public int Id { get; set; }
    public int TravelRequestParticipantId { get; set; }
    public DateTime Date { get; set; }
    /// <summary>參與時段：full=全天(1 天) / am=上半天(0.5 天) / pm=下半天(0.5 天)；權重見 ParticipantDateSlots</summary>
    public string Slot { get; set; } = ParticipantDateSlots.Full;

    // Navigation
    public TravelRequestParticipant Participant { get; set; } = null!;
}
