using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class PayrollReadService(IDbConnection db) : IPayrollReadService
{
    /// <param name="employeeId">
    /// 指定員工時只計算該員工（供 GET /me/payroll 員工自助查詢近 N 個月使用）；
    /// null＝全公司（人事薪資頁 / 寄送薪資單）。計算邏輯完全共用，不另開一套公式。
    /// </param>
    public async Task<MonthlyPayrollDto> CalculateMonthlyPayrollAsync(int year, int month, Guid? employeeId = null)
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
                   u.OtherAllowance, u.AdjustmentDifference,
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
              AND (@EmployeeId IS NULL OR u.Id = @EmployeeId)
            ORDER BY u.Name
            """;

        // 2. 查詢「上一個月」已核准的假日執行活動天數（以 EndDate 歸月，獎金計入次月薪資）
        //    例：4 月薪資只計入 EndDate 落在 3/1~3/31 的活動；跨月活動以 EndDate 所屬月份歸屬
        //
        //    ⚠ 只認 TravelRequestParticipants（2026-09 改）：
        //    申請人**不再**因為送了單就自動領津貼，要領就得把自己加進參與執行人員清單
        //    （表單的人員下拉本來就含自己）。舊版另有一支 tr.EmployeeId 的 UNION ALL，
        //    導致申請人無條件領整單 HolidayDays，且自己又勾進清單時會被重複計算兩次。
        const string travelSql = """
            SELECT p.UserId AS EmployeeId,
                   -- 有勾選參與日期者取個人假日天數（含半天 0.5）；NULL=全程參與，沿用整單
                   SUM(CAST(COALESCE(p.HolidayDays, tr.HolidayDays) AS decimal(5,1))) AS TotalDays
            FROM TravelRequestParticipants p
            JOIN TravelRequests tr ON p.TravelRequestId = tr.Id
            WHERE tr.IsHolidayTravel = 1
              AND tr.ApprovalStatus = 'approved'
              AND tr.EndDate >= @PrevMonthFirstDay
              AND tr.EndDate <  @CurrMonthFirstDay
              AND (@EmployeeId IS NULL OR p.UserId = @EmployeeId)
            GROUP BY p.UserId
            """;

        // 2b. 查詢「上一個月」加班日、已核准且選擇「加班費」的加班申請
        //     歸月規則與假日津貼一致：加班日所屬月份的**次月**薪資（例：2026/08/15 加班 → 2026/09 薪資）。
        //     金額與時數皆取核准當下寫入的快照欄，故日後調薪不會回溯改動歷史加班費。
        //     EmployeeId IS NOT NULL：FK 為 SetNull，員工刪除後的殘留列會讓 GROUP BY 產生 NULL key，
        //     下方 (Guid)r.EmployeeId 會爆。
        const string overtimePaySql = """
            SELECT o.EmployeeId,
                   SUM(o.OvertimePayAmount) AS TotalPay,
                   SUM(o.PayableHours)      AS TotalHours
            FROM OvertimeRequests o
            WHERE o.ApprovalStatus    = 'approved'
              AND o.CompensationType  = 'pay'
              AND o.OvertimePayAmount IS NOT NULL
              AND o.EmployeeId        IS NOT NULL
              AND o.OvertimeDate >= @PrevMonthFirstDay
              AND o.OvertimeDate <  @CurrMonthFirstDay
              AND (@EmployeeId IS NULL OR o.EmployeeId = @EmployeeId)
            GROUP BY o.EmployeeId
            """;

        // 2c. 國定假日出勤加倍工資（四週彈性工時 §6.3.1）：
        //     國定假日出勤者，前 8 小時**加發 1 日日薪**（月薪已內含當日 1 日工資，故當日合計 2 倍）。
        //     歸月規則同假日津貼與加班費：**加班日所屬月份的次月**薪資。
        //
        //     ⚠ 出勤事實有**兩種來源**，必須 UNION 兩張表並依 (人, 日期) 去重：
        //       A 主管排了活動日、本人為預定人力 → 當天直接打上下班卡，**根本不會有加班單**
        //       B 未排活動日 → 上下班卡鎖定，一律走加班申請
        //     只掃加班單，來源 A 整批領不到；兩表相加而不去重，來源 A 又提第 9 小時起加班單者
        //     會**領兩份日薪**。此處用 UNION（非 UNION ALL）達成去重，故一天恆計 1。
        //
        //     ⚠ 出勤即加發一日、**不按時數比例折算**（客戶明訂），故只算天數、不看時數。
        //
        //     切換日（SystemSetting.FlexibleWorkStartDate）之前**一律不計** —— 這個薪資項是
        //     新制才有的東西，回溯到舊月份會讓歷史薪資憑空變大。null ＝ 尚未切換 ＝ 全部不計。
        const string publicHolidayWorkSql = """
            WITH PublicHolidays AS (
                SELECT c.Date
                FROM CalendarDays c
                CROSS JOIN (SELECT TOP 1 FlexibleWorkStartDate AS SwitchDate
                            FROM SystemSettings ORDER BY Id) s
                WHERE c.IsHoliday = 1
                  -- 國定假日的判準是「IsHoliday 且 Description 非空」：行事曆把週六日也標成
                  -- IsHoliday = 1，只是沒有名稱。只看旗標會把每個週末都算成國定假日。
                  AND c.Description IS NOT NULL AND LTRIM(RTRIM(c.Description)) <> ''
                  AND c.Date >= @PrevMonthFirstDay
                  AND c.Date <  @CurrMonthFirstDay
                  AND s.SwitchDate IS NOT NULL
                  AND c.Date >= s.SwitchDate
            ),
            Worked AS (
                SELECT a.UserId AS EmployeeId, CAST(a.RecordDate AS date) AS WorkDate
                FROM AttendanceRecords a
                JOIN PublicHolidays h ON h.Date = CAST(a.RecordDate AS date)
                WHERE a.ClockInTime IS NOT NULL
                UNION
                SELECT o.EmployeeId, CAST(o.OvertimeDate AS date)
                FROM OvertimeRequests o
                JOIN PublicHolidays h ON h.Date = CAST(o.OvertimeDate AS date)
                WHERE o.ApprovalStatus = 'approved'
                  AND o.EmployeeId IS NOT NULL
            )
            SELECT EmployeeId, COUNT(*) AS Days
            FROM Worked
            WHERE (@EmployeeId IS NULL OR EmployeeId = @EmployeeId)
            GROUP BY EmployeeId
            """;

        // 2d. 補休未休完加班津貼（四週彈性工時 §7.5）：
        //     補休 lot 到期仍未休完者，依該 lot 的**原始加班費率快照**換算為加班津貼，
        //     於到期月的**次月**薪資單獨立顯示。7/31 到期 → 8 月薪資；隔年 1/31 → 隔年 2 月。
        //
        //     ⚠ 費率必須取 lot 上的 RateSnapshot（加班核准當下寫入），**不可事後重算** ——
        //     重算會拿到改版後的費率。時薪則取**現行**底薪（薪資本來就是即時重算、無月結快照）。
        //
        //     ⚠ 視窗是「ExpiresAt 落在上個月」的半開區間，故同一個 lot 只會被結算到一次；
        //     這也是本項**不寫 CompensatoryLots.SettledAt / SettledAmount** 的原因 ——
        //     薪資端即時重算，另寫一份落地金額等於製造第二個真相，兩者遲早對不起來。
        //     那兩個欄位保留給日後「明確結算批次」的設計，目前恆為 null。
        const string compensatorySettlementSql = """
            SELECT l.UserId AS EmployeeId,
                   SUM(l.RemainingHours)                            AS Hours,
                   SUM(l.RemainingHours * ISNULL(l.RateSnapshot, 0)) AS WeightedHours,
                   MIN(l.ExpiresAt)                                 AS ExpiresAt
            FROM CompensatoryLots l
            WHERE l.RemainingHours > 0
              AND l.ExpiresAt >= @PrevMonthFirstDay
              AND l.ExpiresAt <  @CurrMonthFirstDay
              AND (@EmployeeId IS NULL OR l.UserId = @EmployeeId)
            GROUP BY l.UserId
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
              AND (@EmployeeId IS NULL OR EmployeeId = @EmployeeId)
            """;

        // 5. 查詢該月已核准的事假/病假/生理假/家庭照顧假時數（按員工 + 假別分組）
        //    事假/病假/家庭照顧假為「小時」單位，以 SUM(Hours) 累計，C# 端再 ÷ 8 換算天數
        const string leaveSql = """
            SELECT lr.EmployeeId, lr.LeaveType,
                   SUM(lr.Hours) AS TotalHours
            FROM LeaveRequests lr
            WHERE lr.ApprovalStatus = 'approved'
              AND lr.LeaveType IN ('personal', 'sick', 'menstrual', 'family_care')
              AND lr.StartDate <= @LastDay
              AND lr.EndDate   >= @FirstDay
              AND (@EmployeeId IS NULL OR lr.EmployeeId = @EmployeeId)
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
              AND (@EmployeeId IS NULL OR lr.EmployeeId = @EmployeeId)
            GROUP BY lr.EmployeeId
            """;

        // 5c. 查詢該月已核准的育嬰留職停薪日曆天數（逐日歸月）
        //     刻意不沿用上面 leaveSql 的「區間相交 + 整單 SUM(Hours)」寫法 —— 那個寫法會讓跨月假單
        //     在每個月各扣一次全額（見 docs/business/payroll-formula.md 已知限制）。育嬰留停動輒數月，
        //     必須逐日歸月，故改以「假單區間 ∩ 當月區間」的實際天數計算。
        //     parental_leave 為連續日曆天型，parental_leave_daily 強制單日（StartDate = EndDate），
        //     兩者用同一段日期交集即可正確歸月，不需 LeaveDayExpander 逐日展開。
        const string parentalSql = """
            SELECT lr.EmployeeId,
                   SUM(DATEDIFF(day,
                         CASE WHEN lr.StartDate > @FirstDay THEN lr.StartDate ELSE @FirstDay END,
                         CASE WHEN lr.EndDate   < @LastDay  THEN lr.EndDate   ELSE @LastDay  END) + 1) AS Days
            FROM LeaveRequests lr
            WHERE lr.ApprovalStatus = 'approved'
              AND lr.LeaveType IN ('parental_leave', 'parental_leave_daily')
              AND lr.StartDate <= @LastDay
              AND lr.EndDate   >= @FirstDay
              AND (@EmployeeId IS NULL OR lr.EmployeeId = @EmployeeId)
            GROUP BY lr.EmployeeId
            """;

        // 6. 查詢該月所有已核准的請假明細（全假別）
        const string leaveDetailSql = """
            SELECT lr.EmployeeId, lr.LeaveType, lr.StartDate, lr.EndDate, lr.Hours
            FROM LeaveRequests lr
            WHERE lr.ApprovalStatus = 'approved'
              AND lr.StartDate <= @LastDay
              AND lr.EndDate   >= @FirstDay
              AND (@EmployeeId IS NULL OR lr.EmployeeId = @EmployeeId)
            ORDER BY lr.StartDate
            """;

        var employees = (await db.QueryAsync<dynamic>(employeeSql, new { FirstDay = firstDay, EmployeeId = employeeId })).ToList();
        var travelDays = (await db.QueryAsync<dynamic>(travelSql, new {
                PrevMonthFirstDay = prevMonthFirstDay,
                CurrMonthFirstDay = firstDay,
                EmployeeId        = employeeId,
            }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => (decimal)r.TotalDays);
        // 加班申請試算加班費（上月加班、已核准且選「加班費」者的快照合計）
        var overtimePayRows = (await db.QueryAsync<dynamic>(overtimePaySql, new {
                PrevMonthFirstDay = prevMonthFirstDay,
                CurrMonthFirstDay = firstDay,
                EmployeeId        = employeeId,
            }))
            .ToList();
        var calcOvertimePayMap   = overtimePayRows.ToDictionary(r => (Guid)r.EmployeeId, r => (decimal)r.TotalPay);
        var calcOvertimeHoursMap = overtimePayRows.ToDictionary(r => (Guid)r.EmployeeId, r => (decimal)r.TotalHours);
        // 國定假日出勤天數（上月；打卡 ∪ 已核准加班單，依日期去重）
        var publicHolidayDaysMap = (await db.QueryAsync<dynamic>(publicHolidayWorkSql, new {
                PrevMonthFirstDay = prevMonthFirstDay,
                CurrMonthFirstDay = firstDay,
                EmployeeId        = employeeId,
            }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => (int)r.Days);
        // 到期未休完的補休 lot（上月到期者，於本月薪資結算）
        var compSettlementMap = (await db.QueryAsync<dynamic>(compensatorySettlementSql, new {
                PrevMonthFirstDay = prevMonthFirstDay,
                CurrMonthFirstDay = firstDay,
                EmployeeId        = employeeId,
            }))
            .ToDictionary(r => (Guid)r.EmployeeId,
                          r => ((decimal)r.Hours, (decimal)r.WeightedHours, (DateTime)r.ExpiresAt));
        var brackets = (await db.QueryAsync<dynamic>(bracketSql)).ToList();
        var adjustments = (await db.QueryAsync<dynamic>(adjustmentSql, new { Year = year, Month = month, EmployeeId = employeeId }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => r);

        // 事假/病假天數：Dictionary<(EmployeeId, LeaveType), TotalDays (decimal)>
        // TotalHours ÷ 8 = 實際天數（保留小數，例如 2 小時 = 0.25 天）
        var leaveRecords = (await db.QueryAsync<dynamic>(leaveSql, new { FirstDay = firstDay, LastDay = lastDay, EmployeeId = employeeId }))
            .ToList();
        var leaveDaysMap = new Dictionary<(Guid, string), decimal>();
        foreach (var lr in leaveRecords)
            leaveDaysMap[((Guid)lr.EmployeeId, (string)lr.LeaveType)] = (decimal)lr.TotalHours / 8m;

        // 本年度本月之前已用生理假時數：Dictionary<EmployeeId, PriorHours>
        var yearFirstDay = new DateTime(year, 1, 1);
        var priorMenstrualMap = (await db.QueryAsync<dynamic>(priorMenstrualSql, new { YearFirstDay = yearFirstDay, FirstDay = firstDay, EmployeeId = employeeId }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => (decimal)r.TotalHours);

        // 請假明細：Dictionary<EmployeeId, LeaveDetailDto[]>
        var leaveDetails = (await db.QueryAsync<dynamic>(leaveDetailSql, new { FirstDay = firstDay, LastDay = lastDay, EmployeeId = employeeId }))
            .GroupBy(r => (Guid)r.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new LeaveDetailDto(
                    (string)r.LeaveType,
                    (DateTime)r.StartDate,
                    (DateTime)r.EndDate,
                    (decimal)r.Hours)).ToArray());

        // 育嬰留停天數：Dictionary<EmployeeId, 該月留停日曆天數>
        var parentalDaysMap = (await db.QueryAsync<dynamic>(parentalSql, new { FirstDay = firstDay, LastDay = lastDay, EmployeeId = employeeId }))
            .ToDictionary(r => (Guid)r.EmployeeId, r => (decimal)r.Days);

        int daysInMonth = DateTime.DaysInMonth(year, month);

        var results = new List<EmployeePayrollDto>();
        foreach (var emp in employees)
        {
            decimal baseSalary     = (decimal)emp.BaseSalary;
            decimal mealAllowance  = (decimal?)emp.MealAllowance ?? 0m;
            decimal overtimePay    = (decimal?)emp.OvertimePay ?? 0m;
            decimal otherAllow     = (decimal?)emp.OtherAllowance       ?? 0m;
            decimal adjDiff        = (decimal?)emp.AdjustmentDifference ?? 0m;
            // dailySalary 刻意在折減前先算好：事假 / 病假 / 生理假 / 家庭照顧假的扣薪仍以
            // 原始底薪推算的日薪為基準，避免留停按比例後又被重複折減一次。
            decimal dailySalary    = Math.Round(baseSalary / 30m, 0);

            // 上月國定假日出勤天數（打卡 ∪ 已核准加班單，已依日期去重）。
            // 提早取出是因為下方「整月留停是否整列剔除」要看它 —— 漏看會讓整月留停卻在
            // 國定假日出勤過的人整列消失，該筆加倍工資憑空不見。
            int publicHolidayWorkDays = publicHolidayDaysMap.TryGetValue((Guid)emp.EmployeeId, out var phd) ? phd : 0;

            // 上月到期、仍未休完的補休 lot（見下方 netSalary；同樣提早取出供留停判定）。
            // 時薪與加班費同一支公式（ROUND(底薪 ÷ 240, 2)），刻意不沿用 dailySalary ÷ 8
            //（後者已先 ROUND 到整數元，再除 8 會繼承取整誤差）。
            // 底薪取**折減前**的值 —— 這批時數是過去半年賺得的，不該因為結算當月剛好在育嬰留停而縮水。
            decimal compSettlementHours  = 0m;
            decimal compSettlementAmount = 0m;
            string? compSettlementNote   = null;
            if (compSettlementMap.TryGetValue((Guid)emp.EmployeeId, out var cs))
            {
                compSettlementHours  = cs.Item1;
                compSettlementAmount = Math.Round(
                    cs.Item2 * OvertimePayCalculator.HourlyRate(baseSalary),
                    0, MidpointRounding.AwayFromZero);
                compSettlementNote   = CompensatoryLotService.SettlementLabel(cs.Item3);
            }

            decimal holidayDays = travelDays.TryGetValue((Guid)emp.EmployeeId, out var days) ? days : 0m;

            // 加班申請試算加班費：與手填的 User.OvertimePay 併存、不取代 —— 兩者是不同來源的兩筆錢
            decimal calcOvertimePay   = calcOvertimePayMap.TryGetValue((Guid)emp.EmployeeId, out var cop) ? cop : 0m;
            decimal calcOvertimeHours = calcOvertimeHoursMap.TryGetValue((Guid)emp.EmployeeId, out var coh) ? coh : 0m;

            // ── 育嬰留職停薪：不支薪 ────────────────────────────────────────────
            // 折減率＝「1 − 留停天數 ÷ 30」，與事假等無薪假的「日薪 × 天數」完全等價
            // （日薪本身就是底薪 ÷ 30）。刻意不用「(當月天數 − 留停天數) ÷ 30」：
            // 31 天的月份請 1 天留停時該式為 30/30 = 1，會完全不折減，「不支薪」形同無效。
            // 不折減的項目：勞健保（續保者仍須繳全額）、勞退自提、加班費與假日津貼（本就是實績金額）。
            decimal parentalLeaveDays = parentalDaysMap.TryGetValue((Guid)emp.EmployeeId, out var pld) ? pld : 0m;

            // 投保／提繳基準：恆為折減前的底薪。留停續保者的投保薪資不會因當月少領而降級，
            // 若用折減後的底薪查級距，會連帶把勞健保也「按比例」少扣（例：底薪 60000 折成 8000
            // 會掉到最低級距 29500），與「勞健保不折減」的規則相違。勞退自提同理。
            decimal insuredBaseSalary = baseSalary;

            if (parentalLeaveDays > 0)
            {
                // 整月留停且當月確實無其他應發／扣項時，整列剔除（不產生薪資單）。
                // 有加班費、上月假日津貼或當月薪資調整（其他加項／扣項）時仍須出單，
                // 否則這些已賺得的金額會憑空消失且不計入月合計。
                bool hasOtherItems = overtimePay != 0m || calcOvertimePay != 0m || holidayDays > 0m
                                  || publicHolidayWorkDays > 0 || compSettlementAmount != 0m
                                  || (adjustments.TryGetValue((Guid)emp.EmployeeId, out var padj)
                                      && ((decimal)padj.OtherAddition != 0m || (decimal)padj.OtherDeduction != 0m));
                if (parentalLeaveDays >= daysInMonth && !hasOtherItems) continue;

                decimal workRatio = Math.Max(0m, 1m - parentalLeaveDays / 30m);
                baseSalary    = Math.Round(baseSalary    * workRatio, 0);
                mealAllowance = Math.Round(mealAllowance * workRatio, 0);
                otherAllow    = Math.Round(otherAllow    * workRatio, 0);
                adjDiff       = Math.Round(adjDiff       * workRatio, 0);
            }

            // 參與人員可逐日勾上半天 / 下半天，故 holidayDays 為 0.5 的倍數；
            // dailySalary 已四捨五入為整數，奇數日薪 × .5 天必然落在中點，
            // 明確指定 AwayFromZero（Math.Round 預設是銀行家捨入，會少 1 元）。
            decimal holidayAllowance = Math.Round(dailySalary * holidayDays, 0, MidpointRounding.AwayFromZero);

            // 國定假日出勤加倍工資：出勤天數 × 日薪（天數為整數，不會有 .5 的中點問題）。
            // ⚠ 刻意用**折減前**的 dailySalary —— 與假日津貼同一基準；留停折減只作用在底薪與加給。
            decimal publicHolidayDoublePay = dailySalary * publicHolidayWorkDays;


            // 查找級距：第一個 SalaryBracket >= 投保底薪的級距，若無則取最高級距
            var bracket = brackets.FirstOrDefault(b => (decimal)b.SalaryBracket >= insuredBaseSalary)
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

            // 家庭照顧假扣薪：日薪 × 天數（不另支薪，比照事假全額扣除）
            decimal familyCareDays = leaveDaysMap.TryGetValue((empId, "family_care"), out var fcd) ? fcd : 0m;
            decimal familyCareDeduction = Math.Round(dailySalary * familyCareDays, 0);

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

            // 勞退自提扣款 = 提繳底薪 × 自提率%，四捨五入至整數（比照勞健保費公式）
            // 用 insuredBaseSalary：留停當月的提繳基準同樣不隨少領而降低
            decimal laborPensionSelfDeduction = Math.Round(insuredBaseSalary * (laborPensionRate ?? 0m) / 100m, 0);

            decimal netSalary = baseSalary + mealAllowance + overtimePay + calcOvertimePay
                              + otherAllow + adjDiff
                              + holidayAllowance + publicHolidayDoublePay + compSettlementAmount + otherAddition
                              - laborIns - healthIns
                              - personalDeduction - sickDeduction - menstrualDeduction
                              - familyCareDeduction
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
                familyCareDays,
                familyCareDeduction,
                otherDeduction,
                otherDeductionNote,
                note,
                netSalary,
                leaveDetails.TryGetValue(empId, out var ld) ? ld : null,
                dependentCount,
                cappedN,
                otherAllow,
                adjDiff,
                laborPensionRate,
                laborPensionSelfDeduction,
                parentalLeaveDays,
                calcOvertimePay,
                calcOvertimeHours,
                publicHolidayWorkDays,
                publicHolidayDoublePay,
                compSettlementHours,
                compSettlementAmount,
                compSettlementNote));
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
            results.Sum(r => r.FamilyCareLeaveDeduction),
            results.Sum(r => r.OtherDeduction),
            results.Sum(r => r.NetSalary),
            results.Sum(r => r.OtherAllowanceAmount),
            results.Sum(r => r.AdjustmentDifference),
            results.Sum(r => r.LaborPensionSelfDeduction),
            results.Sum(r => r.ParentalLeaveDays),
            results.Sum(r => r.CalculatedOvertimePay),
            results.Sum(r => r.PublicHolidayDoublePay),
            results.Sum(r => r.CompensatorySettlementAmount));
    }
}
