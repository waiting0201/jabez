using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 改班申請共用（static，**不呼叫 SaveChanges** —— 交易邊界交給呼叫端，比照 LeaveRevocationService）。
/// </summary>
public static class ShiftChangeRequestService
{
    /// <summary>
    /// 簽核任務 / 簽核紀錄使用的 applicationType。
    ///
    /// ⚠ **與銷假申請的關鍵差異**：銷假是 <c>ResolveApprovalItemIdAsync("leave", …)</c> 借用請假的流程設定、
    /// 自己沒有 ApprovalItem；改班要的是 §3.5.2 的**逐部門六條簽核路線**，
    /// 管理員必須在〈簽核流程設定〉建得出 6 個 ApprovalItem，所以它必須有自己的 ApplicationType。
    /// </summary>
    public const string AppType = "shift_change";

    /// <summary>申請單號前綴（LV- / OT- / LVR- 已被佔用）。</summary>
    public const string RequestNoPrefix = "SC-";

    /// <summary>「進行中」的改班單狀態：同一人同一月份同時只能有一張（2026-09-28 客戶要求）。</summary>
    public static readonly string[] InFlightStatuses = ["draft", "pending", "returned"];

    /// <summary>被其他進行中改班單佔用的日期（月曆上的 LockReason）。</summary>
    public const string LockPendingChange = "change";

    /// <summary>
    /// 該員該月被**進行中**改班單佔用的日期（<paramref name="excludeRequestId"/> 那張除外）。
    /// 月曆鎖定、改班表單可選日期、活動日覆蓋補排三處共用。
    /// </summary>
    public static async Task<HashSet<DateTime>> OccupiedDatesAsync(
        AppDbContext db, Guid userId, int year, int month, int? excludeRequestId) =>
        (await db.ShiftChangeRequestDates.AsNoTracking()
            .Where(d => d.ShiftChangeRequest!.EmployeeId == userId
                     && d.ShiftChangeRequest.Year == year && d.ShiftChangeRequest.Month == month
                     && InFlightStatuses.Contains(d.ShiftChangeRequest.ApprovalStatus)
                     && (excludeRequestId == null || d.ShiftChangeRequestId != excludeRequestId))
            .Select(d => d.Date)
            .ToListAsync())
        .Select(d => d.Date).ToHashSet();

