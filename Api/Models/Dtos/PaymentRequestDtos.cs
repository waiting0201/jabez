namespace Jabez.Api.Models.Dtos;

// ── 指定審核者共用 DTOs ───────────────────────────────────────────────────────

/// <summary>指定審核者回應 DTO</summary>
public sealed record DesignatedReviewerDto(
    int       Id,
    Guid      ReviewerId,
    string    ReviewerName,
    int       StepOrder,
    string    Status,        // "pending" | "approved" | "returned"
    DateTime? ReviewedAt,
    string?   Comment,
    int       ApprovalStepOrder = 0,   // 所屬 designated 步驟（ApprovalStep.StepOrder）
    int?      SelectedDepartmentId = null,
    string?   SelectedDepartmentName = null);

/// <summary>指定審核者請求 DTO（用於 Create/Update/Submit）</summary>
public sealed record DesignatedReviewerRequest(
    Guid ReviewerId,
    int  StepOrder,
    int  ApprovalStepOrder = 0,        // 所屬 designated 步驟；舊 payload 未帶＝0，由 Helper 補成唯一 designated step
    int? SelectedDepartmentId = null);

// ── 整單批次附件共用 DTO（照片 / PDF）────────────────────────────────────────

/// <summary>整單批次附件回應 DTO（請款一般請款 / 預支沖銷共用）</summary>
public sealed record AttachmentDto(
    int     Id,
    string  FileName,
    string? FileUrl);

// ── Invoice DTOs ──────────────────────────────────────────────────────────────

public sealed record InvoiceItemDto(
    int       Id,
    string    FileName,
    string    InvoiceNo,
    decimal   Amount,
    string?   ItemName,
    string?   Note,
    string?   FileUrl,
    DateTime? InvoiceDate = null);

public sealed record InvoiceItemRequest(
    string    FileName,
    string    InvoiceNo,
    decimal   Amount,
    string?   ItemName,
    string?   Note,
    DateTime? InvoiceDate = null);

public sealed record PaymentRequestDto(
    int              Id,
    string?          RequestNo,
    string           Type,
    int              ProjectId,
    string           ProjectCode,
    string           ProjectName,
    InvoiceItemDto[] Invoices,
    decimal          TotalAmount,
    string           ApprovalStatus,
    string?          SubmittedBy,
    DateTime         CreatedAt,
    DateTime?        SubmittedAt,      // 送簽日期（申請日期）；草稿為 null
    DateTime?        ReviewedAt,
    string?          ReviewNote,
    string?          Reason = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null,
    int?             VendorId    = null,
    string?          VendorName  = null,
    string?          VendorTaxId = null,
    InstallmentDto[]? Installments = null,
    string?          PaymentStatus = null,
    AttachmentDto[]? Attachments = null);

