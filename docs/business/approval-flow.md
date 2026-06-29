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

## 依部門挑流程（部門專屬 + 階層繼承 + 通用 fallback，2026-06 新增）

`ApprovalItem` 新增 `DepartmentId`（nullable）欄位，讓**同一申請類型可同時存在多個流程**：

- `DepartmentId == null` → 該申請類型的**通用預設流程**（fallback，每類型至多一個）
- `DepartmentId == X` → **部門 X 專屬流程**（每 (類型, 部門) 至多一個）
- 唯一索引由「`ApplicationType` 唯一」改為「`(ApplicationType, DepartmentId)` 唯一」，且帶過濾條件 `HasFilter("[ApplicationType] IS NOT NULL")`（僅在 `ApplicationType` 非 null 時檢查唯一性）；FK→Department `OnDelete=SetNull`（部門刪除時該流程自動退回為通用預設）。

**送單挑流程**：8 個 `*RequestHandler.SubmitAsync` 在首次送出（`ApprovalItemId is null`）時呼叫共用 helper [ApprovalFlowService.ResolveApprovalItemIdAsync](../../Api/Services/ApprovalFlowService.cs)`(applicationType, applicantDepartmentId)`：

```
優先序（由高到低，取最先命中者）：
  1. DepartmentId == 申請人部門（自身專屬流程）
  2. DepartmentId == 最近祖先部門（沿 Department.ParentId 逐層往上，距離越近越優先）
  3. DepartmentId == null（通用預設）
  皆無：ApprovalItemId 保持 null（等同無簽核流程）
```

- **部門階層繼承（2026-06 強化）**：子部門未設專屬流程時，會**自動沿用最近一層有設定流程的上層部門**，不必為每個子部門各複製一份。例如「營運管理部」設了流程、其下三個子部門未設，則三個子部門送單時自動套用營運管理部的流程；某子部門若另設了自己的專屬流程，則以子部門自身的為準（距離 0 最優先）。實作以 `ParentId` 建立部門鏈（EF 版記憶體往上走、Dapper `/active` 版用遞迴 CTE），兩處優先序必須一致。
  - **距離計算**：EF 版用 `chain.IndexOf(DepartmentId)`（索引 0 = 自身部門最優先，越大越遠，通用預設取 `int.MaxValue` 墊底）；Dapper 版用遞迴 CTE 的 `Depth` 欄位（`Depth=0` 自身，越大越遠，通用預設取 `2147483647` 墊底）。兩者邏輯等價。
- 申請人部門取自 `submitter.DepartmentId`（送出者本人）。Superadmin 無部門 → 落到通用預設（但 Superadmin 另有自動核准捷徑，通常不經此）。
- **退回重送不重挑**：`ApprovalItemId` 僅在首次送出解析，之後沿用，確保流程一致。
- **步驟解析 / 待審清單不受影響**：`ResolveStartingStepAsync` 與 `StepMatchClause` 讀的是申請單上已存的 `ApprovalItemId`，與「哪個流程」解耦，天然相容。
- **`GET /approval-items/active`**（申請表單偵測指定審核步驟用）同樣部門感知：以 JWT `department_id` 套用相同 fallback，回傳「呼叫者實際會走」的流程，由 [ApprovalReadService.GetActiveByTypeAsync](../../Api/Services/Dapper/ApprovalReadService.cs) 子查詢挑單一流程後聚合 steps。
- **設定頁**（[approval-list](../../Admin/src/app/features/admin/approvals/pages/approval-list/)）新增「適用部門」下拉（含「通用（預設）」= null），列表多一欄顯示部門；建立 / 編輯以 `(類型, 部門)` 判重，後端重複回 409。

> 典型用法：步驟結構大致相同、只差某一關審核主管時，**複製通用預設流程 → 改那一關的部門 / 職稱 → 綁定該部門**即可。
> 一個父部門帶多個子部門時，**只需在父部門設一份流程**，子部門即自動繼承（除非子部門自設專屬流程覆蓋）。

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

### 分期撥款（Installments，2026-05 上線；2026-05 Phase 2 完成）

4 種申請類型（payment_request / advance / travel / travel_payment）支援**多筆分期撥款**，撥款資料**單一真相**＝子表 `XxxInstallment[]`：

