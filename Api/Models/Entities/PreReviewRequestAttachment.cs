namespace Jabez.Api.Models.Entities;

public class PreReviewRequestAttachment
{
    public int     Id                 { get; set; }
    public int     PreReviewRequestId { get; set; }
    public string  FileName           { get; set; } = string.Empty;
    public string? FileUrl            { get; set; }
    public int     SortOrder          { get; set; }

    // Navigation
    public PreReviewRequest PreReviewRequest { get; set; } = null!;
}
