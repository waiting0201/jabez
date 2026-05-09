# 請款簽核流程

本文件定義 Jabez 系統的簽核流程主軸：簽核步驟、狀態流轉、撥款 / 退款通知、批次核准、自審跳過、上層級審核、申請人指定審核、跨步驟同人去重。

升級機制（找上層部門主管）詳見 [approval-escalation.md](approval-escalation.md)；PDF 簽名欄詳見 [pdf-signatures.md](pdf-signatures.md)。

## 簽核步驟（Seed 預設）

| 步驟 | 審核者 | 說明 |
|------|--------|------|
| Step 1 | 申請人部門的部門主管(JT=4) | 部門主管初核（`UseApplicantDepartment=true`） |
| Step 2 | 會計部主管(JT=4) | 取得紙本資料審核 |
| Step 3 | 財務部主管(JT=4) | 填入預計撥款日，核決及撥款 |
| Step 4 | 總監(JT=5, 總監室) | 最終核決 |

## 狀態流轉

```
draft → pending → approved / returned / rejected
```

- `draft`：草稿，可編輯
- `pending`：已送出，等待審核中（逐步推進 `CurrentStepOrder`）
- `approved`：所有步驟核准完成
- `returned`：退回申請人修改（可重新送出）
- `rejected`：拒絕（終止狀態）

## 核決後通知與撥款

當**最後一步**（Step 4 總監）核准後，系統自動：
1. 狀態變更為 `approved`
2. **通知申請人**：信件主旨 `[已核准] 請款申請 #XX`
3. **通知財務部全員**：信件主旨 `[可撥款] 請款申請 #XX 已核准`

財務部收到通知後，透過 `PATCH /payment-requests/{id}/payment-date` 填入：
- `EstimatedPaymentDate`：預計撥款日
- `PaidAt`：實際撥款日

> 此端點僅限**財務體系部門**（部門 Code ∈ AC / FIN / Jabez HQ / CEO，定義於 `Api/Common/Constants.cs` `DepartmentCodes.FinancialAndAbove`）或 **Superadmin** 操作。同樣規則套用於 `/advance-requests/{id}/payment-date`、`/travel-requests/{id}/payment-date`、`/travel-payment-requests/{id}/payment-date`、`/holiday-travel-requests/{id}/payment-date`，以及預支結案 / 出差結案端點。

### 撥款 / 退款完成通知申請人

當財務在以上端點將 `PaidAt`（或預支沖銷 / 出差沖銷的 `RefundedAt`）從 `null` → 有值時，系統自動同時透過 **Email + LINE Flex Message** 通知申請人：

| 觸發欄位轉換 | 適用申請類型 | 通知方法 |
|---|---|---|
| `PaidAt`（null → 有值） | payment_request / advance / travel / travel_payment | `NotifyApplicantPaidAsync` |
| `RefundedAt`（null → 有值） | advance / travel | `NotifyApplicantRefundedAsync` |

- **僅首次轉換**：之後若調整撥款日或退款日不會重發（避免騷擾）。
- **Email + LINE 雙軌**：與其他簽核通知一致；申請人未綁定 LINE 仍會收到 Email。
- **LINE Flex 模板**：`BuildApplicantPaidMessage` / `BuildApplicantRefundedMessage`（品牌綠 #4A6B3A，列出申請編號 / 金額 / 日期）。
- **金額來源**：撥款用 `TotalAmount` (payment) 或 `GrandTotal` (travel/advance/travel_payment)；退款用 `RefundedAmount`。

## 批次核准（全選核准）

擁有 `approval-tasks:batch-approve` 權限的使用者，可在簽核作業「待審核」頁籤勾選多筆待審申請一次核准。

- **動作限定**：僅支援 `approved`；退回/拒絕仍須進入詳情頁個別操作。
- **權限獨立**：批次核准為獨立權限，不依賴 `approval-tasks:write`；未擁有此權限者按鈕不顯示，後端亦回 403。
- **逐筆驗證**：每筆仍經過 `AuthorizeStepAsync`（職稱/部門/指定/升級），失敗者回報於 `failed` 清單，不中斷其他項目。
- **撥款類留空**：批次核准 payment_request / advance 時 `EstimatedPaymentDate`、`PaidAt` 留空，後端回傳 `pendingPayment` 清單，前端以 banner 提示使用者「前往補填」撥款/退款日。
- **沖銷結案不觸發**：批次核准不會設定 `CloseAdvance`；沖銷結案仍須於詳情頁或獨立結案端點操作。

