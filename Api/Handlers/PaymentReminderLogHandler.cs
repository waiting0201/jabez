using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Jabez.Api.Handlers;

/// <summary>
/// 撥款提醒紀錄查詢 + 手動觸發端點（僅 Superadmin，由 AppRouter.IsSuperAdminRoute 守門）。
/// </summary>
public sealed class PaymentReminderLogHandler(
    AppDbContext db,
    IPaymentReminderService reminder,
    IJwtService jwtService)
{
    /// <summary>GET /admin/payment-reminder-logs?from=&to=&status=&triggerSource=&financeUserId=&page=&pageSize=</summary>
    public async Task<IActionResult> GetPagedAsync(HttpRequest req)
    {
        DateTime? from = TryParseDate(req.Query["from"]);
        DateTime? to   = TryParseDate(req.Query["to"]);
        if (from is null && to is null)
        {
            to   = Clock.Today;
            from = to.Value.AddDays(-6);
        }
        if (from is not null && to is not null && (to.Value - from.Value).TotalDays > 92)
            throw AppException.BadRequest("查詢區間最長 92 天。");

        var fromDate = from.HasValue ? DateOnly.FromDateTime(from.Value) : (DateOnly?)null;
        var toDate   = to.HasValue   ? DateOnly.FromDateTime(to.Value)   : (DateOnly?)null;

        string? status = NullIfEmpty(req.Query["status"]);
        string? source = NullIfEmpty(req.Query["triggerSource"]);
        Guid?   userId = Guid.TryParse(req.Query["financeUserId"], out var uid) ? uid : null;

        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;

        var q = db.PaymentReminderLogs.AsNoTracking()
                  .Include(l => l.FinanceUser)
                  .Include(l => l.TriggeredByUser)
                  .AsQueryable();
        if (fromDate.HasValue) q = q.Where(l => l.ReminderDateTaipei >= fromDate.Value);
        if (toDate.HasValue)   q = q.Where(l => l.ReminderDateTaipei <= toDate.Value);
        if (status is not null) q = q.Where(l => l.Status == status);
        if (source is not null) q = q.Where(l => l.TriggerSource == source);
        if (userId.HasValue)    q = q.Where(l => l.FinanceUserId == userId);

        int total = await q.CountAsync();
        var rows  = await q.OrderByDescending(l => l.TickedAt)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .Select(l => new
                           {
                               l.Id,
                               l.BatchId,
                               l.TickedAt,
                               l.TickedAtTaipei,
                               l.ReminderDateTaipei,
                               l.TriggerSource,
                               TriggeredByUserName = l.TriggeredByUser != null ? l.TriggeredByUser.Name : null,
                               l.FinanceUserId,
                               FinanceUserName     = l.FinanceUser != null ? l.FinanceUser.Name : l.UserNameSnapshot,
                               l.UserNameSnapshot,
                               l.LineUserIdSnapshot,
                               l.ItemCount,
                               l.Status,
                               l.ErrorCategory,
                               l.ErrorMessage,
                               l.HttpStatusCode,
                               l.DurationMs,
                               l.CreatedAt,
                           })
                           .ToListAsync();

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new OkObjectResult(ApiResponse.Ok(new
        {
            items      = rows,
            totalCount = total,
            page,
            pageSize,
            totalPages = Math.Max(1, totalPages),
        }));
    }

    /// <summary>POST /admin/payment-reminder/run — Superadmin 手動觸發（除錯/驗證用）</summary>
    public async Task<IActionResult> ManualRunAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req)
            ?? throw AppException.Unauthorized();
        Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId);

        var result = await reminder.RunAsync(triggerSource: "manual", triggeredByUserId: userId);
        return new OkObjectResult(ApiResponse.Ok(result, "撥款提醒已手動執行。"));
    }

    private static DateTime? TryParseDate(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null
            : DateTime.TryParse(s, out var d) ? d.Date : null;

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
