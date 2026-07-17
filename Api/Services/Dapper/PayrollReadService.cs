using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class PayrollReadService(IDbConnection db) : IPayrollReadService
{
    public async Task<MonthlyPayrollDto> CalculateMonthlyPayrollAsync(int year, int month)
    {
        var firstDay          = new DateTime(year, month, 1);
        var lastDay           = firstDay.AddMonths(1).AddDays(-1);
        var prevMonthFirstDay = firstDay.AddMonths(-1);   // 假日活動獎金：歸屬於上個月 EndDate 的申請

        // 1. 查詢所有在職員工（有底薪、未離職或離職日在該月之後）
        //    同時取出健保眷屬人數（HealthInsuranceDependents）與保費覆蓋欄位
        const string employeeSql = """
            SELECT u.Id AS EmployeeId, u.Name AS EmployeeName,
                   u.Email, u.SendPaySlip,
                   d.Name AS DepartmentName, jt.Name AS JobTitleName,
                   u.HireDate, u.BaseSalary, u.MealAllowance, u.OvertimePay,
                   u.PositionAllowance, u.DutyAllowance, u.OtherAllowance,
                   u.AdjustmentDifference, u.OverseasAllowance,
                   u.HealthInsuranceOverride, u.LaborInsuranceOverride,
                   u.LaborPensionSelfContributionRate,
                   (SELECT COUNT(*) FROM HealthInsuranceDependents WHERE UserId = u.Id) AS DependentCount
            FROM Users u
            LEFT JOIN Departments d  ON u.DepartmentId = d.Id
            LEFT JOIN JobTitles jt   ON u.JobTitleId = jt.Id
            WHERE u.IsSuperAdmin = 0
              AND u.Status = 'active'
              AND u.BaseSalary IS NOT NULL
              AND u.BaseSalary > 0
              AND (u.ResignDate IS NULL OR u.ResignDate >= @FirstDay)
            ORDER BY u.Name
            """;

        // 2. 查詢「上一個月」已核准的假日執行活動天數（以 EndDate 歸月，獎金計入次月薪資）
        //    例：4 月薪資只計入 EndDate 落在 3/1~3/31 的活動；跨月活動以 EndDate 所屬月份歸屬
        const string travelSql = """
            ;WITH HolidayTravelDays AS (
                -- 申請人的假日天數
                SELECT tr.EmployeeId, tr.HolidayDays
                FROM TravelRequests tr
                WHERE tr.IsHolidayTravel = 1
                  AND tr.ApprovalStatus = 'approved'
                  AND tr.EndDate >= @PrevMonthFirstDay
                  AND tr.EndDate <  @CurrMonthFirstDay
                UNION ALL
                -- 參與執行人員的假日天數（有勾選參與日期者取個人假日天數；NULL=全程參與，沿用整單）
                SELECT p.UserId AS EmployeeId, COALESCE(p.HolidayDays, tr.HolidayDays) AS HolidayDays
                FROM TravelRequestParticipants p
                JOIN TravelRequests tr ON p.TravelRequestId = tr.Id
                WHERE tr.IsHolidayTravel = 1
                  AND tr.ApprovalStatus = 'approved'
                  AND tr.EndDate >= @PrevMonthFirstDay
                  AND tr.EndDate <  @CurrMonthFirstDay
            )
            SELECT EmployeeId, SUM(HolidayDays) AS TotalDays
            FROM HolidayTravelDays
            GROUP BY EmployeeId
            """;

        // 3. 查詢所有勞健保級距
        const string bracketSql = """
            SELECT SalaryBracket, LaborInsuranceEmployee, HealthInsuranceEmployee
            FROM InsuranceBrackets
            ORDER BY SalaryBracket ASC
            """;

        // 4. 查詢該月所有薪資調整（其他加項 + 其他扣項 + 備注）
        const string adjustmentSql = """
            SELECT EmployeeId, OtherAddition, OtherAdditionNote,
                   OtherDeduction, OtherDeductionNote, Note
            FROM PayrollAdjustments
            WHERE Year = @Year AND Month = @Month
            """;

        // 5. 查詢該月已核准的事假/病假時數（按員工 + 假別分組）
        //    事假/病假為「小時」單位，以 SUM(Hours) 累計，C# 端再 ÷ 8 換算天數
        const string leaveSql = """
            SELECT lr.EmployeeId, lr.LeaveType,
                   SUM(lr.Hours) AS TotalHours
            FROM LeaveRequests lr
            WHERE lr.ApprovalStatus = 'approved'
              AND lr.LeaveType IN ('personal', 'sick', 'menstrual')
              AND lr.StartDate <= @LastDay
              AND lr.EndDate   >= @FirstDay
            GROUP BY lr.EmployeeId, lr.LeaveType
            """;

        // 5b. 查詢「本年度、本月之前」已核准生理假時數（依 StartDate 歸年月）
        //     用於判斷年度前 3 天（24h）純生理假額度是否已用罄；超過部分併入病假計算
        const string priorMenstrualSql = """
            SELECT lr.EmployeeId, SUM(lr.Hours) AS TotalHours
            FROM LeaveRequests lr
            WHERE lr.ApprovalStatus = 'approved'
              AND lr.LeaveType = 'menstrual'
              AND lr.StartDate >= @YearFirstDay
              AND lr.StartDate <  @FirstDay
            GROUP BY lr.EmployeeId
            """;

        // 6. 查詢該月所有已核准的請假明細（全假別）
        const string leaveDetailSql = """
            SELECT lr.EmployeeId, lr.LeaveType, lr.StartDate, lr.EndDate, lr.Hours
            FROM LeaveRequests lr
            WHERE lr.ApprovalStatus = 'approved'
              AND lr.StartDate <= @LastDay
              AND lr.EndDate   >= @FirstDay
            ORDER BY lr.StartDate
            """;

        var employees = (await db.QueryAsync<dynamic>(employeeSql, new { FirstDay = firstDay })).ToList();
        var travelDays = (await db.QueryAsync<dynamic>(travelSql, new {
                PrevMonthFirstDay = prevMonthFirstDay,
                CurrMonthFirstDay = firstDay,
            }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => (int)r.TotalDays);
        var brackets = (await db.QueryAsync<dynamic>(bracketSql)).ToList();
        var adjustments = (await db.QueryAsync<dynamic>(adjustmentSql, new { Year = year, Month = month }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => r);

        // 事假/病假天數：Dictionary<(EmployeeId, LeaveType), TotalDays (decimal)>
        // TotalHours ÷ 8 = 實際天數（保留小數，例如 2 小時 = 0.25 天）
        var leaveRecords = (await db.QueryAsync<dynamic>(leaveSql, new { FirstDay = firstDay, LastDay = lastDay }))
            .ToList();
        var leaveDaysMap = new Dictionary<(Guid, string), decimal>();
        foreach (var lr in leaveRecords)
            leaveDaysMap[((Guid)lr.EmployeeId, (string)lr.LeaveType)] = (decimal)lr.TotalHours / 8m;

        // 本年度本月之前已用生理假時數：Dictionary<EmployeeId, PriorHours>
        var yearFirstDay = new DateTime(year, 1, 1);
        var priorMenstrualMap = (await db.QueryAsync<dynamic>(priorMenstrualSql, new { YearFirstDay = yearFirstDay, FirstDay = firstDay }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => (decimal)r.TotalHours);

        // 請假明細：Dictionary<EmployeeId, LeaveDetailDto[]>
        var leaveDetails = (await db.QueryAsync<dynamic>(leaveDetailSql, new { FirstDay = firstDay, LastDay = lastDay }))
            .GroupBy(r => (Guid)r.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new LeaveDetailDto(
                    (string)r.LeaveType,
                    (DateTime)r.StartDate,
                    (DateTime)r.EndDate,
                    (decimal)r.Hours)).ToArray());

        var results = new List<EmployeePayrollDto>();
        foreach (var emp in employees)
        {
            decimal baseSalary     = (decimal)emp.BaseSalary;
            decimal mealAllowance  = (decimal?)emp.MealAllowance ?? 0m;
            decimal overtimePay    = (decimal?)emp.OvertimePay ?? 0m;
            decimal positionAllow  = (decimal?)emp.PositionAllowance    ?? 0m;
            decimal dutyAllow      = (decimal?)emp.DutyAllowance        ?? 0m;
            decimal otherAllow     = (decimal?)emp.OtherAllowance       ?? 0m;
            decimal adjDiff        = (decimal?)emp.AdjustmentDifference ?? 0m;
            decimal overseasAllow  = (decimal?)emp.OverseasAllowance    ?? 0m;
            decimal dailySalary    = Math.Round(baseSalary / 30m, 0);

            int holidayDays = travelDays.TryGetValue((Guid)emp.EmployeeId, out var days) ? days : 0;
            // dailySalary 已四捨五入為整數、holidayDays 為 int，目前乘積必為整數；
            // 仍包一層 Math.Round 作防禦，未來若改為比例天數計算不會跑出小數金額。
            decimal holidayAllowance = Math.Round(dailySalary * holidayDays, 0);

            // 查找級距：第一個 SalaryBracket >= BaseSalary 的級距，若無則取最高級距
            var bracket = brackets.FirstOrDefault(b => (decimal)b.SalaryBracket >= baseSalary)
                       ?? brackets.LastOrDefault();

            decimal baseHealthIns = bracket is not null ? (decimal)bracket.HealthInsuranceEmployee : 0m;
            decimal fullLaborIns  = bracket is not null ? (decimal)bracket.LaborInsuranceEmployee  : 0m;

            // 若員工有個別覆蓋值，以覆蓋值取代級距值（低收入戶 / 身心障礙補貼場景）
            decimal overrideHealth = (decimal?)emp.HealthInsuranceOverride ?? baseHealthIns;
            decimal overrideLabor  = (decimal?)emp.LaborInsuranceOverride  ?? fullLaborIns;

            // 勞退自提率（%，非覆寫、無 lookup，直接乘底薪算扣款）
            decimal? laborPensionRate = (decimal?)emp.LaborPensionSelfContributionRate;

            // 健保眷屬加成：健保費 × (1 + min(眷屬人數, 3))
            // 最多計至 3 口眷屬，超過 3 口仍以 3 計
            int  dependentCount  = (int)emp.DependentCount;
            int  cappedN         = Math.Min(dependentCount, 3);
            decimal healthIns    = Math.Round(overrideHealth * (1 + cappedN), 0);

            // 入職首月：勞保費按加保天數比例計算（月勞保費 ÷ 30 × 當月加保天數）
            // 健保費：入職當月收全月，不按比例
            decimal laborIns = overrideLabor;
            DateTime? hireDate = (DateTime?)emp.HireDate;
            if (hireDate.HasValue
                && hireDate.Value.Year == year
                && hireDate.Value.Month == month)
            {
                int insuredDays = (lastDay - hireDate.Value).Days + 1;
                laborIns = Math.Round(overrideLabor / 30m * insuredDays, 0);
            }

            // 事假扣薪：日薪 × 事假天數（不給薪）；天數 = SUM(Hours) / 8，保留小數
            var empId = (Guid)emp.EmployeeId;
            decimal personalDays = leaveDaysMap.TryGetValue((empId, "personal"), out var pd) ? pd : 0m;
            decimal personalDeduction = Math.Round(dailySalary * personalDays, 0);

            // 生理假：本月時數中，本年度前 3 天（24h）為純生理假，超過部分併入病假計算（兩者皆半薪）
            //   pureThisMonth = min(本月生理假時數, max(0, 24 - 本年度本月前已用生理假時數))
            decimal menstrualHoursThisMonth = (leaveDaysMap.TryGetValue((empId, "menstrual"), out var md) ? md : 0m) * 8m;
            decimal priorMenstrualHours     = priorMenstrualMap.TryGetValue(empId, out var pm) ? pm : 0m;
            const decimal annualPureCapHours = 24m;   // 3 天
            decimal pureMenstrualHours = Math.Min(menstrualHoursThisMonth, Math.Max(0m, annualPureCapHours - priorMenstrualHours));
            decimal mergedToSickHours  = menstrualHoursThisMonth - pureMenstrualHours;

            // 生理假扣薪（純生理假部分）：日薪 × 0.5 × 天數（半薪）
            decimal menstrualDays = pureMenstrualHours / 8m;
            decimal menstrualDeduction = Math.Round(dailySalary * 0.5m * menstrualDays, 0);

            // 病假扣薪：日薪 × 0.5 × 病假天數（半薪）；天數 = SUM(Hours) / 8
            //   超過 3 天的生理假時數併入病假天數一同計算
            decimal sickDays = (leaveDaysMap.TryGetValue((empId, "sick"), out var sd) ? sd : 0m) + mergedToSickHours / 8m;
            decimal sickDeduction = Math.Round(dailySalary * 0.5m * sickDays, 0);

            // 其他加項 / 其他扣項
            decimal otherAddition = 0m;
            string? otherAdditionNote = null;
            decimal otherDeduction = 0m;
            string? otherDeductionNote = null;
            string? note = null;
            if (adjustments.TryGetValue(empId, out var adj))
            {
                otherAddition      = (decimal)adj.OtherAddition;
                otherAdditionNote  = (string?)adj.OtherAdditionNote;
                otherDeduction     = (decimal)adj.OtherDeduction;
                otherDeductionNote = (string?)adj.OtherDeductionNote;
                note               = (string?)adj.Note;
            }

            // 勞退自提扣款 = 底薪 × 自提率%，四捨五入至整數（比照勞健保費公式）
            decimal laborPensionSelfDeduction = Math.Round(baseSalary * (laborPensionRate ?? 0m) / 100m, 0);

            decimal netSalary = baseSalary + mealAllowance + overtimePay
                              + positionAllow + dutyAllow + otherAllow + adjDiff + overseasAllow
                              + holidayAllowance + otherAddition
                              - laborIns - healthIns
                              - personalDeduction - sickDeduction - menstrualDeduction
                              - otherDeduction
                              - laborPensionSelfDeduction;

            results.Add(new EmployeePayrollDto(
                empId,
                (string)emp.EmployeeName,
                (string?)emp.Email,
                (bool)emp.SendPaySlip,
                (string?)emp.DepartmentName,
                (string?)emp.JobTitleName,
                (DateTime?)emp.HireDate,
                baseSalary,
                mealAllowance,
                overtimePay,
                dailySalary,
                holidayDays,
                holidayAllowance,
                otherAddition,
                otherAdditionNote,
                laborIns,
                healthIns,
                personalDays,
                personalDeduction,
                sickDays,
                sickDeduction,
                menstrualDays,
                menstrualDeduction,
                otherDeduction,
                otherDeductionNote,
                note,
                netSalary,
                leaveDetails.TryGetValue(empId, out var ld) ? ld : null,
                dependentCount,
                cappedN,
                positionAllow,
                dutyAllow,
                otherAllow,
                adjDiff,
                overseasAllow,
                laborPensionRate,
                laborPensionSelfDeduction));
        }

        return new MonthlyPayrollDto(
            year, month, results,
            results.Sum(r => r.BaseSalary),
            results.Sum(r => r.MealAllowance),
            results.Sum(r => r.OvertimePay),
            results.Sum(r => r.HolidayAllowance),
            results.Sum(r => r.OtherAddition),
            results.Sum(r => r.LaborInsurance),
            results.Sum(r => r.HealthInsurance),
            results.Sum(r => r.PersonalLeaveDeduction),
            results.Sum(r => r.SickLeaveDeduction),
            results.Sum(r => r.MenstrualLeaveDeduction),
            results.Sum(r => r.OtherDeduction),
            results.Sum(r => r.NetSalary),
            results.Sum(r => r.PositionAllowance),
            results.Sum(r => r.DutyAllowance),
            results.Sum(r => r.OtherAllowanceAmount),
            results.Sum(r => r.AdjustmentDifference),
            results.Sum(r => r.OverseasAllowance),
            results.Sum(r => r.LaborPensionSelfDeduction));
    }
}
