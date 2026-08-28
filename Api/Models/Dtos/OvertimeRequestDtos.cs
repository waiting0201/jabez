namespace Jabez.Api.Models.Dtos;

/// <summary>加班申請的關聯專案明細（讀取用；加班報表與簽核任務詳情共用）</summary>
public sealed record OvertimeProjectDto(
    int     ProjectId,
    string  ProjectCode,
    string  ProjectName,
    decimal EstimatedHours);

/// <summary>加班申請的關聯專案明細（寫入用；Create / Update 共用）</summary>
public sealed record OvertimeProjectRequest(
    int     ProjectId,
    decimal EstimatedHours);

/// <summary>加班費試算的單一分段（倍率 / 該段時數 / 該段金額，金額未捨入）</summary>
public sealed record OvertimePaySegmentDto(
    decimal Multiplier,
    decimal Hours,
    decimal Amount);

/// <summary>
/// 加班費試算結果（表單即時試算與核准寫快照共用）。
/// Segments 是必要的：使用者看到總額不會相信，看到「2h ×1.34 + 6h ×1.67」才會。
/// </summary>
public sealed record OvertimePayEstimateDto(
    DateTime OvertimeDate,
    bool     IsHoliday,        // 日別（排班制員工恆為 false，見 OvertimePayCalculator 註解）
    decimal  HourlyRate,       // ROUND(BaseSalary / 240, 2)
    decimal  RequestedHours,   // = OvertimeRequest.EstimatedHours
    decimal  PayableHours,     // = min(RequestedHours, CapHours)
    decimal  ExcessHours,      // 超出上限、不計酬的時數
    decimal  CapHours,
    decimal  Amount,           // 總額（AwayFromZero 捨入至元）
    OvertimePaySegmentDto[] Segments,
    bool     HasBaseSalary = true,          // false → 未設定底薪，Amount 必為 0，前端顯示提示而非 NT$0
    bool     HasHolidayTravelConflict = false);  // 同日已有已核准的假日執行活動 → 可能與假日津貼雙重給付

public sealed record OvertimeRequestDto(
    int       Id,
    string    EmployeeName,
    DateTime  OvertimeDate,
    OvertimeProjectDto[] Projects,
    decimal   EstimatedHours,          // 合計 = Projects 各列加總
    string    Reason,
    string    ApprovalStatus,
    DateTime  CreatedAt,
    DateTime? ReviewedAt,
    string?   ReviewNote,
    int?      ApprovalItemId       = null,
    int?      CurrentStepOrder     = null,
    Guid?     ReviewedById         = null,
    DesignatedReviewerDto[]? DesignatedReviewers = null,
    // 補償方式與加班費快照（compensationType = compensatory | pay）
    string    CompensationType     = "compensatory",
    decimal?  OvertimePayAmount    = null,
    decimal?  HourlyRateSnapshot   = null,
    decimal?  PayableHours         = null,
    bool?     IsHolidayOvertime    = null);

public sealed record CreateOvertimeRequestRequest(
    Guid?    EmployeeId,
    int?     ApprovalItemId       = null,
    DateTime OvertimeDate         = default,
    OvertimeProjectRequest[]? Projects = null,   // 必填，至少 1 列（Handler 驗證）
    string   Reason               = "",
    DesignatedReviewerRequest[]? DesignatedReviewers = null,
    string   CompensationType     = "compensatory");   // compensatory | pay，未知值一律正規化為 compensatory

public sealed record UpdateOvertimeRequestRequest(
    DateTime? OvertimeDate,
    OvertimeProjectRequest[]? Projects,          // 必填，至少 1 列（整批替換，不支援省略）
    string?   Reason,
    DesignatedReviewerRequest[]? DesignatedReviewers = null,
    string?   CompensationType   = null);        // null＝不變更
