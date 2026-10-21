/*
================================================================================
 清除預支單分期撥款誤填的「實際撥款日」（ADV-20260921-001 第 2 期）
================================================================================

 背景
 ----
 財務在 ADV-20260921-001 的撥款明細第 2 期（預計 2026-10-26、金額 84,496）
 誤將「實際撥款日」填上，款項其實尚未撥出，需改回空白（＝未撥款）。
 前台無法自救：已撥款列在撥款明細編輯器中四欄全鎖、刪除鈕隱藏，後端
 InstallmentValidator 亦拒絕修改已撥款列（已撥款列保護），故以腳本更正。

 只改一列、三個欄位
 ------------------
   AdvanceRequestInstallments.PaidAt       → NULL
   AdvanceRequestInstallments.PaidByUserId → NULL
   AdvanceRequestInstallments.UpdatedAt    → SYSUTCDATETIME()（比照 InstallmentUpsertService 寫 UTC）

 · PaidAt 與 PaidByUserId 是撥款時一起寫入的（InstallmentUpsertService.Apply），
   只清 PaidAt 會留下「未撥款卻有出納」的殘列，故兩欄一起清。
 · ExpectedDate / Amount / Note / InstallmentNo 一律不動。

 改完會立刻生效的
 ----------------
   · 撥款狀態由子表即時推算（InstallmentReadService.ComputeStatus），
     本單會從「全部撥款 / 部分撥款」退一階
   · 已核准頁籤的「預計撥款日 / 預計撥款額」與待撥款清單會重新列出這一期
   · 撥款提醒（PaymentReminderService）會把這期當作待撥款，於預計撥款日前推播財務
   · 列印 PDF 的出納簽名章改取「最後一期已撥款者」（若第 1 期已撥則改用第 1 期）

 無法回收的
 ----------
   當初填入撥款日時已發出的「已撥款」LINE / Email 通知無法撤回，需口頭告知申請人。

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認報表無誤後改成 1 再執行一次才會真正寫入。不寫死任何 Id，以 RequestNo +
 InstallmentNo 定位，並以預計撥款日 / 金額防呆。

 空跑要確認三件事：
   ① 「目標分期」只有 1 列，Action 為「清除」，預計撥款日 2026-10-26、金額 84,496
   ② 「同單全部分期」中，第 2 期的 PaidAt 確實是誤填的那個日期
   ③ 「異動後驗證」第 2 期 PaidAt / PaidByUserId 皆為 NULL

 ⚠ 本腳本可安全重跑：第 2 期已是未撥款時落入 already 而被跳過。
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit         bit           = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入

DECLARE @RequestNo      nvarchar(50)  = N'ADV-20260921-001';
DECLARE @InstallmentNo  int           = 2;
DECLARE @ExpectedDate   date          = '2026-10-26';   -- 防呆用，不參與定位
DECLARE @ExpectedAmount decimal(18,2) = 84496;          -- 防呆用，不參與定位

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- A) 解析目標分期並分類
--      clear   = 已填撥款日            → 本次清除
--      already = 已是未撥款            → 跳過（腳本可重跑）
-- ---------------------------------------------------------------------------
DECLARE @Ins TABLE (
    Id         int PRIMARY KEY,
    [Action]   varchar(10) NOT NULL
);

INSERT INTO @Ins (Id, [Action])
SELECT  i.Id,
        CASE WHEN i.PaidAt IS NULL THEN 'already' ELSE 'clear' END
FROM AdvanceRequestInstallments i
JOIN AdvanceRequests a ON a.Id = i.AdvanceRequestId
WHERE a.RequestNo     = @RequestNo
  AND i.InstallmentNo = @InstallmentNo;

-- ---------------------------------------------------------------------------
-- B) 異動前報表
-- ---------------------------------------------------------------------------
PRINT N'=== 目標單 ===';
SELECT  a.Id, a.RequestNo, a.ApprovalStatus, a.IsClosed, a.GrandTotal,
        u.Name AS [申請人], a.ActivityName
FROM AdvanceRequests a
LEFT JOIN Users u ON u.Id = a.SubmittedById
WHERE a.RequestNo = @RequestNo;

PRINT N'=== 目標分期 ===';
SELECT  i.Id, i.InstallmentNo,
        CONVERT(varchar(10), i.ExpectedDate, 23) AS [預計撥款日],
        i.Amount, i.PaidAt, pu.Name AS [出納], i.Note,
        CASE x.[Action] WHEN 'clear' THEN N'清除' ELSE N'已是未撥款→略過' END AS [Action]
FROM @Ins x
JOIN AdvanceRequestInstallments i ON i.Id = x.Id
LEFT JOIN Users pu ON pu.Id = i.PaidByUserId;

PRINT N'=== 同單全部分期（異動前）===';
SELECT  i.InstallmentNo, CONVERT(varchar(10), i.ExpectedDate, 23) AS [預計撥款日],
        i.Amount, i.PaidAt, pu.Name AS [出納], i.Note
FROM AdvanceRequestInstallments i
JOIN AdvanceRequests a ON a.Id = i.AdvanceRequestId
LEFT JOIN Users pu ON pu.Id = i.PaidByUserId
WHERE a.RequestNo = @RequestNo
ORDER BY i.InstallmentNo;

-- ---------------------------------------------------------------------------
-- C) 保護閘門（全部排在報表之後，空跑一次就能看到所有問題）
-- ---------------------------------------------------------------------------
-- 1) 單號查無此單＝跑錯資料庫或單號抄錯
IF NOT EXISTS (SELECT 1 FROM AdvanceRequests WHERE RequestNo = @RequestNo)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'預支單號在本資料庫找不到，已中止（是否跑錯資料庫或單號抄錯？）。', 16, 1);
    RETURN;
END

-- 2) 該期不存在（或重複）
IF (SELECT COUNT(*) FROM @Ins) <> 1
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'找不到第 %d 期撥款（或不只一列），已中止。請見「同單全部分期」表。', 16, 1, @InstallmentNo);
    RETURN;
END

-- 3) 預計撥款日 / 金額與交辦內容不符：代表期別抄錯或明細已被改過
IF NOT EXISTS (
    SELECT 1 FROM @Ins x JOIN AdvanceRequestInstallments i ON i.Id = x.Id
    WHERE CAST(i.ExpectedDate AS date) = @ExpectedDate AND i.Amount = @ExpectedAmount)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'第 %d 期的預計撥款日或金額與交辦內容不符，已中止。請見「目標分期」表。', 16, 1, @InstallmentNo);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- D) 清除實際撥款日
-- ---------------------------------------------------------------------------
UPDATE i
SET    i.PaidAt       = NULL,
       i.PaidByUserId = NULL,
       i.UpdatedAt    = SYSUTCDATETIME()
FROM AdvanceRequestInstallments i
JOIN @Ins x ON x.Id = i.Id
WHERE x.[Action] = 'clear';

PRINT N'已清除列數：' + CAST(@@ROWCOUNT AS nvarchar(10));

-- ---------------------------------------------------------------------------
-- E) 異動後驗證
-- ---------------------------------------------------------------------------
PRINT N'=== 同單全部分期（異動後）===';
SELECT  i.InstallmentNo, CONVERT(varchar(10), i.ExpectedDate, 23) AS [預計撥款日],
        i.Amount, i.PaidAt, i.PaidByUserId, i.Note
FROM AdvanceRequestInstallments i
JOIN AdvanceRequests a ON a.Id = i.AdvanceRequestId
WHERE a.RequestNo = @RequestNo
ORDER BY i.InstallmentNo;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'>>> 已寫入（COMMIT）';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'>>> 空跑結束（ROLLBACK），確認無誤後把 @Commit 改成 1 再執行';
END
