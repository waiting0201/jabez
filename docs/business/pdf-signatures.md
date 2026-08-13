# PDF 簽名欄

7 個含簽名檔的 PDF（請款 / 預支 / 出差預支 / 出差預支沖銷 / 出差請款 / 預支沖銷 / 假日執行活動）共用 [Admin/src/app/shared/services/pdf-core.service.ts](../../Admin/src/app/shared/services/pdf-core.service.ts) 的 `buildDynamicSignBlocks()` helper，依 `flow.steps` 動態建立簽名欄。

## 何時可以列印（2026-08 統一）

這 7 種即「紙本財務單」，送出成功彈窗要求「**單位主管簽核完畢後**，再印出<單別>連同紙本單據寄回會計室」——紙本在流程**中途**就要印，故：

- **申請詳情頁的列印按鈕條件一律 `approvalStatus !== 'draft'`**（不是 `=== 'approved'`）；已簽的關卡帶簽章與日期、未簽的留白
- PDF service 內**不得再放 `status !== 'approved'` 的閘**（只擋資料不足），否則按鈕看得到、按了沒反應
- 前端規範見 [frontend-design.md §8.6](../frontend-design.md#86-列印-pdf-按鈕的顯示條件)
- 預審申請不走紙本流程，維持 `approved` 才可印；簽核作業頁（審核者側）亦維持 `approved`

## 規則

1. **每個 step 一格**：依 `stepOrder` 為每個**非指定簽核**步驟建一格簽名欄
2. **欄位順序**：建立後反轉 → 最高 stepOrder 在最左、最低在最右（最後簽核者位於申請者左側）。**「總監」一律排在最左**：當總監僅來自指定簽核（情境 C）時，仍會推到最左；flow 與指定皆有總監（情境 D/E）時，「總監核准」在最左、「總監（指定）」緊接其右
3. **Label 由 step 推斷**（依序判定，`resolveStepLabel`）：
   - `useDirectSupervisor=true` → `上層級`
   - 總監步驟（`isDirectorStep`）→ `總監核准`。判定優先用 `jobTitleLevel === 1` 或 `departmentCode === 'Office of the Director'`（職稱/部門**改名不受影響**）；Level / Code 缺值時（舊資料）才 fallback 名稱含「總監」
   - `departmentName` 含「財務」→ `財務部簽核`
   - `departmentName` 含「會計」→ `會計`
   - 其他 → `note` || `departmentName` || `jobTitleName` || `Step N`
4. **未審核的 step 渲染為空欄**（保留位置）
5. **簽核批次過濾（`roundNo`，2026-07 新增）**：`buildDynamicSignBlocks` 進入點先以 `opts.roundNo ?? 1` 過濾 `records`（`(r.roundNo ?? 1) === roundNo`）。**追加預支**時同一張單會有多輪簽核紀錄併存，不過濾的話 `records.find(r => r.stepOrder === …)` 永遠取到第 1 輪 → **PDF 會印出前一輪的簽章與日期**。`AdvancePdfService` 傳入 `r.currentRoundNo`；其餘 6 個 PDF 不傳（預設 1，行為不變）

## 指定簽核者（`useApplicantDesignated`）特殊處理

**只有「原生」指定簽核步驟（`flow.steps[].useApplicantDesignated === true`）不獨立佔欄位** —— 這種步驟沒有固定角色（誰簽由申請人挑），畫一格固定標籤沒有意義。

> ⚠️ **「例外指定審核」命中的步驟照常佔一格**（2026-08 修正）。例外指定審核只改變「由誰挑審核者」，步驟本身的角色是固定的（上層級 / 會計 / 財務…），也**確實會產生該 `stepOrder` 的 `ApprovalRecord`**。2026-07 導入例外指定審核時誤把「不佔欄」規則一併套到例外命中的步驟，導致該關卡整格從 PDF 消失、簽章遺失（實務上各流程的 Step 1「上層級」普遍掛例外名單，8 種 PDF 全中）。詳見 [approval-flow.md](approval-flow.md#pdf-簽名欄)。

`designatedStepOrders`（由 [pdf-core.service.ts](../../Admin/src/app/shared/services/pdf-core.service.ts) 的 `designatedStepOrdersOf(request.designatedReviewers)` 取自各筆 designee 的 `approvalStepOrder`，8 個 PDF service 各傳一次）現僅用於**辨識例外命中的步驟以挑選紀錄**：該步驟可能有多位 designee → 同 `stepOrder` 多筆紀錄（含同步驟自動代簽），故取**最後一筆 `approved`**，取不到才退回 `records.find(stepOrder)`。一般步驟維持 `find`。

例外：若**原生**指定簽核紀錄裡有人為總監（`isDirectorReviewer`：優先用 `reviewerJobTitleLevel === 1` 判定，缺值時 fallback 職稱名稱含「總監」）：

| 情境 | flow 有總監步驟 | 指定簽核含總監 | 結果 |
|---|---|---|---|
| A | ✓ | ✗ | 1 個總監核准欄（flow step 那位），位於最左 |
| B | ✗ | ✗ | 不顯示總監欄 |
| C | ✗ | ✓ | 1 個總監核准欄（指定的總監），位於最左 |
| D/E | ✓ | ✓ | **2 欄並列（左→右）**：總監核准 + 總監（指定）— 不論同人或不同人 |

> 上表僅適用**原生**指定簽核步驟。多位指定總監：取最後一筆（最新核准）。其他非總監的原生指定簽核者，簽名**不**顯示在 PDF（例外指定審核者則顯示在該步驟自己的欄位裡）。

## 出納欄與申請者欄

固定欄位，不依 flow 動態：

| PDF 類型 | 含出納 | 出納簽名來源 |
|---|---|---|
| 請款 / 預支 / 出差預支 / 出差請款 | ✓ | `installments[]` 取**最後一期已撥款**的 `PaidByUserId` + `PaidAt`（從子表推算，父表已無 cache）|
| 預支沖銷 | ✓ | `refundedBy` + `refundedAt`（沖銷對應的預支父表 RefundedByUserId / RefundedAt）|
| 出差預支沖銷 / 假日執行活動 | ✗ | — |

申請者欄永遠在最右，標籤為 `請款人`（payment）或 `申請者`（其他）。

## 涉及元件

| 元件 | 說明 |
|------|------|
| [pdf-core.service.ts](../../Admin/src/app/shared/services/pdf-core.service.ts) `buildDynamicSignBlocks` | 共用 helper，所有動態簽名欄邏輯集中此處 |
| [pdf-core.service.ts](../../Admin/src/app/shared/services/pdf-core.service.ts) `resolveStepLabel` | step → label 規則（內部 function）|
| `ApprovalRecordDto.ReviewerJobTitle` | 後端 DTO，[PaymentRequestDtos.cs](../../Api/Models/Dtos/PaymentRequestDtos.cs) |
| `recordSql` | Dapper SQL `LEFT JOIN JobTitles` 取審核者職稱，[PaymentRequestReadService.cs](../../Api/Services/Dapper/PaymentRequestReadService.cs) |
| `ApprovalRecord.reviewerJobTitle` | 前端 interface，[approval-task.model.ts](../../Admin/src/app/features/admin/approval-tasks/models/approval-task.model.ts) |
| 7 個 PDF service 的 `_buildSignBlocks` | thin wrapper 呼叫共用 helper，差異僅 `cashier` 設定與 `applicantLabel` |

> **資料來源**：簽名章 / 簽核日期 / 動態簽名欄全部來自 `GET /approval-tasks/{appType}/{id}`（`flow` + `approvalRecords` + `submittedBySignatureUrl`）。取不到（403）時 `flow?.steps ?? []` 會產生**零格簽核欄**，印出無簽核欄的單子——故該端點對**申請人本人**放行，見 [approval-flow.md](approval-flow.md) 的「存取控制（`GET /approval-tasks/{type}/{id}`）」。

---

## 請款單頂端人員標籤

請款單 PDF 標題下方第一列左側顯示申請人姓名，標籤依 `paymentDetail.paymentType` 切換：

| `paymentType` | 標籤 | 理由 |
|---|---|---|
| `vendor`（廠商請款） | **請款人：** | 實際受款人是廠商（顯示於下方「受款人資訊」區塊），這位是提出請款的員工 |
| 其他（`general` / `travel` / `business_trip`） | **受款人：** | 員工本人即為受款人 |

實作位置：[payment-pdf.service.ts](../../Admin/src/app/features/admin/payment-requests/services/payment-pdf.service.ts) `printPaymentRequest()` 內的 `payerLabel` 判斷。

## 請款單受款人資訊（廠商請款專用）

請款單 PDF（[payment-pdf.service.ts](../../Admin/src/app/features/admin/payment-requests/services/payment-pdf.service.ts)）當 `paymentDetail.paymentType === 'vendor'` 時，會在明細表格下方、撥款日列上方額外渲染「受款人資訊」區塊：

| 欄位 | 來源（Vendor 實體） |
|------|--------------------|
| 廠商名稱 | `Name` |
| 統編     | `TaxId` |
| 聯絡人   | `ContactPerson` |
| 聯絡電話 | `Phone` |
| 帳戶資料 | `BankAccount`（存摺封面圖不嵌入） |
| 公司地址 | `Address` |

空值統一顯示「—」。其他請款類型（`travel` / `general` / `business_trip`）**不**渲染此區塊，版面保持原狀。

資料管線：

| 層級 | 檔案 |
|------|------|
| SQL JOIN | [PaymentRequestReadService.cs](../../Api/Services/Dapper/PaymentRequestReadService.cs) `paymentSql` 已 JOIN `Vendors`，新增 4 個欄位（ContactPerson / Phone / BankAccount / Address）|
| DTO      | [PaymentRequestDtos.cs](../../Api/Models/Dtos/PaymentRequestDtos.cs) `PaymentTaskDetailDto` |
| 前端 model | [approval-task.model.ts](../../Admin/src/app/features/admin/approval-tasks/models/approval-task.model.ts) `PaymentTaskDetail` |

---

## 跨業務關聯

- **簽核流程主軸**（簽核步驟、狀態、指定審核） → [approval-flow.md](approval-flow.md)
- **跨步驟同人去重的代簽 ApprovalRecord** → [approval-flow.md §跨步驟同人去重](approval-flow.md#跨步驟同人去重限縮總監-or-相鄰-step)
- **PDF 用 jsPDF 共用機制** → [frontend-design.md](../frontend-design.md)
- **簽名檔 Blob 代理路由** → [api-routes.md §檔案代理](../api-routes.md#檔案代理blob-storage)
