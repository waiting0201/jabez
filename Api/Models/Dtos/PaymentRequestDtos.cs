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
    string           RequestNo,
    string           Type,
    int              ProjectId,
    string           ProjectCode,
    string           ProjectName,
    InvoiceItemDto[] Invoices,
    decimal          TotalAmount,
    string           ApprovalStatus,
    string?          SubmittedBy,
    DateTime         CreatedAt,
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
    string   Reason);

/// <summary>假日活動每位人員（申請人 + 參與者）的津貼預估，依 round(BaseSalary / 30) × HolidayDays 計算，與 PayrollReadService 一致</summary>
public sealed record HolidayAllowanceDto(
    Guid    UserId,
    string  UserName,
    int     Allowance,
    bool    IsApplicant);

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
    string?   PaymentStatus = null)
{
    public TravelRequestItemDto[] Items { get; init; } = Items ?? Array.Empty<TravelRequestItemDto>();
}

public sealed record OvertimeTaskDetailDto(
    int      OvertimeRequestId,
    DateTime OvertimeDate,
    string?  ProjectIds,
    decimal  EstimatedHours,
    string   Reason);

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
    PreReviewTaskDetailDto?     PreReviewDetail         = null);  // 預審申請詳情

/// <summary>簽核作業「申請人」下拉選項（僅財務體系部門可取得）。</summary>
public sealed record ApprovalTaskApplicantDto(Guid Id, string Name);
