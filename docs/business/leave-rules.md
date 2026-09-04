# 請假規則

本文件定義 Jabez 的請假業務規則：19 種假別、時間單位、年假 / 喪假 / 補休額度、天數上限驗證、日期重疊驗證、人事薪資整合。

## 假別一覽（19 種）

| # | 假別 | LeaveType | 時間單位 | 天數上限 | 薪資影響 |
|---|------|-----------|---------|---------|---------|
| 1 | 年假(特休假) | `annual` | 半天 | 依年資（3~30 天） | 有薪 |
| 2 | 事假 | `personal` | 小時 | 無上限 | 按天數扣除全額薪資 |
| 3 | 病假 | `sick` | 小時 | 無上限 | 按天數扣除半薪 |
| 4 | 補休 | `compensatory` | 半天（扣 4 小時/半天） | 期初匯入 + 依加班時數（**僅限選「補休」的加班單**） | 有薪 |
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
| 18 | 育嬰留職停薪 | `parental_leave` | 天（**連續日曆天**） | 每名子女合計 730 天（2 年） | **不支薪**（底薪與各加給按在職天數比例計算；整月留停者不入薪資名單） |
| 19 | 育嬰留停(單日) | `parental_leave_daily` | 天（**一次一日**） | 每人每年 30 日，併入該名子女 730 天 | 同上 |

