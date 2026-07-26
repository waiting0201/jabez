# 請假規則

本文件定義 Jabez 的請假業務規則：16 種假別、時間單位、年假 / 喪假 / 補休額度、天數上限驗證、日期重疊驗證、人事薪資整合。

## 假別一覽（16 種）

| # | 假別 | LeaveType | 時間單位 | 天數上限 | 薪資影響 |
|---|------|-----------|---------|---------|---------|
| 1 | 年假(特休假) | `annual` | 半天 | 依年資（3~30 天） | 有薪 |
| 2 | 事假 | `personal` | 小時 | 無上限 | 按天數扣除全額薪資 |
| 3 | 病假 | `sick` | 小時 | 無上限 | 按天數扣除半薪 |
| 4 | 補休 | `compensatory` | 半天（扣 4 小時/半天） | 期初匯入 + 依加班時數 | 有薪 |
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
| 15 | 高階主管假 | `senior_executive` | 半天 | **每年 20 天**（曆年歸零） | **不扣任何項目**（協理以上專用，`JobTitle.Level ≤ 3`） |
| 16 | 生理假 | `menstrual` | 天（一次請一天） | 每月 1 天、全年 12 天（**限女性**） | 按天數扣除半薪（前 3 天/年純生理假，超過併入病假） |

