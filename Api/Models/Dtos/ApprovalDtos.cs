namespace Jabez.Api.Models.Dtos;

public sealed record ApprovalStepDto(
    int     Id,
    int     StepOrder,
    int?    DepartmentId,
    string? DepartmentName,
    int?    JobTitleId,
    string? JobTitleName,
    bool    UseApplicantDepartment,
    string? Note);

public sealed record ApprovalItemDto(
    int               Id,
    string            Name,
    string            Code,
    string?           Description,
    bool              IsActive,
    string?           ApplicationType,
    ApprovalStepDto[] Steps,
    DateTime          CreatedAt);

public sealed record CreateApprovalItemRequest(
    string  Name,
    string  Code,
    string? Description     = null,
    bool    IsActive        = true,
    string? ApplicationType = null);

public sealed record UpdateApprovalItemRequest(
    string?  Name,
    string?  Code,
    string?  Description,
    bool?    IsActive,
    string?  ApplicationType);

public sealed record CreateApprovalStepRequest(
    int     StepOrder,
    int?    DepartmentId            = null,
    int?    JobTitleId              = null,
    bool    UseApplicantDepartment  = false,
    string? Note                    = null);

public sealed record UpdateApprovalStepRequest(
    int?     StepOrder,
    int?     DepartmentId,
    int?     JobTitleId,
    bool?    UseApplicantDepartment,
    string?  Note);
