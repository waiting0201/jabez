using System.Security.Claims;
using Jabez.Api.Common;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// 打卡提醒 admin 端點：手動觸發（僅 Superadmin，供本地/Production 除錯驗證）。
/// 路由白名單由 AppRouter.IsSuperAdminRoute 守門。
/// </summary>
public sealed class AttendanceReminderAdminHandler(IAttendanceReminderService service)
{
    /// <summary>
    /// POST /admin/attendance-reminder/run?type=clockIn|clockOut
    /// 繞過時點與週末檢查，強制對符合條件的員工推播。
    /// </summary>
    public async Task<IActionResult> RunAsync(HttpRequest req)
    {
        var type = req.Query["type"].ToString();
        if (string.IsNullOrWhiteSpace(type))
            throw AppException.BadRequest("必須提供 ?type=clockIn|clockOut");

        if (type is not ("clockIn" or "clockOut"))
            throw AppException.BadRequest("type 必須為 clockIn 或 clockOut");

        // 從 JWT sub claim 取得觸發者 (Superadmin) 用以記錄至 AttendanceReminderLogs
        Guid? triggeredByUserId = null;
        var sub = req.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? req.HttpContext.User.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var uid)) triggeredByUserId = uid;

        var result = await service.ForceRunAsync(type, triggeredByUserId);
        return new OkObjectResult(ApiResponse.Ok(new
        {
            type,
            recipientCount = result.RecipientCount,
            pushedCount    = result.PushedCount,
            failureCount   = result.FailureCount,
            batchId        = result.BatchId,
        }));
    }
}
