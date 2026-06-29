namespace Jabez.Api.Models.Dtos;

public sealed record ApprovalStepDto(
    int     Id,
    int     StepOrder,
    int?    DepartmentId,
    string? DepartmentName,
    int?    JobTitleId,
    string? JobTitleName,
    bool    UseApplicantDepartment,
    bool    UseDirectSupervisor,
    bool    UseApplicantDesignated,
    string? Note,
    bool    DesignatedRequiresDepartment = false);

public sealed record ApprovalItemDto(
    int               Id,
    string            Name,
    string            Code,
    string?           Description,
    bool              IsActive,
    string?           ApplicationType,
    int?              DepartmentId,
    string?           DepartmentName,
    ApprovalStepDto[] Steps,
    DateTime          CreatedAt);

// 輕量級流程摘要（供申請表單判斷是否有指定審核步驟，不含部門 / 職稱等敏感設定）
public sealed record ApprovalFlowSummaryDto(
    int                          Id,
    string?                      ApplicationType,
    ApprovalFlowStepSummaryDto[] Steps);

public sealed record ApprovalFlowStepSummaryDto(
    int  StepOrder,
    bool UseApplicantDesignated,
    bool DesignatedRequiresDepartment = false);

public sealed record CreateApprovalItemRequest(
    string  Name,
    string  Code,
    string? Description     = null,
    bool    IsActive        = true,
    string? ApplicationType = null,
    int?    DepartmentId    = null);

public sealed record UpdateApprovalItemRequest(
    string?  Name,
    string?  Code,
    string?  Description,
    bool?    IsActive,
    string?  ApplicationType,
    int?     DepartmentId);

public sealed record CreateApprovalStepRequest(
    int     StepOrder,
    int?    DepartmentId                 = null,
    int?    JobTitleId                   = null,
    bool    UseApplicantDepartment       = false,
    bool    UseDirectSupervisor          = false,
    bool    UseApplicantDesignated       = false,
    string? Note                         = null,
    bool    DesignatedRequiresDepartment = false);

public sealed record UpdateApprovalStepRequest(
    int?     StepOrder,
    int?     DepartmentId,
    int?     JobTitleId,
    bool?    UseApplicantDepartment,
    bool?    UseDirectSupervisor,
    bool?    UseApplicantDesignated,
    string?  Note,
    bool?    DesignatedRequiresDepartment = null);
