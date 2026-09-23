/*
================================================================================
 行事曆：把「補假」改名為「彈性休假日」
================================================================================

 背景
 ----
 公司行事曆（CalendarDays）的資料來自外部來源 ruyut/TaiwanCalendar，
 因國定假日產生的補假，其 Description 逐字就是「補假」。
 公司決定改稱「彈性休假日」：那幾天仍是放假日（IsHoliday = 1，不用上班、
 不用打卡、不算缺勤），但**要休得自己請假**，故請假日計算不再把它們扣掉
 （見 Api/Common/WorkCalendarHelper.cs 的 CalendarScope）。

 為什麼需要這支腳本
 ------------------
 程式端的改名落在 CalendarDayHandler.MapDescription（匯入時映射），
 只對「**之後**才匯入的年度」生效。DB 裡已匯入年度的舊資料仍是「補假」，
 需要這支一次性回填。

 ⚠ 反過來不成立：**不能只跑這支腳本而不改程式**。ImportYearAsync 是
   「整年 RemoveRange 後重建」，只要有人再按一次「匯入 {年} 年」，
   手改過的名稱就會整批被沖回「補假」。

 範圍
 ----
 Description = N'補假' 的所有列，不分年度。

   · 「調整放假」**刻意不動**（2026-09-23 業務決議：只改「補假」）。
   · 「補行上班」是 IsHoliday = 0 的補班日，與本次無關。
   · 週六日的 Description 為空字串，不受影響。

 冪等
 ----
 以 Description = N'補假' 為條件，跑第二次時命中 0 列。可安全重跑。

 執行方式
 --------
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。
 staging 與正式站**各跑一次**。

================================================================================
*/

SET NOCOUNT ON;

DECLARE @Commit bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入

DECLARE @SourceName nvarchar(100) = N'補假';        -- 外部行事曆原文
DECLARE @TargetName nvarchar(100) = N'彈性休假日';  -- 本公司用語
-- ⚠ 兩者的單一真相為 Api/Common/Constants.cs 的 CalendarDescriptions，改字要一起改

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- A) 異動前：列出將被改名的日子（請人工核對年度與筆數）
-- ---------------------------------------------------------------------------
PRINT N'=== 將被改名的日子 ===';
SELECT  c.Year,
        CONVERT(varchar(10), c.Date, 23) AS [日期],
        DATENAME(weekday, c.Date)        AS [星期],
        c.IsHoliday                      AS [是否放假],
        c.Description                    AS [現在的說明]
FROM CalendarDays c
WHERE c.Description = @SourceName
ORDER BY c.Date;

PRINT N'=== 各年度筆數 ===';
SELECT  c.Year, COUNT(*) AS [補假天數]
FROM CalendarDays c
WHERE c.Description = @SourceName
GROUP BY c.Year
ORDER BY c.Year;

-- 參考用：已經是新名稱的（重跑時會在這裡看到上次的成果）
PRINT N'=== 已經是「彈性休假日」的（重跑時應等於上次的異動筆數）===';
SELECT  c.Year, COUNT(*) AS [已改名天數]
FROM CalendarDays c
WHERE c.Description = @TargetName
GROUP BY c.Year
ORDER BY c.Year;

-- ---------------------------------------------------------------------------
-- B) 改名
-- ---------------------------------------------------------------------------
UPDATE CalendarDays
SET    Description = @TargetName
WHERE  Description = @SourceName;
DECLARE @Changed int = @@ROWCOUNT;

-- ---------------------------------------------------------------------------
-- C) 異動後驗證
-- ---------------------------------------------------------------------------
PRINT N'=== 異動後殘留檢查（應全部為 0）===';
SELECT
    (SELECT COUNT(*) FROM CalendarDays WHERE Description = @SourceName)                    AS [仍叫補假],
    (SELECT COUNT(*) FROM CalendarDays WHERE Description = @TargetName AND IsHoliday = 0)  AS [彈性休假日卻非放假];

PRINT N'=== 結果 ===';
SELECT  @Changed AS [本次異動筆數],
        (SELECT COUNT(*) FROM CalendarDays WHERE Description = @TargetName) AS [目前彈性休假日總數];

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'=== @Commit = 1 → 已 COMMIT，名稱已變更 ===';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'=== @Commit = 0 → 已 ROLLBACK（空跑，未寫入任何變更）===';
END