> **天數上限一律指「工作日」**：除歲時祭儀假外，全部假別的天數 / 時數皆已扣除**國定假日與六日**（詳見 [§扣除假日計算天數](#扣除假日計算天數2026-07-新增2026-07-擴大適用)）。

## 時間單位規則

請假輸入依假別分為三種單位，儲存仍為 `LeaveRequest.Hours`（`decimal(5,1)`）：

| 單位 | 換算 | 輸入 UI | 適用假別 |
|------|------|---------|---------|
| 小時 (`hour`) | 自然小時（**整點**）；**跨日逐日累加只算工作日** | 日期 + 整點小時下拉（分鐘僅 00） | 事假、病假、產檢假、陪產假 |
| 半天 (`half_day`) | 4 小時 = 半天 | 日期 + 上午/下午 選擇 | 年假、補休、高階主管假 |
| 整天 (`day`) | 8 小時 = 1 天 | 起迄日期選擇 | 公假、婚假、產假、喪假、歲時祭儀假、流產假系列、生理假 |

- **產假特例**：選擇起始日後，結束日自動填為起始日 + 55 天（共 56 個**日曆天**），總時數為其中**工作日數 × 8**（約 40 天 / 320 小時，非固定 448）。法規為一次請完，禁止重複活躍申請（同 `EmployeeId` 存在 `pending` / `approved` 產假）。
- **補休扣除**：申請 1 個半天（4 小時）→ 從可補休時數池扣 4 小時。
- **高階主管假權限閘門**：前後端皆檢查 `JobTitle.Level ≤ 3`；前端透過 JWT `job_title_level` claim 判斷選項可見性，後端在 `CreateAsync` / `UpdateAsync` / `SubmitAsync` 各階段驗證。
- **高階主管假額度**：協理以上每年 20 天（曆年 1/1~12/31），當年度未用完歸零、隔年重新給予 20 天。比照年假動態計算（不儲存、不排程，按 `StartDate` 年度過濾）。額度上限驗證於 `ValidateLeaveQuotaAsync` 的 `senior_executive` 分支；API 端點 `GET /leave-requests/senior-executive-quota` 回 `totalDays` / `usedDays` / `availableDays`。
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
- 天數換算：`累計時數 ÷ 8 小時 = 天數`（時數已扣除國定假日與六日，故等同「工作日數」）
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
- **可補休時數來源兩塊**：
  1. **期初匯入餘額**（`User.CompensatoryOpeningHours`）：系統上線前（115/1~6/30）以紙本累計、由使用者管理頁手動輸入；**須於 116/6/30（含）前休完，逾期未休部分歸零作廢**。到期日為固定常數 `LeaveRequestHandler.CompensatoryOpeningExpiry`（2027-06-30）。
  2. **系統加班補休**：07/01 起系統內已核准加班申請 `EstimatedHours` 合計；**不到期**。
- **可補休時數計算**（FIFO：補休先消耗期初餘額，期初到期後其未用部分作廢）：
  - 到期前：`可用 = 期初 + 系統加班 − 已用補休`
  - 到期後：`可用 = 系統加班 − max(0, 已用補休 − 期初)`（期初未用部分作廢）
- API 端點：`GET /leave-requests/compensatory-hours`（回 `openingHours` / `openingRemaining` / `openingExpiry` / `openingExpired` / `totalOvertimeHours` / `usedCompensatoryHours` / `availableHours`）。

## 生理假規則（限女性）

- **資格限定**：僅 `EmployeeProfile.Gender == "F"` 之員工可申請（性別存於人事資料卡，不在 JWT、不在 User）。前後端皆驗證：
  - 前端：依 `GET /leave-requests/menstrual-quota` 回傳的 `isFemale` 過濾下拉選單（比照歲時祭儀假以 `isIndigenous` 過濾的模式）。
  - 後端：`CreateAsync` / `UpdateAsync` 前置檢查 + `ValidateLeaveQuotaAsync`（submit）再次驗證。
- **時間單位**：整天（8 小時 = 1 天），**一次請一天**（每月上限 1 天即受此限制）。
- **每月上限**：1 天（8 小時）；依申請起始日所屬「年月」累計。
- **全年上限**：12 天（96 小時）；依申請起始日所屬「年度」累計。兩者皆硬性擋件。
- **薪資（半薪）**：全部按天數扣除半薪（`日薪 × 0.5 × 天數`）。
- **併入病假**：全年累計**前 3 天（24 小時）為純生理假**（薪資列「生理假扣薪」）；**超過 3 天的部分併入病假計算**（薪資併入「病假扣薪」）。因兩者皆半薪，淨薪不變，差異僅在扣款項目的歸類。薪資模組以「本年度本月之前已用生理假時數」判斷前 3 天額度是否用罄（詳見 [payroll-formula.md](payroll-formula.md)）。
- **API 端點**：`GET /leave-requests/menstrual-quota`（回 `isFemale` + 月/年配額）。

## 扣除假日計算天數（2026-07 新增，2026-07 擴大適用）

**工作日型假別**選定起迄日後，系統扣除**國定假日與六日**，只計算實際工作日，並在表單即時列出「實際請假日清單」與天數。

- **適用假別（工作日型，15 種）**：`annual`（年假）/ `personal`（事假）/ `sick`（病假）/ `compensatory`（補休）/ `official`（公假）/ `senior_executive`（高階主管假）/ `marriage`（婚假）/ `maternity`（產假）/ `bereavement`（喪假）/ `miscarriage_3m`・`miscarriage_2to3m`・`miscarriage_under2m`（流產假系列）/ `prenatal_checkup`（產檢假）/ `paternity`（陪產假）/ `menstrual`（生理假）。集合同步於後端 `LeaveRequestHandler.WorkingDayLeaveTypes` 與前端 `WORKING_DAY_LEAVE_TYPES`（[leave-request.model.ts](../../Admin/src/app/features/admin/leave-requests/models/leave-request.model.ts)）。
- **不適用假別（連續日曆天，不扣假日）**：僅 `ceremonial_festival`（歲時祭儀假）。
- **天數上限一律改以工作日計**：婚假 8 / 喪假 8・6・3 / 流產假 28・7・5 / 產檢假・陪產假 7 / 生理假每月 1 天・全年 12 天等數字不變，但語意變成「N 個工作日」（`ValidateLeaveQuotaAsync` 比對的 `Hours / 8` 本來就是扣假日後的值，無需額外改動）。
- **產假特例**：區間仍固定為「起始日 + 55 天 = 56 個**日曆天**」（法定一次請完、不可拆），但 `Hours` 只計其中工作日（約 40 天 / 320 小時），不再固定 448 小時。
- **假日來源＝唯一權威 `CalendarDays` 表**：台灣政府行事曆匯入時 `IsHoliday=true` 已同時涵蓋**六日 + 國定假**、補班六為工作日（`IsHoliday=false`）。透過 [CalendarDayReadService](../../Api/Services/Dapper/CalendarDayReadService.cs) 的 `GetHolidayDatesAsync` / `HasDataForRangeAsync` 讀取（與出差假日活動共用）。
- **行事曆完整性逐年檢查**：`HasDataForRangeAsync` 為 EXISTS 語意（區間內任一天有資料即 true），產假 56 天與拉長後的婚假 / 喪假可能跨年，故 `LeaveRequestHandler.HasCalendarForAllYearsAsync` 對區間橫跨的**每個年度**各查一次，全部有資料才算已匯入。
- **前端顯示**：[leave-request-form](../../Admin/src/app/features/admin/leave-requests/pages/leave-request-form/) 於工作日型假別（day / half_day / hour 三種單位皆適用）選好起迄日後呼叫輕量端點 `GET /leave-requests/working-days?start=&end=&leaveType=`（免 `calendar-days:read`），列出逐日 chip + 合計天數；行事曆未匯入時退回僅扣六日並提示。產假的結束日不在表單上，前端改以 `maternityEndDate`（起始日 +55 天）當區間終點查詢。
- **後端權威重算**：工作日型假別的 `Day` 單位（含產假）以工作日數 × 8、`Hour` 單位以逐日累加時數，於 Create / Update / **Submit** 覆寫 `Hours`；**Submit 時強制要求行事曆已匯入**（缺資料擋件並提示匯入，訊息含跨年區間的年度範圍），區間全為假日亦擋件。`half_day` 由前端以 working-days 端點計算後送出（後端沿用既有「HalfDay 信任 client」原則）。

### 小時單位跨日的時數計算（`personal` / `sick` / `prenatal_checkup` / `paternity`）

工作日標準時段為 **08:00–17:00（全日 8 小時）**，與 half_day 的 am 08:00–12:00 / pm 13:00–17:00 一致。常數同步於後端 `LeaveRequestHandler.WorkdayStartHour` / `WorkdayEndHour` 與前端 `WORKDAY_START_HOUR` / `WORKDAY_END_HOUR`；演算法同步於 `ComputeHourUnitHoursAsync`（後端）與 `computeHourUnitHours`（前端）。

| 情境 | 時數 |
|------|------|
| 同日 | `endHour − startHour`（維持既有語意，不扣午休）；當日為假日 → 0，送出擋件 |
| 跨日 · 首個工作日 | `Clamp(17 − startHour, 0, 8)` |
| 跨日 · 中間工作日 | 各 8 小時 |
| 跨日 · 末個工作日 | `Clamp(endHour − 8, 0, 8)` |
| 落在假日的日期 | 0（且不把時段挪到相鄰工作日） |

> 範例：週五 14:00 → 下週一 12:00 ＝ 3（週五）+ 0（六日）+ 4（週一）＝ **7 小時**。
> 此規則同時修正了改版前「跨日以連續時鐘時數計算、會把夜間時數算進去」的問題（原本同案例為 70 小時）。

## 依請假天數決定簽核關卡（2026-07 新增）

請假簽核流程可**依申請天數**動態決定關卡：**< 3 天只需單位主管；≥ 3 天需 單位主管 + 部門最高主管 + 總監**。以簽核步驟的**天數門檻 `ApprovalStep.MinDays`** 實作（詳見 [approval-flow.md §天數門檻](approval-flow.md#依請假天數決定簽核關卡minday-門檻2026-07-新增)）：

- 天數＝`Hours / 8`（已扣假日後的工作日）。`MinDays=null` 的步驟一律納入；`MinDays=N` 的步驟僅當天數 ≥ N 才納入，否則乾淨略過。
- 請假流程建議配置：Step1 單位主管（`UseApplicantDepartment`，MinDays 空）→ Step2 部門最高主管（`UseDirectSupervisor`，MinDays=3）→ Step3 總監（固定 `JobTitle.Level=1`，MinDays=3）。
- 角色由既有模式對應，可於「簽核流程設定」頁自行調整每關的部門 / 職稱 / 天數門檻。

## 職務代理人（2026-07 新增）

請假表單可指定**職務代理人**（記錄 + 通知，**不參與簽核**）：

- 欄位 `LeaveRequest.AgentUserId`（nullable，FK→Users `OnDelete=NoAction`；刪除使用者時由 `UserHandler.DeleteAsync` 清洗設 NULL）。
- 前端下拉選項取自輕量端點 `GET /users/lookup`（在職者、排除本人）；列表 / 詳情顯示代理人姓名。
- 送出時（含 Superadmin 自動核准）以 Email 通知代理人「您被指定為 XXX 的職務代理人」（[ApprovalNotificationService.NotifyLeaveAgentAsync](../../Api/Services/ApprovalNotificationService.cs)，沿用 `ApprovalEmailEnabled` 開關）。

## 請假申請步驟

```
請假申請 → 選擇假別 → 填入開始/結束時間（除歲時祭儀假外皆顯示扣假日後請假日清單）→ 職務代理人 → 請假原因 → 指定審核人
依天數決定關卡：< 3 天單位主管；≥ 3 天 單位主管 + 部門最高主管 + 總監
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
| `LeaveRequestHandler.GetMenstrualQuotaAsync()` | 生理假配額 API（`isFemale` + 月/年配額） |
| `LeaveRequestHandler.IsFemaleAsync()` | 查 `EmployeeProfile.Gender == "F"`（生理假限定） |
| `LeaveRequestHandler.CalculateAnnualLeaveDays()` | 年資 → 年假天數計算 |
| `PayrollReadService` | 新增查詢該月所有請假明細 |
| `PayrollHandler.BuildLeaveDetailSection()` | 薪資明細信件請假紀錄 HTML |
| 前端 `leave-request.model.ts` | 16 種假別定義、喪假關係常數、天數上限常數、`MenstrualQuota` |
| 前端 `leave-request-form` | 假別下拉選單（分群組）、條件式欄位、額度提示 |
| 前端 `payroll-form` | 本月請假紀錄表格 |

---

## 跨業務關聯

- **請假走簽核流程** → [approval-flow.md](approval-flow.md)（請假屬 Group A 全程禁止自審）
- **事假 / 病假扣薪計算** → [payroll-formula.md §扣薪規則](payroll-formula.md)
- **打卡時段阻擋規則**（已核准請假時段內無法打卡） → [api-routes.md §出勤打卡](../api-routes.md#出勤打卡)
- **產假狀態 / 配額查詢端點** → [api-routes.md §請款 / 請假...](../api-routes.md#請款--請假--出差--加班--預支申請)
- **`LeaveRequest` Entity 結構** → [database-schema.md](../database-schema.md)
