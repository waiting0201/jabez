/*
================================================================================
 診斷：找出「送單後會卡死」的簽核關卡（唯讀，不修改任何資料）
================================================================================

 背景
 ----
 綁部門／職稱的「固定審核者池」關卡與上層級關卡不同 —— 簽核引擎不會因為查無人員
 而跳過，而是照樣停在該關。但沒有人通得過 AuthorizeStepAsync 的部門／職稱比對，
 也不會有人收到通知，結果是單子送得出去卻卡死在半路，只能靠 Superadmin 介入。

 2026-09 已於 ApprovalFlowService.ValidateFixedStepsHaveReviewersAsync 加上送單防呆：
 這類關卡查無人員時直接擋下送出並報錯。**上線前必須先跑本腳本、把查出的設定修掉**，
 否則原本「送出後卡死」的單會變成「送不出去」。

 用法
 ----
 直接執行；有回傳列 = 有問題要修。兩段各自獨立，都要看。
 用 sqlcmd 跑請加 -u（否則訊息中的中文會變亂碼）：
   sqlcmd -S <server> -d <db> -U <user> -P <pwd> -i 01-diagnose-unreachable-approval-steps.sql -u

 相關文件：docs/business/approval-flow.md §送單防呆：固定關卡查無審核者即擋下
================================================================================
*/

SET NOCOUNT ON;

-- ─────────────────────────────────────────────────────────────────────────────
-- 【A】固定部門／職稱關卡：全公司查無符合條件的在職人員
--      這種關卡對「所有」走到它的申請人都無解，優先修。
-- ─────────────────────────────────────────────────────────────────────────────
PRINT N'===== [A] 固定部門/職稱關卡查無在職審核者 =====';

SELECT
    ai.ApplicationType,
    ai.Id                            AS ApprovalItemId,
    ISNULL(aid.Name, N'(通用預設)')  AS FlowScope,
    s.StepOrder,
    ISNULL(sd.Name, N'(不限部門)')   AS StepDepartment,
    ISNULL(jt.Name, N'(不限職稱)')   AS StepJobTitle,
    s.MinDays
FROM ApprovalSteps s
JOIN ApprovalItems ai   ON ai.Id  = s.ApprovalItemId AND ai.IsActive = 1
LEFT JOIN Departments aid ON aid.Id = ai.DepartmentId
LEFT JOIN Departments sd  ON sd.Id  = s.DepartmentId
LEFT JOIN JobTitles   jt  ON jt.Id  = s.JobTitleId
WHERE s.UseApplicantDepartment = 0
  AND s.UseDirectSupervisor    = 0
  AND s.UseApplicantDesignated = 0
  AND NOT EXISTS (
        SELECT 1 FROM Users u
        WHERE u.Status = 'active' AND u.IsSuperAdmin = 0
          AND (s.DepartmentId IS NULL OR u.DepartmentId = s.DepartmentId)
          AND (s.JobTitleId   IS NULL OR u.JobTitleId   = s.JobTitleId))
ORDER BY ai.ApplicationType, ai.Id, s.StepOrder;

-- ─────────────────────────────────────────────────────────────────────────────
-- 【B】UseApplicantDepartment + 指定職稱的關卡：該部門沒有這個職稱的人
--      只對「流程實際會解析到這張 ApprovalItem」的部門成立，故先用遞迴 CTE
--      重現引擎的流程解析優先序：申請人部門 > 最近祖先部門 > 通用預設(NULL)。
--
--      請款類（payment_request / advance / write_off / travel_write_off / pre_review）
--      現行明確設計為「該部門無人則跳過該關」，不會卡死，故排除。
-- ─────────────────────────────────────────────────────────────────────────────
PRINT N'';
PRINT N'===== [B] 申請人部門關卡：該部門查無指定職稱的在職人員 =====';

WITH DeptChain AS (
    -- 每個部門到自己與各層祖先的距離（0 = 自己）
    SELECT d.Id AS DeptId, d.Id AS AncestorId, 0 AS Distance, d.ParentId
    FROM Departments d
    UNION ALL
    SELECT c.DeptId, p.Id, c.Distance + 1, p.ParentId
    FROM DeptChain c
    JOIN Departments p ON p.Id = c.ParentId
),
Candidates AS (
    -- 部門專屬流程（含沿 ParentId 往上的祖先部門）
    SELECT dc.DeptId, ai.ApplicationType, ai.Id AS ItemId, dc.Distance AS Priority
    FROM DeptChain dc
    JOIN ApprovalItems ai ON ai.IsActive = 1 AND ai.DepartmentId = dc.AncestorId
    UNION ALL
    -- 通用預設流程（排最後）
    SELECT d.Id, ai.ApplicationType, ai.Id, 999999
    FROM Departments d
    CROSS JOIN ApprovalItems ai
    WHERE ai.IsActive = 1 AND ai.DepartmentId IS NULL
),
Resolved AS (
    SELECT DeptId, ApplicationType, ItemId,
           ROW_NUMBER() OVER (PARTITION BY DeptId, ApplicationType ORDER BY Priority) AS rn
    FROM Candidates
)
SELECT
    r.ApplicationType,
    r.ItemId          AS ApprovalItemId,
    s.StepOrder,
    jt.Name           AS RequiredJobTitle,
    d.Name            AS BlockedDepartment,
    s.MinDays         AS OnlyWhenDaysAtLeast,
    (SELECT COUNT(*) FROM Users u
      WHERE u.DepartmentId = d.Id AND u.Status = 'active' AND u.IsSuperAdmin = 0)
                      AS AffectedActiveUsers
FROM Resolved r
JOIN Departments   d  ON d.Id = r.DeptId
JOIN ApprovalSteps s  ON s.ApprovalItemId = r.ItemId
JOIN JobTitles     jt ON jt.Id = s.JobTitleId
WHERE r.rn = 1
  AND s.UseApplicantDepartment = 1
  AND s.UseDirectSupervisor    = 0
  AND s.UseApplicantDesignated = 0
  AND s.JobTitleId IS NOT NULL
  AND r.ApplicationType NOT IN
      ('payment_request', 'advance', 'write_off', 'travel_write_off', 'pre_review')
  -- 該部門有在職人員，但沒有任何人是這個職稱
  AND EXISTS (SELECT 1 FROM Users u
              WHERE u.DepartmentId = d.Id AND u.Status = 'active' AND u.IsSuperAdmin = 0)
  AND NOT EXISTS (SELECT 1 FROM Users u
                  WHERE u.DepartmentId = d.Id AND u.Status = 'active'
                    AND u.IsSuperAdmin = 0 AND u.JobTitleId = s.JobTitleId)
ORDER BY r.ApplicationType, r.ItemId, s.StepOrder, d.Name
OPTION (MAXRECURSION 100);
