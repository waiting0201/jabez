namespace Jabez.Api.Models.Dtos;

/// <summary>颱風假清單項目（GET /disaster-days）</summary>
public sealed record DisasterDayOffDto(
    int       Id,
    /// <summary>停班日 yyyy-MM-dd</summary>
    string    Date,
    string    County,
    string    CountyName,
    string    Segment,
    bool      IsPaid,
    string    Source,
    string?   SourceText,
    string    Status,
    DateTime  CreatedAt,
    string?   CreatedByName,
    DateTime? CancelledAt,
    string?   CancelledByName,
    /// <summary>目前該縣市工作地的在職員工數</summary>
    int       AffectedCount);

/// <summary>手動建立（可多縣市）。IsPaid 未帶則用系統預設。</summary>
public sealed record CreateDisasterDayOffRequest(
    DateTime Date, string[] Counties, string Segment, bool? IsPaid = null);

/// <summary>修改時段 / 給薪（null ＝ 不變更）</summary>
public sealed record UpdateDisasterDayOffRequest(string? Segment = null, bool? IsPaid = null);

public sealed record DisasterAffectedEmployeeDto(
    Guid UserId, string Name, string? DepartmentName, bool Worked, decimal? DeductionDays);

public sealed record DisasterAdjustedLeaveDto(
    int LeaveRequestId, string? RequestNo, string EmployeeName, string LeaveType,
    decimal OriginalHours, decimal Hours);

/// <summary>影響名單：受影響員工 + 被退回時數的假單 + 該縣市未設工作地的人數提示</summary>
public sealed record DisasterAffectedDto(
    IReadOnlyList<DisasterAffectedEmployeeDto> Employees,
    IReadOnlyList<DisasterAdjustedLeaveDto> AdjustedLeaves,
    int MissingCountyCount);

public sealed record DisasterManualItemDto(string County, string CountyName, string Text);

public sealed record DisasterFetchLogDto(
    int Id, DateTime FetchedAt, DateTime? SourceUpdatedAt, bool Success, string Summary,
    IReadOnlyList<DisasterManualItemDto> ManualItems);

/// <summary>GET /me/disaster-day-off：今天本人適用的颱風假（打卡頁提示用）</summary>
public sealed record MyDisasterDayOffDto(
    string Date, string County, string CountyName, string Segment, bool IsPaid);
