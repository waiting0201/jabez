using System.Data;
using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public sealed class AttendanceReminderLogReadService(IDbConnection db)
    : IAttendanceReminderLogReadService
{
    private const string SelectColumns = """
        SELECT l.Id, l.BatchId, l.TickedAt, l.TickedAtTaipei, l.TargetTimeTaipei,
               l.ReminderType, l.TriggerSource,
               l.TriggeredByUserId, tu.Name AS TriggeredByName,
               l.UserId, u.Name AS UserName,
               l.LineUserIdSnapshot, l.UserNameSnapshot,
               l.Status, l.ErrorCategory, l.ErrorMessage,
               l.HttpStatusCode, l.DurationMs, l.CreatedAt
        FROM   AttendanceReminderLogs l
        LEFT JOIN Users u  ON u.Id  = l.UserId
        LEFT JOIN Users tu ON tu.Id = l.TriggeredByUserId
        """;

    public async Task<PagedResult<AttendanceReminderLogDto>> GetPagedAsync(
        DateTime? fromTaipei,
        DateTime? toTaipei,
        string?   reminderType,
        string?   status,
        string?   errorCategory,
        Guid?     userId,
        string?   triggerSource,
        int       page,
        int       pageSize,
        CancellationToken ct)
    {
        var p = new DynamicParameters();
        p.Add("Skip", (page - 1) * pageSize);
        p.Add("Take", pageSize);
        p.Add("From", fromTaipei);
        p.Add("To",   toTaipei?.AddDays(1));   // 半開區間：[from, to+1day)
        p.Add("Type", reminderType);
        p.Add("Status", status);
        p.Add("ErrorCat", errorCategory);
        p.Add("UserId", userId);
        p.Add("Source", triggerSource);

        const string whereSql = """
            WHERE (@From   IS NULL OR l.TickedAtTaipei >= @From)
              AND (@To     IS NULL OR l.TickedAtTaipei <  @To)
              AND (@Type   IS NULL OR l.ReminderType   =  @Type)
              AND (@Status IS NULL OR l.Status         =  @Status)
              AND (@ErrorCat IS NULL OR l.ErrorCategory = @ErrorCat)
              AND (@UserId   IS NULL OR l.UserId        = @UserId)
              AND (@Source   IS NULL OR l.TriggerSource = @Source)
            """;

        var countSql = "SELECT COUNT(*) FROM AttendanceReminderLogs l " + whereSql;
        int total = await db.ExecuteScalarAsync<int>(new CommandDefinition(countSql, p, cancellationToken: ct));

        var listSql = SelectColumns + " " + whereSql +
                      " ORDER BY l.TickedAtTaipei DESC, l.Id DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        var rows = await db.QueryAsync<AttendanceReminderLogDto>(
            new CommandDefinition(listSql, p, cancellationToken: ct));

        int totalPages = pageSize > 0 ? (int)Math.Ceiling((double)total / pageSize) : 1;
        return new PagedResult<AttendanceReminderLogDto>(rows, total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<AttendanceReminderLogDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var sql = SelectColumns + " WHERE l.Id = @Id";
        var cmd = new CommandDefinition(sql, new { Id = id }, cancellationToken: ct);
        return await db.QueryFirstOrDefaultAsync<AttendanceReminderLogDto>(cmd);
    }

    public async Task<IReadOnlyList<AttendanceReminderLogDto>> GetByBatchIdAsync(Guid batchId, CancellationToken ct)
    {
        // batchStart 排前面（用 ReminderType 排序：'b' < 'c'），其餘依 Id 升序
        var sql = SelectColumns + """
             WHERE l.BatchId = @BatchId
             ORDER BY CASE WHEN l.ReminderType = 'batchStart' THEN 0 ELSE 1 END, l.Id
            """;
        var cmd = new CommandDefinition(sql, new { BatchId = batchId }, cancellationToken: ct);
        var rows = await db.QueryAsync<AttendanceReminderLogDto>(cmd);
        return rows.ToList();
    }

    public async Task<AttendanceReminderLogStatsDto> GetStatsAsync(DateTime todayTaipei, CancellationToken ct)
    {
        // 今日（台北日）統計
        const string todayStatsSql = """
            SELECT
                SUM(CASE WHEN Status='success'    THEN 1 ELSE 0 END) AS TodayPushed,
                SUM(CASE WHEN Status='failure'    THEN 1 ELSE 0 END) AS TodayFailed,
                SUM(CASE WHEN Status='batchStart' THEN 1 ELSE 0 END) AS TodayBatchTicks
            FROM AttendanceReminderLogs
            WHERE CAST(TickedAtTaipei AS DATE) = CAST(@Today AS DATE);
            """;
        var todayRow = await db.QueryFirstAsync<(int? p, int? f, int? b)>(
            new CommandDefinition(todayStatsSql, new { Today = todayTaipei }, cancellationToken: ct));

        // 最近 7 天每日趨勢（不含今天 + 含今天 = 7 天）
        const string trendSql = """
            SELECT CAST(TickedAtTaipei AS DATE) AS Day,
                   SUM(CASE WHEN Status='success' THEN 1 ELSE 0 END) AS Pushed,
                   SUM(CASE WHEN Status='failure' THEN 1 ELSE 0 END) AS Failed
            FROM AttendanceReminderLogs
            WHERE TickedAtTaipei >= DATEADD(day, -6, CAST(@Today AS DATE))
              AND Status IN ('success', 'failure')
            GROUP BY CAST(TickedAtTaipei AS DATE)
            ORDER BY Day;
            """;
        var trend = await db.QueryAsync<AttendanceReminderLogDailyDto>(
            new CommandDefinition(trendSql, new { Today = todayTaipei }, cancellationToken: ct));

        return new AttendanceReminderLogStatsDto(
            TodayPushed:     todayRow.p ?? 0,
            TodayFailed:     todayRow.f ?? 0,
            TodayBatchTicks: todayRow.b ?? 0,
            Last7Days:       trend.ToList());
    }
}
