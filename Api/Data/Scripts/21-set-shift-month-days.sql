/*
================================================================================
 指定員工的某月個人排班整批補登例假 / 休假（首例：鄭宇婷 2026-11）
================================================================================

 背景
 ----
 該員錯過 10/25 截止、11 月從未排定（ShiftScheduleMonths 無列）；改班申請 SC-20261027-001
 已於 10/28 退回未重送，正式站切換日仍為 null 故 26 號自動排班亦未執行。
 Script 20 只補了 11/27 一天休假，總覽表因此仍為「未排滿」（例假 0/4、休假 1/4）。
 本案由管理端交辦直接補登，以腳本寫入。

 寫入範圍
 --------
   ShiftScheduleDays：@Days 每一列 (UserId, Date, DayType)
     · 該日無紀錄 → INSERT
     · 該日已是相同日別 → 跳過（腳本可安全重跑）
     · 該日已有**不同**日別 → 中止（不覆寫既有紀錄）
   ShiftScheduleMonths：該月無列 → INSERT committed（比照 ShiftScheduleHandler 存檔通過時的寫法）；
     已有列 → 不動（Script 20 的理由：改 Status 會誤觸 20 號提醒 / 26 號自動排班）
   既有但不在 @Days 的紀錄（例：Script 20 補的 11/27 休假）一律保留。
   CreatedAt / UpdatedAt / CommittedAt 寫 Clock.Now（台北時間 naive）。

 保護閘門（任一命中即整份中止）
 ------------------------------
   ① 姓名查無 / 同名不只一位在職者（同名時改用 @Email 定位）
   ② @Days 有日期不在 @Year/@Month 內，或 DayType 不是 statutory_off / rest_day
   ③ 某日為國定假日 / 彈性休假日（國定假日不入表）
   ④ 某日該員是活動日預定人力（活動日優先，鎖為上班日）
   ⑤ 某日該員有簽核中 / 已核准的假單
   ⑥ 該員該月有 **pending** 的改班申請（核准時會覆寫，結果難預期）
   ⑦ 某日已有不同日別的紀錄

 刻意不擋、只列報表的
 --------------------
   · **returned** 的改班申請：不會自動生效，但請告知本人刪除，避免日後重送覆寫本次結果
   · 關卡 C（任 28 天休息日 ≥ 8）：腳本不跑 ShiftScheduleValidator，只在報表 F 列出
     「當月起 ～ 次月已定案範圍」內休息日不足 8 天的 28 天區間，供判斷是否要調整日期

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。

 空跑要確認：
   ① 「目標員工」只有 1 列，且是本人（看部門 / Email）
   ② 「異動後」例假 / 休假天數與交辦一致（本案：例假 5、休假 3 ＋ 既有 11/27 ＝ 4）
   ③ 報表 F 是否有休息日不足的區間（有的話先回報再決定要不要 COMMIT）
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit   bit            = 0;               -- ← 空跑用 0；確認後改成 1 才會真正寫入

DECLARE @Name     nvarchar(100)  = N'鄭宇婷';
DECLARE @Email    nvarchar(256)  = NULL;            -- 同名時填 Email 定位（優先於姓名）
DECLARE @Year     int            = 2026;
DECLARE @Month    int            = 11;

DECLARE @Days TABLE (Date date PRIMARY KEY, DayType varchar(20) NOT NULL);
INSERT INTO @Days (Date, DayType) VALUES
    ('2026-11-06', 'statutory_off'),
    ('2026-11-11', 'statutory_off'),
    ('2026-11-20', 'statutory_off'),
    ('2026-11-22', 'statutory_off'),
    ('2026-11-29', 'statutory_off'),
    ('2026-11-01', 'rest_day'),
    ('2026-11-21', 'rest_day'),
    ('2026-11-28', 'rest_day');

DECLARE @MonthStart date = DATEFROMPARTS(@Year, @Month, 1);
DECLARE @MonthEnd   date = EOMONTH(@MonthStart);
DECLARE @Now datetime2 = CONVERT(datetime2, SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time');

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- A) 解析目標員工
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

-- ---------------------------------------------------------------------------
-- B) 異動前報表
-- ---------------------------------------------------------------------------
PRINT N'=== 目標員工 ===';
SELECT u.Id, u.Name, u.Email, d.Name AS [部門], u.IsShiftWorker, u.Status
FROM @Users x
JOIN Users u ON u.Id = x.Id
LEFT JOIN Departments d ON d.Id = u.DepartmentId;

PRINT N'=== 該月班表狀態 ===';
SELECT m.Year, m.Month, m.Status, m.CommittedAt, m.AutoAssignedAt
FROM ShiftScheduleMonths m
WHERE m.UserId = @UserId AND m.Year = @Year AND m.Month = @Month;

PRINT N'=== 該月班表（異動前；未列出者為上班日）===';
SELECT CONVERT(varchar(10), s.Date, 23) AS [日期], s.DayType
FROM ShiftScheduleDays s
WHERE s.UserId = @UserId AND s.Date BETWEEN @MonthStart AND @MonthEnd
ORDER BY s.Date;

PRINT N'=== 交辦日期逐日檢查 ===';
SELECT CONVERT(varchar(10), x.Date, 23)                         AS [日期],
       x.DayType                                                AS [交辦日別],
       s.DayType                                                AS [既有日別],
       c.Description                                            AS [國定假日],
       (SELECT TOP 1 a.Title FROM ActivityDays a
          JOIN ActivityDayAssignees aa ON aa.ActivityDayId = a.Id
         WHERE aa.UserId = @UserId AND CAST(a.Date AS date) = x.Date) AS [活動日],
       (SELECT TOP 1 l.LeaveType + N' ' + l.ApprovalStatus FROM LeaveRequests l
         WHERE l.EmployeeId = @UserId AND l.ApprovalStatus IN ('pending', 'approved')
           AND CAST(l.StartDate AS date) <= x.Date AND CAST(l.EndDate AS date) >= x.Date) AS [假單]
FROM @Days x
LEFT JOIN ShiftScheduleDays s ON s.UserId = @UserId AND s.Date = x.Date
LEFT JOIN CalendarDays c      ON CAST(c.Date AS date) = x.Date
                             AND c.IsHoliday = 1 AND ISNULL(c.Description, N'') <> N''
ORDER BY x.Date;

PRINT N'=== 該月改班申請 ===';
SELECT r.Id, r.RequestNo, r.ApprovalStatus, r.ReviewedAt, r.ReviewNote
FROM ShiftChangeRequests r
WHERE r.EmployeeId = @UserId AND r.Year = @Year AND r.Month = @Month;

-- ---------------------------------------------------------------------------
-- C) 保護閘門（全部排在報表之後，空跑一次就能看到所有問題）
-- ---------------------------------------------------------------------------
IF (SELECT COUNT(*) FROM @Users) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'目標員工查無或同名不只一位在職者，已中止（同名請改填 @Email）。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM @Days
           WHERE Date NOT BETWEEN @MonthStart AND @MonthEnd
              OR DayType NOT IN ('statutory_off', 'rest_day'))
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'@Days 有日期不在該月內，或日別不是 statutory_off / rest_day，已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM @Days x
           JOIN CalendarDays c ON CAST(c.Date AS date) = x.Date
           WHERE c.IsHoliday = 1 AND ISNULL(c.Description, N'') <> N'')
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有日期為國定假日 / 彈性休假日，不寫入個人排班，已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM @Days x
           JOIN ActivityDays a ON CAST(a.Date AS date) = x.Date
           JOIN ActivityDayAssignees aa ON aa.ActivityDayId = a.Id
           WHERE aa.UserId = @UserId)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有日期該員是活動日預定人力（活動日優先、鎖為上班日），已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM @Days x
           JOIN LeaveRequests l ON l.EmployeeId = @UserId
                               AND l.ApprovalStatus IN ('pending', 'approved')
                               AND CAST(l.StartDate AS date) <= x.Date
                               AND CAST(l.EndDate AS date)   >= x.Date)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有日期該員有簽核中 / 已核准的假單，已中止（請先處理假單）。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM ShiftChangeRequests
           WHERE EmployeeId = @UserId AND Year = @Year AND Month = @Month
             AND ApprovalStatus = 'pending')
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'該員該月有簽核中的改班申請，核准時會覆寫本次結果，已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM @Days x
           JOIN ShiftScheduleDays s ON s.UserId = @UserId AND s.Date = x.Date
           WHERE s.DayType <> x.DayType)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有日期已有不同日別的紀錄，腳本不覆寫，已中止。', 16, 1);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- D) 寫入
-- ---------------------------------------------------------------------------
INSERT INTO ShiftScheduleDays (UserId, Date, DayType, CreatedAt, UpdatedAt)
SELECT @UserId, x.Date, x.DayType, @Now, @Now
FROM @Days x
WHERE NOT EXISTS (SELECT 1 FROM ShiftScheduleDays s
                  WHERE s.UserId = @UserId AND s.Date = x.Date);
PRINT N'ShiftScheduleDays 新增列數：' + CAST(@@ROWCOUNT AS nvarchar(10));

IF NOT EXISTS (SELECT 1 FROM ShiftScheduleMonths
               WHERE UserId = @UserId AND Year = @Year AND Month = @Month)
BEGIN
    INSERT INTO ShiftScheduleMonths (UserId, Year, Month, Status, CommittedAt, AutoAssignedAt, CreatedAt, UpdatedAt)
    VALUES (@UserId, @Year, @Month, 'committed', @Now, NULL, @Now, @Now);
    PRINT N'ShiftScheduleMonths 已新增 committed。';
END
ELSE
    PRINT N'ShiftScheduleMonths 已有該月狀態列，不動。';

-- ---------------------------------------------------------------------------
-- E) 異動後驗證
-- ---------------------------------------------------------------------------
PRINT N'=== 該月班表（異動後）===';
SELECT CONVERT(varchar(10), s.Date, 23) AS [日期], s.DayType
FROM ShiftScheduleDays s
WHERE s.UserId = @UserId AND s.Date BETWEEN @MonthStart AND @MonthEnd
ORDER BY s.Date;

SELECT SUM(CASE WHEN DayType = 'statutory_off' THEN 1 ELSE 0 END) AS [例假天數],
       SUM(CASE WHEN DayType = 'rest_day'      THEN 1 ELSE 0 END) AS [休假天數]
FROM ShiftScheduleDays
WHERE UserId = @UserId AND Date BETWEEN @MonthStart AND @MonthEnd;

SELECT m.Year, m.Month, m.Status, m.CommittedAt
FROM ShiftScheduleMonths m
WHERE m.UserId = @UserId AND m.Year = @Year AND m.Month = @Month;

-- ---------------------------------------------------------------------------
-- F) 關卡 C 參考：任 28 天休息日（例＋休＋國定假日）不足 8 天的區間
--    範圍＝當月 ～ 次月（次月已定案才納入；未定案則只看完整落在當月內的區間，而 30 / 31 天的月份恰好沒有）
-- ---------------------------------------------------------------------------
DECLARE @NextStart date = DATEADD(MONTH, 1, @MonthStart);
DECLARE @RangeEnd  date = CASE WHEN EXISTS (SELECT 1 FROM ShiftScheduleMonths
                                            WHERE UserId = @UserId
                                              AND Year = YEAR(@NextStart) AND Month = MONTH(@NextStart)
                                              AND Status IN ('committed', 'auto'))
                               THEN EOMONTH(@NextStart) ELSE @MonthEnd END;

DECLARE @Timeline TABLE (Date date PRIMARY KEY, IsOff bit NOT NULL);
DECLARE @d date = @MonthStart;
WHILE @d <= @RangeEnd
BEGIN
    INSERT INTO @Timeline (Date, IsOff)
    SELECT @d, CASE WHEN EXISTS (SELECT 1 FROM ShiftScheduleDays
                                 WHERE UserId = @UserId AND Date = @d
                                   AND DayType IN ('statutory_off', 'rest_day'))
                      OR EXISTS (SELECT 1 FROM CalendarDays
                                 WHERE CAST(Date AS date) = @d AND IsHoliday = 1
                                   AND ISNULL(Description, N'') <> N'')
                    THEN 1 ELSE 0 END;
    SET @d = DATEADD(DAY, 1, @d);
END

PRINT N'=== 關卡 C 參考：休息日不足 8 天的 28 天區間（無資料＝通過）===';
SELECT CONVERT(varchar(10), t.Date, 23)                    AS [起],
       CONVERT(varchar(10), DATEADD(DAY, 27, t.Date), 23)  AS [迄],
       (SELECT SUM(CAST(w.IsOff AS int)) FROM @Timeline w
         WHERE w.Date BETWEEN t.Date AND DATEADD(DAY, 27, t.Date)) AS [休息日]
FROM @Timeline t
WHERE DATEADD(DAY, 27, t.Date) <= @RangeEnd
  AND (SELECT SUM(CAST(w.IsOff AS int)) FROM @Timeline w
        WHERE w.Date BETWEEN t.Date AND DATEADD(DAY, 27, t.Date)) < 8
ORDER BY t.Date;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'>>> 已寫入（COMMIT）';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'>>> 空跑結束（ROLLBACK），確認無誤後把 @Commit 改成 1 再執行';
END
