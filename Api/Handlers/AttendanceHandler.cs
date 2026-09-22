using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET  /attendances           → 打卡紀錄列表
/// GET  /attendances/today     → 取得當前使用者今日打卡紀錄
/// POST /attendances/clock-in  → 上班打卡
/// POST /attendances/clock-out → 下班打卡
/// POST /attendances/overtime-start → 加班開始
/// POST /attendances/overtime-end   → 加班結束
/// </summary>
public sealed class AttendanceHandler(
    AppDbContext db,
    IAttendanceReadService reader,
    IJwtService jwtService,
    IProjectAccessResolver access,
    ICalendarDayReadService calendarReader,
    IWorkPatternReadService workPattern,
    IWorkdayScheduleProvider workdaySchedule,
    IShiftScheduleReadService shiftReader)
{
    /// <summary>備註欄長度上限（與 AttendanceRecordConfiguration 的 HasMaxLength(500) 同步）</summary>
    private const int RemarkMaxLength = 500;

    /// <summary>
    /// 出缺勤列表：回傳「打卡紀錄 ∪ 當日請假日」的合併結果（見 <see cref="AttendanceLeaveMerger"/>）。
    /// 合併需要逐日展開請假，故查詢區間必須有界：未指定時回退近一年，跨度上限 MaxRangeDays。
    /// </summary>
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        // 匯出模式放寬 pageSize 上限（仍為顯式常數，不接受前端任意值）
        bool isExport = req.Query["export"] == "true";
        int  maxSize  = isExport ? AttendanceLeaveMerger.ExportMaxPageSize : 100;

        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)             : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, maxSize) : 20;

        Guid? employeeId = Guid.TryParse(req.Query["employeeId"], out var eid) ? eid : null;

        var dateTo   = DateOnly.TryParse(req.Query["dateTo"],   out var dt) ? dt : DateOnly.FromDateTime(Clock.Today);
        var dateFrom = DateOnly.TryParse(req.Query["dateFrom"], out var df) ? df : dateTo.AddYears(-1);

        if (dateTo < dateFrom)
            throw AppException.BadRequest("結束日期不可早於開始日期。");
        if (dateTo.DayNumber - dateFrom.DayNumber > AttendanceLeaveMerger.MaxRangeDays)
            throw AppException.BadRequest($"查詢區間請勿超過 {AttendanceLeaveMerger.MaxRangeDays} 天。");

        var scope  = await access.ResolveAsync(req.HttpContext.User);
        // 應出勤時段依「該列自己的日期」在新舊制間選用，故傳切換日而非單一時段
        var switchDate = await workdaySchedule.GetSwitchDateAsync();
        var result = await AttendanceLeaveMerger.BuildPagedAsync(
            reader, calendarReader, scope, page, pageSize, employeeId, dateFrom, dateTo, switchDate);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    /// <summary>取得當前使用者今日打卡紀錄（含當日已核准請假時段；無打卡紀錄時回傳僅含請假資訊的空殼 DTO）</summary>
    public async Task<IActionResult> GetTodayAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    /// <summary>上班打卡</summary>
    public async Task<IActionResult> ClockInAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockActionRequest>() ?? new ClockActionRequest(null, null);

        var now   = Clock.Now;
        var today = now.Date;

        // 檢查是否已有今日紀錄
        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == userId && a.RecordDate == today);

        if (record is not null && record.ClockInTime.HasValue)
            throw AppException.BadRequest("今日已打上班卡。");

        var ctx = await ResolveClockContextAsync(userId, now);
        if (ctx.Flexible)
        {
            if (!ClockDayPolicy.AllowsClockInOut(ctx.DayType, ctx.IsActivityAssignee))
                throw AppException.BadRequest(
                    ClockDayPolicy.ClockInOutLockReason(ctx.DayType, ctx.IsActivityAssignee)!);

            // 上班打卡鍵 08:30 才開放。**請了上午半天假的人不適用** —— 他本來就是 13:00 才來。
            if (!ctx.AfternoonOnly && TimeOnly.FromDateTime(now) < ClockRules.ClockInOpenFrom)
                throw AppException.BadRequest(
                    $"上班打卡於 {ClockRules.ClockInOpenFrom:HH\\:mm} 開放，請稍候再打卡。");
        }

        await EnsureNotOnLeaveAsync(userId, now, ctx.Flexible, forClockOut: false);

        if (record is null)
        {
            record = new AttendanceRecord
            {
                UserId     = userId,
                RecordDate = today,
                CreatedAt  = now,
            };
            db.AttendanceRecords.Add(record);
        }

        record.ClockInTime      = now;
        record.ClockInLatitude   = body.Latitude;
        record.ClockInLongitude  = body.Longitude;
        record.IsClockInAuto     = false;   // 本人打卡
        record.IsBusinessTrip    = body.IsBusinessTrip;
        // 出差當日不判定遲到（在外辦公本來就不是九點進辦公室）
        RefreshExceptionFlags(record, ctx, body.IsBusinessTrip);

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "上班打卡成功。"));
    }

    /// <summary>下班打卡</summary>
    public async Task<IActionResult> ClockOutAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockActionRequest>() ?? new ClockActionRequest(null, null);

        var now    = Clock.Now;
        var today  = now.Date;
        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == userId && a.RecordDate == today)
            ?? throw AppException.BadRequest("請先打上班卡。");

        if (!record.ClockInTime.HasValue)
            throw AppException.BadRequest("請先打上班卡。");
        if (record.ClockOutTime.HasValue)
            throw AppException.BadRequest("今日已打下班卡。");

        var ctx = await ResolveClockContextAsync(userId, now);
        if (ctx.Flexible && !ClockDayPolicy.AllowsClockInOut(ctx.DayType, ctx.IsActivityAssignee))
            throw AppException.BadRequest(
                ClockDayPolicy.ClockInOutLockReason(ctx.DayType, ctx.IsActivityAssignee)!);

        await EnsureNotOnLeaveAsync(userId, now, ctx.Flexible, forClockOut: true);

        var kind = ClockOutKind.Normal;
        if (ctx.Flexible && record.ClockInTime is { } clockIn)
        {
            kind = ClockRules.ResolveClockOutKind(clockIn, now, ctx.Schedule, ctx.AfternoonOnly);

            // 出差當日：原因欄位仍顯示但改為非必填（§5.1「改的是必填性，不是可見性」）
            if (kind != ClockOutKind.Normal && !body.IsBusinessTrip && string.IsNullOrWhiteSpace(body.Reason))
                throw AppException.BadRequest(kind == ClockOutKind.Early
                    ? "今日出勤未達應下班時間，請填寫早退原因。"
                    : "下班時間已超過應下班時間 30 分鐘，請填寫逾時原因。");
        }

        record.ClockOutTime      = now;
        record.ClockOutLatitude   = body.Latitude;
        record.ClockOutLongitude  = body.Longitude;
        record.IsClockOutAuto     = false;   // 本人打卡
        record.IsBusinessTrip     = body.IsBusinessTrip;
        RefreshExceptionFlags(record, ctx, body.IsBusinessTrip, kind);
        record.ClockOutReason     = string.IsNullOrWhiteSpace(body.Reason)
            ? null
            : body.Reason.Trim()[..Math.Min(body.Reason.Trim().Length, RemarkMaxLength)];

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "下班打卡成功。"));
    }

    /// <summary>
    /// 加班開始打卡（需已核准加班申請）。
    /// 一般上班日：須先打下班卡。
    /// 休假日（行事曆 IsHoliday / 無行事曆時的六日）或當日全日已核准請假：免下班卡，
    /// 今日無打卡紀錄時直接建立「只有加班時間」的紀錄。
    /// </summary>
    public async Task<IActionResult> OvertimeStartAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockActionRequest>() ?? new ClockActionRequest(null, null);

        var now   = Clock.Now;
        var today = now.Date;

        // 1. 先驗加班申請：放寬下班卡前置條件後，已核准加班單成為唯一授權來源
        if (!body.OvertimeRequestId.HasValue)
            throw AppException.BadRequest("請選擇已核准的加班申請。");

        var otRequest = await db.OvertimeRequests.FindAsync(body.OvertimeRequestId.Value)
            ?? throw AppException.BadRequest("找不到指定的加班申請。");

        if (otRequest.EmployeeId != userId)
            throw AppException.BadRequest("加班申請不屬於當前使用者。");
        if (otRequest.ApprovalStatus != "approved")
            throw AppException.BadRequest("加班申請尚未核准。");
        if (otRequest.OvertimeDate.Date != today)
            throw AppException.BadRequest("加班申請日期與今日不符。");

        // 2. 今日打卡紀錄（休假日可能尚未建立）
        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == userId && a.RecordDate == today);

        if (record?.OvertimeStartTime is not null)
            throw AppException.BadRequest("今日已打加班開始卡。");

        // 2.5 例假日全鎖：依法嚴禁出勤與加班（罰鍰 2 萬～100 萬），連加班單都不該救得回來
        var otCtx = await ResolveClockContextAsync(userId, now);
        if (otCtx.Flexible && !ClockDayPolicy.AllowsOvertime(otCtx.DayType))
            throw AppException.BadRequest(
                "本日為您排定的例假日，依法嚴禁出勤與加班，無法打加班卡。");

        // 3. 下班卡前置條件：休假日 / 全日請假豁免
        if (record?.ClockOutTime is null)
        {
            var todayLeaves = await reader.GetLeavesOnDateAsync(userId, DateOnly.FromDateTime(today));
            if (!await CanOvertimeWithoutClockOutAsync(userId, today, todayLeaves))
                throw AppException.BadRequest(record?.ClockInTime is null
                    ? "請先完成上下班打卡。"
                    : "請先打下班卡。");
        }

        // 4. 休假日無打卡紀錄 → 建立只含加班時間的紀錄（比照 ClockInAsync）
        if (record is null)
        {
            record = new AttendanceRecord
            {
                UserId     = userId,
                RecordDate = today,
                CreatedAt  = now,
            };
            db.AttendanceRecords.Add(record);
        }

        record.OvertimeStartTime      = now;
        record.OvertimeStartLatitude   = body.Latitude;
        record.OvertimeStartLongitude  = body.Longitude;
        record.OvertimeRequestId       = body.OvertimeRequestId;
        record.IsBusinessTrip          = body.IsBusinessTrip;
        // 加班打卡也能設出差旗標，同樣要重算遲到／早退
        RefreshExceptionFlags(record, await ResolveClockContextAsync(userId, now), body.IsBusinessTrip);

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "加班開始打卡成功。"));
    }

    /// <summary>加班結束打卡</summary>
    public async Task<IActionResult> OvertimeEndAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockActionRequest>() ?? new ClockActionRequest(null, null);

        var now    = Clock.Now;
        var today  = now.Date;
        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == userId && a.RecordDate == today)
            ?? throw AppException.BadRequest("請先打加班開始卡。");

        if (!record.OvertimeStartTime.HasValue)
            throw AppException.BadRequest("請先打加班開始卡。");
        if (record.OvertimeEndTime.HasValue)
            throw AppException.BadRequest("今日已打加班結束卡。");

        record.OvertimeEndTime      = now;
        record.OvertimeEndLatitude   = body.Latitude;
        record.OvertimeEndLongitude  = body.Longitude;
        record.IsBusinessTrip        = body.IsBusinessTrip;
        // 加班打卡也能設出差旗標，同樣要重算遲到／早退
        RefreshExceptionFlags(record, await ResolveClockContextAsync(userId, now), body.IsBusinessTrip);

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "加班結束打卡成功。"));
    }

    /// <summary>
    /// 修改出缺勤紀錄（上下班時間、加班開始/結束、備註）。
    /// 權限碼 reports-attendance:write 由 AppRouter 控管「誰能改」，此處的部門 scope 控管「能改誰」，
    /// 與 <see cref="GetAllAsync"/> 的可見範圍一致（讀得到才改得到）。
    /// </summary>
    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var recordId))
            throw AppException.BadRequest("無效的 ID。");

        var body = await req.ReadFromJsonAsync<UpdateAttendanceRequest>()
            ?? throw AppException.BadRequest("請提供更新資料。");

        if (body.Remark?.Length > RemarkMaxLength)
            throw AppException.BadRequest($"備註長度不可超過 {RemarkMaxLength} 字。");

        var record = await db.AttendanceRecords.FindAsync(recordId)
            ?? throw AppException.BadRequest("找不到指定的出缺勤紀錄。");

        var scope = await access.ResolveAsync(req.HttpContext.User);
        if (!scope.SeeAll)
        {
            // 比照 AttendanceReadService.BuildDeptScopeFilter：以「該筆紀錄所屬員工的部門」判定
            var ownerDeptId = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == record.UserId)
                .Select(u => u.DepartmentId)
                .FirstOrDefaultAsync();

            if (ownerDeptId is null || !scope.AllowedDepartmentIds.Contains(ownerDeptId.Value))
                throw AppException.Forbidden("您沒有權限修改此員工的出缺勤紀錄。");
        }

        // 時間被人工改動 → 清掉該欄的「系統補卡」標記（改為管理者維護的值）
        if (record.ClockInTime != body.ClockInTime)
            record.IsClockInAuto = false;

        if (record.ClockOutTime != body.ClockOutTime)
            record.IsClockOutAuto = false;

        record.ClockInTime        = body.ClockInTime;
        record.ClockOutTime       = body.ClockOutTime;
        record.OvertimeStartTime  = body.OvertimeStartTime;
        record.OvertimeEndTime    = body.OvertimeEndTime;
        record.Remark             = string.IsNullOrWhiteSpace(body.Remark) ? null : body.Remark.Trim();
        // IsBusinessTrip 刻意不在此異動：出差僅由本人打卡時勾選

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "出缺勤紀錄更新成功。"));
    }

    // ── Helper ──────────────────────────────────────────────────────────────────

    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }

    /// <summary>
    /// 阻擋落在已核准請假時段內的打卡（[StartDate, EndDate) 半開區間）。
    /// 僅針對上下班打卡使用；加班打卡不呼叫此方法。
    /// </summary>
    /// <summary>
    /// 今日打卡的共用判定原料：是否已切換新制、當日日別、是否為活動日預定人力、
    /// 以及「是否只上下午」（請了上午半天假 → 應下班時間改為上班打卡 ＋ 4 小時）。
    /// </summary>
    private readonly record struct ClockContext(
        bool Flexible, string DayType, bool IsActivityAssignee,
        WorkdaySchedule Schedule, bool AfternoonOnly);

    /// <summary>
    /// 依「當日出差旗標」重算遲到／早退。
    ///
    /// ⚠ 出差是**整日**旗標，四個打卡動作任一個都能設定它 ——
    /// 上班時沒勾、下班（或打加班卡）時才勾的人很常見。若只在上班打卡當下算一次遲到，
    /// 事後勾出差不會把它清掉，出缺勤報表就會出現「出差卻被記遲到」的矛盾。
    /// 故每次寫入 IsBusinessTrip 之後都要呼叫本方法重算。
    /// </summary>
    private static void RefreshExceptionFlags(
        AttendanceRecord record, ClockContext ctx, bool isBusinessTrip, ClockOutKind? clockOutKind = null)
    {
        if (!ctx.Flexible) return;

        record.IsLate = !isBusinessTrip
                     && record.ClockInTime is { } ci
                     && !record.IsClockInAuto            // 系統補卡不是本人遲到
                     && ClockRules.IsLate(ci);

        if (clockOutKind is { } kind)
            record.IsEarlyLeave = !isBusinessTrip && kind == ClockOutKind.Early;
        else if (isBusinessTrip)
            record.IsEarlyLeave = false;
    }

    private async Task<ClockContext> ResolveClockContextAsync(Guid userId, DateTime now)
    {
        var switchDate = await workdaySchedule.GetSwitchDateAsync();
        var schedule   = WorkdayHours.For(now.Date, switchDate);
        bool flexible  = switchDate is { } sd && now.Date >= sd.Date;

        if (!flexible)
            return new ClockContext(false, WorkDayTypes.Work, false, schedule, false);

        var dayType    = await shiftReader.ResolveDayTypeAsync(userId, now.Date);
        var isAssignee = await shiftReader.IsActivityAssigneeAsync(userId, now.Date);

        // 「只上下午」＝當日有一段假從上班時刻起、在半天分界（13:00）前結束
        var leaves = await reader.GetLeavesOnDateAsync(userId, DateOnly.FromDateTime(now));
        var dayStart = now.Date.Add(schedule.Start.ToTimeSpan());
        var boundary = now.Date.Add(schedule.HalfDayPmStart.ToTimeSpan());
        bool afternoonOnly = leaves.Any(l => l.StartDate <= dayStart && l.EndDate > dayStart && l.EndDate <= boundary);

        return new ClockContext(true, dayType, isAssignee, schedule, afternoonOnly);
    }

    private async Task EnsureNotOnLeaveAsync(
        Guid userId, DateTime when, bool flexible = false, bool forClockOut = false)
    {
        var active = await reader.GetActiveLeaveAtAsync(userId, when);
        if (active is null) return;

        // 半天假 13:00 交接的容許帶（前後各 5 分鐘）：
        // 下午請假者於此區間打**下班**卡、上午請假者於此區間打**上班**卡，皆視為準時。
        // 同一個容許帶也實現「請假開始前 5 分鐘提前解鎖下班打卡鍵」（§5.1.4）。
        if (flexible)
        {
            var tolerance = TimeSpan.FromMinutes(ClockRules.LeaveHandoverToleranceMinutes);
            if (forClockOut && when >= active.StartDate - tolerance) return;   // 假快開始了，先打下班卡
            if (!forClockOut && when >= active.EndDate - tolerance)   return;   // 假快結束了，先打上班卡
        }

        var typeZh = LeaveTypeNames.GetZh(active.LeaveType);
        throw AppException.BadRequest(
            $"您於此時段有已核准的請假（#{active.Id} {typeZh} " +
            $"{active.StartDate:MM/dd HH:mm}–{active.EndDate:MM/dd HH:mm}），無法打卡。");
    }

    /// <summary>
    /// 今日是否「免下班卡即可打加班開始」：
    /// (a) 行事曆休假日（該年度無行事曆資料時退回六日判定，比照 WorkCalendarHelper）
    /// (b) 當日全日已核准請假
    /// GET /attendances/today 的 CanOvertimeWithoutClockOut 與 OvertimeStartAsync 的放行共用此判定。
    /// 排班制員工（六日與國定假日皆為工作日）不適用 (a)，須照常先打下班卡。
    /// </summary>
    private async Task<bool> CanOvertimeWithoutClockOutAsync(
        Guid userId, DateTime today, IReadOnlyList<ActiveLeaveDto> leaves)
    {
        var isShiftWorker = await workPattern.IsShiftWorkerAsync(userId);
        if (await WorkCalendarHelper.IsHolidayAsync(calendarReader, isShiftWorker, today))
            return true;

        return CoversFullWorkday(leaves, today);
    }

    /// <summary>
    /// 當日已核准請假是否涵蓋整個上班時段：上午段 08:00–12:00 與下午段 13:00–17:00 各需被某一張單完整覆蓋。
    /// 可由一張全日單（存 00:00–23:59）或「上午半天 + 下午半天」兩張單共同滿足；只請半天則不成立。
    /// 刻意不用 Hours >= 8 判定 —— 多日請假的 Hours 是「天數 × 8」會誤判。
    /// </summary>
    private static bool CoversFullWorkday(IReadOnlyList<ActiveLeaveDto> leaves, DateTime today)
    {
        if (leaves.Count == 0) return false;

        bool Covers(DateTime from, DateTime to) => leaves.Any(l => l.StartDate <= from && l.EndDate >= to);

        return Covers(today.AddHours(WorkdayHours.StartHour),    today.AddHours(WorkdayHours.LunchStartHour))
            && Covers(today.AddHours(WorkdayHours.LunchEndHour), today.AddHours(WorkdayHours.EndHour));
    }

    /// <summary>
    /// 組合今日打卡 DTO + 當日已核准請假清單。打卡紀錄不存在時回傳 Id=0 的空殼 DTO，仍帶請假資訊供前端顯示提示。
    /// </summary>
    private async Task<TodayAttendanceDto> BuildTodayDtoAsync(Guid userId)
    {
        var now    = Clock.Now;
        var today  = DateOnly.FromDateTime(now);
        var record = await reader.GetTodayAsync(userId);
        var leaves = await reader.GetLeavesOnDateAsync(userId, today);
        var exempt = await CanOvertimeWithoutClockOutAsync(userId, now.Date, leaves);

        // 四週彈性工時的旗標：前端不自行重組日別規則，只吃這裡算好的結果
        var ctx = await ResolveClockContextAsync(userId, now);
        bool canClock = !ctx.Flexible || ClockDayPolicy.AllowsClockInOut(ctx.DayType, ctx.IsActivityAssignee);
        string? lockReason = ctx.Flexible
            ? ClockDayPolicy.ClockInOutLockReason(ctx.DayType, ctx.IsActivityAssignee)
            : null;
        DateTime? expectedOut = ctx.Flexible && record?.ClockInTime is { } ci
            ? ClockRules.ExpectedClockOut(ci, ctx.Schedule, ctx.AfternoonOnly)
            : null;

        var flexFields = (
            FlexibleEnabled:      ctx.Flexible,
            DayType:              ctx.Flexible ? ctx.DayType : null,
            IsActivityAssignee:   ctx.IsActivityAssignee,
            CanClockInOut:        canClock,
            ClockLockReason:      lockReason,
            ExpectedClockOutTime: expectedOut);

        return record is null
            ? new TodayAttendanceDto(
                Id: 0,
                RecordDate: today.ToDateTime(TimeOnly.MinValue),
                ClockInTime: null,             ClockInLatitude:  null, ClockInLongitude:  null,
                ClockOutTime: null,            ClockOutLatitude: null, ClockOutLongitude: null,
                OvertimeStartTime: null,       OvertimeStartLatitude: null, OvertimeStartLongitude: null,
                OvertimeEndTime:   null,       OvertimeEndLatitude:   null, OvertimeEndLongitude:   null,
                OvertimeRequestId: null,
                TodayLeaves: leaves,
                CanOvertimeWithoutClockOut: exempt,
                IsBusinessTrip: false,
                FlexibleEnabled:      flexFields.FlexibleEnabled,
                DayType:              flexFields.DayType,
                IsActivityAssignee:   flexFields.IsActivityAssignee,
                CanClockInOut:        flexFields.CanClockInOut,
                ClockLockReason:      flexFields.ClockLockReason,
                ExpectedClockOutTime: flexFields.ExpectedClockOutTime)
            : record with
            {
                TodayLeaves = leaves,
                CanOvertimeWithoutClockOut = exempt,
                FlexibleEnabled      = flexFields.FlexibleEnabled,
                DayType              = flexFields.DayType,
                IsActivityAssignee   = flexFields.IsActivityAssignee,
                CanClockInOut        = flexFields.CanClockInOut,
                ClockLockReason      = flexFields.ClockLockReason,
                ExpectedClockOutTime = flexFields.ExpectedClockOutTime,
            };
    }
}
