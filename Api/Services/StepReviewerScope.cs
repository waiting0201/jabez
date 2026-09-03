namespace Jabez.Api.Services;

/// <summary>
/// 簽核步驟的「固定審核者範圍」（部門 + 職稱），供
/// <see cref="IEscalationService.FindSuperiorInAncestorDepartmentsAsync"/> 判斷
/// 「這個候選人是不是流程後面本來就會找上的人」。
///
/// 兩個欄位皆為 null 表示不限部門也不限職稱（＝全公司），這種範圍無從用來判定，呼叫端不應建出。
/// </summary>
/// <param name="DepartmentId">部門（null＝不限部門）；UseApplicantDepartment 步驟由呼叫端先解析成申請人部門</param>
/// <param name="JobTitleId">職稱（null＝不限職稱）</param>
public readonly record struct StepReviewerScope(
    int? DepartmentId,
    int? JobTitleId)
{
    /// <summary>此範圍是否涵蓋指定的部門 / 職稱組合（＝該員會落在這一關的審核者池裡）。</summary>
    public bool Covers(int? departmentId, int? jobTitleId)
        => (DepartmentId is null || DepartmentId == departmentId)
        && (JobTitleId   is null || JobTitleId   == jobTitleId);
}
