# 薪水計算公式（人事薪資模組）

1. **日薪** = 底薪 ÷ 30（四捨五入至整數）
2. **假日津貼** = 日薪 × 假日執行活動天數（**上個月**歸月：以已核准假日執行活動申請的 `EndDate` 所屬月份歸月，獎金計入次月薪資。例：3 月活動 → 4 月薪資；跨月活動（如 3/30~4/2）EndDate=4/2 歸 4 月 → 5 月薪資）
   - **申請人**：領整單 `HolidayDays`（活動全期間的假日天數，Submit 時依行事曆快照；`int`，不逐日、不半天）。
   - **參與執行人員**：`COALESCE(TravelRequestParticipant.HolidayDays, TravelRequest.HolidayDays)` — 有勾選個人參與日期者領「勾選日期中屬假日者的**時段權重總和**」（全天 1.0 / 上半天 0.5 / 下半天 0.5，Submit 時快照至 `decimal(5,1)`）；未勾選（NULL）＝全程參與，沿用整單天數。
   - 跨月活動不依個人參與日期拆月，一律以整單 `EndDate` 歸月。
   - **中點捨入**：天數可為 0.5 的倍數，奇數日薪 × .5 天必然落在 `.5` 中點，故金額一律 `Math.Round(日薪 × 天數, 0, MidpointRounding.AwayFromZero)`（`Math.Round` 預設是銀行家捨入，會少 1 元）。[PayrollReadService](../../Api/Services/Dapper/PayrollReadService.cs) 與簽核台預估 [PaymentRequestReadService.BuildHolidayAllowances](../../Api/Services/Dapper/PaymentRequestReadService.cs) 兩處須一致。
3. **勞保費** = `User.LaborInsuranceOverride ?? lookupBracket(底薪).EmployeeLabor`（覆寫優先；無覆寫則查級距表向上取最近級距）
4. **健保費** = `(User.HealthInsuranceOverride ?? lookupBracket(底薪).EmployeeHealth) × (1 + min(健保眷屬數, 3))`（覆寫優先；眷屬上限 3 口，第 4 口起不再加；眷屬數來自 `HealthInsuranceDependents` 表）
5. **事假扣薪** = 日薪 × 事假天數（按天數扣除全額薪資）
6. **病假扣薪** = 日薪 × 0.5 × 病假天數（按天數扣除半薪）
7. **生理假扣薪** = 日薪 × 0.5 × 生理假天數（按天數扣除半薪）
   - 限女性（`EmployeeProfile.Gender == "F"`）；每月 1 天、全年 12 天上限（見 [leave-rules.md](leave-rules.md)）。
   - **前 3 天/年（24h）為純生理假**，列「生理假扣薪」；**超過 3 天的部分併入「病假扣薪」**（兩者皆半薪，淨薪不變，差異僅在歸類）。
   - 拆分依「本年度本月之前已用生理假時數」判斷前 3 天額度是否用罄：`pureThisMonth = min(本月生理假時數, max(0, 24 − 本年度本月前生理假時數))`，餘額併入病假天數。
