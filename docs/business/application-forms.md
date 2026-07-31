# 申請表類型總覽

系統共有 **10 種申請表**，依用途分為四類。每種申請表都走簽核流程（詳見 [approval-flow.md](approval-flow.md)）。

## 單號（RequestNo）對照

7 種金錢相關申請表均有單號，格式 `{PREFIX}-yyyyMMdd-NNN`（per-prefix-per-day 序號池，於 Handler `CreateAsync` 產生，由 unique index 保護並發）：

| 申請表 | 前綴 | 範例 |
|--------|------|------|
| 請款申請 PaymentRequest | `PR-` | `PR-20260520-001` |
| 預審申請 PreReviewRequest | `PRV-` | `PRV-20260520-001` |
| 預支申請 AdvanceRequest | `ADV-` | `ADV-20260520-001` |
| 出差請款申請 TravelPaymentRequest | `TPR-` | `TPR-20260520-001` |
| 出差預支申請 TravelRequest（`IsHolidayTravel=false`） | `TR-` | `TR-20260520-001` |
| 假日執行活動申請 TravelRequest（`IsHolidayTravel=true`） | `HTR-` | `HTR-20260520-001` |
| 預支沖銷申請 WriteOffRecord | `WO-` | `WO-20260520-001` |
| 出差預支沖銷申請 TravelWriteOffRecord | `TWO-` | `TWO-20260520-001` |

> 請假 / 加班無單號（僅以 GUID Id 識別）。

## 一般申請表（5 種）

