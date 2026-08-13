using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jabez.Api.Services;

public sealed class ApprovalNotificationService(
    AppDbContext db,
    IEmailService emailService,
    ILineService lineService,
    ILogger<ApprovalNotificationService> logger) : IApprovalNotificationService
{
    private static readonly Dictionary<string, string> AppTypeLabels = new()
    {
        ["payment_request"] = "請款申請",
        ["leave"]           = "請假申請",
        ["leave_revocation"] = "銷假申請",
        ["travel"]          = "出差預支申請",
        ["overtime"]        = "加班申請",
        ["advance"]         = "預支申請",
        ["write_off"]       = "預支沖銷申請",
        ["travel_write_off"] = "出差預支沖銷申請",
        ["travel_payment"]  = "出差請款申請",
        ["pre_review"]      = "預審申請",
    };

    /// <inheritdoc />
    public async Task NotifyReviewersAsync(
        string applicationType, int applicationId, int? approvalItemId,
        int targetStepOrder, Guid applicantId)
    {
        try
        {
            if (approvalItemId is null) return;

            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();

            var step = await db.ApprovalSteps
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ApprovalItemId == approvalItemId && s.StepOrder == targetStepOrder);
            if (step is null) return;

            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            var applicantName = applicant?.Name ?? "未知";
            var applicantDeptId = applicant?.DepartmentId;

            // 根據步驟設定查找符合條件的審核者（與 AuthorizeStepAsync / StepMatchClause 一致）
            IQueryable<Models.Entities.User> query;

            // 取得歷史已審者集合（最近一次 returned 之後的 approved）— 用於排除重複通知
            var approvedIds = await GetApprovedReviewerIdsAsync(applicationType, applicationId);

            // ── 指定審核模式：查詢 RequestDesignatedReviewers 表找「本步驟」當前 pending 最小 StepOrder 的審核者 ──
            // 是否為指定審核步驟＝原生 UseApplicantDesignated 或該申請已有 designee 綁定本步驟（例外指定審核命中的快照）
            bool isDesignatedStep = step.UseApplicantDesignated
                || await db.RequestDesignatedReviewers.AsNoTracking()
                    .AnyAsync(r => r.RequestType == applicationType
                                && r.RequestId == applicationId
                                && r.ApprovalStepOrder == targetStepOrder);

            if (isDesignatedStep)
            {
                // 必須以 ApprovalStepOrder 綁定本步驟，否則多指定步驟時會撈到前一步殘留的 pending designee
                var currentDesignated = await db.RequestDesignatedReviewers
                    .AsNoTracking()
                    .Where(r => r.RequestType == applicationType
                             && r.RequestId == applicationId
                             && r.ApprovalStepOrder == targetStepOrder
                             && r.Status == "pending")
                    .OrderBy(r => r.StepOrder)
                    .Select(r => new { r.ReviewerId, r.StepOrder })
                    .FirstOrDefaultAsync();

                if (currentDesignated is null)
                {
                    logger.LogWarning("指定審核步驟找不到 pending 的指定審核者：{AppType} #{Id}, Step {Step}",
                        applicationType, applicationId, targetStepOrder);
                    return;
                }

                // 跨步驟同人去重：當前指定審核者若已在歷史中審過，不再通知（自動代簽由 ProcessReviewAsync 處理）
                if (approvedIds.Contains(currentDesignated.ReviewerId))
                {
                    logger.LogInformation("指定審核者已於先前步驟審核，跳過通知：UserId={UserId}（{AppType} #{Id}）",
                        currentDesignated.ReviewerId, applicationType, applicationId);
                    return;
                }

                var designatedReviewer = await db.Users.AsNoTracking()
                    .Where(u => u.Id == currentDesignated.ReviewerId && !string.IsNullOrEmpty(u.Email))
                    .Select(u => new { u.Name, u.Email })
                    .FirstOrDefaultAsync();

                if (designatedReviewer is null)
                {
                    logger.LogWarning("指定審核者找不到或無 Email：UserId={UserId}（{AppType} #{Id}）",
                        currentDesignated.ReviewerId, applicationType, applicationId);
                    return;
                }

                var label2   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
                var summary2 = await GetSummaryAsync(applicationType, applicationId);
                var subject2 = $"[待審核] {label2} #{applicationId} — {applicantName}（指定審核）";
                var siteUrl2 = await GetSiteUrlAsync();
                var linkUrl2 = BuildReviewUrl(siteUrl2, applicationType, applicationId);
                var body2    = BuildReviewerEmail(designatedReviewer.Name, applicantName, label2, applicationId, summary2, targetStepOrder, linkUrl2);
                if (emailEnabled)
                    await emailService.SendAsync(designatedReviewer.Email!, subject2, body2);
                await PushLineByEmailAsync(designatedReviewer.Email!,
                    LineFlexMessageBuilder.BuildSpecificReviewerMessage(applicantName, label2, applicationId, summary2, "指定審核", linkUrl2),
                    lineEnabled);
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

            // 跨步驟同人去重：排除已在歷史中審過此申請者
            if (approvedIds.Count > 0)
                query = query.Where(u => !approvedIds.Contains(u.Id));

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

            // 一次查出所有 reviewer 的 LINE userId
            var reviewerEmails = reviewers.Select(r => r.Email!).ToArray();
            var lineMap = await db.Users.AsNoTracking()
                .Where(u => reviewerEmails.Contains(u.Email) && u.LineUserId != null)
                .Select(u => new { u.Email, u.LineUserId })
                .ToDictionaryAsync(u => u.Email!, u => u.LineUserId!);

            foreach (var r in reviewers)
            {
                var body = BuildReviewerEmail(r.Name, applicantName, label, applicationId, summary, targetStepOrder, linkUrl);
                if (emailEnabled)
                    await emailService.SendAsync(r.Email!, subject, body);

                if (lineEnabled && lineMap.TryGetValue(r.Email!, out var lineUid))
                {
                    try
                    {
                        var flex = LineFlexMessageBuilder.BuildReviewerMessage(applicantName, label, applicationId, summary, targetStepOrder, linkUrl);
                        await lineService.PushMessageAsync(lineUid, flex);
                    }
                    catch (Exception lex) { logger.LogWarning(lex, "LINE 推播失敗：{LineUserId}", lineUid); }
                }

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
        string action, string? reviewNote, string? contextLabel = null)
    {
        try
        {
            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();

            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            if (applicant is null || string.IsNullOrEmpty(applicant.Email)) return;

            var label   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType) + (contextLabel ?? "");
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

            if (emailEnabled)
                await emailService.SendAsync(applicant.Email, subject, body);
            await PushLineByUserIdAsync(applicantId,
                LineFlexMessageBuilder.BuildApplicantResultMessage(label, applicationId, action, reviewNote, linkUrl),
                lineEnabled);
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
            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();

            // 跨步驟同人去重：被通知者若已在歷史中審過，跳過通知（自動代簽由 ProcessReviewAsync 處理）
            var approvedIds = await GetApprovedReviewerIdsAsync(applicationType, applicationId);
            if (approvedIds.Contains(reviewerId))
            {
                logger.LogInformation("特定審核者已於先前步驟審核，跳過通知：UserId={UserId}（{AppType} #{Id}）",
                    reviewerId, applicationType, applicationId);
                return;
            }

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

            if (emailEnabled)
                await emailService.SendAsync(reviewer.Email, subject, body);
            var suffix = isDelegate ? "代理審核" : "升級審核";
            await PushLineByUserIdAsync(reviewerId,
                LineFlexMessageBuilder.BuildSpecificReviewerMessage(applicantName, label, applicationId, summary, suffix, linkUrl),
                lineEnabled);
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
            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();

            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            var applicantName = applicant?.Name ?? "未知";
            var summary = await GetSummaryAsync(applicationType, applicationId);

            // 查詢財務管理部所有有 Email 的使用者
            // 以 DepartmentCodes.FinanceStep 比對（含舊短碼 FIN + 改制後英文全名）；
            // 原本硬編碼 == "FIN"，組織改制後查無部門，通知會靜默不送只留一行 warning。
            var recipients = await db.Users.AsNoTracking()
                .Where(u => u.Department != null && u.Department.Code != null
                         && DepartmentCodes.FinanceStep.Contains(u.Department.Code)
                         && !u.IsSuperAdmin && !string.IsNullOrEmpty(u.Email))
                .Select(u => new { u.Name, u.Email })
                .ToListAsync();

            if (recipients.Count == 0)
            {
                logger.LogWarning("財務部(FIN)無可通知的使用者：PaymentRequest #{Id}", applicationId);
                return;
            }

            var label   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
            var subject = $"[可撥款] {label} #{applicationId} 已核准 — {applicantName}";
            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = BuildReviewUrl(siteUrl, applicationType, applicationId);

            var finEmails = recipients.Select(r => r.Email!).ToArray();
            var finLineMap = await db.Users.AsNoTracking()
                .Where(u => finEmails.Contains(u.Email) && u.LineUserId != null)
                .Select(u => new { u.Email, u.LineUserId })
                .ToDictionaryAsync(u => u.Email!, u => u.LineUserId!);

            foreach (var r in recipients)
            {
                var body = BuildFinanceDeptEmail(r.Name, applicantName, applicationId, summary, linkUrl, label);
                if (emailEnabled)
                    await emailService.SendAsync(r.Email!, subject, body);

                if (lineEnabled && finLineMap.TryGetValue(r.Email!, out var lineUid))
                {
                    try
                    {
                        var flex = LineFlexMessageBuilder.BuildFinanceDeptMessage(applicantName, label, applicationId, summary, linkUrl);
                        await lineService.PushMessageAsync(lineUid, flex);
                    }
                    catch (Exception lex) { logger.LogWarning(lex, "LINE 推播失敗：{LineUserId}", lineUid); }
                }

                logger.LogInformation("已寄送撥款通知：{Email}（{AppType} #{Id}）", r.Email, applicationType, applicationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送財務部(FIN)撥款通知失敗：PaymentRequest #{Id}", applicationId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyFinanceRefundAsync(AdvanceRequest advance, decimal refundAmount)
    {
        try
        {
            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();

            var applicant = advance.SubmittedById.HasValue
                ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == advance.SubmittedById.Value)
                : null;
            var applicantName = applicant?.Name ?? "未知";

            // 收件人同 NotifyFinancePaymentAsync：以 DepartmentCodes.FinanceStep 比對，不可硬編碼 "FIN"
            var recipients = await db.Users.AsNoTracking()
                .Where(u => u.Department != null && u.Department.Code != null
                         && DepartmentCodes.FinanceStep.Contains(u.Department.Code)
                         && !u.IsSuperAdmin && !string.IsNullOrEmpty(u.Email))
                .Select(u => new { u.Name, u.Email })
                .ToListAsync();

            if (recipients.Count == 0) return;

            var subject = $"[需匯款] 預支申請 #{advance.Id} 沖銷超額 — 差額 {refundAmount:N0} 元";
            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = BuildReviewUrl(siteUrl, "advance", advance.Id);

            var refundEmails = recipients.Select(r => r.Email!).ToArray();
            var refundLineMap = await db.Users.AsNoTracking()
                .Where(u => refundEmails.Contains(u.Email) && u.LineUserId != null)
                .Select(u => new { u.Email, u.LineUserId })
                .ToDictionaryAsync(u => u.Email!, u => u.LineUserId!);

            foreach (var r in recipients)
            {
                var body = BuildRefundEmail(r.Name, applicantName, advance.Id, advance.RequestNo,
                    advance.GrandTotal, refundAmount, linkUrl);
                if (emailEnabled)
                    await emailService.SendAsync(r.Email!, subject, body);

                if (lineEnabled && refundLineMap.TryGetValue(r.Email!, out var lineUid))
                {
                    try
                    {
                        var flex = LineFlexMessageBuilder.BuildRefundMessage(applicantName, advance.RequestNo,
                            advance.GrandTotal, refundAmount, linkUrl);
                        await lineService.PushMessageAsync(lineUid, flex);
                    }
                    catch (Exception lex) { logger.LogWarning(lex, "LINE 推播失敗：{LineUserId}", lineUid); }
                }

                logger.LogInformation("已寄送退款通知：{Email}（AdvanceRequest #{Id}）", r.Email, advance.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送退款通知失敗：AdvanceRequest #{Id}", advance.Id);
        }
    }

    /// <inheritdoc />
    public async Task NotifyFinanceTravelRefundAsync(TravelRequest travel, decimal refundAmount)
    {
        try
        {
            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();

            var applicant = travel.EmployeeId.HasValue
                ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == travel.EmployeeId.Value)
                : null;
            var applicantName = applicant?.Name ?? "未知";

            // 收件人同 NotifyFinancePaymentAsync：以 DepartmentCodes.FinanceStep 比對，不可硬編碼 "FIN"
            var recipients = await db.Users.AsNoTracking()
                .Where(u => u.Department != null && u.Department.Code != null
                         && DepartmentCodes.FinanceStep.Contains(u.Department.Code)
                         && !u.IsSuperAdmin && !string.IsNullOrEmpty(u.Email))
                .Select(u => new { u.Name, u.Email })
                .ToListAsync();

            if (recipients.Count == 0) return;

            var subject = $"[需匯款] 出差申請 #{travel.Id} 沖銷超額 — 差額 {refundAmount:N0} 元";
            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = BuildReviewUrl(siteUrl, "travel", travel.Id);

            var trvEmails = recipients.Select(r => r.Email!).ToArray();
            var trvLineMap = await db.Users.AsNoTracking()
                .Where(u => trvEmails.Contains(u.Email) && u.LineUserId != null)
                .Select(u => new { u.Email, u.LineUserId })
                .ToDictionaryAsync(u => u.Email!, u => u.LineUserId!);

            foreach (var r in recipients)
            {
                var body = BuildTravelRefundEmail(r.Name, applicantName, travel.Id,
                    travel.Destination, travel.GrandTotal, refundAmount, linkUrl);
                if (emailEnabled)
                    await emailService.SendAsync(r.Email!, subject, body);

                if (lineEnabled && trvLineMap.TryGetValue(r.Email!, out var lineUid))
                {
                    try
                    {
                        var flex = LineFlexMessageBuilder.BuildTravelRefundMessage(applicantName,
                            travel.Destination, travel.GrandTotal, refundAmount, linkUrl);
                        await lineService.PushMessageAsync(lineUid, flex);
                    }
                    catch (Exception lex) { logger.LogWarning(lex, "LINE 推播失敗：{LineUserId}", lineUid); }
                }

                logger.LogInformation("已寄送出差退款通知：{Email}（TravelRequest #{Id}）", r.Email, travel.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送出差退款通知失敗：TravelRequest #{Id}", travel.Id);
        }
    }

    /// <inheritdoc />
    public async Task NotifyApplicantPaidAsync(
        string applicationType, int applicationId, Guid applicantId, decimal amount, DateTime paidAt,
        int? installmentNo = null, int? totalInstallments = null)
    {
        try
        {
            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();

            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            if (applicant is null || string.IsNullOrEmpty(applicant.Email)) return;

            var label   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
            var summary = await GetSummaryAsync(applicationType, applicationId);
            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = BuildReviewUrl(siteUrl, applicationType, applicationId);

            // 分期撥款時主旨附「第 N/M 期」
            var installmentSuffix = installmentNo.HasValue && totalInstallments.HasValue
                ? $"（第 {installmentNo}/{totalInstallments} 期）"
                : "";

            var subject = $"[已撥款] 您的{label} #{applicationId} 已撥款 — {amount:N0} 元{installmentSuffix}";
            var body    = BuildApplicantPaidEmail(applicant.Name, label, applicationId, summary, amount, paidAt, linkUrl, installmentNo, totalInstallments);

            if (emailEnabled)
                await emailService.SendAsync(applicant.Email, subject, body);
            await PushLineByUserIdAsync(applicantId,
                LineFlexMessageBuilder.BuildApplicantPaidMessage(label, applicationId, amount, paidAt, linkUrl, installmentNo, totalInstallments),
                lineEnabled);
            logger.LogInformation("已寄送撥款完成通知：{Email}（{AppType} #{Id}{Suffix}）",
                applicant.Email, applicationType, applicationId, installmentSuffix);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送撥款完成通知失敗：{AppType} #{Id}", applicationType, applicationId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyApplicantRefundedAsync(
        string applicationType, int applicationId, Guid applicantId, decimal refundAmount, DateTime refundedAt)
    {
        try
        {
            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();

            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == applicantId);
            if (applicant is null || string.IsNullOrEmpty(applicant.Email)) return;

            var label   = AppTypeLabels.GetValueOrDefault(applicationType, applicationType);
            var summary = await GetSummaryAsync(applicationType, applicationId);
            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = BuildReviewUrl(siteUrl, applicationType, applicationId);

            var subject = $"[已退款] 您的{label} #{applicationId} 退款已匯款 — {refundAmount:N0} 元";
            var body    = BuildApplicantRefundedEmail(applicant.Name, label, applicationId, summary, refundAmount, refundedAt, linkUrl);

            if (emailEnabled)
                await emailService.SendAsync(applicant.Email, subject, body);
            await PushLineByUserIdAsync(applicantId,
                LineFlexMessageBuilder.BuildApplicantRefundedMessage(label, applicationId, refundAmount, refundedAt, linkUrl),
                lineEnabled);
            logger.LogInformation("已寄送退款完成通知：{Email}（{AppType} #{Id}）",
                applicant.Email, applicationType, applicationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送退款完成通知失敗：{AppType} #{Id}", applicationType, applicationId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyLeaveAgentAsync(int leaveRequestId)
    {
        try
        {
            var lr = await db.LeaveRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == leaveRequestId);
            if (lr?.AgentUserId is null) return;

            var (emailEnabled, _) = await ReadNotificationFlagsAsync();
            if (!emailEnabled) return;

            var agent = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == lr.AgentUserId);
            if (agent is null || string.IsNullOrEmpty(agent.Email)) return;

            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == lr.EmployeeId);
            var applicantName = applicant?.Name ?? "同仁";
            var period  = $"{lr.StartDate:yyyy/MM/dd} ~ {lr.EndDate:yyyy/MM/dd}";
            var siteUrl = await GetSiteUrlAsync();

            var subject = $"[職務代理] 您被指定為 {applicantName} 的職務代理人";
            var body = $"""
                <p>{agent.Name} 您好，</p>
                <p>{applicantName} 已提出請假申請，並指定您於下列期間擔任職務代理人：</p>
                <ul>
                  <li>請假期間：{period}</li>
                  <li>事由：{System.Net.WebUtility.HtmlEncode(lr.Reason)}</li>
                </ul>
                <p>請留意於該期間協助代理相關職務。此通知僅供知會，您不需要於系統中進行任何簽核動作。</p>
                {BuildButtonHtml($"{siteUrl}/admin/leave-requests", "前往請假管理")}
                """;

            await emailService.SendAsync(agent.Email, subject, body);
            logger.LogInformation("已通知職務代理人：{Email}（Leave #{Id}）", agent.Email, leaveRequestId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "通知職務代理人失敗：Leave #{Id}", leaveRequestId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyLeaveRevocationAgentAsync(int revocationId)
    {
        try
        {
            var rv = await db.LeaveRevocations.AsNoTracking()
                .Include(x => x.LeaveRequest)
                .Include(x => x.Dates)
                .FirstOrDefaultAsync(x => x.Id == revocationId);
            var lr = rv?.LeaveRequest;
            if (lr?.AgentUserId is null) return;

            var (emailEnabled, _) = await ReadNotificationFlagsAsync();
            if (!emailEnabled) return;

            var agent = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == lr.AgentUserId);
            if (agent is null || string.IsNullOrEmpty(agent.Email)) return;

            var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == lr.EmployeeId);
            var applicantName = applicant?.Name ?? "同仁";
            var cancelledDates = string.Join("、", rv!.Dates.OrderBy(d => d.Date).Select(d => $"{d.Date:yyyy/MM/dd}"));
            var isFullyCancelled = lr.ApprovalStatus == "cancelled";
            var siteUrl = await GetSiteUrlAsync();

            var subject = isFullyCancelled
                ? $"[職務代理] {applicantName} 已銷假，代理職務解除"
                : $"[職務代理] {applicantName} 已部分銷假，代理期間調整";
            var body = $"""
                <p>{agent.Name} 您好，</p>
                <p>{applicantName} 的請假申請（{lr.StartDate:yyyy/MM/dd} ~ {lr.EndDate:yyyy/MM/dd}）已辦理銷假並完成簽核：</p>
                <ul>
                  <li>取消的請假日：{cancelledDates}</li>
                  <li>{(isFullyCancelled ? "整張假單已全數取消，您不需再代理相關職務。" : $"其餘期間仍需代理，剩餘請假時數 {lr.Hours} 小時。")}</li>
                </ul>
                <p>此通知僅供知會，您不需要於系統中進行任何簽核動作。</p>
                {BuildButtonHtml($"{siteUrl}/admin/leave-requests", "前往請假管理")}
                """;

            await emailService.SendAsync(agent.Email, subject, body);
            logger.LogInformation("已通知職務代理人銷假：{Email}（LeaveRevocation #{Id}）", agent.Email, revocationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "通知職務代理人銷假失敗：LeaveRevocation #{Id}", revocationId);
        }
    }

    /// <inheritdoc />
    public async Task<(bool EmailSent, bool LineSent, string? ErrorMessage)> NotifyFinanceUpcomingPaymentsAsync(
        Guid financeUserId,
        IReadOnlyList<(string AppType, string AppLabel, int ApplicationId, string Applicant, DateTime ExpectedDate, decimal Amount)> items)
    {
        try
        {
            if (items.Count == 0) return (false, false, "no items");

            var (emailEnabled, lineEnabled) = await ReadNotificationFlagsAsync();
            var financeUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == financeUserId);
            if (financeUser is null) return (false, false, "finance user not found");

            var siteUrl = await GetSiteUrlAsync();
            var linkUrl = $"{siteUrl}/admin/approval-tasks";  // 暫導至待審任務清單；之後可改為專用待撥清單頁

            var subject = $"[撥款提醒] 您有 {items.Count} 筆預計撥款日將屆";
            var emailBody = BuildUpcomingPaymentsEmail(financeUser.Name, items, linkUrl);

            bool emailSent = false;
            if (emailEnabled && !string.IsNullOrEmpty(financeUser.Email))
            {
                await emailService.SendAsync(financeUser.Email, subject, emailBody);
                emailSent = true;
            }

            bool lineSent = false;
            if (lineEnabled)
            {
                var flexItems = items
                    .Select(i => (i.AppLabel, i.ApplicationId, i.Applicant, i.ExpectedDate, i.Amount))
                    .ToList()
                    .AsReadOnly();
                var flex = LineFlexMessageBuilder.BuildUpcomingPaymentsMessage(financeUser.Name, items.Count, flexItems, linkUrl);
                await PushLineByUserIdAsync(financeUserId, flex, lineEnabled);
                lineSent = true;
            }

            logger.LogInformation("已寄送撥款日將屆提醒：{Name}（{Count} 筆，email={Email}, line={Line}）",
                financeUser.Name, items.Count, emailSent, lineSent);
            return (emailSent, lineSent, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寄送撥款日將屆提醒失敗：FinanceUser={Id}", financeUserId);
            return (false, false, ex.Message);
        }
    }

    private static string BuildUpcomingPaymentsEmail(
        string financeUserName,
        IReadOnlyList<(string AppType, string AppLabel, int ApplicationId, string Applicant, DateTime ExpectedDate, decimal Amount)> items,
        string linkUrl)
    {
        var rows = string.Join("", items.Select((i, idx) => $"""
              <tr style="background:{(idx % 2 == 0 ? "#EDE9E1" : "#F5F2ED")};">
                <td style="padding:8px 12px;color:#525358;">{i.ExpectedDate:yyyy-MM-dd}</td>
                <td style="padding:8px 12px;color:#525358;">{i.AppLabel}</td>
                <td style="padding:8px 12px;color:#525358;font-weight:600;">#{i.ApplicationId}</td>
                <td style="padding:8px 12px;color:#525358;">{i.Applicant}</td>
                <td style="padding:8px 12px;color:#B8892A;font-weight:600;text-align:right;">{i.Amount:N0} 元</td>
              </tr>
            """));
        return $"""
        <div style="font-family:'Microsoft JhengHei','Segoe UI',sans-serif;max-width:680px;margin:0 auto;">
          <div style="background:#B8892A;padding:16px 24px;border-radius:8px 8px 0 0;">
            <h2 style="color:#fff;margin:0;font-size:18px;">撥款日將屆提醒</h2>
          </div>
          <div style="background:#F5F2ED;padding:24px;border-radius:0 0 8px 8px;">
            <p style="color:#525358;margin:0 0 16px;">{financeUserName} 您好，</p>
            <p style="color:#525358;margin:0 0 16px;">您負責的撥款作業中，下列 <strong>{items.Count}</strong> 筆預計撥款日即將到期，請及早安排撥款。</p>
            <table style="width:100%;border-collapse:collapse;margin:0 0 16px;font-size:14px;">
              <thead><tr style="background:#4A6B3A;color:#fff;">
                <th style="padding:8px 12px;text-align:left;">預計撥款日</th>
                <th style="padding:8px 12px;text-align:left;">申請類型</th>
                <th style="padding:8px 12px;text-align:left;">編號</th>
                <th style="padding:8px 12px;text-align:left;">申請人</th>
                <th style="padding:8px 12px;text-align:right;">金額</th>
              </tr></thead>
              <tbody>{rows}</tbody>
            </table>
            {BuildButtonHtml(linkUrl, "前往撥款作業")}
            <hr style="border:none;border-top:1px solid #DDD6C8;margin:16px 0;" />
            <p style="color:#A39685;font-size:12px;margin:0;">此信件由系統自動寄發，請勿直接回覆。</p>
          </div>
        </div>
        """;
    }

    // ── 取得申請摘要 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 取得此申請「最近一次 returned 之後」所有 approved 且為「總監（JobTitle.Level=1）」的審核者 Id。
    /// 用於排除已審者重複通知（與 ApprovalFlowService.GetApprovedSupervisorIdsAsync 一致）。
    /// 註：同人去重新規則限縮為「總監 OR 相鄰 step」；通知時無相鄰判斷上下文，僅排除總監。
    /// 「相鄰 step 同人」會在 SkipUnreviewableStepsAsync 進入該 step 前就被自動跳過，根本不會觸發通知。
    /// </summary>
    private async Task<HashSet<Guid>> GetApprovedReviewerIdsAsync(string applicationType, int applicationId)
    {
        // 追加預支：只看本批次，否則第 1 輪審過的總監在追加輪收不到通知
        var roundNo = await AdvanceSupplementService.ResolveCurrentRoundAsync(db, applicationType, applicationId);

        var lastReturnedAt = await db.ApprovalRecords.AsNoTracking()
            .Where(r => r.ApplicationType == applicationType
                     && r.ApplicationId == applicationId
                     && r.RoundNo == roundNo
                     && r.Action == "returned")
            .MaxAsync(r => (DateTime?)r.ReviewedAt) ?? DateTime.MinValue;

        var ids = await (from r in db.ApprovalRecords.AsNoTracking()
                         join u in db.Users.AsNoTracking() on r.ReviewedById equals u.Id
                         join j in db.JobTitles.AsNoTracking() on u.JobTitleId equals j.Id
                         where r.ApplicationType == applicationType
                            && r.ApplicationId == applicationId
                            && r.RoundNo == roundNo
                            && r.Action == "approved"
                            && r.ReviewedById != null
                            && r.ReviewedAt > lastReturnedAt
                            && j.Level == 1
                         select r.ReviewedById!.Value)
                        .Distinct()
                        .ToListAsync();

        return [.. ids];
    }

    private async Task<string> GetSummaryAsync(string applicationType, int applicationId)
    {
        return applicationType switch
        {
            "payment_request" => await GetPaymentSummaryAsync(applicationId),
            "leave"           => await GetLeaveSummaryAsync(applicationId),
            "leave_revocation" => await GetLeaveRevocationSummaryAsync(applicationId),
            "travel"          => await GetTravelSummaryAsync(applicationId),
            "overtime"        => await GetOvertimeSummaryAsync(applicationId),
            "advance"         => await GetAdvanceSummaryAsync(applicationId),
            "write_off"       => await GetWriteOffSummaryAsync(applicationId),
            "travel_write_off" => await GetTravelWriteOffSummaryAsync(applicationId),
            "travel_payment"  => await GetTravelPaymentSummaryAsync(applicationId),
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

    private async Task<string> GetLeaveRevocationSummaryAsync(int id)
    {
        var rv = await db.LeaveRevocations.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.RevokedHours,
                LeaveType = x.LeaveRequest!.LeaveType,
                Dates     = x.Dates.OrderBy(d => d.Date).Select(d => d.Date).ToList(),
            })
            .FirstOrDefaultAsync();
        if (rv is null) return $"#{id}";

        var dateList = string.Join("、", rv.Dates.Select(d => d.ToString("MM/dd")));
        return $"取消{LeaveTypeNames.GetZh(rv.LeaveType)} {rv.Dates.Count} 天 / {rv.RevokedHours} 小時（{dateList}）";
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

    private async Task<string> GetWriteOffSummaryAsync(int id)
    {
        var wo = await db.WriteOffRecords.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.RequestNo, x.GrandTotal })
            .FirstOrDefaultAsync();
        return wo is not null ? $"{wo.RequestNo}（{wo.GrandTotal:N0} 元）" : $"#{id}";
    }

    private async Task<string> GetTravelWriteOffSummaryAsync(int id)
    {
        var two = await db.TravelWriteOffRecords.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.RequestNo, x.GrandTotal })
            .FirstOrDefaultAsync();
        return two is not null ? $"{two.RequestNo}（{two.GrandTotal:N0} 元）" : $"#{id}";
    }

    private async Task<string> GetTravelPaymentSummaryAsync(int id)
    {
        var tpr = await db.TravelPaymentRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Destination, x.StartDate, x.EndDate, x.GrandTotal })
            .FirstOrDefaultAsync();
        return tpr is not null
            ? $"{tpr.Destination}（{tpr.StartDate:yyyy-MM-dd} ~ {tpr.EndDate:yyyy-MM-dd}，{tpr.GrandTotal:N0} 元）"
            : $"#{id}";
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
            "payment_request"  => "payment-requests",
            "leave"            => "leave-requests",
            "leave_revocation" => "leave-revocations",
            "travel"           => "travel-requests",
            "overtime"         => "overtime-requests",
            "advance"          => "advance-requests",
            "write_off"        => "write-off-requests",
            "travel_write_off" => "travel-write-off-requests",
            "travel_payment"   => "travel-payment-requests",
            _                  => "approval-tasks",
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

    // ── LINE 推播輔助 ──────────────────────────────────────────────────────────

    /// <summary>根據 Email 查找 LineUserId 並推播（找不到或失敗靜默忽略）。enabled=false 時跳過。</summary>
    private async Task PushLineByEmailAsync(string email, object flexMessage, bool enabled)
    {
        if (!enabled) return;
        try
        {
            var lineUserId = await db.Users.AsNoTracking()
                .Where(u => u.Email == email && u.LineUserId != null)
                .Select(u => u.LineUserId)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(lineUserId))
                await lineService.PushMessageAsync(lineUserId, flexMessage);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LINE 推播失敗（by email）：{Email}", email);
        }
    }

    /// <summary>根據 UserId 查找 LineUserId 並推播（找不到或失敗靜默忽略）。enabled=false 時跳過。</summary>
    private async Task PushLineByUserIdAsync(Guid userId, object flexMessage, bool enabled)
    {
        if (!enabled) return;
        try
        {
            var lineUserId = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId && u.LineUserId != null)
                .Select(u => u.LineUserId)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(lineUserId))
                await lineService.PushMessageAsync(lineUserId, flexMessage);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LINE 推播失敗（by userId）：{UserId}", userId);
        }
    }

    /// <summary>讀取系統開關（簽核通知 Email / LINE 是否啟用）。預設皆 true。</summary>
    private async Task<(bool emailEnabled, bool lineEnabled)> ReadNotificationFlagsAsync()
    {
        var s = await db.SystemSettings.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new { x.ApprovalEmailEnabled, x.ApprovalLineEnabled })
            .FirstOrDefaultAsync();
        return (s?.ApprovalEmailEnabled ?? true, s?.ApprovalLineEnabled ?? true);
    }

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

    private static string BuildRefundEmail(
        string recipientName, string applicantName, int advanceId, string requestNo,
        decimal advanceTotal, decimal refundAmount, string linkUrl)
    {
        return $"""
        <div style="font-family: 'Microsoft JhengHei', 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto;">
          <div style="background: #B8892A; padding: 16px 24px; border-radius: 8px 8px 0 0;">
            <h2 style="color: #fff; margin: 0; font-size: 18px;">預支沖銷超額 — 需匯款差額</h2>
          </div>
          <div style="background: #F5F2ED; padding: 24px; border-radius: 0 0 8px 8px;">
            <p style="color: #525358; margin: 0 0 16px;">{recipientName} 您好，</p>
            <p style="color: #525358; margin: 0 0 16px;">
              <strong>{applicantName}</strong> 的預支申請已結案，沖銷金額超過預支金額，請進行差額匯款：
            </p>
            <table style="width: 100%; border-collapse: collapse; margin: 0 0 16px;">
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73; width: 120px;">預支單號</td>
                <td style="padding: 8px 12px; color: #525358; font-weight: 600;">{requestNo}</td>
              </tr>
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">預支金額</td>
                <td style="padding: 8px 12px; color: #525358;">{advanceTotal:N0} 元</td>
              </tr>
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73;">應退還差額</td>
                <td style="padding: 8px 12px; color: #A04040; font-weight: 600;">{refundAmount:N0} 元</td>
              </tr>
            </table>
            {BuildButtonHtml(linkUrl, "前往預支申請")}
            <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 16px 0;" />
            <p style="color: #A39685; font-size: 12px; margin: 0;">此信件由系統自動寄發，請勿直接回覆。</p>
          </div>
        </div>
        """;
    }

    private static string BuildTravelRefundEmail(
        string recipientName, string applicantName, int travelId,
        string destination, decimal travelTotal, decimal refundAmount, string linkUrl)
    {
        return $"""
        <div style="font-family: 'Microsoft JhengHei', 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto;">
          <div style="background: #B8892A; padding: 16px 24px; border-radius: 8px 8px 0 0;">
            <h2 style="color: #fff; margin: 0; font-size: 18px;">出差沖銷超額 — 需匯款差額</h2>
          </div>
          <div style="background: #F5F2ED; padding: 24px; border-radius: 0 0 8px 8px;">
            <p style="color: #525358; margin: 0 0 16px;">{recipientName} 您好，</p>
            <p style="color: #525358; margin: 0 0 16px;">
              <strong>{applicantName}</strong> 的出差申請已結案，沖銷金額超過出差金額，請進行差額匯款：
            </p>
            <table style="width: 100%; border-collapse: collapse; margin: 0 0 16px;">
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73; width: 120px;">出差申請編號</td>
                <td style="padding: 8px 12px; color: #525358; font-weight: 600;">#{travelId}</td>
              </tr>
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">出差地點</td>
                <td style="padding: 8px 12px; color: #525358;">{destination}</td>
              </tr>
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73;">出差金額</td>
                <td style="padding: 8px 12px; color: #525358;">{travelTotal:N0} 元</td>
              </tr>
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">應退還差額</td>
                <td style="padding: 8px 12px; color: #A04040; font-weight: 600;">{refundAmount:N0} 元</td>
              </tr>
            </table>
            {BuildButtonHtml(linkUrl, "前往出差申請")}
            <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 16px 0;" />
            <p style="color: #A39685; font-size: 12px; margin: 0;">此信件由系統自動寄發，請勿直接回覆。</p>
          </div>
        </div>
        """;
    }

    private static string BuildApplicantPaidEmail(
        string applicantName, string label, int applicationId, string summary,
        decimal amount, DateTime paidAt, string linkUrl,
        int? installmentNo = null, int? totalInstallments = null)
    {
        var installmentLabel = installmentNo.HasValue && totalInstallments.HasValue
            ? $"第 {installmentNo}/{totalInstallments} 期"
            : "";
        var titleSuffix = string.IsNullOrEmpty(installmentLabel) ? "" : $"（{installmentLabel}）";
        var installmentRow = string.IsNullOrEmpty(installmentLabel)
            ? ""
            : $"""
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73;">撥款期數</td>
                <td style="padding: 8px 12px; color: #525358; font-weight: 600;">{installmentLabel}</td>
              </tr>
            """;
        return $"""
        <div style="font-family: 'Microsoft JhengHei', 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto;">
          <div style="background: #4A6B3A; padding: 16px 24px; border-radius: 8px 8px 0 0;">
            <h2 style="color: #fff; margin: 0; font-size: 18px;">{label}已撥款{titleSuffix}</h2>
          </div>
          <div style="background: #F5F2ED; padding: 24px; border-radius: 0 0 8px 8px;">
            <p style="color: #525358; margin: 0 0 16px;">{applicantName} 您好，</p>
            <p style="color: #525358; margin: 0 0 16px;">
              您的<strong>{label} #{applicationId}</strong> 已由財務完成撥款作業，款項已撥付{titleSuffix}。
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
              {installmentRow}
              <tr>
                <td style="padding: 8px 12px; color: #6E6F73;">撥款金額</td>
                <td style="padding: 8px 12px; color: #4A6B3A; font-weight: 600;">{amount:N0} 元</td>
              </tr>
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">撥款日期</td>
                <td style="padding: 8px 12px; color: #525358;">{paidAt:yyyy-MM-dd}</td>
              </tr>
            </table>
            {BuildButtonHtml(linkUrl, "查看詳情")}
            <hr style="border: none; border-top: 1px solid #DDD6C8; margin: 16px 0;" />
            <p style="color: #A39685; font-size: 12px; margin: 0;">此信件由系統自動寄發，請勿直接回覆。</p>
          </div>
        </div>
        """;
    }

    private static string BuildApplicantRefundedEmail(
        string applicantName, string label, int applicationId, string summary,
        decimal refundAmount, DateTime refundedAt, string linkUrl)
    {
        return $"""
        <div style="font-family: 'Microsoft JhengHei', 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto;">
          <div style="background: #4A6B3A; padding: 16px 24px; border-radius: 8px 8px 0 0;">
            <h2 style="color: #fff; margin: 0; font-size: 18px;">{label}退款完成</h2>
          </div>
          <div style="background: #F5F2ED; padding: 24px; border-radius: 0 0 8px 8px;">
            <p style="color: #525358; margin: 0 0 16px;">{applicantName} 您好，</p>
            <p style="color: #525358; margin: 0 0 16px;">
              您的<strong>{label} #{applicationId}</strong> 退款已由財務完成匯款作業。
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
                <td style="padding: 8px 12px; color: #6E6F73;">退款金額</td>
                <td style="padding: 8px 12px; color: #4A6B3A; font-weight: 600;">{refundAmount:N0} 元</td>
              </tr>
              <tr style="background: #EDE9E1;">
                <td style="padding: 8px 12px; color: #6E6F73;">退款日期</td>
                <td style="padding: 8px 12px; color: #525358;">{refundedAt:yyyy-MM-dd}</td>
              </tr>
            </table>
            {BuildButtonHtml(linkUrl, "查看詳情")}
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
