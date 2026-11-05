/*
================================================================================
 補登一張「已核准」的事後加班申請（補休）並寫入出缺勤（首例：劉闓毅 2026-11-04 14:30–18:30 4 小時）
================================================================================

 背景
 ----
 系統沒有「管理者代建加班單」的入口（加班單的申請人一律取 JWT）。本案由管理端交辦直接補登，
 不經簽核，以腳本一次寫入。寫入結果等同「本人送出事後申請 → Superadmin 自動核准」那條路徑
 （OvertimeRequestHandler.SubmitAsync 的 Superadmin 分支 + PostHocOvertimeService + CompensatoryLotService）。

 寫入範圍
 --------
   OvertimeRequests         ：一張 approved、RequestKind = post_hoc、CompensationType = compensatory，
                              ActualStartTime / ActualEndTime ＝ 交辦起訖，EstimatedHours ＝ @Hours，
                              SettledHours ＝ min(@Hours, 起訖時數)（休假日無正常上班時段可扣），
                              單號 OT-{今日}-NNN（當日流水號接續），SubmittedAt / ReviewedAt ＝ 現在，
                              ReviewedById ＝ Superadmin、ReviewNote 註明交辦補登；ApprovalItemId 留 NULL（比照 Superadmin 自動核准）。
                              加班費快照欄全部 NULL（補休單本來就不寫，見 OvertimeCompensationService.ClearSnapshot）。
   OvertimeRequestProjects  ：一列 (@ProjectCode, @Hours)
   AttendanceRecords        ：當日有紀錄 → 寫入加班起訖、綁 OvertimeRequestId、IsManuallyAdjusted；
                              無紀錄（休假日沒打上下班卡）→ 建立只含加班時間的紀錄。上班 / 下班卡一律不動。
   AttendanceAuditLogs      ：一列，ModifiedByName ＝「系統（管理端交辦補登 {單號}）」
   CompensatoryLots         ：**僅當加班日 ≥ 四週彈性工時切換日**才開 lot（與 CompensatoryLotService.ApplyAsync 同條件）：
                              Hours / RemainingHours ＝ SettledHours、RateSnapshot ＝ 加權平均倍率（四捨五入 2 位）、
                              ExpiresAt ＝ 1–6 月 → 當年 7/31 23:59:59；7–12 月 → 隔年 1/31 23:59:59。
                              切換日 null 或加班日早於切換日 → 不開 lot，舊制補休餘額由已核准的補休型加班單即時加總。

 保護閘門（任一命中即整份中止）
 ------------------------------
   ① 姓名查無 / 同名不只一位在職者（同名時改用 @Email 定位）
   ② @ProjectCode 未填、查無、非唯一或專案已結案（未填時報表 B 會列出該員近期加班用過的專案）
   ③ 起訖不合理：訖 ≤ 起、起不在加班日當天、訖 ≥ 隔日 06:00、@Hours ≤ 0 或超過起訖時數
   ④ 該員該日已有非 rejected 的加班單（同人同日只能一張）
   ⑤ 當日出缺勤已有加班打卡
   ⑥ 日別不是休假日：上班日（起訖與正常上班時段重疊，結算會被扣掉 → 應走事後申請由系統結算）、
      例假日（依法禁止加班）、新制國定假日（前 8 小時改走加倍工資，不適用本腳本）一律中止
   ⑦ 超過單日上限（休假日 12 小時）

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。冪等：閘門 ④ 擋重跑，不會建出第二張。

 空跑要確認：
   ① 「目標員工」只有 1 列且是本人
   ② 「日別判定」為 rest_day，「是否開 lot」符合預期
   ③ 「寫入後」加班單 / 出缺勤 / 補休 lot 的時數與交辦一致
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit      bit           = 0;                  -- ← 空跑用 0；確認後改成 1 才會真正寫入

DECLARE @Name        nvarchar(100) = N'劉闓毅';
DECLARE @Email       nvarchar(256) = NULL;               -- 同名時填 Email 定位（優先於姓名）
DECLARE @Date        date          = '2026-11-04';
DECLARE @Start       datetime2(0)  = '2026-11-04T14:30:00';
DECLARE @End         datetime2(0)  = '2026-11-04T18:30:00';
DECLARE @Hours       decimal(5,1)  = 4.0;
DECLARE @ProjectCode nvarchar(50)  = N'J11511';         -- ← 必填：加班歸屬的專案代號（空跑報表 B 可參考）
DECLARE @Reason      nvarchar(500) = N'管理端交辦補登：11/4 14:30–18:30 加班 4 小時（補休）';

DECLARE @SuperAdminId uniqueidentifier = '00000000-0000-0000-0000-000000000001';
DECLARE @Now datetime2(0) = CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time');
DECLARE @CrossDayCutoffHour int = 6;                     -- OvertimeSettlementService.CrossDayEndCutoffHour

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- A) 解析目標員工 / 專案 / 日別
-- ---------------------------------------------------------------------------
DECLARE @Users TABLE (Id uniqueidentifier PRIMARY KEY);
INSERT INTO @Users (Id)
SELECT u.Id
FROM Users u
WHERE u.IsSuperAdmin = 0
  AND u.Status = 'active'
  AND ( (@Email IS NOT NULL AND u.Email = @Email)
     OR (@Email IS NULL     AND u.Name  = @Name) );

DECLARE @UserId uniqueidentifier = (SELECT TOP 1 Id FROM @Users);
DECLARE @IsShiftWorker bit = (SELECT IsShiftWorker FROM Users WHERE Id = @UserId);

DECLARE @Projects TABLE (Id int PRIMARY KEY, Status nvarchar(20));
INSERT INTO @Projects (Id, Status)
SELECT p.Id, p.Status FROM Projects p WHERE @ProjectCode IS NOT NULL AND p.Code = @ProjectCode;
DECLARE @ProjectId int = (SELECT TOP 1 Id FROM @Projects);

-- 四週彈性工時切換日（SystemSettings 單列）
DECLARE @SwitchDate date = (SELECT TOP 1 CAST(FlexibleWorkStartDate AS date) FROM SystemSettings ORDER BY Id);
DECLARE @Flexible bit = CASE WHEN @SwitchDate IS NOT NULL AND @Date >= @SwitchDate THEN 1 ELSE 0 END;

-- 日別：比照 ShiftScheduleReadService.ResolveDayTypeAsync + OvertimePayCalculator.EffectiveDayType
--   新制：國定假日（IsHoliday 且 Description 非空）優先 → 個人排班 → 查無即上班日
--   舊制：排班制員工恆為上班日；有行事曆用 IsHoliday、該年無資料退回六日；國定假日收斂為 rest_day
DECLARE @IsPublicHoliday bit = CASE WHEN EXISTS (
    SELECT 1 FROM CalendarDays c
    WHERE CAST(c.Date AS date) = @Date AND c.IsHoliday = 1 AND ISNULL(c.Description, N'') <> N'') THEN 1 ELSE 0 END;
DECLARE @ScheduledType varchar(20) = (
    SELECT TOP 1 s.DayType FROM ShiftScheduleDays s WHERE s.UserId = @UserId AND CAST(s.Date AS date) = @Date);
DECLARE @HasCalendarYear bit = CASE WHEN EXISTS (SELECT 1 FROM CalendarDays WHERE Year = YEAR(@Date)) THEN 1 ELSE 0 END;
DECLARE @CalendarHoliday bit = CASE
    WHEN @HasCalendarYear = 1 THEN CASE WHEN EXISTS (
            SELECT 1 FROM CalendarDays c WHERE CAST(c.Date AS date) = @Date AND c.IsHoliday = 1) THEN 1 ELSE 0 END
    WHEN DATEPART(weekday, @Date) IN (1, 7) THEN 1   -- 依 @@DATEFIRST 預設 7：1＝週日、7＝週六
    ELSE 0 END;

DECLARE @DayType varchar(20) = CASE
    WHEN @Flexible = 1 AND @IsPublicHoliday = 1      THEN 'public_holiday'
    WHEN @Flexible = 1                               THEN ISNULL(@ScheduledType, 'work')
    WHEN @IsShiftWorker = 1                          THEN 'work'
    WHEN @CalendarHoliday = 1                        THEN 'rest_day'
    ELSE 'work' END;

-- 起訖時數（捨去到 0.1 小時）與結算時數
DECLARE @PunchHours decimal(5,1) = FLOOR(DATEDIFF(minute, @Start, @End) / 6.0) / 10.0;
DECLARE @Settled    decimal(5,1) = CASE WHEN @Hours < @PunchHours THEN @Hours ELSE @PunchHours END;

-- 補休 lot 加權倍率（休假日級距：1–2h ×1.34、3–8h ×1.67、9h 起 ×2.67），比照 CompensatoryLotService.WeightedRate
DECLARE @T1 decimal(9,4) = CASE WHEN @Settled < 2 THEN @Settled ELSE 2 END;
DECLARE @T2 decimal(9,4) = CASE WHEN @Settled <= 2 THEN 0 WHEN @Settled < 8 THEN @Settled - 2 ELSE 6 END;
DECLARE @T3 decimal(9,4) = CASE WHEN @Settled > 8 THEN @Settled - 8 ELSE 0 END;
DECLARE @Rate decimal(5,2) = CASE WHEN @Settled > 0
    THEN ROUND((@T1 * 1.34 + @T2 * 1.67 + @T3 * 2.67) / @Settled, 2) END;
DECLARE @ExpiresAt datetime2(0) = CASE WHEN MONTH(@Date) <= 6
    THEN DATETIMEFROMPARTS(YEAR(@Date),     7, 31, 23, 59, 59, 0)
    ELSE DATETIMEFROMPARTS(YEAR(@Date) + 1, 1, 31, 23, 59, 59, 0) END;

-- ---------------------------------------------------------------------------
-- B) 異動前報表
-- ---------------------------------------------------------------------------
PRINT N'=== 目標員工 ===';
SELECT u.Id, u.Name, u.Email, d.Name AS [部門], u.IsShiftWorker, u.Status
FROM @Users x JOIN Users u ON u.Id = x.Id
LEFT JOIN Departments d ON d.Id = u.DepartmentId;

PRINT N'=== 日別判定 ===';
SELECT CONVERT(varchar(10), @Date, 23) AS [加班日], @SwitchDate AS [切換日], @Flexible AS [新制],
       @ScheduledType AS [個人排班], @IsPublicHoliday AS [國定假日], @DayType AS [判定日別],
       @PunchHours AS [起訖時數], @Settled AS [結算時數], @Rate AS [lot倍率], @ExpiresAt AS [lot到期],
       CASE WHEN @Flexible = 1 THEN N'開 lot' ELSE N'不開 lot（舊制即時加總）' END AS [補休額度];

PRINT N'=== 該員近 90 天加班單用過的專案（@ProjectCode 參考）===';
SELECT p.Code, p.Name, p.Status, COUNT(*) AS [次數], MAX(o.OvertimeDate) AS [最近一次]
FROM OvertimeRequests o
JOIN OvertimeRequestProjects op ON op.OvertimeRequestId = o.Id
JOIN Projects p ON p.Id = op.ProjectId
WHERE o.EmployeeId = @UserId AND o.OvertimeDate >= DATEADD(day, -90, @Date)
GROUP BY p.Code, p.Name, p.Status
ORDER BY MAX(o.OvertimeDate) DESC;

PRINT N'=== 當日既有加班單 ===';
SELECT o.Id, o.RequestNo, o.ApprovalStatus, o.RequestKind, o.EstimatedHours, o.SettledHours
FROM OvertimeRequests o
WHERE o.EmployeeId = @UserId AND CAST(o.OvertimeDate AS date) = @Date;

PRINT N'=== 當日出缺勤（異動前）===';
SELECT a.Id, a.ClockInTime, a.ClockOutTime, a.OvertimeStartTime, a.OvertimeEndTime, a.OvertimeRequestId, a.Remark
FROM AttendanceRecords a
WHERE a.UserId = @UserId AND CAST(a.RecordDate AS date) = @Date;

PRINT N'=== 當日已核准請假（僅供參考）===';
SELECT l.Id, l.LeaveType, l.StartDate, l.EndDate, l.ApprovalStatus
FROM LeaveRequests l
WHERE l.EmployeeId = @UserId AND l.ApprovalStatus IN ('pending', 'approved')
  AND CAST(l.StartDate AS date) <= @Date AND CAST(l.EndDate AS date) >= @Date;

-- ---------------------------------------------------------------------------
-- C) 保護閘門
-- ---------------------------------------------------------------------------
IF (SELECT COUNT(*) FROM @Users) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'目標員工查無或同名不只一位在職者，已中止（同名請改填 @Email）。', 16, 1);
    RETURN;
END

IF @ProjectCode IS NULL OR (SELECT COUNT(*) FROM @Projects) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'@ProjectCode 未填、查無或代號不唯一，已中止（可參考報表「近 90 天加班單用過的專案」）。', 16, 1);
    RETURN;
END

IF (SELECT Status FROM @Projects) <> 'active'
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'專案已結案，已中止。', 16, 1);
    RETURN;
END

IF @End <= @Start
   OR CAST(@Start AS date) <> @Date
   OR @End >= DATEADD(hour, @CrossDayCutoffHour, CAST(DATEADD(day, 1, @Date) AS datetime2(0)))
   OR @Hours <= 0 OR @Hours > @PunchHours
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'起訖或時數不合理（訖須晚於起、起須在加班日當天、訖須早於隔日 06:00、時數須 > 0 且不超過起訖時數），已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM OvertimeRequests o
           WHERE o.EmployeeId = @UserId AND CAST(o.OvertimeDate AS date) = @Date
             AND o.ApprovalStatus <> 'rejected')
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'該員該日已有加班單（非已拒絕），同人同日只能一張；若為本腳本先前寫入則已完成，已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM AttendanceRecords a
           WHERE a.UserId = @UserId AND CAST(a.RecordDate AS date) = @Date
             AND (a.OvertimeStartTime IS NOT NULL OR a.OvertimeEndTime IS NOT NULL))
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'當日出缺勤已有加班打卡，已中止（請先確認是否重複）。', 16, 1);
    RETURN;
END

IF @DayType <> 'rest_day'
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'判定日別不是休假日（上班日 / 例假日 / 國定假日），本腳本不適用，已中止；請回報日別再決定處理方式。', 16, 1);
    RETURN;
END

IF @Hours > 12
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'超過休假日單日加班上限 12 小時，已中止。', 16, 1);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- D) 寫入
-- ---------------------------------------------------------------------------
-- 單號：OT-{今日}-NNN 當日流水號接續（比照 RequestNoGenerator）
DECLARE @Prefix nvarchar(20) = N'OT-' + CONVERT(char(8), @Now, 112) + N'-';
DECLARE @Seq int = ISNULL((
    SELECT MAX(TRY_CAST(RIGHT(RequestNo, 3) AS int)) FROM OvertimeRequests WITH (UPDLOCK, HOLDLOCK)
    WHERE RequestNo LIKE @Prefix + N'%'), 0) + 1;
DECLARE @RequestNo nvarchar(30) = @Prefix + RIGHT(N'000' + CAST(@Seq AS nvarchar(10)), 3);

INSERT INTO OvertimeRequests (
    RequestNo, EmployeeId, ApprovalItemId, OvertimeDate, EstimatedHours, SettledHours, CompensationType,
    RequestKind, ActualStartTime, ActualEndTime, Reason, ApprovalStatus, CurrentStepOrder,
    ReviewedById, ReviewedAt, ReviewNote, CreatedAt, SubmittedAt)
VALUES (
    @RequestNo, @UserId, NULL, CAST(@Date AS datetime2(0)), @Hours, @Settled, 'compensatory',
    'post_hoc', @Start, @End, @Reason, 'approved', 1,
    @SuperAdminId, @Now, N'管理端交辦以腳本補登（Scripts/22）', @Now, @Now);

DECLARE @OtId int = SCOPE_IDENTITY();

INSERT INTO OvertimeRequestProjects (OvertimeRequestId, ProjectId, EstimatedHours, SortOrder)
VALUES (@OtId, @ProjectId, @Hours, 0);

-- 出缺勤：無紀錄則建立只含加班時間的紀錄
DECLARE @AttId int = (SELECT TOP 1 Id FROM AttendanceRecords
                      WHERE UserId = @UserId AND CAST(RecordDate AS date) = @Date);
IF @AttId IS NULL
BEGIN
    -- IsLate / IsEarlyLeave 在 DB 沒有預設值，必須明填
    INSERT INTO AttendanceRecords (UserId, RecordDate, CreatedAt,
        IsClockInAuto, IsClockOutAuto, IsBusinessTrip, IsManuallyAdjusted, IsLate, IsEarlyLeave)
    VALUES (@UserId, CAST(@Date AS datetime2(0)), @Now, 0, 0, 0, 0, 0, 0);
    SET @AttId = SCOPE_IDENTITY();
END

-- 稽核（Before 取寫入前的值）
INSERT INTO AttendanceAuditLogs (
    AttendanceRecordId, OwnerUserId, RecordDate, ModifiedById, ModifiedByName, ModifiedAt,
    ClockInBefore, ClockInAfter, ClockOutBefore, ClockOutAfter,
    OvertimeStartBefore, OvertimeStartAfter, OvertimeEndBefore, OvertimeEndAfter, RemarkBefore, RemarkAfter)
SELECT a.Id, a.UserId, a.RecordDate, @SuperAdminId, N'系統（管理端交辦補登 ' + @RequestNo + N'）', @Now,
       a.ClockInTime, a.ClockInTime, a.ClockOutTime, a.ClockOutTime,
       a.OvertimeStartTime, @Start, a.OvertimeEndTime, @End, a.Remark, a.Remark
FROM AttendanceRecords a WHERE a.Id = @AttId;

UPDATE AttendanceRecords
SET OvertimeStartTime  = @Start,
    OvertimeEndTime    = @End,
    OvertimeRequestId  = @OtId,
    IsManuallyAdjusted = 1,
    LastAdjustedById   = @SuperAdminId,
    LastAdjustedAt     = @Now
WHERE Id = @AttId;

-- 補休 lot（僅新制）
IF @Flexible = 1 AND @Settled > 0
    INSERT INTO CompensatoryLots (
        UserId, SourceOvertimeRequestId, EarnedDate, Hours, RemainingHours, RateSnapshot,
        ExpiresAt, IsOpening, CreatedAt)
    VALUES (
        @UserId, @OtId, CAST(@Date AS datetime2(0)), @Settled, @Settled, @Rate,
        @ExpiresAt, 0, @Now);

-- ---------------------------------------------------------------------------
-- E) 寫入後報表
-- ---------------------------------------------------------------------------
PRINT N'=== 寫入後：加班單 ===';
SELECT o.Id, o.RequestNo, o.OvertimeDate, o.RequestKind, o.CompensationType, o.ApprovalStatus,
       o.ActualStartTime, o.ActualEndTime, o.EstimatedHours, o.SettledHours, o.ReviewNote
FROM OvertimeRequests o WHERE o.Id = @OtId;

PRINT N'=== 寫入後：專案明細 ===';
SELECT op.ProjectId, p.Code, p.Name, op.EstimatedHours
FROM OvertimeRequestProjects op JOIN Projects p ON p.Id = op.ProjectId
WHERE op.OvertimeRequestId = @OtId;

PRINT N'=== 寫入後：出缺勤 ===';
SELECT a.Id, a.ClockInTime, a.ClockOutTime, a.OvertimeStartTime, a.OvertimeEndTime, a.OvertimeRequestId, a.IsManuallyAdjusted
FROM AttendanceRecords a WHERE a.Id = @AttId;

PRINT N'=== 寫入後：補休 lot（舊制為空）===';
SELECT l.Id, l.EarnedDate, l.Hours, l.RemainingHours, l.RateSnapshot, l.ExpiresAt
FROM CompensatoryLots l WHERE l.SourceOvertimeRequestId = @OtId;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'>>> 已 COMMIT。';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'>>> 空跑完成（已 ROLLBACK）。確認無誤後將 @Commit 改為 1 再執行。';
END
