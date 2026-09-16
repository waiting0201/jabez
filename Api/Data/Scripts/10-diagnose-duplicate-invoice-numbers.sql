/*
================================================================================
 診斷：找出跨表重複的發票號碼（唯讀，不修改任何資料）
================================================================================

 背景
 ----
 發票號碼唯一性是**程式層**檢查（DB 無唯一索引），2026-09 前三個 Handler 各寫一份，
 規則不一致：
   · 出差請款 TravelPaymentRequestItems **從未做過任何檢查** → 可能已有歷史重複；
   · 跨表查詢時漏加 ApprovalStatus != 'rejected' → 已拒絕的沖銷單會永久佔住號碼。

 2026-09 收斂為單一真相 Api/Common/InvoiceUniquenessChecker.cs：四張明細表對稱檢查、
 一律排除已拒絕的單。**上線前先跑本腳本**，把既有重複挑出來人工處理 —— 否則使用者
 編輯這些舊單（發票欄根本沒動）時會被別張舊單擋住而存不回去。

 佔號規則提醒：草稿（draft）與退回修改中（returned）的單**仍佔號**，
 要釋放號碼必須刪掉那張單。已拒絕（rejected）的單不佔號，故本腳本一併排除。

 用法
 ----
 直接執行；有回傳列 = 有重複要處理。兩段各自獨立，都要看。
 用 sqlcmd 跑請加 -u（否則訊息中的中文會變亂碼）：
   sqlcmd -S <server> -d <db> -U <user> -P <pwd> -i 10-diagnose-duplicate-invoice-numbers.sql -u

 相關文件：docs/business/application-forms.md §發票號碼重複檢查規則
================================================================================
*/

SET NOCOUNT ON;

-- 四張「實際報帳」明細表攤平成同一份清單（排除已拒絕的單、空號碼、含中文的手打文字）
-- 手打中文排除規則須與 InvoiceNoHelper.IsManualText 一致：含 CJK 統一表意文字者不比對。
IF OBJECT_ID('tempdb..#Invoices') IS NOT NULL DROP TABLE #Invoices;

SELECT * INTO #Invoices FROM (
    SELECT
        ii.InvoiceNo AS InvoiceNo,
        N'請款單' AS SourceLabel,
        pr.Id AS RequestId,
        ISNULL(pr.RequestNo, N'(尚未取號)') AS RequestNo,
        ISNULL(u.Name, N'(不詳)') AS Applicant,
        pr.ApprovalStatus AS ApprovalStatus,
        pr.CreatedAt AS CreatedAt
    FROM InvoiceItems ii
    JOIN PaymentRequests pr ON pr.Id = ii.PaymentRequestId
    LEFT JOIN Users u ON u.Id = pr.SubmittedById
    WHERE pr.ApprovalStatus <> 'rejected' AND NULLIF(LTRIM(RTRIM(ii.InvoiceNo)), '') IS NOT NULL

    UNION ALL

    SELECT
        wi.InvoiceNo,
        N'預支沖銷單',
        wo.Id,
        ISNULL(wo.RequestNo, N'(尚未取號)'),
        ISNULL(u.Name, N'(不詳)'),
        wo.ApprovalStatus,
        wo.CreatedAt
    FROM WriteOffItems wi
    JOIN WriteOffRecords wo ON wo.Id = wi.WriteOffRecordId
    LEFT JOIN Users u ON u.Id = wo.SubmittedById
    WHERE wo.ApprovalStatus <> 'rejected' AND NULLIF(LTRIM(RTRIM(wi.InvoiceNo)), '') IS NOT NULL

    UNION ALL

    SELECT
        twi.InvoiceNo,
        N'出差沖銷單',
        two.Id,
        ISNULL(two.RequestNo, N'(尚未取號)'),
        ISNULL(u.Name, N'(不詳)'),
        two.ApprovalStatus,
        two.CreatedAt
    FROM TravelWriteOffItems twi
    JOIN TravelWriteOffRecords two ON two.Id = twi.TravelWriteOffRecordId
    LEFT JOIN Users u ON u.Id = two.SubmittedById
    WHERE two.ApprovalStatus <> 'rejected' AND NULLIF(LTRIM(RTRIM(twi.InvoiceNo)), '') IS NOT NULL

    UNION ALL

    SELECT
        tpi.InvoiceNo,
        N'出差請款單',
        tpr.Id,
        ISNULL(tpr.RequestNo, N'(尚未取號)'),
        ISNULL(u.Name, N'(不詳)'),
        tpr.ApprovalStatus,
        tpr.CreatedAt
    FROM TravelPaymentRequestItems tpi
    JOIN TravelPaymentRequests tpr ON tpr.Id = tpi.TravelPaymentRequestId
    LEFT JOIN Users u ON u.Id = tpr.EmployeeId
    WHERE tpr.ApprovalStatus <> 'rejected' AND NULLIF(LTRIM(RTRIM(tpi.InvoiceNo)), '') IS NOT NULL
) AS x;

-- 含中文 / CJK 的手打文字（「收據」「領據」）本就不比對，排除。
-- 【必須用 Latin1_General_BIN2】中文 collation 下的字元範圍 [一-鿿] 不是照 Unicode 碼位排序，
-- 比對會整個失效（「收據」判定為 0）；二進位 collation 才等價於 InvoiceNoHelper.IsManualText。
DELETE FROM #Invoices
WHERE PATINDEX(N'%[' + NCHAR(0x4E00) + N'-' + NCHAR(0x9FFF) + N']%',
               InvoiceNo COLLATE Latin1_General_BIN2) > 0;

-- ─────────────────────────────────────────────────────────────────────────────
-- 【A】重複的發票號碼總覽：有幾筆、散在哪幾種單
--      每一列都代表「新規則上線後會互相擋住」的一組單，須人工判讀保留哪張。
-- ─────────────────────────────────────────────────────────────────────────────
PRINT N'===== [A] 重複發票號碼總覽 =====';

SELECT
    InvoiceNo,
    COUNT(*) AS 明細筆數,
    COUNT(DISTINCT SourceLabel + CAST(RequestId AS NVARCHAR(20))) AS 涉及單數,
    STRING_AGG(SourceLabel + N' ' + RequestNo + N'／' + Applicant + N'／' + ApprovalStatus, N'；')
        WITHIN GROUP (ORDER BY CreatedAt) AS 佔用清單
FROM #Invoices
GROUP BY InvoiceNo
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, InvoiceNo;

-- ─────────────────────────────────────────────────────────────────────────────
-- 【B】逐筆明細：同上但一列一單，供實際去系統上刪除 / 修正時對照
-- ─────────────────────────────────────────────────────────────────────────────
PRINT N'===== [B] 重複發票號碼逐筆明細 =====';

SELECT
    i.InvoiceNo,
    i.SourceLabel AS 單別,
    i.RequestNo AS 單號,
    i.RequestId AS 單據Id,
    i.Applicant AS 申請人,
    i.ApprovalStatus AS 狀態,
    i.CreatedAt AS 建立時間
FROM #Invoices i
WHERE i.InvoiceNo IN (SELECT InvoiceNo FROM #Invoices GROUP BY InvoiceNo HAVING COUNT(*) > 1)
ORDER BY i.InvoiceNo, i.CreatedAt;

DROP TABLE #Invoices;
