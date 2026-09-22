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
    // 加班報表的「加班費金額」欄為薪資性資訊（依核准當下底薪試算），
    // 與能否進入報表頁（:read）刻意分離 —— 沒有此碼者頁面照進、時數與補償方式照看，
    // 只是金額欄不回傳、不顯示。
    public const string ReportsOvertimeAmount         = "reports-overtime:amount";
    public const string ReportsPaymentRead            = "reports-payment:read";
    public const string ReportsProjectWaterLevelRead  = "reports-project-water-level:read";
    // 專案水位表的「總專案水位」欄（分母＝契約金額，含公司保留 40%）為管理層資訊，
    // 與能否進入報表頁（:read）刻意分離 —— 沒有此碼者頁面照進，只是該欄不回傳、不顯示。
    public const string ReportsProjectWaterLevelTotal = "reports-project-water-level:total";
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

    // ── 四週彈性工時：個人排班 ──────────────────────────────────────────
    // ⚠ 這四碼**刻意不進 PermissionConfiguration.HasData**：Id 1–77 已全滿無空號，
    //   而 PermissionHandler.CreateAsync 以 max(Id)+1 配號，正式站 78+ 很可能已被
    //   UI 建立的權限占用 —— 寫死就撞 PK，而 Program.cs 啟動時的 MigrateAsync 一拋例外
    //   整個 Function App 就起不來。改由 raw SQL migration 以 Code 為準動態取號，
    //   與「UI 建立的權限」同一處置（同樣不受 EF seed 管理）。

    /// <summary>檢視個人排班月曆。一般同仁即持有此碼。</summary>
    public const string ShiftScheduleRead    = "shift-schedule:read";
    /// <summary>排定／修改自己的班表。</summary>
    public const string ShiftScheduleWrite   = "shift-schedule:write";
    /// <summary>
    /// 檢視**全公司**排班。未持有者的可見範圍退回 ProjectAccessScope（部門可見性四旗標）。
    /// ⚠ 刻意以權限碼判定，**不得硬編 JobTitle.Level ≤ 3**（組織改制後職級對應會漂移，
    /// 前例見 DepartmentCodes 的 'FIN' 硬編碼事故）。
    /// </summary>
    public const string ShiftScheduleViewAll = "shift-schedule:view-all";
    /// <summary>進入〈出勤／排休總覽表〉報表頁。</summary>
    public const string ReportsShiftScheduleRead = "reports-shift-schedule:read";

    /// <summary>檢視活動日清單。</summary>
    public const string ActivityDaysRead = "activity-days:read";
    /// <summary>
    /// 排定／改期活動日並勾選預定人力（規格上是「各部門協理主管」）。
    /// ⚠ 同樣以權限碼判定，**不得硬編 `JobTitle.Level ≤ 3`** —— 組織改制後職級對應會漂移。
    /// 可排定的部門範圍另由 <c>ProjectAccessScope</c> 控管（只能排自己看得到的部門）。
    /// </summary>
    public const string ActivityDaysWrite = "activity-days:write";
}

/// <summary>
/// 非正式環境的訊息標記（例：<c>【測試站｜此為測試訊息】</c>）。**正式站不設此值**，輸出完全不變。
///
/// 設定鍵 <c>App:EnvironmentLabel</c>（Azure App Setting <c>App__EnvironmentLabel</c>）。
///
/// <b>為什麼要有</b>：測試站與正式站送的是同一批真實員工看得懂的訊息，
/// 收件人手機／信箱上完全分不出哪一則是測試 —— 2026-09 曾因此讓 29 位同仁
/// 各收到 2 則測試 LINE 推播而無從判斷真偽，且**訊息送出後收不回來**。
///
/// <b>為什麼 LINE 與 Email 共用同一個設定鍵</b>：拆成兩個鍵的必然結果是有人只設了一邊，
/// 症狀是「LINE 標了、Email 沒標」而且不會有任何錯誤。一個概念就給一個鍵。
///
/// 兩個通道各自的套用位置（都是該通道的**唯一收斂點**，故日後新增模板自動涵蓋）：
///   · LINE  → <c>LineFlexMessageBuilder.BuildBubble</c>（altText ＋ header 第一行）
///   · Email → <c>EmailService.SendAsync</c>（主旨前綴 ＋ 內文頂端橫幅）
///
/// ⚠ **Email 的曝險比 LINE 大**：`SystemSetting.ApprovalEmailEnabled` 只管簽核通知，
///   **帳號通知信（UserHandler）與薪資明細信（PayrollHandler）完全不受它管制**，
///   關掉那個開關並不會讓測試站停止寄信給真實員工。
/// </summary>
public static class EnvironmentLabel
{
    public const string ConfigKey = "App:EnvironmentLabel";

