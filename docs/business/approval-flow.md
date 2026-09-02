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
- **審核任務 / 詳情頁顯示的簽核流程**：[PaymentRequestReadService](../../Api/Services/Dapper/PaymentRequestReadService.cs) 的 flow lookup **以 `ApprovalItem.Id` 為 key（對應申請列已存的 `ApprovalItemId`）**，而非以 `ApplicationType` 為 key——否則同一類型有多個流程（部門專屬 + 通用預設）時會把各 `ApprovalItem` 的 steps 合併到同一條流程，造成 review 頁簽核流程**重複顯示**。每張申請只顯示自己送單時解析到的那條流程；`ApprovalItemId` 為空（理論上不應發生）時退回該類型通用預設（無則取最小 Id）。
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

**5 種**申請類型（payment_request / advance / travel / travel_payment / **write_off**）支援**多筆分期撥款**，撥款資料**單一真相**＝子表 `XxxInstallment[]`（沖銷差額分期為 2026-07 新增，規則見下方「預支沖銷差額分期撥款」）：

- **填寫時機**：財務撥款步驟**核准當下**即填預計撥款日 + 各期金額，透過 `PATCH /approval-tasks/{appType}/{id}/review` 的 `installments` 欄位**與審核同交易原子寫入**；此時撥款明細**必填**（加總須 == 申請總額，否則不可核准）。核准後仍可在「設定撥款明細」區塊透過獨立 endpoint 修改未撥列 / 填實際撥款日。
  - **「財務撥款步驟」判定**：看**該簽核步驟綁定的部門 Code** 是否屬財務管理部（後端 `DepartmentCodes.FinanceStep` = `{FIN, Financial Management Department}`，即舊短碼 + 改制後英文全名；Superadmin 視同）。刻意**只含財務管理部、不含 CEO / 總監 / HQ / 會計**，避免上層核准步驟被誤判為撥款填寫節點而擋住簽核。同一判定用於 `IsFinanceStepAsync`（後端撥款明細必填）與前端 `FINANCE_STEP_DEPT_CODES`（`canSetPaymentDate` 顯示撥款表單 / `canCloseAdvance` / `canCloseTravelRequest` 結案 checkbox），**前後端兩處須同步**。注意此「步驟判定」與「使用者撥款權限判定」`DepartmentCodes.FinancialAndAbove`（含 CEO/總監/HQ/會計）是**兩個不同集合**。
    - ⚠️ **一律用 `DepartmentCodes.FinanceStep` 集合比對，不得硬編碼 `== "FIN"`**（2026-08 踩過：沖銷結案的兩處判定寫死 `FIN`，組織改制後 Code 變 `Financial Management Department` 即失效。前端用的是含新碼的集合，故 checkbox 照常顯示、財務勾得下去，後端卻靜默略過不結案也不報錯，財務只好在總監審完後再按一次結案按鈕。現已改走 `IsFinanceStepAsync`，且勾選但判定不成立時改丟 400，不再靜默）。
  - 例外：`holiday_travel`（假日執行活動）不在 review 流程填撥款明細，僅走核准後的獨立 endpoint。
  - 批次核准不填撥款明細，最終 approved 後由「待補撥款」提醒（`BuildPendingPaymentReminderAsync`）追蹤。
- **獨立 endpoint**：`PATCH /{type}-requests/{id}/installments`（舊 `/payment-date` 已於 Phase 2 移除）；**僅 ApprovalStatus == approved 可呼叫**（4 種一致；review 路徑因在核准同交易內寫入故不經此守衛）
- **DTO**：`UpsertInstallmentsRequest { installments[], approvalStatus? }`，每筆 `{ id?, installmentNo, expectedDate, paidAt?, amount, note? }`
- **持久化核心共用**：`InstallmentUpsertService.Apply`（validate + diff，**不 SaveChanges**，交易邊界交呼叫端）— 獨立 endpoint 與 review 原子寫入共用同一份邏輯；5 種子表實作 `IInstallmentEntity` 介面以泛型化
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
  - `closed` = **已結案**（2026-07 新增第 4 個按鈕，非撥款態）：父表 `IsClosed = 1`。只有**預支（advance）與出差預支（travel）**有結案概念，`PaymentStatusClause` 以 `supportsClosed: true` 開啟；其餘所有類型（請款 / 出差請款 / 兩種沖銷 / 請假 / 加班 / 假日活動 / 預審）一律 `1=0` 短路
  - 簽核作業 → 已核准 Tab 的篩選按鈕對應：`全部` / `尚未撥款` (`unpaid`) / `部分撥款` (`partial`) / `全部撥款` (`paid`) / `已結案` (`closed`)
  - 出差沖銷（travel_write_off）退款仍以父表 `RefundedAt` 兩態判斷；遇 `paymentStatus=partial` 或 `closed` 時整批 `1=0` 短路。**預支沖銷（write_off）自 2026-07 起改走分期**，列表 filter 尚未接上分期三態（另案）
- **PDF 出納簽名章**：取 `installments[]` 中最後一期已撥款者的 `PaidBySignatureUrl` + `PaidAt`

### 預支沖銷差額分期撥款（2026-07 新增）

沖銷金額累計超過預支總額時，公司需補撥差額給員工。原本只有 `AdvanceRequest` 上單一組 `EstimatedRefundDate / RefundedAt`，**無法分次撥款**；改為第 5 種分期子表 `WriteOffInstallment`（FK → `WriteOffRecord`）。

**應撥總額 `RefundDue`**（[WriteOffRefundCalculator](../../Api/Common/WriteOffRefundCalculator.cs)，前端同一份公式在 `write-off-request.model.ts` 的 `calcRefundDue()`）：

```
RefundDue = max(0, 前次已沖銷 + 本次沖銷 − 預支總額)
          − max(0, 前次已沖銷            − 預支總額)
```

以「增額」而非「總超支」計算 —— 每張沖銷單各自算得出、彼此不重疊，加總即等於整張預支單的超支總額，**不必等結案**。

範例（預支 10,000）：

| | 本次沖銷 | 累計 | RefundDue |
|---|---|---|---|
| 第 1 次沖銷 | 12,000 | 12,000 | **2,000** |
| 第 2 次沖銷 | 3,000 | 15,000 | **3,000** |
| | | | 合計 5,000 = 總超支 |

**「前次已沖銷」的判定＝已核准且核准時間早於本單**（2026-08 修正，[`WriteOffRefundCalculator.PriorApprovedTotalAsync`](../../Api/Common/WriteOffRefundCalculator.cs)）：

- 本單尚未核准（財務核准當下計算）→ 前次＝**當下全部已核准**的其他沖銷單
- 本單已核准（核准後修改撥款明細 / 詳情頁顯示）→ 前次＝**`ReviewedAt` 早於本單**者；同時間以 Id 較小者為前序；舊資料 `ReviewedAt` 為 null 視為更早
- ⚠️ 原本以 **Id 較小** 判定前序，但沖銷單的**建立順序與核准順序未必一致**：較晚建立卻先核准的單會把同一段超支算成自己的增額，之後較早建立的單再算一次 → **同一筆超支重複撥款**
- EF 版（`WriteOffRequestHandler` / `ApprovalTaskHandler` 共用同一支）與 Dapper 版（`WriteOffRequestReadService.BaseSql` 的 `AdvanceWrittenOffTotal` 子查詢）**條件必須一致**，否則畫面顯示的差額與實際寫入的撥款金額會對不起來

**「已沖銷金額」一律只計已核准**：可沖銷預支單下拉（`GET /write-off-requests/available-advances`）原本算「非 rejected」（含草稿 / 簽核中），與詳情頁的「已核准」基準不同，同一張預支單在兩個畫面顯示不同餘額。現統一為**已核准**；草稿 / 簽核中 / 已退回的金額另以 `PendingWriteOffTotal` 帶出，表單顯示「另有 N 元沖銷中」提示，不計入待沖銷餘額。

**規則**：

- **財務核准當下必填**（`RefundDue > 0` 時）：走既有的 `PATCH /approval-tasks/write_off/{id}/review` 的 `installments` 欄位，與審核同交易原子寫入。`RefundDue = 0`（未超支）則不要求、UI 也不顯示撥款區塊
- **核准後修改**：`PATCH /write-off-requests/{id}/installments`（僅 approved、限財務體系 / Superadmin）
- 驗證、已撥款列保護、每期 `PaidAt` null→value 觸發「已撥款（第 N/M 期）」通知，皆與其他 4 種一致
- **出差預支沖銷（travel_write_off）不在此範圍**，仍維持單一預計撥款日

**簽核頁同步維護預支單撥款明細**：預支沖銷簽核頁另設「關聯預支單撥款明細」區塊，直接讀寫 `PATCH /advance-requests/{id}/installments`，與預支申請單完全同步（同一份資料，不是複本）。