- **填寫時機**：財務（FIN）步驟**核准當下**即填預計撥款日 + 各期金額，透過 `PATCH /approval-tasks/{appType}/{id}/review` 的 `installments` 欄位**與審核同交易原子寫入**；此時撥款明細**必填**（加總須 == 申請總額，否則不可核准）。核准後仍可在「設定撥款明細」區塊透過獨立 endpoint 修改未撥列 / 填實際撥款日。
  - 例外：`holiday_travel`（假日執行活動）不在 review 流程填撥款明細，僅走核准後的獨立 endpoint。
  - 批次核准不填撥款明細，最終 approved 後由「待補撥款」提醒（`BuildPendingPaymentReminderAsync`）追蹤。
- **獨立 endpoint**：`PATCH /{type}-requests/{id}/installments`（舊 `/payment-date` 已於 Phase 2 移除）；**僅 ApprovalStatus == approved 可呼叫**（4 種一致；review 路徑因在核准同交易內寫入故不經此守衛）
- **DTO**：`UpsertInstallmentsRequest { installments[], approvalStatus? }`，每筆 `{ id?, installmentNo, expectedDate, paidAt?, amount, note? }`
- **持久化核心共用**：`InstallmentUpsertService.Apply`（validate + diff，**不 SaveChanges**，交易邊界交呼叫端）— 獨立 endpoint 與 review 原子寫入共用同一份邏輯；4 種子表實作 `IInstallmentEntity` 介面以泛型化
- **驗證**（`InstallmentValidator.Validate`）：
  - 序號 1-based 連續無斷號
  - SUM(amount) == 申請總額（PaymentRequest.TotalAmount / 其他三者的 GrandTotal）容忍 0.01 浮點誤差
  - **已 PaidAt 列保護**：ExpectedDate / Amount / PaidAt 三欄全鎖死、不可刪除
  - 未撥列完全可改可刪
- **每筆 PaidAt null→value 觸發一次通知**：`NotifyApplicantPaidAsync` 加 `installmentNo` / `totalInstallments` 參數，Email + LINE Flex 標題附「（第 N/M 期）」
- **三態 status**（`PaymentInstallmentStatus` enum）：
  - `Unpaid`：installments 為空或所有 PaidAt 為 null
  - `PartiallyPaid`：部分 PaidAt 有值
  - `FullyPaid`：所有 PaidAt 都有值
- **List filter 三態**：[PaymentRequestReadService](../../Api/Services/Dapper/PaymentRequestReadService.cs) 的 `PaymentStatusClause` 用 `EXISTS / NOT EXISTS` 子查詢 `XxxInstallments` 對應三態：
  - `paid` = 有 installments 且所有 PaidAt 非 null（FullyPaid）
  - `partial` = 至少一期 PaidAt 非 null 且至少一期 PaidAt 為 null（PartiallyPaid）
  - `unpaid` = 無 installments 或所有 PaidAt 為 null（Unpaid）
  - 簽核作業 → 已核准 Tab 的篩選按鈕對應：`全部` / `尚未撥款` (`unpaid`) / `部分撥款` (`partial`) / `全部撥款` (`paid`)
  - 沖銷類（write_off / travel_write_off）退款仍以父表 `RefundedAt` 兩態判斷；遇 `paymentStatus=partial` 時整批 `1=0` 短路（沖銷無分期概念）
- **PDF 出納簽名章**：取 `installments[]` 中最後一期已撥款者的 `PaidBySignatureUrl` + `PaidAt`

### 撥款明細編輯 UI 限制（[approval-task-review](../../Admin/src/app/features/admin/approval-tasks/pages/approval-task-review/)）

簽核作業頁同時在 2 個區塊提供撥款明細編輯（待審核 = 計劃用、已核准 = 實際維護）。前端規則：

