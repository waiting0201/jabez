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
/// POST /attendances/clock-challenge → 取得一次性打卡挑戰碼（防機器人打卡）
/// POST /attendances/clock-in  → 上班打卡
/// POST /attendances/clock-out → 下班打卡（須帶 projectAllocations 專案工時佔比）
/// PUT  /attendances/today/project-allocations → 當日修改專案工時佔比
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
    IShiftScheduleReadService shiftReader,
    IAttendancePunchGuard punchGuard,
    IProjectAllocationSchedule allocationSchedule)
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
            reader, calendarReader, scope, page, pageSize, employeeId, dateFrom, dateTo, switchDate, shiftReader);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    /// <summary>取得當前使用者今日打卡紀錄（含當日已核准請假時段；無打卡紀錄時回傳僅含請假資訊的空殼 DTO）</summary>
    public async Task<IActionResult> GetTodayAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    /// <summary>
    /// 取得一次性打卡挑戰碼（防機器人打卡）：四個打卡動作送出前須先取碼，
    /// 簽發後至少等 MinWaitMs 才能使用，規則見 <see cref="AttendancePunchGuard"/>。
    /// </summary>
    public async Task<IActionResult> IssueChallengeAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockChallengeRequest>();
        var dto    = punchGuard.IssueChallenge(userId, body?.Action ?? string.Empty);
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    /// <summary>上班打卡</summary>
    public async Task<IActionResult> ClockInAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockActionRequest>() ?? new ClockActionRequest(null, null);

        await punchGuard.GuardAsync(req, userId, "clock-in", body);

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

            // 上班打卡鍵 08:30（自訂時段者為上班前 30 分）才開放。
            // **請了上午半天假的人不適用** —— 他本來就是 13:00 才來。
            if (!ctx.AfternoonOnly && TimeOnly.FromDateTime(now) < ctx.Profile.OpenFrom)
                throw AppException.BadRequest(
                    $"上班打卡於 {ctx.Profile.OpenFrom:HH\\:mm} 開放，請稍候再打卡。");
        }

        await EnsureNotOnLeaveAsync(userId, now, ctx.Flexible, forClockOut: false);

        // 請假結束後不得再補打上班卡：EnsureNotOnLeaveAsync 只看「此刻」是否在假內，
        // 全日請假 08:00–17:00 到了 20:30 就不在任何假單內而被放行（2026-10-19）。
        var todayLeaves = await reader.GetLeavesOnDateAsync(userId, DateOnly.FromDateTime(now));
        if (ComputeClockInNotAfter(today, todayLeaves, ctx.Schedule) is { } notAfter && now >= notAfter)
            throw AppException.BadRequest(notAfter == today
                ? "您今日已全日請假，無法打上班卡。"
                : $"您今日有已核准的請假，應出勤時段已於 {notAfter:HH\\:mm} 結束，無法打上班卡。");

        // 遲到：超過準時界線（公司 09:30、自訂時段 S+2 分、上午請假者 13:05）須填遲到原因（2026-10-06）。
        // 出差只是出缺勤紀錄上的標記，不影響遲到判定與原因必填（2026-10-12）。
        if (ctx.Flexible
            && ClockRules.IsLate(now, ctx.Schedule, ctx.AfternoonOnly, ctx.Profile)
            && string.IsNullOrWhiteSpace(body.Reason))
            throw AppException.BadRequest(
                $"已超過 {ClockRules.LateThreshold(ctx.Schedule, ctx.AfternoonOnly, ctx.Profile):HH\\:mm}，請填寫遲到原因。");

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
        record.LateReason        = TrimReason(body.Reason);
        RefreshExceptionFlags(record, ctx);

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "上班打卡成功。"));
    }

    /// <summary>下班打卡</summary>
    public async Task<IActionResult> ClockOutAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockActionRequest>() ?? new ClockActionRequest(null, null);

        await punchGuard.GuardAsync(req, userId, "clock-out", body);

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
            kind = ClockRules.ResolveClockOutKind(clockIn, now, ctx.Schedule, ctx.AfternoonOnly, ctx.Profile);

            if (kind != ClockOutKind.Normal && string.IsNullOrWhiteSpace(body.Reason))
                throw AppException.BadRequest(kind == ClockOutKind.Early
                    ? "今日出勤未達應下班時間，請填寫早退原因。"
                    : $"下班時間已超過應下班時間 {ctx.Profile.GraceMinutes} 分鐘，請填寫逾時原因。");
        }

        // 專案工時佔比（上線日起新舊制皆必填）：與打卡同一次 SaveChanges 寫入，驗證失敗則整筆下班卡不成立。
        // 上線日前一律忽略（前端也不會送）
        bool allocationRequired = allocationSchedule.IsActive(today);
        if (allocationRequired)
        {
            ProjectAllocationValidator.Validate(body.ProjectAllocations);
            await ProjectAllocationValidator.EnsureProjectsActiveAsync(db, body.ProjectAllocations!);
        }

        record.ClockOutTime      = now;
        record.ClockOutLatitude   = body.Latitude;
        record.ClockOutLongitude  = body.Longitude;
        record.IsClockOutAuto     = false;   // 本人打卡
        record.IsBusinessTrip     = body.IsBusinessTrip;
        RefreshExceptionFlags(record, ctx, kind);
        record.ClockOutReason     = TrimReason(body.Reason);

        if (allocationRequired)
            db.AttendanceProjectAllocations.AddRange(
                ProjectAllocationValidator.ToEntities(record.Id, body.ProjectAllocations!));

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "下班打卡成功。"));
    }

    /// <summary>
    /// 修改今日的專案工時佔比（整批替換）。只能改**自己今天、已打下班卡**的那筆；
    /// 隔天起鎖定（此端點只認 Clock.Now 當日；歷史日只有系統補卡日可經 FillMissingProjectAllocationsAsync 補填一次）。
    /// </summary>
    public async Task<IActionResult> UpdateTodayProjectAllocationsAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<UpdateProjectAllocationsRequest>()
                     ?? throw AppException.BadRequest("請求內容不可為空。");

        var today  = Clock.Now.Date;
        if (!allocationSchedule.IsActive(today))
            throw AppException.BadRequest("專案工時功能尚未啟用。");

        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == userId && a.RecordDate == today);

        if (record is null || !record.ClockOutTime.HasValue)
            throw AppException.BadRequest("今日尚未打下班卡，無法修改專案工時。");

        ProjectAllocationValidator.Validate(body.ProjectAllocations);
        await ProjectAllocationValidator.EnsureProjectsActiveAsync(db, body.ProjectAllocations!);

        // 刪除與新增須同一交易（DbContext 啟用 EnableRetryOnFailure，須走 execution strategy）；
        // 先刪後加，避開 (AttendanceRecordId, ProjectId) 唯一索引的順序問題
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.AttendanceProjectAllocations
                .Where(x => x.AttendanceRecordId == record.Id)
                .ExecuteDeleteAsync();
            db.AttendanceProjectAllocations.AddRange(
                ProjectAllocationValidator.ToEntities(record.Id, body.ProjectAllocations!));
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });
        db.ChangeTracker.Clear();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "專案工時已更新。"));
    }

    /// <summary>
    /// 本人待補填專案佔比的日子：登入時系統自動補下班卡（IsClockOutAuto）的紀錄沒經過下班打卡視窗，
    /// 前端進系統時以此清單逐日跳出強制視窗。
    /// </summary>
    public async Task<IActionResult> GetMissingProjectAllocationsAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        return new OkObjectResult(ApiResponse.Ok(await ListMissingAllocationDaysAsync(userId)));
    }

    /// <summary>
    /// 補填系統補卡日的專案佔比（同一份佔比可一次套用多天）。
    /// 這是「隔天鎖定」的唯一例外：每個 id 都必須仍在 <see cref="MissingAllocationDays"/> 內
    /// （本人、系統補下班卡、上線日之後、今天以前、尚無佔比），故只能補一次、不能藉此改歷史已填的佔比。
    /// 回傳剩餘待補清單。
    /// </summary>
    public async Task<IActionResult> FillMissingProjectAllocationsAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<FillMissingAllocationsRequest>()
                     ?? throw AppException.BadRequest("請求內容不可為空。");

        var ids = (body.AttendanceIds ?? []).Distinct().ToList();
        if (ids.Count == 0)
            throw AppException.BadRequest("請指定要補填的日期。");

        ProjectAllocationValidator.Validate(body.ProjectAllocations);
        await ProjectAllocationValidator.EnsureProjectsActiveAsync(db, body.ProjectAllocations!);

        var eligible = await MissingAllocationDays(userId)
            .Where(a => ids.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();
        if (eligible.Count != ids.Count)
            throw AppException.BadRequest("所選日期不需補填或已補填過，請重新整理後再試。");

        foreach (var id in ids)
            db.AttendanceProjectAllocations.AddRange(
                ProjectAllocationValidator.ToEntities(id, body.ProjectAllocations!));
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(await ListMissingAllocationDaysAsync(userId), "專案工時已補填。"));
    }

    /// <summary>
    /// 待補填佔比的判準單一真相（GET 清單與 PUT 驗證共用）。
    /// 尚未上線（上線日未設定或在今天之後）時恆為空集合；上線後只追上線日（含）之後的紀錄。
    /// </summary>
    private IQueryable<AttendanceRecord> MissingAllocationDays(Guid userId)
    {
        var today = Clock.Now.Date;
        if (allocationSchedule.StartDate is not { } start || start > today)
            return db.AttendanceRecords.Where(_ => false);

        return db.AttendanceRecords.Where(a =>
            a.UserId == userId
            && a.IsClockOutAuto
            && a.ClockOutTime != null
            && a.RecordDate >= start
            && a.RecordDate < today
            && !db.AttendanceProjectAllocations.Any(x => x.AttendanceRecordId == a.Id));
    }

    private async Task<List<MissingAllocationDayDto>> ListMissingAllocationDaysAsync(Guid userId)
    {
        var rows = await MissingAllocationDays(userId)
            .AsNoTracking()
            .OrderBy(a => a.RecordDate)
            .Select(a => new { a.Id, a.RecordDate, a.ClockInTime, a.ClockOutTime })
            .ToListAsync();
        return [.. rows.Select(r => new MissingAllocationDayDto(
            r.Id, r.RecordDate.ToString("yyyy-MM-dd"), r.ClockInTime, r.ClockOutTime))];
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

        await punchGuard.GuardAsync(req, userId, "overtime-start", body);

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
        if (otRequest.RequestKind == OvertimeRequestKinds.PostHoc)
            throw AppException.BadRequest("事後申請的加班核准後由系統自動寫入出缺勤，不需（也不能）打加班開始卡。");

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
        var todayLeaves = await reader.GetLeavesOnDateAsync(userId, DateOnly.FromDateTime(today));
        bool exempt = await CanOvertimeWithoutClockOutAsync(userId, today, todayLeaves);
        if (record?.ClockOutTime is null && !exempt)
            throw AppException.BadRequest(record?.ClockInTime is null
                ? "請先完成上下班打卡。"
                : "請先打下班卡。");

        // 3.5 防灌工時（2026-10）：一般上班日須在「實際下班打卡」與「應下班時間」之後才能打加班開始卡，
        // 否則可在正常上班時段內（如 00:03）打加班卡把上班時間領成加班費。休假日 / 全日請假（exempt）不套此限。
        if (!exempt)
        {
            var notBefore = ComputeOvertimeNotBefore(today, record?.ClockOutTime, todayLeaves, otCtx);
            if (now < notBefore)
                throw AppException.BadRequest(
                    $"須於下班後（{notBefore:HH:mm} 起）才能打加班開始卡，正常上班時段內不可打加班卡。");
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

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "加班開始打卡成功。"));
    }

    /// <summary>加班結束打卡</summary>
    public async Task<IActionResult> OvertimeEndAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockActionRequest>() ?? new ClockActionRequest(null, null);

        await punchGuard.GuardAsync(req, userId, "overtime-end", body);

        var now    = Clock.Now;
        var today  = now.Date;
        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == userId && a.RecordDate == today);

        // 跨日加班（2026-10）：加班過午夜，結束卡落在隔天，但紀錄的 RecordDate 是加班開始那天。
        // 取消「自動補加班結束卡」後，這是員工唯一能讓跨日加班被結算的路徑 ——
        // 隔天 CrossDayEndCutoffHour 點前，今天沒有未結束的加班時，改接前一天未結束的那筆。
        bool carriedOver = false;
        if ((record?.OvertimeStartTime is null || record.OvertimeEndTime.HasValue)
            && now.Hour < OvertimeSettlementService.CrossDayEndCutoffHour)
        {
            var yesterday = today.AddDays(-1);
            var carried = await db.AttendanceRecords.FirstOrDefaultAsync(a =>
                a.UserId == userId && a.RecordDate == yesterday
                && a.OvertimeStartTime != null && a.OvertimeEndTime == null);
            if (carried is not null) { record = carried; carriedOver = true; }
        }

        if (record is null || !record.OvertimeStartTime.HasValue)
            throw AppException.BadRequest("請先打加班開始卡。");
        if (record.OvertimeEndTime.HasValue)
            throw AppException.BadRequest("今日已打加班結束卡。");

        record.OvertimeEndTime      = now;
        record.OvertimeEndLatitude   = body.Latitude;
        record.OvertimeEndLongitude  = body.Longitude;
        // 跨日補打（carriedOver）不動前一天的出差旗標 —— 那是前一天的整日屬性。
        if (!carriedOver)
            record.IsBusinessTrip = body.IsBusinessTrip;

        // 加班給付依實際打卡結算（2026-10 防灌工時）：min(核准時數, 開始～結束)。
        // 舊單（SettledHours 為 null）與未核准的單不處理，見 OvertimeSettlementService。
        if (record.OvertimeRequestId is { } otId)
        {
            var ot = await db.OvertimeRequests.FirstOrDefaultAsync(o => o.Id == otId);
            if (ot is not null)
                await OvertimeSettlementService.SyncFromPunchesAsync(
                    db, shiftReader, workdaySchedule, ot, record.OvertimeStartTime, record.OvertimeEndTime);
        }

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

        // 禁止修改自己的出缺勤（2026-10 防灌工時）：能改別人的卡不等於能改自己的卡 ——
        // 否則持有 reports-attendance:write 的人可以自己補卡、自己灌工時。Superadmin 無打卡紀錄，不受限。
        var callerId = await GetUserIdAsync(req);
        if (record.UserId == callerId && !IsSuperAdmin(req))
            throw AppException.Forbidden("不可修改自己的出缺勤紀錄，請洽主管或人事處理。");

        // 時間先後的基本完整性（過去完全沒驗證，可存出「下班早於上班」「加班結束早於開始」）
        if (body.ClockInTime is { } ci && body.ClockOutTime is { } co && co <= ci)
            throw AppException.BadRequest("下班時間必須晚於上班時間。");
        if (body.OvertimeStartTime is { } os && body.OvertimeEndTime is { } oe && oe <= os)
            throw AppException.BadRequest("加班結束時間必須晚於加班開始時間。");

        // 合理性（V21）：新值必須落在該筆 RecordDate 當天 00:00 ～ 隔天清晨（跨日加班，與加班結束卡同一個 cutoff）。
        // 只檢查「有被改動」的欄位，歷史上既有的異常值不會讓不相干的備註修改存不了。
        var windowStart = record.RecordDate.Date;
        var windowEnd   = windowStart.AddDays(1).AddHours(OvertimeSettlementService.CrossDayEndCutoffHour);
        void EnsureInWindow(DateTime? next, DateTime? current, string label)
        {
            if (next is { } v && v != current && (v < windowStart || v >= windowEnd))
                throw AppException.BadRequest(
                    $"{label}須落在紀錄日期 {windowStart:yyyy/MM/dd} 當天至隔日 {OvertimeSettlementService.CrossDayEndCutoffHour:00}:00 之前。");
        }
        EnsureInWindow(body.ClockInTime,       record.ClockInTime,       "上班時間");
        EnsureInWindow(body.ClockOutTime,      record.ClockOutTime,      "下班時間");
        EnsureInWindow(body.OvertimeStartTime, record.OvertimeStartTime, "加班開始時間");
        EnsureInWindow(body.OvertimeEndTime,   record.OvertimeEndTime,   "加班結束時間");

        // 層級（V21）：修改者職級須嚴格高於被修改者（JobTitle.Level 數字越小越高）；同級或更低一律 403。
        // 被修改者無職稱視為最低層級；修改者無職稱則無從比較，拒絕。Superadmin 例外。
        if (!IsSuperAdmin(req))
        {
            var callerLevel = await db.Users.AsNoTracking()
                .Where(u => u.Id == callerId).Select(u => (int?)u.JobTitle!.Level).FirstOrDefaultAsync();
            var ownerLevel = await db.Users.AsNoTracking()
                .Where(u => u.Id == record.UserId).Select(u => (int?)u.JobTitle!.Level).FirstOrDefaultAsync();
            if (callerLevel is null || (ownerLevel is not null && callerLevel.Value >= ownerLevel.Value))
                throw AppException.Forbidden("僅能修改職級低於自己的員工出缺勤紀錄。");
        }

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

        var newRemark = string.IsNullOrWhiteSpace(body.Remark) ? null : body.Remark.Trim();

        bool changed = record.ClockInTime       != body.ClockInTime
                    || record.ClockOutTime      != body.ClockOutTime
                    || record.OvertimeStartTime != body.OvertimeStartTime
                    || record.OvertimeEndTime   != body.OvertimeEndTime
                    || record.Remark            != newRemark;
        bool overtimeChanged = record.OvertimeStartTime != body.OvertimeStartTime
                            || record.OvertimeEndTime   != body.OvertimeEndTime;
        bool clockChanged    = record.ClockInTime  != body.ClockInTime
                            || record.ClockOutTime != body.ClockOutTime;

        if (changed)
        {
            // 異動紀錄（2026-10）：修改前後的各欄位值 + 修改人快照，每次實質修改寫一列。
            // 值沒變的重複儲存不寫（否則稽核紀錄全是噪音）。
            var modifierName = await db.Users.AsNoTracking()
                .Where(u => u.Id == callerId).Select(u => u.Name).FirstOrDefaultAsync() ?? "—";
            var now = Clock.Now;
            AttendanceAuditWriter.Add(db, record, callerId, modifierName, now,
                body.ClockInTime, body.ClockOutTime, body.OvertimeStartTime, body.OvertimeEndTime, newRemark);

            // 「系統補卡」旗標**不再清除**：IsClockInAuto / IsClockOutAuto 記的是「這個值最初怎麼來的」，
            // 管理者改過另以 IsManuallyAdjusted 標示，報表兩個 badge 可並存（原本是系統補的、後來被誰改過）。
            record.IsManuallyAdjusted = true;
            record.LastAdjustedById   = callerId;
            record.LastAdjustedAt     = now;
        }

        record.ClockInTime        = body.ClockInTime;
        record.ClockOutTime       = body.ClockOutTime;
        record.OvertimeStartTime  = body.OvertimeStartTime;
        record.OvertimeEndTime    = body.OvertimeEndTime;
        record.Remark             = newRemark;
        // IsBusinessTrip 刻意不在此異動：出差僅由本人打卡時勾選

        // 上下班時間被改動 → 依「該筆紀錄自己的日期」重算遲到／早退（2026-10-06），
        // 否則報表的 badge 仍是改之前的結果。切換日前的紀錄 ctx.Flexible = false，不動旗標。
        if (clockChanged)
        {
            var ctx = await ResolveClockContextAsync(record.UserId, record.RecordDate);
            if (ctx.Flexible)
            {
                ClockOutKind? kind = record.ClockInTime is { } inAt && record.ClockOutTime is { } outAt
                    ? ClockRules.ResolveClockOutKind(inAt, outAt, ctx.Schedule, ctx.AfternoonOnly, ctx.Profile)
                    : null;
                RefreshExceptionFlags(record, ctx, kind);
                if (kind is null) record.IsEarlyLeave = false;
            }
        }

        // 加班起訖被改動 → 重算該日加班單的結算時數（同一次 SaveChanges，冪等）。
        // 這是「補登期限外」或「忘了打加班結束卡」的唯一補正路徑：沒有結算時數就沒有給付。
        if (overtimeChanged)
        {
            var ot = await OvertimeSettlementService.FindForRecordAsync(db, record);
            if (ot is not null)
            {
                record.OvertimeRequestId ??= ot.Id;   // 以日期比對找到的單順手綁定，之後的重算就不必再猜
                await OvertimeSettlementService.SyncFromPunchesAsync(
                    db, shiftReader, workdaySchedule, ot, record.OvertimeStartTime, record.OvertimeEndTime);
            }
        }

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "出缺勤紀錄更新成功。"));
    }

    // ── Helper ──────────────────────────────────────────────────────────────────

    /// <summary>Superadmin 判定（比照 ApprovalTaskHandler：is_superadmin claim）。principal 由 AppRouter 寫入 HttpContext.User。</summary>
    private static bool IsSuperAdmin(HttpRequest req) =>
        string.Equals(req.HttpContext.User.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase);

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
        WorkdaySchedule Schedule, bool AfternoonOnly, ClockProfile Profile);

    /// <summary>
    /// 重算遲到／早退旗標。出差（`IsBusinessTrip`）只是出缺勤紀錄上的標記，**不免除**遲到／早退 ——
    /// 2026-10-12 起勾出差不再要求已核准出差單，若仍免責等於本人自宣告即可抹掉遲到紀錄。
    /// </summary>
    private static void RefreshExceptionFlags(
        AttendanceRecord record, ClockContext ctx, ClockOutKind? clockOutKind = null)
    {
        if (!ctx.Flexible) return;

        record.IsLate = record.ClockInTime is { } ci
                     && !record.IsClockInAuto            // 系統補卡不是本人遲到
                     && ClockRules.IsLate(ci, ctx.Schedule, ctx.AfternoonOnly, ctx.Profile);

        if (clockOutKind is { } kind)
            record.IsEarlyLeave = kind == ClockOutKind.Early;
    }

    /// <summary>打卡原因（遲到／早退／逾時）正規化：空白存 null、上限 500 字。</summary>
    private static string? TrimReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return null;
        var t = reason.Trim();
        return t[..Math.Min(t.Length, RemarkMaxLength)];
    }

    private async Task<ClockContext> ResolveClockContextAsync(Guid userId, DateTime now)
    {
        var switchDate = await workdaySchedule.GetSwitchDateAsync();
        var schedule   = WorkdayHours.For(now.Date, switchDate);
        bool flexible  = switchDate is { } sd && now.Date >= sd.Date;

        if (!flexible)
            return new ClockContext(false, WorkDayTypes.Work, false, schedule, false, ClockProfile.Company);

        var dayType    = await shiftReader.ResolveDayTypeAsync(userId, now.Date);
        var isAssignee = await shiftReader.IsActivityAssigneeAsync(userId, now.Date);

        // 「只上下午」＝當日有一段假從上班時刻起、在半天分界（13:00）前結束
        var leaves = await reader.GetLeavesOnDateAsync(userId, DateOnly.FromDateTime(now));
        var dayStart = now.Date.Add(schedule.Start.ToTimeSpan());
        var boundary = now.Date.Add(schedule.HalfDayPmStart.ToTimeSpan());
        bool afternoonOnly = leaves.Any(l => l.StartDate <= dayStart && l.EndDate > dayStart && l.EndDate <= boundary);

        // 自訂上下班時段（賣店等）只在新制生效，舊制分支一律走公司預設
        var profile = await workPattern.GetClockProfileAsync(userId);

        return new ClockContext(true, dayType, isAssignee, schedule, afternoonOnly, profile);
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
    /// 一般上班日「加班開始卡」最早可打時間 ＝ max(應下班時間〔ExpectedWorkWindow，已避開請假時段〕, 實際下班打卡)。
    /// OvertimeStartAsync 的擋件與 GET /attendances/today 的 CanOvertimeStartNow 共用此函式，避免兩處規則漂移。
    /// 休假日 / 全日請假（exempt）不適用，呼叫端自行略過。
    /// </summary>
    private static DateTime ComputeOvertimeNotBefore(
        DateTime today, DateTime? clockOutAt, IReadOnlyList<ActiveLeaveDto> todayLeaves, ClockContext otCtx)
    {
        var window    = ExpectedWorkWindow.Compute(today, ToLeaveDays(today, todayLeaves), otCtx.Schedule);
        var notBefore = window.End ?? today.Add(otCtx.Schedule.End.ToTimeSpan());
        if (clockOutAt is { } outAt && outAt > notBefore) notBefore = outAt;
        return notBefore;
    }

    /// <summary>
    /// 上班卡最晚可打的時間：當日有已核准請假時 ＝ 應出勤時段訖點（ExpectedWorkWindow，已避開請假）；
    /// 整天被請假蓋滿則回當日 00:00（今日不開放）。無請假回 null —— 不設限，維持既有行為。
    /// ClockInAsync 的擋件與 GET /attendances/today 的 ClockInNotAfter 共用此函式。
    /// </summary>
    private static DateTime? ComputeClockInNotAfter(
        DateTime today, IReadOnlyList<ActiveLeaveDto> todayLeaves, WorkdaySchedule schedule)
    {
        if (todayLeaves.Count == 0) return null;
        var window = ExpectedWorkWindow.Compute(today, ToLeaveDays(today, todayLeaves), schedule);
        return window.End ?? today;
    }

    /// <summary>當日已核准請假 → ExpectedWorkWindow 吃的逐日時段（跨日假單裁到當日 00:00 / 23:59:59）</summary>
    private static List<LeaveDay> ToLeaveDays(DateTime today, IReadOnlyList<ActiveLeaveDto> todayLeaves) =>
        todayLeaves.Select(l => new LeaveDay(
            today, 0m, string.Empty,
            l.StartDate.Date < today ? TimeOnly.MinValue : TimeOnly.FromDateTime(l.StartDate),
            l.EndDate.Date   > today ? TimeOnly.MaxValue : TimeOnly.FromDateTime(l.EndDate))).ToList();

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

        // 跨日加班：隔天凌晨（CrossDayEndCutoffHour 前）今天沒有未結束的加班時，改顯示前一天未結束的那筆，
        // 打卡頁才會讓「加班結束」按鈕可按（前端只看 overtimeStartTime / overtimeEndTime）。
        if (record?.OvertimeStartTime is null && now.Hour < OvertimeSettlementService.CrossDayEndCutoffHour)
        {
            var carried = await reader.GetOpenOvertimeOnAsync(userId, now.Date.AddDays(-1));
            if (carried is not null) record = carried;
        }

        var leaves = await reader.GetLeavesOnDateAsync(userId, today);
        var exempt = await CanOvertimeWithoutClockOutAsync(userId, now.Date, leaves);

        // 四週彈性工時的旗標：前端不自行重組日別規則，只吃這裡算好的結果
        var ctx = await ResolveClockContextAsync(userId, now);
        bool canClock = !ctx.Flexible || ClockDayPolicy.AllowsClockInOut(ctx.DayType, ctx.IsActivityAssignee);
        string? lockReason = ctx.Flexible
            ? ClockDayPolicy.ClockInOutLockReason(ctx.DayType, ctx.IsActivityAssignee)
            : null;
        DateTime? expectedOut = ctx.Flexible && record?.ClockInTime is { } ci
            ? ClockRules.ExpectedClockOut(ci, ctx.Schedule, ctx.AfternoonOnly, ctx.Profile)
            : null;
        // 正常下班帶的終點（公司預設 T+30 分、自訂時段 E+5 分），前端據此判斷逾時、不再寫死 30 分
        DateTime? normalOutUntil = expectedOut?.AddMinutes(ctx.Profile.GraceMinutes);
        // 遲到界線：前端據此決定上班打卡是否先開原因對話框（與 ClockInAsync 擋件同一函式）
        DateTime? lateAfter = ctx.Flexible
            ? now.Date.Add(ClockRules.LateThreshold(ctx.Schedule, ctx.AfternoonOnly, ctx.Profile).ToTimeSpan())
            : null;

        // 加班開始卡的時間規則（與 OvertimeStartAsync 同一函式）：未到最早可打時間 → 前端據此停用按鈕
        DateTime? overtimeNotBefore = null;
        bool canStartOvertimeNow    = true;
        if (!exempt)
        {
            var clockOutToday = record is { } r && r.RecordDate.Date == now.Date ? r.ClockOutTime : null;
            var nb = ComputeOvertimeNotBefore(now.Date, clockOutToday, leaves, ctx);
            canStartOvertimeNow = now >= nb;
            overtimeNotBefore   = canStartOvertimeNow ? null : nb;
        }

        DateTime? clockInNotAfter = ComputeClockInNotAfter(now.Date, leaves, ctx.Schedule);

        // 專案工時佔比：只帶「今天」的紀錄（跨日加班借用的前一天紀錄不顯示、也不可改）
        bool allocationRequired = allocationSchedule.IsActive(now);
        IReadOnlyList<ProjectAllocationDto> allocations = record is { Id: > 0 } rec && rec.RecordDate.Date == now.Date
            ? await reader.GetProjectAllocationsAsync(rec.Id)
            : [];

        var flexFields = (
            FlexibleEnabled:      ctx.Flexible,
            DayType:              ctx.Flexible ? ctx.DayType : null,
            IsActivityAssignee:   ctx.IsActivityAssignee,
            CanClockInOut:        canClock,
            ClockLockReason:      lockReason,
            ExpectedClockOutTime: expectedOut,
            NormalClockOutUntil:  normalOutUntil,
            LateAfter:            lateAfter);

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
                CanStartOvertimeNow: canStartOvertimeNow,
                OvertimeStartNotBefore: overtimeNotBefore,
                IsBusinessTrip: false,
                FlexibleEnabled:      flexFields.FlexibleEnabled,
                DayType:              flexFields.DayType,
                IsActivityAssignee:   flexFields.IsActivityAssignee,
                CanClockInOut:        flexFields.CanClockInOut,
                ClockLockReason:      flexFields.ClockLockReason,
                ExpectedClockOutTime: flexFields.ExpectedClockOutTime,
                NormalClockOutUntil:  flexFields.NormalClockOutUntil,
                LateAfter:            flexFields.LateAfter,
                ProjectAllocations:   allocations,
                ProjectAllocationRequired: allocationRequired,
                ClockInNotAfter:      clockInNotAfter)
            : record with
            {
                TodayLeaves = leaves,
                CanOvertimeWithoutClockOut = exempt,
                CanStartOvertimeNow  = canStartOvertimeNow,
                OvertimeStartNotBefore = overtimeNotBefore,
                FlexibleEnabled      = flexFields.FlexibleEnabled,
                DayType              = flexFields.DayType,
                IsActivityAssignee   = flexFields.IsActivityAssignee,
                CanClockInOut        = flexFields.CanClockInOut,
                ClockLockReason      = flexFields.ClockLockReason,
                ExpectedClockOutTime = flexFields.ExpectedClockOutTime,
                NormalClockOutUntil  = flexFields.NormalClockOutUntil,
                LateAfter            = flexFields.LateAfter,
                ProjectAllocations   = allocations,
                ProjectAllocationRequired = allocationRequired,
                ClockInNotAfter      = clockInNotAfter,
            };
    }
}