public sealed record CreatePaymentRequestRequest(
    string              Type,
    int                 ProjectId,
    InvoiceItemRequest[] Invoices,
    string?             Reason = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdatePaymentRequestRequest(
    string?              Type,
    int?                 ProjectId,
    InvoiceItemRequest[] Invoices,
    string?             Reason = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

// 退還差額匯款日期
public sealed record RefundDateRequest(DateTime? RefundedAt);

// 審核動作（核准 / 退回修改 / 拒絕）
public sealed record ReviewPaymentRequestRequest(
    string    Action,           // "approved" | "returned" | "rejected"
    string?   ReviewNote,
    string    ApplicationType,  // "payment_request" | "leave" | "travel" | "overtime" | "advance" | "write_off"
    DateTime? EstimatedRefundDate,  // 預支 / 出差沖銷時填入：上層預支 / 出差申請的預計退款日
    DateTime? RefundedAt,           // 預支 / 出差沖銷時填入：上層預支 / 出差申請的實際退款日
    bool?     CloseAdvance,         // 預支結案（僅沖銷申請的財務部步驟使用）
    List<InstallmentInput>? Installments = null);  // 撥款類於財務步驟核准時一併送出的撥款明細（與審核同交易原子寫入）

public sealed record ApprovalRecordDto(
    int      StepOrder,
    string   Action,          // "approved" | "returned" | "rejected"
    string   ReviewedBy,
    DateTime ReviewedAt,
    string?  ReviewNote,
    string?  OnBehalfOf,      // 代理審核：代替誰審核（null 表示非代理）
    bool     IsEscalated,     // 是否為升級審核
    string?  ReviewerSignatureUrl = null,   // 審核者簽名檔 URL
    string?  ReviewerJobTitle    = null,    // 審核者職稱（顯示用）
    string?  ReviewerDepartmentName = null, // 審核者部門（指定審核步驟顯示用，區分同名審核者）
    int?     ReviewerJobTitleLevel = null,  // 審核者職稱層級（PDF 簽名欄判定總監 Level=1 用，避免依賴職稱名稱）
    int      RoundNo = 1);                  // 簽核批次（僅 advance 追加預支會 > 1）

// ── ApprovalTask 多型 DTOs ─────────────────────────────────────────────────

public sealed record ApprovalFlowStepDto(
    int     StepOrder,
    string? DepartmentName,
    string? DepartmentCode,
    string? JobTitleName,
    bool    UseDirectSupervisor,
    bool    UseApplicantDesignated,
    string? Note,
    int?    JobTitleLevel = null); // 職稱層級（PDF 簽名欄判定總監 Level=1 用，避免依賴職稱名稱）

public sealed record ApprovalFlowDto(
    int                  Id,
    string               Name,
    ApprovalFlowStepDto[] Steps);

public sealed record PaymentTaskDetailDto(
    int              PaymentRequestId,
    string           RequestNo,
    string           PaymentType,
    string           ProjectCode,
    string           ProjectName,
    InvoiceItemDto[] Invoices,
    decimal          TotalAmount,
    string?          Reason = null,
    int?             VendorId             = null,
    string?          VendorName           = null,
    string?          VendorTaxId          = null,
    string?          VendorContactPerson  = null,
    string?          VendorPhone          = null,
    string?          VendorBankAccount    = null,
    string?          VendorAddress        = null,
    InstallmentDto[]? Installments        = null,
    string?          PaymentStatus        = null,   // Unpaid | PartiallyPaid | FullyPaid
    AttachmentDto[]? Attachments          = null);

public sealed record LeaveTaskDetailDto(
    int      LeaveRequestId,
    string   LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal  Hours,
    string   Reason,
    string?  RequestNo = null);   // LV-yyyyMMdd-NNN（草稿無單號，簽核頁看到的一律已送簽故必有值）

/// <summary>銷假申請詳情（含原請假單資訊與被取消的逐日清單）</summary>
public sealed record LeaveRevocationTaskDetailDto(
    int      LeaveRevocationId,
    int      LeaveRequestId,
    string   LeaveType,
    DateTime LeaveStartDate,
    DateTime LeaveEndDate,
    decimal  LeaveHours,           // 原請假時數（OriginalHours ?? Hours）
    string   LeaveReason,
    decimal  RevokedHours,
    string   Reason,
    LeaveRevocationDateDto[] Dates,
    string?  RequestNo = null);   // LVR-yyyyMMdd-NNN

/// <summary>
/// 假日活動每位人員（申請人 + 參與者）的參與明細。
/// Days = 個人假日天數：參與者取 COALESCE(個人, 整單)（逐日勾選上/下半天者為 0.5 的倍數），申請人固定為整單。
/// Dates = 參與者逐日勾選的日期 + 時段（null＝全程參與；申請人不逐日故恆為 null），供簽核頁列出「幾月幾號 全天/上午/下午」。
/// **刻意不含個人津貼金額**：津貼＝round(BaseSalary / 30) × Days，逐人揭露等同揭露該員底薪，
/// 故只在 <see cref="TravelTaskDetailDto.HolidayAllowanceTotal"/> 回傳全單合計。
/// </summary>
public sealed record HolidayAllowanceDto(
    Guid    UserId,
    string  UserName,
    decimal Days,
    bool    IsApplicant,
    ParticipantDateDto[]? Dates = null);

public sealed record TravelTaskDetailDto(
    int       TravelRequestId,
    string    RequestNo,
    string    Destination,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   GrandTotal,
    string    Purpose,
    string?   ProjectCode,
    string?   ProjectName,
    bool      IsHolidayTravel,
    DateTime? EstimatedRefundDate,
    DateTime? RefundedAt,
    int?      HolidayDays = null,
    TravelRequestItemDto[] Items = null!,
    HolidayAllowanceDto[]? HolidayAllowances = null,
    InstallmentDto[]? Installments = null,
    string?   PaymentStatus = null,
    bool      IsClosed = false,
    DateTime? ClosedAt = null,
    decimal?  RefundAmount = null,
    decimal?  RefundedAmount = null,
    DateTime? AdvanceNeededDate = null,
    /// <summary>假日活動津貼合計（僅 IsHolidayTravel=true 時提供）；逐人金額不外流，只給合計。</summary>
    int?      HolidayAllowanceTotal = null)
{
    public TravelRequestItemDto[] Items { get; init; } = Items ?? Array.Empty<TravelRequestItemDto>();
}

public sealed record OvertimeTaskDetailDto(
    int      OvertimeRequestId,
    DateTime OvertimeDate,
    decimal  EstimatedHours,                      // 預估總時數（= Projects 合計）
    string   Reason,
    OvertimeProjectDto[]? Projects = null,        // 關聯專案明細（含各案時數）
    // 補償方式與加班費快照 —— 審核者必須看得到金額，否則是盲簽
    string   CompensationType  = "compensatory",  // compensatory | pay
    decimal? OvertimePayAmount = null,
    decimal? PayableHours      = null,
    bool?    IsHolidayOvertime = null,
    string?  RequestNo         = null);           // OT-yyyyMMdd-NNN

/// <summary>出差請款申請審核任務詳情 DTO</summary>
public sealed record TravelPaymentTaskDetailDto(
    int       TravelPaymentRequestId,
    string    RequestNo,
    string    Destination,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   GrandTotal,
    string    Purpose,
    string?   ProjectCode,
    string?   ProjectName,
    TravelPaymentRequestItemDto[] Items = null!,
    InstallmentDto[]? Installments = null,
    string?   PaymentStatus = null)
{
    public TravelPaymentRequestItemDto[] Items { get; init; } = Items ?? Array.Empty<TravelPaymentRequestItemDto>();
}

public sealed record ApprovalTaskDto(
    int                         Id,
    string                      ApplicationType,   // "payment_request" | "leave" | "travel" | "overtime" | "advance" | "write_off" | "travel_write_off" | "travel_payment" | "pre_review"
    string                      Title,
    string                      SubmittedBy,
    DateTime                    SubmittedAt,
    string                      Status,
    int                         CurrentStepOrder,
    DateTime?                   ReviewedAt,
    string?                     ReviewNote,
    ApprovalFlowDto?            Flow,
    PaymentTaskDetailDto?       PaymentDetail,
    LeaveTaskDetailDto?         LeaveDetail,
    TravelTaskDetailDto?        TravelDetail,
    OvertimeTaskDetailDto?      OvertimeDetail,
    AdvanceTaskDetailDto?       AdvanceDetail,
    WriteOffTaskDetailDto?      WriteOffDetail,
    TravelWriteOffTaskDetailDto? TravelWriteOffDetail,
    ApprovalRecordDto[]         ApprovalRecords,
    DesignatedReviewerDto[]?    DesignatedReviewers     = null,
    string?                     SubmittedBySignatureUrl = null,
    TravelPaymentTaskDetailDto? TravelPaymentDetail     = null,   // 出差請款申請詳情
    PreReviewTaskDetailDto?     PreReviewDetail         = null,   // 預審申請詳情
    LeaveRevocationTaskDetailDto? LeaveRevocationDetail = null,   // 銷假申請詳情
    StepReviewersDto[]?         StepReviewers           = null);  // 各關卡實際可簽核的人（僅 pending 單計算）

/// <summary>
/// 一個簽核關卡的可簽核者（簽核流程時間軸用）。
/// 上層級 / 指定審核這類動態關卡本身沒有人名可顯示，申請人與審核者都看不出「這關是誰簽」，
/// 故由後端以與授權判定同一套規則逐關解析後帶回；Reviewers 為空＝這關查無可簽核人員（單子會卡住）。
/// </summary>
public sealed record StepReviewersDto(
    int                  StepOrder,
    PendingReviewerDto[] Reviewers);

/// <summary>可簽核者顯示資訊。</summary>
/// <param name="IsEscalated">是否為升級審核指派（上層級關卡由上層部門主管接手）</param>
public sealed record PendingReviewerDto(
    Guid    Id,
    string  Name,
    string? JobTitleName,
    string? DepartmentName,
    bool    IsEscalated);

/// <summary>簽核作業「申請人」下拉選項（僅財務體系部門可取得）。</summary>
public sealed record ApprovalTaskApplicantDto(Guid Id, string Name);
