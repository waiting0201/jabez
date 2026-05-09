using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>專業訓練紀錄（1 User : N ProfessionalTraining）</summary>
public class ProfessionalTraining
{
    public Guid   Id           { get; set; }
    public Guid   UserId       { get; set; }
    public string TrainingName { get; set; } = string.Empty;   // 訓練名稱
    public string? TrainingOrg { get; set; }                   // 訓練機構
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate   { get; set; }
    public decimal? Hours      { get; set; }                   // 訓練時數

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;
}
