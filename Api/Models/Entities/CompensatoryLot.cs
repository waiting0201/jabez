namespace Jabez.Api.Models.Entities;

/// <summary>
/// 補休「批次」（lot）—— 四週彈性工時把補休池從**純聚合 SUM** 改為逐筆帳務的核心表。
///
/// 現況（切換前）補休池是三個聚合相減：期初 <c>User.CompensatoryOpeningHours</c>
/// ＋ 已核准補休制加班單的 SUM(EstimatedHours) − 補休假的 SUM(Hours)，
/// FIFO 只是 <c>Math.Min(used, opening)</c> 的算術模擬，**沒有到期日、沒有加班單↔補休單對應**。
///
/// 新制每筆加班選「換取補休」時開一個 lot，並快照**當時的原始加班費率**（1.34／1.67／2.67）——
/// 到期未休完時要依該費率換算津貼，事後才算會拿到改版後的費率而算錯。
///
/// 效期（2026-09-16 客戶修訂，比產生期間多留一個月）：
///   1–6 月產生 → 用至 7/31 → 8 月薪資結算；
///   7–12 月產生 → 用至隔年 1/31 → 隔年 2 月薪資結算。
///
/// 切換當下的既有餘額整批做成一筆期初 lot（<see cref="IsOpening"/>），由一次性腳本產生、不進 migration。
/// </summary>
public class CompensatoryLot
{
    public int  Id     { get; set; }
    public Guid UserId { get; set; }

    /// <summary>來源加班單；期初 lot 為 null。</summary>
    public int? SourceOvertimeRequestId { get; set; }

    /// <summary>加班發生日（FIFO 的排序鍵，非核准日）。</summary>
    public DateTime EarnedDate { get; set; }

    /// <summary>入帳時數（加班時數 1:1 換算）。</summary>
    public decimal Hours { get; set; }

    /// <summary>尚未被扣抵的剩餘時數。扣抵一律走 CompensatoryUsage，本欄為快取。</summary>
    public decimal RemainingHours { get; set; }

    /// <summary>
    /// 原始加班費率快照（1.34 / 1.67 / 2.67）。到期未休完時據此換算津貼。
    /// 期初 lot 無從得知當時費率，為 null（結算方式另議）。
    /// </summary>
    public decimal? RateSnapshot { get; set; }

    /// <summary>使用期限（該日 23:59:59 後失效）。</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>是否為「切換日整批轉入」的期初 lot。</summary>
    public bool IsOpening { get; set; }

    /// <summary>到期結算（換算為加班津貼）的時間；null ＝ 尚未結算。</summary>
    public DateTime? SettledAt { get; set; }

    /// <summary>結算金額（進該月薪資單的獨立項目）。</summary>
    public decimal? SettledAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public User?            User            { get; set; }
    public OvertimeRequest? SourceOvertimeRequest { get; set; }
    public ICollection<CompensatoryUsage> Usages { get; set; } = [];
}
