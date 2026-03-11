namespace Jabez.Api.Models.Dtos;

public sealed record InsuranceBracketDto(
    int      Id,
    decimal  SalaryBracket,
    decimal  LaborInsuranceEmployee,
    decimal  HealthInsuranceEmployee,
    DateTime CreatedAt);

public sealed record CreateInsuranceBracketRequest(
    decimal  SalaryBracket,
    decimal  LaborInsuranceEmployee,
    decimal  HealthInsuranceEmployee);

public sealed record UpdateInsuranceBracketRequest(
    decimal? SalaryBracket,
    decimal? LaborInsuranceEmployee,
    decimal? HealthInsuranceEmployee);