    /// <summary>目前值；空字串＝正式環境。由 <c>Program.cs</c> 於啟動時寫入。</summary>
    public static string Value { get; set; } = "";

    public static bool HasValue => !string.IsNullOrWhiteSpace(Value);

    /// <summary>加在標題 / 主旨前面；無標記時原樣回傳。</summary>
    public static string Prefix(string text) => HasValue ? $"{Value}{text}" : text;
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
/// AuthHandler（登入時自動補打下班卡＝上班打卡時間 + FullDayHours + 午休，一律 +9）。
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

    // ── 四週彈性工時：時段版本化（flexible-work-hours.md §10.2）──────────────
    // 上面 5 個 const 是**舊制**的值，等同 Legacy，保留供尚未遷移的消費點使用。
    // 新消費點一律改吃 For(date, switchDate) 取回的 WorkdaySchedule。

    /// <summary>舊制：08:00–17:00、午休 12:00–13:00、半天 am 08:00–12:00 / pm 13:00–17:00。</summary>
    public static readonly WorkdaySchedule Legacy = new(
        Start:          new TimeOnly(8, 0),
        LunchStart:     new TimeOnly(12, 0),
        LunchEnd:       new TimeOnly(13, 0),
        End:            new TimeOnly(17, 0),
        HalfDayAmEnd:   new TimeOnly(12, 0),
        HalfDayPmStart: new TimeOnly(13, 0),
        FullDayHours:   8m);

    /// <summary>
    /// 新制（四週彈性工時）：09:00–18:00、午休 12:30–13:30、半天**全假別統一** am 09:00–13:00 / pm 13:00–18:00。
    /// 半天分界點 13:00 刻意落在午休（12:30–13:30）之中，好處是上下午接得起來、不重疊也不留空檔。
    /// 舊制「補休上午 09:00–13:00」的假別特例在新制取消（全假別統一，見 §10.2）。
    /// </summary>
    public static readonly WorkdaySchedule Flexible = new(
        Start:          new TimeOnly(9, 0),
        LunchStart:     new TimeOnly(12, 30),
        LunchEnd:       new TimeOnly(13, 30),
        End:            new TimeOnly(18, 0),
        HalfDayAmEnd:   new TimeOnly(13, 0),
        HalfDayPmStart: new TimeOnly(13, 0),
        FullDayHours:   8m);

    /// <summary>
    /// 依「該筆資料自己的日期」選用時段，**不是依今天**。
    /// <paramref name="switchDate"/> 來自 <c>SystemSetting.FlexibleWorkStartDate</c>；
    /// null（尚未切換）或 date 早於切換日 → 舊制。
    ///
    /// 呼叫端傳的 date：請假單傳 <c>StartDate</c>、打卡傳 <c>RecordDate</c>、報表傳該列日期。
    /// 舊資料不遷移（§10.2），拿新制時段去比對舊單會生出不存在的「未打卡」與錯誤的請假時段顯示。
    /// </summary>
    public static WorkdaySchedule For(DateTime date, DateTime? switchDate) =>
        IsFlexible(date, switchDate) ? Flexible : Legacy;

    /// <summary>
    /// 該日期是否已套用四週彈性工時。與 <see cref="For"/> 同一個判準，抽出來供
    /// 「不是時段、但同樣以切換日分歧」的規則使用（例：國定假日是否另立第四種加班日別）。
    /// </summary>
    public static bool IsFlexible(DateTime date, DateTime? switchDate) =>
        switchDate is { } s && date.Date >= s.Date;
}

