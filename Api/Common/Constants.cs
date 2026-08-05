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
    // 出勤打卡（員工本人）：讀自己今日打卡 / 執行上下班・加班打卡
    public const string AttendancesRead             = "attendances:read";
    public const string AttendancesWrite            = "attendances:write";
    // 出缺勤報表（管理端）：查全公司打卡紀錄 / 人工修改他人紀錄。與上面兩碼刻意分離 ——
    // 前者是「員工對自己」，後者是「管理者對別人」，權限對象不同不可共用。
    public const string ReportsAttendanceRead       = "reports-attendance:read";
    public const string ReportsAttendanceWrite      = "reports-attendance:write";
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
    public const string LineQuotaRead                = "line-quota:read";
    public const string PreReviewRequestsRead        = "pre-review-requests:read";
    public const string PreReviewRequestsWrite       = "pre-review-requests:write";
    public const string PreReviewRequestsDelete      = "pre-review-requests:delete";
}

public static class RoleNames
{
    public const string Admin   = "admin";
    public const string Manager = "manager";
    public const string Viewer  = "viewer";
}

/// <summary>
/// 工作日標準時段：08:00–17:00（全日 8 小時），午休 12:00–13:00
/// （與 half_day 的 am 08:00–12:00 / pm 13:00–17:00 一致）。
/// 消費點：LeaveRequestHandler（Hour 單位時數計算）、AttendanceHandler（全日請假判定）、
/// AuthHandler（登入時自動補打下班卡＝上班打卡時間 + FullDayHours）。
/// 刻意不與 SystemSetting.WorkStartTime / WorkEndTime（預設 09:00 / 18:00）合併 ——
/// 後者僅供打卡提醒推播的時點判斷，語意不同。
/// 前端對應常數見 leave-request.model.ts 的 WORKDAY_START_HOUR / WORKDAY_END_HOUR，兩處須同步。
/// </summary>
public static class WorkdayHours
{
    public const int StartHour      = 8;
    public const int LunchStartHour = 12;
    public const int LunchEndHour   = 13;
    public const int EndHour        = 17;
    public const int FullDayHours   = 8;   // 全日實際工時（EndHour - StartHour - 午休 1 小時）
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

    // 2026 組織改制後新的部門代碼（Code 改為英文全名）。舊短碼一併保留以相容尚未改制的環境。
    public const string AccountingEn = "Accounting Department";            // 會計室（財務管理部下）
    public const string FinanceEn    = "Financial Management Department";  // 財務管理部
    public const string ExecutiveEn  = "Office of the Director";           // 總監室

    /// <summary>
    /// 財務 / 管理 / 總監級部門：成員可執行撥款日 / 退款日 / 結案 / 批次核准等業務操作。
    /// 同時涵蓋舊短碼（CEO/FIN/AC/Jabez HQ）與改制後英文全名碼，避免改組織就失效。
    /// 前端對應清單見 approval-task-list.ts 的 PAYMENT_FILTER_DEPT_CODES，兩處須同步。
    /// </summary>
    public static readonly IReadOnlySet<string> FinancialAndAbove =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Accounting,
            Finance,
            HQAdmin,
            Executive,
            AccountingEn,
            FinanceEn,
            ExecutiveEn,
        };

    /// <summary>
    /// 財務撥款步驟專用：簽核流程中「填撥款日 / 撥款明細 / 結案」的財務節點所綁定之部門 Code。
    /// 僅財務管理部（舊短碼 FIN + 改制後英文全名 FinanceEn），刻意不含 CEO / 總監 / HQ / 會計，
    /// 避免把上層核准步驟誤判為撥款填寫節點而擋住簽核。
    /// 2026-07 起「支票已支付」註記（WriteOffRequestHandler.UpdateCheckPaymentsAsync）亦改用本集合，
    /// 與撥款判定同範圍；此處是比對**登入者自身部門**，不是比對步驟綁定部門。
    /// 前端對應判定見 approval-task-review.ts 的 FINANCE_STEP_DEPT_CODES（canSetPaymentDate /
    /// canCloseAdvance / canCloseTravelRequest / checkPaidDisabledHint）與 approval-task-list.ts 的
    /// FINANCE_STEP_DEPT_CODES（總監待簽核 tab 可見性），三處須同步。
    /// </summary>
    public static readonly IReadOnlySet<string> FinanceStep =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Finance,
            FinanceEn,
        };

    /// <summary>
    /// 指定審核者「部門最高層級自動略過」限定部門（2026-07 新增）：
    /// 僅當第一個指定審核步驟（先選部門模式）選的部門屬於此集合時，
    /// 首位指定人若為該部門職稱最高者，才會自動略過其後的指定審核步驟；其餘部門一律不抑制。
    /// 前端對應清單見 designated-reviewers-picker.ts 的 DESIGNATED_TOP_LEVEL_SUPPRESSION_DEPT_CODES，兩處須同步。
    /// </summary>
    public static readonly IReadOnlySet<string> DesignatedTopLevelSuppression =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Operations Department",
            "Brand Department(疆界地域美學)",
        };
}

/// <summary>
/// 假日執行活動參與人員逐日時段（DB 值 + 天數權重的單一真相）。
/// 前端對應定義見 holiday-travel-request.model.ts 的 ParticipantDaySlot / PARTICIPANT_SLOT_LABELS，兩處須同步。
/// </summary>
public static class ParticipantDateSlots
{
    public const string Full = "full";
    public const string Am   = "am";
    public const string Pm   = "pm";

    /// <summary>時段對應天數權重：全天 1.0、上/下半天 0.5；未知 / 空值一律視為全天（向後相容 Slot 欄位上線前的舊資料）</summary>
    public static decimal Weight(string? slot) => slot switch
    {
        Am or Pm => 0.5m,
        _        => 1m,
    };

    /// <summary>正規化：null / 空字串 / 未知值 → full</summary>
    public static string Normalize(string? slot) => slot is Am or Pm ? slot : Full;

    /// <summary>驗證用：空值視為合法（等同 full），其餘只接受 full / am / pm</summary>
    public static bool IsValid(string? slot) => string.IsNullOrEmpty(slot) || slot is Full or Am or Pm;
}
