# 申請表類型總覽

系統共有 **9 種申請表**，依用途分為三類。每種申請表都走簽核流程（詳見 [approval-flow.md](approval-flow.md)）。

## 單號（RequestNo）對照

6 種金錢相關申請表均有單號，格式 `{PREFIX}-yyyyMMdd-NNN`（per-prefix-per-day 序號池，於 Handler `CreateAsync` 產生，由 unique index 保護並發）：

| 申請表 | 前綴 | 範例 |
|--------|------|------|
| 請款申請 PaymentRequest | `PR-` | `PR-20260520-001` |
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
| 1 | 請款申請 | `/admin/payment-requests` | `/payment-requests` / `payment_request` | **Group B 首位跳過** | 一般費用請款（含發票明細）；走簽核 + 撥款。**Type=`vendor` 時必須選擇 `Vendor` 主檔（廠商管理 `/admin/vendors`），找不到時可從表單即時新增** |
| 2 | 請假申請 | `/admin/leave-requests` | `/leave-requests` / `leave` | **Group A 全程禁止** | 15 種假別；走簽核（無撥款） |
| 3 | 加班申請 | `/admin/overtime-requests` | `/overtime-requests` / `overtime` | **Group A 全程禁止** | 加班預申請；走簽核（無撥款） |
| 4 | 預支申請 | `/admin/advance-requests` | `/advance-requests` / `advance` | **Group B 首位跳過** | 費用預支；走簽核 + 撥款，**事後須沖銷** |
| 5 | 出差預支申請 | `/admin/travel-requests` | `/travel-requests` / `travel` | **Group A 全程禁止** | 出差預支款項；走簽核 + 撥款，**事後走沖銷流程** |

## 出差類申請表（2 種）

| # | 申請表 | 前端路徑 | API Prefix / RequestType | 自審分組 | 流程特性 |
|---|--------|----------|--------------------------|---------|---------|
| 6 | 出差請款申請 | `/admin/travel-payment-requests` | `/travel-payment-requests` / `travel_payment` | **Group A 全程禁止** | 員工小額代墊後直接請款（**無沖銷流程**）；走簽核 + 撥款 |
| 7 | 假日執行活動申請 | `/admin/holiday-travel-requests` | `/holiday-travel-requests` / `holiday_travel` | **Group B 首位跳過** | 假日活動，**計入假日津貼**（無發票明細）；共用 `TravelRequest` entity（`IsHolidayTravel=true`） |

## 沖銷類申請表（2 種，獨立簽核流程）

| # | 申請表 | 前端路徑 | API Prefix / RequestType | 自審分組 | 流程特性 |
|---|--------|----------|--------------------------|---------|---------|
| 8 | 預支沖銷申請 | `/admin/write-off-requests` | `/write-off-requests` / `write_off` | **Group B 首位跳過** | 沖銷預支申請（含發票上傳）；獨立簽核流程，可能產生退款 |
| 9 | 出差預支沖銷申請 | `/admin/travel-write-off-requests` | `/travel-write-off-requests` / `travel_write_off` | **Group B 首位跳過** | 沖銷出差預支申請；獨立簽核流程，可能產生退款 |

> **自審分組說明**：所有 9 種申請表均支援指定審核者（`UseApplicantDesignated`）模式，但對「申請人本身排入指定審核者清單」的處理方式分為兩組。詳見 [approval-flow.md §申請人指定審核模式](approval-flow.md#申請人指定審核模式useapplicantdesignated)。

## 流程關係圖

```
預支申請        ──→  預支沖銷申請（事後沖銷）
出差預支申請    ──→  出差預支沖銷申請（事後沖銷）
出差請款申請    ──→  （無沖銷，小額代墊直接請款）
請款 / 加班 / 請假 / 假日執行活動  ──→  獨立流程，無沖銷
```

## 假日執行活動 vs 出差預支 差異

兩者共用 `TravelRequest` entity，但 `IsHolidayTravel` 旗標決定行為：

| 項目 | 出差預支（`IsHolidayTravel=false`） | 假日執行活動（`IsHolidayTravel=true`） |
|------|-------------------------------------|--------------------------------------|
| 前端路徑 | `/admin/travel-requests` | `/admin/holiday-travel-requests` |
| 含 Items 與發票明細 | ✓ | ✗（僅記錄活動地點 / 期間 / 參與人員） |
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

## 跨業務關聯

- **簽核流程主軸**（含批次核准 / 自審 / 上層級 / 指定審核 / 跨步驟去重）→ [approval-flow.md](approval-flow.md)
- **簽核升級機制**（找上層部門主管 + 代理人）→ [approval-escalation.md](approval-escalation.md)
- **請假 15 種假別細則** → [leave-rules.md](leave-rules.md)
- **PDF 簽名欄規則** → [pdf-signatures.md](pdf-signatures.md)
- **假日執行活動的假日津貼計算** → [payroll-formula.md](payroll-formula.md)
- **API 端點清單** → [api-routes.md](../api-routes.md)
- **Entity 結構** → [database-schema.md](../database-schema.md)