## 自審跳過規則（僅限請款）

當申請人本身符合某步驟的審核者條件時（例如部門主管送出自己部門的請款），該步驟**自動跳過**（視為已通過），不觸發升級機制。若所有步驟都被跳過，申請**自動核准**。

此行為與加班/請假/出差不同 — 後者會觸發升級機制往上層部門找主管審核（詳見 [approval-escalation.md](approval-escalation.md)）。

## 上層級審核模式（UseDirectSupervisor）

`ApprovalStep` 新增 `UseDirectSupervisor`（bool, 預設 false）欄位，啟用時系統自動找同部門中層級最接近的上級作為審核者。

**層級判斷：** `JobTitle.Level` 數字越小 = 層級越高。上層級 = 同部門中 `Level < 申請人 Level` 且 `Level` 最大（最接近）的人。

**逐步往上爬：** 多個連續的 `UseDirectSupervisor` 步驟會自動往上找不同層級：
- 第 1 個上層級步驟（rank=0）→ 找最接近的上級（例如資深工程師）
- 第 2 個上層級步驟（rank=1）→ 找第 2 層上級（例如主任工程師）
- 第 N 個上層級步驟 → 找第 N 層上級
- rank 計算方式：該步驟前有幾個 `UseDirectSupervisor` 步驟

**規則：**
- 同層級有多人 → 全部通知，任一人審核即通過
- 找不到更高層級的人 → 該步驟自動跳過（視為通過）
- 所有步驟都跳過 → 自動核准
- 此模式不走 EscalationService 升級機制
- 啟用時自動忽略 `DepartmentId` 和 `JobTitleId`（隱含使用申請人部門）

**可與現有模式混用：** 每個 ApprovalStep 獨立判斷，例如 Step 1 用 `UseDirectSupervisor=true`，Step 2 也用 `UseDirectSupervisor=true`（自動往上一層），Step 3 維持固定部門 + 職稱。

**涉及元件：**
| 元件 | 說明 |
|------|------|
| `ApprovalStep.UseDirectSupervisor` | Entity 欄位 |
| `ApprovalFlowService.FindNthSuperiorLevelAsync()` | 找同部門第 N 層上級 |
| `ApprovalTaskHandler.AuthorizeStepAsync()` | 驗證審核者是否為正確層級的上級 |
| `PaymentRequestReadService.StepMatchClause()` | Dapper SQL 以 ROW_NUMBER 計算 rank 匹配審核者 |
| `ApprovalNotificationService.NotifyReviewersAsync()` | 通知正確層級的上級 |
| 前端 `approval-flow.html` | 設定頁 checkbox 開關 |

## 申請人指定審核模式（UseApplicantDesignated）

`ApprovalStep` 新增 `UseApplicantDesignated`（bool, 預設 false）欄位，啟用時審核者由申請人在表單中**依序指定多人**。

**設計背景：** 因跨部門專案支援情境，簽核流程因人員配置不同而不固定，故由申請人在送出時自行決定第一步驟要哪些人審核、以何順序。

**資料模型：** 不使用申請表本身的欄位，而是獨立資料表 `RequestDesignatedReviewers`：

| 欄位 | 說明 |
|------|------|
| `RequestType` | `payment_request` / `leave` / `travel` / `overtime` / `advance` / `write_off` |
| `RequestId` | 關聯申請單 ID |
| `ReviewerId` | 審核者 User ID |
| `StepOrder` | 審核順序（1, 2, 3...），依序逐一通過 |
| `Status` | `pending` / `approved` / `returned` |
| `ReviewedAt` | 審核時間 |
| `Comment` | 審核備注 |

