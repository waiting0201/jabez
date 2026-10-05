namespace Jabez.Api.Models.Entities;

/// <summary>
/// 出缺勤異動紀錄（2026-10 防灌工時）：管理者每次經 <c>PUT/PATCH /attendances/{id}</c> 修改打卡紀錄，
/// 留一列「誰、何時、改了哪些欄位（前後值）、備註」。只增不改不刪，供勞檢與內稽。
/// 本人打卡與系統自動補卡不經此流程、不留紀錄（前者見 AttendancePunchLog）。
///
/// 關聯：僅對 <see cref="AttendanceRecord"/> 設 Cascade FK（紀錄隨員工刪除時一併清除）。
/// <see cref="ModifiedById"/> / <see cref="OwnerUserId"/> 刻意**不設 FK**：兩者都指向 Users，
/// 再加上 Users → AttendanceRecords → 本表 的 Cascade，會撞 SQL Server 1785 multiple cascade paths；
/// 因此改存姓名快照 <see cref="ModifiedByName"/>，管理者離職 / 被刪後異動紀錄仍看得懂。
/// </summary>
public class AttendanceAuditLog
{
    public long      Id                 { get; set; }
    public int       AttendanceRecordId { get; set; }
    /// <summary>被修改的打卡紀錄所屬員工（冗餘欄位，方便依員工查）</summary>
    public Guid      OwnerUserId        { get; set; }
    public DateTime  RecordDate         { get; set; }
    public Guid      ModifiedById       { get; set; }
    public string    ModifiedByName     { get; set; } = string.Empty;
    public DateTime  ModifiedAt         { get; set; }

    public DateTime? ClockInBefore       { get; set; }
    public DateTime? ClockInAfter        { get; set; }
    public DateTime? ClockOutBefore      { get; set; }
    public DateTime? ClockOutAfter       { get; set; }
    public DateTime? OvertimeStartBefore { get; set; }
    public DateTime? OvertimeStartAfter  { get; set; }
    public DateTime? OvertimeEndBefore   { get; set; }
    public DateTime? OvertimeEndAfter    { get; set; }
    public string?   RemarkBefore        { get; set; }
    public string?   RemarkAfter         { get; set; }

    // Navigation
    public AttendanceRecord? AttendanceRecord { get; set; }
}
