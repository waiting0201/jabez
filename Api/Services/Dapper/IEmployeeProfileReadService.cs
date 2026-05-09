using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IEmployeeProfileReadService
{
    /// <summary>
    /// 一次讀回員工人事資料卡 + 9 個子表（QueryMultiple，單次 round-trip）。
    /// 若 EmployeeProfile 不存在，回傳預設空 DTO（前端不會 404 失敗）。
    /// </summary>
    Task<EmployeeProfileDetailDto> GetByUserIdAsync(Guid userId);
}
