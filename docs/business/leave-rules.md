# 請假規則

本文件定義 Jabez 的請假業務規則：17 種假別、時間單位、年假 / 喪假 / 補休額度、天數上限驗證、日期重疊驗證、人事薪資整合。

## 假別一覽（17 種）

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
| 15 | 高階主管假 | `senior_executive` | 半天 | **每年 24 天**（曆年歸零） | **不扣任何項目**（協理以上專用，`JobTitle.Level ≤ 3`） |
| 16 | 生理假 | `menstrual` | 天（一次請一天） | 每月 1 天、全年 12 天（**限女性**） | 按天數扣除半薪（前 3 天/年純生理假，超過併入病假） |
| 17 | 家庭照顧假 | `family_care` | 小時 | **全年 7 天**（56 小時，曆年歸零） | 按天數扣除全額薪資（不另支薪） |

> **天數上限一律指「工作日」**：除歲時祭儀假外，全部假別的天數 / 時數皆已扣除**國定假日與六日**（詳見 [§扣除假日計算天數](#扣除假日計算天數2026-07-新增2026-07-擴大適用)）。

## 時間單位規則

請假輸入依假別分為三種單位，儲存仍為 `LeaveRequest.Hours`（`decimal(5,1)`）：

| 單位 | 換算 | 輸入 UI | 適用假別 |
|------|------|---------|---------|
| 小時 (`hour`) | 自然小時（**整點**）；**跨日逐日累加只算工作日** | 日期 + 整點小時下拉（分鐘僅 00） | 事假、家庭照顧假、病假、產檢假、陪產假 |
| 半天 (`half_day`) | 4 小時 = 半天 | 日期 + 上午/下午 選擇 | 年假、補休、高階主管假 |
| 整天 (`day`) | 8 小時 = 1 天 | 起迄日期選擇 | 公假、婚假、產假、喪假、歲時祭儀假、流產假系列、生理假 |

- **產假特例**：選擇起始日後，結束日自動填為起始日 + 55 天（共 56 個**日曆天**），總時數為其中**工作日數 × 8**（約 40 天 / 320 小時，非固定 448）。法規為一次請完，禁止重複活躍申請（同 `EmployeeId` 存在 `pending` / `approved` 產假）。
- **補休扣除**：申請 1 個半天（4 小時）→ 從可補休時數池扣 4 小時。
- **高階主管假權限閘門**：前後端皆檢查 `JobTitle.Level ≤ 3`；前端透過 JWT `job_title_level` claim 判斷選項可見性，後端在 `CreateAsync` / `UpdateAsync` / `SubmitAsync` 各階段驗證。
- **高階主管假額度**：協理以上每年 24 天（曆年 1/1~12/31），當年度未用完歸零、隔年重新給予 24 天（2026-08 由 20 天調整）。比照年假動態計算（不儲存、不排程）。**年度基準一律為「請假起始日所屬曆年」（`item.StartDate.Year`）**，非「今天所屬年度」—— 額度上限驗證於 `ValidateLeaveQuotaAsync` 的 `senior_executive` 分支；API 端點 `GET /leave-requests/senior-executive-quota` **支援 `?year=`**（未帶或超出 2000~2100 則預設當年度），回 `year` / `totalDays` / `usedDays` / `availableDays`；前端表單以起始日年度查詢額度，起始日跨年時自動重載。
  > ⚠️ 年假（`annual`）與歲時祭儀假（`ceremonial_festival`）目前仍以 `Clock.Now.Year` 為基準（`ValidateLeaveQuotaAsync` 與其 quota endpoint），跨年送件會年度錯配，尚未比照修正。
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
- 年假、家庭照顧假按**年度**累計，產假系列與喪假**不限年度**
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
  2. **系統加班補休**：07/01 起系統內已核准加班申請 `EstimatedHours` 合計（該欄本身已是**各關聯專案時數的合計快取**，故此處仍只需 SUM 父表，不必展開 `OvertimeRequestProject` 子表）；**不到期**。
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

## 家庭照顧假規則（2026-08 新增）

法源：《性別平等工作法》第 20 條。

- **申請事由**：家庭成員有**預防接種**、發生**嚴重疾病**或其他**重大事故**須親自照顧時得申請。
- **家庭成員範圍**：配偶、父母、公婆、岳父母、子女、祖父母、孫子女、兄弟姊妹，或以永久共同生活為目的同居之家屬與伴侶。此範圍**僅於申請表單以提示文字呈現**，不存欄位、不做系統驗證（照顧對象不入庫）。
- **時間單位**：小時（比照事假），跨日逐日累加只算工作日。
- **全年上限**：**7 日（56 小時）**，依申請年度累計、曆年歸零。走 `LeaveRequestHandler.LeaveTypeDaysLimit["family_care"] = 7` 的泛用分支（非產假系列 → 年度制），無獨立驗證邏輯、無獨立配額端點。
- **併入年度事假計算**：法規語意上併計事假額度；**系統實作採獨立 56 小時上限**，事假本身維持無上限（見 [§假別一覽](#假別一覽17-種)），故不做技術性額度連動，僅於表單提示文字說明。
- **薪資（不另支薪）**：比照事假**按天數扣除全額薪資**（`日薪 × 天數`），但於薪資頁與薪資明細信中**獨立一列「家庭照顧假扣薪」**呈現，不併入事假欄位。
- **雇主不得拒絕准假**，亦不得影響全勤獎金或考績 —— 此為管理面規範，系統不做強制邏輯（仍走一般簽核流程）。

## 扣除假日計算天數（2026-07 新增，2026-07 擴大適用）

**工作日型假別**選定起迄日後，系統扣除**國定假日與六日**，只計算實際工作日，並在表單即時列出「實際請假日清單」與天數。

- **適用假別（工作日型，16 種）**：`annual`（年假）/ `personal`（事假）/ `sick`（病假）/ `compensatory`（補休）/ `official`（公假）/ `senior_executive`（高階主管假）/ `marriage`（婚假）/ `maternity`（產假）/ `bereavement`（喪假）/ `miscarriage_3m`・`miscarriage_2to3m`・`miscarriage_under2m`（流產假系列）/ `prenatal_checkup`（產檢假）/ `paternity`（陪產假）/ `menstrual`（生理假）/ `family_care`（家庭照顧假）。集合同步於後端 `LeaveDayExpander.WorkingDayLeaveTypes`（`LeaveRequestHandler` 轉引同一份，與銷假逐日展開共用）與前端 `WORKING_DAY_LEAVE_TYPES`（[leave-request.model.ts](../../Admin/src/app/features/admin/leave-requests/models/leave-request.model.ts)）。
- **不適用假別（連續日曆天，不扣假日）**：僅 `ceremonial_festival`（歲時祭儀假）。
- **天數上限一律改以工作日計**：婚假 8 / 喪假 8・6・3 / 流產假 28・7・5 / 產檢假・陪產假 7 / 生理假每月 1 天・全年 12 天等數字不變，但語意變成「N 個工作日」（`ValidateLeaveQuotaAsync` 比對的 `Hours / 8` 本來就是扣假日後的值，無需額外改動）。
- **產假特例**：區間仍固定為「起始日 + 55 天 = 56 個**日曆天**」（法定一次請完、不可拆），但 `Hours` 只計其中工作日（約 40 天 / 320 小時），不再固定 448 小時。
- **假日來源＝唯一權威 `CalendarDays` 表**：台灣政府行事曆匯入時 `IsHoliday=true` 已同時涵蓋**六日 + 國定假**、補班六為工作日（`IsHoliday=false`）。透過 [CalendarDayReadService](../../Api/Services/Dapper/CalendarDayReadService.cs) 的 `GetHolidayDatesAsync` / `HasDataForRangeAsync` 讀取（與出差假日活動共用）。
- **行事曆完整性逐年檢查**：`HasDataForRangeAsync` 為 EXISTS 語意（區間內任一天有資料即 true），產假 56 天與拉長後的婚假 / 喪假可能跨年，故 `LeaveRequestHandler.HasCalendarForAllYearsAsync` 對區間橫跨的**每個年度**各查一次，全部有資料才算已匯入。
- **前端顯示**：[leave-request-form](../../Admin/src/app/features/admin/leave-requests/pages/leave-request-form/) 於工作日型假別（day / half_day / hour 三種單位皆適用）選好起迄日後呼叫輕量端點 `GET /leave-requests/working-days?start=&end=&leaveType=`（免 `calendar-days:read`），列出逐日 chip + 合計天數；行事曆未匯入時退回僅扣六日並提示。產假的結束日不在表單上，前端改以 `maternityEndDate`（起始日 +55 天）當區間終點查詢。
- **後端權威重算**：工作日型假別的 `Day` 單位（含產假）以工作日數 × 8、`Hour` 單位以逐日累加時數，於 Create / Update / **Submit** 覆寫 `Hours`；**Submit 時強制要求行事曆已匯入**（缺資料擋件並提示匯入，訊息含跨年區間的年度範圍），區間全為假日亦擋件。`half_day` 由前端以 working-days 端點計算後送出（後端沿用既有「HalfDay 信任 client」原則）。

### 小時單位跨日的時數計算（`personal` / `family_care` / `sick` / `prenatal_checkup` / `paternity`）

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

## 銷假規則（2026-08 新增）

已核准的請假若因專案調度需改回上班，可提出**銷假申請**，送出後**重跑一次原本的請假簽核流程**。

### 可銷條件（三道都要成立）

| 條件 | 說明 |
|---|---|
| 假單 `ApprovalStatus = 'approved'` | 草稿 / 審核中 / 已拒絕 / 已 `cancelled` 皆不可銷 |
| 假單 `EndDate` 尚未過去 | 假期已結束即不可銷假 |
| 逐日只能勾**今天（含）以後** | 已休完的日子不可銷，避免更動已結算薪資與既有出勤紀錄 |

### 逐日部分銷假

- 銷假時逐日勾選要取消的日期，**允許挖空中間日**（例：請 8/10–8/14，只取消 8/12）。
- 各日時數由 [`LeaveDayExpander`](../../Api/Common/LeaveDayExpander.cs) 展開，規則與送出時的權威重算完全一致，保證 `Σ 逐日時數 == LeaveRequest.Hours`：
  Day → 每工作日 8h；Hour → 同日 `end−start`、跨日首日 `Clamp(17−start,0,8)` / 中間 8 / 末日 `Clamp(end−8,0,8)`；HalfDay → 單日 am→am 4 / am→pm 8 / pm→pm 4，多日首(am 8 / pm 4) + 中間 8 + 末(pm 8 / am 4)；歲時祭儀假整段日曆天每天 8h。
- 「哪幾天被取消」的單一真相＝ `LeaveRevocationDate`。可銷清單會排除：已核准銷假的日、被其他進行中銷假單佔用的日、今天以前的日。
- 同一天不會被兩張銷假單重複扣除：`revocable-dates` 過濾 + `Create/Update/Submit` 三處重驗。

### 送簽期間：仍視為請假中

銷假送簽期間父單 `LeaveRequest` **完全不動**（維持 `approved`、`Hours` 不變），因此：

- 打卡仍被阻擋、打卡提醒仍排除該員、簽核升級仍視為「主管請假中」
- 各假別額度仍佔用、該時段仍不可重複請假
- **核准後才一次生效**

### 核准 / 退回 / 拒絕的效果

| 動作 | 效果 |
|---|---|
| 核准 | [`LeaveRevocationService.ApplyAsync`](../../Api/Services/LeaveRevocationService.cs)：從**該假單所有已核准銷假單的 distinct 日期**整組重算 → `LeaveRequest.Hours = Σ 未銷日時數`；首次銷假時把原時數存入 `OriginalHours`。全數銷完 → `ApprovalStatus = 'cancelled'`（終止狀態，自此不落入任何 `approved` 查詢）。另寄 Email 通知原假單的職務代理人「代理已解除 / 部分解除」 |
| 退回 | 銷假單轉 `returned`，可修改後重送；父單不受影響 |
| 拒絕 | 銷假單轉 `rejected`，**父單零回滾**（自始至終維持 `approved`）；被拒的日期回到可銷清單 |

> 從逐日整組重算（而非 `Hours -= X`）是刻意設計：天然冪等、併發安全，兩張銷假單搶同一天也會收斂。

### 下游影響

- **重疊驗證**：`cancelled` 已被 SQL 狀態清單排除；部分銷假則由 `LeaveRequestHandler.FilterFullyRevokedAsync` 逐日後置過濾 —— 重疊區間內每一天都已銷才不算衝突，故挖空的中間日可重新申請。
- **打卡 / 報表 / 提醒 / 升級**：`AttendanceReadService`（打卡阻擋 / 休假日免下班卡兩處，共用 `LeaveRevocationService.NotRevokedClause`）、`AttendanceReminderReadService`、`EscalationService.IsOnLeaveAsync` 皆加上「該日無已核准銷假」的排除條件。
  - **出缺勤報表**自 2026-08 改走 [`AttendanceLeaveMerger`](../../Api/Common/AttendanceLeaveMerger.cs)：已核准請假的日子會產生「請假虛擬列」，銷假日則不產列（逐日排除走 `ListApprovedRevokedDatesAsync` 批次查詢，不在假單層級過濾，以免誤刪部分銷假的其餘日子）。見 [attendance-clock-rules.md §出缺勤報表](attendance-clock-rules.md#出缺勤報表打卡--請假日2026-08-新增)。
- **薪資**：`LeaveRequest.Hours` 保持「剩餘有效時數」語意，故 [`PayrollReadService`](../../Api/Services/Dapper/PayrollReadService.cs) 的三段扣薪 SQL 不需改動即自動正確；`cancelled` 不在 `approved` 集合內。生理假「年度前 3 天半薪」的門檻只決定金額掛哪一行（生理假 / 病假扣薪率同為日薪 × 0.5），對實領薪資零影響。
  > **既有已知限制（非本次引入）**：`leaveSql` 以「區間相交 + 整單 SUM(Hours)」計算，跨月假單會被兩個月各扣一次全額。銷假後 `Hours` 遞減，兩個月等比例變小，錯誤形態不變。若要修正，正解是把扣薪改為逐日歸月。
- **補休池**：`ComputeCompensatoryAsync` 公式不需改 —— 銷假只把來自 `earned` 的時數還回池子，不會讓已到期作廢的期初額度復活。

### 簽核掛接（「跑原本的簽核一次」）

| 用途 | applicationType |
|---|---|
| `ResolveApprovalItemIdAsync`（挑流程設定） | **`"leave"`** —— 直接複用請假的 ApprovalItem + Steps，管理端**不需另設銷假流程**（簽核流程設定頁的類型下拉刻意不含銷假） |
| `ResolveStartingStepAsync` / `ApprovalRecord` / `RequestDesignatedReviewer` / 簽核任務 | **`"leave_revocation"`** —— 隔離「此人已審過」查詢，避免與同 Id 的請假單撞號 |

- `requestDays` 帶**原假單天數**（`(OriginalHours ?? Hours) / 8`），讓銷假回到與原假單相同的那組 `MinDays` 關卡（`LeaveRevocationHandler.SubmitAsync` 與 `ApprovalTaskHandler` 推進時同源）。
- 自審規則沿用請假：Group A 全程禁止自審（否定清單自動涵蓋），自審時嘗試升級審核且停在總監之前。
- 流程若含「申請人指定審核」步驟，銷假表單同樣要挑指定審核者。

## 請假申請步驟

```
請假申請 → 選擇假別 → 填入開始/結束時間（除歲時祭儀假外皆顯示扣假日後請假日清單）→ 職務代理人 → 請假原因 → 指定審核人
依天數決定關卡：< 3 天單位主管；≥ 3 天 單位主管 + 部門最高主管 + 總監
```

## 人事薪資頁面整合

- 薪資編輯頁顯示該月**所有已核准**的請假紀錄（假別、期間、天數）
- 薪資明細信件同步顯示「本月請假紀錄」表格
- 事假扣薪、家庭照顧假扣薪與病假扣薪仍於扣款項目中獨立計算（家庭照顧假比照事假全額扣薪，但獨立一列）

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
| `PayrollReadService` | 新增查詢該月所有請假明細；事假 / 病假 / 生理假 / 家庭照顧假扣薪計算 |
| `PayrollHandler.BuildLeaveDetailSection()` | 薪資明細信件請假紀錄 HTML |
| 前端 `leave-request.model.ts` | 17 種假別定義、喪假關係常數、天數上限常數、`MenstrualQuota` |
| 前端 `leave-request-form` | 假別下拉選單（分群組）、條件式欄位、額度提示 |
| 前端 `payroll-form` | 本月請假紀錄表格 |
| `LeaveRevocation` / `LeaveRevocationDate` | Entity：銷假申請 + 逐日明細 |
| `LeaveDayExpander` | 請假單逐日展開的單一真相（假別分類常數 `WorkingDayLeaveTypes` / `TimeUnitMap` 亦收斂於此） |
| `LeaveRevocationService.ApplyAsync()` | 銷假核准後套用到父單（逐日整組重算 Hours、全銷轉 cancelled） |
| `LeaveRevocationHandler` | 銷假 CRUD + `revocable-dates` + `Submit`（重跑請假簽核） |
| `LeaveRevocationReadService` | Dapper：銷假列表 / 單筆（JOIN 原假單 + 逐日明細 + 指定審核者） |
| `LeaveRequestHandler.FilterFullyRevokedAsync()` | 重疊驗證的逐日後置過濾（挖空的日子可重新申請） |
| 前端 `leave-revocation-form` | 銷假表單（原假單唯讀卡 + 逐日 chip 勾選 + 原因 + 指定審核者） |
| 前端 `leave-revocation.service.ts` / `leave-revocation.model.ts` | 銷假 HTTP service 與型別 |

---

## 跨業務關聯

- **請假走簽核流程** → [approval-flow.md](approval-flow.md)（請假屬 Group A 全程禁止自審）
- **事假 / 病假扣薪計算** → [payroll-formula.md §扣薪規則](payroll-formula.md)
- **打卡時段阻擋規則**（已核准請假時段內無法打上下班卡；例外：**當日全日請假 + 已核准加班單 → 可直接打「加班開始」**，免下班卡；**已核准銷假的日子不再阻擋**） → [attendance-clock-rules.md](attendance-clock-rules.md)
- **銷假重跑請假簽核** → [approval-flow.md §銷假重跑請假簽核](approval-flow.md#銷假重跑請假簽核2026-08-新增)
- **銷假通知（審核 + 職務代理人解除）** → [notifications.md](notifications.md)
- **產假狀態 / 配額查詢端點** → [api-routes.md §請款 / 請假...](../api-routes.md#請款--請假--出差--加班--預支申請)
- **`LeaveRequest` Entity 結構** → [database-schema.md](../database-schema.md)
