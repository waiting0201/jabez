/*
================================================================================
 修正：卡在「上層級」關卡但查無可簽核人員的請假單 → 推進到下一個有人可簽的關卡
================================================================================

 問題
 ----
 上層級關卡（UseDirectSupervisor）的審核者＝「申請人同部門、active、職級高於申請人」
 之第 N 近層級。送單當下有人可簽、之後申請人升遷（或該層級的人離職 / 轉調）時，
 這一關就變成沒有任何人通得過 AuthorizeStepAsync —— 單子停在該關，誰的待審清單都撈
 不到，簽核流程畫面上也只印「審核中…」而沒有任何人名。

 實例：陳婉婷（營運管理及發展部 / 執行長）2026-08-03 送出的請假單停在 Step2「上層級」，
 該部門已無高於執行長者 → 0 位候選人。

 修法
 ----
 把這類單推進到「下一個有人可簽的固定關卡」（綁部門 + 職稱，含 UseApplicantDepartment），
 等同引擎本來就會做的「上層級無人 → 跳過該關」。刻意跳過以下關卡，不當作安全落點：
   · MinDays > 申請天數（Hours/8）的關卡 —— 這張單根本不走這關
   · 指定審核 / 上層級關卡           —— 人選臨時決定或同樣可能無人，不保證有人接手
 找不到安全落點的單一律不動（印出後交人工判斷），不會自動核准。

 被跳過的關卡不留 ApprovalRecord，簽核流程時間軸會顯示「已跳過」。
 本腳本不發通知，推進後請自行告知新的審核者。

 ⚠ 一律以條件比對定位，不寫死 Id / 姓名；各環境可直接套用。

 用法
 ----
 1) 先以 @Commit = 0 執行（空跑）：只印出「卡住的單 / 預計推進到哪一關」，最後 ROLLBACK。
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

-- ── 1. 找出 pending 且目前停在「上層級」關卡的請假單 ─────────────────────────────
DECLARE @Stuck TABLE (
    LeaveId        int PRIMARY KEY,
    EmployeeId     uniqueidentifier,
    DepartmentId   int NULL,
    ApplicantLevel int NULL,
    ApprovalItemId int,
    CurrentStep    int,
    RequestDays    decimal(9,2),
    NextStep       int NULL
);

INSERT INTO @Stuck (LeaveId, EmployeeId, DepartmentId, ApplicantLevel, ApprovalItemId, CurrentStep, RequestDays)
SELECT l.Id, l.EmployeeId, u.DepartmentId, jt.Level, l.ApprovalItemId, l.CurrentStepOrder, l.Hours / 8.0
FROM LeaveRequests l
JOIN Users u          ON u.Id  = l.EmployeeId
LEFT JOIN JobTitles jt ON jt.Id = u.JobTitleId
JOIN ApprovalSteps s  ON s.ApprovalItemId = l.ApprovalItemId
                     AND s.StepOrder      = l.CurrentStepOrder
                     AND s.UseDirectSupervisor = 1
WHERE l.ApprovalStatus = 'pending'
  -- 已指名升級接手者的不算卡住（該員簽得到）
  AND NOT EXISTS (
        SELECT 1 FROM EscalationOverrides eo
        WHERE eo.ApplicationType = 'leave'
          AND eo.ApplicationId   = l.Id
          AND eo.StepOrder       = l.CurrentStepOrder)
  -- 該關的第 N 近上層級（N = 此步驟前的上層級關卡數）不存在 → 0 位候選人
  AND NOT EXISTS (
        SELECT 1
        FROM (
            SELECT DISTINCT jt2.Level,
                   ROW_NUMBER() OVER (ORDER BY jt2.Level DESC) AS rn
            FROM Users u2
            JOIN JobTitles jt2 ON jt2.Id = u2.JobTitleId
            WHERE u2.DepartmentId = u.DepartmentId
              AND jt2.Level  < jt.Level
              AND u2.Status  = 'active'
              AND u2.IsSuperAdmin = 0
        ) ranked
        WHERE ranked.rn = (
            SELECT COUNT(*) + 1 FROM ApprovalSteps prev
            WHERE prev.ApprovalItemId    = s.ApprovalItemId
              AND prev.UseDirectSupervisor = 1
              AND prev.StepOrder         < s.StepOrder));

-- ── 2. 為每張單找「下一個有人可簽的固定關卡」 ──────────────────────────────────
UPDATE st
SET NextStep = (
    SELECT MIN(s2.StepOrder)
    FROM ApprovalSteps s2
    WHERE s2.ApprovalItemId = st.ApprovalItemId
      AND s2.StepOrder      > st.CurrentStep
      AND s2.UseDirectSupervisor  = 0
      AND s2.UseApplicantDesignated = 0
      AND (s2.MinDays IS NULL OR st.RequestDays >= s2.MinDays)
      AND EXISTS (
            SELECT 1 FROM Users ru
            WHERE ru.DepartmentId = CASE WHEN s2.UseApplicantDepartment = 1
                                         THEN st.DepartmentId ELSE s2.DepartmentId END
              AND ru.Status = 'active'
              AND ru.IsSuperAdmin = 0
              AND ru.Id <> st.EmployeeId
              AND (s2.JobTitleId IS NULL OR ru.JobTitleId = s2.JobTitleId)))
FROM @Stuck st;

PRINT N'===== 卡住的請假單（上層級關卡查無可簽核人員） =====';
SELECT st.LeaveId, u.Name AS 申請人, d.Name AS 部門, jt.Name AS 職稱,
       st.CurrentStep AS 目前關卡, st.RequestDays AS 申請天數,
       st.NextStep    AS 預計推進到,
       CASE WHEN st.NextStep IS NULL THEN N'找不到有人可簽的關卡 → 本次不動，請人工處理'
            ELSE CONCAT(N'Step', st.CurrentStep, N' → Step', st.NextStep) END AS 處理方式
FROM @Stuck st
JOIN Users u           ON u.Id  = st.EmployeeId
LEFT JOIN Departments d ON d.Id = u.DepartmentId
LEFT JOIN JobTitles jt  ON jt.Id = u.JobTitleId
ORDER BY st.LeaveId;

-- ── 3. 推進 ────────────────────────────────────────────────────────────────────
UPDATE l
SET CurrentStepOrder = st.NextStep
FROM LeaveRequests l
JOIN @Stuck st ON st.LeaveId = l.Id
WHERE st.NextStep IS NOT NULL;

DECLARE @Changed int = @@ROWCOUNT;

PRINT N'';
PRINT N'===== 改動後 =====';
SELECT l.Id AS LeaveId, u.Name AS 申請人, l.ApprovalStatus, l.CurrentStepOrder,
       s.StepOrder, ISNULL(d2.Name, N'(依申請人部門)') AS 關卡部門, j2.Name AS 關卡職稱, s.MinDays
FROM LeaveRequests l
JOIN @Stuck st         ON st.LeaveId = l.Id AND st.NextStep IS NOT NULL
JOIN Users u           ON u.Id = l.EmployeeId
JOIN ApprovalSteps s   ON s.ApprovalItemId = l.ApprovalItemId AND s.StepOrder = l.CurrentStepOrder
LEFT JOIN Departments d2 ON d2.Id = s.DepartmentId
LEFT JOIN JobTitles j2   ON j2.Id = s.JobTitleId
ORDER BY l.Id;

PRINT N'';
IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT CONCAT(N'已提交，推進的請假單數：', @Changed);
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT CONCAT(N'空跑完成（未寫入），預計推進的請假單數：', @Changed,
                 N'。確認上方結果無誤後，把 @Commit 改成 1 重跑。');
END
