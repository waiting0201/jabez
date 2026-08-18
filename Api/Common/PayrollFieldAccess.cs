using System.Security.Claims;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Common;

/// <summary>
/// 員工管理的「薪資欄位級權限」單一真相。
///
/// 進得了員工管理（users:read）不等於看得到薪資：底薪 / 2 種加給 / 勞健保覆寫 / 勞退自提率，
/// 以及人事資料卡的薪資調整歷史，另需 payroll:read（＝「人事薪資」模組同一把鑰匙）。
/// 規範見 docs/backend-design.md「欄位級權限（Handler 內判定的例外）」。
///
/// ⚠ 日後在 User 新增任何薪資欄位，必須同時加進 <see cref="Mask"/>，
/// 並同步前端 user-form.ts 的 SALARY_CONTROLS（漏改＝外洩）。
/// </summary>
public static class PayrollFieldAccess
{
    /// <summary>是否可看薪資欄位：Superadmin 全通過，否則需持有 payroll:read。</summary>
    public static bool CanSeeSalary(ClaimsPrincipal user)
    {
        if (string.Equals(user.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        return user.FindAll("permissions").Any(c => c.Value == PermissionCodes.PayrollRead);
    }

    /// <summary>
    /// 抹除 UserDto 的 8 個薪資欄位（回 null 而非 403，前端據此隱藏即可）。
    /// SendPaySlip（布林旗標）與 CompensatoryOpeningHours（時數）不含金額，刻意保留。
    /// </summary>
    public static UserDto Mask(UserDto u) => u with
    {
        BaseSalary                      = null,
        MealAllowance                   = null,
        OvertimePay                     = null,
        OtherAllowance                  = null,
        AdjustmentDifference            = null,
        // 勞健保覆寫金額與勞退自提率能反推投保級距 / 底薪區間，一併抹除
        HealthInsuranceOverride         = null,
        LaborInsuranceOverride          = null,
        LaborPensionSelfContributionRate = null,
    };

    /// <summary>無薪資權限時清除人事資料卡的薪資調整歷史（含 TotalAmount，整張表為薪資原料）。</summary>
    public static EmployeeProfileDetailDto Mask(EmployeeProfileDetailDto p) => p with
    {
        SalaryAdjustmentRecords = [],
    };
}