| 元件 | 禁用條件 |
|------|---------|
| **「+ 新增一期」按鈕** | `SUM(已填金額) ≥ 申請總額`（容忍 0.01）**或** `paymentStatus = 'FullyPaid'`。避免新增 0 元空期或讓 SUM 超過總額。 |
| **「儲存撥款明細」按鈕** | `SUM ≠ 申請總額`（容忍 0.01）**或** `paymentStatus = 'FullyPaid'`。FullyPaid 時所有列鎖定，無可儲存內容。 |
| **金額 input** | `min="1" step="1"`（整數，不可 0 或負）；`max = 申請總額 − 其他列已填金額`（剩餘額度）。已撥款列：`readonly` + 灰底。 |
| **預計撥款日 / 實際撥款日 input** | 已撥款列：`readonly` + 灰底。 |
| **備註 input** | 已撥款列：`readonly` + 灰底（避免修改歷史紀錄）。 |
| **刪除按鈕（⨯）** | 已撥款列：完全隱藏。只剩 1 列時也隱藏（避免清空）。 |

標題列顯示「剩餘 X 元」hint 即時反映 `申請總額 − installmentsSum()`，配合按鈕禁用狀態給使用者明確視覺回饋。

> 上述限制由 helpers `canAddInstallmentRow / isInstallmentsSumValid / isFullyPaid / installmentRowMax / isInstallmentLocked` 統一掌控；後端 `InstallmentValidator.Validate` 提供等同的伺服端防線。

> 此端點仍限**財務體系部門**（部門 Code ∈ AC / FIN / Jabez HQ / CEO，`DepartmentCodes.FinancialAndAbove`）或 **Superadmin** 操作。

歷史：原採兩階段過渡，Phase 1 父表保留 `EstimatedPaymentDate` / `PaidAt` / `PaidByUserId` 作 cache 由 Handler 同步寫回。2026-05 Phase 2 由 [BackfillInstallmentsFromParentCache](../../Api/Data/Migrations/) → [RemovePaymentDateCacheFromParents](../../Api/Data/Migrations/) 兩個 migration 拆除父表 cache。

### 撥款日將屆提醒（PaymentReminderFunction，2026-05 新增）

每日 09:00 (Taipei) `TimerTrigger` 自動執行：
- 撈出所有「PaidAt 為空 + ExpectedDate ≤ 今天+N 天」的 installments（4 種申請類型 UNION）
- N 由 `SystemSetting.PaymentReminderDaysBefore` 控制（預設 3，0-30）
- 對**財務體系部門全員**各推一則彙整通知（Email + LINE，沿用 `ApprovalEmailEnabled` + `ApprovalLineEnabled` 開關）
- 同日同人去重（`PaymentReminderLog` 記錄 success 後當日不再推）
- 手動觸發：Superadmin 可從 `/admin/payment-reminder-logs` 頁面或 `POST /api/admin/payment-reminder/run` 觸發
- cron 由 `PaymentReminderCron` app setting 控制（預設 `0 0 1 * * *`，即 UTC 01:00 = Taipei 09:00）

### 撥款 / 退款完成通知申請人

當財務將 `PaidAt`（或預支沖銷 / 出差沖銷的 `RefundedAt`）從 `null` → 有值時，系統自動同時透過 **Email + LINE Flex Message** 通知申請人：

| 觸發欄位轉換 | 適用申請類型 | 通知方法 |
|---|---|---|
| installment.`PaidAt`（null → 有值） | payment_request / advance / travel / travel_payment | `NotifyApplicantPaidAsync`（含 N/M 期）|
| `RefundedAt`（null → 有值） | advance / travel | `NotifyApplicantRefundedAsync` |

- **分期情境**：每填一筆 PaidAt 都推一次通知（含「第 N/M 期」），不只首次。
- **Email + LINE 雙軌**：與其他簽核通知一致；申請人未綁定 LINE 仍會收到 Email。
- **LINE Flex 模板**：`BuildApplicantPaidMessage` / `BuildApplicantRefundedMessage`（品牌綠 #4A6B3A，列出申請編號 / 金額 / 日期 / 期數）。
- **金額來源**：撥款分期用 `installment.Amount`；退款用 `RefundedAmount`。

## 批次核准（全選核准）

擁有 `approval-tasks:batch-approve` 權限的使用者，可在簽核作業「待審核」頁籤勾選多筆待審申請一次核准。

