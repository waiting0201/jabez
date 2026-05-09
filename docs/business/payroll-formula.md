# 薪水計算公式（人事薪資模組）

1. **日薪** = 底薪 ÷ 30（四捨五入至整數）
2. **假日津貼** = 日薪 × 假日執行活動天數（**上個月**歸月：以已核准假日執行活動申請的 `EndDate` 所屬月份歸月，獎金計入次月薪資。例：3 月活動 → 4 月薪資；跨月活動（如 3/30~4/2）EndDate=4/2 歸 4 月 → 5 月薪資）
3. **勞保費** = `User.LaborInsuranceOverride ?? lookupBracket(底薪).EmployeeLabor`（覆寫優先；無覆寫則查級距表向上取最近級距）
4. **健保費** = `(User.HealthInsuranceOverride ?? lookupBracket(底薪).EmployeeHealth) × (1 + min(健保眷屬數, 3))`（覆寫優先；眷屬上限 3 口，第 4 口起不再加；眷屬數來自 `HealthInsuranceDependents` 表）
5. **事假扣薪** = 日薪 × 事假天數（按天數扣除全額薪資）
6. **病假扣薪** = 日薪 × 0.5 × 病假天數（按天數扣除半薪）
7. **實領薪水** = 底薪 + 假日津貼 - 勞保費 - 健保費 - 事假扣薪 - 病假扣薪

> 人事薪資為動態計算，不儲存於資料庫。前端可匯出 PDF 薪資表。
> 薪資編輯頁與薪資明細信件額外顯示該月**所有已核准的請假紀錄**（全假別，非僅事假/病假）。
> 健保費若眷屬數 ≥ 1，PDF 與信件會在金額右側補註腳「（含健保眷屬 N 口）」。

---

## 跨業務關聯

- **健保眷屬資料 / 上限 3 口計算** → [hr-profile.md](hr-profile.md)
- **底薪自動同步**（薪資調整紀錄 → User.BaseSalary） → [hr-profile.md](hr-profile.md)
- **假日執行活動的歸月規則** → [application-forms.md](application-forms.md)
- **事假 / 病假的扣薪天數來源** → [leave-rules.md](leave-rules.md)
- **勞健保級距 lookup（級距表 entity）** → [database-schema.md](../database-schema.md)（`InsuranceBracket`）
- **PayrollHandler 計算邏輯實作** → [Api/Handlers/PayrollHandler.cs](../../Api/Handlers/PayrollHandler.cs)
- **PayrollReadService SQL** → [Api/Services/Dapper/PayrollReadService.cs](../../Api/Services/Dapper/PayrollReadService.cs)
