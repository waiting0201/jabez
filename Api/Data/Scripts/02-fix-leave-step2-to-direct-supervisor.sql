/*
================================================================================
 修正：請假流程「申請人部門的協理」關卡 → 改為上層級審核（UseDirectSupervisor）
================================================================================

 問題
 ----
 請假流程的「≥3 天需部門協理簽核」關卡設定為
 UseApplicantDepartment = 1 + JobTitleId = 協理。沒有協理編制的部門（賣店、營業所、
 部分發展部…），其成員一旦請 ≥3 天，單子會停在該關但無人可審 —— 送得出去卻卡死。

 修法
 ----
 改為 UseDirectSupervisor（部門上層主管）。引擎會自動找同部門中層級最接近的上級；
 同部門找不到時（例如部門最高主管本人送單）會沿部門 ParentId 往上層部門找並以
 升級審核指派，仍找不到才跳過該關。天數門檻 MinDays 維持不變。

 ⚠ 執行前務必先跑 01-diagnose-unreachable-approval-steps.sql 確認實際狀況，
    各環境的 ApprovalItem / JobTitle Id 不同，本腳本一律以「條件比對」定位，不寫死 Id。

 ⚠ MinDays 在後台編輯表單是「一律覆寫」語意（ApprovalHandler：body 沒帶就會被清成
    NULL）。若改走後台 UI 而非本腳本，務必記得重填天數門檻，否則所有請假單都會跑到
    這一關。

 用法
 ----
 1) 先以 @Commit = 0 執行（空跑）：只印出「改動前 / 預計改動後」，最後 ROLLBACK。
 2) 確認無誤後把 @Commit 改成 1 再執行一次，才會真正寫入。
 用 sqlcmd 跑請加 -u（否則訊息中的中文會變亂碼）。

 相關文件：docs/business/approval-flow.md §上層級審核模式（UseDirectSupervisor）
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入

BEGIN TRANSACTION;

-- ── 定位目標關卡：啟用中的請假流程，UseApplicantDepartment + 職稱＝協理 ──────────
DECLARE @Targets TABLE (StepId int PRIMARY KEY);

INSERT INTO @Targets (StepId)
SELECT s.Id
FROM ApprovalSteps s
JOIN ApprovalItems ai ON ai.Id = s.ApprovalItemId
JOIN JobTitles     jt ON jt.Id = s.JobTitleId
WHERE ai.IsActive = 1
  AND ai.ApplicationType       = 'leave'
  AND s.UseApplicantDepartment = 1
  AND s.UseDirectSupervisor    = 0
  AND s.UseApplicantDesignated = 0
  AND jt.Name                  = N'協理';

PRINT N'===== 改動前 =====';
SELECT ai.Id AS ApprovalItemId, ISNULL(d.Name, N'(通用預設)') AS FlowScope, s.Id AS StepId,
       s.StepOrder, s.UseApplicantDepartment, s.UseDirectSupervisor,
       jt.Name AS JobTitle, s.MinDays, s.Note
FROM ApprovalSteps s
JOIN ApprovalItems ai   ON ai.Id = s.ApprovalItemId
LEFT JOIN Departments d ON d.Id  = ai.DepartmentId
LEFT JOIN JobTitles  jt ON jt.Id = s.JobTitleId
WHERE s.Id IN (SELECT StepId FROM @Targets)
ORDER BY ai.Id, s.StepOrder;

-- ── 套用：與 ApprovalHandler 切換 UseDirectSupervisor 時的正規化完全一致 ─────────
--    （DepartmentId / JobTitleId 清空、UseApplicantDepartment 設 true）
--    MinDays 刻意不動，維持原本的天數門檻。
UPDATE s
SET s.UseDirectSupervisor    = 1,
    s.UseApplicantDepartment = 1,
    s.DepartmentId           = NULL,
    s.JobTitleId             = NULL,
    s.Note                   = CASE
                                 WHEN s.Note LIKE N'%協理%'
                                   THEN REPLACE(s.Note, N'部門協理', N'部門上層主管')
                                 ELSE s.Note
                               END
FROM ApprovalSteps s
WHERE s.Id IN (SELECT StepId FROM @Targets);

DECLARE @Changed int = @@ROWCOUNT;

PRINT N'';
PRINT N'===== 改動後 =====';
SELECT ai.Id AS ApprovalItemId, ISNULL(d.Name, N'(通用預設)') AS FlowScope, s.Id AS StepId,
       s.StepOrder, s.UseApplicantDepartment, s.UseDirectSupervisor,
       s.JobTitleId, s.MinDays, s.Note
FROM ApprovalSteps s
JOIN ApprovalItems ai   ON ai.Id = s.ApprovalItemId
LEFT JOIN Departments d ON d.Id  = ai.DepartmentId
WHERE s.Id IN (SELECT StepId FROM @Targets)
ORDER BY ai.Id, s.StepOrder;

-- ── 防呆：MinDays 不應在本次被清掉 ────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM ApprovalSteps
           WHERE Id IN (SELECT StepId FROM @Targets) AND MinDays IS NULL)
BEGIN
    PRINT N'';
    PRINT N'!! 偵測到 MinDays 為 NULL —— 天數門檻遺失，已中止並回復。';
    ROLLBACK TRANSACTION;
    RETURN;
END

PRINT N'';
IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT CONCAT(N'已提交，異動關卡數：', @Changed);
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT CONCAT(N'空跑完成（未寫入），預計異動關卡數：', @Changed,
                 N'。確認上方結果無誤後，把 @Commit 改成 1 重跑。');
END
