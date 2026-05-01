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
    IProjectAccessResolver access)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;

        Guid? employeeId   = Guid.TryParse(req.Query["employeeId"], out var eid) ? eid : null;
        DateOnly? dateFrom = DateOnly.TryParse(req.Query["dateFrom"], out var df) ? df : null;
        DateOnly? dateTo   = DateOnly.TryParse(req.Query["dateTo"],   out var dt) ? dt : null;

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var result = await reader.GetPagedAsync(scope, page, pageSize, employeeId, dateFrom, dateTo);
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

        await EnsureNotOnLeaveAsync(userId, now);

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

        await EnsureNotOnLeaveAsync(userId, now);

        record.ClockOutTime      = now;
        record.ClockOutLatitude   = body.Latitude;
        record.ClockOutLongitude  = body.Longitude;

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "下班打卡成功。"));
    }

    /// <summary>加班開始打卡（需已下班 + 已核准加班申請）</summary>
    public async Task<IActionResult> OvertimeStartAsync(HttpRequest req)
    {
        var userId = await GetUserIdAsync(req);
        var body   = await req.ReadFromJsonAsync<ClockActionRequest>() ?? new ClockActionRequest(null, null);

        var now    = Clock.Now;
        var today  = now.Date;
        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.UserId == userId && a.RecordDate == today)
            ?? throw AppException.BadRequest("請先完成上下班打卡。");

        if (!record.ClockOutTime.HasValue)
            throw AppException.BadRequest("請先打下班卡。");
        if (record.OvertimeStartTime.HasValue)
            throw AppException.BadRequest("今日已打加班開始卡。");

        // 驗證加班申請
        if (!body.OvertimeRequestId.HasValue)
            throw AppException.BadRequest("請選擇已核准的加班申請。");

        var otRequest = await db.OvertimeRequests.FindAsync(body.OvertimeRequestId.Value)
            ?? throw AppException.BadRequest("找不到指定的加班申請。");

        if (otRequest.ApprovalStatus != "approved")
            throw AppException.BadRequest("加班申請尚未核准。");
        if (otRequest.OvertimeDate.Date != today)
            throw AppException.BadRequest("加班申請日期與今日不符。");

        record.OvertimeStartTime      = now;
        record.OvertimeStartLatitude   = body.Latitude;
        record.OvertimeStartLongitude  = body.Longitude;
        record.OvertimeRequestId       = body.OvertimeRequestId;

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

        await db.SaveChangesAsync();

        var dto = await BuildTodayDtoAsync(userId);
        return new OkObjectResult(ApiResponse.Ok(dto, "加班結束打卡成功。"));
    }

    /// <summary>修改出缺勤紀錄（上下班時間、加班開始/結束）</summary>
    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var recordId))
            throw AppException.BadRequest("無效的 ID。");

        var body = await req.ReadFromJsonAsync<UpdateAttendanceRequest>()
            ?? throw AppException.BadRequest("請提供更新資料。");

        var record = await db.AttendanceRecords.FindAsync(recordId)
            ?? throw AppException.BadRequest("找不到指定的出缺勤紀錄。");

        record.ClockInTime        = body.ClockInTime;
        record.ClockOutTime       = body.ClockOutTime;
        record.OvertimeStartTime  = body.OvertimeStartTime;
        record.OvertimeEndTime    = body.OvertimeEndTime;

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
    private async Task EnsureNotOnLeaveAsync(Guid userId, DateTime when)
    {
        var active = await reader.GetActiveLeaveAtAsync(userId, when);
        if (active is null) return;

        var typeZh = LeaveTypeNames.GetZh(active.LeaveType);
        throw AppException.BadRequest(
            $"您於此時段有已核准的請假（#{active.Id} {typeZh} " +
            $"{active.StartDate:MM/dd HH:mm}–{active.EndDate:MM/dd HH:mm}），無法打卡。");
    }

    /// <summary>
    /// 組合今日打卡 DTO + 當日已核准請假清單。打卡紀錄不存在時回傳 Id=0 的空殼 DTO，仍帶請假資訊供前端顯示提示。
    /// </summary>
    private async Task<TodayAttendanceDto> BuildTodayDtoAsync(Guid userId)
    {
        var today  = DateOnly.FromDateTime(Clock.Now);
        var record = await reader.GetTodayAsync(userId);
        var leaves = await reader.GetLeavesOnDateAsync(userId, today);

        return record is null
            ? new TodayAttendanceDto(
                Id: 0,
                RecordDate: today.ToDateTime(TimeOnly.MinValue),
                ClockInTime: null,             ClockInLatitude:  null, ClockInLongitude:  null,
                ClockOutTime: null,            ClockOutLatitude: null, ClockOutLongitude: null,
                OvertimeStartTime: null,       OvertimeStartLatitude: null, OvertimeStartLongitude: null,
                OvertimeEndTime:   null,       OvertimeEndLatitude:   null, OvertimeEndLongitude:   null,
                OvertimeRequestId: null,
                TodayLeaves: leaves)
            : record with { TodayLeaves = leaves };
    }
}
