using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

/// <param name="UserId">同仁</param>
/// <param name="UserName">姓名</param>
/// <param name="DepartmentName">部門（失敗時要通知該部門協理）</param>
/// <param name="Success">是否排出合法班表</param>
/// <param name="FailureReason">失敗原因</param>
/// <param name="StatutoryOffDates">排定的例假日（供預覽核對）</param>
/// <param name="RestDayDates">排定的休假日</param>
public sealed record AutoScheduleUserResult(
    Guid    UserId,
    string  UserName,
    string? DepartmentName,
    bool    Success,
    string? FailureReason,
    string[] StatutoryOffDates,
    string[] RestDayDates);

public sealed record AutoScheduleRunResult(
    int Year, int Month, bool DryRun,
    int Assigned, int Failed, int Skipped,
    AutoScheduleUserResult[] Results);

public interface IAutoShiftScheduleService
{
    /// <summary>
    /// 對「逾期未完成排班」者自動排班。
    /// </summary>
    /// <param name="dryRun">true ＝ 只算不寫（預覽），不動任何資料。</param>
    Task<AutoScheduleRunResult> RunAsync(int year, int month, bool dryRun, Guid? onlyUserId = null,
        CancellationToken ct = default);
}

/// <summary>
/// 逾期未排班者的系統自動排班（四週彈性工時 §3.5.1）。
///
/// <b>逾期定義</b>：至 25 號 23:59 為止，該員次月班表**未通過 §3.3 的配額檢核** ——
/// 完全沒排、或排到一半沒排滿，都算逾期（以 <c>ShiftScheduleMonth.Status != committed</c> 判定）。
///
/// <b>執行時點</b>：26 號的排程批次（由 <c>ShiftScheduleReminderService</c> 在推
/// 「系統已為您自動排班」通知之前呼叫），一次處理所有逾期者。
///
/// ⚠ **排不出合法班表者一律留白不寫入**（§3.5.1 第 6 點）：寧可沒有班表交人工處理，
/// 也不可產生違反勞基法的班表 —— 例假日違規的罰鍰是 2 萬～100 萬元。
/// </summary>
public sealed class AutoShiftScheduleService(
    AppDbContext db,
    ICalendarDayReadService calendarReader,
    ILogger<AutoShiftScheduleService> logger) : IAutoShiftScheduleService
{
    public async Task<AutoScheduleRunResult> RunAsync(
        int year, int month, bool dryRun, Guid? onlyUserId = null, CancellationToken ct = default)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        // 逾期者＝在職、非超管，且該月沒有 committed 的整月狀態
        var committed = db.ShiftScheduleMonths
            .Where(m => m.Year == year && m.Month == month && m.Status == ShiftScheduleMonthStatus.Committed)
            .Select(m => m.UserId);

        var query = db.Users.AsNoTracking()
            .Where(u => u.Status == "active" && !u.IsSuperAdmin && !committed.Contains(u.Id));

        if (onlyUserId is { } uid) query = query.Where(u => u.Id == uid);

        var targets = await query
            .Select(u => new
            {
                u.Id,
                u.Name,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                u.DepartmentId,
            })
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

        if (targets.Count == 0)
            return new AutoScheduleRunResult(year, month, dryRun, 0, 0, 0, []);

        var holidays = (await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd))
            .Keys.ToHashSet();

        // 活動日：以「該員所屬部門」為範圍（跨部門活動不該影響別部門的人）
        var activities = await db.ActivityDays.AsNoTracking()
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd)
            .Select(a => new { a.Date, a.DepartmentId })
            .ToListAsync(ct);
        var activityByDept = activities.ToLookup(a => a.DepartmentId, a => a.Date.Date);

        var now = Clock.Now;
        var results = new List<AutoScheduleUserResult>(targets.Count);
        int assigned = 0, failed = 0;

        foreach (var t in targets)
        {
            ct.ThrowIfCancellationRequested();

            var deptActivities = t.DepartmentId is { } dep
                ? activityByDept[dep].ToHashSet()
                : [];

            var context = await LoadContextAsync(t.Id, monthStart, monthEnd, holidays, ct);
            var outcome = AutoShiftScheduler.Build(year, month, holidays, deptActivities, context);

            results.Add(new AutoScheduleUserResult(
                t.Id, t.Name, t.DepartmentName, outcome.Success, outcome.FailureReason,
                [.. outcome.Days.Where(kv => kv.Value == WorkDayTypes.StatutoryOff)
                                .Select(kv => kv.Key.ToString("MM/dd")).Order()],
                [.. outcome.Days.Where(kv => kv.Value == WorkDayTypes.RestDay)
                                .Select(kv => kv.Key.ToString("MM/dd")).Order()]));

            if (!outcome.Success)
            {
                failed++;
                logger.LogWarning(
                    "[AutoShiftSchedule] 排不出合法班表：{Name}（{Dept}）{Year}/{Month} — {Reason}",
                    t.Name, t.DepartmentName, year, month, outcome.FailureReason);
                continue;
            }

            assigned++;
            if (dryRun) continue;

            await WriteAsync(t.Id, monthStart, monthEnd, outcome.Days, year, month, now, ct);
        }

        if (!dryRun) await db.SaveChangesAsync(ct);

        return new AutoScheduleRunResult(year, month, dryRun, assigned, failed, 0, [.. results]);
    }

    /// <summary>關卡 A／B 的跨月上下文（同 ShiftScheduleHandler 的作法）。</summary>
    private async Task<Dictionary<DateTime, string>> LoadContextAsync(
        Guid userId, DateTime monthStart, DateTime monthEnd,
        IReadOnlySet<DateTime> holidays, CancellationToken ct)
    {
        var from = monthStart.AddDays(-(ShiftScheduleValidator.RollingWindowDays - 1));
        var to   = monthEnd.AddDays(ShiftScheduleValidator.RollingWindowDays - 1);

        var saved = await db.ShiftScheduleDays.AsNoTracking()
            .Where(d => d.UserId == userId
                     && ((d.Date >= from && d.Date < monthStart) || (d.Date > monthEnd && d.Date <= to)))
            .ToDictionaryAsync(d => d.Date.Date, d => WorkDayTypes.Normalize(d.DayType), ct);

        var outerHolidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, from, to);
        foreach (var kv in outerHolidays)
        {
            if (kv.Key >= monthStart && kv.Key <= monthEnd) continue;
            saved[kv.Key] = WorkDayTypes.PublicHoliday;
        }
        return saved;
    }

    private async Task WriteAsync(
        Guid userId, DateTime monthStart, DateTime monthEnd,
        IReadOnlyDictionary<DateTime, string> days, int year, int month, DateTime now, CancellationToken ct)
    {
        var existing = await db.ShiftScheduleDays
            .Where(d => d.UserId == userId && d.Date >= monthStart && d.Date <= monthEnd)
            .ToListAsync(ct);
        db.ShiftScheduleDays.RemoveRange(existing);

        foreach (var (date, type) in days)
        {
            if (type == WorkDayTypes.Work) continue;   // 上班日是預設值，不落地
            db.ShiftScheduleDays.Add(new ShiftScheduleDay
            {
                UserId = userId, Date = date, DayType = type, CreatedAt = now, UpdatedAt = now,
            });
        }

        var monthRow = await db.ShiftScheduleMonths
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Year == year && m.Month == month, ct);

        if (monthRow is null)
        {
            monthRow = new ShiftScheduleMonth { UserId = userId, Year = year, Month = month, CreatedAt = now };
            db.ShiftScheduleMonths.Add(monthRow);
        }

        // 標記為 auto：同仁側據此顯示「系統自動排班」badge，26 號的通知也靠它挑收件人
        monthRow.Status         = ShiftScheduleMonthStatus.Auto;
        monthRow.AutoAssignedAt = now;
        monthRow.UpdatedAt      = now;
    }
}
