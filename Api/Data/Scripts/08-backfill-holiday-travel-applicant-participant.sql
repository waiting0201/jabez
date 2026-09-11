/*
================================================================================
 回填假日執行活動的申請人為「參與執行人員」
================================================================================

 背景
 ----
 2026-09-11 之前，假日執行活動的申請人是**無條件**領假日津貼的：

   · 薪資：PayrollReadService 的 HolidayTravelDays CTE 有一支
     `SELECT tr.EmployeeId ... FROM TravelRequests tr`，只要單子核准，
     申請人就拿整單 TravelRequest.HolidayDays 天，與參與人員清單無關。
   · 簽核頁：PaymentRequestReadService.BuildHolidayAllowances 把申請人
     寫死成清單第一列（IsApplicant = 1）。

 業務決議改成「申請人不會自動被算成參與者」：要領津貼就得把自己加進
 參與執行人員清單（表單的人員下拉本來就含自己），比照一般參與者可逐日
 勾選與半天。上述兩處已於同一次變更移除申請人專用分支。

 為什麼需要這支腳本
 ------------------
 薪資是**即時重算、沒有月結快照**（見 CLAUDE.md「過往薪資」）。程式一改，
 過去已核准的假日活動單，申請人那份津貼會從**歷史月份**憑空消失——但錢
 早就發出去了，帳會對不起來。故把歷史單的申請人補進 TravelRequestParticipants，
 讓既有金額維持不變，新規則只對日後的單生效。

 範圍
 ----
 IsHolidayTravel = 1 且 ApprovalStatus IN ('pending', 'returned', 'approved')
 的 TravelRequests。

   · approved：已發過錢，必須維持金額（本腳本的主要目的）。
   · pending / returned：送簽時申請人仍預期自己會被計入，核准後才發錢；
     若不補，這批單會在核准當下靜默少一個人。送簽中的單無法由申請人自行
     編輯，故一併補。
   · draft：申請人還能自己編輯，且會看到表單新增的提示文字，不補。
   · rejected / cancelled：不會發津貼，不補。

 寫入內容
 --------
 一列 TravelRequestParticipants：
   TravelRequestId = 該單、UserId = tr.EmployeeId、
   SortOrder       = 現有最大 SortOrder + 1（清單為空時為 1），
   HolidayDays     = NULL（＝全程參與，讀取端一律 COALESCE 回整單 HolidayDays，
                     與舊行為「申請人領整單 HolidayDays」完全等值）

 不寫 TravelRequestParticipantDates：沒有逐日勾選＝全程參與，
 正是舊制申請人的語意（不逐日、不半天）。

 冪等
 ----
 申請人已在清單中者跳過（DB 亦有唯一索引 (TravelRequestId, UserId)）。
 可安全重跑。

 環境無關
 --------
 不寫死任何 Id，本機 / staging / 正式站皆可直接跑。整份為單一 batch、
 不含 GO，可貼進 SSMS / Azure Data Studio 執行。

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。

 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P 'Strong@Password123' -d JabezDb -C -N -u \
   -i /tmp/08.sql

 空跑要確認三件事：
   ① 主報表的 Action：「補進清單」= 這次會新增的、「已在清單中」= 跳過的
   ② 補進的每一列，HolidayDays 欄位顯示的整單天數與該員原本領到的天數相同
   ③ 「申請人帳號異常」0 列（申請人已被刪除 / 查無此人）

 ⚠ 跑完之後，這批單的簽核頁「參與執行人員」卡片會多出申請人那一列
   （掛「申請人」badge、顯示「全程參與」），津貼合計不變。
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 目標單：假日執行活動，且處於「已送簽（含已核准）」狀態
-- ---------------------------------------------------------------------------
DECLARE @Targets TABLE (
    TravelRequestId int           PRIMARY KEY,
    RequestNo       nvarchar(50)  NULL,
    ApprovalStatus  nvarchar(30)  NOT NULL,
    ApplicantId     uniqueidentifier NOT NULL,
    HolidayDays     int           NOT NULL,
    EndDate         datetime2(7)  NOT NULL,
    AlreadyListed   bit           NOT NULL,
    NextSortOrder   int           NOT NULL
);

INSERT INTO @Targets (TravelRequestId, RequestNo, ApprovalStatus, ApplicantId,
                      HolidayDays, EndDate, AlreadyListed, NextSortOrder)
SELECT tr.Id,
       tr.RequestNo,
       tr.ApprovalStatus,
       tr.EmployeeId,
       tr.HolidayDays,
       tr.EndDate,
       CASE WHEN EXISTS (SELECT 1 FROM TravelRequestParticipants p
                         WHERE p.TravelRequestId = tr.Id
                           AND p.UserId          = tr.EmployeeId)
            THEN 1 ELSE 0 END,
       ISNULL((SELECT MAX(p.SortOrder) FROM TravelRequestParticipants p
               WHERE p.TravelRequestId = tr.Id), 0) + 1
FROM TravelRequests tr
WHERE tr.IsHolidayTravel = 1
  AND tr.ApprovalStatus IN ('pending', 'returned', 'approved');

-- ---------------------------------------------------------------------------
-- 閘門：申請人帳號必須存在（FK 為 Restrict，不存在會直接炸掉整份）
-- ---------------------------------------------------------------------------
SELECT N'申請人帳號異常' AS Issue,
       t.TravelRequestId, t.RequestNo, t.ApplicantId
FROM @Targets t
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Id = t.ApplicantId);

IF EXISTS (SELECT 1 FROM @Targets t
           WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Id = t.ApplicantId))
BEGIN
    PRINT N'⛔ 有單的申請人帳號查無此人，整份中止（請先人工判讀上表）。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- 主報表：這次會怎麼處理每一張單
--   HolidayDays 欄＝補進去之後該員會領到的天數（全程參與＝整單天數），
--   與舊制「申請人領整單 HolidayDays」相同，用來逐列核對金額不變。
-- ---------------------------------------------------------------------------
SELECT CASE WHEN t.AlreadyListed = 1 THEN N'已在清單中（跳過）' ELSE N'補進清單' END AS [Action],
       t.RequestNo,
       t.ApprovalStatus,
       u.Name        AS ApplicantName,
       t.HolidayDays AS [假日天數（全程參與）],
       CONVERT(date, t.EndDate) AS EndDate,
       t.NextSortOrder AS SortOrder
FROM @Targets t
JOIN Users u ON u.Id = t.ApplicantId
ORDER BY [Action], t.EndDate DESC, t.RequestNo;

-- ---------------------------------------------------------------------------
-- 寫入
-- ---------------------------------------------------------------------------
INSERT INTO TravelRequestParticipants (TravelRequestId, UserId, SortOrder, HolidayDays)
SELECT t.TravelRequestId, t.ApplicantId, t.NextSortOrder, NULL
FROM @Targets t
WHERE t.AlreadyListed = 0;

DECLARE @Inserted int = @@ROWCOUNT;

SELECT N'補進清單筆數' AS Summary, @Inserted AS Cnt
UNION ALL SELECT N'已在清單中（跳過）', (SELECT COUNT(*) FROM @Targets WHERE AlreadyListed = 1)
UNION ALL SELECT N'目標單總數',         (SELECT COUNT(*) FROM @Targets);

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'✅ 已寫入（COMMIT）。';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'🔍 空跑（ROLLBACK），未寫入任何資料。確認報表無誤後把 @Commit 改成 1 再跑一次。';
END;
