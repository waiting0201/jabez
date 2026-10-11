using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class RefreshToken
{
    public int      Id        { get; set; }
    public string   Token     { get; set; } = string.Empty;
    public Guid     UserId    { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool     IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = Clock.Now;

    /// <summary>
    /// 這條輪替鏈的**原始登入時間**：登入時等於 CreatedAt，refresh 輪替時由舊 token 原樣帶下來。
    /// 超過 <see cref="AuthPolicy.RefreshAbsoluteDays"/> 天就必須重新登入（輪替不延長）。
    /// </summary>
    public DateTime SessionStartedAt { get; set; } = Clock.Now;

    /// <summary>撤銷時間（Clock.Now）；舊資料為 null。用於區分「競態重送」與「被盜用重用」。</summary>
    public DateTime? RevokedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
