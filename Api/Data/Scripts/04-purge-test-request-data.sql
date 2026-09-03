/*
================================================================================
 清除測試申請單與測試專案
================================================================================

 目的
 ----
 把開發過程留下的測試申請單、測試專案清掉，讓清單頁與報表回到乾淨狀態。
 **帳號一律不動**（測試帳號留著繼續用；要停用請改跑 03-deactivate-test-accounts.sql）。

 刪除範圍（三條件取聯集，再補上相依單據）
 ------------------------------------------
 ① 測試帳號送出的所有申請單
      Email 為 @example.com，或 cherng1217@gmail.com / cherng1217@hotmail.com
 ② 掛在測試專案上的所有申請單
      Projects.Code IN (P2026CoreTest, D1test, D2test, D3test, Digitaltest)
      —— 含真人員工開在這些專案上的單，否則專案刪不掉（見下方「為什麼專案刪不掉」）
 ③ 明列的 10 張單號
      PR-20260822-001/002/003、PR-20260830-005/007、PR-20260805-001、
      ADV-20260812-002/003、TPR-20260813-001、HTR-20260812-002
 ④ 預審申請 **全表清空**（PreReviewRequests，不分申請人與狀態）
 ⑤ 相依單據：母單被刪就一起刪 —— 預支沖銷（母單＝預支單）、
      出差預支沖銷（母單＝出差單）、銷假單（母單＝請假單）
 最後刪除 ⑥ 五個測試專案本身。

 為什麼不能從畫面上刪
 --------------------
 所有 *RequestHandler.DeleteAsync 都有 `ApprovalStatus != "draft" && != "returned"`
 的閘門，本批多數是 approved / pending，API 一律擋下；
 ProjectHandler.DeleteAsync 另外對「有請款單」「有加班明細關聯」直接擋下。
 故只能走本腳本。

 為什麼專案刪不掉
 ----------------
 Projects → PaymentRequests / PreReviewRequests / AdvanceRequests 是 CASCADE，
 但 **OvertimeRequestProjects.ProjectId 是 NO_ACTION**（雙 FK 子表的第二主檔，
 見 backend-design.md §7.5），殘留一列就會擋住整個 DELETE。
 本腳本把引用測試專案的加班單整張納入刪除範圍，並在刪專案前再做一次防禦性清除。

 ⚠ 多型關聯必須手動清
 --------------------
 ApprovalRecords(ApplicationType+ApplicationId)、EscalationOverrides(同左)、
 RequestDesignatedReviewers(RequestType+RequestId) 指向 9 種申請父表但**沒有真 FK**，
 cascade 不會清。漏清的話單子刪掉、殘列仍掛著 ReviewerId / ReviewedById 指向 Users，
 日後刪該員工會噴 FK_RequestDesignatedReviewers_Users_ReviewerId。
 本腳本在刪父單前先用 (AppType, AppId) 清掉這三張表。

 ⚠ 附件 blob 不會被刪
 --------------------
 發票影像、報價單、整單附件仍留在 Azurite / Blob Storage，成為孤兒 blob。
 本機不影響功能（沒有父列就沒人去讀），需要時再自行清容器。

 用法
 ----
 1) 先以 @Commit = 0 執行（空跑）：印出完整刪除清單與筆數，最後 ROLLBACK。
 2) 確認無誤後把 @Commit 改成 1 再執行一次，才會真正寫入。

 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P 'Strong@Password123' -d JabezDb -C -N -u \
   -i /path/to/04-purge-test-request-data.sql

 本腳本一律以「條件比對」定位，不寫死 Id，各環境可重複執行（第二次跑會是 0 筆）。
 相關文件：docs/business/approval-flow.md §申請人指定審核模式
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入

BEGIN TRANSACTION;

-- ── ① 測試帳號（帳號本身不刪，只用來認出他們送的單）──────────────────────────
DECLARE @TestUsers TABLE (Id uniqueidentifier PRIMARY KEY);

INSERT INTO @TestUsers (Id)
SELECT Id
FROM Users
WHERE IsSuperAdmin = 0
  AND (Email LIKE '%@example.com'
       OR Email IN ('cherng1217@gmail.com', 'cherng1217@hotmail.com'));

-- ── ② 測試專案 ────────────────────────────────────────────────────────────────
DECLARE @TestProjects TABLE (Id int PRIMARY KEY);

INSERT INTO @TestProjects (Id)
SELECT Id
FROM Projects
WHERE Code IN ('P2026CoreTest', 'D1test', 'D2test', 'D3test', 'Digitaltest');

-- ── ③ 明列單號 ────────────────────────────────────────────────────────────────
DECLARE @Nos TABLE (RequestNo nvarchar(50) PRIMARY KEY);

INSERT INTO @Nos (RequestNo) VALUES
    ('PR-20260822-001'), ('PR-20260822-002'), ('PR-20260822-003'),
    ('PR-20260830-005'), ('PR-20260830-007'), ('PR-20260805-001'),
    ('ADV-20260812-002'), ('ADV-20260812-003'),
    ('TPR-20260813-001'), ('HTR-20260812-002');

-- ── ④ 組出目標單清單 (AppType, AppId) ─────────────────────────────────────────
-- AppType 使用與 ApprovalRecords.ApplicationType 相同的字面值，供多型清洗直接 JOIN。
DECLARE @T TABLE (
    AppType nvarchar(30) NOT NULL,
    AppId   int          NOT NULL,
    Label   nvarchar(80) NULL,
    PRIMARY KEY (AppType, AppId)
);

-- 請款申請
INSERT INTO @T (AppType, AppId, Label)
SELECT 'payment_request', Id, ISNULL(RequestNo, N'(草稿)')
FROM PaymentRequests
WHERE SubmittedById IN (SELECT Id FROM @TestUsers)
   OR RequestNo     IN (SELECT RequestNo FROM @Nos)
   OR ProjectId     IN (SELECT Id FROM @TestProjects);

-- 預審申請（全表清空）
INSERT INTO @T (AppType, AppId, Label)
SELECT 'pre_review', Id, ISNULL(RequestNo, N'(草稿)')
FROM PreReviewRequests;

-- 預支申請
INSERT INTO @T (AppType, AppId, Label)
SELECT 'advance', Id, ISNULL(RequestNo, N'(草稿)')
FROM AdvanceRequests
WHERE SubmittedById IN (SELECT Id FROM @TestUsers)
   OR RequestNo     IN (SELECT RequestNo FROM @Nos)
   OR ProjectId     IN (SELECT Id FROM @TestProjects);

-- 出差預支 / 假日執行活動（同一張表，靠 IsHolidayTravel 分流 AppType）
INSERT INTO @T (AppType, AppId, Label)
SELECT CASE WHEN IsHolidayTravel = 1 THEN 'holiday_travel' ELSE 'travel' END,
       Id, ISNULL(RequestNo, N'(草稿)')
FROM TravelRequests
WHERE EmployeeId IN (SELECT Id FROM @TestUsers)
   OR RequestNo  IN (SELECT RequestNo FROM @Nos)
   OR ProjectId  IN (SELECT Id FROM @TestProjects);

-- 出差請款申請
INSERT INTO @T (AppType, AppId, Label)
SELECT 'travel_payment', Id, ISNULL(RequestNo, N'(草稿)')
FROM TravelPaymentRequests
WHERE EmployeeId IN (SELECT Id FROM @TestUsers)
   OR RequestNo  IN (SELECT RequestNo FROM @Nos)
   OR ProjectId  IN (SELECT Id FROM @TestProjects);

-- 加班申請（無單號；引用測試專案者整張納入，否則 OvertimeRequestProjects 會擋住刪專案）
INSERT INTO @T (AppType, AppId, Label)
SELECT 'overtime', o.Id, CONCAT(N'加班單 #', o.Id)
FROM OvertimeRequests o
WHERE o.EmployeeId IN (SELECT Id FROM @TestUsers)
   OR EXISTS (SELECT 1 FROM OvertimeRequestProjects op
              WHERE op.OvertimeRequestId = o.Id
                AND op.ProjectId IN (SELECT Id FROM @TestProjects));

-- 請假申請（無單號、無專案）
INSERT INTO @T (AppType, AppId, Label)
SELECT 'leave', Id, CONCAT(N'請假單 #', Id)
FROM LeaveRequests
WHERE EmployeeId IN (SELECT Id FROM @TestUsers);

-- ── ⑤ 相依單據：母單被刪就一起刪 ──────────────────────────────────────────────
-- 這三段必須排在上面之後（要讀 @T 已收錄的母單 Id）。
-- FK 本身是 CASCADE，但多型足跡要靠 @T 清，所以仍須逐張收錄。

-- 預支沖銷（母單＝預支單）
INSERT INTO @T (AppType, AppId, Label)
SELECT 'write_off', w.Id, ISNULL(w.RequestNo, N'(草稿)')
FROM WriteOffRecords w
WHERE w.SubmittedById   IN (SELECT Id FROM @TestUsers)
   OR w.AdvanceRequestId IN (SELECT AppId FROM @T WHERE AppType = 'advance');

-- 出差預支沖銷（母單＝出差單）
INSERT INTO @T (AppType, AppId, Label)
SELECT 'travel_write_off', w.Id, ISNULL(w.RequestNo, N'(草稿)')
FROM TravelWriteOffRecords w
WHERE w.SubmittedById  IN (SELECT Id FROM @TestUsers)
   OR w.TravelRequestId IN (SELECT AppId FROM @T WHERE AppType IN ('travel', 'holiday_travel'));

-- 銷假申請（母單＝請假單）
INSERT INTO @T (AppType, AppId, Label)
SELECT 'leave_revocation', r.Id, CONCAT(N'銷假單 #', r.Id)
FROM LeaveRevocations r
WHERE r.EmployeeId     IN (SELECT Id FROM @TestUsers)
   OR r.LeaveRequestId IN (SELECT AppId FROM @T WHERE AppType = 'leave');

-- ── 刪除前報表 ────────────────────────────────────────────────────────────────
PRINT N'===== 將刪除的申請單（依類型彙總）=====';
SELECT AppType AS 類型, COUNT(*) AS 筆數 FROM @T GROUP BY AppType ORDER BY AppType;

PRINT N'===== 將刪除的申請單（逐筆）=====';
SELECT AppType AS 類型, AppId AS Id, Label AS 單號 FROM @T ORDER BY AppType, AppId;

PRINT N'===== 將刪除的專案 =====';
SELECT p.Id, p.Code AS 專案代碼, p.Name AS 專案名稱, p.Status AS 狀態
FROM Projects p JOIN @TestProjects tp ON tp.Id = p.Id
ORDER BY p.Code;

PRINT N'===== 將清除的簽核足跡（多型關聯，無 FK）=====';
SELECT N'ApprovalRecords' AS 資料表, COUNT(*) AS 筆數
FROM ApprovalRecords a JOIN @T t ON t.AppType = a.ApplicationType AND t.AppId = a.ApplicationId
UNION ALL
SELECT N'EscalationOverrides', COUNT(*)
FROM EscalationOverrides e JOIN @T t ON t.AppType = e.ApplicationType AND t.AppId = e.ApplicationId
UNION ALL
SELECT N'RequestDesignatedReviewers', COUNT(*)
FROM RequestDesignatedReviewers r JOIN @T t ON t.AppType = r.RequestType AND t.AppId = r.RequestId;

-- ── 執行刪除 ──────────────────────────────────────────────────────────────────
-- 1) 先清多型足跡（沒有 FK，cascade 不會處理；必須趕在父列消失前用 Id 比對）
DELETE a FROM ApprovalRecords a
JOIN @T t ON t.AppType = a.ApplicationType AND t.AppId = a.ApplicationId;

DELETE e FROM EscalationOverrides e
JOIN @T t ON t.AppType = e.ApplicationType AND t.AppId = e.ApplicationId;

DELETE r FROM RequestDesignatedReviewers r
JOIN @T t ON t.AppType = r.RequestType AND t.AppId = r.RequestId;

-- 2) 再刪申請單本體：先刪子單（沖銷 / 銷假），再刪母單。
--    明細、附件、分期撥款子表皆為 CASCADE，會自動一起走。
DELETE w FROM WriteOffRecords w       JOIN @T t ON t.AppType = 'write_off'        AND t.AppId = w.Id;
DELETE w FROM TravelWriteOffRecords w JOIN @T t ON t.AppType = 'travel_write_off' AND t.AppId = w.Id;
DELETE r FROM LeaveRevocations r      JOIN @T t ON t.AppType = 'leave_revocation' AND t.AppId = r.Id;

DELETE x FROM PaymentRequests x       JOIN @T t ON t.AppType = 'payment_request' AND t.AppId = x.Id;
DELETE x FROM PreReviewRequests x     JOIN @T t ON t.AppType = 'pre_review'      AND t.AppId = x.Id;
DELETE x FROM AdvanceRequests x       JOIN @T t ON t.AppType = 'advance'         AND t.AppId = x.Id;
DELETE x FROM TravelRequests x        JOIN @T t ON t.AppType IN ('travel', 'holiday_travel') AND t.AppId = x.Id;
DELETE x FROM TravelPaymentRequests x JOIN @T t ON t.AppType = 'travel_payment'  AND t.AppId = x.Id;
DELETE x FROM OvertimeRequests x      JOIN @T t ON t.AppType = 'overtime'        AND t.AppId = x.Id;
DELETE x FROM LeaveRequests x         JOIN @T t ON t.AppType = 'leave'           AND t.AppId = x.Id;

-- 3) 防禦性清除：OvertimeRequestProjects.ProjectId 是 NO_ACTION，殘一列就擋住刪專案。
--    上一步已把引用測試專案的加班單整張刪掉（cascade 連帶清掉明細），
--    這行只為保險 —— 正常情況會是 0 筆。
DELETE op FROM OvertimeRequestProjects op
JOIN @TestProjects tp ON tp.Id = op.ProjectId;

-- 4) 最後刪專案（ProjectPaymentSchedules 為 CASCADE，自動一起走）
DELETE p FROM Projects p JOIN @TestProjects tp ON tp.Id = p.Id;

-- ── 刪除後驗證 ────────────────────────────────────────────────────────────────
PRINT N'===== 刪除後殘留檢查（全部應為 0）=====';
SELECT N'殘留：引用測試專案的請款單' AS 檢查項, COUNT(*) AS 筆數
FROM PaymentRequests x JOIN @TestProjects tp ON tp.Id = x.ProjectId
UNION ALL SELECT N'殘留：引用測試專案的加班明細', COUNT(*)
FROM OvertimeRequestProjects x JOIN @TestProjects tp ON tp.Id = x.ProjectId
UNION ALL SELECT N'殘留：未刪除的測試專案', COUNT(*)
FROM Projects p JOIN @TestProjects tp ON tp.Id = p.Id
UNION ALL SELECT N'殘留：預審申請（應全數清空）', COUNT(*) FROM PreReviewRequests
UNION ALL SELECT N'孤兒：ApprovalRecords（父單已不存在）', COUNT(*)
FROM ApprovalRecords a
WHERE NOT EXISTS (SELECT 1 FROM PaymentRequests       p WHERE a.ApplicationType = 'payment_request'  AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM PreReviewRequests     p WHERE a.ApplicationType = 'pre_review'       AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM AdvanceRequests       p WHERE a.ApplicationType = 'advance'          AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM TravelRequests        p WHERE a.ApplicationType IN ('travel','holiday_travel') AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM TravelPaymentRequests p WHERE a.ApplicationType = 'travel_payment'   AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM WriteOffRecords       p WHERE a.ApplicationType = 'write_off'        AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM TravelWriteOffRecords p WHERE a.ApplicationType = 'travel_write_off' AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM OvertimeRequests      p WHERE a.ApplicationType = 'overtime'         AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM LeaveRequests         p WHERE a.ApplicationType = 'leave'            AND p.Id = a.ApplicationId)
  AND NOT EXISTS (SELECT 1 FROM LeaveRevocations      p WHERE a.ApplicationType = 'leave_revocation' AND p.Id = a.ApplicationId)
UNION ALL SELECT N'孤兒：RequestDesignatedReviewers（父單已不存在）', COUNT(*)
FROM RequestDesignatedReviewers r
WHERE NOT EXISTS (SELECT 1 FROM PaymentRequests       p WHERE r.RequestType = 'payment_request'  AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM PreReviewRequests     p WHERE r.RequestType = 'pre_review'       AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM AdvanceRequests       p WHERE r.RequestType = 'advance'          AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM TravelRequests        p WHERE r.RequestType IN ('travel','holiday_travel') AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM TravelPaymentRequests p WHERE r.RequestType = 'travel_payment'   AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM WriteOffRecords       p WHERE r.RequestType = 'write_off'        AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM TravelWriteOffRecords p WHERE r.RequestType = 'travel_write_off' AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM OvertimeRequests      p WHERE r.RequestType = 'overtime'         AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM LeaveRequests         p WHERE r.RequestType = 'leave'            AND p.Id = r.RequestId)
  AND NOT EXISTS (SELECT 1 FROM LeaveRevocations      p WHERE r.RequestType = 'leave_revocation' AND p.Id = r.RequestId);

-- ── 收尾 ──────────────────────────────────────────────────────────────────────
IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'✅ 已 COMMIT，變更已寫入資料庫。';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'🔍 空跑模式（@Commit = 0），已 ROLLBACK，資料庫未變更。';
    PRINT N'   確認上面清單無誤後，把 @Commit 改成 1 再執行一次。';
END