**支票已支付註記**：支票由公司**直接付給廠商**，不是撥給員工的錢，因此不進撥款分期；改由沖銷明細的 `CheckPaid` 勾選註記，`PATCH /write-off-requests/{id}/check-payments`（**限財務管理部**（`DepartmentCodes.FinanceStep`，比對登入者自身部門）**/ Superadmin**，pending 或 approved 皆可，`CheckAmount = 0` 的明細不可勾）。簽核頁的「支票已支付」**整欄對所有審核者顯示**，但非財務管理部（或單子非 pending / approved）時 checkbox `disabled` 反白且帶 title 說明原因，只有財務管理部 / Superadmin 能實際勾選（後端同步以 403 擋下）。
> 2026-07 收窄：原本用 `DepartmentCodes.FinancialAndAbove`（含總監室 / 會計室 / Jabez HQ），造成財務管理部以外的人也能勾；現與撥款日 / 撥款明細 / 結案同用 `FinanceStep`。撥款明細的加總基準**維持含支票的整單金額**，未因此改變。

**舊資料 backfill**：migration `AddWriteOffInstallmentsAndCheckPaid` 把既有 `AdvanceRequest.RefundAmount > 0` 且有退款日的資料，寫成該預支單**最後一張已核准沖銷單**的第 1 期。

### 母單結案（預支 / 出差）

「結案」只存在於**母單**（`AdvanceRequest` / `TravelRequest` 的 `IsClosed` / `ClosedAt` / `ClosedById`），沖銷單自身沒有結案概念。結案後該母單不可再新增沖銷（`available-advances` 清單直接排除），亦不可再新增追加預支。

**兩個入口 = 同一個冪等動作，勾過就不必再按一次**：

| 入口 | 時機 | 授權比對對象 |
|---|---|---|
| 簽核時勾「預支結案 / 出差結案」checkbox（`review` 的 `CloseAdvance`） | 財務步驟核准當下**登記**，整張單核准才生效 | **步驟綁定部門**（`IsFinanceStepAsync` → `DepartmentCodes.FinanceStep`），Superadmin 視同 |
| 核准後按「預支結案」按鈕（`PATCH /approval-tasks/{write_off\|travel_write_off}/{id}/close`） | 沖銷單已 `approved` | **登入者自身部門**（`DepartmentCodes.FinancialAndAbove`，較廣） |

兩者最終都呼叫 `CloseAdvanceRequestAsync` / `CloseTravelRequestAsync`，開頭即 `if (... || IsClosed) return;` —— **重複呼叫是 no-op**，`ClosedAt` / `ClosedById` 不會被覆寫、超額匯款通知也不會重發。前端 `canCloseAfterApproval()`（按鈕）與 `canCloseAdvance()` / `canCloseTravelRequest()`（checkbox）皆帶「母單尚未結案」條件，已結案時改顯示唯讀提示，不會要求使用者按第二次。

#### 延後結案：`PendingClose` 登記制（2026-08 改）

財務多半**不是**流程最後一關（實務流程 `… → 會計室 → 財務管理部 → 總監室`）。原本財務勾選當下就寫 `IsClosed`，會造成總監尚未核准時預支單已關閉、無法補開沖銷單，且總監退回也不會還原。現改為兩段式：

1. **登記**：財務於其關卡勾選 → 只設沖銷單自身的 `WriteOffRecord.PendingClose` / `TravelWriteOffRecord.PendingClose = true`，母單**完全不動**
2. **生效**：任一次審核使該沖銷單轉 `ApprovalStatus == "approved"` 時，若 `PendingClose` 為 true 才真正結案。財務本身就是最後一關時，兩段在同一次呼叫內完成，行為與過去一致

- **退回 / 拒絕會清除登記**（`action is "rejected" or "returned"` → `PendingClose = false`）：申請人改金額重送後，需由財務依新金額重新判斷是否結案，不沿用舊決定
- **登記期間不鎖定**：`IsClosed` 仍為 0，該預支單照常出現在 `available-advances`、可再開沖銷單、可追加預支 —— 只有真結案才鎖
- **批次核准**：不會**設定** `CloseAdvance`，但若先前已登記，批次核准使其轉 `approved` 時同樣會生效（結案觸發點不看 `closeAdvance` 參數，只看 `PendingClose && approved`）
- **`ClosedById` 語意**：記的是**實際觸發結案那次審核的審核者**（延後後多半是最後一關的人，非當初登記的財務）。此欄純稽核用，未出現在任何 DTO / PDF / 前端
- **前端**：`pendingClose` 隨 task detail 回傳；已登記時 checkbox 隱藏並改顯示「財務已登記結案，待完成所有簽核關卡後自動生效」，後續關卡（如總監）也看得到

**結案當下的 `RefundAmount`**：只計 `ApprovalStatus == "approved"` 的沖銷單，**外加觸發本次結案的那一張**（`triggeringWriteOffId`）—— 結案發生在 `SaveChangesAsync` 之前，該張在 DB 仍是 `pending`。刻意不用 `!= "rejected"`，否則 `draft` / `returned` 的沖銷單會灌進差額，發出金額偏高的「[需匯款]」通知。基準與 `WriteOffRefundCalculator` 一致。

### 依預支單彙總檢視（2026-07 新增）

一張預支單可對應多張沖銷單，過去只能逐張沖銷單點進去看，對不出「這張預支單到底沖到哪」。沖銷清單本來就依 `AdvanceRequestId` 把沖銷單 group 在一起，母層列（同一預支單 ≥ 2 筆時出現的摘要列）操作欄現在多一個**檢視**按鈕，進入彙總頁 `/admin/write-off-requests/by-advance/:advanceId`（[write-off-overview](../../Admin/src/app/features/admin/write-off-requests/pages/write-off-overview/)）。

- **資料來源**：`GET /write-off-requests/by-advance/{advanceRequestId}`（`AdvanceWriteOffOverviewDto`）—— `advance` 直接沿用 `IAdvanceRequestReadService.GetByIdAsync`（含批次 `rounds` / 費用明細 / 撥款分期），`writeOffs[]` 由 `WriteOffRequestReadService.GetByAdvanceIdAsync` 一次撈回（明細 / 指定審核者 / 附件 / 差額撥款分期 / `refundDue` 皆為批次查詢，不逐單 N+1）
- **權限**：`write-off-requests:read`（不需 `advance-requests:read`——沖銷申請人本來就看得到自己沖的那張預支單；頁頭的「預支單詳情」連結才用 `advance-requests:read` 控管）
- **可見性**：Superadmin，或**預支單申請人**，或該預支單底下**任一沖銷單**的申請人 / 審核者 / 指定審核者；皆不符合回 404（與單筆 `GET /write-off-requests/{id}` 同一套判定，只是範圍擴大到整個群組）
- **頁面內容**：預支資訊（含各批次日期 / 金額）→ 金額摘要（預支總額 / 已沖銷加總 / 待沖銷餘額 / 應撥差額加總，**已拒絕的沖銷單不計入加總**）→ 沖銷單一覽表 → 預支費用明細 → 預支撥款明細 → 逐張沖銷單的完整卡片（明細 / 附件 / 該次差額撥款）

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

> 此端點仍限**財務體系部門**（部門 Code ∈ AC / FIN / Jabez HQ / CEO，或 2026 改制後英文全名碼 Accounting Department / Financial Management Department / Office of the Director；見 `DepartmentCodes.FinancialAndAbove`）或 **Superadmin** 操作。

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
- **沖銷結案不主動觸發，但會讓既有登記生效**：批次核准不會**設定** `CloseAdvance`；但若財務先前已勾選登記（`PendingClose = true`），批次核准使該沖銷單轉 `approved` 時仍會完成結案（觸發點只看 `PendingClose && approved`）。未登記者，結案仍須於詳情頁或獨立結案端點操作。

## 總監室簽核（2026-07 新增「總監待簽核」，2026-08 擴為四態）

簽核作業列表的「總監室簽核」頁籤，讓財務管理部與會計室掌握**所有與總監關卡有關**的申請 —— 頁籤內再以四個子狀態切換：**待簽核 / 已核准 / 退回修改中 / 已拒絕**。原名為「總監待簽核」、只有「只差總監一步」一種狀態，2026-08 擴充後可從同一入口追完整批單的後續結果。

- **範圍與狀態拆成兩個正交參數（2026-08）**：`GET /approval-tasks?scope=director&status={pending|approved|returned|rejected}`。
  - 舊值 `status=director_pending` 由 [ApprovalTaskHandler.GetAllAsync](../../Api/Handlers/ApprovalTaskHandler.cs) 相容為 `scope=director&status=pending`（舊書籤 / 舊前端不會壞）。
  - **為何不用複合字串**（`director_approved` …）：`status` 會同時承載「範圍」與「狀態」兩種語意，SQL 參數 `@StatusFilter` 拿去和 `ApprovalStatus` 等值比對就會靜默回空 —— 這正是 2026-08 之前 `status=returned` 靜默回傳待審清單的同一個根因。現行 `status` 一律走白名單 `ValidListStatuses` 正規化（非法值 → `pending`）。
- **可見範圍（2026-08 擴充會計室）**：財務管理部 + 會計室（`DepartmentCodes.DirectorPendingView` = 舊短碼 `FIN` / `AC` + 改制後英文全名 `Financial Management Department` / `Accounting Department`）或 Superadmin 可見此頁籤；其他部門呼叫 `GET /approval-tasks?scope=director` 一律回 403（四種子狀態共用同一組可見權限）。前端以 `approval-task-list.ts` 的 `canSeeDirectorTab`（`DIRECTOR_SCOPE_DEPT_CODES`）控制頁籤顯示，權限判定同時存在後端（防止繞過 UI 直接打 API）。
  - **檢視權與撥款寫入權刻意分離**：本集合只給「看得到頁籤」，不等於 `DepartmentCodes.FinanceStep`（撥款日 / 撥款明細 / 沖銷結案 / 支票已支付）。會計室看得到清單，但這些寫入型操作仍只有財務管理部 / Superadmin 可執行。
