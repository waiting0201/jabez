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

        var count = await service.ForceRunAsync(type);
        return new OkObjectResult(ApiResponse.Ok(new { type, pushedCount = count }));
    }
}
