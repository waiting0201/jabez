using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

/// <summary>
/// 〈出勤／排休總覽表〉（四週彈性工時功能 B，§4）——
/// 選年月 ＋ 部門，看全員當月預排出勤狀態；**以三種顏色顯示例假日／休假日／排定活動日**。
///
/// GET /reports/shift-schedule?year=&amp;month=[&amp;departmentId=]
///
/// 與 §3.1 同一套可見性判定，不另立一套：
/// <list type="bullet">
///   <item>進入頁面：<c>reports-shift-schedule:read</c></item>
///   <item>持 <c>shift-schedule:view-all</c> → 全公司</item>
///   <item>未持者 → 僅 <c>ProjectAccessScope</c> 涵蓋的部門</item>
/// </list>
///
/// ⚠ **已知取捨**：六個部門的 <c>Department.CanSeeAll = 1</c>，故那些部門的一般同仁
/// 也看得到全公司排班。這是與需求方確認後接受的現狀（排班不視為敏感資料），見 §3.1。
///
/// 不分頁：一個部門最多數十人 × 31 天，一次回完整表格才畫得出橫向月曆；
/// 需要匯出時前端直接用已載入的資料產 Excel（同 payroll 的做法）。
/// </summary>
public sealed class ShiftScheduleReportHandler(
    AppDbContext db,
    IProjectAccessResolver access,
    ICalendarDayReadService calendarReader)
{
    public async Task<IActionResult> GetOverviewAsync(HttpRequest req)
    {
        var now   = Clock.Now;
        int year  = int.TryParse(req.Query["year"],  out var y) ? y : now.Year;
        int month = int.TryParse(req.Query["month"], out var m) ? m : now.Month;

        if (month is < 1 or > 12) throw AppException.BadRequest("月份必須介於 1 ~ 12。");
        if (year < now.Year - 3 || year > now.Year + 3)
            throw AppException.BadRequest($"年份 {year} 超出合理範圍。");

        var monthStart  = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthEnd    = monthStart.AddDays(daysInMonth - 1);

        var scope = await access.ResolveAsync(req.HttpContext.User);
        int? departmentId = int.TryParse(req.Query["departmentId"], out var d) ? d : null;

        // ── 員工母體：在職、非超管、落在可見部門內 ──────────────────
        var empQuery = db.Users.AsNoTracking()
            .Where(u => u.Status == "active" && !u.IsSuperAdmin);

        if (!scope.SeeAll)
            empQuery = empQuery.Where(u => u.DepartmentId != null
                                        && scope.AllowedDepartmentIds.Contains(u.DepartmentId.Value));
        if (departmentId is { } deptId)
            empQuery = empQuery.Where(u => u.DepartmentId == deptId);

        var employees = await empQuery
            .OrderBy(u => u.Department != null ? u.Department.Name : "")
            .ThenBy(u => u.Name)
            .Select(u => new
            {
                u.Id,
                u.Name,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                u.DepartmentId,
            })
            .ToListAsync();

        var userIds = employees.Select(e => e.Id).ToList();

        // ── 排班、國定假日、活動日 ────────────────────────────────
        var savedDays = await db.ShiftScheduleDays.AsNoTracking()
            .Where(x => userIds.Contains(x.UserId) && x.Date >= monthStart && x.Date <= monthEnd)
            .Select(x => new { x.UserId, x.Date, x.DayType })
            .ToListAsync();
        var savedByUser = savedDays.ToLookup(x => x.UserId);

        var holidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);

        // 活動日：只取員工母體所屬部門的（跨部門活動不應污染本表）
        var deptIds = employees.Select(e => e.DepartmentId).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var activities = await db.ActivityDays.AsNoTracking()
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd && deptIds.Contains(a.DepartmentId))
            .Select(a => new
            {
                a.Date,
                a.DepartmentId,
                AssigneeIds = a.Assignees.Select(x => x.UserId).ToList(),
            })
            .ToListAsync();

        var activityByDeptDate = activities
            .ToLookup(a => (a.DepartmentId, a.Date.Date));
        var assigneeByDate = new HashSet<(Guid, DateTime)>(
            activities.SelectMany(a => a.AssigneeIds.Select(uid => (uid, a.Date.Date))));

        // ── 組表 ──────────────────────────────────────────────────
        var requiredRest = ShiftScheduleValidator.RequiredRestDaysFor(year, month);
        var workingCount = new int[daysInMonth + 1];

        var rows = new List<ShiftOverviewRowDto>(employees.Count);
        foreach (var emp in employees)
        {
            var saved = savedByUser[emp.Id].ToDictionary(x => x.Date.Date, x => x.DayType);
            var map   = ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, saved, holidays);

            var cells = new List<ShiftOverviewCellDto>(daysInMonth);
            int statutoryOff = 0, restDay = 0;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = monthStart.AddDays(day - 1);
                var type = map[date];

                if (type == WorkDayTypes.StatutoryOff) statutoryOff++;
                else if (type == WorkDayTypes.RestDay) restDay++;
                else if (type == WorkDayTypes.Work)    workingCount[day]++;

                bool hasActivity = emp.DepartmentId is { } dep
                                && activityByDeptDate[(dep, date)].Any();

                cells.Add(new ShiftOverviewCellDto(
                    Day:                day,
                    DayType:            type,
                    IsActivityDay:      hasActivity,
                    IsActivityAssignee: assigneeByDate.Contains((emp.Id, date))));
            }

            rows.Add(new ShiftOverviewRowDto(
                UserId:               emp.Id,
                UserName:             emp.Name,
                DepartmentName:       emp.DepartmentName,
                StatutoryOffCount:    statutoryOff,
                RestDayCount:         restDay,
                RequiredStatutoryOff: ShiftScheduleValidator.RequiredStatutoryOffDays,
                RequiredRestDay:      requiredRest,
                QuotaSatisfied:       statutoryOff >= ShiftScheduleValidator.RequiredStatutoryOffDays
                                   && restDay      >= requiredRest,
                Cells:                [.. cells]));
        }

        // ── 表尾：每日出勤人數 ────────────────────────────────────
        var dayStats = new List<ShiftOverviewDayStatDto>(daysInMonth);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = monthStart.AddDays(day - 1);
            bool isHoliday = holidays.TryGetValue(date, out var holidayName);
            bool isWeekday = date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

            dayStats.Add(new ShiftOverviewDayStatDto(
                Day:             day,
                Date:            date,
                Weekday:         (int)date.DayOfWeek,
                IsPublicHoliday: isHoliday,
                HolidayName:     isHoliday ? holidayName : null,
                WorkingCount:    workingCount[day],
                // §3.4 軟性預警：週一至週五全員皆休才是異常；週末與國定假日本來就可能沒人
                NoCoverage:      rows.Count > 0 && workingCount[day] == 0 && isWeekday && !isHoliday));
        }

        return new OkObjectResult(ApiResponse.Ok(new ShiftOverviewDto(
            Year:        year,
            Month:       month,
            DaysInMonth: daysInMonth,
            DepartmentId: departmentId,
            DayStats:    [.. dayStats],
            Rows:        [.. rows])));
    }
}