- **資料範圍依部門收斂（2026-08 新增）**：由 [ApprovalTaskHandler.GetAllAsync](../../Api/Handlers/ApprovalTaskHandler.cs) 算出 `directorStepDeptId` 並傳給 reader，**四種子狀態一律套用**。
  - 財務管理部 / Superadmin → `null`，看全部（維持原行為）。
  - 其他有檢視權的部門（會計室）→ 傳自身 `DepartmentId`，SQL 追加「該單流程中存在綁定該部門的步驟」，即**只看流程需要自己部門簽核的單**。沒有會計關卡的請假 / 銷假 / 加班 / 預審等他人單據因此不會出現（此清單本身不套部門可見性 scope，故必須靠這層收斂）。
    - **刻意不比對 `StepOrder`**：實務流程多為「… → 會計 → 財務 → 總監」，總監常是最後一關，會計關卡在其之前；若限定「總監之後的步驟」會讓會計室看到空清單。
    - **相容只綁職稱、未綁部門的會計關卡**：部分流程的會計步驟 `DepartmentId` 為 null、只綁 `JobTitleId`，故條件為 `DepartmentId = 該部門 OR (DepartmentId IS NULL AND JobTitleId = 呼叫者職稱)`。
- **匹配條件（四態）**：一律不受呼叫者本身職稱/部門限制（檢視者並非該步驟審核者，僅是檢視），再套上方的部門收斂條件。實作於 [PaymentRequestReadService.StepMatchClause](../../Api/Services/Dapper/PaymentRequestReadService.cs) 的 `directorScope` 分支（擋在所有分支最前面，Superadmin 也走同一條），涵蓋全部申請類型。

  | 子狀態 | `ApprovalStatus` | 總監關卡條件 |
  |---|---|---|
  | 待簽核 | `pending` | 流程中存在 `JobTitle.Level = 1` 的步驟 **且 `StepOrder = CurrentStepOrder`**（＝已輪到總監，原「總監待簽核」語意不變） |
  | 已核准 | `approved` | 流程中存在 `JobTitle.Level = 1` 的步驟（**不綁 `CurrentStepOrder`**） |
  | 退回修改中 | `returned` | 同上 |
  | 已拒絕 | `rejected` | 同上 |

  **為何後三態不綁 `CurrentStepOrder`**：單子已離開總監那一步 —— `approved` 已走完全部步驟、`returned` / `rejected` 停在退回（拒絕）者所在的步驟，`CurrentStepOrder` 不再指向總監，綁了就永遠查不到東西。
- **僅供檢視**：此頁籤內的申請單仍只能由總監本人（或 Superadmin）實際核准；財務管理部 / 會計室人員點擊進入詳情頁為唯讀（前端固定顯示查看圖示，不顯示可編輯的鉛筆圖示），送出審核動作仍會被 `AuthorizeStepAsync` 擋下。

## 簽核作業「退回修改中」頁籤（2026-08 新增）

簽核作業列表新增「退回修改中」頁籤，列出 `ApprovalStatus = 'returned'`（已退回、正在申請人手上待修改）的單。此前審核者按下「退回修改」後就再也看不到那張單，只有申請人在各自的申請清單看得到，無從追蹤對方改了沒。

- **可見範圍**：所有審核者皆有此頁籤（不限財務體系）。單筆是否列出，由 [PaymentRequestReadService.StepMatchClause](../../Api/Services/Dapper/PaymentRequestReadService.cs) 的 `returned` 分支以 **四選一** 判定：
  1. 我在這張單留過任何 `ApprovalRecord`（含我親自退回的、退回前已核准的）
  2. 我是這張單的指定審核者（`RequestDesignatedReviewers`）
  3. 我是這張單的升級審核者（`EscalationOverrides`，自審時被指派）
  4. 該單流程中存在**綁定我職稱**的固定關卡（`UseApplicantDepartment` 相符，或步驟未綁部門）
- **與「已核准」/「已拒絕」的差異**：那兩個頁籤只看第 1 條（我親自審過）。退回常發生在**還沒輪到我之前**，只比對 `ApprovalRecords` 會讓流程後段的審核者完全看不到，故多放行第 2~4 條。
- **刻意的簡化取捨**：
  - 第 4 條**不重用待審核分支的 `UseDirectSupervisor` 遞迴解析**。那段靠 `CurrentStepOrder` 定位「第幾層直屬主管」（`ROW_NUMBER` 視窗函數 + 相關子查詢），脫離當前步驟即無從成立，且成本隨流程步驟數線性放大。直屬主管的情形由第 1 條涵蓋（主管退回前必已留下紀錄）。
  - 第 4 條的 `JobTitleId` **不放行 `IS NULL`**（待審核分支放行，因為那裡另有 `CurrentStepOrder` 收斂）。否則「不限職稱」的關卡會讓全公司都看到這張單。
  - 第 2 條**不限 `Status = 'pending'`**（待審核分支有限）：退回時該筆 designee 會被設為 `'returned'`，限 pending 會漏掉「自己被指定、然後這張單被退回」的情形。
- **舊行為修正**：2026-08 之前 `StepMatchClause` 沒有 `returned` 分支，非 Superadmin 傳 `status=returned` 會 fall through 到待審核分支（寫死 `ApprovalStatus = 'pending'`），**靜默回傳完全不相干的待審清單**。現行 `status` 一律走 [ApprovalTaskHandler](../../Api/Handlers/ApprovalTaskHandler.cs) 的 `ValidListStatuses` 白名單正規化。
- **列表為唯讀**：退回單的 `status` 不是 `pending`，操作欄自動顯示查看圖示；要重新簽核須等申請人修改後重送。

## 簽核作業列表「申請人」篩選（2026-07 新增）

簽核作業列表「已核准」頁籤原本只有「全部類型」下拉，新增「全部申請人」下拉，供財務清查特定同仁的已核准單據（可與類型、撥款 / 退款子篩選任意組合）。

**2026-08 起篩選列改為各頁籤常駐**：類型 + 申請人下拉在 待審核 / 已核准 / 退回修改中 / 已拒絕 / 總監室簽核（四態）皆可用；**撥款 / 退款子篩選仍只在「已核准」頁籤顯示**（其他狀態的單尚未進入撥款階段，篩了沒有意義）。後端零改動 —— `applicationType` / `submittedByUserId` 的 WHERE 本來就與 `status` 正交，各狀態分支共用同一組 `SubmitterClause` / `TypeAllowed`。

- **可見範圍**：僅**財務體系部門**（`DepartmentCodes.FinancialAndAbove` = `CEO` / `FIN` / `AC` / `Jabez HQ` + 改制後英文全名總監室 / 財務管理部 / 會計室）或 Superadmin 可見，與撥款 / 退款子篩選同一集合。前端以 `approval-task-list.ts` 的 `canSeeApplicantFilter` 控制顯示，後端 [ApprovalTaskHandler.CanFilterByApplicant](../../Api/Handlers/ApprovalTaskHandler.cs) 為同一判定的真相。
- **選項來源**：`GET /approval-tasks/applicants` —— 10 種申請單中曾送出（`ApprovalStatus <> 'draft'`）者的申請人去重清單，依姓名排序，排除 Superadmin。非財務體系呼叫回 403。
- **篩選行為**：`GET /approval-tasks?submittedByUserId={guid}`，於 [PaymentRequestReadService](../../Api/Services/Dapper/PaymentRequestReadService.cs) 各申請類型 SQL 直接加 WHERE（不是撈完再丟），涵蓋全部類型。**申請人欄位不一致**：請款 / 預支 / 沖銷 / 出差沖銷 / 預審用 `SubmittedById`，請假 / 出差 / 假日執行活動 / 加班 / 出差請款用 `EmployeeId`。
- **非財務體系帶此參數一律靜默忽略**（不回 403）；按單一 ID 查詢詳情時不套用。
- 篩選**不放寬可見範圍**：仍疊在原本的審核者可見性條件之上，只會縮小結果。

## 依請假天數決定簽核關卡（MinDays 門檻，2026-07 新增）

`ApprovalStep` 新增 `MinDays`（nullable int）欄位，讓**簽核步驟可依申請天數動態納入 / 略過**：

