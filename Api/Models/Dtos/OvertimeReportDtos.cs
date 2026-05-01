namespace Jabez.Api.Models.Dtos;

public sealed record OvertimeReportDto(
    int       Id,
    string    EmployeeName,
    DateTime  OvertimeDate,
    string[]? ProjectCodes,
    string[]? ProjectNames,
    decimal   EstimatedHours,
    decimal?  ActualHours,
    string    Reason);
