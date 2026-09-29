using System.IdentityModel.Tokens.Jwt;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

/// <summary>
/// 個人排班排例／休（四週彈性工時功能 A）。
///
/// GET  /shift-schedules?year=&amp;month=[&amp;userId=]  → 某人某月的月曆（含檢核結果）
/// PUT  /shift-schedules                            → 整月整批替換
/// GET  /shift-schedules/adjustments                → 本人未確認的「活動日覆蓋班表」通知
/// POST /shift-schedules/adjustments/ack            → 全部標為已讀（「我知道了」）
///
/// 設計重點：
/// <list type="bullet">
///   <item><b>整月整批替換</b>（比照人事資料卡的 9 組子表）：一次送整個月，Handler 先清後寫，
///         避免逐格 PATCH 造成「只改了一半」的不合法班表。</item>
///   <item><b>國定假日不入表</b>：唯讀、不佔配額，一律由 CalendarDay 解析。送進來的一律丟棄。</item>
///   <item><b>擋存判準收斂在 <see cref="ShiftScheduleValidator"/></b>，讀（GET）與寫（PUT）共用同一份，
///         前端只是把結果顯示出來，不自行重算。</item>
/// </list>
///
/// 看別人的班表：未持 <c>shift-schedule:view-all</c> 者僅限 <c>ProjectAccessScope</c> 涵蓋的部門。
/// ⚠ 已知取捨：六個部門的 <c>CanSeeAll = 1</c>，故那些部門的一般同仁也看得到全公司排班 ——
/// 這是與需求方確認後接受的現狀（排班不視為敏感資料），見 flexible-work-hours.md §3.1。
/// </summary>
public sealed class ShiftScheduleHandler(
    AppDbContext db,
    IJwtService jwtService,
    IProjectAccessResolver access,
    ICalendarDayReadService calendarReader,
    IEmployeeWorkdaysFactory workdaysFactory)
{
    public async Task<IActionResult> GetMonthAsync(HttpRequest req)
    {
        var callerId = await GetUserIdAsync(req);
        var (year, month) = ParseYearMonth(req);

        var targetId = Guid.TryParse(req.Query["userId"], out var q) ? q : callerId;
        if (targetId != callerId) await EnsureCanViewOthersAsync(req, targetId);

        var dto = await BuildMonthDtoAsync(targetId, year, month);
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    public async Task<IActionResult> SaveMonthAsync(HttpRequest req)
    {
        var callerId = await GetUserIdAsync(req);

        var body = await req.ReadFromJsonAsync<SaveShiftScheduleRequest>()
                   ?? throw AppException.BadRequest("Invalid request body.");

        var targetId = body.UserId ?? callerId;
        // 排班一律只能排自己的：代排會讓「誰排的」失去意義，且改班申請的簽核對象也會錯亂。
        if (targetId != callerId)
            throw AppException.Forbidden("僅能排定自己的班表。");

        ValidateYearMonth(body.Year, body.Month);

        var now         = Clock.Now;
        var editability = ShiftScheduleWindow.Evaluate(
            body.Year, body.Month, now, await ResolveGraceDeadlineAsync(targetId, body.Year, body.Month));

        if (!editability.CanEdit)
            throw AppException.BadRequest(editability.Reason);

        var monthStart = new DateTime(body.Year, body.Month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        // 國定假日：唯讀格，送進來一律丟棄（判準與月曆讀取、配額重算共用 PublicHolidayRule）
        var publicHolidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);

        var incoming = new Dictionary<DateTime, string>();
        foreach (var d in body.Days)
        {
            var date = d.Date.Date;
            if (date < monthStart || date > monthEnd) continue;      // 超出當月 → 丟棄
            if (publicHolidays.ContainsKey(date))     continue;      // 國定假日 → 丟棄
            if (!WorkDayTypes.IsSelectable(d.DayType))
                throw AppException.BadRequest($"{date:M/d} 的日別「{d.DayType}」不是可勾選的狀態。");

            incoming[date] = d.DayType;
        }

        // 當日臨時調休：只有「今天」那一格能動，其餘必須與現況相同
        if (editability.Mode == ShiftScheduleEditMode.SameDayOnly)
            await EnsureOnlyTodayChangedAsync(targetId, monthStart, monthEnd, incoming, now, editability);

        // 檢核（與 GET、改班申請共用同一份判準，含活動日／請假鎖定）
        var monthDays = ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, incoming, publicHolidays);
        var result    = await ShiftScheduleConstraintService.EvaluateAsync(
            db, calendarReader, workdaysFactory, targetId, body.Year, body.Month, monthDays);

        if (!result.CanSave)
            throw AppException.BadRequest(string.Join(" ", result.Blocks));

        await ReplaceMonthAsync(targetId, monthStart, monthEnd, incoming, now);
        await UpsertMonthStatusAsync(targetId, body.Year, body.Month, now);
        await db.SaveChangesAsync();

        var dto = await BuildMonthDtoAsync(targetId, body.Year, body.Month);
        return new OkObjectResult(ApiResponse.Ok(dto, "排班已儲存。"));
    }

    /// <summary>
    /// 本人未確認的「活動日覆蓋班表」通知（鈴鐺 + 個人排班頁提示卡）。對象一律取 JWT sub。
    /// </summary>
    public async Task<IActionResult> GetMyAdjustmentsAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);

        var rows = await db.ShiftScheduleAdjustments.AsNoTracking()
            .Where(a => a.UserId == userId && a.AcknowledgedAt == null)
            .OrderBy(a => a.Date).ThenBy(a => a.Id)
            .Select(a => new ShiftScheduleAdjustmentDto(
                a.Id, a.Date, a.ActivityTitle, a.OriginalDayType, a.RelocatedTo, a.CreatedAt))
            .ToListAsync();

        return new OkObjectResult(ApiResponse.Ok(rows));
    }

    /// <summary>把本人所有未確認的通知標為已讀。</summary>
    public async Task<IActionResult> AcknowledgeAdjustmentsAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var now    = Clock.Now;

        var count = await db.ShiftScheduleAdjustments
            .Where(a => a.UserId == userId && a.AcknowledgedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.AcknowledgedAt, (DateTime?)now));

        return new OkObjectResult(ApiResponse.Ok(new { count }));
    }

    // ── 組裝 ────────────────────────────────────────────────────────

    private async Task<ShiftScheduleMonthDto> BuildMonthDtoAsync(Guid userId, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);
        var now        = Clock.Now;

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Name })
            .FirstOrDefaultAsync()
            ?? throw AppException.NotFound("查無此使用者。");

        var publicHolidays   = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);
        var locks            = await ShiftScheduleConstraintService.LoadLockedDatesAsync(
            db, workdaysFactory, userId, monthStart, monthEnd);

        var saved = await db.ShiftScheduleDays.AsNoTracking()
            .Where(d => d.UserId == userId && d.Date >= monthStart && d.Date <= monthEnd)
            .ToDictionaryAsync(d => d.Date.Date, d => d.DayType);

        var monthStatus = await db.ShiftScheduleMonths.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Year == year && m.Month == month);

        var activities = await db.ActivityDays.AsNoTracking()
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd)
            .Select(a => new
            {
                a.Id,
                a.Date,
                a.Title,
                IsAssignee = a.Assignees.Any(x => x.UserId == userId),
            })
            .ToListAsync();
        var activityByDate = activities.ToLookup(a => a.Date.Date);

        var editability = ShiftScheduleWindow.Evaluate(
            year, month, now, await ResolveGraceDeadlineAsync(userId, year, month));

        var dayTypes = ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, saved, publicHolidays);

        var days = new List<ShiftScheduleDayDto>();
        for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
        {
            var isPublicHoliday = publicHolidays.TryGetValue(d, out var holidayName);
            // 同一天多個活動時，優先取本人被指派的那一筆（否則會把本人的指派蓋掉）
            var act             = activityByDate[d].OrderByDescending(a => a.IsAssignee).FirstOrDefault();
            var dayLock         = locks.GetValueOrDefault(d);

            days.Add(new ShiftScheduleDayDto(
                Date:               d,
                DayType:            dayTypes[d],
                HolidayName:        isPublicHoliday ? holidayName : null,
                // 國定假日、活動日預定人力、已請假日恆唯讀；其餘依「此刻這一格能不能改」。
                // 例外：鎖定日卻排著例假／休假（歷史資料或覆蓋前的衝突）—— 放行點選，前端只允許改成上班日，
                // 否則格子點不動、LockBlocks 又擋存，同仁兩邊都改不了（2026-09-29 死結）
                ReadOnly:           isPublicHoliday
                                 || (dayLock is not null && !WorkDayTypes.QuotaBearing.Contains(dayTypes[d]))
                                 || !ShiftScheduleWindow.CanEditDate(editability, d, now),
                IsActivityDay:      act is not null,
                ActivityTitle:      act?.Title,
                IsActivityAssignee: act?.IsAssignee ?? false,
                LockReason:         dayLock?.Reason,
                LeaveLabel:         dayLock?.Reason == ShiftScheduleConstraintService.LockLeave ? dayLock.Label : null));
        }

        var result = await ShiftScheduleConstraintService.EvaluateAsync(
            db, calendarReader, workdaysFactory, userId, year, month, dayTypes, locks);

        return new ShiftScheduleMonthDto(
            UserId:         user.Id,
            UserName:       user.Name,
            Year:           year,
            Month:          month,
            Editable:       editability.CanEdit,
            EditMode:       ToEditModeString(editability.Mode),
            EditReason:     editability.Reason,
            Status:         monthStatus?.Status,
            CommittedAt:    monthStatus?.CommittedAt,
            AutoAssignedAt: monthStatus?.AutoAssignedAt,
            Days:           [.. days],
            Validation:     ShiftScheduleConstraintService.ToDto(result));
    }

    // ── 寫入 ────────────────────────────────────────────────────────

    /// <summary>整月整批替換：先刪後寫（國定假日不入表，上班日亦不入表以免無謂膨脹）。</summary>
    private async Task ReplaceMonthAsync(
        Guid userId, DateTime monthStart, DateTime monthEnd,
        Dictionary<DateTime, string> incoming, DateTime now)
    {
        var existing = await db.ShiftScheduleDays
            .Where(d => d.UserId == userId && d.Date >= monthStart && d.Date <= monthEnd)
            .ToListAsync();
        db.ShiftScheduleDays.RemoveRange(existing);

        foreach (var (date, type) in incoming)
        {
            // 上班日是預設值，不落地 —— 少寫 ~20 列/人/月，且「查無紀錄即上班日」與解析器的退回順序一致
            if (type == WorkDayTypes.Work) continue;

            db.ShiftScheduleDays.Add(new ShiftScheduleDay
            {
                UserId    = userId,
                Date      = date,
                DayType   = type,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }

    private async Task UpsertMonthStatusAsync(Guid userId, int year, int month, DateTime now)
    {
        var entity = await db.ShiftScheduleMonths
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Year == year && m.Month == month);

        if (entity is null)
        {
            entity = new ShiftScheduleMonth
            {
                UserId = userId, Year = year, Month = month, CreatedAt = now,
            };
            db.ShiftScheduleMonths.Add(entity);
        }

        // 通過擋存判準才寫得到這裡，故一律 committed；
        // 本人重排會把 26 號自動排班留下的 auto 覆蓋掉，AutoAssignedAt 保留供追溯。
        entity.Status      = ShiftScheduleMonthStatus.Committed;
        entity.CommittedAt = now;
        entity.UpdatedAt   = now;
    }

    // ── 閘門 ────────────────────────────────────────────────────────

    /// <summary>當日臨時調休：除了「今天」以外的格子都必須與現況相同。</summary>
    private async Task EnsureOnlyTodayChangedAsync(
        Guid userId, DateTime monthStart, DateTime monthEnd,
        Dictionary<DateTime, string> incoming, DateTime now, ShiftScheduleEditability editability)
    {
        if (!ShiftScheduleWindow.CanEditDate(editability, now.Date, now))
            throw AppException.BadRequest(
                $"當日調整已於 {ShiftScheduleWindow.SameDayCutoff:HH\\:mm} 截止，異動請提出〈改班申請〉。");

        var saved = await db.ShiftScheduleDays.AsNoTracking()
            .Where(d => d.UserId == userId && d.Date >= monthStart && d.Date <= monthEnd)
            .ToDictionaryAsync(d => d.Date.Date, d => WorkDayTypes.Normalize(d.DayType));

        for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
        {
            if (d == now.Date) continue;

            var before = saved.TryGetValue(d, out var s) ? s : WorkDayTypes.Work;
            var after  = incoming.TryGetValue(d, out var i) ? i : WorkDayTypes.Work;
            if (before != after)
                throw AppException.BadRequest(
                    $"當月班表已定案，僅可調整今日（{now:M/d}）的狀態；{d:M/d} 的異動請提出〈改班申請〉。");
        }
    }

    /// <summary>
    /// 當月到職者的寬限期截止時刻 ＝ 帳號通知寄出後的第 3 個**工作天**結束。
    /// 工作天以公司行事曆判定（排除國定假日與六日），故日曆跨度可能超過 3 天。
    /// 非當月到職 / 未寄出通知 → null（無寬限）。
    /// </summary>
    private async Task<DateTime?> ResolveGraceDeadlineAsync(Guid userId, int year, int month)
    {
        var target = new DateTime(year, month, 1);

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.HireDate, u.CredentialsSentAt, u.IsShiftWorker })
            .FirstOrDefaultAsync();

        if (user?.CredentialsSentAt is not { } sentAt) return null;
        if (user.HireDate is not { } hire) return null;

        // 只有「到職月 ＝ 目標月」才有寬限
        if (hire.Year != target.Year || hire.Month != target.Month) return null;

        var from = sentAt.Date;
        var to   = from.AddDays(30);   // 上界：3 個工作天不可能超過 30 天

        // ⚠ 工作天刻意用 CalendarDay（全公司一份）而非個人班表 —— 新人當月本來就沒有班表
        // Attendance 語意：彈性休假日（原「補假」）不用上班，不該計入寬限的 3 個工作天
        var (_, _, working) = await WorkCalendarHelper.ComputeWorkingDatesAsync(
            calendarReader, ignoreHolidays: false, from, to, CalendarScope.Attendance);

        var counted = working.Where(d => d >= from).Take(ShiftScheduleWindow.GraceWorkingDays).ToList();
        if (counted.Count < ShiftScheduleWindow.GraceWorkingDays) return null;

        return counted[^1].Date.AddDays(1).AddTicks(-1);   // 第 3 個工作天的 23:59:59.999…
    }

    private async Task EnsureCanViewOthersAsync(HttpRequest req, Guid targetId)
    {
        var principal = await jwtService.ValidateRequestAsync(req)
                        ?? throw AppException.Unauthorized("Invalid token.");

        if (principal.FindFirst("is_superadmin")?.Value == "true") return;
        if (principal.FindAll("permissions").Any(c => c.Value == PermissionCodes.ShiftScheduleViewAll)) return;

        var scope = await access.ResolveAsync(principal);
        if (scope.SeeAll) return;

        var deptId = await db.Users.AsNoTracking()
            .Where(u => u.Id == targetId).Select(u => u.DepartmentId).FirstOrDefaultAsync();

        if (deptId is null || !scope.AllowedDepartmentIds.Contains(deptId.Value))
            throw AppException.Forbidden("無權檢視該員工的排班。");
    }

    // ── 小工具 ──────────────────────────────────────────────────────

    private static (int Year, int Month) ParseYearMonth(HttpRequest req)
    {
        var now = Clock.Now;
        int year  = int.TryParse(req.Query["year"],  out var y) ? y : now.Year;
        int month = int.TryParse(req.Query["month"], out var m) ? m : now.Month;
        ValidateYearMonth(year, month);
        return (year, month);
    }

    private static void ValidateYearMonth(int year, int month)
    {
        if (month is < 1 or > 12) throw AppException.BadRequest("月份必須介於 1 ~ 12。");
        // 與 RequestDateGuard 同一組防呆範圍（今日 ±3 年），擋掉民國年誤填
        var now = Clock.Now;
        if (year < now.Year - 3 || year > now.Year + 3)
            throw AppException.BadRequest($"年份 {year} 超出合理範圍，請確認是否誤填民國年。");
    }

    private static string ToEditModeString(ShiftScheduleEditMode mode) => mode switch
    {
        ShiftScheduleEditMode.Open        => "open",
        ShiftScheduleEditMode.GracePeriod => "grace_period",
        ShiftScheduleEditMode.SameDayOnly => "same_day_only",
        _                                 => "closed",
    };

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