- `MinDays == null` → **一律納入**（既有步驟與其他 8 種申請完全不受影響）。
- `MinDays == N` → **僅當申請天數 ≥ N 才納入**此步驟，否則視為不存在（乾淨略過、不寫代簽）。
- **天數來源**：目前僅**請假**傳入 `requestDays = Hours / 8`（已扣假日後的工作日）；其餘申請類型不傳（`requestDays=null`），MinDays 無作用。
- **實作單一真相＝引擎兩處**（[ApprovalFlowService](../../Api/Services/ApprovalFlowService.cs)）：
  - `ResolveStartingStepAsync`（送出解析起始步驟）與 `SkipUnreviewableStepsAsync`（核准後推進下一步）各接受 `decimal? requestDays`，`requestDays` 非 null 時先過濾 `MinDays > requestDays` 的步驟。
  - `ResolveStartingStepAsync` 以 `currentStep++` 逐步略過（維持位置計數與 StepOrder 對齊）；`SkipUnreviewableStepsAsync` 為 StepOrder-based，直接整批 `FilterStepsByMinDays` 移除。
  - 呼叫端：[LeaveRequestHandler.SubmitAsync](../../Api/Handlers/LeaveRequestHandler.cs) 傳 `item.Hours/8m`；[ApprovalTaskHandler.ProcessReviewAsync](../../Api/Handlers/ApprovalTaskHandler.cs) 對 leave 傳 `leaveRequest.Hours/8m`。
- **Dapper 待審 / 簽核清單無需改**：跳過的步驟已由 `CurrentStepOrder` 推進帶過，`StepMatchClause` 只比對 `CurrentStepOrder`。
- **邊界**：`MinDays=3` 代表「天數 ≥ 3 才納入」；剛好 3 天走完整鏈，符合「三天以上要總監」。
- **設定頁**（[approval-flow](../../Admin/src/app/features/admin/approvals/pages/approval-flow/)）每步新增「適用天數門檻」數字輸入（留空＝一律適用），步驟列以「≥ N 天適用」badge 標示。
- **典型請假配置**：Step1 單位主管（`UseApplicantDepartment`，門檻空）→ Step2 部門最高主管（`UseDirectSupervisor`，門檻 3）→ Step3 總監（固定 `JobTitle.Level=1`，門檻 3）。`UseDirectSupervisor` 解析的是「申請人上一層」，若組織需「部門絕對最高主管」可改為固定職稱。
- **已知簡化**：簽核詳情頁時間軸目前顯示流程定義的全部步驟；< 3 天的單雖只走 Step1 即核准，時間軸仍列出 Step2/3（設定頁以 badge 標示「≥N天適用」），未做 per-request 隱藏。

## 自審跳過規則（僅限請款）

當申請人本身符合某步驟的審核者條件時（例如部門主管送出自己部門的請款），該步驟**自動跳過**（視為已通過），不觸發升級機制。若所有步驟都被跳過，申請**自動核准**。

此行為與加班/請假/出差不同 — 後者會觸發升級機制往上層部門找主管審核（詳見 [approval-escalation.md](approval-escalation.md)）。

## 送單防呆：固定關卡查無審核者即擋下（2026-09 新增）

**問題：** 綁部門／職稱的「固定審核者池」關卡與上層級關卡不同 —— 引擎**不會**因為查無人員而跳過，而是照樣停在該關。但沒有人通得過 `AuthorizeStepAsync` 的部門／職稱比對，也沒有人會收到通知，結果是**單子送得出去卻卡死在半路**，只能靠 Superadmin 介入。

**改法：** [ApprovalFlowService.ValidateFixedStepsHaveReviewersAsync](../../Api/Services/ApprovalFlowService.cs) 在 `ResolveStartingStepAsync` 主迴圈前先掃過**全部**步驟（不只走到第一個停留關），任一固定池關卡查無可審人員即丟 400 擋下送出，訊息帶出關卡序號 + 部門 + 職稱：

> 簽核流程第 2 關找不到可審核的人員（發展三部 / 協理），無法送出申請。請聯絡管理員調整簽核流程設定或該部門人員職稱。

**可審人員的判定：** `Status='active'` + 非 superadmin + 非申請人本人，再依關卡條件套部門／職稱過濾。`UseApplicantDepartment` 但申請人未設部門 → 一律視為無人（該關的部門條件永遠對不上）。

**以下關卡刻意不檢查**（各自另有合法的「無人可審」出路）：

| 關卡 | 不檢查的原因 |
|------|--------------|
| 被 `MinDays` 門檻擋掉 | 這張單根本不走這關 |
| 指定審核步驟 | 由 `DesignatedReviewerHelper.ValidateAndNormalizeAsync` 負責 |
| `UseDirectSupervisor` | 找不到人時往上層部門升級，再找不到則設計上允許跳過 |
| 申請人即審核者（自審） | 另有跳過（請款類）／升級（請假・出差・加班）機制 |
| 請款類的 `UseApplicantDepartment` | 現行明確設計為「該部門無人則跳過」，由 `SkipEmptyApplicantDeptTypes` 常數界定 |

**上線前必須先修設定：** 此防呆會讓既有的流程設定問題從「送出後卡死」變成「送不出去」。以請假通用預設流程（ApprovalItem 4）Step2「申請人部門的協理」（`MinDays=3`）為例，**沒有協理的部門**其成員一旦請 ≥3 天就會被擋下。修法擇一：該部門補上協理職稱人員、把該關改為 `UseDirectSupervisor`、或放寬為不限職稱。

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
- **同部門找不到更高層級的人 → 沿部門 `ParentId` 往上層部門找（2026-09 新增，見下）**
- 連上層部門都找不到 → 該步驟自動跳過（視為通過）
- 所有步驟都跳過 → 自動核准
- 啟用時自動忽略 `DepartmentId` 和 `JobTitleId`（隱含使用申請人部門）

### 同部門無上級時往上層部門接手（2026-09 新增，全部 9 種申請類型適用）

**問題：** 原本「同部門找不到更高階者 → 跳過該關」是**乾淨跳過**（不寫 `ApprovalRecord`、不寫代簽人），事後從簽核歷程完全看不出這關曾存在。最常踩到的是**部門最高主管本人送單**：他上面同部門沒有人，若流程各關皆為上層級模式，會一路跳到底 → `autoApproved = true`，**送出當下直接變成已核准、沒有任何人審過**。

**改法：** 同部門解析不到上級時，先呼叫 [EscalationService.FindSuperiorInAncestorDepartmentsAsync](../../Api/Services/EscalationService.cs) 沿部門 `ParentId` 逐層往上找；找到即**停在該關**並以升級審核指派該員（寫 `EscalationOverride`），找不到才退回原本的跳過。

| 情境 | 改動前 | 改動後 |
|------|--------|--------|
| 同部門有上級 | 停在該關由上級審 | 不變 |
| 同部門無上級、上層部門有更高階者 | 乾淨跳過 | **停在該關，由上層部門該員審（升級審核）** |
| 同部門與所有上層部門皆無更高階者 | 乾淨跳過 | 不變（維持跳過） |

**設計取捨：**
- **找不到時回退跳過、不丟例外** —— 與自審升級（`TryEscalateAsync` 找不到就 400 擋下送出）刻意不同。此處行為只增不減，確保原本送得出的單不會因這次改動而送不出去。
- **不套用請假／加班的「停在總監前」規則** —— 上層級關卡的語意就是往上找；排除總監會讓部門最高主管仍然無人可審，等同此機制失效。
- **同部門內取最接近申請人職級的一位**（`Level` 由大到小），避免一步跳到最頂層。
- **排除總監歷史已審者**（`supervisorIds`），避免把已經審過的總監再找回來重審；與 `SkipUnreviewableStepsAsync` 的去重規則同源。非總監的重複指派則允許，與「非總監允許重審」的既有規則一致。

**兩個觸發點（送單時 + 簽核推進時都要有，否則中段的上層級關卡仍會被跳掉）：**

| 觸發點 | 位置 | 產出 |
|--------|------|------|
| 送出申請 | [ApprovalFlowService.ResolveStartingStepAsync](../../Api/Services/ApprovalFlowService.cs) | 回傳 `EscalationResult`，由 9 個 SubmitAsync Handler 寫 `EscalationOverride` + `NotifySpecificReviewerAsync` |
| 簽核推進 | [ApprovalFlowService.SkipUnreviewableStepsAsync](../../Api/Services/ApprovalFlowService.cs) | 回傳值新增第 4 項 `escalation`，由 [ApprovalTaskHandler.ProcessReviewAsync](../../Api/Handlers/ApprovalTaskHandler.cs) 寫 `EscalationOverride` + 通知該員 |

**連續兩個上層級關卡都升級到同一人 → 沿用既有的「相鄰 step 同人」去重：**
部門最高主管送單時，Step1（rank 0）與 Step2（rank 1）在同部門都解析不到人，會升級到**同一位**上層部門主管。
若不處理，該員得為同一張單連審兩次。`ResolveReviewerPoolAsync` 對「已升級的上層級步驟」回的是**空池**，
空池不進第二階段去重判定，因此在 [SkipUnreviewableStepsAsync](../../Api/Services/ApprovalFlowService.cs) 內改為
**升級接手時直接以「被指派的那一位」當池**，讓既有的 (A) 總監 / (B) 相鄰 step 規則正常生效 —— 相鄰時自動跳過 + 寫代簽
（`自動核准：已於先前步驟核准本申請`），不再重複打擾同一人。

**授權與可見性：**
- [AuthorizeStepAsync](../../Api/Handlers/ApprovalTaskHandler.cs) 的 `UseDirectSupervisor` 分支**必須先檢查 `EscalationOverride` 才做層級比對** —— 升級進來的人不在申請人部門，會先被同部門比對擋成 403。共用 helper `HasEscalationOverrideAsync`（與一般步驟的 override 檢查同一支）。
- 待審清單無需改：`StepMatchClause` 已有 `EscalationOverrides` 分支（綁 `StepOrder = CurrentStepOrder`）。
- 前端無需改：`ApprovalRecord.IsEscalated` 由既有邏輯設定，時間軸沿用「升級審核」紫色 badge。

