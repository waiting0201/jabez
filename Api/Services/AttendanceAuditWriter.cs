using Jabez.Api.Data;
using Jabez.Api.Models.Entities;

namespace Jabez.Api.Services;

/// <summary>
/// 出缺勤異動紀錄（<see cref="AttendanceAuditLog"/>）的共用寫入點（2026-10）。
/// 原本內聯於 <c>AttendanceHandler.UpdateAsync</c>，事後加班核准（ApprovalTaskHandler）也要寫同一種稽核列，
/// 抽出後兩處共用，避免欄位對應漂移。靜態、**不呼叫 SaveChanges**（交易邊界在呼叫端）。
/// 必須在把新值寫回 <paramref name="record"/> 之前呼叫（Before 欄取自 record 現值）。
/// </summary>
public static class AttendanceAuditWriter
{
    public static void Add(
        AppDbContext db,
        AttendanceRecord record,
        Guid modifiedById,
        string modifiedByName,
        DateTime modifiedAt,
        DateTime? clockInAfter,
        DateTime? clockOutAfter,
        DateTime? overtimeStartAfter,
        DateTime? overtimeEndAfter,
        string? remarkAfter)
    {
        db.AttendanceAuditLogs.Add(new AttendanceAuditLog
        {
            AttendanceRecordId  = record.Id,
            OwnerUserId         = record.UserId,
            RecordDate          = record.RecordDate,
            ModifiedById        = modifiedById,
            ModifiedByName      = modifiedByName,
            ModifiedAt          = modifiedAt,
            ClockInBefore       = record.ClockInTime,       ClockInAfter       = clockInAfter,
            ClockOutBefore      = record.ClockOutTime,      ClockOutAfter      = clockOutAfter,
            OvertimeStartBefore = record.OvertimeStartTime, OvertimeStartAfter = overtimeStartAfter,
            OvertimeEndBefore   = record.OvertimeEndTime,   OvertimeEndAfter   = overtimeEndAfter,
            RemarkBefore        = record.Remark,            RemarkAfter        = remarkAfter,
        });
    }
}
