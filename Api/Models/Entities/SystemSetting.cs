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

    // ── 撥款提醒設定（TimerTrigger 用）──────────────────────
    /// <summary>預計撥款日提前提醒天數（預設 3 天；範圍 0 = 當日, 1-30）。</summary>
    public int    PaymentReminderDaysBefore { get; set; } = 3;

    // ── 四週彈性工時（§30-1）────────────────────────────────
    /// <summary>
    /// 四週彈性工時的「制度切換日」。<c>null</c> ＝ 尚未切換，全系統維持舊制（08:00–17:00、午休 12:00–13:00）。
    ///
    /// 設為某日後，該日（含）起的單據改用新制（09:00–18:00、午休 12:30–13:30、個人排例／休）。
    /// **刻意做成可設定值而非程式常數**：需求方決議「做好、驗收沒問題才切換」，
    /// 寫死常數會讓每次改期都要重新部署（見 flexible-work-hours.md §10.5）。
    ///
    /// 判定基準因單據而異，一律比對「該筆資料自己的日期」而非今天：
    /// 請假單看 <c>StartDate</c>、打卡看 <c>RecordDate</c>、報表看該列日期 —— 因為舊資料不遷移
    /// （§10.2），系統內會同時存在兩套時段，回看歷史月份必須拿舊制常數去比對舊單。
    ///
    /// 單一真相為 <see cref="Common.WorkdayHours.For"/>，不要在各處自己比日期。
    /// 建議切換日訂在月初（薪資即時重算、無月結快照，月中切會讓同一個月跨兩套制度）。
    /// </summary>
    public DateTime? FlexibleWorkStartDate { get; set; }
}
