/*
================================================================================
 指定員工的個人排班加一天「休假日」（首例：鄭宇婷 2026-11-27）
================================================================================

 背景
 ----
 11 月班表開放期已於 10/25 截止（ShiftScheduleWindow：前一個月 25 日 23:59），
 10/26 起同仁只能走〈改班申請〉。本案由管理端交辦直接補登，故以腳本寫入。

 只動一張表、一列
 ----------------
   ShiftScheduleDays：(UserId, @Date) 一列，DayType = 'rest_day'
     · 該日無紀錄（＝上班日，上班日不落地）→ INSERT
     · 該日已有紀錄（例假日 statutory_off）→ 中止，例假不可直接改成休假（例假 ≥4 是擋存條件）
     · 該日已是休假 → 跳過（腳本可安全重跑）
   CreatedAt / UpdatedAt 比照 ShiftScheduleHandler 寫 Clock.Now（台北時間 naive）。

 刻意不動的
 ----------
   · ShiftScheduleMonths.Status：不改 draft / committed，改了會誤觸 20 號提醒或 26 號自動排班
   · 不另挑一天換回上班：例＋休合計會比應排天數多 1 天（validator 對「多排」不擋、也不警示），
     若要維持合計不變，請另外交辦哪一天改回上班

 保護閘門（任一命中即整份中止）
 ------------------------------
   ① 姓名查無 / 同名不只一位在職者（同名時改用 @Email 定位）
   ② @Date 是國定假日 / 彈性休假日（IsHoliday=1 且 Description 非空）—— 國定假日不入表
   ③ 該員當天是活動日預定人力（活動日優先，鎖為上班日）
   ④ 該員當天有簽核中 / 已核准的假單（排班月曆鎖為上班日；要休應先處理假單）
   ⑤ 該員有涵蓋當天、進行中（pending / returned）的改班申請（核准時會覆寫，結果難預期）
   ⑥ 當天已是例假日

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。

 空跑要確認三件事：
   ① 「目標員工」只有 1 列，且是本人（看部門 / Email）
   ② 「11 月班表（異動前）」中 11/27 不在清單內（＝上班日）
   ③ 「異動後」該月休假天數 +1、11/27 為休假日
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit   bit            = 0;               -- ← 空跑用 0；確認後改成 1 才會真正寫入

DECLARE @Name     nvarchar(100)  = N'鄭宇婷';
DECLARE @Email    nvarchar(256)  = NULL;            -- 同名時填 Email 定位（優先於姓名）
DECLARE @Date     date           = '2026-11-27';

DECLARE @MonthStart date = DATEFROMPARTS(YEAR(@Date), MONTH(@Date), 1);
DECLARE @MonthEnd   date = EOMONTH(@Date);
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
WHERE m.UserId = @UserId AND m.Year = YEAR(@Date) AND m.Month = MONTH(@Date);

PRINT N'=== 該月班表（異動前；未列出者為上班日）===';
SELECT CONVERT(varchar(10), s.Date, 23) AS [日期], s.DayType
FROM ShiftScheduleDays s
WHERE s.UserId = @UserId AND s.Date BETWEEN @MonthStart AND @MonthEnd
ORDER BY s.Date;

SELECT SUM(CASE WHEN DayType = 'statutory_off' THEN 1 ELSE 0 END) AS [例假天數],
       SUM(CASE WHEN DayType = 'rest_day'      THEN 1 ELSE 0 END) AS [休假天數]
FROM ShiftScheduleDays
WHERE UserId = @UserId AND Date BETWEEN @MonthStart AND @MonthEnd;

PRINT N'=== 當天行事曆 / 活動日 / 假單 / 改班申請 ===';
SELECT CONVERT(varchar(10), c.Date, 23) AS [日期], c.IsHoliday, c.Description
FROM CalendarDays c WHERE CAST(c.Date AS date) = @Date;

SELECT a.Id, a.Title, CONVERT(varchar(10), a.Date, 23) AS [活動日]
FROM ActivityDays a
JOIN ActivityDayAssignees x ON x.ActivityDayId = a.Id
WHERE x.UserId = @UserId AND CAST(a.Date AS date) = @Date;

SELECT l.Id, l.RequestNo, l.LeaveType, l.StartDate, l.EndDate, l.ApprovalStatus
FROM LeaveRequests l
WHERE l.EmployeeId = @UserId
  AND l.ApprovalStatus IN ('pending', 'approved')
  AND CAST(l.StartDate AS date) <= @Date AND CAST(l.EndDate AS date) >= @Date;

SELECT r.Id, r.RequestNo, r.ApprovalStatus, rd.FromDayType, rd.ToDayType
FROM ShiftChangeRequests r
JOIN ShiftChangeRequestDates rd ON rd.ShiftChangeRequestId = r.Id
WHERE r.EmployeeId = @UserId
  AND r.ApprovalStatus IN ('pending', 'returned')
  AND CAST(rd.Date AS date) = @Date;

-- ---------------------------------------------------------------------------
-- C) 保護閘門（全部排在報表之後，空跑一次就能看到所有問題）
-- ---------------------------------------------------------------------------
IF (SELECT COUNT(*) FROM @Users) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'目標員工查無或同名不只一位在職者，已中止（同名請改填 @Email）。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM CalendarDays
           WHERE CAST(Date AS date) = @Date AND IsHoliday = 1 AND ISNULL(Description, N'') <> N'')
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'該日為國定假日 / 彈性休假日，不寫入個人排班，已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM ActivityDays a
           JOIN ActivityDayAssignees x ON x.ActivityDayId = a.Id
           WHERE x.UserId = @UserId AND CAST(a.Date AS date) = @Date)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'該員當天是活動日預定人力（活動日優先、鎖為上班日），已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM LeaveRequests l
           WHERE l.EmployeeId = @UserId AND l.ApprovalStatus IN ('pending', 'approved')
             AND CAST(l.StartDate AS date) <= @Date AND CAST(l.EndDate AS date) >= @Date)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'該員當天有簽核中 / 已核准的假單，已中止（請先處理假單）。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM ShiftChangeRequests r
           JOIN ShiftChangeRequestDates rd ON rd.ShiftChangeRequestId = r.Id
           WHERE r.EmployeeId = @UserId AND r.ApprovalStatus IN ('pending', 'returned')
             AND CAST(rd.Date AS date) = @Date)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'該員有涵蓋當天、進行中的改班申請，已中止。', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM ShiftScheduleDays
           WHERE UserId = @UserId AND Date = @Date AND DayType = 'statutory_off')
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'該日已是例假日，例假不可直接改成休假，已中止。', 16, 1);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- D) 寫入休假日
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM ShiftScheduleDays
           WHERE UserId = @UserId AND Date = @Date AND DayType = 'rest_day')
BEGIN
    PRINT N'該日已是休假日，略過（腳本可重跑）。';
END
ELSE
BEGIN
    INSERT INTO ShiftScheduleDays (UserId, Date, DayType, CreatedAt, UpdatedAt)
    VALUES (@UserId, @Date, 'rest_day', @Now, @Now);
    PRINT N'已新增列數：' + CAST(@@ROWCOUNT AS nvarchar(10));
END

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
