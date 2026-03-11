namespace Jabez.Api.Models.Dtos;

public sealed record OvertimeReportDto(
    int       Id,
    string    EmployeeName,
    DateTime  OvertimeDate,
    string[]? ProjectCodes,
    decimal   EstimatedHours,
    decimal?  ActualHours,
    string    Reason);
