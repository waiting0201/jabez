using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

public sealed class PaymentReminderService(
    AppDbContext db,
    IPaymentReminderReadService reader,
    IApprovalNotificationService notifier,
    ILogger<PaymentReminderService> logger) : IPaymentReminderService
{
    private static readonly Dictionary<string, string> AppTypeLabels = new()
    {
        ["payment_request"] = "請款申請",
        ["advance"]         = "預支申請",
        ["travel"]          = "出差預支",
        ["travel_payment"]  = "出差請款",
    };

    public async Task<PaymentReminderRunResult> RunAsync(string triggerSource, Guid? triggeredByUserId = null)
    {
        var batchId       = Guid.NewGuid();
        var nowUtc        = DateTime.UtcNow;
        var taipeiTz      = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        var nowTaipei     = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, taipeiTz);
        var todayTaipei   = DateOnly.FromDateTime(nowTaipei);

        // 讀設定：提前 N 天
        var setting = await db.SystemSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
        var daysBefore = Math.Clamp(setting?.PaymentReminderDaysBefore ?? 3, 0, 30);
        var toDate    = todayTaipei.AddDays(daysBefore);

        // 撈待撥 installments
        var upcoming = await reader.GetUpcomingAsync(todayTaipei, toDate);
        logger.LogInformation("[PaymentReminder] batch={Batch} 撈到 {Count} 筆待提醒 installments（範圍 {From} ~ {To}, 提前 {Days} 天）",
            batchId, upcoming.Count, todayTaipei, toDate, daysBefore);

        // 撈財務部全員（active + Department.Code 屬於 FinancialAndAbove）
        var financeUsers = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.Status == "active"
                     && !u.IsSuperAdmin
                     && u.Department != null
                     && u.Department.Code != null
                     && DepartmentCodes.FinancialAndAbove.Contains(u.Department.Code))
            .ToListAsync();

        // 寫 batchStart log（無論有無對象都記）
        var batchStartLog = new PaymentReminderLog
        {
            BatchId            = batchId,
            TickedAt           = nowUtc,
            TickedAtTaipei     = nowTaipei,
            ReminderDateTaipei = todayTaipei,
            TriggerSource      = triggerSource,
            TriggeredByUserId  = triggeredByUserId,
            ItemCount          = upcoming.Count,
            Status             = "batchStart",
            CreatedAt          = nowUtc,
        };
        db.PaymentReminderLogs.Add(batchStartLog);
        await db.SaveChangesAsync();

        int success = 0, skipped = 0, failure = 0;

        // 若無待撥則只記 batchStart 不推
        if (upcoming.Count == 0 || financeUsers.Count == 0)
        {
            return new PaymentReminderRunResult(batchId, upcoming.Count, financeUsers.Count, 0, 0, 0);
        }

        // 組通知 items（轉成通用 tuple 給 notifier）
        var items = upcoming
            .Select(u => (
                AppType:        u.ApplicationType,
                AppLabel:       AppTypeLabels.GetValueOrDefault(u.ApplicationType, u.ApplicationType),
                ApplicationId:  u.ApplicationId,
                Applicant:      u.ApplicantName ?? "—",
                ExpectedDate:   u.ExpectedDate,
                Amount:         u.Amount))
            .ToList()
            .AsReadOnly();

        foreach (var fu in financeUsers)
        {
            var startMs = Environment.TickCount;

            // 同日去重：若該財務人員今日已 success 推過，跳過
            var alreadySent = await db.PaymentReminderLogs.AsNoTracking()
                .AnyAsync(l => l.FinanceUserId == fu.Id
                            && l.ReminderDateTaipei == todayTaipei
                            && l.Status == "success");
            if (alreadySent)
            {
                db.PaymentReminderLogs.Add(new PaymentReminderLog
                {
                    BatchId            = batchId,
                    TickedAt           = nowUtc,
                    TickedAtTaipei     = nowTaipei,
                    ReminderDateTaipei = todayTaipei,
                    TriggerSource      = triggerSource,
                    TriggeredByUserId  = triggeredByUserId,
                    FinanceUserId      = fu.Id,
                    UserNameSnapshot   = fu.Name,
                    LineUserIdSnapshot = fu.LineUserId,
                    ItemCount          = items.Count,
                    Status             = "skipped_already_sent",
                    DurationMs         = Environment.TickCount - startMs,
                    CreatedAt          = nowUtc,
                });
                skipped++;
                continue;
            }

            var (emailSent, lineSent, err) = await notifier.NotifyFinanceUpcomingPaymentsAsync(fu.Id, items);
            var status = (emailSent || lineSent) ? "success" : "failure";
            if (status == "success") success++; else failure++;

            db.PaymentReminderLogs.Add(new PaymentReminderLog
            {
                BatchId            = batchId,
                TickedAt           = nowUtc,
                TickedAtTaipei     = nowTaipei,
                ReminderDateTaipei = todayTaipei,
                TriggerSource      = triggerSource,
                TriggeredByUserId  = triggeredByUserId,
                FinanceUserId      = fu.Id,
                UserNameSnapshot   = fu.Name,
                LineUserIdSnapshot = fu.LineUserId,
                ItemCount          = items.Count,
                Status             = status,
                ErrorMessage       = err?.Length > 500 ? err[..500] : err,
                DurationMs         = Environment.TickCount - startMs,
                CreatedAt          = nowUtc,
            });
        }

        await db.SaveChangesAsync();

        logger.LogInformation("[PaymentReminder] batch={Batch} 完成：success={Success}, skipped={Skip}, failure={Fail}",
            batchId, success, skipped, failure);

        return new PaymentReminderRunResult(batchId, upcoming.Count, financeUsers.Count, success, skipped, failure);
    }
}
