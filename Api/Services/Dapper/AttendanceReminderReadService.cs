using System.Data;
using Dapper;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public sealed class AttendanceReminderReadService(IDbConnection db)
    : IAttendanceReminderReadService
{
    public async Task<IReadOnlyList<AttendanceReminderRecipientDto>> GetRecipientsAsync(
        DateTime targetTime, string type, bool shiftWorkersOnly = false, CancellationToken ct = default)
    {
        // 白名單保護：只接受固定字串，避免 SQL injection
        var clockColumn = type switch
        {
            "clockIn"  => "ClockInTime",
            "clockOut" => "ClockOutTime",
            _ => throw new ArgumentException("type 必須為 clockIn 或 clockOut", nameof(type))
        };

        // 六日只提醒排班制員工（賣店 / 營業所照常營業）
        var shiftWorkerFilter = shiftWorkersOnly ? "AND u.IsShiftWorker = 1" : "";

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
              {shiftWorkerFilter}
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
                      -- 該日已核准銷假 → 恢復推播提醒
                      AND  NOT EXISTS (
                            SELECT 1 FROM LeaveRevocationDates rvd
                            JOIN LeaveRevocations rv ON rv.Id = rvd.LeaveRevocationId
                            WHERE rv.LeaveRequestId = lr.Id
                              AND rv.ApprovalStatus = 'approved'
                              AND rvd.Date = CAST(@TargetTime AS DATE))
                   )
            """;

        var cmd = new CommandDefinition(sql, new { TargetTime = targetTime }, cancellationToken: ct);
        var rows = await db.QueryAsync<AttendanceReminderRecipientDto>(cmd);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AttendanceReminderClockOutCandidateDto>> GetClockOutCandidatesAsync(
        DateTime today, CancellationToken ct = default)
    {
        // 與舊制收件人 SQL 的關鍵差別：**把 ClockInTime 的值撈出來**。
        // 新制的下班提醒時點是「實際上班打卡 ＋ 9 小時 − 2 分」，沒有這個值就算不出來。
        //
        // AfternoonOnly：當日有一段已核准假在半天分界（13:00）前結束 → 視為請了上午半天假，
        // 應下班時間改為 ＋4 小時（午休已過，不再扣那 1 小時）。
        const string sql = @"
            SELECT u.Id AS UserId, u.LineUserId, u.Name AS UserName, a.ClockInTime,
                   CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM LeaveRequests lr
                        WHERE lr.EmployeeId = u.Id
                          AND lr.ApprovalStatus = 'approved'
                          AND CAST(lr.StartDate AS DATE) <= @Today
                          AND lr.EndDate > @Today AND lr.EndDate <= @Boundary
                   ) THEN 1 ELSE 0 END AS bit) AS AfternoonOnly
            FROM   Users u
            JOIN   AttendanceRecords a
                   ON a.UserId = u.Id AND CAST(a.RecordDate AS DATE) = @Today
            WHERE  u.LineUserId IS NOT NULL
              AND  u.LineUserId <> ''
              AND  u.IsSuperAdmin = 0
              AND  u.Status = 'active'
              AND  (u.ResignDate IS NULL OR CAST(u.ResignDate AS DATE) >= @Today)
              AND  a.ClockInTime  IS NOT NULL
              AND  a.ClockOutTime IS NULL";

        var cmd = new CommandDefinition(
            sql,
            new { Today = today.Date, Boundary = today.Date.AddHours(13) },
            cancellationToken: ct);
        var rows = await db.QueryAsync<AttendanceReminderClockOutCandidateDto>(cmd);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetAlreadyPushedUserIdsAsync(
        DateTime today, string reminderType, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT DISTINCT UserId
            FROM   AttendanceReminderLogs
            WHERE  UserId IS NOT NULL
              AND  Status = 'success'
              AND  ReminderType = @Type
              -- 刻意不用 CAST(TickedAtTaipei AS DATE) = @Today：CAST 會讓既有索引
              -- IX_AttendanceReminderLogs_TickedAtTaipei_Status_Type 無法 seek。
              -- 這支查詢在提醒時段是每分鐘一次，紀錄表又只增不減，寫成半開區間才不會隨時間變慢。
              AND  TickedAtTaipei >= @Today
              AND  TickedAtTaipei <  @Tomorrow";

        var cmd = new CommandDefinition(
            sql,
            new { Today = today.Date, Tomorrow = today.Date.AddDays(1), Type = reminderType },
            cancellationToken: ct);
        var rows = await db.QueryAsync<Guid>(cmd);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AttendanceReminderRecipientDto>> GetHalfDayLeaveRecipientsAsync(
        DateTime today, string segment, DateTime boundary, CancellationToken ct = default)
    {
        // am ＝ 假在分界前結束（下午才上班）；pm ＝ 假從分界起算（下午開始休）
        var segmentClause = segment == "am"
            ? "lr.EndDate > @Today AND lr.EndDate <= @Boundary"
            : "lr.StartDate >= @Boundary AND lr.StartDate < DATEADD(day, 1, @Today)";

        var sql = $@"
            SELECT u.Id AS UserId, u.LineUserId, u.Name AS UserName
            FROM   Users u
            WHERE  u.LineUserId IS NOT NULL
              AND  u.LineUserId <> ''
              AND  u.IsSuperAdmin = 0
              AND  u.Status = 'active'
              AND  (u.ResignDate IS NULL OR CAST(u.ResignDate AS DATE) >= @Today)
              AND  EXISTS (
                    SELECT 1 FROM LeaveRequests lr
                    WHERE  lr.EmployeeId = u.Id
                      AND  lr.ApprovalStatus = 'approved'
                      AND  CAST(lr.StartDate AS DATE) <= @Today
                      AND  {segmentClause}
                      AND  NOT EXISTS (
                            SELECT 1 FROM LeaveRevocationDates rvd
                            JOIN LeaveRevocations rv ON rv.Id = rvd.LeaveRevocationId
                            WHERE rv.LeaveRequestId = lr.Id
                              AND rv.ApprovalStatus = 'approved'
                              AND rvd.Date = @Today)
                   )";

        var cmd = new CommandDefinition(sql, new { Today = today.Date, Boundary = boundary }, cancellationToken: ct);
        var rows = await db.QueryAsync<AttendanceReminderRecipientDto>(cmd);
        return rows.ToList();
    }
}
