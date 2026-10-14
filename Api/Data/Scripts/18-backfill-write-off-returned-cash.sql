/*
================================================================================
 18 —— 歷史預支沖銷「同仁繳回預支餘額」補登（2026-10）
================================================================================

 背景
 ----
 系統過去只記「公司補撥給員工」（AdvanceRequest.RefundAmount / RefundedAmount），
 從未記錄「同仁把用不完的預支款繳回公司」。2026-10 新增
 WriteOffRecords.ReturnedAmount / ReturnedAt / ReturnedProofFileName / ReturnedProofUrl，
 本腳本供客戶提供「預支單號＋繳回金額＋繳回日期」後補登歷史單。

 寫入規則
 --------
 · 以 AdvanceRequests.RequestNo 定位預支單（不寫死 Id，本機 / staging / 正式站同一份可用）。
 · 寫在該預支單「最後一張已核准」的沖銷單（ReviewedAt 最新；同時間取 Id 較大者）。
 · 只寫 ReturnedAmount、ReturnedAt 兩欄；不上傳憑證照片（ReturnedProofFileName / Url 維持 NULL）。
   ⚠ 前台日後若編輯該單並填入繳回金額，會因「憑證必填」被要求補照片；已核准單前台本就不可編輯，故不衝突。
 · 繳回金額不計入 GrandTotal，RefundDue、專案水位、撥款分期完全不受影響。

 閘門（任一不通過整份 ROLLBACK 中止並列報表）
 -------------------------------------------
   1. 對照表列數須等於 @ExpectedRows（防漏抄 / 多抄）
   2. 預支單號找不到
   3. 該預支單沒有任何已核准的沖銷單
   4. 金額須 > 0
   5. 金額超過尚未結清金額：預支總額 − 已核准核銷合計 − 他單已核准繳回
   6. 目標沖銷單已有「不同」的繳回資料（避免覆蓋；確認要覆蓋請把 @AllowOverwrite 改成 1）
   同單號在對照表重複會在 INSERT 時撞 PRIMARY KEY。

 冪等
 ----
 目標沖銷單的 ReturnedAmount / ReturnedAt 已等於對照表的值 → 視為 already 跳過，可安全重跑。

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 請先把客戶提供的資料填進下方 @Map（範例列已註解），確認報表無誤後改成 @Commit = 1 再執行。

 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P 'Strong@Password123' -d JabezDb -C -N -u \
   -i /tmp/18.sql

 空跑要確認三件事：
   ① 閘門全數通過（沒有 RAISERROR）
   ② 主報表每列 Action 為「補登」（重跑時為「已補登」）
   ③ 「補登後尚未結清」欄位符合客戶預期（多半為 0）
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit         bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入
DECLARE @AllowOverwrite bit = 0;   -- 目標沖銷單已有不同的繳回資料時是否覆蓋

DECLARE @ExpectedRows int = 0;     -- ← 對照表應有的列數（填完 @Map 後同步修改）

-- ---------------------------------------------------------------------------
-- 預支單號 → 繳回金額 / 繳回日期 對照表（由客戶提供後填入）
--
-- RequestNo 設 PRIMARY KEY：同一單號打兩次會當場撞 PK，不會靜靜地讓後面那列覆蓋前面。
-- ---------------------------------------------------------------------------
DECLARE @Map TABLE (
    AdvanceRequestNo nvarchar(50)  PRIMARY KEY,
    ReturnedAmount   decimal(18,2) NOT NULL,
    ReturnedAt       date          NOT NULL
);

-- INSERT INTO @Map (AdvanceRequestNo, ReturnedAmount, ReturnedAt) VALUES
--     (N'ADV-20260907-001', 1830, '2026-10-09'),
--     (N'ADV-20260907-002',  500, '2026-10-12');

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- A) 解析預支單與目標沖銷單（最後一張已核准）
-- ---------------------------------------------------------------------------
DECLARE @T TABLE (
    AdvanceRequestNo  nvarchar(50)  PRIMARY KEY,
    AdvanceId         int           NULL,
    AdvanceGrandTotal decimal(18,2) NULL,
    ApprovedCount     int           NOT NULL,
    TargetWoId        int           NULL,
    TargetWoNo        nvarchar(50)  NULL,
    TargetGrandTotal  decimal(18,2) NULL,
    OldReturnedAmount decimal(18,2) NULL,
    OldReturnedAt     datetime2     NULL,
    ReturnedAmount    decimal(18,2) NOT NULL,
    ReturnedAt        date          NOT NULL,
    ApprovedWrittenOff decimal(18,2) NULL,   -- 該預支單全部已核准核銷合計
    OtherReturned     decimal(18,2) NULL,    -- 他單（非目標單）已核准繳回合計
    [Action]          varchar(10)   NOT NULL DEFAULT 'pending'
);

INSERT INTO @T (AdvanceRequestNo, ReturnedAmount, ReturnedAt, ApprovedCount)
SELECT m.AdvanceRequestNo, m.ReturnedAmount, m.ReturnedAt, 0 FROM @Map m;

UPDATE t SET
    t.AdvanceId         = a.Id,
    t.AdvanceGrandTotal = a.GrandTotal
FROM @T t
JOIN AdvanceRequests a ON a.RequestNo = t.AdvanceRequestNo;

UPDATE t SET
    t.ApprovedCount      = x.Cnt,
    t.ApprovedWrittenOff = x.Total
FROM @T t
CROSS APPLY (SELECT COUNT(*) AS Cnt, ISNULL(SUM(w.GrandTotal), 0) AS Total
             FROM WriteOffRecords w
             WHERE w.AdvanceRequestId = t.AdvanceId AND w.ApprovalStatus = 'approved') x
WHERE t.AdvanceId IS NOT NULL;

UPDATE t SET
    t.TargetWoId         = w.Id,
    t.TargetWoNo         = w.RequestNo,
    t.TargetGrandTotal   = w.GrandTotal,
    t.OldReturnedAmount  = w.ReturnedAmount,
    t.OldReturnedAt      = w.ReturnedAt
FROM @T t
CROSS APPLY (SELECT TOP 1 w.Id, w.RequestNo, w.GrandTotal, w.ReturnedAmount, w.ReturnedAt
             FROM WriteOffRecords w
             WHERE w.AdvanceRequestId = t.AdvanceId AND w.ApprovalStatus = 'approved'
             ORDER BY w.ReviewedAt DESC, w.Id DESC) w
WHERE t.AdvanceId IS NOT NULL;

UPDATE t SET
    t.OtherReturned = ISNULL((SELECT SUM(w.ReturnedAmount) FROM WriteOffRecords w
                              WHERE w.AdvanceRequestId = t.AdvanceId
                                AND w.ApprovalStatus = 'approved'
                                AND w.Id <> t.TargetWoId), 0)
FROM @T t
WHERE t.TargetWoId IS NOT NULL;

UPDATE @T SET [Action] =
    CASE WHEN OldReturnedAmount = ReturnedAmount AND CAST(OldReturnedAt AS date) = ReturnedAt THEN 'already'
         WHEN OldReturnedAmount IS NOT NULL THEN 'conflict'
         ELSE 'backfill' END
WHERE TargetWoId IS NOT NULL;

-- ---------------------------------------------------------------------------
-- B) 報表（先印後驗，閘門中止時也看得到原因）
-- ---------------------------------------------------------------------------
PRINT N'=== 主報表 ===';
SELECT  t.AdvanceRequestNo                                  AS [預支單號],
        t.AdvanceGrandTotal                                 AS [預支總額],
        t.ApprovedWrittenOff                                AS [已核准核銷合計],
        t.OtherReturned                                     AS [他單已核准繳回],
        t.TargetWoNo                                        AS [目標沖銷單],
        t.ReturnedAmount                                    AS [繳回金額],
        t.ReturnedAt                                        AS [繳回日期],
        t.OldReturnedAmount                                 AS [現有繳回金額],
        CASE WHEN t.Action = 'backfill' THEN N'補登'
             WHEN t.Action = 'already'  THEN N'已補登'
             WHEN t.Action = 'conflict' THEN N'衝突'
             ELSE N'異常' END                                AS [Action],
        t.AdvanceGrandTotal - t.ApprovedWrittenOff - t.OtherReturned
          - CASE WHEN t.Action = 'already' THEN t.OldReturnedAmount ELSE t.ReturnedAmount END
                                                            AS [補登後尚未結清]
FROM @T t
ORDER BY t.AdvanceRequestNo;

-- ---------------------------------------------------------------------------
-- C) 閘門
-- ---------------------------------------------------------------------------
DECLARE @N int;

-- 1) 列數
SET @N = (SELECT COUNT(*) FROM @Map);
IF @N <> @ExpectedRows
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'對照表共 %d 列，與 @ExpectedRows 不符，已中止。請確認是否漏抄或多抄，並同步修改 @ExpectedRows。', 16, 1, @N);
    RETURN;
END

-- 2) 預支單號找不到
SET @N = (SELECT COUNT(*) FROM @T WHERE AdvanceId IS NULL);
IF @N > 0
BEGIN
    SELECT AdvanceRequestNo AS [找不到的預支單號] FROM @T WHERE AdvanceId IS NULL;
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 個預支單號在本資料庫找不到，已中止。請見「找不到的預支單號」表。', 16, 1, @N);
    RETURN;
END

-- 3) 沒有已核准的沖銷單
SET @N = (SELECT COUNT(*) FROM @T WHERE TargetWoId IS NULL);
IF @N > 0
BEGIN
    SELECT AdvanceRequestNo AS [無已核准沖銷單的預支單號] FROM @T WHERE TargetWoId IS NULL;
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 張預支單沒有任何已核准的沖銷單，已中止。', 16, 1, @N);
    RETURN;
END

-- 4) 金額須 > 0
SET @N = (SELECT COUNT(*) FROM @T WHERE ReturnedAmount <= 0);
IF @N > 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 列繳回金額 <= 0，已中止。', 16, 1, @N);
    RETURN;
END

-- 5) 超過尚未結清金額（冪等重跑的 already 列不重驗，其金額已在 OtherReturned 之外自洽）
SET @N = (SELECT COUNT(*) FROM @T
          WHERE [Action] <> 'already'
            AND ReturnedAmount > AdvanceGrandTotal - ApprovedWrittenOff - OtherReturned);
IF @N > 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 列繳回金額超過尚未結清金額（預支總額 − 已核准核銷 − 他單已核准繳回），已中止。請見主報表。', 16, 1, @N);
    RETURN;
END

-- 6) 目標沖銷單已有不同的繳回資料
SET @N = (SELECT COUNT(*) FROM @T WHERE [Action] = 'conflict');
IF @N > 0 AND @AllowOverwrite = 0
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR (N'有 %d 張目標沖銷單已有不同的繳回資料，已中止。確定要覆蓋請把 @AllowOverwrite 改成 1。', 16, 1, @N);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- D) 寫入（只寫金額與日期兩欄，不動憑證欄位）
-- ---------------------------------------------------------------------------
UPDATE w SET
    w.ReturnedAmount = t.ReturnedAmount,
    w.ReturnedAt     = t.ReturnedAt
FROM WriteOffRecords w
JOIN @T t ON t.TargetWoId = w.Id
WHERE t.[Action] = 'backfill' OR (t.[Action] = 'conflict' AND @AllowOverwrite = 1);
DECLARE @Changed int = @@ROWCOUNT;

-- ---------------------------------------------------------------------------
-- E) 異動後驗證
-- ---------------------------------------------------------------------------
PRINT N'=== 異動後驗證（「未寫到」應為 0）===';
SELECT
    @Changed AS [本次寫入筆數],
    (SELECT COUNT(*) FROM WriteOffRecords w JOIN @T t ON t.TargetWoId = w.Id
      WHERE w.ReturnedAmount IS NULL OR w.ReturnedAmount <> t.ReturnedAmount
         OR CAST(w.ReturnedAt AS date) <> t.ReturnedAt)
      AS [未寫到];

PRINT N'=== 逐單結果 ===';
SELECT  t.AdvanceRequestNo, w.RequestNo AS [沖銷單], w.ReturnedAmount, w.ReturnedAt,
        a.GrandTotal - ISNULL((SELECT SUM(x.GrandTotal) FROM WriteOffRecords x
                               WHERE x.AdvanceRequestId = a.Id AND x.ApprovalStatus = 'approved'), 0)
                     - ISNULL((SELECT SUM(x.ReturnedAmount) FROM WriteOffRecords x
                               WHERE x.AdvanceRequestId = a.Id AND x.ApprovalStatus = 'approved'), 0)
          AS [尚未結清金額]
FROM @T t
JOIN WriteOffRecords w ON w.Id = t.TargetWoId
JOIN AdvanceRequests a ON a.Id = t.AdvanceId
ORDER BY t.AdvanceRequestNo;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'=== @Commit = 1 → 已 COMMIT，繳回資料已補登 ===';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'=== @Commit = 0 → 空跑，已 ROLLBACK，資料庫未異動 ===';
END
