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
/// 活動日（四週彈性工時 §3.2）—— 各部門協理於活動 3 個月前預先排定日期並勾選預定人力，
/// 讓同仁排自己的班時看得到。
///
/// GET    /activity-days?year=&amp;month=[&amp;departmentId=]
/// POST   /activity-days
/// PUT    /activity-days/{id}      （含**改期**）
/// DELETE /activity-days/{id}
///
/// 三個關鍵設計：
/// <list type="number">
///   <item><b>活動日是疊加旗標，不是第 5 種日別</b>。同一天可以既是上班日又是活動日；
///         **也可以壓在國定假日上**（2026-09-17 決議，因為假日活動本來就會排在國定假日）。
///         本 Handler 完全不碰 <c>ShiftScheduleDay</c>。</item>
///   <item><b>活動日優先（2026-09-29 改）</b>。預定人力當天若排了例假／休假，存檔時由
///         <see cref="ActivityScheduleOverrideService"/> 改為上班日並把少掉的那一天自動補排到當月其他日子，
///         同仁以右上角鈴鐺收到通知。原設計「不改寫班表、請同仁自提改班申請」走不通 ——
///         活動日格子在月曆與改班表單都被鎖成唯讀，同仁兩邊都改不了。
///         改期／移除預定人力時**不還原**先前的覆蓋。</item>
///   <item><b>不受 10–25 日開放期限制</b>。活動日得因業主通知或天氣因素變更，
///         協理於原活動日之前皆可改期。</item>
/// </list>
///
/// 授權：`activity-days:write` ＋ 只能排定 <c>ProjectAccessScope</c> 涵蓋的部門
/// （**不得硬編 JobTitle.Level 判定「協理」**，組織改制後職級對應會漂移）。
/// </summary>
public sealed class ActivityDayHandler(
    AppDbContext db,
    IJwtService jwtService,
    IProjectAccessResolver access,
    ICalendarDayReadService calendarReader,
    IEmployeeWorkdaysFactory workdaysFactory)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var (year, month) = ParseYearMonth(req);
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        var scope = await access.ResolveAsync(req.HttpContext.User);

        var query = db.ActivityDays.AsNoTracking()
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd);

        // 部門可見性：看得到哪些部門的活動日
        if (!scope.SeeAll)
            query = query.Where(a => scope.AllowedDepartmentIds.Contains(a.DepartmentId));

        if (int.TryParse(req.Query["departmentId"], out var deptFilter))
            query = query.Where(a => a.DepartmentId == deptFilter);

        var rows = await query.OrderBy(a => a.Date).ThenBy(a => a.Id).ToListAsync();
        var holidays = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);

        var dtos = new List<ActivityDayDto>(rows.Count);
        foreach (var r in rows) dtos.Add(await ToDtoAsync(r, holidays));

        return new OkObjectResult(ApiResponse.Ok(dtos));
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await ReadBodyAsync(req);

        await EnsureCanWriteDepartmentAsync(req, body.DepartmentId);
        await EnsureAssigneesValidAsync(req, body.AssigneeUserIds);

        var now = Clock.Now;
        var entity = new ActivityDay
        {
            Date            = body.Date.Date,
            DepartmentId    = body.DepartmentId,
            Title           = body.Title.Trim(),
            CreatedByUserId = userId,
            CreatedAt       = now,
            UpdatedAt       = now,
        };
        db.ActivityDays.Add(entity);
        await db.SaveChangesAsync();

        await ReplaceAssigneesAsync(entity.Id, body.AssigneeUserIds);
        await db.SaveChangesAsync();

        // 活動日優先：預定人力當天排了例假／休假 → 改上班日並自動補排
        var adjusted = await ActivityScheduleOverrideService.ApplyAsync(
            db, calendarReader, workdaysFactory, entity, body.AssigneeUserIds);
        await db.SaveChangesAsync();

        var affected = await RecheckAsync(body.AssigneeUserIds, [entity.Date]);
        var dto = await ToDtoAsync(entity, await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, entity.Date, entity.Date));

        return new OkObjectResult(ApiResponse.Ok(
            new SaveActivityDayResultDto(dto, DateChanged: false, Adjusted: [.. adjusted], Affected: [.. affected]),
            "活動日已建立。"));
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var activityId))
            throw AppException.BadRequest("Invalid id.");

        var entity = await db.ActivityDays
            .Include(a => a.Assignees)
            .FirstOrDefaultAsync(a => a.Id == activityId)
            ?? throw AppException.NotFound("查無此活動日。");

        var body = await ReadBodyAsync(req);

        await EnsureCanWriteDepartmentAsync(req, entity.DepartmentId);      // 原部門
        if (body.DepartmentId != entity.DepartmentId)
            await EnsureCanWriteDepartmentAsync(req, body.DepartmentId);    // 新部門
        // 更新時只驗「新增」的人力：既有名單即使是歷史資料（本人 / 範圍外）也不該讓整張活動日改不動
        await EnsureAssigneesValidAsync(req, body.AssigneeUserIds, entity.Assignees.Select(a => a.UserId).ToHashSet());

        var oldDate = entity.Date.Date;
        var newDate = body.Date.Date;

        // 改期只能往「原活動日之前」操作 —— 活動都過了才改沒有意義，且會影響已成立的出勤事實
        if (oldDate != newDate && oldDate < Clock.Now.Date)
            throw AppException.BadRequest($"原活動日（{oldDate:yyyy/MM/dd}）已過，無法改期。");

        // 受影響者＝原活動日的預定人力 ∪ 新名單（改期後兩邊的班表都要重驗）
        var previousAssignees = entity.Assignees.Select(a => a.UserId).ToList();
        var affectedUsers = previousAssignees.Union(body.AssigneeUserIds).Distinct().ToList();

        entity.Date         = newDate;
        entity.DepartmentId = body.DepartmentId;
        entity.Title        = body.Title.Trim();
        entity.UpdatedAt    = Clock.Now;

        await ReplaceAssigneesAsync(entity.Id, body.AssigneeUserIds);
        await db.SaveChangesAsync();

        // 活動日優先：只看「活動日現在在哪一天」的預定人力（舊日期已經沒有活動，不還原先前的覆蓋）
        var adjusted = await ActivityScheduleOverrideService.ApplyAsync(
            db, calendarReader, workdaysFactory, entity, body.AssigneeUserIds);
        await db.SaveChangesAsync();

        // 月份來源含新舊兩天（跨月改期時兩個月都要重驗）
        var affected = await RecheckAsync(affectedUsers, [oldDate, newDate]);
        var dto = await ToDtoAsync(entity, await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, newDate, newDate));

        return new OkObjectResult(ApiResponse.Ok(
            new SaveActivityDayResultDto(dto, DateChanged: oldDate != newDate, Adjusted: [.. adjusted], Affected: [.. affected]),
            oldDate != newDate ? "活動日已改期。" : "活動日已更新。"));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var activityId))
            throw AppException.BadRequest("Invalid id.");

        var entity = await db.ActivityDays
            .Include(a => a.Assignees)
            .FirstOrDefaultAsync(a => a.Id == activityId)
            ?? throw AppException.NotFound("查無此活動日。");

        await EnsureCanWriteDepartmentAsync(req, entity.DepartmentId);

        // Assignees 走 Cascade，但這裡已 Include 進追蹤範圍，交給 EF 一併刪除
        db.ActivityDays.Remove(entity);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "活動日已刪除。"));
    }

    // ── 內部 ────────────────────────────────────────────────────────

    /// <summary>
    /// 覆蓋並補排後，對受影響同仁重跑 §3.3 三條檢核（例假排滿 / 連續上班 ≤12 天 / 14 天內 ≥2 例假），
    /// 回報**仍不通過**者（班表本來就沒排完、或補排找不到合法日子）。只讀不寫。
    ///
    /// 「活動日當天排了例假／休假」的衝突已由 <see cref="ActivityScheduleOverrideService"/> 處理掉，不在此列。
    /// </summary>
    /// <param name="userIds">受影響的同仁（原活動日 ∪ 新活動日的預定人力）。</param>
    /// <param name="monthDates">要重跑三條檢核的月份來源（改期時含新舊兩個日期，可能跨月）。</param>
    private async Task<List<AffectedScheduleDto>> RecheckAsync(
        IReadOnlyCollection<Guid> userIds,
        IReadOnlyCollection<DateTime> monthDates)
    {
        var result = new List<AffectedScheduleDto>();
        if (userIds.Count == 0) return result;

        // 受影響的「年月」集合：改期可能跨月，兩個日期各自所屬的月都要驗
        var months = monthDates.Select(d => (d.Year, d.Month)).Distinct().ToList();

        var names = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        foreach (var (year, month) in months)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd   = monthStart.AddMonths(1).AddDays(-1);
            var holidays   = await ShiftScheduleMap.LoadPublicHolidaysAsync(calendarReader, monthStart, monthEnd);

            var saved = await db.ShiftScheduleDays.AsNoTracking()
                .Where(d => userIds.Contains(d.UserId) && d.Date >= monthStart && d.Date <= monthEnd)
                .ToListAsync();
            var byUser = saved.ToLookup(d => d.UserId);

            foreach (var uid in userIds)
            {
                // 該月完全沒排班的人不算「受影響」—— 他本來就還沒排，不是被改期弄壞的
                if (!byUser[uid].Any()) continue;

                // 日別組裝走共用 helper（§10.4：四個消費點必須同一份，各寫各的必然漂移）
                var userDays = byUser[uid].ToDictionary(x => x.Date.Date, x => x.DayType);
                var map      = ShiftScheduleMap.BuildMonthDayTypes(monthStart, monthEnd, userDays, holidays);

                var check = ShiftScheduleValidator.Validate(year, month, map);
                if (check.CanSave) continue;
                var blocks = check.Blocks;

                result.Add(new AffectedScheduleDto(
                    uid, names.TryGetValue(uid, out var n) ? n : "(已離職)",
                    year, month, [.. blocks]));
            }
        }
        return result;
    }

    private async Task ReplaceAssigneesAsync(int activityDayId, IReadOnlyCollection<Guid> userIds)
    {
        var existing = await db.ActivityDayAssignees
            .Where(a => a.ActivityDayId == activityDayId).ToListAsync();
        db.ActivityDayAssignees.RemoveRange(existing);

        foreach (var uid in userIds.Distinct())
            db.ActivityDayAssignees.Add(new ActivityDayAssignee
            {
                ActivityDayId = activityDayId,
                UserId        = uid,
            });
    }

    private async Task<ActivityDayDto> ToDtoAsync(ActivityDay e, IReadOnlyDictionary<DateTime, string> holidays)
    {
        // ⚠ 這裡刻意走導覽屬性而非 .Join(db.Users, a => a.UserId, u => u.Id, …)：
        // 後者會讓 EF 把 join key 推斷成 object（`(object)a.UserId`）而無法翻譯成 SQL，
        // 症狀是「資料其實已寫入、但回應噴 500」。
        var assignees = await db.ActivityDayAssignees.AsNoTracking()
            .Where(a => a.ActivityDayId == e.Id && a.User != null)
            .OrderBy(a => a.User!.Name)
            .Select(a => new ActivityDayAssigneeDto(
                a.UserId,
                a.User!.Name,
                a.User.Department != null ? a.User.Department.Name : null,
                a.User.JobTitle   != null ? a.User.JobTitle.Name   : null))
            .ToListAsync();

        var deptName = await db.Departments.AsNoTracking()
            .Where(d => d.Id == e.DepartmentId).Select(d => d.Name).FirstOrDefaultAsync();
        var creator = await db.Users.AsNoTracking()
            .Where(u => u.Id == e.CreatedByUserId).Select(u => u.Name).FirstOrDefaultAsync();

        var isHoliday = holidays.TryGetValue(e.Date.Date, out var holidayName);

        return new ActivityDayDto(
            Id:              e.Id,
            Date:            e.Date,
            DepartmentId:    e.DepartmentId,
            DepartmentName:  deptName,
            Title:           e.Title,
            CreatedByUserId: e.CreatedByUserId,
            CreatedByName:   creator,
            IsPublicHoliday: isHoliday,
            HolidayName:     isHoliday ? holidayName : null,
            Assignees:       [.. assignees]);
    }

    private async Task<SaveActivityDayRequest> ReadBodyAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<SaveActivityDayRequest>()
                   ?? throw AppException.BadRequest("Invalid request body.");

        if (string.IsNullOrWhiteSpace(body.Title))
            throw AppException.BadRequest("請填寫活動名稱。");
        if (body.Title.Trim().Length > 200)
            throw AppException.BadRequest("活動名稱不可超過 200 字。");
        if (body.DepartmentId <= 0)
            throw AppException.BadRequest("請選擇部門。");

        // 與 RequestDateGuard 同一組年份防呆（今日 ±3 年），擋掉民國年誤填
        RequestDateGuard.Ensure(body.Date, "活動日期");

        return body with { AssigneeUserIds = body.AssigneeUserIds ?? [] };
    }

    /// <summary>
    /// 預定人力驗證：必須在職、非 Superadmin；非 Superadmin 呼叫者另須：
    /// 人員所屬部門落在其 ProjectAccessScope 內（不得指派範圍外部門的人），且不得把自己列為預定人力
    /// （否則可自行把自己的例假 / 休假覆蓋成上班日、或繞過排班規範）。
    /// <paramref name="existingUserIds"/>：更新時既有的預定人力，不重驗範圍 / 本人條件。
    /// </summary>
    private async Task EnsureAssigneesValidAsync(
        HttpRequest req, IReadOnlyCollection<Guid> userIds, IReadOnlySet<Guid>? existingUserIds = null)
    {
        if (userIds.Count == 0) return;

        var found = await db.Users.AsNoTracking()
            .CountAsync(u => userIds.Contains(u.Id) && u.Status == "active" && !u.IsSuperAdmin);

        if (found != userIds.Distinct().Count())
            throw AppException.BadRequest("預定人力中含無效或已離職的人員。");

        var principal = await jwtService.ValidateRequestAsync(req)
                        ?? throw AppException.Unauthorized("Invalid token.");
        if (principal.FindFirst("is_superadmin")?.Value == "true") return;

        var added = userIds.Distinct().Where(u => existingUserIds is null || !existingUserIds.Contains(u)).ToList();
        if (added.Count == 0) return;

        var callerId = await GetUserIdAsync(req);
        if (added.Contains(callerId))
            throw AppException.BadRequest("不可將自己列為預定人力，請由其他有權限的人建立或調整。");

        var scope = await access.ResolveAsync(principal);
        if (scope.SeeAll) return;

        var deptIds = await db.Users.AsNoTracking()
            .Where(u => added.Contains(u.Id))
            .Select(u => u.DepartmentId)
            .ToListAsync();
        if (deptIds.Any(d => d is null || !scope.AllowedDepartmentIds.Contains(d.Value)))
            throw AppException.Forbidden("預定人力中含不在您可見部門範圍內的人員。");
    }

    private async Task EnsureCanWriteDepartmentAsync(HttpRequest req, int departmentId)
    {
        var principal = await jwtService.ValidateRequestAsync(req)
                        ?? throw AppException.Unauthorized("Invalid token.");

        if (principal.FindFirst("is_superadmin")?.Value == "true") return;

        var scope = await access.ResolveAsync(principal);
        if (scope.SeeAll) return;
        if (scope.AllowedDepartmentIds.Contains(departmentId)) return;

        throw AppException.Forbidden("無權排定該部門的活動日。");
    }

    private static (int Year, int Month) ParseYearMonth(HttpRequest req)
    {
        var now = Clock.Now;
        int year  = int.TryParse(req.Query["year"],  out var y) ? y : now.Year;
        int month = int.TryParse(req.Query["month"], out var m) ? m : now.Month;
        if (month is < 1 or > 12) throw AppException.BadRequest("月份必須介於 1 ~ 12。");
        return (year, month);
    }

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
