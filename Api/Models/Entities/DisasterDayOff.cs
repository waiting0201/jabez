using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 天然災害停班（颱風假）：某一天、某個縣市、某個時段（全天 / 上午 / 下午）停止上班。
///
/// <b>刻意不寫進 <c>CalendarDay</c></b>：行事曆的 <c>IsHoliday</c> / 國定假日判準被假日津貼、加班日別、
/// 排班配額、國定假日加倍工資等多處消費，且 <c>ImportYearAsync</c> 是整年 RemoveRange 重建（手改會被洗掉）；
/// 颱風假只做一件事 —— 把該員該時段從「應出勤」移除（單一真相 <c>IDisasterDayOffReadService.GetSegmentsAsync</c>）。
///
/// 縣市維度見 <see cref="Common.TaiwanCounties"/>；員工的工作地解析為
/// User.WorkCounty ?? Department.County ?? 祖先部門 County ?? null（null ＝ 不套用）。
///
/// 建立 / 取消者只存 Id 與姓名快照、<b>不設 FK</b>：避免新增一條指向 Users 的 NO_ACTION 外鍵
/// （UserHandler.DeleteAsync 的清洗清單須同步），歷史紀錄也不該因帳號被刪而失去「誰建的」。
/// </summary>
public class DisasterDayOff
{
    public int       Id            { get; set; }
    /// <summary>停班日（只取日期）</summary>
    public DateTime  Date          { get; set; }
    /// <summary>縣市代碼（TaiwanCounties 的 key，如 ILA）</summary>
    public string    County        { get; set; } = string.Empty;
    /// <summary>full / am / pm（DisasterSegments）</summary>
    public string    Segment       { get; set; } = DisasterSegments.Full;
    /// <summary>是否給薪。自動抓取者套用 SystemSetting.DisasterDayOffPaidDefault，HR 可逐筆改。</summary>
    public bool      IsPaid        { get; set; } = true;
    /// <summary>dgpa（人事行政總處公告自動建立）/ manual（HR 手動）</summary>
    public string    Source        { get; set; } = DisasterDayStatus.SourceManual;
    /// <summary>公告原文（dgpa 來源）；供 HR 核對</summary>
    public string?   SourceText    { get; set; }
    /// <summary>active / cancelled。取消者保留列：HR 取消過的同日同縣市，自動抓取不得再重建。</summary>
    public string    Status        { get; set; } = DisasterDayStatus.Active;
    public DateTime  CreatedAt     { get; set; }
    public Guid?     CreatedById   { get; set; }
    public string?   CreatedByName { get; set; }
    public DateTime? CancelledAt   { get; set; }
    public Guid?     CancelledById { get; set; }
    public string?   CancelledByName { get; set; }
}