8. **家庭照顧假扣薪** = 日薪 × 家庭照顧假天數（按天數扣除全額薪資，公式同事假但**獨立一列**呈現）
   - 全年 7 日（56 小時）上限，法源《性別平等工作法》§20（見 [leave-rules.md](leave-rules.md#家庭照顧假規則2026-08-新增)）。
9. **育嬰留職停薪按比例**（2026-08 新增；非扣項，而是**應發項目的折減**）
   - 該月留停日曆天數由 `parentalSql` **逐日歸月**計算（假單區間 ∩ 當月區間），涵蓋 `parental_leave` 與 `parental_leave_daily`。
   - `留停天數 ≥ 當月天數` **且當月無其他應發／扣項**（加班費、上月假日津貼、`PayrollAdjustment` 其他加項／扣項皆為 0）→ **整月留停，該員工不產生薪資列**。有其中任一項仍會出單（底薪與加給折為 0），避免已賺得的金額憑空消失、且不計入月合計。
   - 折減率 `workRatio = max(0, 1 − 留停天數 ÷ 30)`，**底薪 + 伙食費 + 5 種加給**乘上此比例。與事假等無薪假的「日薪 × 天數」**完全等價**（日薪即底薪 ÷ 30）。
   - ⚠️ 刻意**不用**「(當月天數 − 留停天數) ÷ 30」：31 天的月份請 1 天留停時該式為 30/30 = 1，會完全不折減，「不支薪」形同無效。
   - **不折減**：勞保費、健保費（續保者仍繳全額）、勞退自提、加班費、假日津貼。實作上另存 `insuredBaseSalary`（折減前底薪）供**級距 lookup 與勞退自提**使用 —— 若用折減後底薪查級距會掉到低級距，等於把保費也按比例少扣。
   - `dailySalary` 在折減前先算好，避免事假 / 病假等扣薪被雙重折減。
   - ⚠️ **實領可能為負數**：在職天數少但保費全額時，差額即員工**應補繳的保費**。薪資編輯頁與薪資明細信皆顯示警示，提醒人事另行收取或辦理個人負擔部分遞延繳納（最長 3 年），勿直接寄送明細。系統**不做遞延帳**。
   - 詳見 [leave-rules.md §育嬰留職停薪規則](leave-rules.md#育嬰留職停薪規則2026-08-新增)。
10. **實領薪水** = 底薪 + 伙食費 + 加班費 + 加給合計 + 假日津貼 + 其他加項 − 勞保費 − 健保費 − 事假扣薪 − 病假扣薪 − 生理假扣薪 − 家庭照顧假扣薪 − 其他扣項 − 勞退自提扣款
   - **加給合計** = 職務加給 + 主管加給 + 其他加給 + 調整差額 + 外派加給（5 種來自 User 表，由最新生效 SalaryAdjustmentRecord 同步而來，亦可在基本資料手動覆寫，null/未填視為 0）
   - 底薪與加給若當月有育嬰留停，已為折減後的金額（見第 9 條）。
11. **勞退自提扣款** = 提繳底薪 × `User.LaborPensionSelfContributionRate`（%，0~6 整數，員工自願提撥）÷ 100（四捨五入至整數）
   - 性質同 `LaborInsuranceOverride`/`HealthInsuranceOverride`：User 表直接欄位，**不**經過 SalaryAdjustmentRecord 歷史同步，可在基本資料 Tab 直接編輯（null 視為 0%）。
   - 提繳底薪＝`insuredBaseSalary`（育嬰留停折減前的底薪）。

> 人事薪資為動態計算，不儲存於資料庫。前端可匯出 PDF 薪資表。
> 薪資編輯頁與薪資明細信件額外顯示該月**所有已核准的請假紀錄**（全假別，非僅事假/病假/家庭照顧假）。

### 銷假對扣薪的影響（2026-08）

`LeaveRequest.Hours` 的語意是**剩餘有效時數** —— 銷假核准後由 [`LeaveRevocationService.ApplyAsync`](../../Api/Services/LeaveRevocationService.cs) 從逐日重算並遞減，全數銷完則 `ApprovalStatus` 轉 `cancelled`（不在 `approved` 集合內）。因此 [`PayrollReadService`](../../Api/Services/Dapper/PayrollReadService.cs) 的三段 SQL（`leaveSql` / `priorMenstrualSql` / `leaveDetailSql`）**不需任何改動即自動正確**。

- 銷假只能取消**今天（含）以後**的請假日，故不會回頭更動已休完的日子。
- 生理假「年度前 3 天半薪」的門檻只決定金額掛「生理假扣薪」還是「病假扣薪」哪一行 —— 兩者扣薪率同為 `日薪 × 0.5`，**對實領薪水零影響**。
- 補休池（`ComputeCompensatoryAsync`）公式不需改：銷假只把來自 `earned` 的時數還回池子，不會讓已到期作廢的期初額度復活。

> **既有已知限制（非銷假引入）**：`leaveSql` 以「區間相交 + 整單 `SUM(Hours)`」計算扣薪，**跨月假單會被兩個月各扣一次全額**。銷假後 `Hours` 遞減，兩個月等比例變小，錯誤形態不變、不會惡化。若要修正，正解是把扣薪改為「逐日歸月」（需要一張請假逐日明細表）。
>
> ⚠️ **新增長期型假別時不得沿用 `leaveSql` 的寫法** —— 2026-08 新增的育嬰留職停薪動輒橫跨數月，若沿用區間相交會災難性放大這個 bug，故 `parentalSql` 改以「假單區間 ∩ 當月區間」的日期交集逐日歸月。`parental_leave` 為連續日曆天型、`parental_leave_daily` 強制單日，兩者用同一段交集即可正確歸月，不需 `LeaveDayExpander` 逐日展開。
> 健保費若眷屬數 ≥ 1，PDF 與信件會在金額右側補註腳「（含健保眷屬 N 口）」。

---

## 薪資欄位連動規則（重要 / 避免遺忘）

人事薪資模組仰賴 `User` 表的 **7 個薪資欄位**，這 7 個欄位的「真實來源」是員工人事資料卡的「薪資調整歷史（`SalaryAdjustmentRecord`）」。**每次新增 / 修改 / 刪除欄位時，三個位置必須同步更新**。

### 資料流（Source of Truth）

```
SalaryAdjustmentRecord（人事資料卡 Tab 2，多筆歷史）
   │
   │  PUT /users/{id}/profile
   │  → EmployeeProfileHandler.UpsertAsync
   │  → 取 EffectiveDate ≤ Asia/Taipei 今日 中 EffectiveDate 最大的那一筆
   │  → 寫回 User 表（7 個欄位一次同步）
   ▼
User 表 7 個薪資欄位（基本資料 Tab 1 可手動覆寫）
   │
   │  GET /payroll?year=YYYY&month=MM
   │  → PayrollReadService 直接讀 User.* 計算
   ▼
EmployeePayrollDto / 月度合計 / 薪資編輯頁 / 薪資明細 Email + PDF
```

### 7 個欄位對照表

| # | 名稱 | SalaryAdjustmentRecord | User 欄位 | EmployeePayrollDto | NetSalary 角色 |
|---|---|---|---|---|---|
| 1 | 底薪 | `BaseSalary` | `BaseSalary` | `BaseSalary` | 加項 |
| 2 | 伙食費 | `MealAllowance` | `MealAllowance` | `MealAllowance` | 加項 |
| 3 | 職務加給 | `PositionAllowance` | `PositionAllowance` | `PositionAllowance` | 加項 |
| 4 | 主管加給 | `DutyAllowance` | `DutyAllowance` | `DutyAllowance` | 加項 |
| 5 | 其他加給 | `OtherAllowance` | `OtherAllowance` | `OtherAllowanceAmount` ⚠️ | 加項 |
| 6 | 調整差額 | `AdjustmentDifference` | `AdjustmentDifference` | `AdjustmentDifference` | 加項 |
| 7 | 外派加給 | `OverseasAllowance` | `OverseasAllowance` | `OverseasAllowance` | 加項 |

> ⚠️ `EmployeePayrollDto.OtherAllowanceAmount` 命名特例：因 DTO 既有 `OtherAddition`/`OtherDeduction` 用「Other」字首，為避免歧義改用 `OtherAllowanceAmount`，前端 model 對應 `otherAllowanceAmount`。其它 6 個欄位三層命名一致。

### 同步觸發 & 規則

- **觸發點**：`PUT /users/{id}/profile`（人事資料卡 Tab 2 儲存）
- **挑選紀錄**：`EffectiveDate <= Clock.Now.Date`（Asia/Taipei）中 `EffectiveDate` 最大的一筆
- **無符合紀錄**：完全不動 `User` 表（沿用既有值）
- **null 處理**：SalaryAdjustmentRecord 上的加給為 `decimal?`，`null` 同步後寫回 `User` 仍為 `null`；薪資公式視 `null` 為 `0`
- **手動覆寫**：基本資料 Tab 1 可直接編輯這 7 個欄位（呼叫 `PATCH /users/{id}`）；下次儲存人事資料卡時又會被最新生效紀錄覆蓋——以薪資調整歷史為**最終真實來源**

### 加 / 改 / 刪薪資欄位的 Checklist

新增或修改其中一個欄位，**必須同步以下 14 處**（漏一處就會壞掉；其中第 6 / 12 項漏改是**外洩**，比壞掉更嚴重）：

1. `Api/Models/Entities/SalaryAdjustmentRecord.cs` — entity 欄位
2. `Api/Models/Entities/User.cs` — entity 欄位 + 對應 Configuration / Migration
3. `Api/Models/Dtos/UserDtos.cs` — `UserDto` / `CreateUserRequest` / `UpdateUserRequest`
4. `Api/Services/Dapper/UserReadService.cs` — 3 處 SELECT / tuple / 映射
5. `Api/Handlers/UserHandler.cs` — Create + Update 接收 form 欄位（薪資段落包在 `PayrollFieldAccess.CanSeeSalary` 內）
6. ⚠️ `Api/Common/PayrollFieldAccess.cs` — `Mask(UserDto)` 的抹除清單（**漏改＝無 `payroll:read` 者看得到該欄**）
7. `Api/Handlers/EmployeeProfileHandler.cs` — 同步邏輯 `user.Xxx = latestSalary.Xxx`
8. `Api/Services/Dapper/PayrollReadService.cs` — `employeeSql` SELECT、迴圈讀取、`netSalary` 公式、月度 `Sum`
9. `Api/Models/Dtos/PayrollDtos.cs` — `EmployeePayrollDto` + `MonthlyPayrollDto` 的 `Total*`
10. `Api/Handlers/PayrollHandler.cs` — `BuildPaySlipHtml` 應發項目區塊
11. 前端：`Admin/.../users/models/user.model.ts` + `user-form.ts` (FormGroup / patchValue / submit) + `user-form.html`
12. ⚠️ 前端：`Admin/.../users/pages/user-form/user-form.ts` 的 `SALARY_CONTROLS` 常數（**漏改＝該欄不會被 disable，會出現在無權者的畫面與送出的 payload**）
13. 前端：`Admin/.../payroll/models/payroll.model.ts` + `payroll-list.html` (summary 或 column) + `payroll-form.html` (應發項目)
14. 文件：本檔（更新 7 個欄位對照表 + 公式 + Checklist 數字）+ [hr-profile.md](hr-profile.md)（薪資自動同步條目）

---

## 誰看得到薪資欄位（欄位級權限，2026-08）

「進得了員工管理」不等於「看得到薪資」。`users:read` 只決定能否開啟員工管理，**薪資與可反推薪資的欄位另需 `payroll:read`** —— 沿用「人事薪資」模組同一把鑰匙，未另立權限碼。

| 範圍 | 需要 | 缺少時 |
|---|---|---|
| 員工管理 Tab1 的 11 個薪資 / 勞健保欄 | `payroll:read` | API 回 `null`，前端整段不 render、控制項 `disable()` |
| 員工管理 Tab2「薪資調整歷史」 | `payroll:read` | API 回 `[]`，前端整區不 render |
| Tab3「每月健保費試算」（＝健保覆寫 ×(1+眷屬數)，可反推投保金額） | `payroll:read` | getter 早退回 `null`，區塊自動關閉。**眷屬名單本身不受限** |
| 列印人事資料卡 PDF 的薪資頁 | `payroll:read` | 整個 PAGE 3 連同 `addPage()` 一起跳過，輸出 2 頁 |
| 勞健保級距 lookup（`insurance-brackets/lookup?salary=`） | `payroll:read`（前端不訂閱） | 不發出請求 —— 該端點權限與 users 正交，留著等於開一條由底薪反推級距的側門 |
| **寫入**（`POST /users`、`PATCH /users/{id}`、`PUT /users/{id}/profile` 的薪資部分） | `payroll:read` | 靜默忽略（不回 403，其他欄位照常存檔）。薪資調整歷史為**條件式**整批替換，無權者送空陣列不會刪光既有歷史 |

**不受影響**：`GET /me/user`、`GET /me/profile`（員工看自己的薪資是既有需求）；銀行帳號 / 存摺封面 / 投保起日 / 扶養人數 / 健保眷屬名單 / 期初補休時數 / 寄送薪資表旗標。

實作單一真相：後端 [`Api/Common/PayrollFieldAccess.cs`](../../Api/Common/PayrollFieldAccess.cs)、前端 `user-form.ts` 的 `canSeeSalary` + `SALARY_CONTROLS`。規範見 [backend-design.md 欄位級權限](../backend-design.md) 與 [frontend-design.md 依權限隱藏表單區塊](../frontend-design.md)。

> ⚠️ **副作用**：`payroll:read` 同時是「人事薪資」選單與 `/admin/payroll` 月薪列表的進入權限。要讓某個角色看得到員工管理的薪資欄，就一併給了他人事薪資頁。若日後需要脫鉤，得另立 `users-salary:read` 獨立碼。
> ⚠️ **JWT 快照**：`permissions` 是登入 / refresh 當下的快照，角色權限異動後需**重新登入**才生效。

## 跨業務關聯

- **健保眷屬資料 / 上限 3 口計算** → [hr-profile.md](hr-profile.md)
- **底薪 / 伙食費 / 5 種加給自動同步**（薪資調整紀錄 → User.BaseSalary / MealAllowance / PositionAllowance / DutyAllowance / OtherAllowance / AdjustmentDifference / OverseasAllowance） → [hr-profile.md](hr-profile.md)
- **假日執行活動的歸月規則** → [application-forms.md](application-forms.md)
- **事假 / 病假 / 家庭照顧假的扣薪天數來源、銷假規則** → [leave-rules.md](leave-rules.md)
- **勞健保級距 lookup（級距表 entity）** → [database-schema.md](../database-schema.md)（`InsuranceBracket`）
- **PayrollHandler 計算邏輯實作** → [Api/Handlers/PayrollHandler.cs](../../Api/Handlers/PayrollHandler.cs)
- **PayrollReadService SQL** → [Api/Services/Dapper/PayrollReadService.cs](../../Api/Services/Dapper/PayrollReadService.cs)
