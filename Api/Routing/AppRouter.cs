using Jabez.Api.Common;
using Jabez.Api.Handlers;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Jabez.Api.Routing;

/// <summary>
/// 核心路由分派器。
/// 接收 (method, route) → 拆解 segments → C# List Pattern 分派到對應 Handler。
/// </summary>
public sealed class AppRouter(
    ILogger<AppRouter>     logger,
    IJwtService            jwt,
    HealthHandler          health,
    AuthHandler            auth,
    UserHandler            users,
    RoleHandler            roles,
    PermissionHandler      perms,
    SettingsHandler        settings,
    DepartmentHandler      depts,
    JobTitleHandler        jobTitles,
    VendorHandler          vendors,
    ApprovalHandler        approvals,
    ProjectHandler         projects,
    PaymentRequestHandler  paymentRequests,
    LeaveRequestHandler    leaveRequests,
    TravelRequestHandler   travelRequests,
    OvertimeRequestHandler overtimeRequests,
    AttendanceHandler      attendances,
    ApprovalTaskHandler    approvalTasks,
    InsuranceBracketHandler insuranceBrackets,
    PayrollHandler         payroll,
    OvertimeReportHandler  overtimeReport,
    PaymentReportHandler   paymentReport,
    ProjectWaterLevelHandler projectWaterLevel,
    InvoiceOcrHandler      invoiceOcr,
    QuoteOcrHandler        quoteOcr,
    PreReviewRequestHandler preReviewRequests,
    AdvanceRequestHandler           advanceRequests,
    WriteOffRequestHandler          writeOffRequests,
    TravelWriteOffRequestHandler    travelWriteOffRequests,
    CalendarDayHandler              calendarDays,
    FileHandler                     files,
    LineHandler                     line,
    TravelPaymentRequestHandler     travelPaymentRequests,
    AttendanceReminderAdminHandler  attendanceReminderAdmin,
    AttendanceReminderLogHandler    attendanceReminderLogs,
    PaymentReminderLogHandler       paymentReminderLogs,
    EmployeeProfileHandler          employeeProfile,
    NotificationHandler             notifications)
{
    public async Task<IActionResult> RouteAsync(HttpRequest req, string route)
    {
        var method   = req.Method.ToUpper();
        var segments = route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        logger.LogDebug("Router: method={Method} route={Route}", method, route);

        // CORS preflight — 允許所有 OPTIONS
        if (method == "OPTIONS")
            return new OkResult();

        // 驗證 JWT + 權限檢查（公開路由跳過）
        if (!IsPublicRoute(method, segments))
        {
            var principal = await jwt.ValidateRequestAsync(req);
            if (principal is null)
                return new UnauthorizedObjectResult(
                    ApiResponse.Fail("Unauthorized.", "Invalid or missing Bearer token."));

            // Superadmin-only 路由檢查
            if (IsSuperAdminRoute(method, segments))
                RequireSuperAdmin(principal);

            // 撥款日 / 退款日端點：必須是財務體系部門或 Superadmin（縱深防禦：
            // 即使 Handler 內部的部門檢查被誤刪，路由層仍會擋下）
            if (IsFinanceOrSuperAdminRoute(method, segments))
                RequireFinanceOrSuperAdmin(principal);

            var requiredPermission = GetRequiredPermission(method, segments);
            if (requiredPermission is not null)
                RequirePermission(principal, requiredPermission);

            // 把 principal 寫入 HttpContext.User，讓 Handler 透過 IHttpContextAccessor 可取得
            req.HttpContext.User = principal;
        }

        // List Pattern 路由分派（C# 12）
        return (method, segments) switch
        {
            // ── Health ───────────────────────────────────────────────────────
            ("GET",    ["health"])                    => health.Get(),

            // ── Files (Blob 代理) ──────────────────────────────────────────────
            // 簽名檔、頭像為公開路由；原住民證明屬 HR 敏感資料需 JWT + users:read
            ("GET",    ["files", "signatures", var fileName])        => await files.GetSignatureAsync(fileName),
            ("GET",    ["files", "avatars", var fileName])           => await files.GetAvatarAsync(fileName),
            ("GET",    ["files", "indigenous-proofs", var fileName]) => await files.GetIndigenousProofAsync(fileName),
            ("GET",    ["files", "low-income-proofs", var fileName]) => await files.GetLowIncomeProofAsync(fileName),
            ("GET",    ["files", "disabled-proofs", var fileName])   => await files.GetDisabledProofAsync(fileName),
            ("GET",    ["files", "id-cards", var fileName])          => await files.GetIdCardAsync(fileName),
            ("GET",    ["files", "education-proofs", var fileName])  => await files.GetEducationProofAsync(fileName),
            ("GET",    ["files", "passbooks", var fileName])         => await files.GetPassbookAsync(fileName),
            ("GET",    ["files", "vendor-passbooks", var fileName])  => await files.GetVendorPassbookAsync(fileName),
            ("GET",    ["files", "vendor-id-cards", var fileName])   => await files.GetVendorIdCardAsync(fileName),
            // quotes / request-attachments 的 blob name 含日期子路徑（yyyy/MM/{guid}{ext}），需以 slice pattern 接多段
            ("GET",    ["files", "quotes", .. var quotePath])             => await files.GetQuoteAsync(string.Join("/", quotePath)),
            ("GET",    ["files", "request-attachments", .. var attPath]) => await files.GetRequestAttachmentAsync(string.Join("/", attPath)),

            // ── Auth ──────────────────────────────────────────────────────────
            ("POST",   ["auth", "login"])             => await auth.LoginAsync(req),
            ("POST",   ["auth", "refresh"])           => await auth.RefreshAsync(req),
            ("POST",   ["auth", "change-password"])   => await auth.ChangePasswordAsync(req),

            // ── Users ─────────────────────────────────────────────────────────
            ("GET",    ["users", "lookup"])           => await users.GetLookupAsync(req),
            ("GET",    ["users"])                     => await users.GetAllAsync(req),
            ("POST",   ["users"])                     => await users.CreateAsync(req),
            ("POST",   ["users", var id, "send-credentials"]) => await users.SendCredentialsAsync(id),
            // 人事資料卡：必須在 ["users", var id] catch-all 之前
            ("GET",    ["users", var id, "profile"])  => await employeeProfile.GetByUserIdAsync(req, id),
            ("PUT",    ["users", var id, "profile"])  => await employeeProfile.UpsertAsync(req, id),
            ("GET",    ["users", var id])             => await users.GetByIdAsync(id),
            ("PUT",    ["users", var id])             => await users.UpdateAsync(req, id),
            ("PATCH",  ["users", var id])             => await users.UpdateAsync(req, id),
            ("DELETE", ["users", var id])             => await users.DeleteAsync(id),

            // ── Roles ──────────────────────────────────────────────────────────
            ("GET",    ["roles"])                     => await roles.GetAllAsync(),
            ("POST",   ["roles"])                     => await roles.CreateAsync(req),
            ("GET",    ["roles", var id])             => await roles.GetByIdAsync(id),
            ("PUT",    ["roles", var id])             => await roles.UpdateAsync(req, id),
            ("PATCH",  ["roles", var id])             => await roles.UpdateAsync(req, id),
            ("DELETE", ["roles", var id])             => await roles.DeleteAsync(id),

            // ── Permissions ────────────────────────────────────────────────────
            ("GET",    ["permissions"])               => await perms.GetAllAsync(),
            ("POST",   ["permissions"])               => await perms.CreateAsync(req),
            ("GET",    ["permissions", var id])       => await perms.GetByIdAsync(id),
            ("PUT",    ["permissions", var id])       => await perms.UpdateAsync(req, id),
            ("PATCH",  ["permissions", var id])       => await perms.UpdateAsync(req, id),
            ("DELETE", ["permissions", var id])       => await perms.DeleteAsync(id),

            // ── Settings ───────────────────────────────────────────────────────
            ("GET",    ["settings"])                  => await settings.GetAsync(),
            ("PATCH",  ["settings"])                  => await settings.UpdateAsync(req),

            // ── Departments ────────────────────────────────────────────────────
            ("GET",    ["departments"])               => await depts.GetAllAsync(),
            ("POST",   ["departments"])               => await depts.CreateAsync(req),
            ("GET",    ["departments", var id])       => await depts.GetByIdAsync(id),
            ("PUT",    ["departments", var id])       => await depts.UpdateAsync(req, id),
            ("PATCH",  ["departments", var id])       => await depts.UpdateAsync(req, id),
            ("DELETE", ["departments", var id])       => await depts.DeleteAsync(id),

            // ── Job Titles ─────────────────────────────────────────────────────
            ("GET",    ["job-titles", "lookup"])      => await jobTitles.GetLookupAsync(),
            ("GET",    ["job-titles"])                => await jobTitles.GetAllAsync(),
            ("POST",   ["job-titles"])                => await jobTitles.CreateAsync(req),
            ("GET",    ["job-titles", var id])        => await jobTitles.GetByIdAsync(id),
            ("PUT",    ["job-titles", var id])        => await jobTitles.UpdateAsync(req, id),
            ("PATCH",  ["job-titles", var id])        => await jobTitles.UpdateAsync(req, id),
            ("DELETE", ["job-titles", var id])        => await jobTitles.DeleteAsync(id),

            // ── Vendors（廠商管理）─────────────────────────────────────────
            ("GET",    ["vendors", "lookup"])              => await vendors.GetLookupAsync(),
            ("GET",    ["vendors", "lookup-by-tax-id"])    => await vendors.LookupByTaxIdAsync(req),
            ("GET",    ["vendors"])                   => await vendors.GetAllAsync(),
            ("POST",   ["vendors"])                   => await vendors.CreateAsync(req),
            ("GET",    ["vendors", var id])           => await vendors.GetByIdAsync(id),
            ("PUT",    ["vendors", var id])           => await vendors.UpdateAsync(req, id),
            ("PATCH",  ["vendors", var id])           => await vendors.UpdateAsync(req, id),
            ("DELETE", ["vendors", var id])           => await vendors.DeleteAsync(id),

            // ── Approval Items ─────────────────────────────────────────────────
            // active：輕量級查詢，免 approvals:read 權限（必須在 var id 模式之前）
            ("GET",    ["approval-items", "active"])  => await approvals.GetActiveByTypeAsync(req),
            ("GET",    ["approval-items"])            => await approvals.GetAllAsync(),
            ("POST",   ["approval-items"])            => await approvals.CreateAsync(req),
            ("GET",    ["approval-items", var id])    => await approvals.GetByIdAsync(id),
            ("PUT",    ["approval-items", var id])    => await approvals.UpdateAsync(req, id),
            ("PATCH",  ["approval-items", var id])    => await approvals.UpdateAsync(req, id),
            ("DELETE", ["approval-items", var id])    => await approvals.DeleteAsync(id),
            // Steps
            ("POST",   ["approval-items", var id, "steps"])                        => await approvals.AddStepAsync(req, id),
            ("PUT",    ["approval-items", var id, "steps", var stepId])            => await approvals.UpdateStepAsync(req, id, stepId),
            ("PATCH",  ["approval-items", var id, "steps", var stepId])            => await approvals.UpdateStepAsync(req, id, stepId),
            ("DELETE", ["approval-items", var id, "steps", var stepId])            => await approvals.DeleteStepAsync(id, stepId),

            // ── Projects ───────────────────────────────────────────────────────
            ("GET",    ["projects", "years"])             => await projects.GetYearsAsync(),
            ("GET",    ["projects", "active"])            => await projects.GetActiveAsync(req),
            ("GET",    ["projects"])                    => await projects.GetAllAsync(req),
            ("POST",   ["projects"])                    => await projects.CreateAsync(req),
            ("GET",    ["projects", var id])            => await projects.GetByIdAsync(req, id),
            ("PUT",    ["projects", var id])            => await projects.UpdateAsync(req, id),
            ("PATCH",  ["projects", var id])            => await projects.UpdateAsync(req, id),
            ("DELETE", ["projects", var id])            => await projects.DeleteAsync(id),

            // ── Invoice OCR ────────────────────────────────────────────────────
            ("POST",   ["invoice-ocr"])                            => await invoiceOcr.RecognizeAsync(req),

            // ── Quote OCR（報價單辨識，登入即可）──────────────────────────────────
            ("POST",   ["quote-ocr"])                              => await quoteOcr.RecognizeAsync(req),

            // ── Pre-Review Requests（預審申請）─────────────────────────────────────
            ("GET",    ["pre-review-requests"])                       => await preReviewRequests.GetAllAsync(req),
            ("POST",   ["pre-review-requests"])                       => await preReviewRequests.CreateAsync(req),
            ("PATCH",  ["pre-review-requests", var id, "submit"])     => await preReviewRequests.SubmitAsync(req, id),
            ("GET",    ["pre-review-requests", var id])               => await preReviewRequests.GetByIdAsync(req, id),
            ("PUT",    ["pre-review-requests", var id])               => await preReviewRequests.UpdateAsync(req, id),
            ("PATCH",  ["pre-review-requests", var id])               => await preReviewRequests.UpdateAsync(req, id),
            ("DELETE", ["pre-review-requests", var id])               => await preReviewRequests.DeleteAsync(req, id),

            // ── Payment Requests ───────────────────────────────────────────────
            ("GET",    ["payment-requests"])                       => await paymentRequests.GetAllAsync(req),
            ("POST",   ["payment-requests"])                       => await paymentRequests.CreateAsync(req),
            ("PATCH",  ["payment-requests", var id, "submit"])        => await paymentRequests.SubmitAsync(req, id),
            ("PATCH",  ["payment-requests", var id, "installments"])  => await paymentRequests.UpsertInstallmentsAsync(req, id),
            ("GET",    ["payment-requests", var id])               => await paymentRequests.GetByIdAsync(req, id),
            ("PUT",    ["payment-requests", var id])               => await paymentRequests.UpdateAsync(req, id),
            ("PATCH",  ["payment-requests", var id])               => await paymentRequests.UpdateAsync(req, id),
            ("DELETE", ["payment-requests", var id])               => await paymentRequests.DeleteAsync(req, id),

            // ── Advance Requests ──────────────────────────────────────────────
            ("GET",    ["advance-requests"])                                    => await advanceRequests.GetAllAsync(req),
            ("POST",   ["advance-requests"])                                   => await advanceRequests.CreateAsync(req),
            ("PATCH",  ["advance-requests", var id, "submit"])                 => await advanceRequests.SubmitAsync(req, id),
            ("POST",   ["advance-requests", var id, "supplements"])            => await advanceRequests.CreateSupplementAsync(req, id),
            ("PATCH",  ["advance-requests", var id, "supplements", var round]) => await advanceRequests.UpdateSupplementAsync(req, id, round),
            ("DELETE", ["advance-requests", var id, "supplements", var round]) => await advanceRequests.DeleteSupplementAsync(req, id, round),
            ("PATCH",  ["advance-requests", var id, "installments"])           => await advanceRequests.UpsertInstallmentsAsync(req, id),
            ("GET",    ["advance-requests", var id])                           => await advanceRequests.GetByIdAsync(req, id),
            ("PUT",    ["advance-requests", var id])                           => await advanceRequests.UpdateAsync(req, id),
            ("PATCH",  ["advance-requests", var id])                           => await advanceRequests.UpdateAsync(req, id),
            ("DELETE", ["advance-requests", var id])                           => await advanceRequests.DeleteAsync(req, id),

            // ── Write-Off Requests ──────────────────────────────────────────
            ("GET",    ["write-off-requests", "available-advances"])            => await writeOffRequests.GetAvailableAdvancesAsync(req),
            ("GET",    ["write-off-requests"])                                  => await writeOffRequests.GetAllAsync(req),
            ("POST",   ["write-off-requests"])                                 => await writeOffRequests.CreateAsync(req),
            ("PATCH",  ["write-off-requests", var id, "submit"])               => await writeOffRequests.SubmitAsync(req, id),
            ("PATCH",  ["write-off-requests", var id, "installments"])         => await writeOffRequests.UpsertInstallmentsAsync(req, id),
            ("PATCH",  ["write-off-requests", var id, "check-payments"])       => await writeOffRequests.UpdateCheckPaymentsAsync(req, id),
            ("GET",    ["write-off-requests", var id])                         => await writeOffRequests.GetByIdAsync(req, id),
            ("PUT",    ["write-off-requests", var id])                         => await writeOffRequests.UpdateAsync(req, id),
            ("PATCH",  ["write-off-requests", var id])                         => await writeOffRequests.UpdateAsync(req, id),
            ("DELETE", ["write-off-requests", var id])                         => await writeOffRequests.DeleteAsync(req, id),

            // ── Travel Payment Requests ─────────────────────────────────────
            ("GET",    ["travel-payment-requests"])                                    => await travelPaymentRequests.GetAllAsync(req),
            ("POST",   ["travel-payment-requests"])                                    => await travelPaymentRequests.CreateAsync(req),
            ("PATCH",  ["travel-payment-requests", var id, "submit"])                  => await travelPaymentRequests.SubmitAsync(req, id),
            ("PATCH",  ["travel-payment-requests", var id, "installments"])            => await travelPaymentRequests.UpsertInstallmentsAsync(req, id),
            ("GET",    ["travel-payment-requests", var id])                            => await travelPaymentRequests.GetByIdAsync(req, id),
            ("PUT",    ["travel-payment-requests", var id])                            => await travelPaymentRequests.UpdateAsync(req, id),
            ("PATCH",  ["travel-payment-requests", var id])                            => await travelPaymentRequests.UpdateAsync(req, id),
            ("DELETE", ["travel-payment-requests", var id])                            => await travelPaymentRequests.DeleteAsync(req, id),

            // ── Travel Write-Off Requests ───────────────────────────────────
            ("GET",    ["travel-write-off-requests", "available-travels"])      => await travelWriteOffRequests.GetAvailableTravelsAsync(req),
            ("GET",    ["travel-write-off-requests"])                           => await travelWriteOffRequests.GetAllAsync(req),
            ("POST",   ["travel-write-off-requests"])                          => await travelWriteOffRequests.CreateAsync(req),
            ("PATCH",  ["travel-write-off-requests", var id, "submit"])        => await travelWriteOffRequests.SubmitAsync(req, id),
            ("GET",    ["travel-write-off-requests", var id])                  => await travelWriteOffRequests.GetByIdAsync(req, id),
            ("PUT",    ["travel-write-off-requests", var id])                  => await travelWriteOffRequests.UpdateAsync(req, id),
            ("PATCH",  ["travel-write-off-requests", var id])                  => await travelWriteOffRequests.UpdateAsync(req, id),
            ("DELETE", ["travel-write-off-requests", var id])                  => await travelWriteOffRequests.DeleteAsync(req, id),

            // ── Holiday Travel Requests（假日執行活動，共用 TravelRequestHandler）──────
            ("GET",    ["holiday-travel-requests", "count-holidays"])      => await travelRequests.CountHolidaysAsync(req),
            ("GET",    ["holiday-travel-requests"])                        => await travelRequests.GetAllAsync(req, isHolidayTravel: true),
            ("POST",   ["holiday-travel-requests"])                        => await travelRequests.CreateAsync(req, isHolidayTravel: true),
            ("PATCH",  ["holiday-travel-requests", var id, "submit"])      => await travelRequests.SubmitAsync(req, id, isHolidayTravel: true),
            ("PATCH",  ["holiday-travel-requests", var id, "installments"]) => await travelRequests.UpsertInstallmentsAsync(req, id),
            ("GET",    ["holiday-travel-requests", var id])                => await travelRequests.GetByIdAsync(req, id),
            ("PUT",    ["holiday-travel-requests", var id])                => await travelRequests.UpdateAsync(req, id, isHolidayTravel: true),
            ("PATCH",  ["holiday-travel-requests", var id])                => await travelRequests.UpdateAsync(req, id, isHolidayTravel: true),
            ("DELETE", ["holiday-travel-requests", var id])                => await travelRequests.DeleteAsync(req, id),

            // ── Calendar Days（行事曆管理）─────────────────────────────────────────
            ("GET",    ["calendar-days"])                                  => await calendarDays.GetByYearAsync(req),
            ("POST",   ["calendar-days", "import"])                       => await calendarDays.ImportYearAsync(req),
            ("POST",   ["calendar-days"])                                 => await calendarDays.CreateAsync(req),
            ("PUT",    ["calendar-days", var id])                         => await calendarDays.UpdateAsync(req, id),
            ("PATCH",  ["calendar-days", var id])                         => await calendarDays.UpdateAsync(req, id),
            ("DELETE", ["calendar-days", var id])                         => await calendarDays.DeleteAsync(id),

            // ── Leave Requests ─────────────────────────────────────────────────
            ("GET",    ["leave-requests"])                         => await leaveRequests.GetAllAsync(req),
            ("POST",   ["leave-requests"])                         => await leaveRequests.CreateAsync(req),
            ("GET",    ["leave-requests", "compensatory-hours"])  => await leaveRequests.GetCompensatoryHoursAsync(req),
            ("GET",    ["leave-requests", "annual-quota"])        => await leaveRequests.GetAnnualQuotaAsync(req),
            ("GET",    ["leave-requests", "ceremonial-quota"])    => await leaveRequests.GetCeremonialQuotaAsync(req),
            ("GET",    ["leave-requests", "menstrual-quota"])     => await leaveRequests.GetMenstrualQuotaAsync(req),
            ("GET",    ["leave-requests", "marriage-quota"])      => await leaveRequests.GetMarriageQuotaAsync(req),
            ("GET",    ["leave-requests", "maternity-status"])    => await leaveRequests.GetMaternityStatusAsync(req),
            ("GET",    ["leave-requests", "bereavement-quota"])   => await leaveRequests.GetBereavementQuotaAsync(req),
            ("GET",    ["leave-requests", "senior-executive-eligibility"]) => await leaveRequests.GetSeniorExecutiveEligibilityAsync(req),
            ("GET",    ["leave-requests", "senior-executive-quota"]) => await leaveRequests.GetSeniorExecutiveQuotaAsync(req),
            ("GET",    ["leave-requests", "working-days"])        => await leaveRequests.GetWorkingDaysAsync(req),
            ("PATCH",  ["leave-requests", var id, "submit"])      => await leaveRequests.SubmitAsync(req, id),
            ("GET",    ["leave-requests", var id])                 => await leaveRequests.GetByIdAsync(req, id),
            ("PUT",    ["leave-requests", var id])                 => await leaveRequests.UpdateAsync(req, id),
            ("PATCH",  ["leave-requests", var id])                 => await leaveRequests.UpdateAsync(req, id),
            ("DELETE", ["leave-requests", var id])                 => await leaveRequests.DeleteAsync(req, id),

            // ── Travel Requests ────────────────────────────────────────────────
            ("GET",    ["travel-requests"])                        => await travelRequests.GetAllAsync(req),
            ("POST",   ["travel-requests"])                        => await travelRequests.CreateAsync(req),
            ("PATCH",  ["travel-requests", var id, "submit"])        => await travelRequests.SubmitAsync(req, id),
            ("PATCH",  ["travel-requests", var id, "installments"])  => await travelRequests.UpsertInstallmentsAsync(req, id),
            ("GET",    ["travel-requests", var id])                => await travelRequests.GetByIdAsync(req, id),
            ("PUT",    ["travel-requests", var id])                => await travelRequests.UpdateAsync(req, id),
            ("PATCH",  ["travel-requests", var id])                => await travelRequests.UpdateAsync(req, id),
            ("DELETE", ["travel-requests", var id])                => await travelRequests.DeleteAsync(req, id),

            // ── Overtime Requests ─────────────────────────────────────────────
            ("GET",    ["overtime-requests"])                      => await overtimeRequests.GetAllAsync(req),
            ("POST",   ["overtime-requests"])                      => await overtimeRequests.CreateAsync(req),
            ("PATCH",  ["overtime-requests", var id, "submit"])   => await overtimeRequests.SubmitAsync(req, id),
            ("GET",    ["overtime-requests", var id])              => await overtimeRequests.GetByIdAsync(req, id),
            ("PUT",    ["overtime-requests", var id])              => await overtimeRequests.UpdateAsync(req, id),
            ("PATCH",  ["overtime-requests", var id])              => await overtimeRequests.UpdateAsync(req, id),
            ("DELETE", ["overtime-requests", var id])              => await overtimeRequests.DeleteAsync(req, id),

            // ── Attendances ──────────────────────────────────────────────────
            ("GET",    ["attendances"])                    => await attendances.GetAllAsync(req),
            ("GET",    ["attendances", "today"])           => await attendances.GetTodayAsync(req),
            ("POST",   ["attendances", "clock-in"])       => await attendances.ClockInAsync(req),
            ("POST",   ["attendances", "clock-out"])      => await attendances.ClockOutAsync(req),
            ("POST",   ["attendances", "overtime-start"]) => await attendances.OvertimeStartAsync(req),
            ("POST",   ["attendances", "overtime-end"])   => await attendances.OvertimeEndAsync(req),
            ("PUT",    ["attendances", var id])           => await attendances.UpdateAsync(req, id),
            ("PATCH",  ["attendances", var id])           => await attendances.UpdateAsync(req, id),

            // ── Insurance Brackets ────────────────────────────────────────────
            ("GET",    ["insurance-brackets"])              => await insuranceBrackets.GetAllAsync(),
            ("GET",    ["insurance-brackets", "lookup"])    => await insuranceBrackets.LookupBySalaryAsync(req),
            ("POST",   ["insurance-brackets"])              => await insuranceBrackets.CreateAsync(req),
            ("GET",    ["insurance-brackets", var id])      => await insuranceBrackets.GetByIdAsync(id),
            ("PUT",    ["insurance-brackets", var id])   => await insuranceBrackets.UpdateAsync(req, id),
            ("PATCH",  ["insurance-brackets", var id])   => await insuranceBrackets.UpdateAsync(req, id),
            ("DELETE", ["insurance-brackets", var id])   => await insuranceBrackets.DeleteAsync(id),

            // ── Payroll ──────────────────────────────────────────────────────────
            ("GET",    ["payroll"])                                   => await payroll.GetMonthlyAsync(req),
            ("POST",   ["payroll", "send-slips"])                    => await payroll.SendSlipsAsync(req),
            ("GET",    ["payroll", var id, "adjustment"])             => await payroll.GetAdjustmentAsync(req, id),
            ("PUT",    ["payroll", var id, "adjustment"])             => await payroll.UpsertAdjustmentAsync(req, id),

            // ── Reports ─────────────────────────────────────────────────────────
            ("GET",    ["reports", "overtime"])                    => await overtimeReport.GetAllAsync(req),
            ("GET",    ["reports", "payment", "export"])           => await paymentReport.GetExportAsync(req),
            ("GET",    ["reports", "payment"])                     => await paymentReport.GetAllAsync(req),
            ("GET",    ["reports", "project-water-level"])         => await projectWaterLevel.GetAllAsync(req),

            // ── Approval Tasks ─────────────────────────────────────────────────
            ("GET",    ["approval-tasks"])                                                      => await approvalTasks.GetAllAsync(req),
            ("POST",   ["approval-tasks", "batch-approve"])                                     => await approvalTasks.BatchApproveAsync(req),
            // applicants 必須排在 ["approval-tasks", var id] 之前，否則會被當成 id 吃掉
            ("GET",    ["approval-tasks", "applicants"])                                        => await approvalTasks.GetApplicantsAsync(req),
            ("GET",    ["approval-tasks", var appType, var id]) when ApprovalTaskHandler.ValidAppTypes.Contains(appType)
                                                                                               => await approvalTasks.GetByIdAsync(req, id, appType),
            ("GET",    ["approval-tasks", var id])                                             => await approvalTasks.GetByIdAsync(req, id),
            ("PATCH",  ["approval-tasks", var appType, var id, "review"])                      => await approvalTasks.ReviewAsync(req, appType, id),
            ("PATCH",  ["approval-tasks", "write_off", var id, "close"])                       => await approvalTasks.CloseWriteOffAsync(req, id),
            ("PATCH",  ["approval-tasks", "travel_write_off", var id, "close"])                => await approvalTasks.CloseTravelWriteOffAsync(req, id),

            // ── Attendance Reminder（僅 Superadmin 手動觸發，排程由 TimerTrigger 自動執行）──
            ("POST",   ["admin", "attendance-reminder", "run"]) => await attendanceReminderAdmin.RunAsync(req),

            // ── Attendance Reminder Logs（僅 Superadmin 查詢推播紀錄）──
            // 注意 List Pattern 順序：stats / batches/{guid} 必須排在 {id} 之前
            ("GET",    ["admin", "attendance-reminder-logs"])                    => await attendanceReminderLogs.GetPagedAsync(req),
            ("GET",    ["admin", "attendance-reminder-logs", "stats"])           => await attendanceReminderLogs.GetStatsAsync(req),
            ("GET",    ["admin", "attendance-reminder-logs", "batches", var bid]) => await attendanceReminderLogs.GetByBatchIdAsync(req, bid),
            ("GET",    ["admin", "attendance-reminder-logs", var id])            => await attendanceReminderLogs.GetByIdAsync(req, id),

            // ── 撥款提醒：Superadmin 手動觸發 + log 查詢 ─────────────────────────
            ("POST",   ["admin", "payment-reminder", "run"])                     => await paymentReminderLogs.ManualRunAsync(req),
            ("GET",    ["admin", "payment-reminder-logs"])                       => await paymentReminderLogs.GetPagedAsync(req),

            // ── Me（當前使用者聚合資訊）────────────────────────────────────
            // 注意路由次序：具體路徑（notification-counts / user / profile / files）必須在 catch-all 之前
            ("GET",    ["me", "notification-counts"]) => await notifications.GetMyCountsAsync(req),
            // 員工自助唯讀端點（登入即可，不需任何管理權限）
            ("GET",    ["me", "user"])                                    => await users.GetMineAsync(req),
            ("GET",    ["me", "profile"])                                 => await employeeProfile.GetMineAsync(req),
            ("GET",    ["me", "files", var container, var fileName])      => await files.GetMineAsync(req, container, fileName),

            // ── LINE 綁定 ─────────────────────────────────────────────────────
            ("GET",    ["line", "bind-url"])         => await line.GetBindUrlAsync(req),
            ("POST",   ["line", "bind"])             => await line.BindAsync(req),
            ("POST",   ["line", "unbind"])            => await line.UnbindAsync(req),
            ("GET",    ["line", "binding-status"])    => await line.GetStatusAsync(req),
            ("GET",    ["line", "quota"])             => await line.GetQuotaAsync(req),

            // ── 404 ────────────────────────────────────────────────────────────
            _ => new NotFoundObjectResult(
                     ApiResponse.Fail(
                         "Endpoint not found.",
                         $"Route '/api/{route}' with method {method} does not exist."))
        };
    }

    /// <summary>公開路由（不需 JWT）</summary>
    private static bool IsPublicRoute(string method, string[] segments) =>
        (method, segments) is
            ("GET",  ["health"]) or
            ("POST", ["auth", "login"]) or
            ("POST", ["auth", "refresh"]) or
            // 簽名檔代理：PDF 匯出時需要直接 fetch，不帶 Authorization header
            ("GET",  ["files", "signatures", _]) or
            // 頭像代理：topbar 顯示頭像不帶 Authorization header
            ("GET",  ["files", "avatars", _]);

    /// <summary>根據 HTTP method + 路由 segments 決定所需的權限代碼</summary>
    private static string? GetRequiredPermission(string method, string[] segments) =>
        (method, segments) switch
        {
            // Files
            // signatures / avatars 為公開路由（由 IsPublicRoute 攔住），此處 null 僅為保險
            ("GET", ["files", "signatures", _])         => null,
            ("GET", ["files", "avatars", _])            => null,
            // indigenous-proofs / low-income-proofs / disabled-proofs / id-cards / education-proofs / passbooks 屬 HR 敏感 PII，需 users:read 權限
            ("GET", ["files", "indigenous-proofs", _])  => PermissionCodes.UsersRead,
            ("GET", ["files", "low-income-proofs", _])  => PermissionCodes.UsersRead,
            ("GET", ["files", "disabled-proofs", _])    => PermissionCodes.UsersRead,
            ("GET", ["files", "id-cards", _])           => PermissionCodes.UsersRead,
            ("GET", ["files", "education-proofs", _])   => PermissionCodes.UsersRead,
            ("GET", ["files", "passbooks", _])          => PermissionCodes.UsersRead,
            // vendor-passbooks 為一般檔案（任何登入者皆可讀，與 avatars / signatures 同層）
            ("GET", ["files", "vendor-passbooks", _])   => null,
            // vendor-id-cards 屬敏感 PII（個人工作室身分證），需 vendors:read
            ("GET", ["files", "vendor-id-cards", _])    => PermissionCodes.VendorsRead,
            // quotes（報價單）/ request-attachments（整單附件）為一般業務檔案（任何登入者皆可讀，與 vendor-passbooks 同層）
            ("GET", ["files", "quotes", ..])            => null,
            ("GET", ["files", "request-attachments", ..]) => null,

            // Users（lookup 不需權限，登入即可）
            ("GET",    ["users", "lookup"])               => null,
            // 人事資料卡：GET 需 users:read，PUT 需 users:write
            ("GET",    ["users", _, "profile"])           => PermissionCodes.UsersRead,
            ("PUT",    ["users", _, "profile"])           => PermissionCodes.UsersWrite,
            ("GET",    ["users", ..])                    => PermissionCodes.UsersRead,
            ("POST",   ["users"])                        => PermissionCodes.UsersWrite,
            ("POST",   ["users", _, "send-credentials"]) => PermissionCodes.UsersWrite,
            ("PUT" or "PATCH", ["users", _])             => PermissionCodes.UsersWrite,
            ("DELETE", ["users", _])                     => PermissionCodes.UsersDelete,

            // Roles
            ("GET",    ["roles", ..])                    => PermissionCodes.RolesRead,
            ("POST",   ["roles"])                        => PermissionCodes.RolesWrite,
            ("PUT" or "PATCH", ["roles", _])             => PermissionCodes.RolesWrite,
            ("DELETE", ["roles", _])                     => PermissionCodes.RolesDelete,

            // Permissions — Superadmin-only，由 IsSuperAdminRoute 處理

            // Settings
            ("GET",    ["settings"])                     => PermissionCodes.SettingsRead,
            ("PATCH",  ["settings"])                     => PermissionCodes.SettingsWrite,

            // Departments
            ("GET",    ["departments", ..])              => PermissionCodes.DepartmentsRead,
            ("POST",   ["departments"])                  => PermissionCodes.DepartmentsWrite,
            ("PUT" or "PATCH", ["departments", _])       => PermissionCodes.DepartmentsWrite,
            ("DELETE", ["departments", _])               => PermissionCodes.DepartmentsDelete,

            // Job Titles（lookup 不需權限，登入即可）
            ("GET",    ["job-titles", "lookup"])          => null,
            ("GET",    ["job-titles", ..])               => PermissionCodes.JobTitlesRead,
            ("POST",   ["job-titles"])                   => PermissionCodes.JobTitlesWrite,
            ("PUT" or "PATCH", ["job-titles", _])        => PermissionCodes.JobTitlesWrite,
            ("DELETE", ["job-titles", _])                => PermissionCodes.JobTitlesDelete,

            // Vendors（lookup 與 POST 不需權限：任何登入者皆可使用 quick-add）
            ("GET",    ["vendors", "lookup"])             => null,
            ("GET",    ["vendors", "lookup-by-tax-id"])   => null,
            ("POST",   ["vendors"])                       => null,
            ("GET",    ["vendors", ..])                   => PermissionCodes.VendorsRead,
            ("PUT" or "PATCH", ["vendors", _])            => PermissionCodes.VendorsWrite,
            ("DELETE", ["vendors", _])                    => PermissionCodes.VendorsDelete,

            // Approval Items + Steps
            // active：輕量級查詢，免權限（供申請表單判斷指定審核步驟用，不含敏感設定）
            ("GET",    ["approval-items", "active"])     => null,
            ("GET",    ["approval-items", ..])           => PermissionCodes.ApprovalsRead,
            ("POST",   ["approval-items"])               => PermissionCodes.ApprovalsWrite,
            ("POST",   ["approval-items", _, "steps"])   => PermissionCodes.ApprovalsWrite,
            ("PUT" or "PATCH", ["approval-items", _])    => PermissionCodes.ApprovalsWrite,
            ("PUT" or "PATCH", ["approval-items", _, "steps", _]) => PermissionCodes.ApprovalsWrite,
            ("DELETE", ["approval-items", _])            => PermissionCodes.ApprovalsDelete,
            ("DELETE", ["approval-items", _, "steps", _]) => PermissionCodes.ApprovalsDelete,

            // Projects
            ("GET",    ["projects", "active"])            => null,
            ("GET",    ["projects", ..])                 => PermissionCodes.ProjectsRead,
            ("POST",   ["projects"])                     => PermissionCodes.ProjectsWrite,
            ("PUT" or "PATCH", ["projects", _])          => PermissionCodes.ProjectsWrite,
            ("DELETE", ["projects", _])                  => PermissionCodes.ProjectsDelete,

            // Invoice OCR（登入即可使用，不需特殊權限）
            ("POST",   ["invoice-ocr"])                              => null,

            // Quote OCR（登入即可使用，不需特殊權限）
            ("POST",   ["quote-ocr"])                                => null,

            // Pre-Review Requests（預審申請）
            ("GET",    ["pre-review-requests", ..])          => PermissionCodes.PreReviewRequestsRead,
            ("POST",   ["pre-review-requests"])              => PermissionCodes.PreReviewRequestsWrite,
            ("PUT",    ["pre-review-requests", _])           => PermissionCodes.PreReviewRequestsWrite,
            ("PATCH",  ["pre-review-requests", _, "submit"]) => PermissionCodes.PreReviewRequestsWrite,
            ("PATCH",  ["pre-review-requests", _])           => PermissionCodes.PreReviewRequestsWrite,
            ("DELETE", ["pre-review-requests", _])           => PermissionCodes.PreReviewRequestsDelete,

            // Payment Requests
            ("GET",    ["payment-requests", ..])         => PermissionCodes.PaymentRequestsRead,
            ("POST",   ["payment-requests"])             => PermissionCodes.PaymentRequestsWrite,
            ("PUT",    ["payment-requests", _])          => PermissionCodes.PaymentRequestsWrite,
            ("PATCH",  ["payment-requests", _, "submit"])       => PermissionCodes.PaymentRequestsWrite,
            ("PATCH",  ["payment-requests", _, "installments"]) => PermissionCodes.PaymentRequestsWrite,
            ("PATCH",  ["payment-requests", _])                 => PermissionCodes.PaymentRequestsWrite,
            ("DELETE", ["payment-requests", _])          => PermissionCodes.PaymentRequestsDelete,

            // Advance Requests
            ("GET",    ["advance-requests", ..])         => PermissionCodes.AdvanceRequestsRead,
            ("POST",   ["advance-requests"])             => PermissionCodes.AdvanceRequestsWrite,
            ("PUT",    ["advance-requests", _])          => PermissionCodes.AdvanceRequestsWrite,
            ("PATCH",  ["advance-requests", _, "submit"])       => PermissionCodes.AdvanceRequestsWrite,
            ("PATCH",  ["advance-requests", _, "installments"]) => PermissionCodes.AdvanceRequestsWrite,
            // 追加預支批次：新增 / 編輯 / 放棄皆屬「修改自己的申請單」，一律 Write（DELETE 不用 Delete 權限）
            ("POST",   ["advance-requests", _, "supplements"])     => PermissionCodes.AdvanceRequestsWrite,
            ("PATCH",  ["advance-requests", _, "supplements", _])  => PermissionCodes.AdvanceRequestsWrite,
            ("DELETE", ["advance-requests", _, "supplements", _])  => PermissionCodes.AdvanceRequestsWrite,
            ("PATCH",  ["advance-requests", _])                 => PermissionCodes.AdvanceRequestsWrite,
            ("DELETE", ["advance-requests", _])          => PermissionCodes.AdvanceRequestsDelete,

            // Write-Off Requests
            ("GET",    ["write-off-requests", ..])              => PermissionCodes.WriteOffRequestsRead,
            ("POST",   ["write-off-requests"])                  => PermissionCodes.WriteOffRequestsWrite,
            ("PUT",    ["write-off-requests", _])               => PermissionCodes.WriteOffRequestsWrite,
            ("PATCH",  ["write-off-requests", _, "submit"])     => PermissionCodes.WriteOffRequestsWrite,
            ("PATCH",  ["write-off-requests", _, "installments"])   => PermissionCodes.WriteOffRequestsWrite,
            ("PATCH",  ["write-off-requests", _, "check-payments"]) => PermissionCodes.WriteOffRequestsWrite,
            ("PATCH",  ["write-off-requests", _])               => PermissionCodes.WriteOffRequestsWrite,
            ("DELETE", ["write-off-requests", _])               => PermissionCodes.WriteOffRequestsDelete,

            // Travel Payment Requests
            ("GET",    ["travel-payment-requests", ..])               => PermissionCodes.TravelPaymentRequestsRead,
            ("POST",   ["travel-payment-requests"])                   => PermissionCodes.TravelPaymentRequestsWrite,
            ("PUT",    ["travel-payment-requests", _])                => PermissionCodes.TravelPaymentRequestsWrite,
            ("PATCH",  ["travel-payment-requests", _, "submit"])      => PermissionCodes.TravelPaymentRequestsWrite,
            ("PATCH",  ["travel-payment-requests", _, "installments"]) => PermissionCodes.TravelPaymentRequestsWrite,
            ("PATCH",  ["travel-payment-requests", _])                => PermissionCodes.TravelPaymentRequestsWrite,
            ("DELETE", ["travel-payment-requests", _])                => PermissionCodes.TravelPaymentRequestsDelete,

            // Travel Write-Off Requests
            ("GET",    ["travel-write-off-requests", ..])            => PermissionCodes.TravelWriteOffRequestsRead,
            ("POST",   ["travel-write-off-requests"])                => PermissionCodes.TravelWriteOffRequestsWrite,
            ("PUT",    ["travel-write-off-requests", _])             => PermissionCodes.TravelWriteOffRequestsWrite,
            ("PATCH",  ["travel-write-off-requests", _, "submit"])   => PermissionCodes.TravelWriteOffRequestsWrite,
            ("PATCH",  ["travel-write-off-requests", _])             => PermissionCodes.TravelWriteOffRequestsWrite,
            ("DELETE", ["travel-write-off-requests", _])             => PermissionCodes.TravelWriteOffRequestsDelete,

            // Holiday Travel Requests
            ("GET",    ["holiday-travel-requests", ..])            => PermissionCodes.HolidayTravelRequestsRead,
            ("POST",   ["holiday-travel-requests"])                => PermissionCodes.HolidayTravelRequestsWrite,
            ("PUT",    ["holiday-travel-requests", _])             => PermissionCodes.HolidayTravelRequestsWrite,
            ("PATCH",  ["holiday-travel-requests", _, "submit"])   => PermissionCodes.HolidayTravelRequestsWrite,
            ("PATCH",  ["holiday-travel-requests", _, "installments"]) => PermissionCodes.HolidayTravelRequestsWrite,
            ("PATCH",  ["holiday-travel-requests", _])             => PermissionCodes.HolidayTravelRequestsWrite,
            ("DELETE", ["holiday-travel-requests", _])             => PermissionCodes.HolidayTravelRequestsDelete,

            // Calendar Days
            ("GET",    ["calendar-days", ..])                      => PermissionCodes.CalendarDaysRead,
            ("POST",   ["calendar-days", "import"])                => PermissionCodes.CalendarDaysWrite,
            ("POST",   ["calendar-days"])                          => PermissionCodes.CalendarDaysWrite,
            ("PUT",    ["calendar-days", _])                       => PermissionCodes.CalendarDaysWrite,
            ("PATCH",  ["calendar-days", _])                       => PermissionCodes.CalendarDaysWrite,
            ("DELETE", ["calendar-days", _])                       => PermissionCodes.CalendarDaysDelete,

            // Leave Requests
            ("GET",    ["leave-requests", ..])           => PermissionCodes.LeaveRequestsRead,
            ("POST",   ["leave-requests"])               => PermissionCodes.LeaveRequestsWrite,
            ("PUT",    ["leave-requests", _])            => PermissionCodes.LeaveRequestsWrite,
            ("PATCH",  ["leave-requests", _, "submit"])  => PermissionCodes.LeaveRequestsWrite,
            ("PATCH",  ["leave-requests", _])            => PermissionCodes.LeaveRequestsWrite,
            ("DELETE", ["leave-requests", _])            => PermissionCodes.LeaveRequestsDelete,

            // Travel Requests
            ("GET",    ["travel-requests", ..])          => PermissionCodes.TravelRequestsRead,
            ("POST",   ["travel-requests"])              => PermissionCodes.TravelRequestsWrite,
            ("PUT",    ["travel-requests", _])           => PermissionCodes.TravelRequestsWrite,
            ("PATCH",  ["travel-requests", _, "submit"]) => PermissionCodes.TravelRequestsWrite,
            ("PATCH",  ["travel-requests", _, "installments"]) => PermissionCodes.TravelRequestsWrite,
            ("PATCH",  ["travel-requests", _])           => PermissionCodes.TravelRequestsWrite,
            ("DELETE", ["travel-requests", _])           => PermissionCodes.TravelRequestsDelete,

            // Overtime Requests
            ("GET",    ["overtime-requests", ..])        => PermissionCodes.OvertimeRequestsRead,
            ("POST",   ["overtime-requests"])             => PermissionCodes.OvertimeRequestsWrite,
            ("PUT",    ["overtime-requests", _])          => PermissionCodes.OvertimeRequestsWrite,
            ("PATCH",  ["overtime-requests", _, "submit"]) => PermissionCodes.OvertimeRequestsWrite,
            ("PATCH",  ["overtime-requests", _])          => PermissionCodes.OvertimeRequestsWrite,
            ("DELETE", ["overtime-requests", _])          => PermissionCodes.OvertimeRequestsDelete,

            // Attendances（打卡不需額外權限，登入即可）
            ("GET",    ["attendances", ..])              => null,
            ("POST",   ["attendances", ..])              => null,
            ("PUT" or "PATCH", ["attendances", _])       => null,

            // Insurance Brackets
            ("GET",    ["insurance-brackets", ..])              => PermissionCodes.InsuranceBracketsRead,
            ("POST",   ["insurance-brackets"])                  => PermissionCodes.InsuranceBracketsWrite,
            ("PUT" or "PATCH", ["insurance-brackets", _])       => PermissionCodes.InsuranceBracketsWrite,
            ("DELETE", ["insurance-brackets", _])               => PermissionCodes.InsuranceBracketsDelete,

            // Payroll
            ("GET",    ["payroll"])                           => PermissionCodes.PayrollRead,
            ("POST",   ["payroll", "send-slips"])             => PermissionCodes.PayrollWrite,
            ("GET",    ["payroll", _, "adjustment"])          => PermissionCodes.PayrollRead,
            ("PUT",    ["payroll", _, "adjustment"])          => PermissionCodes.PayrollWrite,

            // Reports
            ("GET",    ["reports", "overtime"])                    => PermissionCodes.ReportsOvertimeRead,
            ("GET",    ["reports", "payment", "export"])           => PermissionCodes.ReportsPaymentRead,
            ("GET",    ["reports", "payment"])                     => PermissionCodes.ReportsPaymentRead,
            ("GET",    ["reports", "project-water-level"])         => PermissionCodes.ReportsProjectWaterLevelRead,

            // Approval Tasks — 指定審核者不需要全域審核權限，改由 Handler 內部依步驟類型判斷
            // GET: 任何已登入使用者可查詢（SQL 已依職稱/指定審核過濾）
            // PATCH review: ReviewAsync 內部對非 UseApplicantDesignated 步驟仍要求 ApprovalTasksWrite

            // LINE — bind / unbind / binding-status 為個人帳號操作，登入即可；quota 屬機敏營運資訊，需 line-quota:read
            ("GET",    ["line", "quota"])                => PermissionCodes.LineQuotaRead,

            _ => null
        };

    /// <summary>判斷是否為 Superadmin-only 路由</summary>
    private static bool IsSuperAdminRoute(string method, string[] segments) =>
        (method, segments) is
            ("POST", ["admin", "attendance-reminder", "run"]) or
            ("GET",  ["admin", "attendance-reminder-logs", ..]) or
            ("POST", ["admin", "payment-reminder", "run"]) or
            ("GET",  ["admin", "payment-reminder-logs", ..]);

    /// <summary>
    /// 撥款日 / 退款日 / 結案 等只允許財務體系部門（AC/FIN/Jabez HQ/CEO）或 Superadmin 操作的路由清單。
    /// Handler 內部仍保留同樣的檢查作為縱深防禦。
    /// </summary>
    private static bool IsFinanceOrSuperAdminRoute(string method, string[] segments) =>
        (method, segments) is
            ("PATCH", ["payment-requests",         _, "installments"]) or
            ("PATCH", ["advance-requests",         _, "installments"]) or
            ("PATCH", ["travel-requests",          _, "installments"]) or
            ("PATCH", ["travel-payment-requests",  _, "installments"]) or
            ("PATCH", ["holiday-travel-requests",  _, "installments"]) or
            ("PATCH", ["write-off-requests",       _, "installments"]) or
            ("PATCH", ["write-off-requests",       _, "check-payments"]);

    /// <summary>檢查是否為 Superadmin，否則拋出 403</summary>
    private static void RequireSuperAdmin(ClaimsPrincipal principal)
    {
        if (principal.FindFirst("is_superadmin")?.Value != "true")
            throw AppException.Forbidden("此功能僅限 Superadmin 使用。");
    }

    /// <summary>檢查是否為財務體系部門（AC/FIN/Jabez HQ/CEO）或 Superadmin，否則拋出 403</summary>
    private static void RequireFinanceOrSuperAdmin(ClaimsPrincipal principal)
    {
        if (principal.FindFirst("is_superadmin")?.Value == "true") return;

        var deptCode = principal.FindFirst("department_code")?.Value;
        if (!string.IsNullOrEmpty(deptCode) && DepartmentCodes.FinancialAndAbove.Contains(deptCode))
            return;

        throw AppException.Forbidden("此操作僅限財務體系部門（會計部 / 行政財務部 / 雅比斯總公司管理部 / 總監室）或 Superadmin 使用。");
    }

    /// <summary>檢查 JWT claims 是否包含指定權限。Superadmin 自動通過。</summary>
    private static void RequirePermission(ClaimsPrincipal principal, string permissionCode)
    {
        // Superadmin 擁有所有權限
        if (principal.FindFirst("is_superadmin")?.Value == "true")
            return;

        var permissions = principal.FindAll("permissions").Select(c => c.Value);
        if (!permissions.Contains(permissionCode))
            throw AppException.Forbidden($"缺少所需權限：{permissionCode}");
    }
}
