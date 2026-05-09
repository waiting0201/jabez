namespace Jabez.Api.Models.Entities;

/// <summary>
/// 系統設定（單行模式，Id 固定為 1）。
/// </summary>
public class SystemSetting
{
    public int    Id                       { get; set; }

    // ── 站台設定 ─────────────────────────────────────────────
    public string SiteName                 { get; set; } = "Jabez Admin";
    public string SiteUrl                  { get; set; } = "https://admin.jabez.com";
    public string ContactEmail             { get; set; } = "admin@jabez.com";
    public string SiteDescription          { get; set; } = "Enterprise administration portal";
    public string Language                 { get; set; } = "zh-TW";
    public string Timezone                 { get; set; } = "Asia/Taipei";
    public int    SessionTimeoutMinutes    { get; set; } = 60;
    public bool   AllowRegistration        { get; set; }
    public bool   RequireEmailVerification { get; set; } = true;
    public bool   MaintenanceMode          { get; set; }
    public string MaintenanceMessage       { get; set; } = "System is under maintenance. Please try again later.";

    // ── 工時設定 ─────────────────────────────────────────────
    public string WorkStartTime            { get; set; } = "09:00";
    public string WorkEndTime              { get; set; } = "18:00";
    public int    MonthlyOvertimeLimit     { get; set; } = 46;

    // ── 通知設定 ─────────────────────────────────────────────
    /// <summary>是否寄送簽核流程相關 Email（待審核 / 結果 / 撥款 / 退款 / 財務）。</summary>
    public bool   ApprovalEmailEnabled     { get; set; } = true;
    /// <summary>是否推播簽核流程相關 LINE 訊息（範圍同 Email）。</summary>
    public bool   ApprovalLineEnabled      { get; set; } = true;
}
