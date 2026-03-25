namespace Jabez.Api.Models.Dtos;

// ── 明細項目 DTO ────────────────────────────────────────────────────────────

public sealed record TravelRequestItemDto(
    int     Id,
    string  Category,
    int     SeqNo,
    string  ItemName,
    decimal UnitPrice,
    string  Quantity,
    decimal TotalPrice,
    string? Note,
    int     SortOrder);

public sealed record TravelRequestItemRequest(
    string  Category,
    int     SeqNo,
    string  ItemName,
    decimal UnitPrice,
    string  Quantity,
    decimal TotalPrice,
    string? Note    = null,
    int     SortOrder = 0);

// ── 主申請單 DTO ────────────────────────────────────────────────────────────

public sealed record TravelRequestDto(
    int       Id,
    string    EmployeeName,
    string    Destination,
    DateTime  StartDate,
    DateTime  EndDate,
    decimal   GrandTotal,
    string    Purpose,
    int?      ProjectId,
    string?   ProjectCode,
    string?   ProjectName,
    bool      IsHolidayTravel,
    string    ApprovalStatus,  // draft | pending | approved | rejected | returned
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId       = null,
    int?      CurrentStepOrder     = null,
    Guid?     ReviewedById         = null,
    TravelRequestItemDto[]?      Items               = null,
    DesignatedReviewerDto[]?     DesignatedReviewers = null,
    bool                         IsClosed            = false,
    DateTime?                    ClosedAt            = null,
    decimal?                     RefundAmount         = null,
    DateTime?                    EstimatedPaymentDate = null,
    DateTime?                    PaidAt               = null,
    DateTime?                    EstimatedRefundDate  = null,
    DateTime?                    RefundedAt           = null);

public sealed record CreateTravelRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId       = null,
    string   Destination          = "",
    DateTime StartDate            = default,
    DateTime EndDate              = default,
    string   Purpose              = "",
    int?     ProjectId            = null,
    bool     IsHolidayTravel      = false,
    TravelRequestItemRequest[]?  Items               = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);

public sealed record UpdateTravelRequestRequest(
    string?   Destination,
    DateTime? StartDate,
    DateTime? EndDate,
    string?   Purpose,
    int?      ProjectId,
    bool?     IsHolidayTravel,
    TravelRequestItemRequest[]?  Items               = null,
    DesignatedReviewerRequest[]? DesignatedReviewers = null);
