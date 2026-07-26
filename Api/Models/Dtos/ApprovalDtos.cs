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
    bool    DesignatedRequiresDepartment = false,
    int?    MinDays                      = null,
    // 例外指定審核名單：名單內的申請人送單時，此步驟改為「由申請人自行指定審核者」。
    // 非空即代表啟用例外（不另設 bool 旗標，避免兩者 desync）；與 UseApplicantDesignated 互斥。
    // 僅 GetByIdAsync（管理頁編輯）會帶出，清單頁一律為 null。
    Guid[]? ExceptionUserIds             = null);

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
    // 「對呼叫者而言」的有效值：步驟原生設定 OR 例外指定審核名單命中呼叫者
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
    bool    DesignatedRequiresDepartment = false,
    int?    MinDays                      = null,
    // 例外指定審核名單（整批替換語意）：null＝不設定、[]＝清空
    Guid[]? ExceptionUserIds             = null);

public sealed record UpdateApprovalStepRequest(
    int?     StepOrder,
    int?     DepartmentId,
    int?     JobTitleId,
    bool?    UseApplicantDepartment,
    bool?    UseDirectSupervisor,
    bool?    UseApplicantDesignated,
    string?  Note,
    bool?    DesignatedRequiresDepartment = null,
    int?     MinDays                      = null,
    // 例外指定審核名單（整批替換語意）：null＝不動、[]＝清空
    Guid[]?  ExceptionUserIds             = null);
