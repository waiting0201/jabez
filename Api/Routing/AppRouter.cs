using Jabez.Api.Common;
using Jabez.Api.Handlers;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jabez.Api.Routing;

/// <summary>
/// 核心路由分派器。
/// 接收 (method, route) → 拆解 segments → C# List Pattern 分派到對應 Handler。
/// </summary>
public sealed class AppRouter(
    IJwtService            jwt,
    HealthHandler          health,
    AuthHandler            auth,
    UserHandler            users,
    RoleHandler            roles,
    PermissionHandler      perms,
    SettingsHandler        settings,
    DepartmentHandler      depts,
    JobTitleHandler        jobTitles,
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
    InvoiceOcrHandler      invoiceOcr)
{
    public async Task<IActionResult> RouteAsync(HttpRequest req, string route)
    {
        var method   = req.Method.ToUpper();
        var segments = route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        Console.WriteLine($"[Router] method={method}, route={route}, segments=[{string.Join(", ", segments)}]");

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

            var requiredPermission = GetRequiredPermission(method, segments);
            if (requiredPermission is not null)
                RequirePermission(principal, requiredPermission);
        }

        // List Pattern 路由分派（C# 12）
        return (method, segments) switch
        {
            // ── Health ───────────────────────────────────────────────────────
            ("GET",    ["health"])                    => health.Get(),

            // ── Auth ──────────────────────────────────────────────────────────
            ("POST",   ["auth", "login"])             => await auth.LoginAsync(req),
            ("POST",   ["auth", "refresh"])           => await auth.RefreshAsync(req),
            ("POST",   ["auth", "change-password"])   => await auth.ChangePasswordAsync(req),

            // ── Users ─────────────────────────────────────────────────────────
            ("GET",    ["users"])                     => await users.GetAllAsync(req),
            ("POST",   ["users"])                     => await users.CreateAsync(req),
            ("POST",   ["users", var id, "send-credentials"]) => await users.SendCredentialsAsync(id),
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
            ("GET",    ["job-titles"])                => await jobTitles.GetAllAsync(),
            ("POST",   ["job-titles"])                => await jobTitles.CreateAsync(req),
            ("GET",    ["job-titles", var id])        => await jobTitles.GetByIdAsync(id),
            ("PUT",    ["job-titles", var id])        => await jobTitles.UpdateAsync(req, id),
            ("PATCH",  ["job-titles", var id])        => await jobTitles.UpdateAsync(req, id),
            ("DELETE", ["job-titles", var id])        => await jobTitles.DeleteAsync(id),

            // ── Approval Items ─────────────────────────────────────────────────
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
            ("GET",    ["projects", "active"])            => await projects.GetActiveAsync(),
            ("GET",    ["projects"])                    => await projects.GetAllAsync(req),
            ("POST",   ["projects"])                    => await projects.CreateAsync(req),
            ("GET",    ["projects", var id])            => await projects.GetByIdAsync(id),
            ("PUT",    ["projects", var id])            => await projects.UpdateAsync(req, id),
            ("PATCH",  ["projects", var id])            => await projects.UpdateAsync(req, id),
            ("DELETE", ["projects", var id])            => await projects.DeleteAsync(id),

            // ── Invoice OCR ────────────────────────────────────────────────────
            ("POST",   ["invoice-ocr"])                            => await invoiceOcr.RecognizeAsync(req),

            // ── Payment Requests ───────────────────────────────────────────────
            ("GET",    ["payment-requests"])                       => await paymentRequests.GetAllAsync(req),
            ("POST",   ["payment-requests"])                       => await paymentRequests.CreateAsync(req),
            ("PATCH",  ["payment-requests", var id, "submit"])        => await paymentRequests.SubmitAsync(req, id),
            ("PATCH",  ["payment-requests", var id, "payment-date"]) => await paymentRequests.UpdatePaymentDateAsync(req, id),
            ("GET",    ["payment-requests", var id])               => await paymentRequests.GetByIdAsync(req, id),
            ("PUT",    ["payment-requests", var id])               => await paymentRequests.UpdateAsync(req, id),
            ("PATCH",  ["payment-requests", var id])               => await paymentRequests.UpdateAsync(req, id),
            ("DELETE", ["payment-requests", var id])               => await paymentRequests.DeleteAsync(req, id),

            // ── Leave Requests ─────────────────────────────────────────────────
            ("GET",    ["leave-requests"])                         => await leaveRequests.GetAllAsync(req),
            ("POST",   ["leave-requests"])                         => await leaveRequests.CreateAsync(req),
            ("GET",    ["leave-requests", "compensatory-hours"])  => await leaveRequests.GetCompensatoryHoursAsync(req),
            ("PATCH",  ["leave-requests", var id, "submit"])      => await leaveRequests.SubmitAsync(req, id),
            ("GET",    ["leave-requests", var id])                 => await leaveRequests.GetByIdAsync(req, id),
            ("PUT",    ["leave-requests", var id])                 => await leaveRequests.UpdateAsync(req, id),
            ("PATCH",  ["leave-requests", var id])                 => await leaveRequests.UpdateAsync(req, id),
            ("DELETE", ["leave-requests", var id])                 => await leaveRequests.DeleteAsync(req, id),

            // ── Travel Requests ────────────────────────────────────────────────
            ("GET",    ["travel-requests"])                        => await travelRequests.GetAllAsync(req),
            ("POST",   ["travel-requests"])                        => await travelRequests.CreateAsync(req),
            ("PATCH",  ["travel-requests", var id, "submit"])     => await travelRequests.SubmitAsync(req, id),
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
            ("GET",    ["payroll"])                        => await payroll.GetMonthlyAsync(req),

            // ── Reports ─────────────────────────────────────────────────────────
            ("GET",    ["reports", "overtime"])                    => await overtimeReport.GetAllAsync(req),
            ("GET",    ["reports", "payment"])                     => await paymentReport.GetAllAsync(req),
            ("GET",    ["reports", "project-water-level"])         => await projectWaterLevel.GetAllAsync(req),

            // ── Approval Tasks ─────────────────────────────────────────────────
            ("GET",    ["approval-tasks"])                                                      => await approvalTasks.GetAllAsync(req),
            ("GET",    ["approval-tasks", var appType, var id]) when ApprovalTaskHandler.ValidAppTypes.Contains(appType)
                                                                                               => await approvalTasks.GetByIdAsync(id, appType),
            ("GET",    ["approval-tasks", var id])                                             => await approvalTasks.GetByIdAsync(id),
            ("PATCH",  ["approval-tasks", var appType, var id, "review"])                      => await approvalTasks.ReviewAsync(req, appType, id),

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
            ("POST", ["auth", "refresh"]);

    /// <summary>根據 HTTP method + 路由 segments 決定所需的權限代碼</summary>
    private static string? GetRequiredPermission(string method, string[] segments) =>
        (method, segments) switch
        {
            // Users
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

            // Permissions
            ("GET",    ["permissions", ..])              => PermissionCodes.PermissionsRead,
            ("POST",   ["permissions"])                  => PermissionCodes.PermissionsWrite,
            ("PUT" or "PATCH", ["permissions", _])       => PermissionCodes.PermissionsWrite,
            ("DELETE", ["permissions", _])               => PermissionCodes.PermissionsDelete,

            // Settings
            ("GET",    ["settings"])                     => PermissionCodes.SettingsRead,
            ("PATCH",  ["settings"])                     => PermissionCodes.SettingsWrite,

            // Departments
            ("GET",    ["departments", ..])              => PermissionCodes.DepartmentsRead,
            ("POST",   ["departments"])                  => PermissionCodes.DepartmentsWrite,
            ("PUT" or "PATCH", ["departments", _])       => PermissionCodes.DepartmentsWrite,
            ("DELETE", ["departments", _])               => PermissionCodes.DepartmentsDelete,

            // Job Titles
            ("GET",    ["job-titles", ..])               => PermissionCodes.JobTitlesRead,
            ("POST",   ["job-titles"])                   => PermissionCodes.JobTitlesWrite,
            ("PUT" or "PATCH", ["job-titles", _])        => PermissionCodes.JobTitlesWrite,
            ("DELETE", ["job-titles", _])                => PermissionCodes.JobTitlesDelete,

            // Approval Items + Steps
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

            // Payment Requests
            ("GET",    ["payment-requests", ..])         => PermissionCodes.PaymentRequestsRead,
            ("POST",   ["payment-requests"])             => PermissionCodes.PaymentRequestsWrite,
            ("PUT",    ["payment-requests", _])          => PermissionCodes.PaymentRequestsWrite,
            ("PATCH",  ["payment-requests", _, "submit"])       => PermissionCodes.PaymentRequestsWrite,
            ("PATCH",  ["payment-requests", _, "payment-date"]) => PermissionCodes.PaymentRequestsWrite,
            ("PATCH",  ["payment-requests", _])                 => PermissionCodes.PaymentRequestsWrite,
            ("DELETE", ["payment-requests", _])          => PermissionCodes.PaymentRequestsDelete,

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
            ("GET",    ["payroll"])                      => PermissionCodes.PayrollRead,

            // Reports
            ("GET",    ["reports", "overtime"])                    => PermissionCodes.ReportsOvertimeRead,
            ("GET",    ["reports", "payment"])                     => PermissionCodes.ReportsPaymentRead,
            ("GET",    ["reports", "project-water-level"])         => PermissionCodes.ReportsProjectWaterLevelRead,

            // Approval Tasks
            ("GET",    ["approval-tasks", ..])           => PermissionCodes.ApprovalTasksRead,
            ("PATCH",  ["approval-tasks", _, _, "review"]) => PermissionCodes.ApprovalTasksWrite,

            _ => null
        };

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
