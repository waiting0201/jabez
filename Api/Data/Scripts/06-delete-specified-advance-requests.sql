/*
================================================================================
 刪除指定的預支申請單（依單號）
================================================================================

 目標單號（1 張，2026-09-08 指定）：
   ADV-20260907-055

 與 04b-purge-test-request-data-per-row.sql 的刪除規則相同，差別只有兩點：
   · 以 **RequestNo 定位**（不寫死 Id），同一份可在本機 / staging / 正式站跑
   · 範圍限定在上面列出的單號，不做任何「測試帳號 / 測試專案」的推導

 刪除順序不可調換
 ----------------
   1) 子單（預支沖銷 WriteOffRecords）的簽核足跡 → 子單本體
   2) 母單（AdvanceRequests）的簽核足跡 → 母單本體

 ApprovalRecords / EscalationOverrides / RequestDesignatedReviewers 這三張表
 以 (ApplicationType, ApplicationId) 多型關聯指向申請父表，**沒有真 FK**，
 cascade 不會清。必須趕在父列消失前刪掉，否則殘列仍掛著 ReviewerId /
 ReviewedById 指向 Users，日後刪該員工會噴
 FK_RequestDesignatedReviewers_Users_ReviewerId。

 母單的 AdvanceRequestItems / AdvanceRequestInstallments /
 AdvanceRequestSupplements / WriteOffRecords 皆為 CASCADE，會自動一起走；
 但 WriteOffRecords 若被 cascade 掃掉，其簽核足跡就變孤兒，故先手動處理。

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。

 @AllowPaid = 0 時，只要目標單（或其沖銷子單）有**已撥款**的分期列就整份中止，
 避免誤刪已出納的帳。確定要刪才改 1。

 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P 'Strong@Password123' -d JabezDb -C -N -u \
   -i /tmp/06.sql

 ⚠ 附件 blob 不會被刪（發票影像 / 整單附件成為孤兒 blob，不影響功能）。
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit    bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入
DECLARE @AllowPaid bit = 0;   -- ← 目標單有已撥款分期時是否放行（預設不放行）

-- ---------------------------------------------------------------------------
-- 目標單號
-- ---------------------------------------------------------------------------
DECLARE @Targets TABLE (RequestNo nvarchar(50) PRIMARY KEY);
INSERT INTO @Targets (RequestNo) VALUES
    (N'ADV-20260907-055');

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 解析 Id
-- ---------------------------------------------------------------------------
DECLARE @Adv TABLE (Id int PRIMARY KEY, RequestNo nvarchar(50));
INSERT INTO @Adv (Id, RequestNo)
SELECT a.Id, a.RequestNo
FROM AdvanceRequests a
JOIN @Targets t ON t.RequestNo = a.RequestNo;

DECLARE @WriteOff TABLE (Id int PRIMARY KEY, RequestNo nvarchar(50), AdvanceRequestId int);
INSERT INTO @WriteOff (Id, RequestNo, AdvanceRequestId)
SELECT w.Id, w.RequestNo, w.AdvanceRequestId
FROM WriteOffRecords w
WHERE w.AdvanceRequestId IN (SELECT Id FROM @Adv);

-- ---------------------------------------------------------------------------
-- 刪除前報表
-- ---------------------------------------------------------------------------
PRINT '=== 找不到的單號（不會被刪，請確認是否打錯或不在本資料庫）===';
SELECT t.RequestNo AS [NotFound]
FROM @Targets t
WHERE NOT EXISTS (SELECT 1 FROM @Adv a WHERE a.RequestNo = t.RequestNo);

PRINT '=== 將被刪除的預支單 ===';
SELECT  a.Id,
        a.RequestNo,
        r.ApprovalStatus,
        r.GrandTotal,
        r.IsClosed,
        u.Name                AS Applicant,
        r.SubmittedAt,
        (SELECT COUNT(*) FROM AdvanceRequestItems        i WHERE i.AdvanceRequestId = a.Id) AS Items,
        (SELECT COUNT(*) FROM AdvanceRequestSupplements  s WHERE s.AdvanceRequestId = a.Id) AS Supplements,
        (SELECT COUNT(*) FROM AdvanceRequestInstallments n WHERE n.AdvanceRequestId = a.Id) AS Installments,
        (SELECT COUNT(*) FROM AdvanceRequestInstallments n WHERE n.AdvanceRequestId = a.Id AND n.PaidAt IS NOT NULL) AS PaidInstallments,
        (SELECT COUNT(*) FROM @WriteOff w WHERE w.AdvanceRequestId = a.Id) AS WriteOffs,
        (SELECT COUNT(*) FROM ApprovalRecords            p WHERE p.ApplicationType = 'advance' AND p.ApplicationId = a.Id) AS ApprovalRecords,
        (SELECT COUNT(*) FROM RequestDesignatedReviewers d WHERE d.RequestType     = 'advance' AND d.RequestId     = a.Id) AS Designees,
        (SELECT COUNT(*) FROM EscalationOverrides        e WHERE e.ApplicationType = 'advance' AND e.ApplicationId = a.Id) AS Escalations
FROM @Adv a
JOIN AdvanceRequests r ON r.Id = a.Id
LEFT JOIN Users u ON u.Id = r.SubmittedById
ORDER BY a.RequestNo;

PRINT '=== 將被連帶刪除的預支沖銷子單 ===';
SELECT  w.Id, w.RequestNo, r.ApprovalStatus, w.AdvanceRequestId
FROM @WriteOff w
JOIN WriteOffRecords r ON r.Id = w.Id
ORDER BY w.Id;

-- ---------------------------------------------------------------------------
-- 已撥款保護
-- ---------------------------------------------------------------------------
DECLARE @PaidCount int =
    (SELECT COUNT(*) FROM AdvanceRequestInstallments n
     WHERE n.AdvanceRequestId IN (SELECT Id FROM @Adv) AND n.PaidAt IS NOT NULL)
  + (SELECT COUNT(*) FROM WriteOffInstallments n
     WHERE n.WriteOffRecordId IN (SELECT Id FROM @WriteOff) AND n.PaidAt IS NOT NULL);

IF @PaidCount > 0 AND @AllowPaid = 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'目標單有 %d 筆已撥款的分期列，已中止。確定要刪請把 @AllowPaid 改成 1。', 16, 1, @PaidCount);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- 1) 子單：預支沖銷（簽核足跡 → 本體）
-- ---------------------------------------------------------------------------
DELETE FROM ApprovalRecords
WHERE ApplicationType = 'write_off' AND ApplicationId IN (SELECT Id FROM @WriteOff);

DELETE FROM EscalationOverrides
WHERE ApplicationType = 'write_off' AND ApplicationId IN (SELECT Id FROM @WriteOff);

DELETE FROM RequestDesignatedReviewers
WHERE RequestType = 'write_off' AND RequestId IN (SELECT Id FROM @WriteOff);

DELETE FROM WriteOffRecords WHERE Id IN (SELECT Id FROM @WriteOff);

-- ---------------------------------------------------------------------------
-- 2) 母單：預支申請（簽核足跡 → 本體）
--    Items / Installments / Supplements 為 CASCADE，隨父列一起走
-- ---------------------------------------------------------------------------
DELETE FROM ApprovalRecords
WHERE ApplicationType = 'advance' AND ApplicationId IN (SELECT Id FROM @Adv);

DELETE FROM EscalationOverrides
WHERE ApplicationType = 'advance' AND ApplicationId IN (SELECT Id FROM @Adv);

DELETE FROM RequestDesignatedReviewers
WHERE RequestType = 'advance' AND RequestId IN (SELECT Id FROM @Adv);

DELETE FROM AdvanceRequests WHERE Id IN (SELECT Id FROM @Adv);

-- ---------------------------------------------------------------------------
-- 刪除後驗證
-- ---------------------------------------------------------------------------
PRINT '=== 刪除後殘留檢查（應全部為 0）===';
SELECT
    (SELECT COUNT(*) FROM AdvanceRequests            WHERE Id IN (SELECT Id FROM @Adv))                                           AS Advances,
    (SELECT COUNT(*) FROM WriteOffRecords            WHERE Id IN (SELECT Id FROM @WriteOff))                                      AS WriteOffs,
    (SELECT COUNT(*) FROM ApprovalRecords            WHERE ApplicationType = 'advance' AND ApplicationId IN (SELECT Id FROM @Adv)) AS AdvApprovalRecords,
    (SELECT COUNT(*) FROM RequestDesignatedReviewers WHERE RequestType     = 'advance' AND RequestId     IN (SELECT Id FROM @Adv)) AS AdvDesignees,
    (SELECT COUNT(*) FROM EscalationOverrides        WHERE ApplicationType = 'advance' AND ApplicationId IN (SELECT Id FROM @Adv)) AS AdvEscalations;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT '=== @Commit = 1 → 已 COMMIT，資料已刪除 ===';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT '=== @Commit = 0 → 已 ROLLBACK（空跑，未寫入任何變更）===';
END
