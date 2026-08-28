namespace Jabez.Api.Models.Dtos;

public sealed record OvertimeReportDto(
    int       Id,
    string    EmployeeName,
    DateTime  OvertimeDate,
    OvertimeProjectDto[] Projects,   // 關聯專案明細（含各案預估時數）
    decimal   EstimatedHours,        // 預估總時數（= Projects 合計）
    decimal?  ActualHours,           // 打卡推算的實際加班時數（整日，無法分攤到各專案）
    string    Reason,
    // 補償方式與加班費快照 —— HR 需從報表直接分辨哪些加班已轉現金
    string    CompensationType  = "compensatory",
    decimal?  OvertimePayAmount = null);
