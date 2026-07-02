# 薪水計算公式（人事薪資模組）

1. **日薪** = 底薪 ÷ 30（四捨五入至整數）
2. **假日津貼** = 日薪 × 假日執行活動天數（**上個月**歸月：以已核准假日執行活動申請的 `EndDate` 所屬月份歸月，獎金計入次月薪資。例：3 月活動 → 4 月薪資；跨月活動（如 3/30~4/2）EndDate=4/2 歸 4 月 → 5 月薪資）
3. **勞保費** = `User.LaborInsuranceOverride ?? lookupBracket(底薪).EmployeeLabor`（覆寫優先；無覆寫則查級距表向上取最近級距）
4. **健保費** = `(User.HealthInsuranceOverride ?? lookupBracket(底薪).EmployeeHealth) × (1 + min(健保眷屬數, 3))`（覆寫優先；眷屬上限 3 口，第 4 口起不再加；眷屬數來自 `HealthInsuranceDependents` 表）
5. **事假扣薪** = 日薪 × 事假天數（按天數扣除全額薪資）
6. **病假扣薪** = 日薪 × 0.5 × 病假天數（按天數扣除半薪）
7. **生理假扣薪** = 日薪 × 0.5 × 生理假天數（按天數扣除半薪）
   - 限女性（`EmployeeProfile.Gender == "F"`）；每月 1 天、全年 12 天上限（見 [leave-rules.md](leave-rules.md)）。
   - **前 3 天/年（24h）為純生理假**，列「生理假扣薪」；**超過 3 天的部分併入「病假扣薪」**（兩者皆半薪，淨薪不變，差異僅在歸類）。
   - 拆分依「本年度本月之前已用生理假時數」判斷前 3 天額度是否用罄：`pureThisMonth = min(本月生理假時數, max(0, 24 − 本年度本月前生理假時數))`，餘額併入病假天數。
8. **實領薪水** = 底薪 + 伙食費 + 加班費 + 加給合計 + 假日津貼 + 其他加項 − 勞保費 − 健保費 − 事假扣薪 − 病假扣薪 − 生理假扣薪 − 其他扣項 − 勞退自提扣款
   - **加給合計** = 職務加給 + 主管加給 + 其他加給 + 調整差額 + 外派加給（5 種來自 User 表，由最新生效 SalaryAdjustmentRecord 同步而來，亦可在基本資料手動覆寫，null/未填視為 0）
9. **勞退自提扣款** = 底薪 × `User.LaborPensionSelfContributionRate`（%，0~6 整數，員工自願提撥）÷ 100（四捨五入至整數）
   - 性質同 `LaborInsuranceOverride`/`HealthInsuranceOverride`：User 表直接欄位，**不**經過 SalaryAdjustmentRecord 歷史同步，可在基本資料 Tab 直接編輯（null 視為 0%）。

> 人事薪資為動態計算，不儲存於資料庫。前端可匯出 PDF 薪資表。
> 薪資編輯頁與薪資明細信件額外顯示該月**所有已核准的請假紀錄**（全假別，非僅事假/病假）。
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

新增或修改其中一個欄位，**必須同步以下 8 處**（漏一處就會壞掉）：

1. `Api/Models/Entities/SalaryAdjustmentRecord.cs` — entity 欄位
2. `Api/Models/Entities/User.cs` — entity 欄位 + 對應 Configuration / Migration
3. `Api/Models/Dtos/UserDtos.cs` — `UserDto` / `CreateUserRequest` / `UpdateUserRequest`
4. `Api/Services/Dapper/UserReadService.cs` — 3 處 SELECT / tuple / 映射
5. `Api/Handlers/UserHandler.cs` — Create + Update 接收 form 欄位
6. `Api/Handlers/EmployeeProfileHandler.cs` — 同步邏輯 `user.Xxx = latestSalary.Xxx`
7. `Api/Services/Dapper/PayrollReadService.cs` — `employeeSql` SELECT、迴圈讀取、`netSalary` 公式、月度 `Sum`
8. `Api/Models/Dtos/PayrollDtos.cs` — `EmployeePayrollDto` + `MonthlyPayrollDto` 的 `Total*`
9. `Api/Handlers/PayrollHandler.cs` — `BuildPaySlipHtml` 應發項目區塊
10. 前端：`Admin/.../users/models/user.model.ts` + `user-form.ts` (FormGroup / patchValue / submit) + `user-form.html`
11. 前端：`Admin/.../payroll/models/payroll.model.ts` + `payroll-list.html` (summary 或 column) + `payroll-form.html` (應發項目)
12. 文件：本檔（更新 7 個欄位對照表 + 公式 + Checklist 數字）+ [hr-profile.md](hr-profile.md)（薪資自動同步條目）

---

## 跨業務關聯

- **健保眷屬資料 / 上限 3 口計算** → [hr-profile.md](hr-profile.md)
- **底薪 / 伙食費 / 5 種加給自動同步**（薪資調整紀錄 → User.BaseSalary / MealAllowance / PositionAllowance / DutyAllowance / OtherAllowance / AdjustmentDifference / OverseasAllowance） → [hr-profile.md](hr-profile.md)
- **假日執行活動的歸月規則** → [application-forms.md](application-forms.md)
- **事假 / 病假的扣薪天數來源** → [leave-rules.md](leave-rules.md)
- **勞健保級距 lookup（級距表 entity）** → [database-schema.md](../database-schema.md)（`InsuranceBracket`）
- **PayrollHandler 計算邏輯實作** → [Api/Handlers/PayrollHandler.cs](../../Api/Handlers/PayrollHandler.cs)
- **PayrollReadService SQL** → [Api/Services/Dapper/PayrollReadService.cs](../../Api/Services/Dapper/PayrollReadService.cs)
