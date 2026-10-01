using System.Security.Claims;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/compensatory?dateFrom=&amp;dateTo= → 加班補休時數總表（一位員工一列，不分頁）
///
/// 權限沿用加班紀錄報表：頁面 reports-overtime:read（路由層），金額 reports-overtime:amount（本 Handler 抹除）。
/// 部門 / 員工篩選在前端以回傳資料過濾（人數約百人、且 /departments 需 departments:read），
/// 部門可見性由 IProjectAccessResolver 在這裡收斂。
///
/// 日別判定：補休單核准時**不寫**日別 / 級距快照（OvertimeCompensationService 只對加班費單寫），
/// 故於報表當下解析，走與加班費同一支 <see cref="OvertimePayCalculator.ResolveDayContextAsync"/>
/// （個人排班 → 國定假日 → 舊制行事曆，切換日前國定假日收斂回休假日）。
/// 國定假日未排活動日者前 8 小時屬「國定假日出勤加倍工資」不列入級距，故該類加班的級距加總會小於期間合計。
///
/// 待補休：四週彈性工時切換後以逐筆 lot 為準（<see cref="CompensatoryLotService.GetBalanceAsync"/>，同請假表單），
/// 切換日為 null 時走聚合版 <see cref="CompensatoryBalance.Compute"/>。
/// </summary>
public sealed class CompensatoryReportHandler(
    ICompensatoryReportReadService reader,
    IShiftScheduleReadService shiftSchedule,
    IWorkdayScheduleProvider scheduleProvider,
    AppDbContext db,
    IProjectAccessResolver access)
{
    /// <summary>查詢區間跨度上限（天），比照出缺勤報表 AttendanceLeaveMerger.MaxRangeDays。</summary>
    public const int MaxRangeDays = 400;

    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var today = Clock.Now.Date;
        var dateFrom = DateTime.TryParse(req.Query["dateFrom"], out var df) ? df.Date : new DateTime(today.Year, today.Month, 1);
        var dateTo   = DateTime.TryParse(req.Query["dateTo"],   out var dt) ? dt.Date : dateFrom.AddMonths(1).AddDays(-1);

        if (dateTo < dateFrom)
            throw AppException.BadRequest("迄日不得早於起日。");
        if ((dateTo - dateFrom).TotalDays > MaxRangeDays)
            throw AppException.BadRequest($"查詢區間不得超過 {MaxRangeDays} 天。");

        var scope     = await access.ResolveAsync(req.HttpContext.User);
        var employees = await reader.GetEmployeesAsync(scope, dateFrom, dateTo);
        var overtimes = await reader.GetPeriodOvertimesAsync(scope, dateFrom, dateTo);

        bool lotMode = await scheduleProvider.GetSwitchDateAsync() is not null;
        var byEmployee = overtimes.GroupBy(o => o.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
        bool canSeeAmount = CanSeeAmount(req.HttpContext.User);
        var now = Clock.Now;

        var rows = new List<CompensatoryReportRowDto>();
        foreach (var e in employees)
        {
            decimal t134 = 0m, t167 = 0m, t267 = 0m, weighted = 0m, earned = 0m;
            foreach (var o in byEmployee.GetValueOrDefault(e.EmployeeId, []))
            {
                earned += o.EstimatedHours;
                var (dayType, isAssignee) = await OvertimePayCalculator.ResolveDayContextAsync(
                    shiftSchedule, scheduleProvider, e.EmployeeId, o.OvertimeDate);
                foreach (var tier in OvertimePayCalculator.SplitCompensatoryTiers(o.EstimatedHours, dayType, isAssignee))
                {
                    weighted += tier.Hours * tier.Multiplier;
                    switch (tier.Multiplier)
                    {
                        case 1.34m: t134 += tier.Hours; break;
                        case 1.67m: t167 += tier.Hours; break;
                        default:    t267 += tier.Hours; break;
                    }
                }
            }

            var available = lotMode
                ? (await CompensatoryLotService.GetBalanceAsync(db, e.EmployeeId)).AvailableHours
                : CompensatoryBalance.Compute(e.OpeningHours, e.EarnedHours, e.UsedHours, now).AvailableHours;
            if (earned == 0m && e.PeriodUsedHours == 0m && available == 0m) continue;

            // 金額只在總額捨入一次（比照 OvertimePayCalculator.Calculate）；底薪未設定者無從換算
            decimal? amount = canSeeAmount && e.BaseSalary is > 0m
                ? Math.Round(weighted * OvertimePayCalculator.HourlyRate(e.BaseSalary.Value), 0, MidpointRounding.AwayFromZero)
                : null;

            rows.Add(new CompensatoryReportRowDto(
                e.EmployeeId, e.EmployeeName, e.DepartmentId, e.DepartmentName,
                t134, t167, t267, earned, e.PeriodUsedHours, available, amount));
        }

        return new OkObjectResult(ApiResponse.Ok(rows));
    }

    /// <summary>是否可看金額：Superadmin 全通過，否則需持有 reports-overtime:amount（同加班紀錄報表）。</summary>
    private static bool CanSeeAmount(ClaimsPrincipal user)
    {
        if (string.Equals(user.FindFirst("is_superadmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        return user.FindAll("permissions").Any(c => c.Value == PermissionCodes.ReportsOvertimeAmount);
    }
}
