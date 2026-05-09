# 請假規則

本文件定義 Jabez 的請假業務規則：15 種假別、時間單位、年假 / 喪假 / 補休額度、天數上限驗證、日期重疊驗證、人事薪資整合。

## 假別一覽（15 種）

| # | 假別 | LeaveType | 時間單位 | 天數上限 | 薪資影響 |
|---|------|-----------|---------|---------|---------|
| 1 | 年假(特休假) | `annual` | 半天 | 依年資（3~30 天） | 有薪 |
| 2 | 事假 | `personal` | 小時 | 無上限 | 按天數扣除全額薪資 |
| 3 | 病假 | `sick` | 小時 | 無上限 | 按天數扣除半薪 |
| 4 | 補休 | `compensatory` | 半天（扣 4 小時/半天） | 依加班時數 | 有薪 |
| 5 | 公假 | `official` | 天 | 無上限 | 有薪 |
| 6 | 婚假 | `marriage` | 天 | 8 天（可不連續） | 有薪 |
| 7 | 產假 | `maternity` | 天（**選起始日、自動填 56 天**） | 56 天 | 有薪 |
| 8 | 流產假(3 個月以上) | `miscarriage_3m` | 天 | 28 天 | 有薪 |
| 9 | 流產假(2-3 個月) | `miscarriage_2to3m` | 天 | 7 天 | 有薪 |
| 10 | 流產假(未滿 2 個月) | `miscarriage_under2m` | 天 | 5 天 | 有薪 |
| 11 | 產檢假 | `prenatal_checkup` | 小時 | 7 天 | 有薪 |
| 12 | 陪產假 | `paternity` | 小時 | 7 天 | 有薪 |
| 13 | 喪假 | `bereavement` | 天 | 依親屬關係（3/6/8 天） | 有薪 |
| 14 | 歲時祭儀假 | `ceremonial_festival` | 天 | 3 天/年（跨年歸零，**限原住民**） | 有薪 |
| 15 | 高階主管假 | `senior_executive` | 半天 | **無上限** | **不扣任何項目**（協理以上專用，`JobTitle.Level ≤ 3`） |

## 時間單位規則

請假輸入依假別分為三種單位，儲存仍為 `LeaveRequest.Hours`（`decimal(5,1)`）：

| 單位 | 換算 | 輸入 UI | 適用假別 |
|------|------|---------|---------|
| 小時 (`hour`) | 自然小時（**整點**） | `datetime-local` 整點步進（分鐘僅 00） | 事假、病假、產檢假、陪產假 |
| 半天 (`half_day`) | 4 小時 = 半天 | 日期 + 上午/下午 選擇 | 年假、補休、高階主管假 |
| 整天 (`day`) | 8 小時 = 1 天 | 起迄日期選擇 | 公假、婚假、產假、喪假、歲時祭儀假、流產假系列 |

- **產假特例**：選擇起始日後，結束日自動填為起始日 + 55 天（共 56 天），總時數固定 448 小時。法規為一次請完，禁止重複活躍申請（同 `EmployeeId` 存在 `pending` / `approved` 產假）。
- **補休扣除**：申請 1 個半天（4 小時）→ 從可補休時數池扣 4 小時。
- **高階主管假權限閘門**：前後端皆檢查 `JobTitle.Level ≤ 3`；前端透過 JWT `job_title_level` claim 判斷選項可見性，後端在 `CreateAsync` / `UpdateAsync` / `SubmitAsync` 各階段驗證。
- **分鐘限制（小時單位）**：僅允許 `:00`（`step="3600"` 秒 = 整點步進），前後端皆驗證時數為整數倍。

## 年假額度規則（依年資）

| 年資 | 年假天數 |
|------|---------|
| 未滿 6 個月 | 0 天 |
| 滿 6 個月 ~ 未滿 1 年 | 3 天 |
| 滿 1 年 ~ 未滿 2 年 | 10 天（優於勞基法 7 日） |
| 滿 2 年 ~ 未滿 3 年 | 10 天 |
| 滿 3 年 ~ 未滿 5 年 | 14 天 |
| 滿 5 年 ~ 未滿 10 年 | 15 天 |
| 10 年以上 | 每年加 1 天，上限 30 天 |

> 年資根據 `User.HireDate` 計算。API 端點：`GET /leave-requests/annual-quota`。

## 喪假親屬關係與天數

| 天數 | 親屬關係 |
|------|---------|
| 8 天 | 配偶、父母、養父母、繼父母 |
| 6 天 | 祖父母（含外祖父母）、子女、配偶之父母、配偶之養父母或繼父母 |
| 3 天 | 曾祖父母、兄弟姊妹、配偶之祖父母 |