    /// <summary>
    /// 改班「套用後」的整月檢視 —— 表單即時試算、申請詳情、簽核頁三處共用，
    /// 申請人送出前看到的檢核結果與審核者看到的必然一致。
    ///
    /// 基底為**現行班表**（非送單當下快照）：簽核期間若班表被別的途徑改動，審核者看到的是
    /// 「此刻核准會變成什麼樣子」，終局核准前的重驗也用同一份（見 <see cref="EnsureValidAsync"/>）。
    /// </summary>
    /// <param name="changes">本次調整；<c>FromDayType</c> 為快照（現行班表已等於目標值時用來顯示「原本是什麼」）。</param>
    /// <param name="excludeRequestId">自己這張單的 Id（其日期不算「被其他改班單佔用」）。</param>
    public static async Task<ShiftChangeMonthViewDto> BuildMonthViewAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, IEmployeeWorkdaysFactory workdaysFactory,
        Guid userId, int year, int month,
        IReadOnlyCollection<ShiftChangeDateDto> changes, int? excludeRequestId)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);
        var today      = Clock.Now.Date;

        var holidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);
        var locks    = await ShiftScheduleConstraintService.LoadLockedDatesAsync(
            db, workdaysFactory, userId, monthStart, monthEnd);

        var saved = await db.ShiftScheduleDays.AsNoTracking()
            .Where(d => d.UserId == userId && d.Date >= monthStart && d.Date <= monthEnd)
            .ToDictionaryAsync(d => d.Date.Date, d => d.DayType);
        var baseMap = ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, saved, holidays);

        var occupied = await OccupiedDatesAsync(db, userId, year, month, excludeRequestId);

        var activities = await db.ActivityDays.AsNoTracking()
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd)
            .Select(a => new { a.Date, a.Title, IsAssignee = a.Assignees.Any(x => x.UserId == userId) })
            .ToListAsync();
        var activityByDate = activities.ToLookup(a => a.Date.Date);

        var resultMap = new Dictionary<DateTime, string>(baseMap);
        var changeList = new List<ShiftChangeDateDto>();
        foreach (var c in changes.OrderBy(c => c.Date))
        {
            var d = c.Date.Date;
            if (!resultMap.ContainsKey(d) || holidays.ContainsKey(d)) continue;
            var to = WorkDayTypes.Normalize(c.ToDayType);
            resultMap[d] = to;
            var from = baseMap[d] != to ? baseMap[d] : WorkDayTypes.Normalize(c.FromDayType);
            changeList.Add(new ShiftChangeDateDto(d, from, to));
        }

        var days = new List<ShiftScheduleDayDto>();
        for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
        {
            var isHoliday = holidays.TryGetValue(d, out var holidayName);
            var act       = activityByDate[d].OrderByDescending(a => a.IsAssignee).FirstOrDefault();
            var dayLock   = locks.GetValueOrDefault(d);
            var isPast    = d < today && !ShiftScheduleWindow.OpenAllFutureMonths;
            var lockReason = dayLock?.Reason ?? (occupied.Contains(d) ? LockPendingChange : null);

            days.Add(new ShiftScheduleDayDto(
                Date:               d,
                DayType:            resultMap[d],
                HolidayName:        isHoliday ? holidayName : null,
                // 活動日／請假鎖定日卻排著例假／休假時放行點選（前端只允許改成上班日），避免死結；
                // 被其他改班單佔用（LockPendingChange）則一律唯讀
                ReadOnly:           isHoliday || isPast
                                 || (lockReason is not null
                                     && !(dayLock is not null && WorkDayTypes.QuotaBearing.Contains(baseMap[d]))),
                IsActivityDay:      act is not null,
                ActivityTitle:      act?.Title,
                IsActivityAssignee: act?.IsAssignee ?? false,
                LockReason:         lockReason,
                LeaveLabel:         dayLock?.Reason == ShiftScheduleConstraintService.LockLeave ? dayLock.Label : null));
        }

        var result = await ShiftScheduleConstraintService.EvaluateAsync(
            db, calendarReader, workdaysFactory, userId, year, month, resultMap, locks);

        return new ShiftChangeMonthViewDto(
            year, month, [.. days], [.. changeList], ShiftScheduleConstraintService.ToDto(result));
    }

    /// <summary>某張改班單的整月檢視（以其逐日明細為本次調整）。</summary>
    public static async Task<ShiftChangeMonthViewDto?> BuildMonthViewAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, IEmployeeWorkdaysFactory workdaysFactory,
        ShiftChangeRequest request)
    {
        if (request.EmployeeId is not { } userId) return null;
        var changes = await db.ShiftChangeRequestDates.AsNoTracking()
            .Where(d => d.ShiftChangeRequestId == request.Id)
            .Select(d => new ShiftChangeDateDto(d.Date, d.FromDayType, d.ToDayType, false))
            .ToListAsync();
        return await BuildMonthViewAsync(db, calendarReader, workdaysFactory, userId, request.Year, request.Month, changes, request.Id);
    }

    /// <summary>
    /// 送簽與每一關核准前的硬性檢核：套用後的整月班表必須符合排班規範（與個人排班同一真相），
    /// 不合格回 400。審核者因此只需就改班原因與部門人力安排判斷，不必自行核對是否觸法。
    /// </summary>
    public static async Task EnsureValidAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, IEmployeeWorkdaysFactory workdaysFactory,
        ShiftChangeRequest request, string prefix)
    {
        var view = await BuildMonthViewAsync(db, calendarReader, workdaysFactory, request);
        if (view is null || view.Validation.CanSave) return;
        throw AppException.BadRequest(prefix + string.Join(" ", view.Validation.Blocks));
    }

    /// <summary>
    /// 核准後把異動寫進班表。**只有終局核准才呼叫** —— 未核准前原班表完全不動，
    /// 故退回 / 拒絕都不需要任何回滾。
    ///
    /// 寫入語意與 ShiftScheduleHandler 一致：上班日不落地（查無紀錄即上班日），
    /// 故 <c>ToDayType = work</c> 時是**刪除**該日的排班列。
    /// </summary>
    public static async Task ApplyAsync(AppDbContext db, ShiftChangeRequest request)
    {
        var dates = await db.ShiftChangeRequestDates.AsNoTracking()
            .Where(d => d.ShiftChangeRequestId == request.Id)
            .ToListAsync();

        if (dates.Count == 0 || request.EmployeeId is not { } userId) return;

        var targetDates = dates.Select(d => d.Date.Date).ToList();
        var existing = await db.ShiftScheduleDays
            .Where(d => d.UserId == userId && targetDates.Contains(d.Date))
            .ToListAsync();

        var now = Clock.Now;

        foreach (var d in dates)
        {
            var row = existing.FirstOrDefault(x => x.Date.Date == d.Date.Date);
            var to  = WorkDayTypes.Normalize(d.ToDayType);

            if (to == WorkDayTypes.Work)
            {
                // 改回上班日 ＝ 刪除該列（與「查無紀錄即上班日」的既定語意一致）
                if (row is not null) db.ShiftScheduleDays.Remove(row);
                continue;
            }

            if (row is null)
            {
                db.ShiftScheduleDays.Add(new ShiftScheduleDay
                {
                    UserId = userId, Date = d.Date.Date, DayType = to,
                    CreatedAt = now, UpdatedAt = now,
                });
            }
            else
            {
                row.DayType   = to;
                row.UpdatedAt = now;
            }
        }
    }
}