- **動作限定**：僅支援 `approved`；退回/拒絕仍須進入詳情頁個別操作。
- **權限獨立**：批次核准為獨立權限，不依賴 `approval-tasks:write`；未擁有此權限者按鈕不顯示，後端亦回 403。
- **逐筆驗證**：每筆仍經過 `AuthorizeStepAsync`（職稱/部門/指定/升級），失敗者回報於 `failed` 清單，不中斷其他項目。
- **撥款類留空**：批次核准 payment_request / advance / travel / travel_payment 時不會建立 installments，後端回傳 `pendingPayment` 清單（檢查條件：無 installments 或仍有 PaidAt 為空），前端以 banner 提示使用者「前往補填撥款明細」。
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
| `ApprovalStepOrder` | **此 designee 所屬的 `ApprovalStep.StepOrder`**（區分同一申請的多個 designated 步驟）|
| `StepOrder` | **同一步驟內**的審核次序（1, 2, 3...），依序逐一通過 |
| `SelectedDepartmentId` | 第二步「先選部門→再選人」時申請人選的部門（僅記錄 / 回填用，授權不使用）|
| `Status` | `pending` / `approved` / `returned` |
| `ReviewedAt` | 審核時間 |
| `Comment` | 審核備注 |

**流程設計：**
- 一條流程**可有多個 `UseApplicantDesignated` 步驟**，每筆 designee 以 `ApprovalStepOrder` 綁定所屬步驟，引擎所有 designee 查詢一律以「當前 `CurrentStepOrder`」過濾（[ApprovalTaskHandler](../../Api/Handlers/ApprovalTaskHandler.cs) `AuthorizeStepAsync` / `ProcessReviewAsync` 整步跳過 / returned；[ApprovalFlowService](../../Api/Services/ApprovalFlowService.cs) `ResolveStartingStepAsync` / `SkipUnreviewableStepsAsync` / `ResolveReviewerPoolAsync`），不會跨步驟混淆或誤核准。
- **第二步「先選部門→再選人」**：當某 designated step 設 `DesignatedRequiresDepartment=true`（[ApprovalStep](../../Api/Models/Entities/ApprovalStep.cs) 新旗標，僅 `UseApplicantDesignated=true` 時有意義），前端送單該步改為「先選部門→篩出該部門的人→選一位」；後端仍只存 `ReviewerId`，`SelectedDepartmentId` 僅供回填 / 稽核。
- 一般情境：Step 1 為 `UseApplicantDesignated=true` 指定審核，Step 2+ 回歸固定流程（固定部門+職稱、UseDirectSupervisor 等）。
- 送單建立 / 讀取 / 正規化 designee 由共用 [DesignatedReviewerHelper](../../Api/Common/DesignatedReviewerHelper.cs)（`BuildEntities` / `ReadForFlowAsync` / `ValidateAndNormalizeAsync`）統一處理：舊 payload 未帶 `ApprovalStepOrder`（=0）且流程只有一個 designated step 時自動補成該 step 的 StepOrder（向後相容）。

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
  - **Group B 首位跳過**（申請人排第 1 位 → 自動跳過此步驟；2+ 位置目前無強制檢查）：`payment_request` / `advance` / `write_off` / `travel_write_off` / `holiday_travel` / `pre_review`
- 退回時：當前等待審核者狀態設為 `returned`，重送時所有指定審核者重置為 `pending`
- **刪除申請單時連帶清除審核足跡**：`RequestDesignatedReviewer` / `ApprovalRecord` / `EscalationOverride` 皆以多型 `RequestType(ApplicationType) + RequestId(ApplicationId)` 關聯父表、**無真正 FK**，EF Cascade 不會處理。故 8 個 `*RequestHandler.DeleteAsync`（草稿 / 退回才可刪）在 `Remove(申請單)` 前須以同一 `RequestType` 字串 `RemoveRange` 這三張表的對應列，否則殘留列會以 `OnDelete(NoAction)` 的 `ReviewerId` / `ReviewedById` 外鍵長期掛在 `Users`，導致日後**無法刪除該使用者**
- 此模式與 `UseDirectSupervisor`、`UseApplicantDepartment` 互斥（每個 ApprovalStep 擇一使用）
- 一條流程**可有多個 `UseApplicantDesignated` 步驟**（每步申請人各自指定、依序簽核；以 `ApprovalStepOrder` 隔離）。整步跳過只影響「被跳過的那一步」的 designee，不會誤核准其他 designated 步驟。
- 唯一索引為 `(RequestType, RequestId, ApprovalStepOrder, ReviewerId)`：允許不同步驟指定同一人，但同一步驟內不可重複指定同一人。

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
