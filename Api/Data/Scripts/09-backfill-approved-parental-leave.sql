/*
================================================================================
 補登一張「已核准」的育嬰留職停薪請假單（含簽核足跡）
================================================================================

 背景
 ----
 劉怡婷（發展二部）2026-03-16 ~ 2026-09-15 實際請了育嬰留職停薪並已用紙本
 核准，但系統裡完全沒有這張單。年資／特休扣除、出缺勤報表、育嬰 730 天額度
 都以 LeaveRequests 為唯一來源，缺這張單等於整段留停在系統中不存在。

 為什麼不走前台補登
 ------------------
 系統其實允許從前台補登過去日期的請假（前後端都沒有「起始日 >= 今天」的
 檢查，唯一下界是 RequestDateGuard 的今日 −3 年，且請假重疊驗證不看打卡）。
 但那需要三位主管為一件早已核准的紙本申請重新簽一次，故改以本腳本直接補
 一張 approved 單，並一併補上 ApprovalRecords / RequestDesignatedReviewers，
 讓詳情頁的簽核時間軸與正常流程一致。

 範圍
 ----
 只寫一張單（參數區指定的員工 + 假別 + 起迄）。不碰任何既有資料。

 寫入內容
 --------
 ① LeaveRequests            1 列，ApprovalStatus = 'approved'
    · EndDate 必須是 23:59（EndOfDay）—— 寫 00:00 會讓最後一天的打卡阻擋
      與重疊檢查失效（見 LeaveRequestHandler.cs 的日期正規化）
    · Hours = 日曆天 × 8。parental_leave 不在 WorkingDayLeaveTypes，
      六日與國定假日一律計入（LeaveDayExpander.cs）
    · ChildBirthDate 是「每名子女 730 天」的分組鍵，不寫則 parental-quota
      永遠算不到這張單
    · CurrentStepOrder = 流程最後一關（核准後不歸零）
 ② ApprovalRecords          每個生效關卡一列，Action = 'approved'
 ③ RequestDesignatedReviewers  指定審核步驟一列，Status = 'approved'
 不寫 EscalationOverrides：核准當下本來就會被刪除，已核准的單不該有。

 冪等
 ----
 同員工 + 同假別 + 同起迄且非 rejected 的單已存在 → 印訊息並中止，可安全重跑。

 環境無關
 --------
 不寫死任何 Id。員工與 Step1 指定審核者一律以 Email 解析（Email 有 unique
 index 且純 ASCII）；簽核流程依申請人部門沿 ParentId 往上解析（同
 ApprovalFlowService.ResolveApprovalItemIdAsync 的優先序：自身部門 > 最近
 祖先 > 通用預設）；固定關卡的審核者依該關的 部門 + 職稱 解析，並排除
 @example.com 測試帳號與 Superadmin。整份為單一 batch、不含 GO。

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。

 docker cp Api/Data/Scripts/09-backfill-approved-parental-leave.sql sqlserver:/tmp/09.sql

 -- 本機
 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P 'Strong@Password123' -d JabezDb -C -N -u -i /tmp/09.sql
 -- 正式站
 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S weyprous.database.windows.net,1433 -U wadmin -P '!QW@@WE#3er4' \
   -d JabezDb -C -N -u -i /tmp/09.sql

 空跑要確認四件事：
   ① 「閘門異常」區塊 0 列（員工／子女年齡／重疊／額度／審核者／單號）
   ② 主報表的 天數 = 184、時數 = 1472.0、單號格式 LV-yyyyMMdd-NNN
   ③ 簽核關卡報表：每一關都解析到「恰好 1 位」且是預期的人名
      （Step1 張釉棱 / Step2 陳婉婷 執行長 / Step3 蔡志堅 總監）
   ④ 沒有出現測試帳號（執行長測試 / 總監測試 / 發二部協理測試d2）

 ⚠ 跑完之後：
   · 年資會扣除 184 天 → 特休額度隨之下降
   · 出缺勤報表 3/16~9/15 每天（含六日）多一列「育嬰留職停薪」請假列
   · 該區間內她無法打上下班卡
   · 育嬰額度 UsedDays +184（該名子女剩 546 天）
   · 薪資：她目前 BaseSalary 為 NULL，不影響任何已發金額；日後若補了底薪，
     3~9 月會套用「整月留停排除 / 按在職天數 ÷30 折減」規則
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入

-- ---------------------------------------------------------------------------
-- 參數
-- ---------------------------------------------------------------------------
DECLARE @EmployeeEmail      nvarchar(200)  = N'evtair2619@jacreative.com.tw';  -- 劉怡婷
DECLARE @Step1ReviewerEmail nvarchar(200)  = N'yuzi@jacreative.com.tw';        -- 張釉棱（發展二部協理）
DECLARE @LeaveType          nvarchar(30)   = N'parental_leave';                -- 長期留停（連續日曆天）
DECLARE @StartDate          date           = '2026-03-16';
DECLARE @EndDate            date           = '2026-09-15';
DECLARE @ChildBirthDate     date           = '2025-10-08';
DECLARE @ContinueInsurance  bit            = 1;                                -- 留停期間續保
DECLARE @SubmittedAt        datetime2(7)   = '2026-03-16T09:00:00';            -- 申請（送簽）日
DECLARE @Reason             nvarchar(500)  = N'育嬰留職停薪（補登紙本已核准申請）';
DECLARE @ReviewNote         nvarchar(1000) = N'補登：紙本已核准之育嬰留職停薪';

-- ---------------------------------------------------------------------------
-- 推導值
-- ---------------------------------------------------------------------------
DECLARE @StartDt datetime2(7) = CONVERT(datetime2(7), @StartDate);
DECLARE @EndDt   datetime2(7) = DATEADD(minute, 23 * 60 + 59, CONVERT(datetime2(7), @EndDate));
DECLARE @Days    int          = DATEDIFF(day, @StartDate, @EndDate) + 1;
DECLARE @Hours   decimal(5,1) = @Days * 8.0;

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- ① 申請人：以 Email 解析，必須恰好 1 人
-- ---------------------------------------------------------------------------
DECLARE @EmpCount int = (SELECT COUNT(*) FROM Users WHERE Email = @EmployeeEmail);

IF @EmpCount <> 1
BEGIN
    SELECT N'申請人 Email 解析異常' AS Issue, @EmployeeEmail AS Email, @EmpCount AS MatchedRows;
    PRINT N'⛔ 申請人 Email 未解析到恰好 1 人，整份中止。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

DECLARE @EmployeeId   uniqueidentifier;
DECLARE @EmployeeName nvarchar(100);
DECLARE @DepartmentId int;
DECLARE @EmpStatus    nvarchar(20);
DECLARE @HireDate     datetime2(7);

SELECT @EmployeeId   = u.Id,
       @EmployeeName = u.Name,
       @DepartmentId = u.DepartmentId,
       @EmpStatus    = u.Status,
       @HireDate     = u.HireDate
FROM Users u
WHERE u.Email = @EmployeeEmail;

IF @EmpStatus <> N'active' OR @HireDate IS NULL OR @DepartmentId IS NULL
BEGIN
    SELECT N'申請人狀態異常' AS Issue, @EmployeeName AS Name, @EmpStatus AS Status,
           @HireDate AS HireDate, @DepartmentId AS DepartmentId;
    PRINT N'⛔ 申請人非在職／未設到職日／未設部門，整份中止。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- ② 冪等：同員工 + 同假別 + 同起迄的單已存在就跳過
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM LeaveRequests lr
           WHERE lr.EmployeeId = @EmployeeId
             AND lr.LeaveType  = @LeaveType
             AND CONVERT(date, lr.StartDate) = @StartDate
             AND CONVERT(date, lr.EndDate)   = @EndDate
             AND lr.ApprovalStatus <> N'rejected')
BEGIN
    SELECT N'已補登過（跳過）' AS [Action], lr.Id, lr.RequestNo, lr.ApprovalStatus,
           lr.Hours, CONVERT(date, lr.StartDate) AS StartDate, CONVERT(date, lr.EndDate) AS EndDate
    FROM LeaveRequests lr
    WHERE lr.EmployeeId = @EmployeeId
      AND lr.LeaveType  = @LeaveType
      AND CONVERT(date, lr.StartDate) = @StartDate
      AND CONVERT(date, lr.EndDate)   = @EndDate
      AND lr.ApprovalStatus <> N'rejected';
    PRINT N'ℹ️ 這張單已經存在，不重複補登（腳本可安全重跑）。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- ③ 子女年齡：起迄日皆須早於子女 3 歲生日（CheckParentalEligibilityAsync 規則）
-- ---------------------------------------------------------------------------
DECLARE @ThirdBirthday date = DATEADD(year, 3, @ChildBirthDate);

IF @ChildBirthDate IS NULL OR @StartDt >= CONVERT(datetime2(7), @ThirdBirthday)
                            OR @EndDt   >= CONVERT(datetime2(7), @ThirdBirthday)
BEGIN
    SELECT N'子女年齡不符' AS Issue, @ChildBirthDate AS ChildBirthDate,
           @ThirdBirthday AS ThirdBirthday, @StartDate AS StartDate, @EndDate AS EndDate;
    PRINT N'⛔ 育嬰留職停薪須於子女滿 3 歲前結束，整份中止。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- ④ 重疊：[StartDate, EndDate) 內不得有其他 draft / pending / approved 假單
--    （同 LeaveRequestReadService.GetOverlappingRequestsAsync 的半開區間）
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM LeaveRequests lr
           WHERE lr.EmployeeId = @EmployeeId
             AND lr.ApprovalStatus IN (N'draft', N'pending', N'approved')
             AND lr.StartDate < @EndDt
             AND lr.EndDate   > @StartDt)
BEGIN
    SELECT N'區間重疊' AS Issue, lr.Id, lr.RequestNo, lr.LeaveType, lr.ApprovalStatus,
           CONVERT(date, lr.StartDate) AS StartDate, CONVERT(date, lr.EndDate) AS EndDate
    FROM LeaveRequests lr
    WHERE lr.EmployeeId = @EmployeeId
      AND lr.ApprovalStatus IN (N'draft', N'pending', N'approved')
      AND lr.StartDate < @EndDt
      AND lr.EndDate   > @StartDt;
    PRINT N'⛔ 該區間已有其他假單，整份中止（請先人工判讀上表）。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- ⑤ 額度：該名子女（兩個育嬰假別併計）approved + pending 合計不得超過 730 天
-- ---------------------------------------------------------------------------
DECLARE @UsedDays decimal(18,2) = ISNULL((
    SELECT SUM(lr.Hours) / 8.0
    FROM LeaveRequests lr
    WHERE lr.EmployeeId = @EmployeeId
      AND lr.LeaveType IN (N'parental_leave', N'parental_leave_daily')
      AND lr.ChildBirthDate = @ChildBirthDate
      AND lr.ApprovalStatus IN (N'approved', N'pending')), 0);

IF @UsedDays + @Days > 730
BEGIN
    SELECT N'育嬰額度不足' AS Issue, @UsedDays AS UsedDays, @Days AS ThisRequestDays,
           730 AS TotalDays;
    PRINT N'⛔ 該名子女的 730 天額度不足，整份中止。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- ⑥ 簽核流程：依申請人部門沿 ParentId 往上，自身部門 > 最近祖先 > 通用預設
-- ---------------------------------------------------------------------------
DECLARE @Chain TABLE (DepartmentId int PRIMARY KEY, Lvl int NOT NULL);

WITH chain AS (
    SELECT d.Id, d.ParentId, 0 AS Lvl
    FROM Departments d
    WHERE d.Id = @DepartmentId
    UNION ALL
    SELECT p.Id, p.ParentId, c.Lvl + 1
    FROM Departments p
    JOIN chain c ON p.Id = c.ParentId
)
INSERT INTO @Chain (DepartmentId, Lvl)
SELECT Id, Lvl FROM chain;

DECLARE @ApprovalItemId int = (
    SELECT TOP 1 ai.Id
    FROM ApprovalItems ai
    LEFT JOIN @Chain c ON c.DepartmentId = ai.DepartmentId
    WHERE ai.ApplicationType = N'leave'
      AND ai.IsActive = 1
      AND (ai.DepartmentId IS NULL OR c.DepartmentId IS NOT NULL)
    ORDER BY CASE WHEN ai.DepartmentId IS NULL THEN 9999 ELSE c.Lvl END, ai.Id
);

IF @ApprovalItemId IS NULL
BEGIN
    PRINT N'⛔ 找不到適用的請假簽核流程（ApprovalItems.ApplicationType = leave），整份中止。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- ⑦ 生效關卡與審核者（MinDays 高於本單天數者不納入）
-- ---------------------------------------------------------------------------
DECLARE @Steps TABLE (
    StepOrder      int PRIMARY KEY,
    IsDesignated   bit NOT NULL,
    DeptId         int NULL,
    JobTitleId     int NULL,
    ReviewerId     uniqueidentifier NULL,
    CandidateCount int NOT NULL DEFAULT 0,
    ReviewedAt     datetime2(7) NOT NULL
);

INSERT INTO @Steps (StepOrder, IsDesignated, DeptId, JobTitleId, ReviewerId, CandidateCount, ReviewedAt)
SELECT s.StepOrder,
       s.UseApplicantDesignated,
       s.DepartmentId,
       s.JobTitleId,
       NULL,
       0,
       DATEADD(hour, s.StepOrder, @SubmittedAt)
FROM ApprovalSteps s
WHERE s.ApprovalItemId = @ApprovalItemId
  AND (s.MinDays IS NULL OR @Days >= s.MinDays);

-- 指定審核步驟 → 參數指定的人（以 Email 解析，須為在職非測試帳號）
DECLARE @Step1ReviewerCount int = (
    SELECT COUNT(*) FROM Users u
    WHERE u.Email = @Step1ReviewerEmail AND u.Status = N'active'
      AND u.IsSuperAdmin = 0 AND u.Email NOT LIKE N'%@example.com');

DECLARE @Step1ReviewerId uniqueidentifier = (
    SELECT MIN(u.Id) FROM Users u
    WHERE u.Email = @Step1ReviewerEmail AND u.Status = N'active'
      AND u.IsSuperAdmin = 0 AND u.Email NOT LIKE N'%@example.com');

UPDATE @Steps
SET ReviewerId     = @Step1ReviewerId,
    CandidateCount = @Step1ReviewerCount
WHERE IsDesignated = 1;

-- 固定關卡（部門 + 職稱）→ 該部門該職稱的在職者，須恰好 1 位
UPDATE st
SET ReviewerId     = x.OnlyId,
    CandidateCount = x.Cnt
FROM @Steps st
CROSS APPLY (
    SELECT COUNT(*) AS Cnt, MIN(u.Id) AS OnlyId
    FROM Users u
    WHERE u.DepartmentId = st.DeptId
      AND u.JobTitleId   = st.JobTitleId
      AND u.Status       = N'active'
      AND u.IsSuperAdmin = 0
      AND u.Email NOT LIKE N'%@example.com'
) x
WHERE st.IsDesignated = 0 AND st.DeptId IS NOT NULL AND st.JobTitleId IS NOT NULL;

-- 關卡報表（逐關核對人名）
SELECT st.StepOrder,
       CASE WHEN st.IsDesignated = 1 THEN N'指定審核' ELSE N'固定關卡（部門＋職稱）' END AS StepKind,
       ISNULL(d.Name, N'—') AS Department,
       ISNULL(j.Name, N'—') AS JobTitle,
       ISNULL(u.Name, N'（未解析）') AS Reviewer,
       ISNULL(u.Email, N'—') AS ReviewerEmail,
       st.CandidateCount,
       st.ReviewedAt
FROM @Steps st
LEFT JOIN Departments d ON d.Id = st.DeptId
LEFT JOIN JobTitles   j ON j.Id = st.JobTitleId
LEFT JOIN Users       u ON u.Id = st.ReviewerId
ORDER BY st.StepOrder;

IF NOT EXISTS (SELECT 1 FROM @Steps)
   OR EXISTS (SELECT 1 FROM @Steps WHERE ReviewerId IS NULL OR CandidateCount <> 1)
BEGIN
    SELECT N'審核者解析異常' AS Issue, st.StepOrder, st.CandidateCount,
           CASE WHEN st.IsDesignated = 1 THEN N'指定審核' ELSE N'固定關卡' END AS StepKind
    FROM @Steps st
    WHERE st.ReviewerId IS NULL OR st.CandidateCount <> 1;
    PRINT N'⛔ 有關卡未解析到恰好 1 位在職審核者（或流程無任何生效關卡），整份中止。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- ⑧ 取號：LV-yyyyMMdd-NNN，日期取送簽日、流水號為當日最大值 + 1
-- ---------------------------------------------------------------------------
DECLARE @Prefix    nvarchar(20) = N'LV-' + CONVERT(nvarchar(8), @SubmittedAt, 112) + N'-';
DECLARE @Seq       int          = ISNULL((SELECT MAX(CONVERT(int, RIGHT(lr.RequestNo, 3)))
                                          FROM LeaveRequests lr
                                          WHERE lr.RequestNo LIKE @Prefix + N'[0-9][0-9][0-9]'), 0) + 1;
DECLARE @RequestNo nvarchar(50) = @Prefix + RIGHT(N'000' + CONVERT(nvarchar(10), @Seq), 3);

IF EXISTS (SELECT 1 FROM LeaveRequests lr WHERE lr.RequestNo = @RequestNo)
BEGIN
    SELECT N'單號衝突' AS Issue, @RequestNo AS RequestNo;
    PRINT N'⛔ 取號撞到既有單號，整份中止。';
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- ⑨ 主報表：這次會補進去的單長什麼樣
-- ---------------------------------------------------------------------------
DECLARE @LastStep        int              = (SELECT MAX(StepOrder) FROM @Steps);
DECLARE @FinalReviewerId uniqueidentifier = (SELECT ReviewerId FROM @Steps WHERE StepOrder = @LastStep);
DECLARE @FinalAt         datetime2(7)     = (SELECT ReviewedAt  FROM @Steps WHERE StepOrder = @LastStep);

SELECT N'補登已核准請假單' AS [Action],
       @RequestNo          AS RequestNo,
       @EmployeeName       AS Employee,
       d.Name              AS Department,
       @LeaveType          AS LeaveType,
       @StartDate          AS StartDate,
       @EndDate            AS EndDate,
       @Days               AS [天數],
       @Hours              AS [時數],
       @ChildBirthDate     AS ChildBirthDate,
       @ContinueInsurance  AS ContinueInsurance,
       @ApprovalItemId     AS ApprovalItemId,
       @LastStep           AS CurrentStepOrder,
       fr.Name             AS [末關核准者],
       @FinalAt            AS [核准時間],
       @UsedDays           AS [該名子女已用天數（本次之前）]
FROM Departments d
LEFT JOIN Users fr ON fr.Id = @FinalReviewerId
WHERE d.Id = @DepartmentId;

-- ---------------------------------------------------------------------------
-- ⑩ 寫入
-- ---------------------------------------------------------------------------
INSERT INTO LeaveRequests
    (EmployeeId, ApprovalItemId, LeaveType, StartDate, EndDate, Hours, Reason,
     ApprovalStatus, ReviewedById, ReviewedAt, ReviewNote, CreatedAt,
     CurrentStepOrder, ChildBirthDate, ContinueInsurance, SubmittedAt, RequestNo)
VALUES
    (@EmployeeId, @ApprovalItemId, @LeaveType, @StartDt, @EndDt, @Hours, @Reason,
     N'approved', @FinalReviewerId, @FinalAt, @ReviewNote, @SubmittedAt,
     @LastStep, @ChildBirthDate, @ContinueInsurance, @SubmittedAt, @RequestNo);

DECLARE @LeaveId int = CONVERT(int, SCOPE_IDENTITY());

INSERT INTO ApprovalRecords
    (ApplicationType, ApplicationId, StepOrder, Action, ReviewedById, ReviewedAt,
     ReviewNote, IsEscalated, RoundNo)
SELECT N'leave', @LeaveId, st.StepOrder, N'approved', st.ReviewerId, st.ReviewedAt,
       @ReviewNote, 0, 1
FROM @Steps st;

DECLARE @RecordRows int = @@ROWCOUNT;

INSERT INTO RequestDesignatedReviewers
    (RequestType, RequestId, ReviewerId, StepOrder, Status, ReviewedAt, Comment,
     CreatedAt, ApprovalStepOrder, SelectedDepartmentId)
SELECT N'leave', @LeaveId, st.ReviewerId, 1, N'approved', st.ReviewedAt, NULL,
       @SubmittedAt, st.StepOrder, NULL
FROM @Steps st
WHERE st.IsDesignated = 1;

DECLARE @DesignatedRows int = @@ROWCOUNT;

-- ---------------------------------------------------------------------------
-- ⑪ 摘要與寫入後回查（空跑時一併顯示，跑完會 ROLLBACK）
-- ---------------------------------------------------------------------------
SELECT N'LeaveRequests' AS [Table], 1 AS Rows
UNION ALL SELECT N'ApprovalRecords',             @RecordRows
UNION ALL SELECT N'RequestDesignatedReviewers',  @DesignatedRows;

SELECT lr.Id, lr.RequestNo, u.Name AS Employee, lr.LeaveType, lr.StartDate, lr.EndDate,
       lr.Hours, lr.ApprovalStatus, lr.CurrentStepOrder, lr.ApprovalItemId,
       lr.ChildBirthDate, lr.ContinueInsurance, lr.SubmittedAt, lr.CreatedAt,
       rv.Name AS ReviewedBy, lr.ReviewedAt
FROM LeaveRequests lr
JOIN Users u       ON u.Id  = lr.EmployeeId
LEFT JOIN Users rv ON rv.Id = lr.ReviewedById
WHERE lr.Id = @LeaveId;

SELECT ar.StepOrder, ar.Action, u.Name AS ReviewedBy, ar.ReviewedAt, ar.RoundNo
FROM ApprovalRecords ar
LEFT JOIN Users u ON u.Id = ar.ReviewedById
WHERE ar.ApplicationType = N'leave' AND ar.ApplicationId = @LeaveId
ORDER BY ar.StepOrder;

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