**可與現有模式混用：** 每個 ApprovalStep 獨立判斷，例如 Step 1 用 `UseDirectSupervisor=true`，Step 2 也用 `UseDirectSupervisor=true`（自動往上一層），Step 3 維持固定部門 + 職稱。

**涉及元件：**
| 元件 | 說明 |
|------|------|
| `ApprovalStep.UseDirectSupervisor` | Entity 欄位 |
| `ApprovalFlowService.FindNthSuperiorLevelAsync()` | 找同部門第 N 層上級 |
| `EscalationService.FindSuperiorInAncestorDepartmentsAsync()` | 同部門無上級時沿 `ParentId` 往上層部門找更高階者（找不到回 null＝維持跳過） |
| `ApprovalTaskHandler.AuthorizeStepAsync()` | 驗證審核者是否為正確層級的上級（**先檢查 `EscalationOverride`**） |
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

**多個指定步驟的前端連動（共用元件 [DesignatedReviewersPicker](../../Admin/src/app/shared/components/designated-reviewers-picker/designated-reviewers-picker.ts)，9 種申請表單統一使用）：**
- **連動閘控**：第一個指定步驟未選好審核者前，其後所有指定步驟的下拉 / 新增鈕 disabled。
- **部門帶入**（僅 `DesignatedRequiresDepartment=true`）：第一個指定步驟選的部門，自動帶入其後指定步驟的部門下拉（申請人手動改過的列不覆寫）。
- **部門最高層級自動略過（req3，2026-07 限定僅 2 個部門適用）**：第一個指定步驟（先選部門模式）選的部門若屬於 `DESIGNATED_TOP_LEVEL_SUPPRESSION_DEPT_CODES`（**Operations Department**（營運管理及發展部）／ **Brand Department(疆界地域美學)**（品牌事業部），比對部門 `Code`），且選到「所選部門中 `JobTitle.Level` 最小（最高職稱）」的人，其後所有指定步驟前端 disable + 不送出、後端亦自動略過；其餘部門一律不抑制，申請人仍須逐一指定每個步驟。部門最高層級判定所需的 `JobTitleLevel` 由輕量端點 `GET /users/lookup` 附帶回傳，部門 `Code` 取自 `GET /departments`。

**部門最高層級抑制（後端權威判定，單一真相）：**
- [DesignatedReviewerHelper.GetSuppressedDesignatedStepOrdersAsync](../../Api/Common/DesignatedReviewerHelper.cs)：若第一個指定步驟為 `DesignatedRequiresDepartment=true`，且其 `SelectedDepartmentId` 對應部門的 `Code` 屬於 `DepartmentCodes.DesignatedTopLevelSuppression`（Operations Department / Brand Department(疆界地域美學)），且其首位 designee（min `StepOrder`）＝該部門中 active、非 superadmin、有職稱者的最高職稱（min `Level`）本人 → 回傳「其後所有指定步驟 StepOrder」為被抑制集合。
- `ValidateAndNormalizeAsync` 對被抑制步驟**不再要求**指定審核者；`ResolveStartingStepAsync` / `SkipUnreviewableStepsAsync` 對被抑制步驟走「乾淨跳過（不寫代簽 ApprovalRecord）」。
- 防誤抑制守門：第一步非部門模式 / 首位沒選人 / 無 `SelectedDepartmentId` / 部門不在限定清單內 / 被指定者不在該部門 / 部門無合格人員 → 皆不抑制。
- **前後端須同步**：後端 `DepartmentCodes.DesignatedTopLevelSuppression`（[Constants.cs](../../Api/Common/Constants.cs)）與前端 `DESIGNATED_TOP_LEVEL_SUPPRESSION_DEPT_CODES`（designated-reviewers-picker.ts），兩處部門 Code 清單須一致。

**規則：**
- 送出（submit）時，如果流程中有 `UseApplicantDesignated` 步驟，`designatedReviewers` 清單必填且至少 1 人。守門落在三層：
  - **前端 fail-fast**：9 種申請表單（皆用共用 `DesignatedReviewersPicker`）的 `submitForApproval()` 在 `form.invalid` 檢查後，逐一驗證每個指定步驟 `designatedSteps` 是否有對應審核者（`_pickerPayload.some(p => p.approvalStepOrder === step.stepOrder)`），**被抑制步驟（`_suppressedSteps`）除外**，缺漏即顯示錯誤不送 HTTP request
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

---

## 例外指定審核（ApprovalStepException，2026-07 新增）

**需求背景：** `UseApplicantDesignated` 是**全流程一刀切**——一個步驟要嘛所有申請人都自行指定審核者，要嘛所有人都走部門／職稱。但實務上有少數人（跨部門支援、特殊職務、專案人員）需要在某個原本固定的步驟自行指定審核者，過去只能為他們另開一條部門專屬流程，成本過高。

**設定方式：** 簽核流程設定頁的步驟表單中，**當該步驟不是「申請人指定審核」時**，可勾選「例外指定審核」並**逐一挑選使用者**。名單存於子表 `ApprovalStepExceptions`（`ApprovalStepId` + `UserId`，unique index）。

| 規則 | 說明 |
|------|------|
| 名單命中的**申請人** | 該步驟對此人改為「由申請人自行指定審核者」，指定審核者**必填**，其餘規則（picker / 驗證 / 授權 / 通知 / PDF）完全沿用 |
| 不在名單內的申請人 | 該步驟照原設定走（固定部門+職稱 / `UseApplicantDepartment` / `UseDirectSupervisor`） |
| 與 `UseApplicantDesignated` | **互斥**。同時設定回 400；把步驟切成 `UseApplicantDesignated=true` 時後端自動清空例外名單 |
| 與 `DesignatedRequiresDepartment` | 沿用同一欄位（不新增欄位）：`UseApplicantDesignated` **或**有例外名單時此旗標才有意義，例外步驟同樣可開「需先選部門再選人」 |
| 與 `MinDays` | 正交且相容（MinDays 先過濾步驟，之後才判定是否指定審核） |
| 「部門最高層級自動略過」 | 因判定改吃「對此申請人的有效指定步驟集合」，語意自然 per-applicant 正確；只因例外而擁有**單一**指定步驟時（`Count < 2`）不抑制 |
| API | 搭載於既有 `POST/PATCH /approval-items/{id}/steps[/{stepId}]` 的 `exceptionUserIds: Guid[]`（**整批替換**：`null`＝不動、`[]`＝清空）。權限仍為 `approvals:write`，**不需新路由 / 新權限** |
| 是否啟用 | **不設 bool 旗標**，一律以 `exceptionUserIds` 是否非空為準（避免 bool 與陣列 desync）；前端 checkbox 僅為 UI 狀態 |

### 兩個單一真相 —— 以時間軸切分（重要）

`UseApplicantDesignated` 散落在 10 處消費（含 10 種申請共用的待審清單 SQL、PDF 簽名欄佈局）。若每處都改成「查例外表算對此申請人的有效值」，`PaymentRequestReadService.flowSql`（跨申請共用的全域 flow 物件，無申請人維度）將無解，且已送出的申請會因管理者事後改名單而當場失效。故採**混合方案**：

| 時間點 | 真相來源 | 消費點 |
|--------|----------|--------|
| **送單前 / 送單當下** | `ApprovalStepExceptions` 表<br>[DesignatedReviewerHelper.GetEffectiveDesignatedStepOrdersAsync](../../Api/Common/DesignatedReviewerHelper.cs) | 僅 2 處：`GET /approval-items/active`、`ValidateAndNormalizeAsync` |
| **送單完成後** | `RequestDesignatedReviewers` 快照（designee 列本身即「申請當下例外命中」的證據）<br>[DesignatedReviewerHelper.EffectiveDesignatedStepOrders](../../Api/Common/DesignatedReviewerHelper.cs) | 其餘全部：`ResolveStartingStepAsync` / `SkipUnreviewableStepsAsync` / `ResolveReviewerPoolAsync` / `AuthorizeStepAsync` / `ProcessReviewAsync` / `NotifyReviewersAsync` / `StepMatchClause` / 9 個 handler 送單通知分支 / PDF |

**好處：** `ApprovalFlowService` 三個方法與 `AuthorizeStepAsync` 簽章完全不動；PDF 後端不用碰；**在飛行中的申請對設定變更天然免疫**（與「`ApprovalItemId` 首次送出後不重挑」的既有哲學一致）。

**代價（必做守門）：** `ValidateAndNormalizeAsync` 送單時會**靜默剔除**綁在「非有效指定步驟」上的 designee，否則惡意 client 可送 `approvalStepOrder=N`（N 其實是固定部門步驟）把該步驟劫持成自己挑的人審。採靜默剔除而非丟 400，因草稿期間申請人可能調部門而換到別條流程，丟錯會誤傷正常使用者。

### 待審清單 SQL 的三處改動（[PaymentRequestReadService.StepMatchClause](../../Api/Services/Dapper/PaymentRequestReadService.cs)）

