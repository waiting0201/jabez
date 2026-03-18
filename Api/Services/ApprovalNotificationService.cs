using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

public sealed class ApprovalNotificationService(
    AppDbContext db,
    IEmailService emailService,
    ILogger<ApprovalNotificationService> logger) : IApprovalNotificationService
{
    private static readonly Dictionary<string, string> AppTypeLabels = new()
    {
        ["payment_request"] = "請款申請",
        ["leave"]           = "請假申請",
        ["travel"]          = "出差申請",
        ["overtime"]        = "加班申請",
        ["advance"]         = "預支申請",
    };

    /// <inheritdoc />
    public async Task NotifyReviewersAsync(
        string applicationType, int applicationId, int? approvalItemId,
        int targetStepOrder, Guid applicantId)
    {
        try
        {
            if (approvalItemId is null) return;

            var step = await db.ApprovalSteps
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ApprovalItemId == approvalItemId && s.StepOrder == targetStepOrder);
            if (step is null) return;

            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            var applicantName = applicant?.Name ?? "未知";
            var applicantDeptId = applicant?.DepartmentId;

            // 根據步驟設定查找符合條件的審核者（與 AuthorizeStepAsync / StepMatchClause 一致）
            IQueryable<Models.Entities.User> query;

            // ── UseApplicantDesignated 模式：直接查指定審核者 ──
            if (step.UseApplicantDesignated)
            {
                // 取得申請表上的 DesignatedReviewerId
                Guid? designatedReviewerId = applicationType switch
                {
                    "payment_request" => (await db.PaymentRequests.AsNoTracking()
                        .Where(x => x.Id == applicationId).Select(x => x.DesignatedReviewerId).FirstOrDefaultAsync()),
                    "leave"           => (await db.LeaveRequests.AsNoTracking()
                        .Where(x => x.Id == applicationId).Select(x => x.DesignatedReviewerId).FirstOrDefaultAsync()),
                    "travel"          => (await db.TravelRequests.AsNoTracking()
                        .Where(x => x.Id == applicationId).Select(x => x.DesignatedReviewerId).FirstOrDefaultAsync()),
                    "overtime"        => (await db.OvertimeRequests.AsNoTracking()
                        .Where(x => x.Id == applicationId).Select(x => x.DesignatedReviewerId).FirstOrDefaultAsync()),
                    "advance"         => (await db.AdvanceRequests.AsNoTracking()
                        .Where(x => x.Id == applicationId).Select(x => x.DesignatedReviewerId).FirstOrDefaultAsync()),
                    _                 => null,
                };

                if (!designatedReviewerId.HasValue)
                {
                    logger.LogWarning("UseApplicantDesignated 步驟找不到 DesignatedReviewerId：{AppType} #{Id}, Step {Step}",
                        applicationType, applicationId, targetStepOrder);
                    return;
                }

                var designatedReviewer = await db.Users.AsNoTracking()
                    .Where(u => u.Id == designatedReviewerId.Value && !string.IsNullOrEmpty(u.Email))
                    .Select(u => new { u.Name, u.Email })
                    .FirstOrDefaultAsync();

                if (designatedReviewer is null)
                {
                    logger.LogWarning("指定審核者找不到或無 Email：UserId={UserId}（{AppType} #{Id}）",
                        designatedReviewerId, applicationType, applicationId);
                    return;
                }

                var label2  = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
                var summary2 = await GetSummaryAsync(applicationType, applicationId);
                var subject2 = $"[待審核] {label2} #{applicationId} — {applicantName}（指定審核）";
                var siteUrl2 = await GetSiteUrlAsync();
                var linkUrl2 = BuildReviewUrl(siteUrl2, applicationType, applicationId);
                var body2 = BuildReviewerEmail(designatedReviewer.Name, applicantName, label2, applicationId, summary2, targetStepOrder, linkUrl2);
                await emailService.SendAsync(designatedReviewer.Email!, subject2, body2);
                logger.LogInformation("已寄送指定審核通知：{Email}（{AppType} #{Id}）", designatedReviewer.Email, applicationType, applicationId);
                return;
            }

            if (step.UseDirectSupervisor)
            {
                // 上層級模式：找同部門中第 N 層上級（Level 越小 = 層級越高）
                if (applicantDeptId is null) return;
                var applicantJobTitleId = applicant?.JobTitleId;
                if (applicantJobTitleId is null) return;
                var applicantLevel = await db.JobTitles.AsNoTracking()
                    .Where(j => j.Id == applicantJobTitleId).Select(j => j.Level).FirstOrDefaultAsync();

                // 計算 rank：此步驟前有幾個 UseDirectSupervisor 步驟
                var rank = await db.ApprovalSteps.AsNoTracking()
                    .CountAsync(s => s.ApprovalItemId == approvalItemId
                        && s.UseDirectSupervisor
                        && s.StepOrder < targetStepOrder);

                // 找第 N 層上級的目標 Level
                var targetLevel = await db.Users.AsNoTracking()
                    .Where(u => u.DepartmentId == applicantDeptId && !u.IsSuperAdmin
                        && u.JobTitle != null && u.JobTitle.Level < applicantLevel)
                    .Select(u => u.JobTitle!.Level)
                    .Distinct()
                    .OrderByDescending(l => l)
                    .Skip(rank)
                    .FirstOrDefaultAsync();

                if (targetLevel == 0) return; // 找不到該層級

                query = db.Users.AsNoTracking()
                    .Where(u => u.DepartmentId == applicantDeptId && !u.IsSuperAdmin
                        && !string.IsNullOrEmpty(u.Email)
                        && u.JobTitle != null && u.JobTitle.Level == targetLevel);
            }
            else
            {
                query = db.Users.AsNoTracking()
                    .Where(u => !u.IsSuperAdmin && !string.IsNullOrEmpty(u.Email));

                if (step.JobTitleId is not null)
                    query = query.Where(u => u.JobTitleId == step.JobTitleId);

                if (step.UseApplicantDepartment)
                {
                    if (applicantDeptId is null) return; // 申請人無部門，無法配對
                    query = query.Where(u => u.DepartmentId == applicantDeptId);
                }
                else if (step.DepartmentId is not null)
                {
                    query = query.Where(u => u.DepartmentId == step.DepartmentId);
                }
            }

            var reviewers = await query.Select(u => new { u.Name, u.Email }).ToListAsync();
            if (reviewers.Count == 0)
            {
                logger.LogWarning("找不到符合條件的審核者：{AppType} #{Id}, Step {Step}",
                    applicationType, applicationId, targetStepOrder);
                return;
            }

            var label   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
            var summary = await GetSummaryAsync(applicationType, applicationId);
            var subject = $"[待審核] {label} #{applicationId} — {applicantName}";
            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = BuildReviewUrl(siteUrl, applicationType, applicationId);

            foreach (var r in reviewers)
            {
                var body = BuildReviewerEmail(r.Name, applicantName, label, applicationId, summary, targetStepOrder, linkUrl);
                await emailService.SendAsync(r.Email!, subject, body);
                logger.LogInformation("已寄送審核通知：{Email}（{AppType} #{Id}）", r.Email, applicationType, applicationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送審核通知失敗：{AppType} #{Id}", applicationType, applicationId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyApplicantAsync(
        string applicationType, int applicationId, Guid applicantId,
        string action, string? reviewNote)
    {
        try
        {
            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            if (applicant is null || string.IsNullOrEmpty(applicant.Email)) return;

            var label   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
            var summary = await GetSummaryAsync(applicationType, applicationId);

            var (tag, desc) = action switch
            {
                "approved" => ("已核准", "已通過所有審核步驟，正式核准"),
                "returned" => ("已退回", "已被退回，請修改後重新送出"),
                "rejected" => ("已拒絕", "已被拒絕"),
                _          => (action, action),
            };

            var siteUrl = await GetSiteUrlAsync();
            // 核准 → 連到簽核詳情；退回 → 連到申請編輯頁；拒絕 → 連到簽核詳情
            var (linkUrl, linkText) = action switch
            {
                "returned" => (BuildRequestUrl(siteUrl, applicationType, applicationId), "前往修改申請"),
                _          => (BuildReviewUrl(siteUrl, applicationType, applicationId), "查看詳情"),
            };

            var subject = $"[{tag}] 您的{label} #{applicationId} {tag}";
            var body    = BuildApplicantEmail(applicant.Name, label, applicationId, summary, desc, reviewNote, linkUrl, linkText);

            await emailService.SendAsync(applicant.Email, subject, body);
            logger.LogInformation("已寄送結果通知：{Email}（{AppType} #{Id} → {Action}）",
                applicant.Email, applicationType, applicationId, action);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送結果通知失敗：{AppType} #{Id}", applicationType, applicationId);
        }
    }

    /// <inheritdoc />
    public async Task NotifySpecificReviewerAsync(
        string applicationType, int applicationId,
        Guid reviewerId, Guid applicantId, bool isDelegate)
    {
        try
        {
            var reviewer  = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == reviewerId);
            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            if (reviewer is null || string.IsNullOrEmpty(reviewer.Email)) return;

            var applicantName = applicant?.Name ?? "未知";
            var label   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
            var summary = await GetSummaryAsync(applicationType, applicationId);
            var tag     = isDelegate ? "（代理審核）" : "（升級審核）";
            var subject = $"[待審核] {label} #{applicationId} — {applicantName}{tag}";
            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = BuildReviewUrl(siteUrl, applicationType, applicationId);
            var body    = BuildReviewerEmail(reviewer.Name, applicantName, label, applicationId, summary, 1, linkUrl);

            await emailService.SendAsync(reviewer.Email, subject, body);
            logger.LogInformation("已寄送升級審核通知：{Email}（{AppType} #{Id}）", reviewer.Email, applicationType, applicationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送升級審核通知失敗：{AppType} #{Id}", applicationType, applicationId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyFinanceDeptAsync(int applicationId, Guid applicantId, string applicationType = "payment_request")
    {
        try
        {
            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            var applicantName = applicant?.Name ?? "未知";
            var summary = await GetSummaryAsync(applicationType, applicationId);

            // 查詢財務部所有有 Email 的使用者
            var financeDept = await db.Departments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Name == "財務部");
            if (financeDept is null)
            {
                logger.LogWarning("找不到「財務部」部門，無法寄送撥款通知：PaymentRequest #{Id}", applicationId);
                return;
            }

            var recipients = await db.Users.AsNoTracking()
                .Where(u => u.DepartmentId == financeDept.Id && !u.IsSuperAdmin && !string.IsNullOrEmpty(u.Email))
                .Select(u => new { u.Name, u.Email })
                .ToListAsync();

            if (recipients.Count == 0)
            {
                logger.LogWarning("財務部無可通知的使用者：PaymentRequest #{Id}", applicationId);
                return;
            }

            var label   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
            var subject = $"[可撥款] {label} #{applicationId} 已核准 — {applicantName}";
            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = BuildReviewUrl(siteUrl, applicationType, applicationId);

            foreach (var r in recipients)
            {
                var body = BuildFinanceDeptEmail(r.Name, applicantName, applicationId, summary, linkUrl, label);
                await emailService.SendAsync(r.Email!, subject, body);
                logger.LogInformation("已寄送撥款通知：{Email}（{AppType} #{Id}）", r.Email, applicationType, applicationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送財務部撥款通知失敗：PaymentRequest #{Id}", applicationId);
        }
    }

    // ── 取得申請摘要 ──────────────────────────────────────────────────────────

    private async Task<string> GetSummaryAsync(string applicationType, int applicationId)
    {
        return applicationType switch
        {
            "payment_request" => await GetPaymentSummaryAsync(applicationId),
            "leave"           => await GetLeaveSummaryAsync(applicationId),
            "travel"          => await GetTravelSummaryAsync(applicationId),
            "overtime"        => await GetOvertimeSummaryAsync(applicationId),
            "advance"         => await GetAdvanceSummaryAsync(applicationId),
            _                 => $"#{applicationId}",
        };
    }

    private async Task<string> GetPaymentSummaryAsync(int id)
    {
        var pr = await db.PaymentRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.TotalAmount, ProjectCode = x.Project != null ? x.Project.Code : "" })
            .FirstOrDefaultAsync();
        return pr is not null ? $"{pr.ProjectCode}（{pr.TotalAmount:N0} 元）" : $"#{id}";
    }

    private async Task<string> GetLeaveSummaryAsync(int id)
    {
        var lr = await db.LeaveRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.LeaveType, x.Hours, x.StartDate, x.EndDate })
            .FirstOrDefaultAsync();
        return lr is not null
            ? $"{lr.LeaveType} {lr.Hours} 小時（{lr.StartDate:yyyy-MM-dd} ~ {lr.EndDate:yyyy-MM-dd}）"
            : $"#{id}";
    }

    private async Task<string> GetTravelSummaryAsync(int id)
    {
        var tr = await db.TravelRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Destination, x.StartDate, x.EndDate })
            .FirstOrDefaultAsync();
        return tr is not null
            ? $"{tr.Destination}（{tr.StartDate:yyyy-MM-dd} ~ {tr.EndDate:yyyy-MM-dd}）"
            : $"#{id}";
    }

    private async Task<string> GetOvertimeSummaryAsync(int id)
    {
        var ot = await db.OvertimeRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.OvertimeDate, x.EstimatedHours })
            .FirstOrDefaultAsync();
        return ot is not null
            ? $"{ot.OvertimeDate:yyyy-MM-dd}（{ot.EstimatedHours} 小時）"
            : $"#{id}";
    }

    private async Task<string> GetAdvanceSummaryAsync(int id)
    {
        var ar = await db.AdvanceRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.GrandTotal, x.ActivityName, ProjectCode = x.Project != null ? x.Project.Code : "" })
            .FirstOrDefaultAsync();
        return ar is not null ? $"{ar.ProjectCode} — {ar.ActivityName}（{ar.GrandTotal:N0} 元）" : $"#{id}";
    }

    // ── 取得前端網站網址 ─────────────────────────────────────────────────────────

    private async Task<string> GetSiteUrlAsync()
    {
        var setting = await db.SystemSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
        var url = setting?.SiteUrl ?? "https://admin.jabez.com";
        return url.TrimEnd('/');
    }

    /// <summary>根據申請類型產生前端審核頁面連結</summary>
    private static string BuildReviewUrl(string siteUrl, string applicationType, int applicationId)
        => $"{siteUrl}/admin/approval-tasks/{applicationType}/{applicationId}/review";

    /// <summary>根據申請類型產生前端申請詳情連結（退回/拒絕時讓申請人查看）</summary>
    private static string BuildRequestUrl(string siteUrl, string applicationType, int applicationId)
    {
        var path = applicationType switch
        {
            "payment_request" => "payment-requests",
            "leave"           => "leave-requests",
            "travel"          => "travel-requests",
            "overtime"        => "overtime-requests",
            "advance"         => "advance-requests",
            _                 => "approval-tasks",
        };
        return $"{siteUrl}/admin/{path}/{applicationId}/edit";
    }

    /// <summary>產生 HTML 按鈕</summary>
    private static string BuildButtonHtml(string url, string text)
        => $"""
            <div style="margin: 16px 0;">
              <a href="{url}" target="_blank"
                 style="display: inline-block; padding: 10px 24px; background: #699F34; color: #fff;
                        text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 14px;">
                {text}
              </a>
            </div>
            """;

    // ── Email HTML 模板 ───────────────────────────────────────────────────────

    private static string BuildReviewerEmail(
        string reviewerName, string applicantName, string label,
        int applicationId, string summary, int stepOrder, string linkUrl)
    {
        return $"""
        <div style="font-family: 'Microsoft JhengHei', 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto;">
          <div style="background: #699F34; padding: 16px 24px; border-radius: 8px 8px 0 0;">
            <h2 style="color: #fff; margin: 0; font-size: 18px;">待審核通知</h2>
          </div>
          <div style="background: #F5F2ED; padding: 24px; border-radius: 0 0 8px 8px;">
            <p style="color: #525358; margin: 0 0 16px;">{reviewerName} 您好，</p>
            <p style="color: #525358; margin: 0 0 16px;">
              <strong>{applicantName}</strong> 已提交一筆<strong>{label}</strong>，等待您的審核：
            </p>
            <table style="width: 100%; border-collapse: collapse; margin: 0 0 16px;">
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73; width: 100px;">申請編號</td>
                <td style="padding: 8px 12px; color: #525358; font-weight: 600;">#{applicationId}</td>
              </tr>
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">申請摘要</td>
                <td style="padding: 8px 12px; color: #525358;">{summary}</td>
              </tr>
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73;">目前步驟</td>
                <td style="padding: 8px 12px; color: #525358;">第 {stepOrder} 步</td>
              </tr>
            </table>
            {BuildButtonHtml(linkUrl, "前往審核")}
            <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 16px 0;" />
            <p style="color: #A39685; font-size: 12px; margin: 0;">此信件由系統自動寄發，請勿直接回覆。</p>
          </div>
        </div>
        """;
    }

    private static string BuildFinanceDeptEmail(
        string recipientName, string applicantName, int applicationId, string summary, string linkUrl, string label = "請款申請")
    {
        return $"""
        <div style="font-family: 'Microsoft JhengHei', 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto;">
          <div style="background: #699F34; padding: 16px 24px; border-radius: 8px 8px 0 0;">
            <h2 style="color: #fff; margin: 0; font-size: 18px;">{label}核准 — 可進行撥款</h2>
          </div>
          <div style="background: #F5F2ED; padding: 24px; border-radius: 0 0 8px 8px;">
            <p style="color: #525358; margin: 0 0 16px;">{recipientName} 您好，</p>
            <p style="color: #525358; margin: 0 0 16px;">
              <strong>{applicantName}</strong> 的{label}已通過所有簽核步驟，請進行撥款作業：
            </p>
            <table style="width: 100%; border-collapse: collapse; margin: 0 0 16px;">
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73; width: 100px;">申請編號</td>
                <td style="padding: 8px 12px; color: #525358; font-weight: 600;">#{applicationId}</td>
              </tr>
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">申請摘要</td>
                <td style="padding: 8px 12px; color: #525358;">{summary}</td>
              </tr>
            </table>
            {BuildButtonHtml(linkUrl, "前往設定撥款日期")}
            <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 16px 0;" />
            <p style="color: #A39685; font-size: 12px; margin: 0;">此信件由系統自動寄發，請勿直接回覆。</p>
          </div>
        </div>
        """;
    }

    private static string BuildApplicantEmail(
        string applicantName, string label, int applicationId,
        string summary, string description, string? reviewNote, string linkUrl, string linkText)
    {
        var noteHtml = string.IsNullOrWhiteSpace(reviewNote)
            ? ""
            : $"""
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">審核意見</td>
                <td style="padding: 8px 12px; color: #525358;">{System.Net.WebUtility.HtmlEncode(reviewNote)}</td>
              </tr>
              """;

        return $"""
        <div style="font-family: 'Microsoft JhengHei', 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto;">
          <div style="background: #699F34; padding: 16px 24px; border-radius: 8px 8px 0 0;">
            <h2 style="color: #fff; margin: 0; font-size: 18px;">審核結果通知</h2>
          </div>
          <div style="background: #F5F2ED; padding: 24px; border-radius: 0 0 8px 8px;">
            <p style="color: #525358; margin: 0 0 16px;">{applicantName} 您好，</p>
            <p style="color: #525358; margin: 0 0 16px;">
              您的<strong>{label} #{applicationId}</strong> {description}。
            </p>
            <table style="width: 100%; border-collapse: collapse; margin: 0 0 16px;">
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73; width: 100px;">申請編號</td>
                <td style="padding: 8px 12px; color: #525358; font-weight: 600;">#{applicationId}</td>
              </tr>
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">申請摘要</td>
                <td style="padding: 8px 12px; color: #525358;">{summary}</td>
              </tr>
              {noteHtml}
            </table>
            {BuildButtonHtml(linkUrl, linkText)}
            <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 16px 0;" />
            <p style="color: #A39685; font-size: 12px; margin: 0;">此信件由系統自動寄發，請勿直接回覆。</p>
          </div>
        </div>
        """;
    }
}
