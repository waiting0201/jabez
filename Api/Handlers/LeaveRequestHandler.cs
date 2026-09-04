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
/// GET    /leave-requests                    → 列表
/// POST   /leave-requests                    → 新增（EmployeeId 由 JWT 決定）
/// GET    /leave-requests/compensatory-hours → 可補休時數查詢
/// GET    /leave-requests/annual-quota       → 年假額度查詢
/// GET    /leave-requests/{id}               → 單筆
/// PUT    /leave-requests/{id}               → 更新（僅 draft 才允許）
/// DELETE /leave-requests/{id}               → 刪除（僅 draft 才允許）
/// PATCH  /leave-requests/{id}/submit        → 送出（draft → pending）
/// </summary>
public sealed class LeaveRequestHandler(
    AppDbContext db,
    ILeaveRequestReadService reader,
    IJwtService jwtService,
    IApprovalNotificationService notifier,
    IApprovalFlowService approvalFlow,
    ICalendarDayReadService calendarReader,
    IWorkPatternReadService workPattern)
{
    private static readonly HashSet<string> ValidLeaveTypes =
        ["annual", "personal", "sick", "compensatory", "marriage", "bereavement",
         "official", "maternity", "miscarriage_3m", "miscarriage_2to3m",
         "miscarriage_under2m", "prenatal_checkup", "paternity",
         "ceremonial_festival", "senior_executive", "menstrual", "family_care",
         "parental_leave", "parental_leave_daily"];

    /// <summary>
    /// 工作日型假別：天數 / 時數以「扣除國定假日與六日後的實際工作日」計算
    /// （前端顯示請假日清單、後端 Day 與 Hour 單位皆權威重算）。
    /// 除歲時祭儀假（依法為連續日曆天）外皆適用；產假區間仍為起始日 +55 天，但只計其中工作日。
    /// 前端 WORKING_DAY_LEAVE_TYPES 須與此保持同步。
    /// </summary>
    private static readonly HashSet<string> WorkingDayLeaveTypes = LeaveDayExpander.WorkingDayLeaveTypes;

    /// <summary>高階主管假可申請之最高職級（JobTitle.Level 數字越小層級越高）</summary>
    private const int SeniorExecMaxLevel = 3;

    /// <summary>高階主管假每年額度（天）：協理以上每年 24 天，曆年未用完歸零、隔年重新給予</summary>
    private const int SeniorExecutiveAnnualDays = 24;

    /// <summary>
    /// 期初補休時數（User.CompensatoryOpeningHours）到期日：系統上線前累計的補休須於此日前休完，
    /// 未休完即歸零作廢；此後系統內加班核准產生的補休不受此限制。全員一致故採固定常數。
    /// </summary>
    private static readonly DateTime CompensatoryOpeningExpiry = new(2027, 6, 30, 23, 59, 59);

    /// <summary>產假固定天數（法規為一次請完）</summary>
    private const int MaternityDays = 56;

    // ── 育嬰留職停薪常數 ──────────────────────────────────────────────────────
    // 法源：性別平等工作法 §16、育嬰留職停薪實施辦法。津貼為勞保局按投保薪資 80% 給付，
    // 非公司給付，故系統不計算津貼金額，只處理「不支薪」與「年資不計入」。

    /// <summary>育嬰留停申請資格：在職滿 6 個月（Superadmin 可繞過，對應「未滿 6 個月經雇主同意亦可」）</summary>
    private const int ParentalMinSeniorityMonths = 6;

    /// <summary>育嬰留停申請資格：子女未滿 3 歲</summary>
    private const int ParentalChildMaxAgeYears = 3;

    /// <summary>育嬰留停每名子女合計上限（天）：2 年。兩種育嬰假別併計，以 ChildBirthDate 為分組鍵</summary>
    private const int ParentalTotalDays = 730;

    /// <summary>彈性單日育嬰留停每人每年上限（日）。雙親合計 60 日無法驗證，僅前端提示</summary>
    private const int ParentalDailyAnnualDays = 30;

    /// <summary>兩種育嬰留停假別（額度併計、薪資按比例、年資扣除皆一體適用）</summary>
    private static readonly HashSet<string> ParentalLeaveTypes = ["parental_leave", "parental_leave_daily"];

    /// <summary>各假別天數上限（不含年假與補休，它們有獨立邏輯）</summary>
    private static readonly Dictionary<string, int> LeaveTypeDaysLimit = new()
    {
        ["marriage"]            = 8,
        ["maternity"]           = 56,
        ["miscarriage_3m"]      = 28,
        ["miscarriage_2to3m"]   = 7,
        ["miscarriage_under2m"] = 5,
        ["prenatal_checkup"]    = 7,
        ["paternity"]           = 7,
        // 家庭照顧假：性別平等工作法 §20，全年 7 日（56 小時）為限，併入年度事假計算
        ["family_care"]         = 7,
    };

    /// <summary>取得指定假別的時間單位（單一真相在 LeaveDayExpander，與逐日展開共用）</summary>
    private static LeaveTimeUnit GetTimeUnit(string leaveType) => LeaveDayExpander.GetTimeUnit(leaveType);

    /// <summary>時間單位轉字串（前端使用）</summary>
    private static string TimeUnitToString(LeaveTimeUnit unit) => LeaveDayExpander.TimeUnitToString(unit);

    /// <summary>喪假親屬關係對應天數上限</summary>
    private static readonly Dictionary<string, int> BereavementDaysLimit = new()
    {
        ["spouse"]                 = 8,
        ["parent"]                 = 8,
        ["adoptive_parent"]        = 8,
        ["step_parent"]            = 8,
        ["grandparent"]            = 6,
        ["child"]                  = 6,
        ["spouse_parent"]          = 6,
        ["spouse_adoptive_parent"] = 6,
        ["great_grandparent"]      = 3,
        ["sibling"]                = 3,
        ["spouse_grandparent"]     = 3,
    };

    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        Guid? filterUserId = user?.IsSuperAdmin == true ? null : userId;
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var result = await reader.GetPagedAsync(page, pageSize, filterUserId);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var exists = user?.IsSuperAdmin == true
            ? await db.LeaveRequests.AnyAsync(x => x.Id == intId)
            : await db.LeaveRequests.AnyAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (!exists)
            return new NotFoundObjectResult(ApiResponse.Fail("Leave request not found."));

        var item = await reader.GetByIdAsync(intId);
        return new OkObjectResult(ApiResponse.Ok(item));
    }

    /// <summary>
    /// 依起迄日回傳「扣除國定假日與六日後的實際請假日清單與天數」（供表單即時顯示）。
    /// 工作日型假別才扣假日；連續日曆天假別（目前僅歲時祭儀假）回整段日曆天。任何登入者可呼叫（免 calendar-days:read）。
    /// GET /leave-requests/working-days?start=&amp;end=&amp;leaveType=
    /// </summary>
    public async Task<IActionResult> GetWorkingDaysAsync(HttpRequest req)
    {
        var callerId = await GetUserIdAsync(req); // 僅需登入身分（同時作為排班制旗標的解析對象）

        if (!DateTime.TryParse(req.Query["start"], out var start) ||
            !DateTime.TryParse(req.Query["end"], out var end))
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的 start / end 日期。"));
        if (end.Date < start.Date)
            return new BadRequestObjectResult(ApiResponse.Fail("結束日不得早於開始日。"));
        if ((end.Date - start.Date).Days > 366)
            return new BadRequestObjectResult(ApiResponse.Fail("日期區間過長。"));

        var leaveType = req.Query["leaveType"].ToString();

        // 非工作日型假別（連續日曆天，目前僅歲時祭儀假）→ 不扣假日，整段日曆天皆為請假日
        if (!string.IsNullOrEmpty(leaveType) && !WorkingDayLeaveTypes.Contains(leaveType))
        {
            var all = EnumerateDates(start.Date, end.Date).ToList();
            return new OkObjectResult(ApiResponse.Ok(new WorkingDaysDto(true, [], all, all.Count)));
        }

        var (hasData, holidays, working) = await ComputeWorkingDatesAsync(callerId, start, end);
        return new OkObjectResult(ApiResponse.Ok(new WorkingDaysDto(hasData, holidays, working, working.Count)));
    }

    private static IEnumerable<DateTime> EnumerateDates(DateTime start, DateTime end)
        => WorkCalendarHelper.EnumerateDates(start, end);

    /// <summary>
    /// 計算 [start, end] 內的請假日 / 假日清單。
    /// 行事曆有資料 → 以 CalendarDay.IsHoliday（已含六日 + 國定假、補班六為工作日）為準；
    /// 無資料 → 退回以星期六日判定（僅扣六日，國定假需匯入行事曆才會扣）。
    /// 實作已抽至 WorkCalendarHelper（與打卡的休假日判定共用同一份規則）。
    /// </summary>
    private async Task<(bool hasData, List<DateTime> holidays, List<DateTime> working)>
        ComputeWorkingDatesAsync(Guid ownerId, DateTime start, DateTime end)
        => await WorkCalendarHelper.ComputeWorkingDatesAsync(
            calendarReader, await workPattern.IsShiftWorkerAsync(ownerId), start, end);

    /// <summary>
    /// HalfDay 時數在 body.Hours 未帶時的退路：以 LeaveDayExpander 同一套「起 &lt; 13:00 ＝上午、
    /// 訖 &gt; 13:00 ＝下午」時段分類推時數，不用 End − Start 時間差 —— 補休的上午時段為 09:00–13:00
    /// （見前端 halfDayAmStartHour / halfDayAmEndHour），時間差會算成 4 小時但界線若用 12:00
    /// 會把訖 13:00 判成下午而算出全日 8 小時。
    /// 跨日半天假一律由 client 帶 Hours（時間差對跨日本來就沒有意義），此處僅處理同日。
    /// </summary>
    private static decimal ComputeHalfDaySlotHours(DateTime start, DateTime end)
    {
        if (start.Date != end.Date) return (decimal)(end - start).TotalHours;

        bool startIsAm = start.Hour < WorkdayHours.LunchEndHour;  // 08:00 / 09:00 → am、13:00 → pm
        bool endIsPm   = end.Hour   > WorkdayHours.LunchEndHour;  // 17:00 → pm、12:00 / 13:00 → am
        return (startIsAm, endIsPm) switch
        {
            (true,  false) => 4m,   // am → am
            (true,  true)  => 8m,   // am → pm（全日）
            (false, true)  => 4m,   // pm → pm
            _              => 0m,   // pm → am：單日無效
        };
    }

    /// <summary>
    /// Hour 單位假別（事假 / 病假 / 產檢假 / 陪產假）的時數計算：逐日累加，只算工作日。
    /// - 同日：維持 end.Hour − start.Hour（不扣午休，沿用既有單日語意）；當日為假日 → 0
    /// - 跨日：首個工作日 Clamp(17 − start.Hour, 0, 8)、中間工作日各 8 小時、末個工作日 Clamp(end.Hour − 8, 0, 8)；
    ///   落在假日的日期一律 0，且不把時段挪到相鄰工作日
    /// </summary>
    private async Task<(bool hasData, decimal hours)> ComputeHourUnitHoursAsync(Guid ownerId, DateTime start, DateTime end)
    {
        var (hasData, _, working) = await ComputeWorkingDatesAsync(ownerId, start, end);
        var workingSet = working.Select(d => d.Date).ToHashSet();

        if (start.Date == end.Date)
        {
            var sameDayHours = workingSet.Contains(start.Date) ? end.Hour - start.Hour : 0;
            return (hasData, Math.Max(0, sameDayHours));
        }

        decimal total = 0m;
        foreach (var d in workingSet)
        {
            if (d == start.Date)
                total += Math.Clamp(WorkdayHours.EndHour - start.Hour, 0, 8);
            else if (d == end.Date)
                total += Math.Clamp(end.Hour - WorkdayHours.StartHour, 0, 8);
            else
                total += 8m;
        }
        return (hasData, total);
    }

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        // BUG-04: EmployeeId 由 JWT 中的 sub claim 決定，不信任客戶端傳入的值
        var employeeId = await GetUserIdAsync(req);

        var body = await req.ReadFromJsonAsync<CreateLeaveRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (string.IsNullOrWhiteSpace(body.LeaveType))
            return new BadRequestObjectResult(ApiResponse.Fail("LeaveType is required."));

        if (!ValidLeaveTypes.Contains(body.LeaveType))
            return new BadRequestObjectResult(ApiResponse.Fail(
                $"Invalid LeaveType '{body.LeaveType}'. Must be one of: {string.Join(", ", ValidLeaveTypes)}"));

        // 喪假必須提供親屬關係
        if (body.LeaveType == "bereavement")
        {
            if (string.IsNullOrWhiteSpace(body.BereavementRelationship))
                return new BadRequestObjectResult(ApiResponse.Fail("喪假必須選擇親屬關係。"));
            if (!BereavementDaysLimit.ContainsKey(body.BereavementRelationship))
                return new BadRequestObjectResult(ApiResponse.Fail("無效的親屬關係。"));
        }

        // 高階主管假：職級檢查（JobTitle.Level <= 3）
        if (body.LeaveType == "senior_executive")
        {
            var eligError = await CheckSeniorExecutiveEligibilityAsync(employeeId);
            if (eligError is not null)
                return new BadRequestObjectResult(ApiResponse.Fail(eligError));
        }

        // 歲時祭儀假：限原住民身份（前置檢查；ValidateLeaveQuotaAsync 會在 submit 時再次驗證）
        if (body.LeaveType == "ceremonial_festival")
        {
            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == employeeId);
            if (applicant?.IsIndigenous != true)
                return new BadRequestObjectResult(ApiResponse.Fail("僅原住民身份之員工可申請歲時祭儀假。"));
        }

        // 生理假：限女性身份（前置檢查；ValidateLeaveQuotaAsync 會在 submit 時再次驗證）
        if (body.LeaveType == "menstrual" && !await IsFemaleAsync(employeeId))
            return new BadRequestObjectResult(ApiResponse.Fail("僅女性員工可申請生理假。"));

        // 育嬰留停：在職滿 6 個月 + 子女未滿 3 歲（前置檢查；submit 時再次驗證）
        if (ParentalLeaveTypes.Contains(body.LeaveType))
        {
            // 單日型的 body.EndDate 稍後會被強制覆寫為起始日，故此處以起始日檢核
            var parentalEnd = body.LeaveType == "parental_leave_daily" ? body.StartDate : body.EndDate;
            var parentalError = await CheckParentalEligibilityAsync(employeeId, body.ChildBirthDate, body.StartDate, parentalEnd);
            if (parentalError is not null)
                return new BadRequestObjectResult(ApiResponse.Fail(parentalError));
        }

        // 產假：禁止重複活躍申請（一次請完制）
        if (body.LeaveType == "maternity")
        {
            var hasActive = await db.LeaveRequests.AnyAsync(l =>
                l.EmployeeId == employeeId
                && l.LeaveType == "maternity"
                && (l.ApprovalStatus == "pending" || l.ApprovalStatus == "approved"));
            if (hasActive)
                return new BadRequestObjectResult(ApiResponse.Fail("已有未完成或進行中的產假申請，產假需一次請完。"));
        }

        if (body.StartDate == default)
            return new BadRequestObjectResult(ApiResponse.Fail("StartDate is required."));

        // 產假自動填充 EndDate 與 Hours（56 天）
        DateTime effectiveStart = body.StartDate;
        DateTime effectiveEnd   = body.EndDate;
        if (body.LeaveType == "maternity")
        {
            effectiveStart = body.StartDate.Date;
            effectiveEnd   = effectiveStart.AddDays(MaternityDays - 1);
        }
        else if (body.LeaveType == "parental_leave_daily")
        {
            // 彈性單日新制：一次申請一日，強制結束日＝起始日（徹底消除跨月歸屬問題）。
            // 結束時間必須是當日 23:59 —— 重疊驗證與打卡阻擋皆為半開區間 [Start, End)，
            // 若存成 00:00 會變成零長度區間，同一天可重複申請、也擋不住當日打卡。
            effectiveStart = body.StartDate.Date;
            effectiveEnd   = EndOfDay(effectiveStart);
        }
        else if (body.LeaveType == "parental_leave")
        {
            // 長期留停為連續日曆天：起始日正規化為 00:00、結束日補滿到 23:59（理由同上）
            if (body.EndDate == default)
                return new BadRequestObjectResult(ApiResponse.Fail("EndDate is required."));
            effectiveStart = body.StartDate.Date;
            effectiveEnd   = EndOfDay(body.EndDate);
            if (effectiveEnd <= effectiveStart)
                return new BadRequestObjectResult(ApiResponse.Fail("EndDate must be after StartDate."));
        }
        else
        {
            if (body.EndDate == default)
                return new BadRequestObjectResult(ApiResponse.Fail("EndDate is required."));
            if (body.EndDate <= body.StartDate)
                return new BadRequestObjectResult(ApiResponse.Fail("EndDate must be after StartDate."));
        }

        var unit = GetTimeUnit(body.LeaveType);

        // 分鐘必須為 0（僅整點，Hour 單位）
        if (unit == LeaveTimeUnit.Hour)
        {
            if (effectiveStart.Minute != 0)
                return new BadRequestObjectResult(ApiResponse.Fail("StartDate 必須為整點（分鐘 00）。"));
            if (effectiveEnd.Minute != 0)
                return new BadRequestObjectResult(ApiResponse.Fail("EndDate 必須為整點（分鐘 00）。"));
        }

        // 時數計算：Hour → 逐日累加只算工作日；HalfDay → 信任 client；Day → 工作日數 × 8。
        // 工作日型假別（除歲時祭儀假外皆是）扣除國定假日與六日；產假區間仍為起始日 +55 天，只計其中工作日。
        // 草稿階段行事曆若尚無資料 → 退回原始算式（送出時會強制要求行事曆並權威重算）。
        bool isWorkingDayType = WorkingDayLeaveTypes.Contains(body.LeaveType);
        decimal hours;
        if (isWorkingDayType && unit == LeaveTimeUnit.Hour)
        {
            (_, hours) = await ComputeHourUnitHoursAsync(employeeId, effectiveStart, effectiveEnd);
        }
        else if (isWorkingDayType && unit == LeaveTimeUnit.Day)
        {
            var (_, _, working) = await ComputeWorkingDatesAsync(employeeId, effectiveStart, effectiveEnd);
            hours = working.Count * 8m;
        }
        else
        {
            hours = unit switch
            {
                LeaveTimeUnit.Hour    => (decimal)(effectiveEnd - effectiveStart).TotalHours,
                LeaveTimeUnit.HalfDay => body.Hours,
                LeaveTimeUnit.Day     => ((effectiveEnd.Date - effectiveStart.Date).Days + 1) * 8m,
                _                     => (decimal)(effectiveEnd - effectiveStart).TotalHours,
            };
        }

        if (hours <= 0 && isWorkingDayType && unit != LeaveTimeUnit.HalfDay)
            return new BadRequestObjectResult(ApiResponse.Fail("此區間全為國定假日或六日，無可請假的工作日。"));
        if (hours <= 0)
            return new BadRequestObjectResult(ApiResponse.Fail("時數必須大於 0。"));
        if (unit == LeaveTimeUnit.HalfDay && hours % 4m != 0m)
            return new BadRequestObjectResult(ApiResponse.Fail("特休／補休／高階主管假需以半天（4 小時）為單位。"));
        if (unit == LeaveTimeUnit.Day && hours % 8m != 0m)
            return new BadRequestObjectResult(ApiResponse.Fail("此假別需以整天（8 小時）為單位。"));
        if (unit == LeaveTimeUnit.Hour && hours % 1m != 0m)
            return new BadRequestObjectResult(ApiResponse.Fail("小時單位必須為整數（整點計時）。"));

        // 日期重疊檢查：擋下與既有 draft / pending / approved 申請時間區間相交者
        var overlapErr = await CheckOverlapAsync(employeeId, effectiveStart, effectiveEnd, body.LeaveType, excludeId: null);
        if (overlapErr is not null)
            return new BadRequestObjectResult(ApiResponse.Fail(overlapErr));

        // 指定審核者存在性驗證
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            var reviewerIds = body.DesignatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
            var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
            if (existCount != reviewerIds.Count)
                return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
        }

        var item = new LeaveRequest
        {
            EmployeeId              = employeeId,   // 強制使用 JWT 身分，忽略 body.EmployeeId
            ApprovalItemId          = body.ApprovalItemId,
            LeaveType               = body.LeaveType,
            StartDate               = effectiveStart,
            EndDate                 = effectiveEnd,
            Hours                   = hours,
            Reason                  = body.Reason,
            BereavementRelationship = body.LeaveType == "bereavement" ? body.BereavementRelationship : null,
            ChildBirthDate          = ParentalLeaveTypes.Contains(body.LeaveType) ? body.ChildBirthDate?.Date : null,
            ContinueInsurance       = ParentalLeaveTypes.Contains(body.LeaveType) ? body.ContinueInsurance : null,
            AgentUserId             = body.AgentUserId == employeeId ? null : body.AgentUserId,  // 代理人不可為本人
            ApprovalStatus          = "draft",
            CreatedAt               = Clock.Now,
        };
        db.LeaveRequests.Add(item);
        await db.SaveChangesAsync();

        // 儲存指定審核者
        if (body.DesignatedReviewers is { Length: > 0 })
        {
            db.RequestDesignatedReviewers.AddRange(
                DesignatedReviewerHelper.BuildEntities("leave", item.Id, body.DesignatedReviewers));
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(item.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Leave request created.")) { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var body = await req.ReadFromJsonAsync<UpdateLeaveRequestRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("LeaveRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned leave requests can be edited.");

        // 指定審核者整組替換（提供 DesignatedReviewers 時才更新）
        if (body.DesignatedReviewers is not null)
        {
            if (body.DesignatedReviewers.Length > 0)
            {
                var reviewerIds = body.DesignatedReviewers.Select(r => r.ReviewerId).Distinct().ToList();
                var existCount = await db.Users.AsNoTracking().CountAsync(u => reviewerIds.Contains(u.Id));
                if (existCount != reviewerIds.Count)
                    return new BadRequestObjectResult(ApiResponse.Fail("一或多位指定審核者不存在。"));
            }
            var old = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "leave" && r.RequestId == intId)
                .ToListAsync();
            db.RequestDesignatedReviewers.RemoveRange(old);
            if (body.DesignatedReviewers.Length > 0)
            {
                db.RequestDesignatedReviewers.AddRange(
                    DesignatedReviewerHelper.BuildEntities("leave", intId, body.DesignatedReviewers));
            }
        }

        if (body.LeaveType is not null)
        {
            if (!ValidLeaveTypes.Contains(body.LeaveType))
                return new BadRequestObjectResult(ApiResponse.Fail(
                    $"Invalid LeaveType '{body.LeaveType}'."));
            item.LeaveType = body.LeaveType;
        }
        if (body.StartDate.HasValue) item.StartDate = body.StartDate.Value;
        if (body.EndDate.HasValue)   item.EndDate   = body.EndDate.Value;
        if (body.Reason is not null) item.Reason    = body.Reason;

        // 職務代理人更新（表單一律帶完整值；不可為本人）
        item.AgentUserId = body.AgentUserId == item.EmployeeId ? null : body.AgentUserId;

        // 喪假親屬關係更新
        var effectiveLeaveType = item.LeaveType;
        if (effectiveLeaveType == "bereavement")
        {
            if (body.BereavementRelationship is not null)
                item.BereavementRelationship = body.BereavementRelationship;
        }
        else
        {
            item.BereavementRelationship = null;
        }

        // 高階主管假：職級檢查
        if (effectiveLeaveType == "senior_executive")
        {
            var eligError = await CheckSeniorExecutiveEligibilityAsync(item.EmployeeId ?? Guid.Empty);
            if (eligError is not null)
                return new BadRequestObjectResult(ApiResponse.Fail(eligError));
        }

        // 歲時祭儀假：限原住民身份（與 CreateAsync 保持一致）
        if (effectiveLeaveType == "ceremonial_festival")
        {
            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == item.EmployeeId);
            if (applicant?.IsIndigenous != true)
                return new BadRequestObjectResult(ApiResponse.Fail("僅原住民身份之員工可申請歲時祭儀假。"));
        }

        // 生理假：限女性身份（與 CreateAsync 保持一致）
        if (effectiveLeaveType == "menstrual" && !await IsFemaleAsync(item.EmployeeId ?? Guid.Empty))
            return new BadRequestObjectResult(ApiResponse.Fail("僅女性員工可申請生理假。"));

        // 育嬰留停欄位更新 + 資格檢查（與 CreateAsync 保持一致）；改為其他假別時一併清空
        if (ParentalLeaveTypes.Contains(effectiveLeaveType))
        {
            if (body.ChildBirthDate.HasValue) item.ChildBirthDate = body.ChildBirthDate.Value.Date;
            item.ContinueInsurance = body.ContinueInsurance;

            var parentalError = await CheckParentalEligibilityAsync(
                item.EmployeeId ?? Guid.Empty, item.ChildBirthDate, item.StartDate, item.EndDate);
            if (parentalError is not null)
                return new BadRequestObjectResult(ApiResponse.Fail(parentalError));
        }
        else
        {
            item.ChildBirthDate    = null;
            item.ContinueInsurance = null;
        }

        var unit = GetTimeUnit(effectiveLeaveType);

        // 育嬰留停日期正規化（與 CreateAsync 保持一致）：起始日 00:00、結束日補滿 23:59。
        // 彈性單日一次申請一日，結束日＝起始日當天 23:59 —— 不可存成 00:00，否則變成零長度區間，
        // 重疊驗證與打卡阻擋的半開區間 [Start, End) 會整個失效；且會被下方通用守門判為
        // EndDate <= StartDate 而恆回 400，導致草稿永遠改不了、送不出。
        if (effectiveLeaveType == "parental_leave_daily")
        {
            item.StartDate = item.StartDate.Date;
            item.EndDate   = EndOfDay(item.StartDate);
        }
        else if (effectiveLeaveType == "parental_leave")
        {
            item.StartDate = item.StartDate.Date;
            item.EndDate   = EndOfDay(item.EndDate);
        }

        // 產假：自動填充 56 個日曆天，不論 client 傳入；時數只計其中工作日
        if (effectiveLeaveType == "maternity")
        {
            item.StartDate = item.StartDate.Date;
            item.EndDate   = item.StartDate.AddDays(MaternityDays - 1);
            var (_, _, maternityWorking) = await ComputeWorkingDatesAsync(item.EmployeeId ?? Guid.Empty, item.StartDate, item.EndDate);
            if (maternityWorking.Count == 0)
                return new BadRequestObjectResult(ApiResponse.Fail("此區間全為國定假日或六日，無可請假的工作日。"));
            item.Hours = maternityWorking.Count * 8m;
        }
        else
        {
            // 分鐘必須為 0（僅整點，Hour 單位）
            if (unit == LeaveTimeUnit.Hour)
            {
                if (item.StartDate.Minute != 0)
                    return new BadRequestObjectResult(ApiResponse.Fail("StartDate 必須為整點（分鐘 00）。"));
                if (item.EndDate.Minute != 0)
                    return new BadRequestObjectResult(ApiResponse.Fail("EndDate 必須為整點（分鐘 00）。"));
            }

            if (item.EndDate <= item.StartDate)
                return new BadRequestObjectResult(ApiResponse.Fail("EndDate must be after StartDate."));

            // 時數依單位計算；工作日型假別扣除國定假日與六日
            // （Hour 逐日累加只算工作日、Day 為工作日數 × 8；草稿階段無行事曆資料則退回原始算式）
            bool isWorkingDayType = WorkingDayLeaveTypes.Contains(effectiveLeaveType);
            decimal recalcHours;
            if (isWorkingDayType && unit == LeaveTimeUnit.Hour)
            {
                (_, recalcHours) = await ComputeHourUnitHoursAsync(item.EmployeeId ?? Guid.Empty, item.StartDate, item.EndDate);
            }
            else if (isWorkingDayType && unit == LeaveTimeUnit.Day)
            {
                var (_, _, working) = await ComputeWorkingDatesAsync(item.EmployeeId ?? Guid.Empty, item.StartDate, item.EndDate);
                recalcHours = working.Count * 8m;
            }
            else
            {
                recalcHours = unit switch
                {
                    LeaveTimeUnit.Hour    => (decimal)(item.EndDate - item.StartDate).TotalHours,
                    LeaveTimeUnit.HalfDay => body.Hours ?? ComputeHalfDaySlotHours(item.StartDate, item.EndDate),
                    LeaveTimeUnit.Day     => ((item.EndDate.Date - item.StartDate.Date).Days + 1) * 8m,
                    _                     => (decimal)(item.EndDate - item.StartDate).TotalHours,
                };
            }
            if (recalcHours <= 0 && isWorkingDayType && unit != LeaveTimeUnit.HalfDay)
                return new BadRequestObjectResult(ApiResponse.Fail("此區間全為國定假日或六日，無可請假的工作日。"));
            if (recalcHours <= 0)
                return new BadRequestObjectResult(ApiResponse.Fail("時數必須大於 0。"));
            if (unit == LeaveTimeUnit.HalfDay && recalcHours % 4m != 0m)
                return new BadRequestObjectResult(ApiResponse.Fail("特休／補休／高階主管假需以半天（4 小時）為單位。"));
            if (unit == LeaveTimeUnit.Day && recalcHours % 8m != 0m)
                return new BadRequestObjectResult(ApiResponse.Fail("此假別需以整天（8 小時）為單位。"));
            if (unit == LeaveTimeUnit.Hour && recalcHours % 1m != 0m)
                return new BadRequestObjectResult(ApiResponse.Fail("小時單位必須為整數（整點計時）。"));
            item.Hours = recalcHours;
        }

        // 日期重疊檢查：排除自身
        var overlapErr = await CheckOverlapAsync(item.EmployeeId!.Value, item.StartDate, item.EndDate, item.LeaveType, excludeId: item.Id);
        if (overlapErr is not null)
            return new BadRequestObjectResult(ApiResponse.Fail(overlapErr));

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(item.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Leave request updated."));
    }

    public async Task<IActionResult> DeleteAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("LeaveRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned leave requests can be deleted.");

        // 一併清除此申請單的審核流程足跡（多型關聯無 FK，須手動刪除，否則殘留列會擋住使用者刪除）
        db.ApprovalRecords.RemoveRange(
            await db.ApprovalRecords.Where(r => r.ApplicationType == "leave" && r.ApplicationId == item.Id).ToListAsync());
        db.EscalationOverrides.RemoveRange(
            await db.EscalationOverrides.Where(o => o.ApplicationType == "leave" && o.ApplicationId == item.Id).ToListAsync());
        db.RequestDesignatedReviewers.RemoveRange(
            await db.RequestDesignatedReviewers.Where(r => r.RequestType == "leave" && r.RequestId == item.Id).ToListAsync());

        db.LeaveRequests.Remove(item);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Leave request '{id}' deleted."));
    }

    /// <summary>補休時數明細（期初匯入 + 系統加班 - 已補休，含期初到期歸零）</summary>
    private readonly record struct CompensatoryBreakdown(
        decimal OpeningHours,      // 期初匯入（系統上線前累計）
        decimal OpeningRemaining,  // 舊補休剩餘（期初未消耗部分；到期後為 0）
        decimal OvertimeHours,     // 系統核准加班可補休時數
        decimal UsedHours,         // 已送出（pending/approved）補休
        decimal AvailableHours,    // 合計可用
        bool    OpeningExpired);   // 期初是否已到期

    /// <summary>
    /// 計算指定使用者的補休時數明細。
    /// FIFO：補休先消耗期初餘額，期初到期後其未用部分作廢，只剩系統加班可補休。
    /// </summary>
    private async Task<CompensatoryBreakdown> ComputeCompensatoryAsync(Guid userId)
    {
        var opening = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CompensatoryOpeningHours)
            .FirstOrDefaultAsync();

        // 系統核准加班時數（07/01 後申請；不到期）
        // ⚠️ 必須濾 CompensationType == "compensatory" —— 選了「加班費」的單已隨次月薪資發放現金，
        //    再進補休池就是同一段工時領兩次（雙重給付）。
        var earned = await db.OvertimeRequests
            .Where(o => o.EmployeeId == userId
                     && o.ApprovalStatus == "approved"
                     && o.CompensationType == OvertimeCompensationService.Compensatory)
            .SumAsync(o => o.EstimatedHours);

        // 已補休時數：已送出（pending / approved）的補休假 Hours 合計
        var used = await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "compensatory"
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
            .SumAsync(l => l.Hours);

        bool expired = Clock.Now > CompensatoryOpeningExpiry;

        // 期初剩餘（未消耗部分）；到期後作廢為 0
        var openingRemaining = expired ? 0m : Math.Max(0m, opening - Math.Min(used, opening));

        // 合計可用：到期前 = 期初 + 加班 - 已用；到期後 = 加班 - 超出期初的已用部分（期初未用作廢）
        var available = expired
            ? earned - Math.Max(0m, used - opening)
            : opening + earned - used;
        available = available < 0 ? 0m : available;

        return new CompensatoryBreakdown(opening, openingRemaining, earned, used, available, expired);
    }

    /// <summary>查詢當前使用者的可補休時數（期初匯入 + 系統加班 - 已補休；期初 116/6/30 到期歸零）</summary>
    public async Task<IActionResult> GetCompensatoryHoursAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var b = await ComputeCompensatoryAsync(userId);

        return new OkObjectResult(ApiResponse.Ok(new
        {
            openingHours          = b.OpeningHours,       // 期初匯入
            openingRemaining      = b.OpeningRemaining,   // 舊補休剩餘
            openingExpiry         = CompensatoryOpeningExpiry,
            openingExpired        = b.OpeningExpired,
            totalOvertimeHours    = b.OvertimeHours,      // 系統加班可補休
            usedCompensatoryHours = b.UsedHours,
            availableHours        = b.AvailableHours,     // 合計可用
        }));
    }

    /// <summary>查詢當前使用者的年假額度（根據 HireDate 計算年資）</summary>
    public async Task<IActionResult> GetAnnualQuotaAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HireDate is null)
            return new OkObjectResult(ApiResponse.Ok(new
            {
                totalDays = 0,
                usedDays = 0m,
                availableDays = 0m,
                seniorityYears = 0,
                seniorityMonths = 0,
                message = "未設定到職日",
            }));

        var now = Clock.Now;
        // 育嬰留停期間不計入工作年資 → 特休額度隨之暫停累積
        var parentalExcludedDays = await GetParentalLeaveDaysAsync(userId, now);
        var (years, months) = SeniorityHelper.Calculate(user.HireDate.Value, now, parentalExcludedDays);
        int totalDays = SeniorityHelper.CalculateAnnualLeaveDays(years, months);

        // 查詢今年已使用的年假天數（pending + approved）
        var startOfYear = new DateTime(now.Year, 1, 1);
        var endOfYear = new DateTime(now.Year, 12, 31, 23, 59, 59);
        var usedHours = await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "annual"
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending")
                     && l.StartDate >= startOfYear && l.StartDate <= endOfYear)
            .SumAsync(l => l.Hours);
        var usedDays = usedHours / 8m;

        return new OkObjectResult(ApiResponse.Ok(new
        {
            totalDays,
            usedDays = Math.Round(usedDays, 1),
            availableDays = Math.Round(Math.Max(0, totalDays - usedDays), 1),
            seniorityYears = years,
            seniorityMonths = months,
            parentalLeaveExcludedDays = parentalExcludedDays,   // 已扣除的育嬰留停天數（0 = 未曾留停）
        }));
    }

    /// <summary>查詢當前使用者的歲時祭儀假額度（僅原住民可用，每年 3 天，跨年歸零）</summary>
    public async Task<IActionResult> GetCeremonialQuotaAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        bool isIndigenous = user?.IsIndigenous == true;
        if (!isIndigenous)
        {
            return new OkObjectResult(ApiResponse.Ok(new
            {
                totalDays = 0,
                usedDays = 0m,
                availableDays = 0m,
                isIndigenous = false,
                message = "僅原住民身份之員工可申請歲時祭儀假。",
            }));
        }

        const int totalDays = 3;
        var now = Clock.Now;
        var startOfYear = new DateTime(now.Year, 1, 1);
        var endOfYear = new DateTime(now.Year, 12, 31, 23, 59, 59);
        var usedHours = await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "ceremonial_festival"
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending")
                     && l.StartDate >= startOfYear && l.StartDate <= endOfYear)
            .SumAsync(l => l.Hours);
        var usedDays = usedHours / 8m;

        return new OkObjectResult(ApiResponse.Ok(new
        {
            totalDays,
            usedDays = Math.Round(usedDays, 1),
            availableDays = Math.Round(Math.Max(0, totalDays - usedDays), 1),
            isIndigenous = true,
        }));
    }

    /// <summary>查詢當前使用者的生理假配額（限女性，每月 1 天、全年 12 天）</summary>
    public async Task<IActionResult> GetMenstrualQuotaAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        bool isFemale = await IsFemaleAsync(userId);
        if (!isFemale)
        {
            return new OkObjectResult(ApiResponse.Ok(new
            {
                isFemale = false,
                annualTotalDays = 0,
                annualUsedDays = 0m,
                annualAvailableDays = 0m,
                monthlyTotalDays = 0,
                monthlyUsedDays = 0m,
                monthlyAvailableDays = 0m,
                message = "僅女性員工可申請生理假。",
            }));
        }

        const int annualTotalDays = 12;
        const int monthlyTotalDays = 1;
        var now = Clock.Now;

        var annualUsedHours = await GetUsedHoursAsync(userId, "menstrual", 0, now.Year);
        var annualUsedDays = annualUsedHours / 8m;

        var monthlyUsedHours = await GetUsedHoursInMonthAsync(userId, "menstrual", 0, now.Year, now.Month);
        var monthlyUsedDays = monthlyUsedHours / 8m;

        return new OkObjectResult(ApiResponse.Ok(new
        {
            isFemale = true,
            annualTotalDays,
            annualUsedDays = Math.Round(annualUsedDays, 1),
            annualAvailableDays = Math.Round(Math.Max(0, annualTotalDays - annualUsedDays), 1),
            monthlyTotalDays,
            monthlyUsedDays = Math.Round(monthlyUsedDays, 1),
            monthlyAvailableDays = Math.Round(Math.Max(0, monthlyTotalDays - monthlyUsedDays), 1),
        }));
    }

    /// <summary>查詢當前使用者的婚假配額（上限 8 天，不限年度）</summary>
    public async Task<IActionResult> GetMarriageQuotaAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        const int maxDays = 8;

        var usedHours = await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "marriage"
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
            .SumAsync(l => l.Hours);
        var usedDays = Math.Round(usedHours / 8m, 1);
        var remaining = Math.Round(Math.Max(0, maxDays - usedDays), 1);

        return new OkObjectResult(ApiResponse.Ok(new MarriageQuotaDto(maxDays, usedDays, remaining)));
    }

    /// <summary>查詢當前使用者的產假狀態（檢查是否已有活躍申請）</summary>
    public async Task<IActionResult> GetMaternityStatusAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);

        var active = await db.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "maternity"
                     && (l.ApprovalStatus == "pending" || l.ApprovalStatus == "approved"))
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync();

        return new OkObjectResult(ApiResponse.Ok(new MaternityStatusDto(
            HasActiveRequest: active is not null,
            ActiveRequestId:  active?.Id,
            StartDate:        active?.StartDate,
            EndDate:          active?.EndDate,
            ApprovalStatus:   active?.ApprovalStatus)));
    }

    /// <summary>查詢當前使用者的喪假配額（依親屬關係）</summary>
    public async Task<IActionResult> GetBereavementQuotaAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var relationship = req.Query["relationship"].ToString();
        if (string.IsNullOrWhiteSpace(relationship))
            return new BadRequestObjectResult(ApiResponse.Fail("relationship 為必填參數。"));
        if (!BereavementDaysLimit.TryGetValue(relationship, out var maxDays))
            return new BadRequestObjectResult(ApiResponse.Fail("無效的親屬關係。"));

        var usedHours = await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == "bereavement"
                     && l.BereavementRelationship == relationship
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
            .SumAsync(l => l.Hours);
        var usedDays = Math.Round(usedHours / 8m, 1);
        var remaining = Math.Round(Math.Max(0, maxDays - usedDays), 1);

        return new OkObjectResult(ApiResponse.Ok(new BereavementQuotaDto(relationship, maxDays, usedDays, remaining)));
    }

    /// <summary>查詢當前使用者高階主管假適用性（Superadmin 或 JobTitle.Level ≤ 3）</summary>
    public async Task<IActionResult> GetSeniorExecutiveEligibilityAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == userId);
        var level = user?.JobTitle?.Level;
        // Superadmin 一律視為符合
        bool eligible = user?.IsSuperAdmin == true || (level.HasValue && level.Value <= SeniorExecMaxLevel);
        return new OkObjectResult(ApiResponse.Ok(new SeniorExecutiveEligibilityDto(eligible, level)));
    }

    /// <summary>
    /// 查詢當前使用者的高階主管假額度（每年 24 天，曆年歸零；僅協理以上 / Superadmin）。
    /// 額度以「請假起始日所屬曆年」為基準，可帶 ?year= 指定；未帶或超出合理範圍則預設當年度。
    /// </summary>
    public async Task<IActionResult> GetSeniorExecutiveQuotaAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        int year = int.TryParse(req.Query["year"], out var y) && y >= 2000 && y <= 2100 ? y : Clock.Now.Year;

        var eligibilityError = await CheckSeniorExecutiveEligibilityAsync(userId);
        if (eligibilityError is not null)
            return new OkObjectResult(ApiResponse.Ok(new
            {
                year,
                totalDays = 0,
                usedDays = 0m,
                availableDays = 0m,
                isEligible = false,
                message = eligibilityError,
            }));

        var usedHours = await GetUsedHoursAsync(userId, "senior_executive", 0, year);
        var usedDays = usedHours / 8m;

        return new OkObjectResult(ApiResponse.Ok(new
        {
            year,
            totalDays = SeniorExecutiveAnnualDays,
            usedDays = Math.Round(usedDays, 1),
            availableDays = Math.Round(Math.Max(0, SeniorExecutiveAnnualDays - usedDays), 1),
            isEligible = true,
        }));
    }

    /// <summary>
    /// 查詢當前使用者的育嬰留職停薪額度。
    /// 兩層額度：每名子女合計 730 天（兩種育嬰假別併計，需帶 childBirthDate 才能計算）
    /// ＋ 彈性單日每人每年 30 日（依當年度）。
    /// </summary>
    public async Task<IActionResult> GetParentalQuotaAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var now = Clock.Now;

        // 資格：在職滿 6 個月（Superadmin 一律通過，比照高階主管假）
        int seniorityMonths = 0;
        bool isEligible = user?.IsSuperAdmin == true;
        string? message = null;
        if (user?.HireDate is null)
        {
            if (!isEligible) message = "未設定到職日";
        }
        else
        {
            var parentalExcludedDays = await GetParentalLeaveDaysAsync(userId, now);
            var (y, m) = SeniorityHelper.Calculate(user.HireDate.Value, now, parentalExcludedDays);
            seniorityMonths = y * 12 + m;
            if (!isEligible) isEligible = seniorityMonths >= ParentalMinSeniorityMonths;
            if (!isEligible) message = $"在職未滿 {ParentalMinSeniorityMonths} 個月";
        }

        // 子女出生日期（選填）：有帶才算得出「該名子女」的總額度與 3 歲資格
        DateTime? childBirthDate = null;
        if (DateTime.TryParse(req.Query["childBirthDate"], out var parsedChild))
            childBirthDate = parsedChild.Date;

        bool childAgeValid = childBirthDate is not null
                          && now.Date < childBirthDate.Value.AddYears(ParentalChildMaxAgeYears);

        decimal usedDays = 0m;
        if (childBirthDate is not null)
        {
            var usedHours = await db.LeaveRequests.AsNoTracking()
                .Where(l => l.EmployeeId == userId
                         && ParentalLeaveTypes.Contains(l.LeaveType)
                         && l.ChildBirthDate == childBirthDate
                         && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
                .SumAsync(l => l.Hours);
            usedDays = usedHours / 8m;
        }

        // 彈性單日年度額度（不分子女，依人依年度）
        var dailyYearUsedHours = await GetUsedHoursAsync(userId, "parental_leave_daily", excludeId: 0, year: now.Year);
        var dailyYearUsed = dailyYearUsedHours / 8m;

        return new OkObjectResult(ApiResponse.Ok(new ParentalQuotaDto(
            IsEligible:         isEligible,
            SeniorityMonths:    seniorityMonths,
            ChildAgeValid:      childAgeValid,
            ChildBirthDate:     childBirthDate,
            TotalDays:          ParentalTotalDays,
            UsedDays:           Math.Round(usedDays, 1),
            AvailableDays:      Math.Round(Math.Max(0, ParentalTotalDays - usedDays), 1),
            DailyYearLimit:     ParentalDailyAnnualDays,
            DailyYearUsed:      Math.Round(dailyYearUsed, 1),
            DailyYearAvailable: Math.Round(Math.Max(0, ParentalDailyAnnualDays - dailyYearUsed), 1),
            Message:            message)));
    }

    /// <summary>檢查使用者是否符合高階主管假資格，回傳錯誤訊息或 null（Superadmin 一律通過）</summary>
    private async Task<string?> CheckSeniorExecutiveEligibilityAsync(Guid userId)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.IsSuperAdmin == true) return null;
        var level = user?.JobTitle?.Level;
        if (!level.HasValue || level.Value > SeniorExecMaxLevel)
            return "高階主管假僅限協理（含）以上職級申請。";
        return null;
    }

    /// <summary>計算指定使用者可用的補休時數（含期初匯入餘額與到期歸零）</summary>
    private async Task<decimal> GetAvailableCompensatoryHoursAsync(Guid userId)
        => (await ComputeCompensatoryAsync(userId)).AvailableHours;

    /// <summary>送出申請（draft → pending）</summary>
    public async Task<IActionResult> SubmitAsync(HttpRequest req, string id)
    {
        var userId = await GetUserIdAsync(req);
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid leave request ID format."));

        var currentUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var item = currentUser?.IsSuperAdmin == true
            ? await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId)
            : await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == intId && x.EmployeeId == userId);
        if (item is null) throw AppException.NotFound("LeaveRequest");

        if (item.ApprovalStatus != "draft" && item.ApprovalStatus != "returned")
            throw AppException.BadRequest("Only draft or returned leave requests can be submitted.");

        // 送簽時才取號：單號日期＝送簽日，草稿不佔號。
        // 退回（returned）重送時已有單號，不可重新配號，否則已流通的單號會被改掉。
        if (string.IsNullOrEmpty(item.RequestNo))
            item.RequestNo = await RequestNoGenerator.NextAsync(
                db.LeaveRequests.Select(x => x.RequestNo), "LV-", Clock.Now);

        // 送簽日期只在首次送簽寫入：退回（returned）重送不改，與單號規則一致。
        item.SubmittedAt ??= Clock.Now;

        // 喪假驗證：必須有親屬關係
        if (item.LeaveType == "bereavement" && string.IsNullOrWhiteSpace(item.BereavementRelationship))
            return new BadRequestObjectResult(ApiResponse.Fail("喪假必須選擇親屬關係。"));

        // 高階主管假：職級檢查（送出時再次驗證，防止草稿期間職級變更）
        if (item.LeaveType == "senior_executive")
        {
            var eligError = await CheckSeniorExecutiveEligibilityAsync(item.EmployeeId ?? Guid.Empty);
            if (eligError is not null)
                return new BadRequestObjectResult(ApiResponse.Fail(eligError));
        }

        // 育嬰留停：資格再次驗證（防止草稿期間子女已滿 3 歲）
        if (ParentalLeaveTypes.Contains(item.LeaveType))
        {
            var parentalError = await CheckParentalEligibilityAsync(
                item.EmployeeId ?? Guid.Empty, item.ChildBirthDate, item.StartDate, item.EndDate);
            if (parentalError is not null)
                return new BadRequestObjectResult(ApiResponse.Fail(parentalError));
        }

        // 工作日型假別（除歲時祭儀假外皆是）：送出時強制要求行事曆已匯入並權威重算 Hours（扣國定假日與六日）。
        // Day → 工作日數 × 8（產假亦走此路徑）；Hour → 逐日累加只算工作日；
        // HalfDay 沿用既有「信任 client」原則不重算。
        // 確保後續 requestDays（Hours/8）分流與天數上限驗證皆以正確工作日為準。
        var submitUnit = GetTimeUnit(item.LeaveType);
        if (WorkingDayLeaveTypes.Contains(item.LeaveType) &&
            submitUnit is LeaveTimeUnit.Day or LeaveTimeUnit.Hour)
        {
            var yearLabel = item.StartDate.Year == item.EndDate.Year
                ? $"{item.StartDate:yyyy}"
                : $"{item.StartDate:yyyy}–{item.EndDate:yyyy}";

            if (submitUnit == LeaveTimeUnit.Day)
            {
                var (hasData, _, working) = await ComputeWorkingDatesAsync(item.EmployeeId ?? Guid.Empty, item.StartDate, item.EndDate);
                if (!hasData)
                    return new BadRequestObjectResult(ApiResponse.Fail(
                        $"尚未匯入 {yearLabel} 年行事曆，無法計算扣除假日後的請假天數，請先於「行事曆設定」匯入。"));
                if (working.Count == 0)
                    return new BadRequestObjectResult(ApiResponse.Fail("此區間全為國定假日或六日，無可請假的工作日。"));
                item.Hours = working.Count * 8m;
            }
            else
            {
                var (hasData, hours) = await ComputeHourUnitHoursAsync(item.EmployeeId ?? Guid.Empty, item.StartDate, item.EndDate);
                if (!hasData)
                    return new BadRequestObjectResult(ApiResponse.Fail(
                        $"尚未匯入 {yearLabel} 年行事曆，無法計算扣除假日後的請假時數，請先於「行事曆設定」匯入。"));
                if (hours <= 0)
                    return new BadRequestObjectResult(ApiResponse.Fail("此區間全為國定假日或六日，無可請假的工作日。"));
                item.Hours = hours;
            }
        }

        // 退回重送時清除舊審核記錄，重置指定審核者狀態，重新走流程
        if (item.ApprovalStatus == "returned")
        {
            var oldRecords = await db.ApprovalRecords
                .Where(r => r.ApplicationType == "leave" && r.ApplicationId == item.Id)
                .ToListAsync();
            db.ApprovalRecords.RemoveRange(oldRecords);

            var oldOverrides = await db.EscalationOverrides
                .Where(o => o.ApplicationType == "leave" && o.ApplicationId == item.Id)
                .ToListAsync();
            db.EscalationOverrides.RemoveRange(oldOverrides);

            // 重置指定審核者狀態為 pending
            var rdrsToReset = await db.RequestDesignatedReviewers
                .Where(r => r.RequestType == "leave" && r.RequestId == item.Id)
                .ToListAsync();
            foreach (var rdr in rdrsToReset)
            {
                rdr.Status     = "pending";
                rdr.ReviewedAt = null;
                rdr.Comment    = null;
            }
        }

        // 日期重疊檢查：排除自身（送出階段再驗一次，防範 draft 期間其他申請已先被建立）
        var overlapErr = await CheckOverlapAsync(item.EmployeeId!.Value, item.StartDate, item.EndDate, item.LeaveType, excludeId: item.Id);
        if (overlapErr is not null)
            return new BadRequestObjectResult(ApiResponse.Fail(overlapErr));

        // 補休時數驗證：申請時數不得超過可用時數
        if (item.LeaveType == "compensatory")
        {
            var available = await GetAvailableCompensatoryHoursAsync(userId);
            var requestedHours = item.Hours;
            if (requestedHours > available)
                return new BadRequestObjectResult(ApiResponse.Fail(
                    $"補休時數不足。申請 {requestedHours} 小時，可用 {available} 小時。"));
        }

        // 天數上限驗證（累計制）
        var quotaError = await ValidateLeaveQuotaAsync(userId, item);
        if (quotaError is not null)
            return new BadRequestObjectResult(ApiResponse.Fail(quotaError));

        // Superadmin 無部門歸屬，直接自動核准
        var submitter = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (submitter?.IsSuperAdmin == true)
        {
            item.ApprovalStatus   = "approved";
            item.CurrentStepOrder = 1;
            item.ReviewedAt       = Clock.Now;
            item.ReviewedById     = userId;
            item.ReviewNote       = "系統自動核准（Superadmin）";
            await db.SaveChangesAsync();
            await notifier.NotifyLeaveAgentAsync(item.Id);
            var saDto = await reader.GetByIdAsync(item.Id);
            return new OkObjectResult(ApiResponse.Ok(saDto, "Leave request auto-approved."));
        }

        // 自動關聯簽核流程（依申請人部門挑流程：部門專屬優先，否則退回通用預設）
        if (item.ApprovalItemId is null)
            item.ApprovalItemId = await approvalFlow.ResolveApprovalItemIdAsync("leave", submitter?.DepartmentId);

        // 正規化各 designee 所屬步驟並驗證每個指定審核步驟皆有審核者
        await DesignatedReviewerHelper.ValidateAndNormalizeAsync(db, "leave", item.Id, item.ApprovalItemId, userId);
        await db.SaveChangesAsync();

        // 查詢指定審核者清單傳給 ResolveStartingStepAsync（含 ApprovalStepOrder 綁定步驟）
        var designatedReviewers = await DesignatedReviewerHelper.ReadForFlowAsync(db, "leave", item.Id);

        // 解析審核步驟（含升級審核邏輯）；帶入申請天數（Hours/8）供 MinDays 天數門檻分流
        // （例：<3 天只走單位主管；≥3 天含部門最高主管 + 總監）
        var (startStep, autoApproved, escalation) =
            await approvalFlow.ResolveStartingStepAsync(item.ApprovalItemId, userId, "leave", designatedReviewers,
                requestDays: item.Hours / 8m);

        if (autoApproved)
        {
            item.ApprovalStatus   = "approved";
            item.CurrentStepOrder = startStep;
            item.ReviewedAt       = Clock.Now;
            item.ReviewedById     = userId;
            item.ReviewNote       = "系統自動核准（所有審核步驟皆為申請人本人）";
        }
        else
        {
            item.ApprovalStatus   = "pending";
            item.CurrentStepOrder = startStep;
        }

        // 升級審核：記錄指派的審核者
        if (escalation is not null)
        {
            db.EscalationOverrides.Add(new EscalationOverride
            {
                ApplicationType  = "leave",
                ApplicationId    = item.Id,
                StepOrder        = startStep,
                ReviewerId       = escalation.ReviewerId,
                OnBehalfOfUserId = escalation.OnBehalfOfUserId,
                CreatedAt        = Clock.Now,
            });
        }

        await db.SaveChangesAsync();

        // 通知審核者
        if (!autoApproved)
        {
            if (escalation is not null)
                await notifier.NotifySpecificReviewerAsync("leave", item.Id, escalation.ReviewerId, userId, escalation.OnBehalfOfUserId is not null);
            else
            {
                // 檢查當前步驟是否為指定審核步驟
                // 指定審核步驟（原生 UseApplicantDesignated 或例外指定審核命中）：讀 designee 快照，
                // 與 ResolveStartingStepAsync 的判定同源，確保不會誤走部門/職稱通知
                bool isDesignatedStep = designatedReviewers.Any(r => r.ApprovalStepOrder == startStep);
                if (isDesignatedStep)
                {
                    var firstReviewer = await db.RequestDesignatedReviewers
                        .AsNoTracking()
                        .Where(r => r.RequestType == "leave" && r.RequestId == item.Id
                                 && r.ApprovalStepOrder == startStep && r.Status == "pending")
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefaultAsync();
                    if (firstReviewer is not null)
                        await notifier.NotifySpecificReviewerAsync("leave", item.Id, firstReviewer.ReviewerId, userId, false);
                }
                else
                    await notifier.NotifyReviewersAsync("leave", item.Id, item.ApprovalItemId, startStep, userId);
            }
        }

        // 通知職務代理人（若有指定；僅知會、不參與簽核）
        await notifier.NotifyLeaveAgentAsync(item.Id);

        var dto = await reader.GetByIdAsync(item.Id);
        var msg = autoApproved ? "Leave request auto-approved." : "Leave request submitted.";
        return new OkObjectResult(ApiResponse.Ok(dto, msg));
    }

    // ── Overlap Validation ───────────────────────────────────────────────────

    /// <summary>
    /// 檢查同員工同期間是否有重疊申請（draft / pending / approved；cancelled 已由 SQL 排除）。
    /// 產假已有獨立 active 檢查（CreateAsync 中），此處跳過避免重複訊息；
    /// 但其他假別仍會檢查與既有產假的重疊（重疊 SQL 不限假別）。
    /// 部分銷假：若既有假單與本次重疊的日子已全數被核准銷假，該筆不算衝突（讓挖空的日子可重新申請）。
    /// </summary>
    /// <returns>衝突時回傳中文錯誤訊息；無衝突回 null</returns>
    private async Task<string?> CheckOverlapAsync(
        Guid employeeId, DateTime startDate, DateTime endDate,
        string leaveType, int? excludeId)
    {
        if (leaveType == "maternity") return null;

        var conflicts = (await reader.GetOverlappingRequestsAsync(employeeId, startDate, endDate, excludeId)).ToList();
        conflicts = await FilterFullyRevokedAsync(conflicts, startDate, endDate);
        if (conflicts.Count == 0) return null;

        var lines = conflicts.Take(3).Select(c =>
        {
            var name = LeaveTypeNames.GetZh(c.LeaveType);
            return $"• #{c.Id} {name} {c.StartDate:yyyy/MM/dd HH:mm}–{c.EndDate:yyyy/MM/dd HH:mm}（{c.ApprovalStatus}）";
        });
        var more = conflicts.Count > 3 ? $"\n（另有 {conflicts.Count - 3} 筆…）" : "";
        return $"申請期間與既有申請衝突，請調整或先處理既有申請：\n{string.Join("\n", lines)}{more}";
    }

    /// <summary>
    /// 剔除「與本次申請重疊的日子已全數被核准銷假」的既有假單。
    /// 逐日比對，故支援部分銷假挖空中間日後重新申請該日的情境。
    /// </summary>
    private async Task<List<OverlappingLeaveRequestDto>> FilterFullyRevokedAsync(
        List<OverlappingLeaveRequestDto> conflicts, DateTime startDate, DateTime endDate)
    {
        if (conflicts.Count == 0) return conflicts;

        var ids = conflicts.Select(c => c.Id).ToList();
        var revoked = await db.LeaveRevocationDates
            .AsNoTracking()
            .Where(d => ids.Contains(d.LeaveRevocation!.LeaveRequestId)
                     && d.LeaveRevocation.ApprovalStatus == "approved")
            .Select(d => new { d.LeaveRevocation!.LeaveRequestId, d.Date })
            .ToListAsync();
        if (revoked.Count == 0) return conflicts;

        var revokedByLeave = revoked
            .GroupBy(r => r.LeaveRequestId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Date.Date).ToHashSet());

        return [.. conflicts.Where(c =>
        {
            if (!revokedByLeave.TryGetValue(c.Id, out var revokedDates)) return true;
            // 重疊區間（日層級）內只要還有任一天未被銷假，即仍算衝突
            var from = c.StartDate.Date > startDate.Date ? c.StartDate.Date : startDate.Date;
            var to   = c.EndDate.Date   < endDate.Date   ? c.EndDate.Date   : endDate.Date;
            return WorkCalendarHelper.EnumerateDates(from, to).Any(d => !revokedDates.Contains(d));
        })];
    }

    // ── Quota Validation ─────────────────────────────────────────────────────

    /// <summary>驗證假別天數上限（累計制），回傳錯誤訊息或 null</summary>
    private async Task<string?> ValidateLeaveQuotaAsync(Guid userId, LeaveRequest item)
    {
        var now = Clock.Now;

        // 年假額度驗證
        if (item.LeaveType == "annual")
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.HireDate is null)
                return "未設定到職日，無法申請年假。";

            // 必須與 GetAnnualQuotaAsync 用同一套扣除算法，否則查得到的額度與實際能送的會不一致
            var parentalExcludedDays = await GetParentalLeaveDaysAsync(userId, now);
            var (years, months) = SeniorityHelper.Calculate(user.HireDate.Value, now, parentalExcludedDays);
            int totalDays = SeniorityHelper.CalculateAnnualLeaveDays(years, months);
            if (totalDays <= 0)
                return "年資不足，尚無年假額度。";

            var usedHours = await GetUsedHoursAsync(userId, "annual", item.Id, now.Year);
            var totalUsedDays = (usedHours + item.Hours) / 8m;
            if (totalUsedDays > totalDays)
                return $"年假額度不足。上限 {totalDays} 天，已使用 {Math.Round(usedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";

            return null;
        }

        // 高階主管假：協理以上每年 24 天（曆年歸零，依申請起始日所屬年度）；資格另由 CheckSeniorExecutiveEligibilityAsync 驗證
        if (item.LeaveType == "senior_executive")
        {
            var year = item.StartDate.Year;
            var usedHours = await GetUsedHoursAsync(userId, "senior_executive", item.Id, year);
            var totalUsedDays = (usedHours + item.Hours) / 8m;
            if (totalUsedDays > SeniorExecutiveAnnualDays)
                return $"高階主管假額度不足。每年上限 {SeniorExecutiveAnnualDays} 天，{year} 年已使用 {Math.Round(usedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";
            return null;
        }

        // 歲時祭儀假：限原住民身份，每年 3 天（跨年歸零）
        if (item.LeaveType == "ceremonial_festival")
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.IsIndigenous != true)
                return "僅原住民身份之員工可申請歲時祭儀假。";

            const int totalDays = 3;
            var usedHours = await GetUsedHoursAsync(userId, "ceremonial_festival", item.Id, now.Year);
            var totalUsedDays = (usedHours + item.Hours) / 8m;
            if (totalUsedDays > totalDays)
                return $"歲時祭儀假額度不足。上限 {totalDays} 天，已使用 {Math.Round(usedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";
            return null;
        }

        // 生理假：限女性，每月 1 天（8h）、全年 12 天（96h）上限
        if (item.LeaveType == "menstrual")
        {
            if (!await IsFemaleAsync(userId))
                return "僅女性員工可申請生理假。";

            // 月上限（依申請起始日所屬年月）
            var monthUsed = await GetUsedHoursInMonthAsync(userId, "menstrual", item.Id, item.StartDate.Year, item.StartDate.Month);
            if (monthUsed + item.Hours > 8m)
                return $"生理假每月上限 1 天。{item.StartDate:yyyy/MM} 已使用 {Math.Round(monthUsed / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";

            // 年上限
            var yearUsed = await GetUsedHoursAsync(userId, "menstrual", item.Id, item.StartDate.Year);
            if (yearUsed + item.Hours > 96m)
                return $"生理假全年上限 12 天。{item.StartDate.Year} 年已使用 {Math.Round(yearUsed / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";

            return null;
        }

        // 育嬰留職停薪：兩層額度
        //   1. 每名子女合計 730 天（2 年）—— 兩種育嬰假別併計，以 ChildBirthDate 為分組鍵
        //   2. 彈性單日另受「每人每年 30 日」限制（依申請起始日所屬年度，非 Clock.Now.Year）
        // 資格（在職滿 6 個月 / 子女未滿 3 歲）由 CheckParentalEligibilityAsync 於三階段前置檢查。
        if (ParentalLeaveTypes.Contains(item.LeaveType))
        {
            var eligError = await CheckParentalEligibilityAsync(userId, item.ChildBirthDate, item.StartDate, item.EndDate);
            if (eligError is not null) return eligError;

            var childUsedHours = await db.LeaveRequests
                .Where(l => l.EmployeeId == userId
                         && ParentalLeaveTypes.Contains(l.LeaveType)
                         && l.ChildBirthDate == item.ChildBirthDate
                         && l.Id != item.Id
                         && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
                .SumAsync(l => l.Hours);
            var childUsedDays = (childUsedHours + item.Hours) / 8m;
            if (childUsedDays > ParentalTotalDays)
                return $"育嬰留職停薪額度不足。每名子女合計上限 {ParentalTotalDays} 天（2 年），已使用 {Math.Round(childUsedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";

            if (item.LeaveType == "parental_leave_daily")
            {
                var dailyYear = item.StartDate.Year;
                var dailyUsedHours = await GetUsedHoursAsync(userId, "parental_leave_daily", item.Id, dailyYear);
                var dailyUsedDays = (dailyUsedHours + item.Hours) / 8m;
                if (dailyUsedDays > ParentalDailyAnnualDays)
                    return $"育嬰留停(單日)額度不足。每人每年上限 {ParentalDailyAnnualDays} 日，{dailyYear} 年已使用 {Math.Round(dailyUsedHours / 8m, 1)} 日，本次申請 {Math.Round(item.Hours / 8m, 1)} 日。";
            }

            return null;
        }

        // 有固定天數上限的假別
        if (LeaveTypeDaysLimit.TryGetValue(item.LeaveType, out var limit))
        {
            // 產假類別不限年度，其他按年度計算
            bool isMaternityType = item.LeaveType is "maternity" or "miscarriage_3m" or "miscarriage_2to3m" or "miscarriage_under2m" or "prenatal_checkup" or "paternity";
            int? year = isMaternityType ? null : now.Year;

            var usedHours = await GetUsedHoursAsync(userId, item.LeaveType, item.Id, year);
            var totalUsedDays = (usedHours + item.Hours) / 8m;
            if (totalUsedDays > limit)
            {
                var leaveLabel = GetLeaveTypeLabel(item.LeaveType);
                return $"{leaveLabel}額度不足。上限 {limit} 天，已使用 {Math.Round(usedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";
            }
            return null;
        }

        // 喪假：根據親屬關係的天數上限
        if (item.LeaveType == "bereavement")
        {
            if (string.IsNullOrWhiteSpace(item.BereavementRelationship) ||
                !BereavementDaysLimit.TryGetValue(item.BereavementRelationship, out var bLimit))
                return "喪假必須選擇有效的親屬關係。";

            // 喪假按同親屬關係累計（不限年度）
            var usedHours = await db.LeaveRequests
                .Where(l => l.EmployeeId == userId
                         && l.LeaveType == "bereavement"
                         && l.BereavementRelationship == item.BereavementRelationship
                         && l.Id != item.Id
                         && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"))
                .SumAsync(l => l.Hours);
            var totalUsedDays = (usedHours + item.Hours) / 8m;
            if (totalUsedDays > bLimit)
                return $"喪假額度不足。上限 {bLimit} 天，已使用 {Math.Round(usedHours / 8m, 1)} 天，本次申請 {Math.Round(item.Hours / 8m, 1)} 天。";
            return null;
        }

        // personal / sick / official / compensatory：無天數上限或由其他邏輯驗證
        return null;
    }

    /// <summary>檢查員工性別是否為女性（生理假限定）；性別存於 EmployeeProfile.Gender（"M"/"F"）</summary>
    private async Task<bool> IsFemaleAsync(Guid userId) =>
        await db.EmployeeProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.Gender)
            .FirstOrDefaultAsync() == "F";

    /// <summary>查詢指定月份已使用時數（排除當前申請，依 StartDate 落於該年月）</summary>
    private async Task<decimal> GetUsedHoursInMonthAsync(Guid userId, string leaveType, int excludeId, int year, int month)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);
        return await db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == leaveType
                     && l.Id != excludeId
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending")
                     && l.StartDate >= startOfMonth && l.StartDate < endOfMonth)
            .SumAsync(l => l.Hours);
    }

    /// <summary>查詢已使用時數（排除當前申請，可選按年度過濾）</summary>
    private async Task<decimal> GetUsedHoursAsync(Guid userId, string leaveType, int excludeId, int? year)
    {
        var query = db.LeaveRequests
            .Where(l => l.EmployeeId == userId
                     && l.LeaveType == leaveType
                     && l.Id != excludeId
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending"));

        if (year.HasValue)
        {
            var startOfYear = new DateTime(year.Value, 1, 1);
            var endOfYear = new DateTime(year.Value, 12, 31, 23, 59, 59);
            query = query.Where(l => l.StartDate >= startOfYear && l.StartDate <= endOfYear);
        }

        return await query.SumAsync(l => l.Hours);
    }

    // ── Seniority / Annual Leave ─────────────────────────────────────────────
    // 年資與年假天數的計算已抽至 Api/Common/SeniorityHelper.cs（因育嬰留停需扣除天數，
    // 且 GetAnnualQuotaAsync 與 ValidateLeaveQuotaAsync 兩處必須用同一套算法）。

    /// <summary>
    /// 當日結束時刻（23:59）。整天型假別的結束日必須涵蓋到當天結束 —— 重疊驗證與打卡阻擋
    /// 皆以半開區間 [StartDate, EndDate) 比對，結束日若停在 00:00 會讓當天完全不受保護。
    /// </summary>
    private static DateTime EndOfDay(DateTime d) => d.Date.AddHours(23).AddMinutes(59);

    /// <summary>
    /// 計算「已經過去」的育嬰留停累計天數，供年資扣除使用（留停期間不計入工作年資）。
    /// 只計已核准者；進行中的留停只算到 asOf 為止，尚未發生的天數不提前扣年資。
    /// 已結束者直接用 Hours ÷ 8（銷假後 Hours 已遞減，天數自動反映）。
    /// </summary>
    private async Task<int> GetParentalLeaveDaysAsync(Guid userId, DateTime asOf)
    {
        var rows = await db.LeaveRequests.AsNoTracking()
            .Where(l => l.EmployeeId == userId
                     && ParentalLeaveTypes.Contains(l.LeaveType)
                     && l.ApprovalStatus == "approved"
                     && l.StartDate <= asOf)
            .Select(l => new { l.StartDate, l.EndDate, l.Hours })
            .ToListAsync();

        var cutoff = asOf.Date;
        decimal total = 0m;
        foreach (var r in rows)
        {
            total += r.EndDate.Date <= cutoff
                ? r.Hours / 8m                                  // 已結束
                : (cutoff - r.StartDate.Date).Days + 1;         // 進行中，只算已過去的日曆天
        }
        return (int)Math.Round(total, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 檢查育嬰留停申請資格，回傳錯誤訊息或 null。
    /// Superadmin 一律通過（比照高階主管假；亦對應法規「在職未滿 6 個月經雇主同意亦可申請」）。
    /// </summary>
    private async Task<string?> CheckParentalEligibilityAsync(
        Guid userId, DateTime? childBirthDate, DateTime startDate, DateTime? endDate = null)
    {
        if (childBirthDate is null)
            return "育嬰留職停薪必須填寫子女出生日期。";

        // 子女未滿 3 歲：起訖日皆須落在 3 歲生日之前（法規為「子女滿 3 歲前」得申請並休畢；
        // 只擋起始日會讓一張 730 天的留停一路延續到子女 5 歲）
        var thirdBirthday = childBirthDate.Value.Date.AddYears(ParentalChildMaxAgeYears);
        if (startDate.Date >= thirdBirthday)
            return $"育嬰留職停薪僅限子女未滿 {ParentalChildMaxAgeYears} 歲前申請。";
        if (endDate.HasValue && endDate.Value.Date >= thirdBirthday)
            return $"育嬰留職停薪須於子女滿 {ParentalChildMaxAgeYears} 歲前結束，請將結束日調整為 {thirdBirthday.AddDays(-1):yyyy/MM/dd}（含）之前。";

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.IsSuperAdmin == true) return null;
        if (user?.HireDate is null)
            return "未設定到職日，無法申請育嬰留職停薪。";

        var (years, months) = SeniorityHelper.Calculate(user.HireDate.Value, Clock.Now);
        if (years * 12 + months < ParentalMinSeniorityMonths)
            return $"育嬰留職停薪需在職滿 {ParentalMinSeniorityMonths} 個月。";
        return null;
    }

    /// <summary>假別中文標籤（用於錯誤訊息）</summary>
    private static string GetLeaveTypeLabel(string leaveType) => leaveType switch
    {
        "annual"             => "年假",
        "personal"           => "事假",
        "sick"               => "病假",
        "compensatory"       => "補休",
        "marriage"           => "婚假",
        "bereavement"        => "喪假",
        "official"           => "公假",
        "maternity"          => "產假",
        "miscarriage_3m"     => "流產假(3個月以上)",
        "miscarriage_2to3m"  => "流產假(2-3個月)",
        "miscarriage_under2m"=> "流產假(未滿2個月)",
        "prenatal_checkup"   => "產檢假",
        "paternity"          => "陪產假",
        "ceremonial_festival"=> "歲時祭儀假",
        "senior_executive"   => "高階主管假",
        "menstrual"          => "生理假",
        "family_care"        => "家庭照顧假",
        "parental_leave"     => "育嬰留職停薪",
        "parental_leave_daily" => "育嬰留停(單日)",
        _                    => leaveType,
    };

    // ── Helper ──────────────────────────────────────────────────────────────────

    /// <summary>從 JWT Bearer Token 取出 sub claim 作為使用者 GUID</summary>
    private async Task<Guid> GetUserIdAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw AppException.Unauthorized("Invalid token claims.");
        return userId;
    }
}
