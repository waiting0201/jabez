using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 活動日覆蓋個人排班（2026-09-29 決議：活動日優先）。static、**不呼叫 SaveChanges**，
/// 交易邊界交給呼叫端（比照 LeaveRevocationService）。
///
/// 起因：原設計是「活動日不改寫個人班表，請同仁自提改班申請」，但活動日預定人力的格子在月曆與改班表單
/// 都被鎖成唯讀、同時又擋存「活動日不可排例假／休假」—— 同仁兩邊都改不了，形成死結。
///
/// 做法：主管存活動日時，對每位當天排了例假／休假的預定人力 ——
/// <list type="number">
///   <item>把該日改為上班日（上班日不落地，故為刪除該列）</item>
///   <item>以 <see cref="ActivityOverrideRelocator"/> 把少掉的例假／休假搬到當月另一個合法日子</item>
///   <item>落一筆 <see cref="ShiftScheduleAdjustment"/>，作為鈴鐺通知與個人排班頁提示卡的來源</item>
/// </list>
///
/// 刻意不做的事：
/// <list type="bullet">
///   <item><b>不動 <c>ShiftScheduleMonth.Status</c></b> —— 改成 draft 會誤觸 20 號未完成提醒，
///         甚至被 26 號自動排班整月覆寫</item>
///   <item><b>活動日改期／移除預定人力時不還原</b>先前被改成上班日的日子 —— 反覆搬動只會讓同仁更混亂，
///         要改回來由同仁自己在開放期內調整、或走〈改班申請〉</item>
///   <item>過去日與今天、國定假日上的活動日不處理（前者是歷史；後者國定假日不入表，本來就沒有衝突）</item>
/// </list>
/// </summary>
public static class ActivityScheduleOverrideService
{
    public static async Task<List<ActivityScheduleAdjustedDto>> ApplyAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, IEmployeeWorkdaysFactory workdaysFactory,
        ActivityDay activity, IReadOnlyCollection<Guid> assigneeIds)
    {
        var result = new List<ActivityScheduleAdjustedDto>();
        var date   = activity.Date.Date;
        var today  = Clock.Now.Date;

        if (assigneeIds.Count == 0 || date <= today) return result;

        var monthStart = new DateTime(date.Year, date.Month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);
        var holidays   = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);
        if (holidays.ContainsKey(date)) return result;

        var ids = assigneeIds.Distinct().ToList();
        var conflicts = await db.ShiftScheduleDays
            .Where(d => ids.Contains(d.UserId) && d.Date == date
                     && (d.DayType == WorkDayTypes.StatutoryOff || d.DayType == WorkDayTypes.RestDay))
            .ToListAsync();
        if (conflicts.Count == 0) return result;

        var conflictUserIds = conflicts.Select(c => c.UserId).ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => conflictUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var now = Clock.Now;
        foreach (var row in conflicts)
        {
            var uid          = row.UserId;
            var originalType = WorkDayTypes.Normalize(row.DayType);

            var saved = await db.ShiftScheduleDays.AsNoTracking()
                .Where(d => d.UserId == uid && d.Date >= monthStart && d.Date <= monthEnd)
                .ToDictionaryAsync(d => d.Date.Date, d => d.DayType);
            var monthDays = ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, saved, holidays);

            // 不可拿來補排的日子：今天以前、國定假日、本人的活動日與請假日、被進行中改班單佔用的日子
            var locks    = await ShiftScheduleConstraintService.LoadLockedDatesAsync(
                db, workdaysFactory, uid, monthStart, monthEnd);
            var occupied = await ShiftChangeRequestService.OccupiedDatesAsync(
                db, uid, date.Year, date.Month, excludeRequestId: null);
            var blocked = monthDays.Keys
                .Where(d => d <= today || holidays.ContainsKey(d) || locks.ContainsKey(d) || occupied.Contains(d))
                .ToHashSet();

            var context  = await ShiftScheduleConstraintService.LoadContextDaysAsync(
                db, calendarReader, uid, monthStart, monthEnd);
            var outcome  = ActivityOverrideRelocator.Relocate(
                date.Year, date.Month, monthDays, date, blocked, context);

            // 衝突日 → 上班日（上班日不落地）
            db.ShiftScheduleDays.Remove(row);

            if (outcome.RelocatedTo is { } to)
            {
                db.ShiftScheduleDays.Add(new ShiftScheduleDay
                {
                    UserId    = uid,
                    Date      = to,
                    DayType   = originalType,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            db.ShiftScheduleAdjustments.Add(new ShiftScheduleAdjustment
            {
                UserId          = uid,
                ActivityDayId   = activity.Id,
                ActivityTitle   = activity.Title,
                Date            = date,
                OriginalDayType = originalType,
                RelocatedTo     = outcome.RelocatedTo,
                CreatedAt       = now,
            });

            result.Add(new ActivityScheduleAdjustedDto(
                uid, names.TryGetValue(uid, out var n) ? n : "(已離職)",
                date, originalType, outcome.RelocatedTo));
        }

        return result.OrderBy(r => r.UserName).ToList();
    }
}
