namespace Jabez.Api.Common;

public static class PermissionCodes
{
    public const string SettingsRead       = "settings:read";
    public const string SettingsWrite      = "settings:write";
    public const string UsersRead         = "users:read";
    public const string UsersWrite        = "users:write";
    public const string UsersDelete       = "users:delete";
    public const string RolesRead         = "roles:read";
    public const string RolesWrite        = "roles:write";
    public const string RolesDelete       = "roles:delete";
    public const string PermissionsRead   = "permissions:read";
    public const string PermissionsWrite  = "permissions:write";
    public const string PermissionsDelete = "permissions:delete";
    public const string DepartmentsRead   = "departments:read";
    public const string DepartmentsWrite  = "departments:write";
    public const string DepartmentsDelete = "departments:delete";
    public const string JobTitlesRead     = "job-titles:read";
    public const string JobTitlesWrite    = "job-titles:write";
    public const string JobTitlesDelete   = "job-titles:delete";
    public const string VendorsRead       = "vendors:read";
    public const string VendorsWrite      = "vendors:write";
    public const string VendorsDelete     = "vendors:delete";
    public const string ApprovalsRead          = "approvals:read";
    public const string ApprovalsWrite         = "approvals:write";
    public const string ApprovalsDelete        = "approvals:delete";
    public const string ProjectsRead           = "projects:read";
    public const string ProjectsWrite          = "projects:write";
    public const string ProjectsDelete         = "projects:delete";
    public const string PaymentRequestsRead    = "payment-requests:read";
    public const string PaymentRequestsWrite   = "payment-requests:write";
    public const string PaymentRequestsDelete  = "payment-requests:delete";
    public const string ApprovalTasksRead          = "approval-tasks:read";
    public const string ApprovalTasksWrite         = "approval-tasks:write";
    public const string ApprovalTasksBatchApprove  = "approval-tasks:batch-approve";
    public const string LeaveRequestsRead      = "leave-requests:read";
    public const string LeaveRequestsWrite     = "leave-requests:write";
    public const string LeaveRequestsDelete    = "leave-requests:delete";
    public const string TravelRequestsRead     = "travel-requests:read";
    public const string TravelRequestsWrite    = "travel-requests:write";
    public const string TravelRequestsDelete      = "travel-requests:delete";
    public const string OvertimeRequestsRead      = "overtime-requests:read";
    public const string OvertimeRequestsWrite     = "overtime-requests:write";
    public const string OvertimeRequestsDelete    = "overtime-requests:delete";
    public const string AttendancesRead             = "reports-attendance:read";
    public const string AttendancesWrite            = "reports-attendance:read";
    public const string InsuranceBracketsRead       = "insurance-brackets:read";
    public const string InsuranceBracketsWrite      = "insurance-brackets:write";
    public const string InsuranceBracketsDelete     = "insurance-brackets:delete";
    public const string PayrollRead                 = "payroll:read";
    public const string PayrollWrite                = "payroll:write";
    public const string ReportsOvertimeRead           = "reports-overtime:read";
    public const string ReportsPaymentRead            = "reports-payment:read";
    public const string ReportsProjectWaterLevelRead  = "reports-project-water-level:read";
    public const string AdvanceRequestsRead             = "advance-requests:read";
    public const string AdvanceRequestsWrite            = "advance-requests:write";
    public const string AdvanceRequestsDelete           = "advance-requests:delete";
    public const string WriteOffRequestsRead            = "write-off-requests:read";
    public const string WriteOffRequestsWrite           = "write-off-requests:write";
    public const string WriteOffRequestsDelete          = "write-off-requests:delete";
    public const string TravelWriteOffRequestsRead      = "travel-write-off-requests:read";
    public const string TravelWriteOffRequestsWrite     = "travel-write-off-requests:write";
    public const string TravelWriteOffRequestsDelete    = "travel-write-off-requests:delete";
    public const string HolidayTravelRequestsRead       = "holiday-travel-requests:read";
    public const string HolidayTravelRequestsWrite      = "holiday-travel-requests:write";
    public const string HolidayTravelRequestsDelete     = "holiday-travel-requests:delete";
    public const string CalendarDaysRead                = "calendar-days:read";
    public const string CalendarDaysWrite               = "calendar-days:write";
    public const string CalendarDaysDelete              = "calendar-days:delete";
    public const string TravelPaymentRequestsRead    = "travel-payment-requests:read";
    public const string TravelPaymentRequestsWrite   = "travel-payment-requests:write";
    public const string TravelPaymentRequestsDelete  = "travel-payment-requests:delete";
}

public static class RoleNames
{
    public const string Admin   = "admin";
    public const string Manager = "manager";
    public const string Viewer  = "viewer";
}

/// <summary>
/// 部門代碼常數。用於「撥款 / 退款 / 結案 / 批次核准」等業務操作權限的硬編碼判斷。
/// 注意：可見性 SeeAll 已改由 Department.CanSeeAll 旗標驅動（見 CLAUDE.md「部門可見性規則」），與此常數無關。
/// </summary>
public static class DepartmentCodes
{
    public const string Accounting = "AC";         // 會計部
    public const string Finance    = "FIN";        // 行政財務部
    public const string HQAdmin    = "Jabez HQ";   // 雅比斯總公司管理部
    public const string Executive  = "CEO";        // 總監室

    /// <summary>
    /// 財務 / 管理 / 總監級部門：成員可執行撥款日 / 退款日 / 結案 / 批次核准等業務操作。
    /// </summary>
    public static readonly IReadOnlySet<string> FinancialAndAbove =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Accounting,
            Finance,
            HQAdmin,
            Executive,
        };
}