> **天數上限一律指「工作日」**：除歲時祭儀假與育嬰留職停薪（`parental_leave`）外，全部假別的天數 / 時數皆已扣除**國定假日與六日**（詳見 [§扣除假日計算天數](#扣除假日計算天數2026-07-新增2026-07-擴大適用)）。育嬰留職停薪為連續日曆天，其「730 天」指日曆天。

## 時間單位規則

請假輸入依假別分為三種單位，儲存仍為 `LeaveRequest.Hours`（`decimal(5,1)`）：

| 單位 | 換算 | 輸入 UI | 適用假別 |
|------|------|---------|---------|
| 小時 (`hour`) | 自然小時（**整點**）；**跨日逐日累加只算工作日** | 日期 + 整點小時下拉（分鐘僅 00） | 事假、家庭照顧假、病假、產檢假、陪產假 |
| 半天 (`half_day`) | 4 小時 = 半天 | 日期 + 上午/下午 選擇（上午時段：**補休 09:00–13:00**、其餘 08:00–12:00） | 年假、補休、高階主管假 |
| 整天 (`day`) | 8 小時 = 1 天 | 起迄日期選擇 | 公假、婚假、產假、喪假、歲時祭儀假、流產假系列、生理假、育嬰留職停薪、育嬰留停(單日) |

- **產假特例**：選擇起始日後，結束日自動填為起始日 + 55 天（共 56 個**日曆天**），總時數為其中**工作日數 × 8**（約 40 天 / 320 小時，非固定 448）。法規為一次請完，禁止重複活躍申請（同 `EmployeeId` 存在 `pending` / `approved` 產假）。
- **補休扣除**：申請 1 個半天（4 小時）→ 從可補休時數池扣 4 小時。
- **半天時段的代表性時刻**：`StartDate` / `EndDate` 存的是時段代表時間 —— 上午 `08:00–12:00`、下午 `13:00–17:00`；
  **唯獨補休的上午為 `09:00–13:00`**（起點 09:00、訖點 13:00），時段下拉標籤與清單頁顯示的時間隨之改變。
  單一真相為前端 [`halfDayAmStartHour()` / `halfDayAmEndHour()`](../../Admin/src/app/features/admin/leave-requests/models/leave-request.model.ts)，
  送出 payload 與下拉標籤共用，勿另行硬編碼 08 / 12。
  > 這是**顯示與儲存時刻**的差異，**不改時數**：半天恆 4 小時、全日恆 8 小時。後端一律以
  > 「起 &lt; 13:00 ＝上午」**與「訖 &gt; 13:00 ＝下午」**分類時段（`LeaveDayExpander.ExpandHalfDayUnit`），
  > **不可改用「等於 08:00 / 12:00」判定，訖點界線也不可用 12:00** —— 補休的訖 13:00 會被判成下午，
  > 單日 am→am 就變成全日 8 小時。逐日展開仍取標準上午時段 08:00–12:00（故出缺勤報表的請假欄、
  > 應出勤時段、登入自動補卡皆不受影響）。半天時數本身走「HalfDay 信任 client」，`body.Hours` 未帶時的退路
  > `LeaveRequestHandler.ComputeHalfDaySlotHours` 亦以同一組時段界線推時數，不用 End − Start 時間差。
  > 打卡阻擋為半開區間 `[Start, End)`，故補休上午請假者 13:00 打上班卡不會被擋。
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

> 年資根據 `User.HireDate` 計算，並**扣除已核准且已經過去的育嬰留職停薪天數**（見 [§育嬰留職停薪規則](#育嬰留職停薪規則2026-08-新增)）。
> 計算收斂於 [Api/Common/SeniorityHelper.cs](../../Api/Common/SeniorityHelper.cs)，`GetAnnualQuotaAsync` 與 `ValidateLeaveQuotaAsync` 兩處必須帶入相同的扣除天數，否則查得到的額度與實際能送的會不一致。
> API 端點：`GET /leave-requests/annual-quota`（回應含 `parentalLeaveExcludedDays`，前端在年假提示區塊顯示「年資已扣除育嬰留停 N 天」）。

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
- **請假時段**：半天單位，**上午時段為 09:00–13:00**、下午 13:00–17:00；時數仍以半天 4 小時計（見上方「半天時段的代表性時刻」）。
- **可補休時數來源兩塊**：
  1. **期初匯入餘額**（`User.CompensatoryOpeningHours`）：系統上線前（115/1~6/30）以紙本累計、由使用者管理頁手動輸入；**須於 116/6/30（含）前休完，逾期未休部分歸零作廢**。到期日為固定常數 `LeaveRequestHandler.CompensatoryOpeningExpiry`（2027-06-30）。
  2. **系統加班補休**：07/01 起系統內已核准加班申請 `EstimatedHours` 合計（該欄本身已是**各關聯專案時數的合計快取**，故此處仍只需 SUM 父表，不必展開 `OvertimeRequestProject` 子表）；**不到期**。
     - ⚠️ **只計入 `CompensationType='compensatory'` 的加班單**（2026-08 新增）。加班申請可整單二擇一選「補休」或「加班費」；
       選加班費的單已依勞基法試算金額、隨加班日**次月**薪資發放現金，再進補休池就是同一段工時領兩次（雙重給付）。
       舊資料因欄位預設值為 `compensatory`，全部原封不動留在池內。詳見 [payroll-formula.md §第 12 條](payroll-formula.md)。
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

## 育嬰留職停薪規則（2026-08 新增）

法源：《性別平等工作法》§16、育嬰留職停薪實施辦法。2026-01-01 起施行「以日為單位」彈性新制。

### 兩種假別代碼

| 代碼 | 名稱 | 適用 | 時間單位 | 上限 |
|------|------|------|---------|------|
| `parental_leave` | 育嬰留職停薪 | 長期留停（數週 ~ 2 年） | 天，**連續日曆天** | 每名子女合計 **730 天**（2 年） |
| `parental_leave_daily` | 育嬰留停(單日) | 彈性單日新制 | 天，**強制一次一日**（`EndDate = StartDate`） | 每人每年 **30 日**，且併入該名子女 730 天總額度 |

> **為何拆兩個代碼**：長期留停與彈性單日在額度基準（每子女 vs 每年度）、日期輸入 UI（起迄 vs 單日）、時數計算（日曆天 vs 工作日）上皆不同，合併成一個代碼會讓額度驗證與表單提示無法講清楚。

### `parental_leave` 刻意不列入工作日型假別

`parental_leave` **不在** `LeaveDayExpander.WorkingDayLeaveTypes` 內（與歲時祭儀假同列），三個理由：

1. **語意正確**：留停整段期間都不在職，含六日與國定假日，`Hours = 日曆天數 × 8`，`Hours ÷ 8` 直接等於日曆天數。
2. **繞開送件阻擋**：工作日型假別在 `SubmitAsync` 會**強制要求區間橫跨的每個年度行事曆皆已匯入**。育嬰留停跨 1~2 年，未來年度行事曆通常尚未匯入，會直接擋件。
3. **避免逐日展開爆量**：非工作日型不觸發前端 `refreshWorkingDays` 的逐日 chip 清單。

`parental_leave_daily` 則為一般工作日請假語意，**仍列入**工作日型（請到國定假日 / 六日會被擋）。

### 申請資格（三階段驗證：Create / Update / Submit）

- **在職滿 6 個月** —— `SeniorityHelper.Calculate`；`IsSuperAdmin` 一律通過（比照高階主管假；亦對應法規「未滿 6 個月經雇主同意亦可申請」）。
- **子女未滿 3 歲** —— `ChildBirthDate` 必填，**起訖日皆須落在 3 歲生日之前**（`StartDate` 與 `EndDate` 都 `< ChildBirthDate + 3 年`）。只擋起始日會讓一張 730 天的留停一路延續到子女 5 歲。前端另有同規則的即時警示（`isParentalChildTooOld`，純前端計算，不依賴 API 的 `childAgeValid`——後者以「今天」判定，看不出所選區間會不會跨過 3 歲生日）。
- 資格檢查共用 `LeaveRequestHandler.CheckParentalEligibilityAsync`。

### 兩個新欄位（`LeaveRequest`）

| 欄位 | 型別 | 用途 |
|------|------|------|
| `ChildBirthDate` | `date?` | 子女出生日期。驗證 3 歲資格（起訖日皆須在 3 歲生日前），並作為「每名子女 730 天」的**累計分組鍵**（不同子女額度互相獨立） |
| `ContinueInsurance` | `bit?` | 留停期間是否續保勞健保。**僅記錄意願**供人事作業參考，系統不算遞延帳、不自動扣款 |

> 比照喪假 `BereavementRelationship` 的「假別專屬欄位」先例；切換為其他假別時一併清空。

### 薪資處理（不支薪）

實作於 [Api/Services/Dapper/PayrollReadService.cs](../../Api/Services/Dapper/PayrollReadService.cs)：

1. 以 `parentalSql` 逐日歸月算出該月留停日曆天數（假單區間 ∩ 當月區間）。
2. `留停天數 ≥ 當月天數` **且當月無其他應發／扣項**（加班費、上月假日津貼、`PayrollAdjustment` 其他加項／扣項皆為 0）→ **整月留停，不產生薪資列**。若有其中任一項仍會出單（底薪與加給折為 0），否則這些已賺得的金額會憑空消失且不計入月合計。
3. 折減率 `workRatio = max(0, 1 − 留停天數 ÷ 30)`，**底薪與 3 項加給**（伙食費 / 其他加給 / 代扣代付款）乘上此比例。
   > 這與事假等無薪假的「日薪 × 天數」**完全等價**（日薪本身就是底薪 ÷ 30）。刻意**不用**「(當月天數 − 留停天數) ÷ 30」：31 天的月份請 1 天留停時該式為 30/30 = 1，會完全不折減，「不支薪」形同無效。

**不折減的項目與理由**：

- **勞保費、健保費** —— 續保者仍須繳全額。實作上另存 `insuredBaseSalary`（折減前底薪）供**級距 lookup** 使用；若用折減後底薪查級距會掉到低級距，等於把保費也「按比例」少扣。
- **勞退自提** —— 同理，提繳基準用 `insuredBaseSalary`。
- **加班費、假日津貼** —— 本就是實績金額。
- **`dailySalary`** —— 在折減前先算好，避免事假 / 病假等扣薪被雙重折減。

> ⚠️ **實領可能為負數**：只在職少數天卻繳全額勞健保時，實領會是負數，差額即員工**應補繳的保費**。薪資編輯頁與薪資明細信皆會顯示警示，提醒人事另行收取或依規辦理個人負擔部分遞延繳納（最長 3 年），**勿直接寄送薪資明細**。

> **刻意不沿用 `leaveSql` 的寫法**：既有扣薪 SQL 用「區間相交 + 整單 `SUM(Hours)`」，跨月假單會被每個月各扣一次全額（見 [payroll-formula.md](payroll-formula.md) 已知限制）。育嬰留停動輒數月，必須逐日歸月，故 `parentalSql` 改用日期交集。**新增長期型假別時不得沿用 `leaveSql` 模式。**

### 年資與特休

留停期間**不計入工作年資**，特休隨之暫停累積。`SeniorityHelper.Calculate(hireDate, now, excludedDays)` 的作法是把到職日往後推 `excludedDays` 天得到「有效到職日」再算日曆差。扣除天數由 `GetParentalLeaveDaysAsync` 提供：只計**已核准**者，進行中的留停只算到今天為止（尚未發生的天數不提前扣年資）；已結束者用 `Hours ÷ 8`（銷假後 `Hours` 已遞減，天數自動反映）。

退休金基數本系統未實作，不受影響。

### 刻意不實作（僅表單提示）

| 項目 | 原因 |
|------|------|
| 雙親合計 60 日 | 配偶可能不在同一公司，系統無從得知，無法驗證 |
| 以日申請須 5 日前預告 | 法規寫「原則上」，緊急狀況需彈性；前端顯示提示文字，後端不硬擋 |
| 8 成薪津貼計算 | 由勞保局按前 6 個月平均月投保薪資給付（每名子女父母雙方各最多 6 個月），非公司給付，員工自行申請 |
| 勞健保個人負擔遞延 3 年 | 需要「應收未收 / 遞延餘額」帳務子表，等同新開一個帳務模組；本次只記錄續保意願 |
| 留停期間不得有工作事實 | 管理面規範，系統不做強制邏輯 |

### 其他已知限制

- **育嬰留職停薪（長期）不開放銷假** —— 列表與檢視頁的「銷假」按鈕對 `parental_leave` 隱藏（`canRevoke()` 排除）。原因：非工作日型假別會逐日展開整段日曆天，2 年留停會在銷假頁產出 700+ 個逐日 chip，UI 無法使用。提前復職暫由人事以編輯／重新申請處理。`parental_leave_daily` 不受此限。
- **`User.Status` 未新增第三態** —— 留停狀態由已核准的育嬰假單推導，避免動到依賴 `Status='active'` 的 6 處查詢（薪資、打卡提醒、簽核升級、指定審核者、付款提醒）。副作用：留停員工仍可能被排入簽核流程、仍會收到打卡提醒（打卡本身已由 `AttendanceHandler` 的「請假時段內擋打卡」自動擋住）。

### 日期區間必須涵蓋整個結束日

兩種育嬰假別的 `StartDate` 一律正規化為當日 00:00、`EndDate` 補滿為當日 **23:59**（`EndOfDay()`）。彈性單日的 `EndDate` 亦為**起始日當天 23:59**，不是 00:00。

原因：重疊驗證（`CheckOverlapAsync`）與打卡阻擋皆以**半開區間 `[StartDate, EndDate)`** 比對。結束日若停在 00:00：

- 彈性單日會變成**零長度區間** → 同一天可重複申請（額度扣兩次、薪資扣兩天）、也不與同日其他假別衝突，且已核准的留停當天仍可打卡。
- 長期留停的**最後一天**同樣不受保護。

此外 `UpdateAsync` 若把彈性單日存成 `EndDate = StartDate`（皆 00:00），會被下游通用守門判為 `EndDate <= StartDate` 而**恆回 400**，草稿永遠改不了、送不出。

### 額度查詢端點

`GET /leave-requests/parental-quota?childBirthDate=yyyy-MM-dd`

帶 `childBirthDate` 才算得出該名子女的 730 天總額度與 3 歲資格；彈性單日的年度 30 日額度不分子女，未帶亦回傳。回應見 `ParentalQuotaDto`。

## 扣除假日計算天數（2026-07 新增，2026-07 擴大適用）

**工作日型假別**選定起迄日後，系統扣除**國定假日與六日**，只計算實際工作日，並在表單即時列出「實際請假日清單」與天數。

- **適用假別（工作日型，17 種）**：`annual`（年假）/ `personal`（事假）/ `sick`（病假）/ `compensatory`（補休）/ `official`（公假）/ `senior_executive`（高階主管假）/ `marriage`（婚假）/ `maternity`（產假）/ `bereavement`（喪假）/ `miscarriage_3m`・`miscarriage_2to3m`・`miscarriage_under2m`（流產假系列）/ `prenatal_checkup`（產檢假）/ `paternity`（陪產假）/ `menstrual`（生理假）/ `family_care`（家庭照顧假）/ `parental_leave_daily`（育嬰留停單日）。集合同步於後端 `LeaveDayExpander.WorkingDayLeaveTypes`（`LeaveRequestHandler` 轉引同一份，與銷假逐日展開共用）與前端 `WORKING_DAY_LEAVE_TYPES`（[leave-request.model.ts](../../Admin/src/app/features/admin/leave-requests/models/leave-request.model.ts)）。
- **不適用假別（連續日曆天，不扣假日）**：`ceremonial_festival`（歲時祭儀假）與 `parental_leave`（育嬰留職停薪，理由見 [§育嬰留職停薪規則](#育嬰留職停薪規則2026-08-新增)）。
- **不適用「人」**：`User.IsShiftWorker = true` 的排班制員工（賣店 / 營業所）不論假別皆不扣假日，見 [§排班制員工不扣假日](#排班制員工不扣假日2026-08-新增)。
- **天數上限一律改以工作日計**：婚假 8 / 喪假 8・6・3 / 流產假 28・7・5 / 產檢假・陪產假 7 / 生理假每月 1 天・全年 12 天等數字不變，但語意變成「N 個工作日」（`ValidateLeaveQuotaAsync` 比對的 `Hours / 8` 本來就是扣假日後的值，無需額外改動）。
- **產假特例**：區間仍固定為「起始日 + 55 天 = 56 個**日曆天**」（法定一次請完、不可拆），但 `Hours` 只計其中工作日（約 40 天 / 320 小時），不再固定 448 小時。
- **假日來源＝唯一權威 `CalendarDays` 表**：台灣政府行事曆匯入時 `IsHoliday=true` 已同時涵蓋**六日 + 國定假**、補班六為工作日（`IsHoliday=false`）。透過 [CalendarDayReadService](../../Api/Services/Dapper/CalendarDayReadService.cs) 的 `GetHolidayDatesAsync` / `HasDataForRangeAsync` 讀取（與出差假日活動共用）。
- **行事曆完整性逐年檢查**：`HasDataForRangeAsync` 為 EXISTS 語意（區間內任一天有資料即 true），產假 56 天與拉長後的婚假 / 喪假可能跨年，故 `LeaveRequestHandler.HasCalendarForAllYearsAsync` 對區間橫跨的**每個年度**各查一次，全部有資料才算已匯入。
- **前端顯示**：[leave-request-form](../../Admin/src/app/features/admin/leave-requests/pages/leave-request-form/) 於工作日型假別（day / half_day / hour 三種單位皆適用）選好起迄日後呼叫輕量端點 `GET /leave-requests/working-days?start=&end=&leaveType=`（免 `calendar-days:read`），列出逐日 chip + 合計天數；行事曆未匯入時退回僅扣六日並提示。產假的結束日不在表單上，前端改以 `maternityEndDate`（起始日 +55 天）當區間終點查詢。
- **後端權威重算**：工作日型假別的 `Day` 單位（含產假）以工作日數 × 8、`Hour` 單位以逐日累加時數，於 Create / Update / **Submit** 覆寫 `Hours`；**Submit 時強制要求行事曆已匯入**（缺資料擋件並提示匯入，訊息含跨年區間的年度範圍），區間全為假日亦擋件。`half_day` 由前端以 working-days 端點計算後送出（後端沿用既有「HalfDay 信任 client」原則）。

### 排班制員工不扣假日（2026-08 新增）

賣店 / 營業所人員排班上班，**六日與國定假日照常營業**，對其而言全都是工作日。若沿用公司行事曆，
他們請六日的假會被算成 0 天並於送出時被擋（「此區間全為國定假日或六日」），等於**無法請六日的假**。

- **設定位置**：員工基本資料（使用者管理 Tab1「員工資訊」）的 **「排班制（六日與國定假日視為工作日）」** 勾選框，
  對應 `User.IsShiftWorker`（預設 `false`）。逐人設定，非部門層級；使用者列表在姓名旁掛「排班制」badge 便於稽核。
- **行為**：勾選者的 [WorkCalendarHelper](../../Api/Common/WorkCalendarHelper.cs) 三個方法一律短路 —— 區間內每一天都是工作日、
  **完全不查行事曆**，且視同「行事曆已匯入」（故 Submit 不會因該年度未匯入行事曆而被擋）。
- **旗標解析對象＝假單所有人**：Create 用 JWT 身分、Update / Submit 用 `LeaveRequest.EmployeeId`、
  銷假重算用 `leave.EmployeeId`、出缺勤報表由 SQL 隨資料列帶出（`AttendanceLeaveSourceRow.IsShiftWorker`，
  避免逐張假單 N+1）。**不可用呼叫者 id** —— Superadmin 代送、主管核准銷假時呼叫者都不是本人，用錯會靜默算錯時數。
- **連帶影響**：
  - 勾選者**請國定假日的假會扣額度**（那天是他的上班日），此為「照常營業」的直接推論。
  - 勾選者**沒有「休假日免下班卡」的放寬**（見 [attendance-clock-rules.md](attendance-clock-rules.md)），週六打加班開始前仍須先打下班卡。
  - 打卡提醒六日改為**只推排班制員工**（見 [attendance-reminder.md](attendance-reminder.md)）。
  - **不影響假日津貼** —— 假日執行活動的 `HolidayDays` 一律走公司行事曆，與此旗標無關。
- **限制**：一個勾選＝全年皆工作日，**無法表達賣店的個別公休日**（例如大年初一公休）。
  若日後有此需求，須升級為「行事曆群組」架構（`CalendarDay` 加群組維度 + 部門指派）；
  屆時把 `WorkCalendarHelper` / `LeaveDayExpander` 的 `bool ignoreHolidays` 換成 `int calendarGroupId` 即可，消費點結構不變。
- **既有假單不回溯**：改變勾選狀態不會重算已核准假單的 `Hours`；但**銷假重算會用「當下」的旗標**，
  故變更設定前請先處理未完成的銷假。

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
| `LeaveTypeNames`（`Api/Common/LeaveTypeNames.cs`） | 假別中文名稱字典（重疊衝突訊息、打卡阻擋訊息、銷假通知信用） |
| `LeaveRequestReadService.GetOverlappingRequestsAsync()` | Dapper：查詢同員工 datetime 區間相交申請 |
| `OverlappingLeaveRequestDto` | 重疊衝突 DTO（內部用） |
| `LeaveRequestHandler.GetAnnualQuotaAsync()` | 年假額度 API |
| `LeaveRequestHandler.GetMenstrualQuotaAsync()` | 生理假配額 API（`isFemale` + 月/年配額） |
| `LeaveRequestHandler.IsFemaleAsync()` | 查 `EmployeeProfile.Gender == "F"`（生理假限定） |
| `SeniorityHelper`（`Api/Common/SeniorityHelper.cs`） | 年資計算（含育嬰留停扣除天數）→ 年假天數，單一真相 |
| `LeaveRequestHandler.GetParentalQuotaAsync()` | 育嬰留停額度 API（每子女 730 天 + 年度單日 30 日） |
| `LeaveRequestHandler.CheckParentalEligibilityAsync()` | 育嬰留停資格（在職滿 6 個月 + 子女未滿 3 歲） |
| `LeaveRequestHandler.GetParentalLeaveDaysAsync()` | 已經過去的留停累計天數（供年資扣除） |
| `LeaveRequest.ChildBirthDate` / `.ContinueInsurance` | Entity 欄位：育嬰留停專用（子女出生日期 / 續保意願） |
| `PayrollReadService` | 查詢該月所有請假明細；事假 / 病假 / 生理假 / 家庭照顧假扣薪計算；**育嬰留停整月排除 + 當月按比例折減**（`parentalSql` 逐日歸月、`insuredBaseSalary` 保護級距 lookup） |
| `PayrollHandler.BuildLeaveDetailSection()` | 薪資明細信件請假紀錄 HTML |
| 前端 `leave-request.model.ts` | 19 種假別定義、喪假關係常數、天數上限常數、`MenstrualQuota`、`ParentalQuota` |
| 前端 `leave-request-form` | 假別下拉選單（分群組）、條件式欄位、額度提示 |
| 前端 `payroll-form` | 本月請假紀錄表格；育嬰留停按比例註記與**實領負數警示** |
| `LeaveRevocation` / `LeaveRevocationDate` | Entity：銷假申請 + 逐日明細 |
| `LeaveDayExpander` | 請假單逐日展開的單一真相（假別分類常數 `WorkingDayLeaveTypes` / `TimeUnitMap` 亦收斂於此）。2026-09 起 `LeaveDay` 除 `Date` / `Hours` 外另帶**逐日時段** `Segment`（`full` / `am` / `pm` / `partial`，見 `Constants.LeaveDaySegments`）+ `Start` / `End`（clamp 在 08:00–17:00），供出缺勤報表顯示「事假 09:00–13:00 (4h)」/「年假 上午」與 `ExpectedWorkWindow` 算應出勤時段。`Hours` 沿用既有整點差語意（不扣午休），與 `End − Start` 不必然等長 |
| `LeaveRevocationService.ApplyAsync()` | 銷假核准後套用到父單（逐日整組重算 Hours、全銷轉 cancelled） |
| `LeaveRevocationHandler` | 銷假 CRUD + `revocable-dates` + `Submit`（重跑請假簽核） |
| `LeaveRevocationReadService` | Dapper：銷假列表 / 單筆（JOIN 原假單 + 逐日明細 + 指定審核者） |
| `LeaveRequestHandler.FilterFullyRevokedAsync()` | 重疊驗證的逐日後置過濾（挖空的日子可重新申請） |
| 前端 `leave-revocation-form` | 銷假表單（原假單唯讀卡 + 逐日 chip 勾選 + 原因 + 指定審核者） |
| 前端 `leave-revocation.service.ts` / `leave-revocation.model.ts` | 銷假 HTTP service 與型別 |

---

## 跨業務關聯

- **請假走簽核流程** → [approval-flow.md](approval-flow.md)（請假屬 Group A 全程禁止自審）
- **事假 / 病假扣薪計算、育嬰留停按比例** → [payroll-formula.md §扣薪規則](payroll-formula.md)
- **打卡時段阻擋規則**（已核准請假時段內無法打上下班卡；例外：**當日全日請假 + 已核准加班單 → 可直接打「加班開始」**，免下班卡；**已核准銷假的日子不再阻擋**） → [attendance-clock-rules.md](attendance-clock-rules.md)
- **銷假重跑請假簽核** → [approval-flow.md §銷假重跑請假簽核](approval-flow.md#銷假重跑請假簽核2026-08-新增)
- **銷假通知（審核 + 職務代理人解除）** → [notifications.md](notifications.md)
- **產假狀態 / 配額查詢端點** → [api-routes.md §請款 / 請假...](../api-routes.md#請款--請假--出差--加班--預支申請)
- **`LeaveRequest` Entity 結構** → [database-schema.md](../database-schema.md)
