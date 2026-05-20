using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Jabez.Api.Handlers;

/// <summary>
/// 鈴噹通知聚合端點：
/// GET /me/notification-counts → 回傳兩段式件數
///   - approvals  ：待我簽核（依申請類型分組）。重用 IPaymentRequestReadService.GetApprovalTasksAsync 的 reviewer 過濾邏輯
///   - myRequests ：我送出的進行中申請（pending / returned，依申請類型分組）
/// </summary>
public sealed class NotificationHandler(
    AppDbContext                db,
    IJwtService                 jwtService,
    IPaymentRequestReadService  approvalTaskReader,
    INotificationReadService    notificationReader)
{
    public async Task<IActionResult> GetMyCountsAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req);
        if (principal is null)
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized."));

        var userIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Invalid token claims."));

        // ── approvals 段：沿用 approval-tasks 既有過濾邏輯，再依 ApplicationType group ──
        // 非 Superadmin 才需要 jobTitleId / deptId 過濾；Superadmin 可看全部
        int?  jobTitleId     = null;
        int?  deptId         = null;
        Guid? reviewerUserId = userId;
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is not null && !user.IsSuperAdmin)
        {
            jobTitleId = user.JobTitleId;
            deptId     = user.DepartmentId;
        }

        var pendingTasks = await approvalTaskReader.GetApprovalTasksAsync(
            reviewerJobTitleId: jobTitleId,
            reviewerDepartmentId: deptId,
            status: "pending",
            reviewerUserId: reviewerUserId);

        var approvalCounts = pendingTasks
            .GroupBy(t => t.ApplicationType)
            .ToDictionary(g => g.Key, g => g.Count());

        // ── myRequests 段：我送出且狀態為 pending / returned 的件數 ──
        var myRequestCounts = await notificationReader.GetMyRequestCountsByTypeAsync(userId);

        // 確保 9 種類型 key 都存在（缺值補 0），方便前端固定排列
        var result = new NotificationCountsDto(
            Approvals:  FillMissingTypes(approvalCounts),
            MyRequests: FillMissingTypes(myRequestCounts));

        return new OkObjectResult(ApiResponse.Ok(result));
    }

    private static IReadOnlyDictionary<string, int> FillMissingTypes(IReadOnlyDictionary<string, int> source)
    {
        var dict = new Dictionary<string, int>(source);
        foreach (var t in ApprovalTaskHandler.ValidAppTypes)
        {
            if (!dict.ContainsKey(t)) dict[t] = 0;
        }
        return dict;
    }
}