**流程設計：**
- Step 1 為 `UseApplicantDesignated=true`：走指定審核者多人順序流程
- Step 2+ 回歸現有固定流程（固定部門+職稱、UseDirectSupervisor 等）

**規則：**
- 送出（submit）時，如果流程中有 `UseApplicantDesignated` 步驟，`designatedReviewers` 清單必填且至少 1 人。守門落在三層：
  - **前端 fail-fast**：9 個申請表單的 `submitForApproval()` 在 `form.invalid` 檢查後立即驗證 `hasDesignatedStep && designatedEntries.filter(e => e.selectedUserId).length === 0`，缺漏即顯示錯誤訊息不送 HTTP request
  - **後端 Handler 守門**：8 個 `*RequestHandler.SubmitAsync`（覆蓋 9 種申請類型，holiday-travel 與 travel 共用 `TravelRequestHandler`）在呼叫 `ResolveStartingStepAsync` 前先查 `ApprovalSteps` + `RequestDesignatedReviewers`，缺漏回 `BadRequest("此簽核流程包含申請人指定審核步驟，請提供指定審核者。")`
  - **`ApprovalFlowService` defense-in-depth**：[ApprovalFlowService.cs](../../Api/Services/ApprovalFlowService.cs) 在處理 `UseApplicantDesignated` 步驟時若 `designatedReviewers` 為 null/空，會 throw `AppException.BadRequest`（與 Handler 訊息一致），確保未來新增第 10 種申請類型若忘記抄 Handler 守門也不會無聲產生孤兒申請
