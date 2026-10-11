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
 · 時數：以上面那條聚合公式算出的「合計可用」為準，負數與 0 一律跳過；但兩邊都**只算切換日之前**
   （2026-10-11 修正，見下方「切換日前後的分界」）。
 · 加班時數取給付基準 `ISNULL(SettledHours, EstimatedHours)`（＝ OvertimeSettlement.BillableHours，
   與畫面上的補休餘額同一基準；2026-10 起新單依實際加班打卡結算）。

 切換日前後的分界（2026-10-11 修正）
 ------------------------------------
 新制以「資料自己的日期」決定歸屬：加班看 OvertimeDate、補休假看 StartDate（WorkdayHours.IsFlexible）。
 切換前就先送出的「切換日以後」的單若也算進期初，切換後新制會再算一次 ⇒ 重複：
   · 加班日 ≥ 切換日：**不算進期初**。新制在打加班結束卡結算時（OvertimeSettlementService）
     由 CompensatoryLotService.ApplyAsync 自行開 lot，費率快照也才正確。
   · 補休假起始日 ≥ 切換日（pending / approved）：**不從期初餘額扣**，改由本腳本直接寫
     CompensatoryUsages 扣抵期初 lot（＝ SyncUsageAsync 的 FIFO 結果；此時只有期初 lot 一筆）。
     之後該假單核准／退回／銷假時 SyncUsageAsync 會「先全額歸還再重扣」，與本腳本的結果銜接。
   期初 lot 不足以扣抵者，不足部分不寫扣抵（同 SyncUsageAsync 回傳的不足時數），列在報表交人工。

 ⚠ 本腳本跑完到設定切換日之間若仍有單據異動，會對不上（切換後 lot 制不再回頭看舊聚合）：
   · 切換日前的加班單在切換**後**才核准 → 不會開 lot（加班日 < 切換日），該段補休會消失；
   · 切換日前的補休假在切換**後**被退回／拒絕 → 期初已扣掉、不會還回。
   故報表另列「切換日前、仍在簽核中」的加班單與補休假，請先催簽完再跑；跑完**當場**設定切換日。
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
 ⚠ **緊接在**「設定 SystemSetting.FlexibleWorkStartDate」之前執行，不要提早太久。
   · 先切換再跑 → 中間這段時間同仁看到的補休餘額是 0。
   · 提早太久跑 → 期初 lot 的金額停在跑的那一刻，而**切換日之前核准的補休制加班單不會開 lot**
     （CompensatoryLotService.ApplyAsync 只對「加班日 ≥ 切換日」開），那段期間新增的餘額會**憑空消失**。
   ⚠ 本腳本的冪等是「已有期初 lot 者**跳過**」，**不是「更新為最新餘額」** ——
     所以提早跑之後再重跑**補不回**那段落差，只能人工調整或先刪掉期初 lot 再跑一次。
================================================================================
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @Commit        bit      = 0;                              -- 改成 1 才會真的寫入
DECLARE @SwitchDate    date     = '2026-11-01';                   -- ⚠ 必須等於即將設定的 SystemSetting.FlexibleWorkStartDate
DECLARE @OpeningExpiry datetime2(7) = '2027-06-30T23:59:59';      -- 同 LeaveRequestHandler.CompensatoryOpeningExpiry
DECLARE @Now           datetime2(7) = SYSDATETIME();

-- 切換日已設定且與 @SwitchDate 不同 → 分界會切錯，整份中止
DECLARE @Configured date = (SELECT TOP 1 CAST(FlexibleWorkStartDate AS date) FROM SystemSettings);
IF @Configured IS NOT NULL AND @Configured <> @SwitchDate
BEGIN
    RAISERROR(N'SystemSettings.FlexibleWorkStartDate 與 @SwitchDate 不一致，請先確認切換日', 16, 1);
    RETURN;
END

BEGIN TRAN;