| # | 申請表 | 前端路徑 | API Prefix / RequestType | 自審分組 | 流程特性 |
|---|--------|----------|--------------------------|---------|---------|
| 1 | 請款申請 | `/admin/payment-requests` | `/payment-requests` / `payment_request` | **Group B 首位跳過** | 一般費用請款（含發票明細）；走簽核 + 撥款。**Type=`vendor` 時必須選擇 `Vendor` 主檔（廠商管理 `/admin/vendors`），找不到時可從表單即時新增**；**Type=`general`（一般請款）明細下方可批次上傳整單附件（照片 / PDF）** |
| 2 | 請假申請 | `/admin/leave-requests` | `/leave-requests` / `leave` | **Group A 全程禁止** | 16 種假別；走簽核（無撥款） |
| 3 | 加班申請 | `/admin/overtime-requests` | `/overtime-requests` / `overtime` | **Group A 全程禁止** | 加班預申請；走簽核（無撥款）。**須至少關聯 1 個專案，逐案填預估時數；整單預估總時數 = 各案加總（後端計算）**；同部門專案可複選，支援其他部門專案請獨立申請 |
| 4 | 預支申請 | `/admin/advance-requests` | `/advance-requests` / `advance` | **Group B 首位跳過** | 費用預支；走簽核 + 撥款，**事後須沖銷**；支援**追加預支**（已核准單可再加批次，重跑同一份簽核流程，見 [approval-flow.md](approval-flow.md#追加預支重跑簽核2026-07-新增)） |
| 5 | 出差預支申請 | `/admin/travel-requests` | `/travel-requests` / `travel` | **Group A 全程禁止** | 出差預支款項；走簽核 + 撥款，**事後走沖銷流程** |

## 出差類申請表（2 種）

| # | 申請表 | 前端路徑 | API Prefix / RequestType | 自審分組 | 流程特性 |
|---|--------|----------|--------------------------|---------|---------|
| 6 | 出差請款申請 | `/admin/travel-payment-requests` | `/travel-payment-requests` / `travel_payment` | **Group A 全程禁止** | 員工小額代墊後直接請款（**無沖銷流程**）；走簽核 + 撥款 |
| 7 | 假日執行活動申請 | `/admin/holiday-travel-requests` | `/holiday-travel-requests` / `holiday_travel` | **Group B 首位跳過** | 假日活動，**計入假日津貼**（無發票明細）；共用 `TravelRequest` entity（`IsHolidayTravel=true`） |

## 沖銷類申請表（2 種，獨立簽核流程）

| # | 申請表 | 前端路徑 | API Prefix / RequestType | 自審分組 | 流程特性 |
|---|--------|----------|--------------------------|---------|---------|
| 8 | 預支沖銷申請 | `/admin/write-off-requests` | `/write-off-requests` / `write_off` | **Group B 首位跳過** | 沖銷預支申請（含發票上傳）；獨立簽核流程，可能產生退款；**明細下方可批次上傳整單附件（照片 / PDF）** |
| 9 | 出差預支沖銷申請 | `/admin/travel-write-off-requests` | `/travel-write-off-requests` / `travel_write_off` | **Group B 首位跳過** | 沖銷出差預支申請；獨立簽核流程，可能產生退款 |

## 預審類申請表（1 種）

| # | 申請表 | 前端路徑 | API Prefix / RequestType | 自審分組 | 流程特性 |
|---|--------|----------|--------------------------|---------|---------|
| 10 | 預審申請 | `/admin/pre-review-requests` | `/pre-review-requests` / `pre_review` | **Group B 首位跳過** | 事前預審：實際花費前送類似請款的單據（含報價單 / 品項 / 金額）走簽核取得核准。**金額不計入任何統計報表**（刻意不加入款項統計 UNION）、**無撥款流程**（無分期撥款 / 撥款日 / 撥款狀態 / 財務撥款必填）。品項含**品項類別下拉**（活動硬體 / 設計師 / 製作產品 / 採購產品 / 採購庶務 / 其他，「其他」可自訂鍵入）；報價單上傳支援 **OCR 自動辨識**（`POST /quote-ocr`）；PDF 列印**合併所有上傳檔**（報價單圖檔 + 附件）成單一 PDF |

> **自審分組說明**：所有 10 種申請表均支援指定審核者（`UseApplicantDesignated`）模式，但對「申請人本身排入指定審核者清單」的處理方式分為兩組。詳見 [approval-flow.md §申請人指定審核模式](approval-flow.md#申請人指定審核模式useapplicantdesignated)。

## 流程關係圖

```
預支申請  ──(可多次追加，同一張單)──→  第 2/3/… 次追加批次（重跑簽核）
預支申請        ──→  預支沖銷申請（事後沖銷；沖銷基準 = 含追加的總額）
出差預支申請    ──→  出差預支沖銷申請（事後沖銷）
出差請款申請    ──→  （無沖銷，小額代墊直接請款）
請款 / 加班 / 請假 / 假日執行活動  ──→  獨立流程，無沖銷
預審申請        ──→  獨立流程，無撥款、不計入報表
```

## 假日執行活動 vs 出差預支 差異

兩者共用 `TravelRequest` entity，但 `IsHolidayTravel` 旗標決定行為：

| 項目 | 出差預支（`IsHolidayTravel=false`） | 假日執行活動（`IsHolidayTravel=true`） |
|------|-------------------------------------|--------------------------------------|
| 前端路徑 | `/admin/travel-requests` | `/admin/holiday-travel-requests` |
| 含 Items 與發票明細 | ✓ | ✗（僅記錄活動地點 / 期間 / 參與人員） |
| 參與人員個人參與日期 | —（不使用參與人員） | ✓ 每位參與人員可逐日勾選參與日期（可不連續，限活動期間內）；**未勾選＝全程參與**；個人假日津貼天數依勾選日期中屬假日者計算（Submit 時快照至 `TravelRequestParticipant.HolidayDays`），未勾選者沿用整單 `HolidayDays` |
| 走沖銷流程 | ✓（`travel-write-off-requests`） | ✗ |
| 計入假日津貼 | ✗ | ✓（依已核准 EndDate 月份歸月，獎金計入次月薪資） |
| 含撥款日 / 預計撥款日 | ✓ | ✓ |
| 權限 Code | `travel-requests:*` | `holiday-travel-requests:*`（獨立權限） |

---

## 發票號碼重複檢查規則

含發票明細的申請（請款 `PaymentRequest`、預支沖銷 `WriteOffRecord`、出差沖銷 `TravelWriteOffRecord`）在建立 / 更新時，會對明細層級的發票號碼做重複檢查：

- **批次內去重**：同一張單的多筆明細不可有相同發票號碼。
- **跨表唯一性**：跨請款 + 沖銷各表全系統唯一（排除已拒絕申請；更新時排除自身明細）。

**例外（手打中文文字排除）**：發票號碼欄位若**含中文 / CJK 字**（如手打「收據」「領據」等非統一發票），視為手打文字，**排除於上述兩項重複檢查之外**——同一張單或跨單填多筆「收據」皆可送出。純英數的真正統一發票（如 `AB12345678`）仍維持重複檢查。判定邏輯見 [InvoiceNoHelper.IsManualText](../../Api/Common/InvoiceNoHelper.cs)。

> 出差請款 `TravelPaymentRequest` / 出差預支 `TravelRequest` / 預支 `AdvanceRequest` 目前無發票重複檢查。

---

## 發票 OCR 多筆辨識

請款 / 預支沖銷 / 出差請款 / 出差預支沖銷四個表單明細的發票上傳，支援 OCR 自動辨識（後端 Google Gemini，端點 `POST /invoice-ocr`）。**一張照片若同時包含多張發票或多個交通票根，會辨識出幾筆就自動展開成幾列明細**（單張則為一列，向下相容）；每列各自保留一份該圖檔複本。辨識準確度受拍攝品質影響，各列辨識後仍需人工核對。UI 流程見 [frontend-design.md §12.5c](../frontend-design.md)。

### 買方抬頭 / 統編驗證

OCR 同時辨識統一發票的**買方抬頭（買受人公司名稱）**與**買方統編**，上傳當下即時比對公司合法的 4 組「抬頭＋統編」白名單，不符者在**該列明細下方顯示紅字警告**（**非阻擋式，仍可送出**）。判定規則（前端共用工具 [`invoice-buyer-validator.ts`](../../Admin/src/app/shared/utils/invoice-buyer-validator.ts)，`VALID_INVOICE_BUYERS` 為單一真相）：

| 抬頭 | 統編 |
|------|------|
| 雅比斯國際創意策略股份有限公司 | 28830371 |
| 雅比斯國際創意策略股份有限公司壯圍營業所 | 92663912 |
| 疆界地域美學有限公司 | 42837895 |
| 疆界地域美學有限公司豐濱營業所 | 60277862 |

- **只驗統一發票**（`docType === 'invoice'`）；交通票根（`ticket`）無買方統編，跳過不驗。
- **抬頭與統編需「皆讀得到」才判斷；任一缺漏即跳過不警告**（涵蓋收銀機 / 二聯式發票無買方欄、手寫發票讀不全等）。
- **統編為主要錨點**（8 碼數字 OCR 較可靠；長中文公司名常被 OCR 缺字截斷，不宜硬比對）：
  - 統編符合某組 + 抬頭相容 → 通過。抬頭相容＝完全相等／互為子字串／**公司名前 3 個識別字相同**（容忍「雅比斯國際創意策略…」被 OCR 讀成「雅比斯…策略…」）。
  - 統編符合某組、但抬頭明顯為**他家公司**（前綴不同）→ 警告「買方抬頭與統編不符」。
  - 統編不在 4 組內 → 警告「買方統編不正確（非公司抬頭）」。
- 正規化：統編全形轉半形後只留數字；抬頭移除空白後比對。
- 警告僅供上傳當下提示，**不持久化**（不存 DB、不寫 migration）；重開草稿不會重現警告。

> 設計取捨：統一編號是法定唯一識別碼，正確即代表是本公司（含區分總公司 / 營業所）。實測 Gemini 對長公司名常缺字（如把「雅比斯國際創意策略股份有限公司」讀成「雅比斯策略股份有限公司」），故以統編為準、抬頭採寬鬆比對，避免每張正確發票都跳假警告。

---

## 跨業務關聯

- **簽核流程主軸**（含批次核准 / 自審 / 上層級 / 指定審核 / 跨步驟去重）→ [approval-flow.md](approval-flow.md)
- **簽核升級機制**（找上層部門主管 + 代理人）→ [approval-escalation.md](approval-escalation.md)
- **請假 16 種假別細則** → [leave-rules.md](leave-rules.md)
- **PDF 簽名欄規則** → [pdf-signatures.md](pdf-signatures.md)
- **假日執行活動的假日津貼計算** → [payroll-formula.md](payroll-formula.md)
- **API 端點清單** → [api-routes.md](../api-routes.md)
- **Entity 結構** → [database-schema.md](../database-schema.md)
