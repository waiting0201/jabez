/*
================================================================================
 變更預支申請單的所屬專案（ADV-20260907-049：C007 → C010-115 產品開發製造）
================================================================================

 背景
 ----
 ADV-20260907-049（已核准、未結案）當初送簽時掛在專案 C007
 「2025年奚卜蘭遊客中心房屋出租案」，實際應歸屬 C010-115「產品開發製造」。
 已核准的單在前台無法編輯（AdvanceRequestHandler.UpdateAsync 僅開放
 draft / returned），故以腳本更正。

 只改一個欄位
 ------------
   AdvanceRequests.ProjectId

 · 該表**沒有 UpdatedAt**，所以 UPDATE 只碰 ProjectId 一欄。
 · 子表一律不動：AdvanceRequestItems / Installments / Supplements 皆無 ProjectId；
   WriteOffRecords 也沒有自己的 ProjectId，全站一律「透過母單 AdvanceRequest.ProjectId
   回扣專案」（見 ProjectWaterLevelReadService 的 wo OUTER APPLY 與
   PaymentReminderService「沖銷單無 ProjectId，專案代號取自母預支單」），
   故沖銷子單會自動跟著搬家，不需另外處理。

 改完會立刻生效的（讀取端即時 JOIN Projects）
 --------------------------------------------
   · 預支申請清單 / 詳情頁 / 簽核作業詳情頁的專案欄位
   · 列印 PDF 的專案名稱與代號（即時渲染）
   · 款項統計報表的專案欄與專案篩選
     （⚠ 部門可見性 scope 看的是**申請人部門**（PaymentReportReadService 的
       `userAlias.DepartmentId IN @AllowedDeptIds`），不是專案部門，
       所以換專案**不會**改變誰看得到這張單）
   · 專案水位表：已核准沖銷金額由舊專案移至新專案
     （本單目前尚無任何沖銷子單，故當下水位不變；日後沖銷核准時會直接記到新專案）
   · 撥款提醒推播的專案代號

 刻意不重算的（送簽當下的快照）
 ------------------------------
   ApprovalItemId / RequestNo / SubmittedAt / CurrentStepOrder，以及三張多型足跡表
   ApprovalRecords / EscalationOverrides / RequestDesignatedReviewers。
   簽核流程是依**申請人部門**解析（ResolveApprovalItemIdAsync），與專案無關，
   故換專案不影響誰簽過、誰能簽。

 ⚠ 已撥款分期會一起改變歸屬
 --------------------------
   撥款金額記在 AdvanceRequestInstallments，沒有自己的專案欄位，一律隨母單。
   本單已有 3 期撥款完成（82,000），改完之後這筆錢在款項統計報表上就算到
   C010-115 名下。這正是本次變更的目的，但務必先在空跑報表確認金額無誤。

 環境無關
 --------
 全程以 RequestNo 與 Projects.Code 定位，**不寫死任何 Id**，同一份可在
 本機 / staging / 正式站跑。整份為單一 batch、不含 GO，可直接貼進
 SSMS / Azure Data Studio 執行。

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。

 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P 'Strong@Password123' -d JabezDb -C -N -u \
   -i /tmp/11.sql

 空跑要確認三件事：
   ① 「目標單與專案解析」的 Action 為「變更」，舊專案＝C007、新專案＝C010-115
   ② 「受影響的金額」逐列金額與預期相符（總額 192,000 / 已撥 3 期 82,000）
   ③ 「異動後驗證」的殘留數全部為 0

 ⚠ 本腳本可安全重跑：已是目標專案的單會落入 already 而被跳過，不會重複異動。
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit              bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入
DECLARE @AllowUnexpectedFrom bit = 0;   -- 目標單現在掛的專案不是 @FromCode 時是否照改
DECLARE @AllowClosedProject  bit = 0;   -- 新專案已結案（Status <> 'active'）時是否放行

DECLARE @RequestNo nvarchar(50)  = N'ADV-20260907-049';
DECLARE @FromCode  nvarchar(50)  = N'C007';       -- 預期的現有專案代號（防呆用，不參與定位）
DECLARE @ToCode    nvarchar(50)  = N'C010-115';   -- 目標專案代號

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- A) 解析目標專案（以 Code 定位；Projects.Code 無唯一索引，故自行檢查命中數）
-- ---------------------------------------------------------------------------
DECLARE @ToHits   int = (SELECT COUNT(*) FROM Projects WHERE Code = @ToCode);
DECLARE @ToProjId int = (SELECT MIN(Id)  FROM Projects WHERE Code = @ToCode);

DECLARE @ToStatus nvarchar(20) = (SELECT Status FROM Projects WHERE Id = @ToProjId);

-- ---------------------------------------------------------------------------
-- B) 解析目標單並分類
--      change  = 現在掛的是 @FromCode          → 本次改
--      already = 現在掛的已是目標專案          → 跳過（腳本可重跑）
--      unexpect= 掛在第三個專案                → 閘門 4 中止
-- ---------------------------------------------------------------------------
DECLARE @Adv TABLE (
    Id            int PRIMARY KEY,
    RequestNo     nvarchar(50) NOT NULL,
    OldProjectId  int          NOT NULL,
    NewProjectId  int          NOT NULL,
    [Action]      varchar(10)  NOT NULL
);

INSERT INTO @Adv (Id, RequestNo, OldProjectId, NewProjectId, [Action])
SELECT  a.Id, a.RequestNo, a.ProjectId, @ToProjId,
        CASE WHEN a.ProjectId = @ToProjId THEN 'already'
             WHEN p.Code      = @FromCode THEN 'change'
             ELSE 'unexpect' END
FROM AdvanceRequests a
LEFT JOIN Projects p ON p.Id = a.ProjectId
WHERE a.RequestNo = @RequestNo
  AND @ToProjId IS NOT NULL;   -- 專案解析失敗者交由閘門 2 處理

-- ---------------------------------------------------------------------------
-- C) 異動前報表
-- ---------------------------------------------------------------------------
PRINT N'=== 目標單與專案解析 ===';
SELECT  a.Id,
        r.RequestNo,
        r.ApprovalStatus,
        r.IsClosed,
        u.Name  AS [申請人],
        du.Name AS [申請人部門],
        r.ActivityName,
        po.Code AS [舊專案代號], po.Name AS [舊專案], dpo.Name AS [舊專案部門],
        pn.Code AS [新專案代號], pn.Name AS [新專案], dpn.Name AS [新專案部門], pn.Status AS [新專案狀態],
        CASE a.[Action] WHEN 'change'  THEN N'變更'
                        WHEN 'already' THEN N'已是目標專案→略過'
                        ELSE N'現有專案非預期→中止' END AS [Action]
FROM @Adv a
JOIN AdvanceRequests r ON r.Id = a.Id
LEFT JOIN Users       u   ON u.Id   = r.SubmittedById
LEFT JOIN Departments du  ON du.Id  = u.DepartmentId
LEFT JOIN Projects    po  ON po.Id  = a.OldProjectId
LEFT JOIN Departments dpo ON dpo.Id = po.DepartmentId
LEFT JOIN Projects    pn  ON pn.Id  = a.NewProjectId
LEFT JOIN Departments dpn ON dpn.Id = pn.DepartmentId;

PRINT N'=== 目標專案代號命中狀況（Hits 必須為 1）===';
SELECT @ToCode AS [目標專案代號], @ToHits AS Hits, @ToProjId AS [解析出的 ProjectId], @ToStatus AS [狀態];

PRINT N'=== 受影響的金額（這些錢會一起改記到新專案名下）===';
SELECT  r.RequestNo,
        r.GrandTotal                                                                                      AS [預支總額],
        (SELECT COUNT(*) FROM AdvanceRequestInstallments i WHERE i.AdvanceRequestId = a.Id)               AS [分期數],
        (SELECT COUNT(*) FROM AdvanceRequestInstallments i WHERE i.AdvanceRequestId = a.Id AND i.PaidAt IS NOT NULL) AS [已撥期數],
        (SELECT ISNULL(SUM(i.Amount), 0) FROM AdvanceRequestInstallments i WHERE i.AdvanceRequestId = a.Id AND i.PaidAt IS NOT NULL) AS [已撥金額],
        (SELECT COUNT(*) FROM WriteOffRecords w WHERE w.AdvanceRequestId = a.Id)                          AS [沖銷子單],
        (SELECT COUNT(*) FROM WriteOffRecords w WHERE w.AdvanceRequestId = a.Id AND w.ApprovalStatus = 'approved') AS [已核准沖銷],
        (SELECT ISNULL(SUM(w.GrandTotal), 0) FROM WriteOffRecords w WHERE w.AdvanceRequestId = a.Id AND w.ApprovalStatus = 'approved') AS [已核准沖銷金額_計入專案水位],
        (SELECT COUNT(*) FROM AdvanceRequestSupplements s WHERE s.AdvanceRequestId = a.Id)                AS [追加批次]
FROM @Adv a
JOIN AdvanceRequests r ON r.Id = a.Id;

-- ---------------------------------------------------------------------------
-- D) 保護閘門（全部排在報表之後，空跑一次就能看到所有問題）
-- ---------------------------------------------------------------------------
DECLARE @N int;

-- 1) 單號查無此單＝跑錯資料庫或單號抄錯
IF NOT EXISTS (SELECT 1 FROM AdvanceRequests WHERE RequestNo = @RequestNo)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'預支單號在本資料庫找不到，已中止（是否跑錯資料庫或單號抄錯？）。', 16, 1);
    RETURN;
END

-- 2) 目標專案代號對不到唯一專案：Projects.Code 無唯一索引，對到多筆時 MIN(Id) 會挑錯家
IF @ToHits <> 1
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'目標專案代號對到 %d 個專案（需恰好 1 個），已中止。請見「目標專案代號命中狀況」表。', 16, 1, @ToHits);
    RETURN;
END

-- 3) 新專案已結案：掛到結案專案上，該單日後在前台編輯時會因下拉（/projects/active）撈不到而被迫改選別案
IF @ToStatus <> 'active' AND @AllowClosedProject = 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'目標專案狀態為 %s（非 active），已中止。確定要放行請把 @AllowClosedProject 改成 1。', 16, 1, @ToStatus);
    RETURN;
END

-- 4) 現有專案不是預期的 @FromCode：代表這張單在交辦後又被人改過，硬改會蓋掉別人的更正
SET @N = (SELECT COUNT(*) FROM @Adv WHERE [Action] = 'unexpect');
IF @N > 0 AND @AllowUnexpectedFrom = 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'目標單現有專案既非 @FromCode 也非目標專案，已中止。確定要覆蓋請把 @AllowUnexpectedFrom 改成 1。', 16, 1);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- E) 變更專案
-- ---------------------------------------------------------------------------
UPDATE r SET r.ProjectId = a.NewProjectId
FROM AdvanceRequests r
JOIN @Adv a ON a.Id = r.Id
WHERE a.[Action] = 'change' OR (a.[Action] = 'unexpect' AND @AllowUnexpectedFrom = 1);
DECLARE @Changed int = @@ROWCOUNT;

-- ---------------------------------------------------------------------------
-- F) 異動後驗證
-- ---------------------------------------------------------------------------
PRINT N'=== 異動後殘留檢查（應全部為 0）===';
SELECT
    (SELECT COUNT(*) FROM AdvanceRequests r JOIN @Adv a ON a.Id = r.Id
      WHERE r.ProjectId <> a.NewProjectId)                                               AS [未改到],
    (SELECT COUNT(*) FROM AdvanceRequests r JOIN @Adv a ON a.Id = r.Id
      JOIN Projects p ON p.Id = r.ProjectId WHERE p.Code = @FromCode)                     AS [仍掛舊專案],
    (SELECT COUNT(*) FROM AdvanceRequests r WHERE r.RequestNo = @RequestNo
      AND NOT EXISTS (SELECT 1 FROM Projects p WHERE p.Id = r.ProjectId))                 AS [專案不存在];

PRINT N'=== 逐單結果 ===';
SELECT  r.RequestNo, r.ApprovalStatus, r.GrandTotal,
        p.Code AS [現在的專案代號], p.Name AS [現在的專案], d.Name AS [專案部門],
        @Changed AS [本次異動筆數]
FROM @Adv a
JOIN AdvanceRequests r ON r.Id = a.Id
LEFT JOIN Projects    p ON p.Id = r.ProjectId
LEFT JOIN Departments d ON d.Id = p.DepartmentId;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'=== @Commit = 1 → 已 COMMIT，專案已變更 ===';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'=== @Commit = 0 → 已 ROLLBACK（空跑，未寫入任何變更）===';
END
