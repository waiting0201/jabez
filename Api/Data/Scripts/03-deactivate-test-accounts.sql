/*
================================================================================
 停用測試帳號（Status → 'inactive'）
================================================================================

 問題
 ----
 測試用假帳號（Email 為 @example.com，或姓名含「測試」）目前 Status 皆為 'active'，
 會一起進入簽核引擎的審核者候選池：

   · 上層級關卡無人時的升級指派（EscalationService.FindSuperiorInAncestorDepartmentsAsync）
     —— 例如「營運管理及發展部」同時有「陳婉婷」與「執行長測試」兩位執行長（同 Level），
     指派給誰取決於排序；被抽中測試帳號時，單子會送到一個沒有人在看的信箱。
   · 固定關卡的審核者池（ResolveReviewerPoolAsync）與跨步驟去重的「全池皆已審」判定
     —— 池中多一個永遠不會去簽的帳號，會讓自動代簽跳過的條件湊不齊。

 修法
 ----
 把測試帳號的 Status 改成 'inactive'。引擎所有找人邏輯都已過濾 Status = 'active'，
 停用後即不再進入任何候選池；歷史簽核紀錄與 PDF 簽名章不受影響（只看 UserId）。

 ⚠ 停用＝無法登入（AuthHandler 對 'inactive' 直接擋下登入與 Refresh）。
    因此本腳本**刻意跳過仍背著未完成任務的測試帳號**，否則那些單會變成沒有人能審：
      · 是 pending 單的指定審核者（RequestDesignatedReviewers.Status = 'pending'）
      · 是任何 EscalationOverride 的升級指派審核者
      · 是 pending 請假單的職務代理人（LeaveRequests.AgentUserId）
      · 是在職員工的職務代理人（Users.AgentUserId）
    被跳過的帳號會在最後一段列出 —— 請先把那些單審完／改指派，再重跑一次本腳本。

 用法
 ----
 1) 先以 @Commit = 0 執行（空跑）：只印出「將停用 / 將保留」兩份清單，最後 ROLLBACK。
 2) 確認無誤後把 @Commit 改成 1 再執行一次，才會真正寫入。
 用 sqlcmd 跑請加 -u（否則訊息中的中文會變亂碼）。

 各環境（本機 / staging / 正式）的測試帳號不同，本腳本一律以「條件比對」定位，不寫死 Id。
 正式環境執行前務必先空跑，確認清單裡沒有真人帳號。

 相關文件：docs/business/approval-escalation.md §不與後續關卡撞人
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入

BEGIN TRANSACTION;

-- ── 定位測試帳號 ────────────────────────────────────────────────────────────────
DECLARE @TestAccounts TABLE (Id uniqueidentifier PRIMARY KEY);

INSERT INTO @TestAccounts (Id)
SELECT u.Id
FROM Users u
WHERE u.IsSuperAdmin = 0
  AND u.Status       = 'active'
  AND (u.Email LIKE '%@example.com' OR u.Name LIKE N'%測試%');

-- ── 仍有未完成任務者不動（停用會讓那些單沒人能審）───────────────────────────────
DECLARE @Blocked TABLE (Id uniqueidentifier PRIMARY KEY);

INSERT INTO @Blocked (Id)
SELECT t.Id
FROM @TestAccounts t
WHERE EXISTS (SELECT 1 FROM RequestDesignatedReviewers r
              WHERE r.ReviewerId = t.Id AND r.Status = 'pending')
   OR EXISTS (SELECT 1 FROM EscalationOverrides e
              WHERE e.ReviewerId = t.Id)
   OR EXISTS (SELECT 1 FROM LeaveRequests l
              WHERE l.AgentUserId = t.Id AND l.ApprovalStatus = 'pending')
   OR EXISTS (SELECT 1 FROM Users u
              WHERE u.AgentUserId = t.Id AND u.Status = 'active');

PRINT N'===== 將停用 =====';
SELECT u.Name, u.Email, ISNULL(d.Name, N'(無部門)') AS Department, ISNULL(j.Name, N'(無職稱)') AS JobTitle
FROM Users u
JOIN @TestAccounts t ON t.Id = u.Id
LEFT JOIN Departments d ON d.Id = u.DepartmentId
LEFT JOIN JobTitles   j ON j.Id = u.JobTitleId
WHERE t.Id NOT IN (SELECT Id FROM @Blocked)
ORDER BY j.Level, u.Name;

PRINT N'===== 保留在職（有未完成任務，處理完再重跑）=====';
SELECT u.Name, u.Email, ISNULL(d.Name, N'(無部門)') AS Department, ISNULL(j.Name, N'(無職稱)') AS JobTitle,
       (SELECT COUNT(*) FROM RequestDesignatedReviewers r
        WHERE r.ReviewerId = u.Id AND r.Status = 'pending')                                   AS PendingDesignated,
       (SELECT COUNT(*) FROM EscalationOverrides e WHERE e.ReviewerId = u.Id)                 AS EscalationAssigned,
       (SELECT COUNT(*) FROM LeaveRequests l
        WHERE l.AgentUserId = u.Id AND l.ApprovalStatus = 'pending')                          AS PendingLeaveAgent,
       (SELECT COUNT(*) FROM Users x WHERE x.AgentUserId = u.Id AND x.Status = 'active')      AS ActiveAgentOf
FROM Users u
JOIN @Blocked b ON b.Id = u.Id
LEFT JOIN Departments d ON d.Id = u.DepartmentId
LEFT JOIN JobTitles   j ON j.Id = u.JobTitleId
ORDER BY j.Level, u.Name;

-- ── 寫入 ────────────────────────────────────────────────────────────────────────
UPDATE u
SET u.Status    = 'inactive',
    u.UpdatedAt = SYSDATETIME()
FROM Users u
JOIN @TestAccounts t ON t.Id = u.Id
WHERE t.Id NOT IN (SELECT Id FROM @Blocked);

PRINT N'===== 已停用筆數 =====';
PRINT CAST(@@ROWCOUNT AS nvarchar(10));

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'已 COMMIT。';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'空跑模式（@Commit = 0），已 ROLLBACK，未寫入任何資料。';
END