-- ── 依現行聚合公式算出每人的可用補休（只算切換日之前） ──────────────────────
;WITH Opening AS (
    SELECT u.Id AS UserId, u.Name, u.CompensatoryOpeningHours AS OpeningHours
    FROM Users u
    WHERE u.IsSuperAdmin = 0 AND u.Status = 'active'
),
Earned AS (
    -- ⚠ 必須濾 CompensationType = 'compensatory'：選了「加班費」的單已隨次月薪資發放現金，
    --   再進補休池就是同一段工時領兩次。
    -- ⚠ 時數取給付基準 ISNULL(SettledHours, EstimatedHours)（未截斷）而非 PayableHours ——
    --   與 LeaveRequestHandler.ComputeCompensatoryAsync 同基準；補休路徑本來就沒有計酬上限。
    -- ⚠ 加班日 >= 切換日者不算：新制結算時會自行開 lot（見檔頭「切換日前後的分界」）。
    SELECT o.EmployeeId AS UserId, SUM(ISNULL(o.SettledHours, o.EstimatedHours)) AS Hours
    FROM OvertimeRequests o
    WHERE o.ApprovalStatus   = 'approved'
      AND o.CompensationType = 'compensatory'
      AND o.EmployeeId IS NOT NULL
      AND o.OvertimeDate < @SwitchDate
    GROUP BY o.EmployeeId
),
Used AS (
    -- 送出即佔用（pending 也算），與 ComputeCompensatoryAsync 一致
    -- ⚠ 起始日 >= 切換日者不在此扣，改寫 CompensatoryUsages（見下方）
    SELECT l.EmployeeId AS UserId, SUM(l.Hours) AS Hours
    FROM LeaveRequests l
    WHERE l.LeaveType = 'compensatory'
      AND l.ApprovalStatus IN ('approved', 'pending')
      AND l.EmployeeId IS NOT NULL
      AND l.StartDate < @SwitchDate
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

-- ── 切換日以後、已送出的補休假：FIFO 扣抵期初 lot ───────────────────────────
-- 依起始日 → Id 排序累加，前面的單先吃完期初餘額（同 SyncUsageAsync 的處理順序）。
-- 期初 lot 到期日早於假單起始日者不可扣（同 SyncUsageAsync 的 ExpiresAt >= StartDate）。
SELECT l.Id AS LeaveRequestId, l.EmployeeId AS UserId, t.Name, l.StartDate, l.ApprovalStatus,
       l.Hours,
       SUM(l.Hours) OVER (PARTITION BY l.EmployeeId ORDER BY l.StartDate, l.Id
                          ROWS UNBOUNDED PRECEDING) - l.Hours AS Before
INTO #PostLeave
FROM LeaveRequests l
JOIN #Target t ON t.UserId = l.EmployeeId
WHERE l.LeaveType = 'compensatory'
  AND l.ApprovalStatus IN ('approved', 'pending')
  AND l.StartDate >= @SwitchDate
  AND l.StartDate <= @OpeningExpiry
  AND NOT EXISTS (SELECT 1 FROM CompensatoryUsages cu WHERE cu.LeaveRequestId = l.Id);

SELECT p.*,
       CASE WHEN t.AvailableHours - p.Before <= 0 THEN 0
            WHEN t.AvailableHours - p.Before >= p.Hours THEN p.Hours
            ELSE t.AvailableHours - p.Before END AS Take
INTO #Usage
FROM #PostLeave p
JOIN #Target t ON t.UserId = p.UserId;

-- ── 空跑報表 ────────────────────────────────────────────────────────────────
SELECT N'【將建立期初 lot】' AS Section, t.Name, t.OpeningHours, t.EarnedHours, t.UsedHours,
       t.AvailableHours                                         AS 期初時數,
       ISNULL(u.Taken, 0)                                       AS 切換後補休扣抵,
       t.AvailableHours - ISNULL(u.Taken, 0)                    AS 期初剩餘
FROM #Target t
LEFT JOIN (SELECT UserId, SUM(Take) AS Taken FROM #Usage GROUP BY UserId) u ON u.UserId = t.UserId
ORDER BY t.Name;

SELECT N'合計' AS Section, COUNT(*) AS People, SUM(AvailableHours) AS TotalHours FROM #Target;

-- 參考：餘額 <= 0 或已有期初 lot 而被跳過的人
SELECT N'【跳過】' AS Section, b.Name, b.AvailableHours,
       CASE WHEN EXISTS (SELECT 1 FROM CompensatoryLots cl WHERE cl.UserId = b.UserId AND cl.IsOpening = 1)
            THEN N'已有期初 lot' ELSE N'餘額 <= 0' END AS Reason
FROM #Balance b
WHERE b.UserId NOT IN (SELECT UserId FROM #Target)
ORDER BY b.Name;

-- 切換日以後的補休假扣抵明細（Take < Hours ＝ 期初餘額不足，不足部分交人工）
SELECT N'【切換後補休假扣抵】' AS Section, Name, LeaveRequestId, StartDate, ApprovalStatus, Hours, Take,
       CASE WHEN Take < Hours THEN N'⚠ 期初不足 ' + CAST(Hours - Take AS nvarchar(20)) + N' 小時' ELSE N'' END AS 備註
FROM #Usage ORDER BY Name, StartDate;

-- 切換日以後的補休假、但本人沒有期初 lot（餘額 <= 0）—— 切換後核准時 SyncUsageAsync 會配不到 lot
SELECT N'⚠ 【切換後補休假，但無期初餘額】' AS Section, u.Name, l.Id AS LeaveRequestId, l.StartDate, l.ApprovalStatus, l.Hours
FROM LeaveRequests l
JOIN Users u ON u.Id = l.EmployeeId
WHERE l.LeaveType = 'compensatory'
  AND l.ApprovalStatus IN ('approved', 'pending')
  AND l.StartDate >= @SwitchDate
  AND l.EmployeeId NOT IN (SELECT UserId FROM #Target)
  AND NOT EXISTS (SELECT 1 FROM CompensatoryLots cl WHERE cl.UserId = l.EmployeeId AND cl.IsOpening = 1)
ORDER BY u.Name, l.StartDate;

-- 切換日前、仍在簽核中的單：切換後才結案會對不上（見檔頭 ⚠），請先催簽完再跑
SELECT N'⚠ 【切換日前、簽核中的補休制加班單】' AS Section, u.Name, o.Id, o.OvertimeDate, o.ApprovalStatus,
       ISNULL(o.SettledHours, o.EstimatedHours) AS Hours
FROM OvertimeRequests o
JOIN Users u ON u.Id = o.EmployeeId
WHERE o.CompensationType = 'compensatory'
  AND o.ApprovalStatus IN ('pending', 'returned')
  AND o.OvertimeDate < @SwitchDate
ORDER BY u.Name, o.OvertimeDate;

SELECT N'⚠ 【切換日前、簽核中的補休假】' AS Section, u.Name, l.Id, l.StartDate, l.ApprovalStatus, l.Hours
FROM LeaveRequests l
JOIN Users u ON u.Id = l.EmployeeId
WHERE l.LeaveType = 'compensatory'
  AND l.ApprovalStatus IN ('pending', 'returned')
  AND l.StartDate < @SwitchDate
ORDER BY u.Name, l.StartDate;

-- ── 寫入 ────────────────────────────────────────────────────────────────────
DECLARE @NewLots TABLE (LotId int, UserId uniqueidentifier);

INSERT INTO CompensatoryLots
    (UserId, SourceOvertimeRequestId, EarnedDate, Hours, RemainingHours,
     RateSnapshot, ExpiresAt, IsOpening, CreatedAt)
OUTPUT inserted.Id, inserted.UserId INTO @NewLots (LotId, UserId)
SELECT t.UserId,
       NULL,                          -- 期初 lot 無來源加班單
       '2000-01-01',                  -- FIFO 排序鍵：期初 lot 一律最早（它才是最早賺得、也最早到期的一批）
       t.AvailableHours,
       t.AvailableHours - ISNULL((SELECT SUM(x.Take) FROM #Usage x WHERE x.UserId = t.UserId), 0),
       NULL,                          -- 費率無從得知，刻意留 NULL
       @OpeningExpiry,
       1,
       @Now
FROM #Target t;

INSERT INTO CompensatoryUsages (LotId, LeaveRequestId, Hours, CreatedAt)
SELECT n.LotId, x.LeaveRequestId, x.Take, @Now
FROM #Usage x
JOIN @NewLots n ON n.UserId = x.UserId
WHERE x.Take > 0;

-- 舊版腳本以今天為 EarnedDate 寫入者一併壓成 2000-01-01（冪等）
UPDATE CompensatoryLots
SET EarnedDate = '2000-01-01'
WHERE IsOpening = 1 AND EarnedDate <> '2000-01-01';

SELECT N'【寫入結果】' AS Section, COUNT(*) AS Lots, SUM(Hours) AS TotalHours, SUM(RemainingHours) AS TotalRemaining
FROM CompensatoryLots WHERE IsOpening = 1;

SELECT N'【寫入扣抵】' AS Section, COUNT(*) AS Usages, ISNULL(SUM(cu.Hours), 0) AS TotalHours
FROM CompensatoryUsages cu
JOIN @NewLots n ON n.LotId = cu.LotId;

DROP TABLE #Usage;
DROP TABLE #PostLeave;
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
