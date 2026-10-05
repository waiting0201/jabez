namespace Jabez.Api.Models.Entities;

/// <summary>
/// 打卡嘗試紀錄（防機器人打卡，2026-10 hotfix）：本人四個打卡動作的**每一次嘗試**都留一列，
/// 成功與被擋下皆記錄，供事後稽核（IP / User-Agent / GPS / 挑戰碼停留時間）。
/// 判定規則見 <see cref="Jabez.Api.Services.AttendancePunchGuard"/>。
/// 系統自動補卡與管理者修改不經此流程、不留紀錄。
/// </summary>
public class AttendancePunchLog
{
    public long      Id             { get; set; }
    public Guid      UserId         { get; set; }
    /// <summary>clock-in / clock-out / overtime-start / overtime-end</summary>
    public string    Action         { get; set; } = string.Empty;
    public DateTime  AttemptedAt    { get; set; }
    public bool      Succeeded      { get; set; }
    /// <summary>被擋下的原因代碼（見 AttendancePunchGuard.BlockReasons），成功為 null</summary>
    public string?   BlockReason    { get; set; }
    public double?   Latitude       { get; set; }
    public double?   Longitude      { get; set; }
    /// <summary>瀏覽器回報的定位精度（公尺）</summary>
    public double?   Accuracy       { get; set; }
    public string?   IpAddress      { get; set; }
    public string?   UserAgent      { get; set; }
    /// <summary>挑戰碼的 nonce；成功列以 filtered unique index 保證一碼只能用一次</summary>
    public string?   ChallengeNonce { get; set; }
    /// <summary>挑戰碼簽發到送出打卡的毫秒數（機器人通常極短）</summary>
    public int?      ChallengeAgeMs { get; set; }

    // Navigation
    public User? User { get; set; }
}
