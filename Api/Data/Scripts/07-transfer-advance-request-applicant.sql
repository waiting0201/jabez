/*
================================================================================
 移轉預支申請單的申請人：代錄帳號 → 實際員工
================================================================================

 背景
 ----
 2026-09-07 這批歷史預支申請單是由代錄帳號 Charles（行政財務管理部 / 系統維管）
 統一登打進系統，SubmittedById 全部掛在他名下，實際上分屬 12 位員工。
 後果是當事人在「我的預支申請」看不到自己的單（清單可見性就是
 WHERE SubmittedById = @UserId），也無法對尚未沖銷的母單開立沖銷單
 （WriteOffRequestHandler.CreateAsync 要求母單 SubmittedById = 呼叫者）。

 目標
 ----
 29 張預支單（AdvanceRequests）＋ 其沖銷子單（WriteOffRecords）的
 **SubmittedById** 由代錄帳號改成實際員工。沖銷子單一律繼承母單的新申請人。

 只改一個欄位
 ------------
 兩張表都**沒有 UpdatedAt**，所以 UPDATE 只碰 SubmittedById 一欄。

 以下欄位都不是「申請人」，一律不動：
   ReviewedById / ClosedById / RefundedByUserId（審核者・結案者・退款登記者）
   AdvanceRequestInstallments.PaidByUserId / WriteOffInstallments.PaidByUserId（出納）
   WriteOffItems.CheckPaidById / AdvanceRequestSupplements.CreatedById

 改完會立刻生效的（讀取端即時 JOIN Users）
 ------------------------------------------
   · 清單可見性 WHERE SubmittedById = @UserId（當事人才看得到自己的單）
   · PDF 的申請人姓名與簽名章（即時渲染，改完重新下載即為新申請人）
   · 款項統計報表的部門 scope（INNER JOIN Users + u.DepartmentId IN @AllowedDeptIds）
   · 簽核 / 撥款通知的收件人
   · 待審清單 StepMatchClause 的 UseApplicantDepartment / UseDirectSupervisor 比對

 刻意不重算的（送簽當下的快照）
 ------------------------------
   ApprovalItemId（依代錄帳號部門＝行政財務管理部解析出的流程）、RequestNo、
   SubmittedAt、CurrentStepOrder，以及三張多型足跡表
   ApprovalRecords / EscalationOverrides / RequestDesignatedReviewers。
   已核准單屬歷史紀錄；唯一仍在簽核中的 WO-20260908-002 剩下的是固定關卡
   （總監室／總監），不看申請人是誰，故換人不影響誰能簽。

 ⚠ pending 單的陷阱（閘門 7 的由來）
 ------------------------------------
   ApprovalFlowService.ResolveReviewerPoolAsync 的三個分支都帶 u.Id != applicant.Id，
   亦即**審核者池會排除申請人本人**。若新申請人剛好是目前關卡的唯一候選人，
   改完那張單就永遠沒人能簽（＝ 05 腳本在救的狀態）。故 pending 單一律先檢查。

 環境無關
 --------
 全程以 RequestNo 與 Users.Email 定位，**不寫死任何 Id**，同一份可在
 本機 / staging / 正式站跑。整份為單一 batch、不含 GO，可直接貼進
 SSMS / Azure Data Studio 執行。

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。

 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P 'Strong@Password123' -d JabezDb -C -N -u \
   -i /tmp/07.sql

 空跑要確認四件事：
   ① 「找不到的單號」0 列  ② 「新申請人解析異常」0 列
   ③  主報表 29 列、Action 全為「移轉」、新申請人逐列對得上交辦清單
   ④ 「pending 卡死預警」0 列

 ⚠ 本腳本可安全重跑：已移轉過的單會落入 already 而被跳過，不會重複異動。
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit             bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入
DECLARE @AllowForeignHolder bit = 0;   -- 目標單現任申請人是「第三者」時是否照改
DECLARE @AllowInactiveNew   bit = 0;   -- 新申請人非 active 時是否放行
DECLARE @AllowStuckPending  bit = 0;   -- 改完會讓 pending 單目前關卡 0 人可簽時是否放行

DECLARE @FromEmail    nvarchar(200) = N'cherng1217@gmail.com';  -- 代錄帳號 Charles
DECLARE @ExpectedRows int           = 29;                        -- 對照表應有的列數

-- ---------------------------------------------------------------------------
-- 單號 → 新申請人 對照表
--
-- · RequestNo 設 PRIMARY KEY：手抄時把同一單號打兩次會當場撞 PK，
--   不會靜靜地讓後面那列覆蓋前面。
-- · 解析鍵用 Email 不用姓名：Users.Email 有 unique index 且純 ASCII，
--   即使 sqlcmd 讀 UTF-8 檔把中文字面量弄糊，寫進 DB 的內容也不受影響
--   （Linux 版 sqlcmd 沒有 -f codepage 可指定輸入編碼）。
--   NewApplicantName 只供人眼核對與報表顯示，不參與解析。
-- ---------------------------------------------------------------------------
DECLARE @Map TABLE (
    RequestNo         nvarchar(50)  PRIMARY KEY,
    NewApplicantEmail nvarchar(200) NOT NULL,
    NewApplicantName  nvarchar(100) NOT NULL
);

INSERT INTO @Map (RequestNo, NewApplicantEmail, NewApplicantName) VALUES
-- 蔡志堅（總監室 / 總監）
    (N'ADV-20260907-002', N'hank@jacreative.com.tw',       N'蔡志堅'),
    (N'ADV-20260907-003', N'hank@jacreative.com.tw',       N'蔡志堅'),
    (N'ADV-20260907-004', N'hank@jacreative.com.tw',       N'蔡志堅'),
-- 陳婉婷（營運管理及發展部 / 執行長）
    (N'ADV-20260907-007', N'cwting@jacreative.com.tw',     N'陳婉婷'),
-- 陳麗安（總監室專案部門 / 協理）
    (N'ADV-20260907-008', N'lianchen@jacreative.com.tw',   N'陳麗安'),
    (N'ADV-20260907-009', N'lianchen@jacreative.com.tw',   N'陳麗安'),
-- 景薇霖（發展三部 / 經理）
    (N'ADV-20260907-026', N'jingw10322@jacreative.com.tw', N'景薇霖'),
    (N'ADV-20260907-027', N'jingw10322@jacreative.com.tw', N'景薇霖'),
-- 黃敏旻（發展三部）
    (N'ADV-20260907-038', N'minmin305@jacreative.com.tw',  N'黃敏旻'),
    (N'ADV-20260907-039', N'minmin305@jacreative.com.tw',  N'黃敏旻'),
    (N'ADV-20260907-040', N'minmin305@jacreative.com.tw',  N'黃敏旻'),
    (N'ADV-20260907-041', N'minmin305@jacreative.com.tw',  N'黃敏旻'),
    (N'ADV-20260907-042', N'minmin305@jacreative.com.tw',  N'黃敏旻'),
    (N'ADV-20260907-071', N'minmin305@jacreative.com.tw',  N'黃敏旻'),
-- 劉闓毅（發展三部）
    (N'ADV-20260907-044', N'kenny@jacreative.com.tw',      N'劉闓毅'),
    (N'ADV-20260907-045', N'kenny@jacreative.com.tw',      N'劉闓毅'),
    (N'ADV-20260907-047', N'kenny@jacreative.com.tw',      N'劉闓毅'),
-- 包郁安（發展一部）
    (N'ADV-20260907-056', N'bao@jacreative.com.tw',        N'包郁安'),
-- 高蘇貞瑋（發展三部 / 經理）
    (N'ADV-20260907-057', N'yavaus@jacreative.com.tw',     N'高蘇貞瑋'),
-- 謝儀萱（發展三部）
    (N'ADV-20260907-058', N'shellybala@jacreative.com.tw', N'謝儀萱'),
    (N'ADV-20260907-059', N'shellybala@jacreative.com.tw', N'謝儀萱'),
    (N'ADV-20260907-060', N'shellybala@jacreative.com.tw', N'謝儀萱'),
    (N'ADV-20260907-061', N'shellybala@jacreative.com.tw', N'謝儀萱'),
-- 張雅婷（發展一部 / 經理）── -065 的沖銷子單 WO-20260908-002 仍在簽核中，一併移轉
    (N'ADV-20260907-062', N'tin@jacreative.com.tw',        N'張雅婷'),
    (N'ADV-20260907-063', N'tin@jacreative.com.tw',        N'張雅婷'),
    (N'ADV-20260907-064', N'tin@jacreative.com.tw',        N'張雅婷'),
    (N'ADV-20260907-065', N'tin@jacreative.com.tw',        N'張雅婷'),
-- 楊雪（發展三部）
    (N'ADV-20260907-066', N'ice919@jacreative.com.tw',     N'楊雪'),
-- 石佳品（發展三部）
    (N'ADV-20260907-067', N'abby.shih77@jacreative.com.tw', N'石佳品');

BEGIN TRANSACTION;

DECLARE @FromUserId uniqueidentifier = (SELECT Id FROM Users WHERE Email = @FromEmail);

-- ---------------------------------------------------------------------------
-- A) 解析新申請人（Email → UserId）
--    LEFT JOIN + COUNT 一次同時抓出「找不到」(Hits = 0) 與「撞名」(Hits > 1)。
--    Hits <> 1 一律被閘門 4 擋下，故 MIN(...) 的值不會被實際使用。
-- ---------------------------------------------------------------------------
DECLARE @NewUsers TABLE (
    Email  nvarchar(200)    PRIMARY KEY,
    Hits   int              NOT NULL,
    UserId uniqueidentifier NULL,
    Name   nvarchar(100)    NULL,
    Dept   nvarchar(100)    NULL,
    Status nvarchar(20)     NULL
);

INSERT INTO @NewUsers (Email, Hits, UserId, Name, Dept, Status)
SELECT  m.NewApplicantEmail,
        COUNT(u.Id),
        MIN(u.Id), MIN(u.Name), MIN(d.Name), MIN(u.Status)
FROM   (SELECT DISTINCT NewApplicantEmail FROM @Map) m
LEFT JOIN Users       u ON u.Email = m.NewApplicantEmail
LEFT JOIN Departments d ON d.Id    = u.DepartmentId
GROUP BY m.NewApplicantEmail;

-- ---------------------------------------------------------------------------
-- B) 解析母單並分類
--      transfer = 現任申請人是代錄帳號        → 本次改
--      already  = 現任申請人已是目標新申請人  → 跳過（腳本可重跑）
--      conflict = 第三者或 NULL               → 閘門 6 中止
-- ---------------------------------------------------------------------------
DECLARE @Adv TABLE (
    Id             int PRIMARY KEY,
    RequestNo      nvarchar(50)     NOT NULL,
    OldApplicantId uniqueidentifier NULL,
    NewApplicantId uniqueidentifier NOT NULL,
    [Action]       varchar(10)      NOT NULL
);

INSERT INTO @Adv (Id, RequestNo, OldApplicantId, NewApplicantId, [Action])
SELECT  a.Id, a.RequestNo, a.SubmittedById, n.UserId,
        CASE WHEN a.SubmittedById = @FromUserId THEN 'transfer'
             WHEN a.SubmittedById = n.UserId    THEN 'already'
             ELSE 'conflict' END
FROM AdvanceRequests a
JOIN @Map      m ON m.RequestNo         = a.RequestNo
JOIN @NewUsers n ON n.Email             = m.NewApplicantEmail
WHERE n.UserId IS NOT NULL;   -- Email 解析失敗者交由閘門 4 處理

-- ---------------------------------------------------------------------------
-- C) 沖銷子單（新申請人直接繼承母單，母子申請人必須同一人）
-- ---------------------------------------------------------------------------
DECLARE @WriteOff TABLE (
    Id               int PRIMARY KEY,
    RequestNo        nvarchar(50)     NULL,
    AdvanceRequestId int              NOT NULL,
    AdvRequestNo     nvarchar(50)     NOT NULL,
    ApprovalStatus   nvarchar(20)     NOT NULL,
    CurrentStepOrder int              NOT NULL,
    ApprovalItemId   int              NULL,
    OldApplicantId   uniqueidentifier NULL,
    NewApplicantId   uniqueidentifier NOT NULL,
    [Action]         varchar(10)      NOT NULL
);

INSERT INTO @WriteOff (Id, RequestNo, AdvanceRequestId, AdvRequestNo, ApprovalStatus,
                       CurrentStepOrder, ApprovalItemId, OldApplicantId, NewApplicantId, [Action])
SELECT  w.Id, w.RequestNo, w.AdvanceRequestId, a.RequestNo, w.ApprovalStatus,
        w.CurrentStepOrder, w.ApprovalItemId, w.SubmittedById, a.NewApplicantId,
        CASE WHEN w.SubmittedById = @FromUserId      THEN 'transfer'
             WHEN w.SubmittedById = a.NewApplicantId THEN 'already'
             ELSE 'conflict' END
FROM WriteOffRecords w
JOIN @Adv a ON a.Id = w.AdvanceRequestId;

-- ---------------------------------------------------------------------------
-- D) 基準計數（誤傷偵測用，必須在 UPDATE 之前取）
-- ---------------------------------------------------------------------------
DECLARE @FromAdvBefore int = (SELECT COUNT(*) FROM AdvanceRequests WHERE SubmittedById = @FromUserId);
DECLARE @FromWoBefore  int = (SELECT COUNT(*) FROM WriteOffRecords WHERE SubmittedById = @FromUserId);

-- ---------------------------------------------------------------------------
-- E) 異動前報表
-- ---------------------------------------------------------------------------
PRINT N'=== 找不到的單號（對照表有、資料庫沒有）===';
SELECT t.RequestNo AS [NotFound], t.NewApplicantName AS [對照表指定新申請人]
FROM @Map t
WHERE NOT EXISTS (SELECT 1 FROM AdvanceRequests a WHERE a.RequestNo = t.RequestNo);

PRINT N'=== 新申請人解析異常（Email 對不到人 / 對到多人 / 非在職）===';
SELECT  n.Email, MIN(m.NewApplicantName) AS [對照表姓名], n.Hits, n.Name, n.Dept, n.Status,
        CASE WHEN n.Hits = 0 THEN N'Email 找不到使用者'
             WHEN n.Hits > 1 THEN N'Email 對到多位使用者'
             ELSE N'使用者非 active' END AS [問題]
FROM @NewUsers n
JOIN @Map m ON m.NewApplicantEmail = n.Email
WHERE n.Hits <> 1 OR n.Status <> 'active'
GROUP BY n.Email, n.Hits, n.Name, n.Dept, n.Status;

PRINT N'=== 將被移轉的預支單（一列一張）===';
SELECT  a.Id,
        a.RequestNo,
        r.ApprovalStatus,
        r.IsClosed,
        r.GrandTotal,
        uo.Name AS [舊申請人],
        do.Name AS [舊部門],
        un.Name AS [新申請人],
        dn.Name AS [新部門],
        r.SubmittedAt,
        (SELECT COUNT(*) FROM AdvanceRequestInstallments i WHERE i.AdvanceRequestId = a.Id) AS Inst,
        (SELECT COUNT(*) FROM AdvanceRequestInstallments i WHERE i.AdvanceRequestId = a.Id AND i.PaidAt IS NOT NULL) AS Paid,
        (SELECT COUNT(*) FROM @WriteOff w WHERE w.AdvanceRequestId = a.Id) AS WriteOffs,
        (SELECT COUNT(*) FROM @WriteOff w WHERE w.AdvanceRequestId = a.Id AND w.ApprovalStatus = 'pending') AS PendingWO,
        CASE a.[Action] WHEN 'transfer' THEN N'移轉'
                        WHEN 'already'  THEN N'已是目標申請人→略過'
                        ELSE N'現任申請人非代錄帳號→中止' END AS [Action]
FROM @Adv a
JOIN AdvanceRequests r ON r.Id = a.Id
LEFT JOIN Users       uo ON uo.Id = a.OldApplicantId
LEFT JOIN Departments do ON do.Id = uo.DepartmentId
LEFT JOIN Users       un ON un.Id = a.NewApplicantId
LEFT JOIN Departments dn ON dn.Id = un.DepartmentId
ORDER BY un.Name, a.RequestNo;

PRINT N'=== 將一併移轉的沖銷子單 ===';
SELECT  w.Id, w.RequestNo, w.AdvRequestNo AS [母單], w.ApprovalStatus, w.CurrentStepOrder AS [目前關卡],
        uo.Name AS [舊申請人], un.Name AS [新申請人],
        CASE w.[Action] WHEN 'transfer' THEN N'移轉'
                        WHEN 'already'  THEN N'已是目標申請人→略過'
                        ELSE N'現任申請人非代錄帳號→中止' END AS [Action]
FROM @WriteOff w
LEFT JOIN Users uo ON uo.Id = w.OldApplicantId
LEFT JOIN Users un ON un.Id = w.NewApplicantId
ORDER BY w.AdvRequestNo, w.Id;

-- pending 單：改申請人後目前關卡是否還有人可簽
-- （只查固定池關卡，判準同 BuildLaterFixedStepScopes：排除上層級與指定審核）
DECLARE @Stuck TABLE (Kind varchar(10), Id int, RequestNo nvarchar(50), StepOrder int, NewApplicantName nvarchar(100));

INSERT INTO @Stuck (Kind, Id, RequestNo, StepOrder, NewApplicantName)
SELECT 'advance', a.Id, a.RequestNo, r.CurrentStepOrder, n.Name
FROM @Adv a
JOIN AdvanceRequests r ON r.Id = a.Id
JOIN ApprovalSteps   s ON s.ApprovalItemId = r.ApprovalItemId AND s.StepOrder = r.CurrentStepOrder
JOIN Users           n ON n.Id = a.NewApplicantId
WHERE r.ApprovalStatus = 'pending' AND a.[Action] <> 'already'
  AND s.UseDirectSupervisor = 0 AND s.UseApplicantDesignated = 0
  AND NOT EXISTS (
        SELECT 1 FROM Users ru
        WHERE ru.DepartmentId = CASE WHEN s.UseApplicantDepartment = 1 THEN n.DepartmentId ELSE s.DepartmentId END
          AND ru.Status = 'active' AND ru.IsSuperAdmin = 0
          AND ru.Id <> a.NewApplicantId
          AND (s.JobTitleId IS NULL OR ru.JobTitleId = s.JobTitleId));

INSERT INTO @Stuck (Kind, Id, RequestNo, StepOrder, NewApplicantName)
SELECT 'write_off', w.Id, w.RequestNo, w.CurrentStepOrder, n.Name
FROM @WriteOff w
JOIN ApprovalSteps s ON s.ApprovalItemId = w.ApprovalItemId AND s.StepOrder = w.CurrentStepOrder
JOIN Users         n ON n.Id = w.NewApplicantId
WHERE w.ApprovalStatus = 'pending' AND w.[Action] <> 'already'
  AND s.UseDirectSupervisor = 0 AND s.UseApplicantDesignated = 0
  AND NOT EXISTS (
        SELECT 1 FROM Users ru
        WHERE ru.DepartmentId = CASE WHEN s.UseApplicantDepartment = 1 THEN n.DepartmentId ELSE s.DepartmentId END
          AND ru.Status = 'active' AND ru.IsSuperAdmin = 0
          AND ru.Id <> w.NewApplicantId
          AND (s.JobTitleId IS NULL OR ru.JobTitleId = s.JobTitleId));

PRINT N'=== pending 卡死預警（改完後目前關卡 0 人可簽，應為空）===';
SELECT Kind, Id, RequestNo, StepOrder AS [目前關卡], NewApplicantName AS [新申請人] FROM @Stuck;

-- ---------------------------------------------------------------------------
-- F) 保護閘門（全部排在報表之後，空跑一次就能看到所有問題）
-- ---------------------------------------------------------------------------
DECLARE @N int;

-- 1) 對照表列數不符：只在編輯 @Map 時觸發，少一列＝靜默漏改，事後查不出來
SET @N = (SELECT COUNT(*) FROM @Map);
IF @N <> @ExpectedRows
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'對照表共 %d 列，與 @ExpectedRows 不符，已中止。請確認是否漏抄或多抄單號。', 16, 1, @N);
    RETURN;
END

-- 2) 代錄帳號不存在＝跑錯資料庫
IF @FromUserId IS NULL
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'代錄帳號 Email 在本資料庫查無使用者，已中止（是否跑錯資料庫？）。', 16, 1);
    RETURN;
END

-- 3) 單號查無此單：對照表本身抄錯，部分套用會留下「28 張改了、1 張沒改且不知是哪張」的半套狀態
SET @N = (SELECT COUNT(*) FROM @Map t WHERE NOT EXISTS (SELECT 1 FROM AdvanceRequests a WHERE a.RequestNo = t.RequestNo));
IF @N > 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'對照表有 %d 個單號在本資料庫找不到，已中止。請見「找不到的單號」表。', 16, 1, @N);
    RETURN;
END

-- 4) Email 對不到人 / 對到多人
SET @N = (SELECT COUNT(*) FROM @NewUsers WHERE Hits <> 1);
IF @N > 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 個新申請人 Email 對不到唯一使用者，已中止。請見「新申請人解析異常」表。', 16, 1, @N);
    RETURN;
END

-- 5) 新申請人非在職：掛到停用帳號 → 當事人登不進來看自己的單，報表卻仍撈得到金額
SET @N = (SELECT COUNT(*) FROM @NewUsers WHERE Status <> 'active');
IF @N > 0 AND @AllowInactiveNew = 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 位新申請人非 active，已中止。確定要放行請把 @AllowInactiveNew 改成 1。', 16, 1, @N);
    RETURN;
END

-- 6) 現任申請人是第三者：硬改會覆蓋掉別人正確的資料，且無 audit 表可復原
SET @N = (SELECT COUNT(*) FROM @Adv WHERE [Action] = 'conflict')
       + (SELECT COUNT(*) FROM @WriteOff WHERE [Action] = 'conflict');
IF @N > 0 AND @AllowForeignHolder = 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 張單的現任申請人既非代錄帳號也非目標新申請人，已中止。確定要覆蓋請把 @AllowForeignHolder 改成 1。', 16, 1, @N);
    RETURN;
END

-- 7) pending 單改完會沒人能簽
SET @N = (SELECT COUNT(*) FROM @Stuck);
IF @N > 0 AND @AllowStuckPending = 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 張 pending 單改完後目前關卡將 0 人可簽，已中止。確定要放行請把 @AllowStuckPending 改成 1。', 16, 1, @N);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- G) 母單：改申請人
-- ---------------------------------------------------------------------------
UPDATE r SET r.SubmittedById = a.NewApplicantId
FROM AdvanceRequests r
JOIN @Adv a ON a.Id = r.Id
WHERE a.[Action] = 'transfer' OR (a.[Action] = 'conflict' AND @AllowForeignHolder = 1);
DECLARE @AdvChanged int = @@ROWCOUNT;

-- ---------------------------------------------------------------------------
-- H) 沖銷子單：改申請人（繼承母單）
-- ---------------------------------------------------------------------------
UPDATE r SET r.SubmittedById = w.NewApplicantId
FROM WriteOffRecords r
JOIN @WriteOff w ON w.Id = r.Id
WHERE w.[Action] = 'transfer' OR (w.[Action] = 'conflict' AND @AllowForeignHolder = 1);
DECLARE @WoChanged int = @@ROWCOUNT;

-- ---------------------------------------------------------------------------
-- I) 異動後驗證
-- ---------------------------------------------------------------------------
PRINT N'=== 異動後殘留檢查（應全部為 0）===';
SELECT
    (SELECT COUNT(*) FROM AdvanceRequests r JOIN @Adv      a ON a.Id = r.Id
      WHERE r.SubmittedById <> a.NewApplicantId OR r.SubmittedById IS NULL)              AS [母單未改到],
    (SELECT COUNT(*) FROM WriteOffRecords r JOIN @WriteOff w ON w.Id = r.Id
      WHERE r.SubmittedById <> w.NewApplicantId OR r.SubmittedById IS NULL)              AS [子單未改到],
    (SELECT COUNT(*) FROM AdvanceRequests r JOIN @Adv      a ON a.Id = r.Id
      WHERE r.SubmittedById = @FromUserId)                                               AS [母單仍掛代錄帳號],
    (SELECT COUNT(*) FROM WriteOffRecords r JOIN @WriteOff w ON w.Id = r.Id
      WHERE r.SubmittedById = @FromUserId)                                               AS [子單仍掛代錄帳號],
    (SELECT COUNT(*) FROM WriteOffRecords r
      JOIN @Adv a ON a.Id = r.AdvanceRequestId
      JOIN AdvanceRequests p ON p.Id = a.Id
      WHERE r.SubmittedById <> p.SubmittedById)                                          AS [母子申請人不一致];

PRINT N'=== 代錄帳號名下總數變化（差額應等於本次移轉筆數）===';
SELECT @FromAdvBefore AS [母單_異動前],
       (SELECT COUNT(*) FROM AdvanceRequests WHERE SubmittedById = @FromUserId) AS [母單_異動後],
       @AdvChanged    AS [母單_本次移轉],
       @FromWoBefore  AS [子單_異動前],
       (SELECT COUNT(*) FROM WriteOffRecords WHERE SubmittedById = @FromUserId) AS [子單_異動後],
       @WoChanged     AS [子單_本次移轉];

PRINT N'=== 逐單結果 ===';
SELECT  a.RequestNo, r.ApprovalStatus,
        un.Name AS [現任申請人], dn.Name AS [現任部門],
        (SELECT COUNT(*) FROM @WriteOff w WHERE w.AdvanceRequestId = a.Id) AS [沖銷子單],
        (SELECT COUNT(*) FROM WriteOffRecords wr WHERE wr.AdvanceRequestId = a.Id AND wr.SubmittedById <> r.SubmittedById) AS [子單不一致]
FROM @Adv a
JOIN AdvanceRequests r ON r.Id = a.Id
LEFT JOIN Users       un ON un.Id = r.SubmittedById
LEFT JOIN Departments dn ON dn.Id = un.DepartmentId
ORDER BY un.Name, a.RequestNo;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'=== @Commit = 1 → 已 COMMIT，申請人已移轉 ===';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'=== @Commit = 0 → 已 ROLLBACK（空跑，未寫入任何變更）===';
END
