using System.Data;
using Dapper;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public sealed class AttendanceReminderReadService(IDbConnection db)
    : IAttendanceReminderReadService
{
    public async Task<IReadOnlyList<AttendanceReminderRecipientDto>> GetRecipientsAsync(
        DateTime targetTime, string type, CancellationToken ct = default)
    {
        // 白名單保護：只接受固定字串，避免 SQL injection
        var clockColumn = type switch
        {
            "clockIn"  => "ClockInTime",
            "clockOut" => "ClockOutTime",
            _ => throw new ArgumentException("type 必須為 clockIn 或 clockOut", nameof(type))
        };

        // 請假覆蓋判斷：用「請假是否覆蓋目標時刻」而非「請假日期是否含今日」，
        // 否則小時制請假（例如下午 13:00-17:00 病假）在上午打卡提醒時會被誤排除。
        // ResignDate 與已打卡判斷仍按日期切（一日一筆 AttendanceRecord、一個 ResignDate 邊界）。
        var sql = $"""
            SELECT u.Id AS UserId, u.LineUserId, u.Name AS UserName
            FROM   Users u
            WHERE  u.LineUserId IS NOT NULL
              AND  u.LineUserId <> ''
              AND  u.IsSuperAdmin = 0
              AND  u.Status = 'active'
              -- ResignDate >= 今天 → 仍在職（離職當日 = 最後上班日，與 PayrollReadService 相同慣例）
              AND  (u.ResignDate IS NULL OR CAST(u.ResignDate AS DATE) >= CAST(@TargetTime AS DATE))
              -- 今日已打該類型卡 → 排除
              AND  NOT EXISTS (
                    SELECT 1 FROM AttendanceRecords a
                    WHERE  a.UserId = u.Id
                      AND  CAST(a.RecordDate AS DATE) = CAST(@TargetTime AS DATE)
                      AND  a.{clockColumn} IS NOT NULL
                   )
              -- 請假涵蓋目標時刻 → 排除
              AND  NOT EXISTS (
                    SELECT 1 FROM LeaveRequests lr
                    WHERE  lr.EmployeeId = u.Id
                      AND  lr.ApprovalStatus = 'approved'
                      AND  lr.StartDate <= @TargetTime
                      AND  lr.EndDate   >= @TargetTime
                   )
            """;

        var cmd = new CommandDefinition(sql, new { TargetTime = targetTime }, cancellationToken: ct);
        var rows = await db.QueryAsync<AttendanceReminderRecipientDto>(cmd);
        return rows.ToList();
    }
}
