using System.Data;
using Dapper;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public sealed class AttendanceReminderReadService(IDbConnection db)
    : IAttendanceReminderReadService
{
    public async Task<IReadOnlyList<AttendanceReminderRecipientDto>> GetRecipientsAsync(
        DateTime today, string type, CancellationToken ct = default)
    {
        // 白名單保護：只接受固定字串，避免 SQL injection
        var clockColumn = type switch
        {
            "clockIn"  => "ClockInTime",
            "clockOut" => "ClockOutTime",
            _ => throw new ArgumentException("type 必須為 clockIn 或 clockOut", nameof(type))
        };

        var sql = $"""
            SELECT u.Id AS UserId, u.LineUserId, u.Name AS UserName
            FROM   Users u
            WHERE  u.LineUserId IS NOT NULL
              AND  u.LineUserId <> ''
              AND  u.IsSuperAdmin = 0
              AND  u.Status = 'active'
              AND  (u.ResignDate IS NULL OR CAST(u.ResignDate AS DATE) > @Today)
              -- 今日已打該類型卡 → 排除
              AND  NOT EXISTS (
                    SELECT 1 FROM AttendanceRecords a
                    WHERE  a.UserId = u.Id
                      AND  CAST(a.RecordDate AS DATE) = @Today
                      AND  a.{clockColumn} IS NOT NULL
                   )
              -- 今日在 approved 請假範圍內 → 排除
              AND  NOT EXISTS (
                    SELECT 1 FROM LeaveRequests lr
                    WHERE  lr.EmployeeId = u.Id
                      AND  lr.ApprovalStatus = 'approved'
                      AND  CAST(lr.StartDate AS DATE) <= @Today
                      AND  CAST(lr.EndDate   AS DATE) >= @Today
                   )
            """;

        var cmd = new CommandDefinition(sql, new { Today = today.Date }, cancellationToken: ct);
        var rows = await db.QueryAsync<AttendanceReminderRecipientDto>(cmd);
        return rows.ToList();
    }
}
