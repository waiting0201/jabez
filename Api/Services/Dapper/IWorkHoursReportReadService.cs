using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IWorkHoursReportReadService
{
    /// <summary>
    /// 區間內「已打下班卡」的打卡紀錄 ⨝ 專案分配（無分配者 Percentage 為 null）。
    /// 已排除 Superadmin 並套用部門可見性 scope；帶 projectId 時只回該專案的分配列（不含未分配）。
    /// </summary>
    Task<IReadOnlyList<WorkHoursRaw>> GetRowsAsync(
        ProjectAccessScope scope, DateTime dateFrom, DateTime dateTo, int? departmentId, int? projectId);
}
