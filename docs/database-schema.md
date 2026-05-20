# 資料庫 Schema

本文件彙整 Jabez 資料庫所有實體（Entity）。EF Core Configuration 寫法、Migration 規範見 [backend-design.md §7-§8](backend-design.md#7-ef-core-configuration)。

## 資料庫名稱：`JabezDb`

本地開發連線字串於 [Api/local.settings.json](../Api/local.settings.json)；遠端 Azure SQL 連線字串記在 memory `reference_azure_sql.md`（敏感資訊不入版控）。

## 40 個資料表實體

| 實體 | 說明 |
|------|------|
| `User` | 使用者（含 DepartmentId、JobTitleId、IsSuperAdmin、LineUserId、IsIndigenous / IsLowIncome / IsDisabled 三個身份旗標、Avatar、SignatureUrl、IndigenousProofUrl / LowIncomeProofUrl / DisabledProofUrl 三個證明檔、HealthInsuranceOverride / LaborInsuranceOverride 兩個健保 / 勞保覆寫值） |
| `Role` | 角色定義 |
| `Permission` | 權限代碼 |
| `UserRole` | 使用者 ↔ 角色（Junction） |
| `RolePermission` | 角色 ↔ 權限（Junction） |
| `RefreshToken` | Refresh Token 儲存 |
| `Department` | 部門主檔（含 ParentId 階層、**CanSeeAll / CanViewSiblings / CanViewDescendants / CanViewParent 四個可見性旗標**） |
| `JobTitle` | 職稱主檔 |
| `Vendor` | 廠商主檔（Name、TaxId 統編 unique-filter index、Phone、ContactPerson、Address、BankAccount、BankBookImageUrl 存摺封面 proxy 路徑、Note、IsActive、CreatedAt；被 PaymentRequest.VendorId 引用，FK OnDelete=Restrict 限引用中不可刪） |
| `ApprovalItem` | 簽核流程項目 |
| `ApprovalStep` | 簽核流程步驟（含 UseDirectSupervisor、UseApplicantDesignated） |
| `ApprovalRecord` | 簽核動作記錄（含 OnBehalfOfUserId 代理標記、IsEscalated 升級標記） |
| `EscalationOverride` | 升級審核指派（記錄被指派的升級/代理審核者，審核完成後清除） |
| `Project` | 專案主檔（含 **DepartmentId 必填**、ContractAmount 契約金額、BusinessAmount 業務執行金額、RemainingAmount 剩餘金額（系統導入時剩餘預算，選填）；實收金額為衍生值，由 `SUM(ProjectPaymentSchedules.DepositAmount)` 即時計算） |
| `ProjectPaymentSchedule` | 專案請款期別明細（一期一筆：請款/發票/入帳日期與金額、扣款備註；扣款金額 = 發票 − 入帳，前端計算不存 DB） |
| `PaymentRequest` | 請款申請（含 `RequestNo` 單號 `PR-yyyyMMdd-NNN` unique index、`VendorId` nullable FK：當 Type=`vendor` 時必填且必須是 IsActive=true 的廠商；其他類型強制為 null；撥款資料統一由 `PaymentRequestInstallment[]` 表達，父表無 cache 欄位） |
| `InvoiceItem` | 請款明細（發票項目） |
| `LeaveRequest` | 請假申請（含 BereavementRelationship 喪假親屬關係） |
| `TravelRequest` | 出差預支申請（含 `RequestNo` 單號 unique index：`IsHolidayTravel=false` → `TR-yyyyMMdd-NNN`、`IsHolidayTravel=true` → `HTR-yyyyMMdd-NNN`，per-prefix-per-day 序號池；含 IsHolidayTravel、IsClosed 結案、GrandTotal 明細合計、`EstimatedRefundDate / RefundedAt / RefundedByUserId` 退款欄位（沖銷超支才用）；撥款資料統一由 `TravelRequestInstallment[]` 表達，父表無撥款 cache 欄位；事後走沖銷流程）。當 `IsHolidayTravel=true`（假日執行活動）時不含 Items 與發票明細，僅記錄活動地點/期間/參與人員 |
| `TravelRequestItem` | 出差預支明細（交通費、住宿費、餐費、雜支）；假日執行活動不使用 |
| `TravelPaymentRequest` | 出差請款申請（含 `RequestNo` 單號 `TPR-yyyyMMdd-NNN` unique index；員工代墊後直接請款，無沖銷流程；撥款資料統一由 `TravelPaymentRequestInstallment[]` 表達，父表無 cache 欄位） |
| `TravelPaymentRequestItem` | 出差請款明細（交通費、住宿費、餐費、雜支，含發票號碼、發票日期、發票檔案上傳；上傳走 multipart + Azure Blob `invoices` container，前端支援拖放、OCR 自動辨識、HEIC/PDF） |
| `OvertimeRequest` | 加班申請（走簽核流程） |
| `AdvanceRequest` | 預支申請（含 `EstimatedRefundDate / RefundedAt / RefundedByUserId` 退款欄位、`RefundAmount / RefundedAmount` 退款金額；撥款資料統一由 `AdvanceRequestInstallment[]` 表達，父表無撥款 cache 欄位） |
| `AdvanceRequestItem` | 預支明細 |
| `WriteOffRecord` | 預支沖銷申請（獨立簽核流程，關聯 AdvanceRequest，含 ApprovalStatus/CurrentStepOrder） |
| `WriteOffItem` | 沖銷明細（含發票號碼、檔案上傳） |
| `TravelWriteOffRecord` | 出差預支沖銷申請（獨立簽核流程，關聯 TravelRequest） |
| `TravelWriteOffItem` | 出差預支沖銷明細（含發票號碼、檔案上傳） |
| `RequestDesignatedReviewer` | 申請人指定審核者清單（多人依序審核） |
| `AttendanceRecord` | 出勤打卡紀錄（每人每天一筆，含 GPS） |
| `AttendanceReminderLog` | 打卡提醒推播紀錄（BatchId 串聯同一次 tick；含 batchStart 紀錄、ErrorCategory 失敗分類、HttpStatusCode、DurationMs；Snapshot 欄位保留歷史） |
| `PaymentReminderLog` | 撥款日將屆提醒推播紀錄（BatchId 串聯同一次 tick；TriggerSource auto/manual；ReminderDateTaipei 用於同日去重；Status: success/failure/batchStart/skipped_already_sent；FinanceUserId 推播對象） |
| `SystemSetting` | 系統設定（含站台 / 工時 / 通知 / 撥款提醒）。`ApprovalEmailEnabled` / `ApprovalLineEnabled` 控制全域簽核通知開關（不影響帳號通知 / 薪資明細 / 打卡提醒）。`PaymentReminderDaysBefore` 控制撥款日將屆提醒提前天數（預設 3 天，0-30） |
| `PaymentRequestInstallment` | 請款撥款明細（多筆，與父表 1:N）：`InstallmentNo` 1-based 連續、`ExpectedDate` 預計撥款日（必填）、`PaidAt` 實際撥款日（null = 未撥）、`Amount` 金額（>= 1）、`Note` 備註、`PaidByUserId` 撥款人 FK→Users。**驗證**（InstallmentValidator）：序號連續無斷號、`SUM(Amount) == 父表 TotalAmount`（容忍 0.01）、已撥款列保護（PaidAt 有值時 ExpectedDate/Amount/PaidAt 不可改、不可刪）。每筆 PaidAt null→value 觸發一次「已撥款」通知（含 N/M 期） |
| `AdvanceRequestInstallment` | 預支撥款明細（同上結構，FK→AdvanceRequest，SUM 對應父表 GrandTotal）|
| `TravelRequestInstallment` | 出差預支撥款明細（同上結構，FK→TravelRequest，SUM 對應父表 GrandTotal）|
| `TravelPaymentRequestInstallment` | 出差請款撥款明細（同上結構，FK→TravelPaymentRequest，SUM 對應父表 GrandTotal）|
| `InsuranceBracket` | 勞健保級距（投保級距、員工負擔勞保、員工負擔健保） |
| `EmployeeProfile` | 員工人事資料卡 1:1 對 User（PK=UserId）；含員工代號 / 英文名 / 身分證號 / 性別 / 婚姻 / 出生地 / 行動電話 / 戶籍 / 通訊 / 緊急聯絡 / 銀行帳號 / 投保起日 / 扶養人 / 專長興趣 / 離職原因 / 身分證正反面影本 / 最高學歷證明 URL |
| `EducationRecord` | 學歷紀錄（最高 / 次之 / 次之，校名 / 科系 / 畢肄業 / 起迄） |
| `EmploymentHistoryRecord` | 經歷紀錄（最近 / 次之 / 次之，服務機構 / 職別 / 任職起迄） |
| `FamilyMember` | 家庭成員（親屬姓名 / 關係 / 年齡 / 職業） |
| `ProfessionalTraining` | 專業訓練（訓練名稱 / 單位 / 起迄 / 時數） |
| `LanguageAbility` | 語言能力（語言 / 聽 / 說 / 讀 / 寫，皆 good 或 fair） |
| `JobTransferRecord` | 職務調整歷史（生效日 / 原單位 / 轉調單位 / 原職務 / 轉調職務，皆字串 snapshot） |
| `RewardPunishmentRecord` | 獎懲歷史（生效日 / 類別 reward 或 punishment / 細項 / 次數 / 原由） |
| `SalaryAdjustmentRecord` | 薪資調整歷史（生效日 / 底薪 / 職位加給 / 職務加給 / 其他加給 / 調整差額 / 駐外津貼 / 伙食津貼 / 合計 / 備註）。**儲存後會把最新生效底薪自動同步至 `User.BaseSalary`** |
| `HealthInsuranceDependent` | 健保眷屬（姓名 / 關係 / 身分證號 / 出生日期）。每位眷屬會在薪資公式中按一份員工健保費計算，最多計 3 口 |

---

## 跨業務關聯

- **Entity Configuration / Migration 規範** → [backend-design.md §7-§8](backend-design.md#7-ef-core-configuration)
- **User 認證相關欄位（IsSuperAdmin / RefreshToken）** → [authentication.md](authentication.md)
- **申請類 entity 業務含義** → [docs/business/application-forms.md](business/application-forms.md)
- **Department 可見性旗標** → [docs/business/department-visibility.md](business/department-visibility.md)
- **ApprovalItem / Step / Record / Override / RequestDesignatedReviewer** → [docs/business/approval-flow.md](business/approval-flow.md) + [docs/business/approval-escalation.md](business/approval-escalation.md)
- **EmployeeProfile + 9 子表** → [docs/business/hr-profile.md](business/hr-profile.md)
- **HealthInsuranceDependent 影響薪資公式** → [docs/business/payroll-formula.md](business/payroll-formula.md)
- **AttendanceReminderLog** → [docs/business/attendance-reminder.md](business/attendance-reminder.md)
- **LeaveRequest 假別與規則** → [docs/business/leave-rules.md](business/leave-rules.md)
