/*
================================================================================
 為「已核准」的加班單補寫當日加班打卡並結算（首例：劉闓毅 2026-11-04 14:30–18:30 4 小時，補休）
================================================================================

 背景
 ----
 員工已有一張核准的加班單，但加班當天打不了加班卡（2026-11-05 修正前，新制下個人休假日 /
 全天請假時「加班開始」要求先打下班卡而被擋，見 commit f76ca4e5）。新制加班單核准時 SettledHours＝0，
 要靠加班打卡結算，沒打到卡＝補休 0 小時。

 與 Scripts/22 的差別：22 是「系統沒有加班單 → 新建一張已核准單」，遇到同日已有加班單會中止；
 本腳本是「已有核准單 → 只補打卡與結算」，不新建加班單。

 寫入範圍（結果等同管理者在出缺勤報表補填加班起訖 → OvertimeSettlementService.SyncFromPunchesAsync）
 --------
   AttendanceRecords   ：當日有紀錄 → 寫入加班起訖、綁 OvertimeRequestId；
                         無紀錄（休假日沒打上下班卡）→ 建立只含加班時間的紀錄。上班 / 下班卡一律不動。
   AttendanceAuditLogs ：一列，ModifiedByName ＝「系統（補登加班打卡 {單號}）」
   OvertimeRequests    ：SettledHours ＝ min(EstimatedHours, 起訖時數捨去到 0.1 小時)（休假日無正常上班時段可扣）
   CompensatoryLots    ：僅當加班日 ≥ 切換日且補償方式為補休：無 lot 則新建；已有 lot 則更新時數，
                         RemainingHours ＝ 新時數 − 已扣抵（比照 CompensatoryLotService.ApplyAsync）

 保護閘門（任一命中即整份中止）
 ------------------------------
   ① 姓名查無 / 同名不只一位在職者（同名時改用 @Email 定位）
   ② 該員該日「已核准、新制（SettledHours 非 NULL）」的加班單不是恰好一張
   ③ 起訖不合理：訖 ≤ 起、起不在加班日當天、訖 ≥ 隔日 06:00
   ④ 當日出缺勤已有加班打卡（請改走出缺勤報表編輯，系統會自動結算）
   ⑤ 日別不是休假日（上班日要扣正常時段、例假日 / 國定假日規則不同，本腳本不適用）

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）；確認後改 1。冪等：閘門 ④ 擋重跑。
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit bit           = 0;                  -- ← 空跑用 0；確認後改成 1 才會真正寫入

DECLARE @Name   nvarchar(100) = N'劉闓毅';
DECLARE @Email  nvarchar(256) = NULL;               -- 同名時填 Email 定位（優先於姓名）
DECLARE @Date   date          = '2026-11-04';
DECLARE @Start  datetime2(0)  = '2026-11-04T14:30:00';
DECLARE @End    datetime2(0)  = '2026-11-04T18:30:00';

DECLARE @SuperAdminId uniqueidentifier = '00000000-0000-0000-0000-000000000001';
DECLARE @Now datetime2(0) = CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time');
DECLARE @CrossDayCutoffHour int = 6;                -- OvertimeSettlementService.CrossDayEndCutoffHour

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- A) 解析目標員工 / 加班單 / 日別
-- ---------------------------------------------------------------------------
DECLARE @Users TABLE (Id uniqueidentifier PRIMARY KEY);
INSERT INTO @Users (Id)
SELECT u.Id FROM Users u
WHERE u.IsSuperAdmin = 0 AND u.Status = 'active'
  AND ( (@Email IS NOT NULL AND u.Email = @Email)
     OR (@Email IS NULL     AND u.Name  = @Name) );

DECLARE @UserId uniqueidentifier = (SELECT TOP 1 Id FROM @Users);
DECLARE @IsShiftWorker bit = (SELECT IsShiftWorker FROM Users WHERE Id = @UserId);

DECLARE @Ots TABLE (Id int PRIMARY KEY);
INSERT INTO @Ots (Id)
SELECT o.Id FROM OvertimeRequests o
WHERE o.EmployeeId = @UserId AND CAST(o.OvertimeDate AS date) = @Date
  AND o.ApprovalStatus = 'approved' AND o.SettledHours IS NOT NULL;

DECLARE @OtId int = (SELECT TOP 1 Id FROM @Ots);
DECLARE @RequestNo nvarchar(50), @Estimated decimal(5,1), @CompType nvarchar(20);
SELECT @RequestNo = RequestNo, @Estimated = EstimatedHours, @CompType = CompensationType
FROM OvertimeRequests WHERE Id = @OtId;

DECLARE @SwitchDate date = (SELECT TOP 1 CAST(FlexibleWorkStartDate AS date) FROM SystemSettings ORDER BY Id);
DECLARE @Flexible bit = CASE WHEN @SwitchDate IS NOT NULL AND @Date >= @SwitchDate THEN 1 ELSE 0 END;

-- 日別（同 Scripts/22）：新制 國定假日 → 個人排班 → 上班日；舊制 排班制恆上班日、行事曆 IsHoliday、無資料退回六日
DECLARE @IsPublicHoliday bit = CASE WHEN EXISTS (
    SELECT 1 FROM CalendarDays c
    WHERE CAST(c.Date AS date) = @Date AND c.IsHoliday = 1 AND ISNULL(c.Description, N'') <> N'') THEN 1 ELSE 0 END;
DECLARE @ScheduledType varchar(20) = (
    SELECT TOP 1 s.DayType FROM ShiftScheduleDays s WHERE s.UserId = @UserId AND CAST(s.Date AS date) = @Date);
DECLARE @HasCalendarYear bit = CASE WHEN EXISTS (SELECT 1 FROM CalendarDays WHERE Year = YEAR(@Date)) THEN 1 ELSE 0 END;
DECLARE @CalendarHoliday bit = CASE
    WHEN @HasCalendarYear = 1 THEN CASE WHEN EXISTS (
            SELECT 1 FROM CalendarDays c WHERE CAST(c.Date AS date) = @Date AND c.IsHoliday = 1) THEN 1 ELSE 0 END
    WHEN DATEPART(weekday, @Date) IN (1, 7) THEN 1
    ELSE 0 END;
DECLARE @DayType varchar(20) = CASE
    WHEN @Flexible = 1 AND @IsPublicHoliday = 1 THEN 'public_holiday'
    WHEN @Flexible = 1                          THEN ISNULL(@ScheduledType, 'work')
    WHEN @IsShiftWorker = 1                     THEN 'work'
    WHEN @CalendarHoliday = 1                   THEN 'rest_day'
    ELSE 'work' END;

-- 結算時數（OvertimeSettlement.ComputeSettled：捨去到 0.1 小時，休假日不扣正常時段）
DECLARE @PunchHours decimal(5,1) = FLOOR(DATEDIFF(minute, @Start, @End) / 6.0) / 10.0;
DECLARE @Settled    decimal(5,1) = CASE WHEN @Estimated < @PunchHours THEN @Estimated ELSE @PunchHours END;

-- 補休 lot 加權倍率（休假日級距：1–2h ×1.34、3–8h ×1.67、9h 起 ×2.67）
DECLARE @T1 decimal(9,4) = CASE WHEN @Settled < 2 THEN @Settled ELSE 2 END;
DECLARE @T2 decimal(9,4) = CASE WHEN @Settled <= 2 THEN 0 WHEN @Settled < 8 THEN @Settled - 2 ELSE 6 END;
DECLARE @T3 decimal(9,4) = CASE WHEN @Settled > 8 THEN @Settled - 8 ELSE 0 END;
DECLARE @Rate decimal(5,2) = CASE WHEN @Settled > 0
    THEN ROUND((@T1 * 1.34 + @T2 * 1.67 + @T3 * 2.67) / @Settled, 2) END;
DECLARE @ExpiresAt datetime2(0) = CASE WHEN MONTH(@Date) <= 6
    THEN DATETIMEFROMPARTS(YEAR(@Date),     7, 31, 23, 59, 59, 0)
    ELSE DATETIMEFROMPARTS(YEAR(@Date) + 1, 1, 31, 23, 59, 59, 0) END;
DECLARE @WantLot bit = CASE WHEN @Flexible = 1 AND ISNULL(@CompType, 'compensatory') <> 'pay' AND @Settled > 0 THEN 1 ELSE 0 END;

-- ---------------------------------------------------------------------------
-- B) 異動前報表
-- ---------------------------------------------------------------------------
PRINT N'=== 目標員工 ===';
SELECT u.Id, u.Name, u.Email, d.Name AS [部門], u.IsShiftWorker
FROM @Users x JOIN Users u ON u.Id = x.Id LEFT JOIN Departments d ON d.Id = u.DepartmentId;

PRINT N'=== 當日全部加班單 ===';
SELECT o.Id, o.RequestNo, o.ApprovalStatus, o.RequestKind, o.CompensationType, o.EstimatedHours, o.SettledHours
FROM OvertimeRequests o WHERE o.EmployeeId = @UserId AND CAST(o.OvertimeDate AS date) = @Date;

PRINT N'=== 日別 / 結算 ===';
SELECT @SwitchDate AS [切換日], @Flexible AS [新制], @ScheduledType AS [個人排班], @IsPublicHoliday AS [國定假日],
       @DayType AS [判定日別], @Estimated AS [核准時數], @PunchHours AS [起訖時數], @Settled AS [結算時數],
       @Rate AS [lot倍率], @ExpiresAt AS [lot到期], @WantLot AS [開/更新 lot];

PRINT N'=== 當日出缺勤（異動前）===';
SELECT a.Id, a.ClockInTime, a.ClockOutTime, a.OvertimeStartTime, a.OvertimeEndTime, a.OvertimeRequestId
FROM AttendanceRecords a WHERE a.UserId = @UserId AND CAST(a.RecordDate AS date) = @Date;

PRINT N'=== 該單既有補休 lot ===';
SELECT l.Id, l.Hours, l.RemainingHours, l.RateSnapshot, l.ExpiresAt,
       (SELECT SUM(cu.Hours) FROM CompensatoryUsages cu WHERE cu.LotId = l.Id) AS [已扣抵]
FROM CompensatoryLots l WHERE l.SourceOvertimeRequestId = @OtId;

-- ---------------------------------------------------------------------------
-- C) 保護閘門
-- ---------------------------------------------------------------------------
IF (SELECT COUNT(*) FROM @Users) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'目標員工查無或同名不只一位在職者，已中止（同名請改填 @Email）。', 16, 1);
    RETURN;
END

IF (SELECT COUNT(*) FROM @Ots) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'該日「已核准、新制」的加班單不是恰好一張（見報表「當日全部加班單」），已中止。若查無加班單請改用 Scripts/22。', 16, 1);
    RETURN;
END

IF @End <= @Start
   OR CAST(@Start AS date) <> @Date
   OR @End >= DATEADD(hour, @CrossDayCutoffHour, CAST(DATEADD(day, 1, @Date) AS datetime2(0)))
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'起訖不合理（訖須晚於起、起須在加班日當天、訖須早於隔日 06:00），已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM AttendanceRecords a
           WHERE a.UserId = @UserId AND CAST(a.RecordDate AS date) = @Date
             AND (a.OvertimeStartTime IS NOT NULL OR a.OvertimeEndTime IS NOT NULL))
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'當日出缺勤已有加班打卡，請改由出缺勤報表編輯（系統會自動結算），已中止。', 16, 1);
    RETURN;
END

IF @DayType <> 'rest_day'
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'判定日別不是休假日，本腳本不適用，已中止；請回報日別再決定處理方式。', 16, 1);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- D) 寫入
-- ---------------------------------------------------------------------------
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

INSERT INTO AttendanceAuditLogs (
    AttendanceRecordId, OwnerUserId, RecordDate, ModifiedById, ModifiedByName, ModifiedAt,
    ClockInBefore, ClockInAfter, ClockOutBefore, ClockOutAfter,
    OvertimeStartBefore, OvertimeStartAfter, OvertimeEndBefore, OvertimeEndAfter, RemarkBefore, RemarkAfter)
SELECT a.Id, a.UserId, a.RecordDate, @SuperAdminId, N'系統（補登加班打卡 ' + ISNULL(@RequestNo, N'') + N'）', @Now,
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

UPDATE OvertimeRequests SET SettledHours = @Settled WHERE Id = @OtId;

IF @WantLot = 1
BEGIN
    IF EXISTS (SELECT 1 FROM CompensatoryLots WHERE SourceOvertimeRequestId = @OtId)
        UPDATE l
        SET Hours          = @Settled,
            RemainingHours = CASE WHEN @Settled - ISNULL(u.Used, 0) > 0 THEN @Settled - ISNULL(u.Used, 0) ELSE 0 END,
            RateSnapshot   = @Rate,
            EarnedDate     = CAST(@Date AS datetime2(0)),
            ExpiresAt      = @ExpiresAt
        FROM CompensatoryLots l
        OUTER APPLY (SELECT SUM(cu.Hours) AS Used FROM CompensatoryUsages cu WHERE cu.LotId = l.Id) u
        WHERE l.SourceOvertimeRequestId = @OtId;
    ELSE
        INSERT INTO CompensatoryLots (
            UserId, SourceOvertimeRequestId, EarnedDate, Hours, RemainingHours, RateSnapshot,
            ExpiresAt, IsOpening, CreatedAt)
        VALUES (
            @UserId, @OtId, CAST(@Date AS datetime2(0)), @Settled, @Settled, @Rate,
            @ExpiresAt, 0, @Now);
END

-- ---------------------------------------------------------------------------
-- E) 寫入後報表
-- ---------------------------------------------------------------------------
PRINT N'=== 寫入後：加班單 ===';
SELECT o.Id, o.RequestNo, o.CompensationType, o.ApprovalStatus, o.EstimatedHours, o.SettledHours
FROM OvertimeRequests o WHERE o.Id = @OtId;

PRINT N'=== 寫入後：出缺勤 ===';
SELECT a.Id, a.ClockInTime, a.ClockOutTime, a.OvertimeStartTime, a.OvertimeEndTime, a.OvertimeRequestId, a.IsManuallyAdjusted
FROM AttendanceRecords a WHERE a.Id = @AttId;

PRINT N'=== 寫入後：補休 lot（舊制或加班費單為空）===';
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
