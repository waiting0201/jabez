namespace Jabez.Api.Models.Entities;

public class WriteOffAttachment
{
    public int     Id              { get; set; }
    public int     WriteOffRecordId { get; set; }
    public string  FileName        { get; set; } = string.Empty;
    public string? FileUrl         { get; set; }
    public int     SortOrder       { get; set; }

    // Navigation
    public WriteOffRecord WriteOffRecord { get; set; } = null!;
}