> 喪假須在 `LeaveRequest.BereavementRelationship` 欄位記錄親屬關係，前端以下拉選單選擇。

## 天數上限驗證（累計制）

- 送出申請（submit）時，後端查詢該使用者**同假別**、**已送出或已核准**的申請總時數
- 加上本次申請時數，檢查是否超過上限
- 天數換算：`累計時數 ÷ 8 小時 = 天數`
- 年假按**年度**累計，產假系列與喪假**不限年度**
- 喪假按**同親屬關係**分別累計

## 日期重疊驗證（防重複申請）

- **觸發點**：Create / Update / Submit 三處皆驗證
- **判定方式**：以 `[StartDate, EndDate)` datetime 半開區間嚴格相交為準（`existing.Start < new.End AND existing.End > new.Start`）
  - 半天 / 小時假時段已編碼於 datetime，「同日上午半天 + 下午半天」、「4/1 09:00-12:00 + 4/1 14:00-17:00」可正確並存
- **比對範圍**：既有申請狀態為 `draft` / `pending` / `approved`（編輯時 `excludeId` 排除自身）
- **跨假別**：不同假別也會檢查重疊（避免事假 + 病假同期重疊）
- **產假特例**：產假已有獨立 active 檢查（`LeaveType=='maternity'` 時若已存在 pending/approved 直接擋下，文案為「已有未完成或進行中的產假申請」），重疊邏輯對 maternity 跳過避免雙重訊息；但其他假別仍會檢查與既有產假的重疊
- **錯誤訊息**：列出最多 3 筆衝突明細（`#ID 假別 起迄時間 (status)`），超過則附「另有 N 筆…」

## 補休規則

- 依系統統計之加班工時扣抵
- 可補休時數 = 已核准加班申請 `EstimatedHours` 合計 − 已送出/已核准補休假 `Hours` 合計
- API 端點：`GET /leave-requests/compensatory-hours`

## 請假申請步驟

```
請假申請 → 選擇假別 → 填入開始/結束時間 → 請假原因 → 指定審核人
如需多層級審核：新增審核人順序等同審核順序
```

## 人事薪資頁面整合

- 薪資編輯頁顯示該月**所有已核准**的請假紀錄（假別、期間、天數）
- 薪資明細信件同步顯示「本月請假紀錄」表格
- 事假扣薪與病假扣薪仍於扣款項目中獨立計算

## 涉及元件

| 元件 | 說明 |
|------|------|
| `LeaveRequest.BereavementRelationship` | Entity 欄位：喪假親屬關係 |
| `LeaveRequestHandler.ValidateLeaveQuotaAsync()` | 天數上限驗證（累計制） |
| `LeaveRequestHandler.CheckOverlapAsync()` | 日期重疊驗證（draft/pending/approved 比對） |
| `LeaveRequestHandler.LeaveTypeNameZh` | 假別中文名稱字典（重疊衝突訊息用） |
| `LeaveRequestReadService.GetOverlappingRequestsAsync()` | Dapper：查詢同員工 datetime 區間相交申請 |
| `OverlappingLeaveRequestDto` | 重疊衝突 DTO（內部用） |
| `LeaveRequestHandler.GetAnnualQuotaAsync()` | 年假額度 API |
| `LeaveRequestHandler.CalculateAnnualLeaveDays()` | 年資 → 年假天數計算 |
| `PayrollReadService` | 新增查詢該月所有請假明細 |
| `PayrollHandler.BuildLeaveDetailSection()` | 薪資明細信件請假紀錄 HTML |
| 前端 `leave-request.model.ts` | 15 種假別定義、喪假關係常數、天數上限常數 |
| 前端 `leave-request-form` | 假別下拉選單（分群組）、條件式欄位、額度提示 |
| 前端 `payroll-form` | 本月請假紀錄表格 |

---

## 跨業務關聯

- **請假走簽核流程** → [approval-flow.md](approval-flow.md)（請假屬 Group A 全程禁止自審）
- **事假 / 病假扣薪計算** → [payroll-formula.md §扣薪規則](payroll-formula.md)
- **打卡時段阻擋規則**（已核准請假時段內無法打卡） → [api-routes.md §出勤打卡](../api-routes.md#出勤打卡)
- **產假狀態 / 配額查詢端點** → [api-routes.md §請款 / 請假...](../api-routes.md#請款--請假--出差--加班--預支申請)
- **`LeaveRequest` Entity 結構** → [database-schema.md](../database-schema.md)
