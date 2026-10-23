namespace Jabez.Api.Models.Entities;

/// <summary>
/// 人事行政總處公告的每次抓取紀錄：以 <see cref="SourceUpdatedAt"/>（頁面「更新時間」）去重，
/// 並保存「待人工判斷項目」（部分鄉鎮 / 無法辨識 / 公告後改為照常）供 HR 查閱與通知去重。
/// </summary>
public class DisasterDayOffFetchLog
{
    public int       Id              { get; set; }
    /// <summary>抓取時間（Clock.Now，台北）</summary>
    public DateTime  FetchedAt       { get; set; }
    /// <summary>頁面標示的更新時間；解析失敗為 null</summary>
    public DateTime? SourceUpdatedAt { get; set; }
    public bool      Success         { get; set; }
    /// <summary>auto（TimerTrigger）/ manual（Superadmin 手動）</summary>
    public string    TriggerSource   { get; set; } = "auto";
    /// <summary>結果摘要（建立 N 筆 / 待人工 M 筆 / 失敗原因）</summary>
    public string    Summary         { get; set; } = string.Empty;
    /// <summary>待人工項目 JSON：[{ county, countyName, text }]</summary>
    public string?   ManualItemsJson { get; set; }
}