- 依 `StepOrder` 升序逐一審核，前一人核准後才輪到下一人
- 指定審核者不需擁有全域 `approval-tasks:write` 權限，被指定即可審核（[ApprovalTaskHandler.cs:140-157](../../Api/Handlers/ApprovalTaskHandler.cs#L140-L157)）
- **批次核准（`POST /approval-tasks/batch-approve`）不支援指定審核者**身份；批次核准要求獨立的 `approval-tasks:batch-approve` 權限。
- 自審規則（依申請類型分為兩組，規則源於 [ApprovalFlowService.cs:51](../../Api/Services/ApprovalFlowService.cs#L51)）：
  - **Group A 全程禁止**（任一位置為申請人 → 報錯）：`leave` / `overtime` / `travel` / `travel_payment`
  - **Group B 首位跳過**（申請人排第 1 位 → 自動跳過此步驟；2+ 位置目前無強制檢查）：`payment_request` / `advance` / `write_off` / `travel_write_off` / `holiday_travel`
- 退回時：當前等待審核者狀態設為 `returned`，重送時所有指定審核者重置為 `pending`
- 此模式與 `UseDirectSupervisor`、`UseApplicantDepartment` 互斥（每個 ApprovalStep 擇一使用）
- 一個流程建議只有一個 `UseApplicantDesignated` 步驟

**存取控制（`GET /approval-tasks/{type}/{id}`）：**
- Superadmin：可查看所有
- 有 `approval-tasks:read` 權限：可查看所有
- 被指定為審核者（任何狀態）：可查看此申請單
- 曾審核過（有 ApprovalRecord）：可查看此申請單
- 其他人：403

**涉及元件：**
| 元件 | 說明 |
|------|------|
| `ApprovalStep.UseApplicantDesignated` | Entity 欄位 |
| `RequestDesignatedReviewer` | 獨立資料表，取代舊的單欄位設計 |
| `ApprovalFlowService.ResolveStartingStepAsync()` | 驗證指定審核者清單、自審規則、解析起始步驟 |
| `ApprovalTaskHandler.AuthorizeStepAsync()` | 驗證當前等待審核者（min StepOrder, Status=pending） |
| `ApprovalTaskHandler.ProcessReviewAsync()` | 核准後推進到下一位指定審核者，全部通過後推進 ApprovalStep |
| `PaymentRequestReadService.StepMatchClause()` | Dapper SQL：匹配 min(StepOrder) 且 Status=pending 的指定審核者 |
| `ApprovalTaskHandler.GetByIdAsync()` | 單筆查詢含存取控制 |
| 前端各申請表單 | 動態新增/刪除/排序多位指定審核者 UI |

## 跨步驟同人去重（限縮：總監 OR 相鄰 step）

> **2026-05 規則限縮**：原本「全歷史」去重對所有審核者生效，過於激進；非總監若在跨多個 step 後再回到同一審核者，可能是流程設計需要分階段把關。新規則只對「總監 (`JobTitle.Level == 1`)」或「相鄰 step 同人」自動跳過 + 代簽，其餘場景要求重新審核。

任一申請進行中時，後續任意 step 的解析審核者池被「該申請已 approved 的所有 ReviewedById」完全覆蓋時，是否自動跳過 + 代簽，依下表判定：

| 情境 | 行為 |
|---|---|
| 池中仍有未審者 | 通知未審者（仍排除已審總監） |
| 池被覆蓋 + 代簽人 `JobTitle.Level == 1`（總監） | **跳過 + 寫代簽** |
| 池被覆蓋 + 代簽人非總監 + 與「上一個有審核紀錄的 step」相鄰 | **跳過 + 寫代簽** |
| 池被覆蓋 + 代簽人非總監 + 不相鄰 | **不跳過**，停在此 step（要求重審） |
| 同一 designated step 內 multi-designee 同人 | **維持原樣，自動代簽**（同 step 內延續，視為「比相鄰更緊」，不論角色） |

「相鄰」精確定義：以 `ApprovalSteps` 依 `StepOrder` 排序後的索引為準，當前 step 索引 == 上一審核 step 索引 + 1（避免稀疏 StepOrder 數值差距誤判）。連鎖跳過時，每跳過一步即更新「上一審核 step」為剛跳過者，下個 step 仍可能算相鄰。

**統一自動代簽**：當某 step 因新規則跳過時，**一律寫一筆代簽 `ApprovalRecord`**（含 `Action='approved' / ReviewedById=代簽人 / ReviewNote='自動核准：已於先前步驟核准本申請'`），讓 PDF 簽名欄、簽核時間軸能正確顯示已審者的簽名。代簽人選擇邏輯：取「step 池 ∩ 歷史已審者」交集後按 `ApprovalRecords.ReviewedAt` 升序取首位（最早審過此申請者）。

**指定審核步驟（`UseApplicantDesignated`）內部**：[ApprovalTaskHandler.cs](../../Api/Handlers/ApprovalTaskHandler.cs) `ProcessReviewAsync` 中以 `while` 迴圈推進 — 下一位 designee 若已於先前步驟核准 → 自動標記 `RequestDesignatedReviewer.Status='approved'` + `Comment='已於先前步驟審核（自動核准）'`，並寫一筆代簽 `ApprovalRecord`，繼續找再下一位；遇到沒在歷史中的 designee 才停下並通知。**此邏輯不受新規則限縮影響**（同 step 內延續）。

**外層整 step 跳過 designated**：當外層 `SkipUnreviewableStepsAsync` 偵測到某未抵達的 designated step 全部 designee 都已在歷史中 → 依新規則判斷（總監 OR 相鄰）→ 整步跳過時，並把該申請所有 pending designee 都設為 approved（保持 `RequestDesignatedReviewers` 與 `ApprovalRecord` 狀態一致）。

**所有剩餘步驟皆被自動代簽** → 申請自動核准 + 通知申請人。

**AuthorizeStepAsync 防呆**：限縮為「總監（`JobTitle.Level == 1`）reviewer 重複 PATCH」→ 400「您已在先前步驟核准過此申請，不需重複審核」。非總監允許重審（與新規則對齊）。

**待審清單同步**：[PaymentRequestReadService.StepMatchClause](../../Api/Services/Dapper/PaymentRequestReadService.cs) pending tab 的 `NOT EXISTS` 子句加上「reviewer 是 Level=1」條件，僅排除「總監已被自動代簽」的殘留待審項目。非總監若不滿足跳過條件 → 該 step 正常顯示在待審清單中。

**代理審核**：以 `ReviewedById`（實際點按者）為去重依據，`OnBehalfOfUserId`（受代理人）不算已審。

**退回重送 → 歷史清零**：以 `ApprovalRecords` 中最近一次 `Action='returned'` 的 `ReviewedAt` 當分隔線，僅計入該時點之後的 approved 紀錄。退回前審過的人重送後仍須再審。不需新增 schema、不影響稽核軌跡（紀錄全保留）。

**升級審核排除**：[EscalationService.FindManagerInDepartmentAsync](../../Api/Services/EscalationService.cs) 的 `excludeUserIds` 語義改為「總監（Level=1）已審者」。實務上 escalation 鏈停在總監前，此調整理論上影響極小，但維持與 `SkipUnreviewableStepsAsync` 邏輯一致。

**涉及元件：**
| 元件 | 說明 |
|---|---|
| `IApprovalFlowService.GetApprovedReviewerIdsAsync` | 共用 helper：取最近一次 returned 之後的 approved ReviewedById HashSet（全部 reviewers，不分 Level） |
| `IApprovalFlowService.GetApprovedSupervisorIdsAsync` | **新增**：同上但只回「總監（Level=1）」的子集 — 條件 (A) 去重池 |
| `ApprovalFlowService.SkipUnreviewableStepsAsync` | 新增 `supervisorIds` / `priorStepOrder` 兩個參數；內部 `adjacencyAnchorStepOrder` 連鎖跳過時更新 |
| `SkippedStepInfo` record | `(StepOrder, ProxyApproverId, IsApplicantDesignated)` — 跨服務溝通跳過資訊 |
| `ApprovalFlowService.ResolveReviewerPoolAsync` | 取代舊 `ResolveUniqueReviewerAsync`，回傳整個 reviewer 池 List<Guid>（最多 50 筆防呆） |
| `ApprovalFlowService.PickEarliestProxyAsync` | 從池 ∩ 已審者中按 ApprovalRecords.ReviewedAt 升序取首位作代簽人 |
| `ApprovalTaskHandler.ProcessReviewAsync` | 呼叫 `SkipUnreviewableStepsAsync` 時額外傳 `supervisorIds`（含 ChangeTracker pending Level=1 reviewers）+ `priorStepOrder=currentStepOrder` |
| `ApprovalTaskHandler.AuthorizeStepAsync` | 防呆限縮：先查 reviewer 的 JobTitle.Level，僅 Level=1 已審者 throw 400 |
| `ApprovalNotificationService.GetApprovedReviewerIdsAsync` | 私用 helper，邏輯改為只回 Level=1 已審者（與 IApprovalFlowService.GetApprovedSupervisorIdsAsync 一致） |
| `ApprovalNotificationService.NotifyReviewersAsync` | 排除集合改為「總監已審者」，相鄰跳過由 SkipUnreviewableStepsAsync 在進入該 step 前先處理 |
| `ApprovalNotificationService.NotifySpecificReviewerAsync` | 入口檢查 reviewerId 是否在「總監已審者」集合中，是則 no-op |
| `IEscalationService.TryEscalateAsync` | 文件更新：`excludeUserIds` 應為 supervisorIds（與 SkipUnreviewableStepsAsync 來源一致） |
| `PaymentRequestReadService.StepMatchClause` | `NOT EXISTS` 子句改為 `NOT (EXISTS Level=1 AND EXISTS ApprovalRecords...)` — 僅排除總監殘留 |

---

## 跨業務關聯

- **9 種申請表的 Group A / B 自審分組** → [application-forms.md](application-forms.md)
- **加班 / 請假 / 出差升級機制**（找上層部門主管 + 代理人） → [approval-escalation.md](approval-escalation.md)
- **核決後的 PDF 簽名欄渲染** → [pdf-signatures.md](pdf-signatures.md)
- **撥款 / 退款日 LINE 通知模板** → [line-integration.md](line-integration.md)
- **撥款日端點權限**（部門 Code AC/FIN/Jabez HQ/CEO） → [department-visibility.md](department-visibility.md)
- **API 端點清單** → [api-routes.md §審核任務](../api-routes.md#審核任務)
- **Entity（ApprovalItem / Step / Record / Override / RequestDesignatedReviewer）** → [database-schema.md](../database-schema.md)