/// <summary>
/// 一組「每日工作時段」。四週彈性工時上線後系統內同時存在新舊兩套（舊單不遷移），
/// 故時段不能再是編譯期 const int —— 且新制午休 12:30 本來就表達不了整點。
/// 取得方式一律走 <see cref="WorkdayHours.For"/>。
/// </summary>
/// <param name="Start">上班時刻。</param>
/// <param name="LunchStart">午休開始（不計入工時）。</param>
/// <param name="LunchEnd">午休結束。</param>
/// <param name="End">下班時刻。</param>
/// <param name="HalfDayAmEnd">上午半天假的訖時刻（舊制 12:00、新制 13:00）。</param>
/// <param name="HalfDayPmStart">下午半天假的起時刻（舊制 13:00、新制 13:00）。</param>
/// <param name="FullDayHours">全日工時，新舊皆 8 —— 時數換算不變，只有時刻平移。</param>
public sealed record WorkdaySchedule(
    TimeOnly Start,
    TimeOnly LunchStart,
    TimeOnly LunchEnd,
    TimeOnly End,
    TimeOnly HalfDayAmEnd,
    TimeOnly HalfDayPmStart,
    decimal  FullDayHours)
{
    /// <summary>半天假時數，恆 4 —— **刻意不由時鐘長度推導**（新制上午 09:00–13:00 看似 4 小時、
    /// 下午 13:00–18:00 看似 5 小時，但兩者都記 4）。這些時刻是「代表時刻」，不是工時計算的分子。</summary>
    public const decimal HalfDayHours = 4m;

    /// <summary>午休長度（小時）。</summary>
    public decimal LunchHours => (decimal)(LunchEnd - LunchStart).TotalHours;

    /// <summary>
    /// 從上班打卡到應下班的「時鐘時數」＝ 全日工時 ＋ 午休（新舊皆 9）。
    /// 語意等同既有的 <c>AttendanceAutoClockService.AutoClockOutHours</c>，
    /// 也是規格 §2.1「應下班時間 ＝ 實際上班打卡時刻 ＋ 9 小時」的來源，兩處應共用本屬性。
    /// </summary>
    public decimal ClockDayHours => FullDayHours + LunchHours;
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
    /// canCloseAdvance / canCloseTravelRequest / checkPaidDisabledHint），兩處須同步。
    /// 「總監室簽核」tab 的可見性 2026-08 起改用 DirectorPendingView（另含會計室），已不共用本集合。
    /// </summary>
    public static readonly IReadOnlySet<string> FinanceStep =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Finance,
            FinanceEn,
        };

    /// <summary>
    /// 「總監室簽核」頁籤檢視權（2026-08 擴充會計室）：財務管理部 + 會計室（各含舊短碼與改制後英文全名）。
    /// 純檢視用途，刻意與寫入型的 FinanceStep（撥款日 / 撥款明細 / 結案 / 支票已支付）分開，
    /// 避免會計室因看得到頁籤而一併取得撥款與結案權限。
    /// 注意：本集合只決定「看不看得到頁籤」（後端以 scope=director 判定，涵蓋四種狀態），
    /// 資料範圍另由 ApprovalTaskHandler 的 directorStepDeptId 控制
    /// （財務管理部 / Superadmin＝全部；其他＝限流程中含自部門關卡者）。
    /// 前端對應清單見 approval-task-list.ts 的 DIRECTOR_SCOPE_DEPT_CODES，兩處須同步。
    /// </summary>
    public static readonly IReadOnlySet<string> DirectorPendingView =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Finance,
            FinanceEn,
            Accounting,
            AccountingEn,
        };

    /// <summary>
    /// 「可撥款」最終核准通知的收件部門（2026-09 擴充會計室）：財務管理部 + 會計室（各含舊短碼與改制後英文全名）。
    /// 財務管理部是實際撥款者；會計室需知悉「總監已簽核、單子走完流程」才能接著入帳，
    /// 過去只寄財務管理部，會計室得自己去「總監室簽核」頁籤輪詢。
    /// 純通知用途，刻意與寫入型的 <see cref="FinanceStep"/>（撥款明細 / 結案 / 支票已支付）分開 ——
    /// 會計室收得到信，但這些寫入型操作仍不可執行。
    /// </summary>
    public static readonly IReadOnlySet<string> PaymentApprovedNotify =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Finance,
            FinanceEn,
            Accounting,
            AccountingEn,
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

/// <summary>
/// 請假「逐日時段」代碼（LeaveDayExpander 展開結果的單一真相）。
/// 半天 / 小時假的時段本身編碼在 LeaveRequest.StartDate / EndDate 的時分上（08 / 12 / 13 / 17 或整點），
/// 展開成逐日後那份資訊會遺失，故由 LeaveDay.Segment 帶出，供出缺勤報表顯示與應出勤時段計算。
/// 前端對應定義見 leave-request.model.ts 的 LeaveDaySegment / LEAVE_DAY_SEGMENT_LABELS，兩處須同步。
/// </summary>
public static class LeaveDaySegments
{
    /// <summary>整個工作日 08:00–17:00</summary>
    public const string Full    = "full";
    /// <summary>上半天 08:00–12:00</summary>
    public const string Am      = "am";
    /// <summary>下半天 13:00–17:00</summary>
    public const string Pm      = "pm";
    /// <summary>小時假的任意區段（實際時分見 LeaveDay.Start / End）</summary>
    public const string Partial = "partial";
}
