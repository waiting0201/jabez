# 資料庫 Schema

本文件彙整 Jabez 資料庫所有實體（Entity）。EF Core Configuration 寫法、Migration 規範見 [backend-design.md §7-§8](backend-design.md#7-ef-core-configuration)。

## 資料庫名稱：`JabezDb`

本地開發連線字串於 [Api/local.settings.json](../Api/local.settings.json)；遠端 Azure SQL 連線字串記在 memory `reference_azure_sql.md`（敏感資訊不入版控）。

## 34 個資料表實體

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
| `ApprovalItem` | 簽核流程項目 |
| `ApprovalStep` | 簽核流程步驟（含 UseDirectSupervisor、UseApplicantDesignated） |
| `ApprovalRecord` | 簽核動作記錄（含 OnBehalfOfUserId 代理標記、IsEscalated 升級標記） |
| `EscalationOverride` | 升級審核指派（記錄被指派的升級/代理審核者，審核完成後清除） |
| `Project` | 專案主檔（含 **DepartmentId 必填**、ReceivedAmount 實收金額、ContractAmount 契約金額、BusinessAmount 業務執行金額） |
| `ProjectPaymentSchedule` | 專案請款期別明細（一期一筆：請款/發票/入帳日期與金額、扣款備註；扣款金額 = 發票 − 入帳，前端計算不存 DB） |
| `PaymentRequest` | 請款申請 |
| `InvoiceItem` | 請款明細（發票項目） |
| `LeaveRequest` | 請假申請（含 BereavementRelationship 喪假親屬關係） |
| `TravelRequest` | 出差預支申請（含 IsHolidayTravel、IsClosed 結案、GrandTotal 明細合計；事後走沖銷流程）。當 `IsHolidayTravel=true`（假日執行活動）時不含 Items 與發票明細，僅記錄活動地點/期間/參與人員 |
| `TravelRequestItem` | 出差預支明細（交通費、住宿費、餐費、雜支）；假日執行活動不使用 |
| `TravelPaymentRequest` | 出差請款申請（員工代墊後直接請款，無沖銷流程；含 EstimatedPaymentDate/PaidAt 撥款欄位） |
| `TravelPaymentRequestItem` | 出差請款明細（交通費、住宿費、餐費、雜支，含發票號碼、發票日期、發票檔案上傳；上傳走 multipart + Azure Blob `invoices` container，前端支援拖放、OCR 自動辨識、HEIC/PDF） |
| `OvertimeRequest` | 加班申請（走簽核流程） |
| `AdvanceRequest` | 預支申請 |
| `AdvanceRequestItem` | 預支明細 |
| `WriteOffRecord` | 預支沖銷申請（獨立簽核流程，關聯 AdvanceRequest，含 ApprovalStatus/CurrentStepOrder） |
| `WriteOffItem` | 沖銷明細（含發票號碼、檔案上傳） |
| `TravelWriteOffRecord` | 出差預支沖銷申請（獨立簽核流程，關聯 TravelRequest） |
| `TravelWriteOffItem` | 出差預支沖銷明細（含發票號碼、檔案上傳） |
| `RequestDesignatedReviewer` | 申請人指定審核者清單（多人依序審核） |
| `AttendanceRecord` | 出勤打卡紀錄（每人每天一筆，含 GPS） |
| `AttendanceReminderLog` | 打卡提醒推播紀錄（BatchId 串聯同一次 tick；含 batchStart 紀錄、ErrorCategory 失敗分類、HttpStatusCode、DurationMs；Snapshot 欄位保留歷史） |
| `SystemSetting` | 系統設定 |
| `InsuranceBracket` | 勞健保級距（投保級距、員工負擔勞保、員工負擔健保） |
| `EmployeeProfile` | 員工人事資料卡 1:1 對 User（PK=UserId）；含員工代號 / 英文名 / 身分證號 / 性別 / 婚姻 / 出生地 / 行動電話 / 戶籍 / 通訊 / 緊急聯絡 / 銀行帳號 / 投保起日 / 扶養人 / 專長興趣 / 離職原因 / 身分證正反面影本 |
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
