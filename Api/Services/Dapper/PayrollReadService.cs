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
        const string employeeSql = """
            SELECT u.Id AS EmployeeId, u.Name AS EmployeeName,
                   u.Email, u.SendPaySlip,
                   d.Name AS DepartmentName, jt.Name AS JobTitleName,
                   u.HireDate, u.BaseSalary, u.MealAllowance, u.OvertimePay
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
                -- 參與執行人員的假日天數
                SELECT p.UserId AS EmployeeId, tr.HolidayDays
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
              AND lr.LeaveType IN ('personal', 'sick')
              AND lr.StartDate <= @LastDay
              AND lr.EndDate   >= @FirstDay
            GROUP BY lr.EmployeeId, lr.LeaveType
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
            decimal dailySalary    = Math.Round(baseSalary / 30m, 0);

            int holidayDays = travelDays.TryGetValue((Guid)emp.EmployeeId, out var days) ? days : 0;
            // dailySalary 已四捨五入為整數、holidayDays 為 int，目前乘積必為整數；
            // 仍包一層 Math.Round 作防禦，未來若改為比例天數計算不會跑出小數金額。
            decimal holidayAllowance = Math.Round(dailySalary * holidayDays, 0);

            // 查找級距：第一個 SalaryBracket >= BaseSalary 的級距，若無則取最高級距
            var bracket = brackets.FirstOrDefault(b => (decimal)b.SalaryBracket >= baseSalary)
                       ?? brackets.LastOrDefault();

            decimal fullLaborIns  = bracket is not null ? (decimal)bracket.LaborInsuranceEmployee  : 0m;
            decimal healthIns     = bracket is not null ? (decimal)bracket.HealthInsuranceEmployee : 0m;

            // 入職首月：勞保費按加保天數比例計算（月勞保費 ÷ 30 × 當月加保天數）
            // 健保費：入職當月收全月，不按比例
            decimal laborIns = fullLaborIns;
            DateTime? hireDate = (DateTime?)emp.HireDate;
            if (hireDate.HasValue
                && hireDate.Value.Year == year
                && hireDate.Value.Month == month)
            {
                int insuredDays = (lastDay - hireDate.Value).Days + 1;
                laborIns = Math.Round(fullLaborIns / 30m * insuredDays, 0);
            }

            // 事假扣薪：日薪 × 事假天數（不給薪）；天數 = SUM(Hours) / 8，保留小數
            var empId = (Guid)emp.EmployeeId;
            decimal personalDays = leaveDaysMap.TryGetValue((empId, "personal"), out var pd) ? pd : 0m;
            decimal personalDeduction = Math.Round(dailySalary * personalDays, 0);

            // 病假扣薪：日薪 × 0.5 × 病假天數（半薪）；天數 = SUM(Hours) / 8
            decimal sickDays = leaveDaysMap.TryGetValue((empId, "sick"), out var sd) ? sd : 0m;
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

            decimal netSalary = baseSalary + mealAllowance + overtimePay
                              + holidayAllowance + otherAddition
                              - laborIns - healthIns
                              - personalDeduction - sickDeduction
                              - otherDeduction;

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
                otherDeduction,
                otherDeductionNote,
                note,
                netSalary,
                leaveDetails.TryGetValue(empId, out var ld) ? ld : null));
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
            results.Sum(r => r.OtherDeduction),
            results.Sum(r => r.NetSalary));
    }
}