10 種申請類型共用同一 clause，**錯一次全錯**：
- `s2`（一般部門/職稱分支）、`s3`（上層級分支）各加 `AND NOT EXISTS (RequestDesignatedReviewers WHERE ApprovalStepOrder = CurrentStepOrder)`
- `s4`（指定分支）**刪掉** `AND s4.UseApplicantDesignated = 1`（`rdr.ApprovalStepOrder = CurrentStepOrder` 本就在條件內）

> ⚠️ `NOT EXISTS` 的 `ApprovalStepOrder = CurrentStepOrder` **絕不可省**：省略的話「step 1 原生指定 + step 2~4 固定部門」的申請推進到 step 2 後，會從所有一般審核者的待審清單消失。

### PDF 簽名欄

**例外命中的步驟照常佔一格簽名欄**（2026-08 修正）。

2026-07 導入例外指定審核時，把「指定簽核步驟不獨立佔簽名欄」的規則改由 `designatedStepOrders`（取自 `designatedReviewers[].approvalStepOrder`）判定，**一併涵蓋例外命中的步驟** —— 這是錯的：

| | 原生 `UseApplicantDesignated` 步驟 | 例外指定審核命中的步驟 |
|---|---|---|
| 步驟角色 | 無固定角色（誰簽全由申請人挑） | **固定**（上層級 / 會計 / 財務…） |
| 是否寫 `ApprovalRecord` | 會（但無固定欄位可掛） | **會**，`StepOrder` 就是該步驟 |
| PDF 簽名欄 | 不佔欄（僅總監有特例，見 [pdf-signatures.md](pdf-signatures.md)） | **佔一格**，標籤沿用 `resolveStepLabel` |

實務上各申請類型的 Step 1「上層級」普遍掛有例外名單，導致 **8 種 PDF 的「上層級」欄整格消失、該主管簽章遺失**。修正後 `buildDynamicSignBlocks` 拆成兩個判定：`isNativeDesignated`（= `step.useApplicantDesignated`，不佔欄、總監 hoist 特例只看它）與 `isExceptionDesignated`（= 非原生但 `designatedStepOrders` 命中，佔欄）。

`designatedStepOrders` 的職責因此縮小為「辨識例外命中步驟以挑紀錄」：該步驟可有多位 designee → 同 `stepOrder` 多筆紀錄，取最後一筆 `approved`。

為此 `drSql` 補上 `rdr.ApprovalStepOrder` 欄位（原先 approval-task 路徑回傳一律為 0），前端 [pdf-core.service.ts](../../Admin/src/app/shared/services/pdf-core.service.ts) 的 `designatedStepOrdersOf()` 與 `designatedStepOrders` 選項由 8 個 PDF service 各傳一行（呼叫端不受本次修正影響）。

### 限定職稱（ApprovalStepDesignatedJobTitle，2026-07 新增）

**需求背景：** 例外步驟的人員下拉原本是「該職稱全部人」或「該部門全部人」，申請人可以挑到任何人。實務上例外步驟通常只該找特定層級（例如協理），過去只能靠表單提示文字人工約束。

**設定方式：** 簽核流程設定頁的步驟表單，於「例外指定審核」名單下方新增「限定職稱」（**可多選**，FormArray 逐列 select，與例外名單同款 UI）。名單存於子表 `ApprovalStepDesignatedJobTitles`（`ApprovalStepId` + `JobTitleId`，unique index）。

| 規則 | 說明 |
|------|------|
| 適用範圍 | **只服務例外指定審核步驟**。原生 `UseApplicantDesignated=true` 步驟維持不限職稱（互斥由 handler 守門：切成原生指定或例外名單清空時，限定職稱一律自動清空；沒有例外名單卻設限定職稱回 400） |
| 是否啟用 | **不設 bool 旗標**，一律以 `designatedJobTitleIds` 是否非空為準（同例外名單的哲學） |
| 前端下拉 | `designatedRequiresDepartment=true` → 部門下拉不變，人員＝**該部門 ∩ 限定職稱**；`=false` → **隱藏職稱下拉**（已限定，該下拉無意義），人員直接列全公司符合職稱者。皆濾 `status==='active'`。查無符合者顯示「查無符合限定職稱的人員」 |
| 後端驗證 | `ValidateAndNormalizeAsync` 於送單時檢查：designee 綁定的步驟若「此申請人例外命中且有設限定職稱」，其 `User.JobTitleId` 須在名單內，否則 **400「步驟 N 的指定審核者職稱不符限定職稱，請重新選擇。」** |
| API | 搭載於既有 `POST/PATCH /approval-items/{id}/steps[/{stepId}]` 的 `designatedJobTitleIds: int[]`（**整批替換**：`null`＝不動、`[]`＝清空）。權限仍為 `approvals:write`，**不需新路由 / 新權限** |
| per-caller 有效值 | `GET /approval-items/active` 只對**命中例外的呼叫者**帶出限定職稱，其餘一律空陣列（與 `useApplicantDesignated` 同一套 per-caller 語意，不外洩設定） |

**為何丟 400 而非靜默剔除**（與上方剔除非法綁定的處理刻意不同）：那是「此步驟對我已非指定步驟」的殘留，使用者無從得知也無從修正；職稱不符則是使用者挑錯人、可自行修正，且靜默剔除會退化成「請提供指定審核者」——填了人卻被說沒填，除錯成本極高。

**交互作用（重要）**
- **與「部門最高層級自動略過」正交**：`GetSuppressedDesignatedStepOrdersAsync` 仍以該部門**全部** active 使用者算 `MIN(JobTitle.Level)`，**刻意不把限定職稱套進判定池**（那是另一條規則的基準）。副作用：若例外步驟限定「協理」而該部門最高是「總監」，申請人永遠選不到最高層級 → 抑制不會觸發、後續指定步驟仍需逐一指定。
- **草稿**：功能上線前存的草稿若含不符職稱的 designee，送單當下才會被擋（規則本就以送單當下為準）。
- **在飛行中的申請免疫**：送單後一律看 designee 快照，故簽核引擎 / 待審清單 SQL / PDF / timeline **皆不需改動**。
- **刪除職稱**：`ApprovalStepDesignatedJobTitles.JobTitleId` 為 `NO_ACTION` 外鍵（避免與 `ApprovalStep.JobTitleId` 的 SetNull 形成多重級聯路徑），已納入 [JobTitleHandler.DeleteAsync](../../Api/Handlers/JobTitleHandler.cs) 清洗；清空後該步驟退回「不限職稱」（**權限放寬**），與 `ApprovalStep.JobTitleId` SetNull 的既有行為一致。
- **設成無人持有的職稱**會讓申請人卡死無法送單，故前端在人員下拉為空時顯示提示。

### 注意事項

- **例外綁在特定流程的步驟上**：若申請人部門解析到另一條流程（部門專屬 vs 通用預設），該例外不生效。管理者必須設在申請人實際會走的那條流程上。
- **刪除使用者**：`ApprovalStepExceptions.UserId` 為 `NO_ACTION` 外鍵，已納入 [UserHandler.DeleteAsync](../../Api/Handlers/UserHandler.cs) 的清洗清單（與 `RequestDesignatedReviewers` 同一區塊）。
- **順修既有 bug**：[ApprovalNotificationService](../../Api/Services/ApprovalNotificationService.cs) 的 designee 查詢原先漏了 `ApprovalStepOrder == targetStepOrder`，多指定步驟時會通知到前一步殘留的 pending designee；例外功能使多指定步驟成為常態，已一併補上。

---

**存取控制（`GET /approval-tasks/{type}/{id}`）：**
- Superadmin：可查看所有
- 有 `approval-tasks:read` 權限：可查看所有
- 被指定為審核者（任何狀態）：可查看此申請單
- 曾審核過（有 ApprovalRecord）：可查看此申請單
- **申請人本人：可查看自己送出的申請單**（2026-08 新增，見下）
- 其他人：403

**申請人本人放行（2026-08 新增）：**

各申請的**詳情頁（detail）**都會另打此端點取簽核歷程，PDF 的**簽名章、簽核日期、動態簽名欄**（`buildDynamicSignBlocks` 以 `flow.steps` 產生欄位）也全部取自這裡。申請人本人若被 403，會出現兩種壞掉的表現：

| 申請類型 | 403 時的症狀 |
|---|---|
| 請款 / 預審 | 列印按鈕條件含 `approvalTask()`，按鈕**整個不出現** → 印不出來 |
| 預支沖銷 / 出差預支沖銷 | 按鈕無條件顯示，但 `flow` 為 undefined → 印出**一格簽核欄都沒有**的沖銷表（連申請者簽名章也空） |

故 [ApprovalTaskHandler.IsApplicantAsync](../../Api/Handlers/ApprovalTaskHandler.cs) 逐型別比對申請人欄位放行本人；申請人欄位各表不同：

- `SubmittedById`：`payment_request` / `advance` / `write_off` / `travel_write_off` / `pre_review`
- `EmployeeId`：`leave` / `leave_revocation` / `travel` / `holiday_travel` / `overtime` / `travel_payment`

> 新增申請類型時，`ValidAppTypes` 與 `IsApplicantAsync` 的 switch **必須同步加一條**，否則該類型的申請人會退回 403（PDF 缺簽名欄）。

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

