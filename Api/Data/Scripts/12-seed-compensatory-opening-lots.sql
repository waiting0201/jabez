/*
================================================================================
 四週彈性工時切換：把現行補休餘額整批做成「期初 lot」
================================================================================

 背景
 ----
 切換前，補休池是三個聚合相減（LeaveRequestHandler.ComputeCompensatoryAsync）：

     期初 User.CompensatoryOpeningHours
   ＋ 已核准且 CompensationType = 'compensatory' 的加班單 SUM(EstimatedHours)
   −  已送出（pending / approved）補休假的 SUM(Hours)

 FIFO 只是 Math.Min(used, opening) 的算術模擬，沒有到期日、沒有加班單↔補休單對應。

 切換後改為逐筆 lot（CompensatoryLots / CompensatoryUsages）：
 每筆加班選「換取補休」時開一個 lot，帶原始加班費率快照與到期日。

 為什麼需要這支腳本
 ------------------
 CompensatoryLotService.ApplyAsync 只對「**加班日 >= 切換日**」的加班單開 lot
 （判準是資料自己的日期，不是今天）。切換日之前累積的那批餘額因此完全沒有 lot ——
 不補的話，全公司的補休餘額在切換當天會直接歸零。

 依 2026-09-22 決議，切換當下的剩餘時數**整批做成一筆期初 lot**（IsOpening = 1），
 而不是逐張加班單回填。理由：
   · 舊資料沒有「原始費率」可言（費率快照是新制才有的東西），逐張回填只能事後推算，
     而事後推算拿到的是**現行**級距，本來就不是當時的費率 —— 精度是假的。
   · 期初 lot 沿用現行 User.CompensatoryOpeningHours 的既有語意與到期日，
     使用者看到的「舊補休剩餘」不會因為換了資料結構而變動。

 範圍與規則
 ----------
 · 對象：所有在職（Status = 'active'）非 Superadmin 且**餘額 > 0** 的員工。
 · 時數：以上面那條聚合公式算出的「合計可用」為準，負數與 0 一律跳過。
 · 到期日：沿用程式常數 LeaveRequestHandler.CompensatoryOpeningExpiry = 2027-06-30 23:59:59。
   ⚠ 若該常數日後有變，本腳本的 @OpeningExpiry 要一起改。
 · 費率快照：期初 lot **刻意留 NULL** —— 無從得知當時費率。
   薪資端的到期結算 SQL 以 ISNULL(RateSnapshot, 0) 計算，
   故期初 lot 到期時**不會**自動換成津貼，交人工處理（見下方「已知取捨」）。
 · 冪等：已有期初 lot 的員工一律跳過，可安全重跑。

 已知取捨
 --------
 期初 lot 的 RateSnapshot 為 NULL ⇒ 到期未休完時津貼金額算出來是 0。
 這是刻意的：憑空給一個費率等於系統自行決定要發多少錢。
 2027-06-30 屆至前，若客戶要求結算這批舊餘額，請由人事以 PayrollAdjustment
 的「其他加項」逐人補入，或另行決議一個統一費率後 UPDATE 本表。

 執行方式
 --------
 1. 先原樣執行（@Commit = 0）看空跑報表，確認人數與時數合理
 2. 再把 @Commit 改成 1 重跑
 ⚠ 必須在「設定 SystemSetting.FlexibleWorkStartDate」**之前或同時**執行。
   先切換再跑，中間這段時間同仁會看到補休餘額是 0。
================================================================================
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @Commit        bit      = 0;                              -- 改成 1 才會真的寫入
DECLARE @OpeningExpiry datetime2(7) = '2027-06-30T23:59:59';      -- 同 LeaveRequestHandler.CompensatoryOpeningExpiry
DECLARE @Now           datetime2(7) = SYSDATETIME();

BEGIN TRAN;

-- ── 依現行聚合公式算出每人的可用補休 ────────────────────────────────────────
;WITH Opening AS (
    SELECT u.Id AS UserId, u.Name, u.CompensatoryOpeningHours AS OpeningHours
    FROM Users u
    WHERE u.IsSuperAdmin = 0 AND u.Status = 'active'
),
Earned AS (
    -- ⚠ 必須濾 CompensationType = 'compensatory'：選了「加班費」的單已隨次月薪資發放現金，
    --   再進補休池就是同一段工時領兩次。
    -- ⚠ 時數用 EstimatedHours（未截斷）而非 PayableHours —— 現行補休路徑本來就沒有計酬上限，
    --   改用截斷值會把既有餘額追溯砍掉。
    SELECT o.EmployeeId AS UserId, SUM(o.EstimatedHours) AS Hours
    FROM OvertimeRequests o
    WHERE o.ApprovalStatus   = 'approved'
      AND o.CompensationType = 'compensatory'
      AND o.EmployeeId IS NOT NULL
    GROUP BY o.EmployeeId
),
Used AS (
    -- 送出即佔用（pending 也算），與 ComputeCompensatoryAsync 一致
    SELECT l.EmployeeId AS UserId, SUM(l.Hours) AS Hours
    FROM LeaveRequests l
    WHERE l.LeaveType = 'compensatory'
      AND l.ApprovalStatus IN ('approved', 'pending')
      AND l.EmployeeId IS NOT NULL
    GROUP BY l.EmployeeId
),
Balance AS (
    SELECT o.UserId, o.Name,
           o.OpeningHours,
           ISNULL(e.Hours, 0) AS EarnedHours,
           ISNULL(u.Hours, 0) AS UsedHours,
           -- 到期前的公式：期初 + 加班 − 已用（@OpeningExpiry 尚未屆至，故不套到期分支）
           o.OpeningHours + ISNULL(e.Hours, 0) - ISNULL(u.Hours, 0) AS AvailableHours
    FROM Opening o
    LEFT JOIN Earned e ON e.UserId = o.UserId
    LEFT JOIN Used   u ON u.UserId = o.UserId
)
-- ⚠ CTE 的生存範圍只到下一個語句，故必須落成暫存表 —— 下方的「跳過」報表也要用到 Balance
SELECT * INTO #Balance FROM Balance;

SELECT * INTO #Target
FROM #Balance b
WHERE b.AvailableHours > 0
  AND NOT EXISTS (SELECT 1 FROM CompensatoryLots cl
                  WHERE cl.UserId = b.UserId AND cl.IsOpening = 1);   -- 冪等

-- ── 空跑報表 ────────────────────────────────────────────────────────────────
SELECT N'【將建立期初 lot】' AS Section, Name, OpeningHours, EarnedHours, UsedHours, AvailableHours
FROM #Target ORDER BY Name;

SELECT N'合計' AS Section, COUNT(*) AS People, SUM(AvailableHours) AS TotalHours FROM #Target;

-- 參考：餘額 <= 0 或已有期初 lot 而被跳過的人
SELECT N'【跳過】' AS Section, b.Name, b.AvailableHours,
       CASE WHEN EXISTS (SELECT 1 FROM CompensatoryLots cl WHERE cl.UserId = b.UserId AND cl.IsOpening = 1)
            THEN N'已有期初 lot' ELSE N'餘額 <= 0' END AS Reason
FROM #Balance b
WHERE b.UserId NOT IN (SELECT UserId FROM #Target)
ORDER BY b.Name;

-- ── 寫入 ────────────────────────────────────────────────────────────────────
INSERT INTO CompensatoryLots
    (UserId, SourceOvertimeRequestId, EarnedDate, Hours, RemainingHours,
     RateSnapshot, ExpiresAt, IsOpening, CreatedAt)
SELECT UserId,
       NULL,                          -- 期初 lot 無來源加班單
       CAST(@Now AS date),            -- FIFO 排序鍵；期初 lot 一律最早（見下方 UPDATE）
       AvailableHours,
       AvailableHours,
       NULL,                          -- 費率無從得知，刻意留 NULL
       @OpeningExpiry,
       1,
       @Now
FROM #Target;

-- FIFO 必須先扣期初 lot（它才是最早賺得、也最早到期的一批）。
-- EarnedDate 用今天會讓它排到所有新 lot 之後，故統一壓成一個絕對早的日期。
UPDATE CompensatoryLots
SET EarnedDate = '2000-01-01'
WHERE IsOpening = 1 AND EarnedDate <> '2000-01-01';

SELECT N'【寫入結果】' AS Section, COUNT(*) AS Lots, SUM(Hours) AS TotalHours
FROM CompensatoryLots WHERE IsOpening = 1;

DROP TABLE #Target;
DROP TABLE #Balance;

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT N'== 已認可（COMMIT）==';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT N'== 空跑（ROLLBACK），把 @Commit 改成 1 才會真的寫入 ==';
END
