using Jabez.Api.Common;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// 打卡提醒紀錄查詢端點（僅 Superadmin，由 AppRouter.IsSuperAdminRoute 守門）。
/// 提供：列表分頁、單筆詳情、依批次查全部、統計卡資料。
/// </summary>
public sealed class AttendanceReminderLogHandler(IAttendanceReminderLogReadService reader)
{
    /// <summary>GET /admin/attendance-reminder-logs?from=&to=&reminderType=&status=&errorCategory=&userId=&triggerSource=&page=&pageSize=</summary>
    public async Task<IActionResult> GetPagedAsync(HttpRequest req)
    {
        var ct = req.HttpContext.RequestAborted;

        DateTime? from = TryParseDate(req.Query["from"]);
        DateTime? to   = TryParseDate(req.Query["to"]);
        // 預設區間：最近 7 天（避免一開列表就拉爆）
        if (from is null && to is null)
        {
            to   = Clock.Today;
            from = to.Value.AddDays(-6);
        }
        if (from is not null && to is not null && (to.Value - from.Value).TotalDays > 92)
            throw AppException.BadRequest("查詢區間最長 92 天。");

        string? reminderType = NullIfEmpty(req.Query["reminderType"]);
        string? status       = NullIfEmpty(req.Query["status"]);
        string? errorCat     = NullIfEmpty(req.Query["errorCategory"]);
        string? source       = NullIfEmpty(req.Query["triggerSource"]);
        Guid?   userId       = Guid.TryParse(req.Query["userId"], out var uid) ? uid : null;

        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)              : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100)      : 20;

        var result = await reader.GetPagedAsync(from, to, reminderType, status, errorCat, userId, source, page, pageSize, ct);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    /// <summary>GET /admin/attendance-reminder-logs/stats</summary>
    public async Task<IActionResult> GetStatsAsync(HttpRequest req)
    {
        var ct = req.HttpContext.RequestAborted;
        var stats = await reader.GetStatsAsync(Clock.Today, ct);
        return new OkObjectResult(ApiResponse.Ok(stats));
    }

    /// <summary>GET /admin/attendance-reminder-logs/batches/{batchId}</summary>
    public async Task<IActionResult> GetByBatchIdAsync(HttpRequest req, string batchId)
    {
        var ct = req.HttpContext.RequestAborted;
        if (!Guid.TryParse(batchId, out var bid))
            throw AppException.BadRequest("batchId 格式不正確。");

        var rows = await reader.GetByBatchIdAsync(bid, ct);
        return new OkObjectResult(ApiResponse.Ok(rows));
    }

    /// <summary>GET /admin/attendance-reminder-logs/{id}</summary>
    public async Task<IActionResult> GetByIdAsync(HttpRequest req, string id)
    {
        var ct = req.HttpContext.RequestAborted;
        if (!long.TryParse(id, out var lid))
            throw AppException.BadRequest("id 格式不正確。");

        var row = await reader.GetByIdAsync(lid, ct);
        if (row is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Not Found.", $"AttendanceReminderLog id={id} 不存在。"));

        return new OkObjectResult(ApiResponse.Ok(row));
    }

    private static DateTime? TryParseDate(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null
            : DateTime.TryParse(s, out var d) ? d.Date : null;

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