## 追加預支重跑簽核（2026-07 新增）

**僅預支申請（advance）適用。** 已核准的預支單可再新增「追加批次」，追加明細掛在**同一張單**上（不是獨立子單），送出後整張單重跑同一份 advance 簽核流程。

### 資料模型

- `AdvanceRequestItem.RoundNo`：所屬批次（1 = 原始預支，≥2 = 第 N 次追加）
- `AdvanceRequest.CurrentRoundNo`：最新已建立的批次號。**「有進行中的追加」＝ `CurrentRoundNo > 1 && ApprovalStatus ∈ {pending, returned}`**
- `AdvanceRequestSupplement`：只存 `RoundNo ≥ 2` 的批次（日期 / 原因 / 回滾快照）。Round 1 即父單本身，零資料重複；各批次金額一律由該批次明細加總推導，不存金額欄位
- `ApprovalRecord.RoundNo`：簽核紀錄所屬批次（其餘 9 種申請恆為 1）

### 狀態流轉

```
approved ──(POST supplements：建立批次 + 併入總額 + 直接送簽)──→ pending
pending  ──核准──→ approved（CurrentRoundNo 維持在新批次）
pending  ──退回──→ returned ──(PATCH supplements/{n} 改明細 → PATCH submit)──→ pending
pending  ──拒絕──→ **回滾**：刪該批次明細/Blob/簽核紀錄 → 還原快照 → approved（CurrentRoundNo−1）
returned ──(DELETE supplements/{n} 主動放棄)──→ 同上回滾
```

「拒絕」不會讓整張已核准（甚至已撥款）的預支單變成 rejected —— 只有該追加批次被撤銷，父單的 `ApprovalStatus / CurrentStepOrder / ReviewedAt / ReviewedById / ReviewNote` 由 `AdvanceRequestSupplement.Prev*` 快照原樣還原。回滾實作見 [AdvanceSupplementService.RollbackAsync](../../Api/Services/AdvanceSupplementService.cs)（駁回時本次拒絕紀錄尚在 ChangeTracker，需另外 Detach 才不會留下孤兒）。

### 金額 / 撥款 / 沖銷連動

- **送簽當下即併入 `GrandTotal`**（不是核准時）。因為財務步驟核准要寫 `installments` 且 `SUM == GrandTotal`，而財務不一定是最後一關
- 追加核准後 `SUM(installments)` 須等於**新**總額：已撥款列鎖定不可改，財務**補一期**新增金額。原本 `FullyPaid` 的單追加後會變回 `PartiallyPaid`
- **追加簽核期間父單不是 `approved` → 該期間無法新增或送出沖銷**（`GET /write-off-requests/available-advances` 自動排除；`POST` / `PATCH` / `submit` 三處皆有守門，避免對變動中的總額沖銷）
- 追加核准後，沖銷「待沖銷」= 新總額 − 已沖銷
- **新增沖銷表單會唯讀列出所選預支單的全批次費用明細**（依「第1次 / 第N次追加」分組，各段標該批次預支日期），供申請人對照填寫實際花費。資料由 `GET /write-off-requests/available-advances` 的 `rounds` / `items` 一併帶回（不需 `advance-requests:read`）；因追加簽核期間該單已被排除，此處列出的批次必為已核准批次

### 守門

| 情境 | 行為 |
|---|---|
| 非 `approved` / 已結案 / 已有進行中追加 → 新增追加 | 400 |
| 有進行中追加 → 整單 `PATCH` / `DELETE` | 400「此預支申請有進行中的追加批次，請先處理追加批次。」**必要**：不擋的話申請人可在追加被退回時改掉甚至刪掉已撥款的原始明細 |
| 非 `returned` 或非最新批次 → 編輯 / 放棄追加批次 | 400 |
| 追加簽核期間 → 沖銷新增 / 編輯 / 送簽 | 400 |

### 簽核作業清單顯示

簽核作業清單（`approval-task-list`）的「摘要」欄，預支申請一律加註本次送簽批次，讓審核者不必進詳情頁就知道在審的是原始預支還是第幾次追加：

| 批次 | 摘要格式 |
|---|---|
| Round 1 | `活動名稱・第1次（總額 元）` |
| Round N ≥ 2 | `活動名稱・第N次追加（本次 元／總額 元）` |

- 批次標籤共用 `roundLabel()`（[advance-request.model.ts](../../Admin/src/app/features/admin/advance-requests/models/advance-request.model.ts)），與詳情 / 表單 / PDF 同一真相
- 「本次」金額取 `advanceDetail.rounds[]` 中對應 `roundNo` 的 `grandTotal`；「總額」為父單 `GrandTotal`（送簽當下已併入追加金額）
- 資料來源即列表既有 payload（`AdvanceTaskDetailDto.Rounds` / `CurrentRoundNo`），純前端顯示，無後端異動

### 通知

追加情境下 approved / returned / rejected 三種結果通知，申請類型名稱後會加註批次（`NotifyApplicantAsync` 的 `contextLabel` 參數）；拒絕另加「原預支單維持核准」，避免申請人誤以為整張單被否決。

---

## 銷假重跑請假簽核（2026-08 新增）

