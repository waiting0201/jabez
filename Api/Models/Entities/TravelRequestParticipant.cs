namespace Jabez.Api.Models.Entities;

public class TravelRequestParticipant
{
    public int Id { get; set; }
    public int TravelRequestId { get; set; }
    public Guid UserId { get; set; }
    public int SortOrder { get; set; }

    // Navigation
    public TravelRequest TravelRequest { get; set; } = null!;
    public User User { get; set; } = null!;
}