已核准的請假可提出**銷假申請**，逐日勾選要取消的日期，送出後**重跑一次原本的請假簽核流程**。業務規則（可銷條件 / 逐日部分銷假 / 下游影響）詳見 [leave-rules.md §銷假規則](leave-rules.md#銷假規則2026-08-新增)，此處只講簽核掛接。

### 與「追加預支」的關鍵差異：獨立子單，父單不動

| | 追加預支（advance） | 銷假（leave_revocation） |
|---|---|---|
| 資料模型 | 批次掛回父單（`AdvanceRequestSupplement` + `RoundNo`） | **獨立子單** `LeaveRevocation`（自帶 ApprovalStatus / CurrentStepOrder / ApprovalItemId） |
| 送簽期間父單狀態 | 轉 `pending`（沖銷等下游需另外守門） | **完全不動，維持 `approved`** —— 打卡阻擋 / 額度佔用 / 重疊驗證自動維持「仍在請假中」 |
| 被拒 / 放棄 | 需 `Prev*` 快照 + `RollbackAsync` 回滾父單 | **零回滾** —— 只改子單狀態 |
| 簽核紀錄隔離 | 同一 `ApplicationType="advance"`，靠 `RoundNo` 分批次 | 換 `ApplicationType`，天然隔離 |

### applicationType 的兩個用途要分開

| 用途 | 傳入值 | 原因 |
|---|---|---|
| `ApprovalFlowService.ResolveApprovalItemIdAsync`（挑流程設定） | **`"leave"`** | 直接複用請假的 ApprovalItem + Steps，管理端不需另設銷假流程。前端「簽核流程設定」的類型下拉刻意**不含**銷假（[approval-list.ts](../../Admin/src/app/features/admin/approvals/pages/approval-list/approval-list.ts)），設了也不會生效 |
| `ResolveStartingStepAsync` / `ApprovalRecord` / `RequestDesignatedReviewer` / `EscalationOverride` / 簽核任務 | **`"leave_revocation"`** | `ApprovalRecord` 是多型 `(ApplicationType, ApplicationId)`，兩者 Id 同為 int 序列會撞號；不隔離會讓「此人已在先前步驟核准過此申請」誤擋原假單的審核者 |

### requestDays 帶「原假單天數」

`MinDays` 天數門檻分流以 `(OriginalHours ?? Hours) / 8` 計算，讓銷假走到與原假單**完全相同**的那組關卡（5 天假銷 1 天，仍回到單位主管 + 部門最高主管 + 總監）。兩處必須同源：

- 送出：[`LeaveRevocationHandler.SubmitAsync`](../../Api/Handlers/LeaveRevocationHandler.cs)
- 每次審核推進：[`ApprovalTaskHandler`](../../Api/Handlers/ApprovalTaskHandler.cs) 的 `requestDays` switch（漏改會讓送出時被 MinDays 跳過的關卡在推進時又冒出來，卡死在無審核者的步驟）

### 沿用請假的其餘規則

- **自審**：Group A 全程禁止自審（`ApprovalFlowService` 的 Group B 否定清單不含銷假，自動落入 Group A）
- **升級審核**：自審時嘗試升級，且與請假一樣**停在總監之前**（`EscalationService` 的 `stopBeforeDirector`）
- **指定審核**：流程含「申請人指定審核」步驟時，銷假表單同樣要挑指定審核者（designee 以 `RequestType="leave_revocation"` 儲存）
- **退回重送**：清本單 `ApprovalRecords` / `EscalationOverrides`、重置 designee 為 pending，與請假 `SubmitAsync` 同一段邏輯

### 核准當下的副作用

`ApprovalTaskHandler` 的 `case "leave_revocation"` 在 `ProcessReviewAsync` 之後，若狀態轉 `approved` 才呼叫 [`LeaveRevocationService.ApplyAsync`](../../Api/Services/LeaveRevocationService.cs)（同交易），從「該假單所有已核准銷假單的 distinct 日期」整組重算父單 `Hours`，全銷則轉 `cancelled`，並通知職務代理人。

> **實作陷阱**：`ApplyAsync` 執行時本張銷假單的 `ApprovalStatus="approved"` 尚在 ChangeTracker、還沒進 DB，只查 DB 會漏掉自己 —— 故明確併入自己的日期（取聯集，重複套用仍收斂）。

---

## 跨步驟同人去重（相鄰 step 同人 OR 總監）

> **2026-05 規則限縮**：原本「全歷史」去重對所有審核者生效，過於激進；非總監若在跨多個 step 後再回到同一審核者，可能是流程設計需要分階段把關。規則改為只對「總監 (`JobTitle.Level == 1`)」或「相鄰 step 同人」自動跳過 + 代簽，其餘場景要求重新審核。
>
> **2026-09 相鄰分支放寬（全池 → 任一人）**：原本相鄰分支也要求審核者池被「已審者」**完全覆蓋**才跳過，
> 導致「同一角色有兩人以上」時永遠湊不齊全池。實例：品牌事業部 Step1「部門主管初核」（`UseDirectSupervisor`）
> 對專案經理送的單解析出來就是「部門協理」，與 Step2 固定的「品牌事業部 + 協理」是**同一池**，
> 而該部門有兩位協理 —— 董修慈簽完 Step1 後，因另一位協理沒審過而不跳過，同一人被要求連簽兩關。
> 固定部門+職稱 / 上層級的池語意本來就是「這個角色任一人可審」，同一人在相鄰前一關已行使過同一份權責，
> 故相鄰分支改為「池中**任一人**已審即跳過」。**指定審核步驟不套用放寬**（見下表）。

任一申請進行中時，後續任意 step 的解析審核者池與「該申請已 approved 的所有 ReviewedById」比對，是否自動跳過 + 代簽，依下表判定：

| 情境 | 行為 |
|---|---|
| 池中無任何已審者 | 通知未審者（仍排除已審總監） |
| 池中**任一人**已審 + 與「上一個有審核紀錄的 step」相鄰 + **非**指定審核步驟 | **跳過 + 寫代簽**（2026-09 放寬） |
| **指定審核步驟** + 相鄰 + 全部 designee 皆已審 | **跳過 + 寫代簽** |
| **指定審核步驟** + 相鄰 + 仍有 designee 未審 | **不跳過**，停在此 step（未審的 designee 仍須審） |
| 池被**完全覆蓋** + 代簽人 `JobTitle.Level == 1`（總監） | **跳過 + 寫代簽**（總監分支維持限縮，不放寬） |
| 有已審者但非總監 + 不相鄰 | **不跳過**，停在此 step（要求重審） |
| 同一 designated step 內 multi-designee 同人 | **維持原樣，自動代簽**（同 step 內延續，視為「比相鄰更緊」，不論角色） |

**指定審核步驟為何不放寬**：`UseApplicantDesignated`（含例外指定命中）的池是申請人**逐位點名**的人，
語意是「這些人都要審」，與 [ApprovalTaskHandler.cs](../../Api/Handlers/ApprovalTaskHandler.cs) `ProcessReviewAsync`
的 in-step `while` 迴圈（逐位推進、遇到未審者就停下）一致。外層若用「任一人」整關跳過，
會讓從沒審過的 designee 被靜默略過，與 in-step 行為矛盾。

**審核者池只含 active 帳號**：[ApprovalFlowService.ResolveReviewerPoolAsync](../../Api/Services/ApprovalFlowService.cs)
的三個分支與送單防呆 `HasStepReviewerAsync` 一致，皆篩 `Status == "active"`。離職 / 停用帳號若混進池中，
會讓仍走「全池皆已審」的總監分支永遠不成立。

「相鄰」精確定義：以 `ApprovalSteps` 依 `StepOrder` 排序後的索引為準，當前 step 索引 == 上一審核 step 索引 + 1（避免稀疏 StepOrder 數值差距誤判）。連鎖跳過時，每跳過一步即更新「上一審核 step」為剛跳過者，下個 step 仍可能算相鄰。

**統一自動代簽**：當某 step 因新規則跳過時，**一律寫一筆代簽 `ApprovalRecord`**（含 `Action='approved' / ReviewedById=代簽人 / ReviewNote='自動核准：已於先前步驟核准本申請'`），讓 PDF 簽名欄、簽核時間軸能正確顯示已審者的簽名。代簽人選擇邏輯：取「step 池 ∩ 已審者」交集後按 `ApprovalRecords.ReviewedAt` 升序取首位（最早審過此申請者）。

> **代簽人一定要從交集挑，不可退回池內第一位**：當次審核的 `ApprovalRecord` 還在 EF ChangeTracker、尚未 SaveChanges
> （由 `ApprovalTaskHandler` 手動補進 `approvedReviewerIds`），而 `PickEarliestProxyAsync` 以 `AsNoTracking` 讀 DB 看不到它。
> 相鄰分支放寬後池中常有「從沒審過的人」（如同部門另一位同職稱主管），若查無紀錄時退回 `pool[0]`，
> 代簽紀錄與 PDF 簽名章會掛到錯的人身上。

**指定審核步驟（`UseApplicantDesignated`）內部**：[ApprovalTaskHandler.cs](../../Api/Handlers/ApprovalTaskHandler.cs) `ProcessReviewAsync` 中以 `while` 迴圈推進 — 下一位 designee 若已於先前步驟核准 → 自動標記 `RequestDesignatedReviewer.Status='approved'` + `Comment='已於先前步驟審核（自動核准）'`，並寫一筆代簽 `ApprovalRecord`，繼續找再下一位；遇到沒在歷史中的 designee 才停下並通知。**此邏輯不受新規則限縮影響**（同 step 內延續）。

**外層整 step 跳過 designated**：當外層 `SkipUnreviewableStepsAsync` 偵測到某未抵達的 designated step 全部 designee 都已在歷史中 → 依新規則判斷（總監 OR 相鄰）→ 整步跳過時，並把該申請所有 pending designee 都設為 approved（保持 `RequestDesignatedReviewers` 與 `ApprovalRecord` 狀態一致）。

**所有剩餘步驟皆被自動代簽** → 申請自動核准 + 通知申請人。

**AuthorizeStepAsync 防呆**：限縮為「總監（`JobTitle.Level == 1`）reviewer 重複 PATCH」→ 400「您已在先前步驟核准過此申請，不需重複審核」。非總監允許重審（與新規則對齊）—— 2026-09 放寬後相鄰同人根本走不到該關，但「非相鄰同人須重審」的場景仍需要它放行，故維持不變。

**待審清單同步**：[PaymentRequestReadService.StepMatchClause](../../Api/Services/Dapper/PaymentRequestReadService.cs) pending tab 的 `NOT EXISTS` 子句加上「reviewer 是 Level=1」條件，僅排除「總監已被自動代簽」的殘留待審項目。非總監若不滿足跳過條件 → 該 step 正常顯示在待審清單中。

**代理審核**：以 `ReviewedById`（實際點按者）為去重依據，`OnBehalfOfUserId`（受代理人）不算已審。

**退回重送 → 歷史清零**：以 `ApprovalRecords` 中最近一次 `Action='returned'` 的 `ReviewedAt` 當分隔線，僅計入該時點之後的 approved 紀錄。退回前審過的人重送後仍須再審。不需新增 schema、不影響稽核軌跡（紀錄全保留）。

**追加預支 → 以批次為範圍（2026-07 新增）**：advance 追加時前一輪的 `ApprovalRecords` 仍在，若「已審過」判定不限定批次，會造成三個嚴重後果 —— ①第 1 輪審過的總監在追加輪看不到也不能審（整單卡死）②所有步驟被自動跳過導致追加未經審核就核准 ③應收通知的審核者被跳過。因此**下列四處判定一律加上 `RoundNo == 該申請目前的 CurrentRoundNo`**，批次由 [AdvanceSupplementService.ResolveCurrentRoundAsync](../../Api/Services/AdvanceSupplementService.cs) 解析（非 advance 恆回 1，行為與過去完全相同）：

| 位置 | 用途 |
|---|---|
| `PaymentRequestReadService.StepMatchClause` | 待審清單去重 |
| `ApprovalTaskHandler.AuthorizeStepAsync` | 重複 PATCH 防呆 |
| `ApprovalFlowService.GetApprovedReviewerIdsAsync` / `GetApprovedSupervisorIdsAsync` | 自動跳過 + 代簽判定 |
| `ApprovalNotificationService.GetApprovedReviewerIdsAsync` | 通知去重 |

> 第 5 處查 `ApprovalRecords` 的位置是 `ApprovalFlowService.PickEarliestProxyAsync`（挑代簽人）。
> 它只在「已判定要跳過」之後決定代簽人是誰、不決定是否跳過，且候選已先收斂成 `pool ∩ approvedReviewerIds`
> （該集合本身已套用 `RoundNo` 與退回分隔線），故不需另加 `RoundNo` 條件。

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
- **撥款日端點權限**（部門 Code AC/FIN/Jabez HQ/CEO，或改制後英文全名碼） → [department-visibility.md](department-visibility.md)
- **API 端點清單** → [api-routes.md §審核任務](../api-routes.md#審核任務)
- **Entity（ApprovalItem / Step / Record / Override / RequestDesignatedReviewer）** → [database-schema.md](../database-schema.md)
