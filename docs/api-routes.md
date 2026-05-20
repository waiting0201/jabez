# API 路由清單

本文件彙整 Jabez API 所有 HTTP 端點的路徑與用途。技術規範（路由分派機制、權限表寫法、輕量端點模式）見 [backend-design.md §3](backend-design.md#3-路由分派設計) 與 [§13](backend-design.md#13-輕量讀取端點模式lightweight-lookup-pattern)。業務含義詳見 [docs/business/](business/) 對應檔案。

---

## 公開路由（不需 JWT）

| Method | Path | 說明 |
|--------|------|------|
| GET | `/health` | 健康檢查 |
| POST | `/auth/login` | 登入取得 JWT |
| POST | `/auth/refresh` | 刷新 Token |

## 認證（需 JWT）

| Method | Path | 說明 |
|--------|------|------|
| POST | `/auth/change-password` | 已登入使用者修改密碼（驗證舊密碼後更新，並清除 `MustChangePassword` 旗標） |

## 使用者管理

| Method | Path | 說明 |
|--------|------|------|
| GET | `/users` | 取得使用者列表 |
| POST | `/users` | 新增使用者 |
| GET | `/users/{id}` | 取得單一使用者 |
| PUT/PATCH | `/users/{id}` | 更新使用者 |
| DELETE | `/users/{id}` | 刪除使用者 |
| POST | `/users/{id}/send-credentials` | 寄送帳號通知信並設置 `MustChangePassword = true`（預設密碼為生日 yyyyMMdd） |

## 角色與權限（僅 Superadmin）

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/roles` | 角色列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/roles/{id}` | 角色 CRUD |
| GET/POST | `/permissions` | 權限列表 / 新增 |
| GET/PUT/DELETE | `/permissions/{id}` | 權限 CRUD |

## 部門與職稱

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/departments` | 部門列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/departments/{id}` | 部門 CRUD |
| GET/POST | `/job-titles` | 職稱列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/job-titles/{id}` | 職稱 CRUD |

## 廠商管理

| Method | Path | 說明 |
|--------|------|------|
| GET | `/vendors/lookup` | **輕量端點**：免 `vendors:read` 權限，僅回 `IsActive=true` 的 `{id, name, taxId}`，供請款申請下拉清單 |
| GET | `/vendors/lookup-by-tax-id?taxId=XXXXXXXX` | **輕量端點**：以統編查 GCIS 公司登記資料，回 `{taxId, name, address, contactPerson}`，免 `vendors:read`，僅需登入 |
| GET | `/vendors` | 廠商列表（含使用筆數，需 `vendors:read`） |
| POST | `/vendors` | 新增廠商（**multipart**：text part `payload` JSON + optional file part `bankBookImage` 存摺封面；**任何登入者皆可，無需權限**） |
| GET | `/vendors/{id}` | 取得廠商（需 `vendors:read`，回應含 `bankBookImageUrl`） |
| PUT/PATCH | `/vendors/{id}` | 更新廠商（**multipart**：text part `payload` + optional `bankBookImage` file / `removeBankBookImage` text flag；需 `vendors:write`） |
| DELETE | `/vendors/{id}` | 刪除廠商（需 `vendors:delete`；若已被請款單引用會回 400，須改用停用；連同存摺封面 blob 一併刪除） |
| GET | `/files/vendor-passbooks/{fileName}` | 廠商存摺封面代理（需 JWT，免特殊權限，與 avatars/signatures 同層的一般檔案） |

## 簽核流程

| Method | Path | 說明 |
|--------|------|------|
| GET | `/approval-items/active?type=<applicationType>` | **輕量摘要：免 `approvals:read` 權限**，回傳該類型啟用流程 `{id, applicationType, steps:[{stepOrder, useApplicantDesignated}]}`，供申請表單判斷是否顯示「指定審核者」欄位 |
| GET/POST | `/approval-items` | 簽核項目列表 / 新增（需 `approvals:read` / `approvals:write`） |
| GET/PUT/PATCH/DELETE | `/approval-items/{id}` | 簽核項目 CRUD |
| POST | `/approval-items/{id}/steps` | 新增簽核步驟 |
| PUT/PATCH | `/approval-items/{id}/steps/{stepId}` | 更新簽核步驟 |
| DELETE | `/approval-items/{id}/steps/{stepId}` | 刪除簽核步驟 |

## 審核任務

| Method | Path | 說明 |
|--------|------|------|
| GET | `/approval-tasks` | 待審核任務列表 |
| GET | `/approval-tasks/{id}` | 取得任務詳情 |
| PATCH | `/approval-tasks/{appType}/{id}/review` | 審核（核准 / 退回） |
| POST | `/approval-tasks/batch-approve` | 批次核准多筆待審申請（僅 approved 動作，需 `approval-tasks:batch-approve` 權限；撥款/退款日留空，完成後以提醒清單回傳需補填者） |

## 專案管理

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/projects` | 專案列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/projects/{id}` | 專案 CRUD |

## 請款 / 請假 / 出差 / 加班 / 預支申請

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/payment-requests` | 請款列表 / 新增（預設 draft，multipart 含 `vendorId` — 當 `type=vendor` 時必填且必須是 IsActive=true 的廠商） |
| GET/PUT/PATCH/DELETE | `/payment-requests/{id}` | 請款 CRUD（DTO 含 `vendorId / vendorName / vendorTaxId`） |
| PATCH | `/payment-requests/{id}/submit` | 送出請款申請（draft → pending） |
| PATCH | `/payment-requests/{id}/installments` | upsert 一或多筆撥款明細（SUM 嚴格驗證 = TotalAmount；已撥款列鎖定不可改不可刪；每筆 PaidAt null→value 觸發一次「已撥款」通知含 N/M 期；僅財務體系部門：AC/FIN/Jabez HQ/CEO） |
| GET/POST | `/leave-requests` | 請假列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/leave-requests/{id}` | 請假 CRUD |
| PATCH | `/leave-requests/{id}/submit` | 送出請假申請（draft → pending） |
| GET | `/leave-requests/compensatory-hours` | 查詢可補休時數（總加班 − 已補休） |
| GET | `/leave-requests/annual-quota` | 查詢年假額度（依 HireDate 計算年資） |
| GET | `/leave-requests/ceremonial-quota` | 查詢歲時祭儀假額度（僅原住民，每年 3 天，跨年歸零） |
| GET | `/leave-requests/marriage-quota` | 查詢婚假配額（上限 8 天，不限年度） |
| GET | `/leave-requests/maternity-status` | 查詢產假狀態（是否已有活躍申請） |
| GET | `/leave-requests/bereavement-quota?relationship={rel}` | 查詢喪假配額（依親屬關係 3/6/8 天） |
| GET | `/leave-requests/senior-executive-eligibility` | 查詢高階主管假適用性（JobTitle.Level ≤ 3） |
| GET/POST | `/travel-requests` | 出差預支申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-requests/{id}` | 出差預支申請 CRUD |
| PATCH | `/travel-requests/{id}/submit` | 送出出差預支申請（draft → pending） |
| GET/POST | `/travel-payment-requests` | 出差請款申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-payment-requests/{id}` | 出差請款申請 CRUD |
| PATCH | `/travel-payment-requests/{id}/submit` | 送出出差請款申請（draft → pending） |
| PATCH | `/travel-payment-requests/{id}/installments` | upsert 分期撥款（同 PaymentRequest 行為） |
| GET | `/holiday-travel-requests` | 假日執行活動申請列表（共用 TravelRequest，`IsHolidayTravel=true`） |
| POST | `/holiday-travel-requests` | 新增假日執行活動申請（預設 draft，無 Items 與發票明細） |
| GET/PUT/PATCH/DELETE | `/holiday-travel-requests/{id}` | 假日執行活動申請 CRUD |
| PATCH | `/holiday-travel-requests/{id}/submit` | 送出假日執行活動申請（draft → pending） |
| PATCH | `/holiday-travel-requests/{id}/installments` | upsert 分期撥款（同 PaymentRequest 行為） |
| GET | `/holiday-travel-requests/count-holidays?startDate=...&endDate=...` | 計算指定區間內的假日天數（用於計算假日津貼） |
| GET/POST | `/overtime-requests` | 加班申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/overtime-requests/{id}` | 加班申請 CRUD |
| PATCH | `/overtime-requests/{id}/submit` | 送出加班申請（draft → pending） |
| GET/POST | `/advance-requests` | 預支申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/advance-requests/{id}` | 預支申請 CRUD |
| PATCH | `/advance-requests/{id}/submit` | 送出預支申請（draft → pending） |
| PATCH | `/advance-requests/{id}/installments` | upsert 分期撥款（同 PaymentRequest 行為） |
| PATCH | `/travel-requests/{id}/installments` | upsert 分期撥款（同 PaymentRequest 行為） |

## 預支沖銷申請

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/write-off-requests` | 預支沖銷申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/write-off-requests/{id}` | 預支沖銷申請 CRUD |
| PATCH | `/write-off-requests/{id}/submit` | 送出預支沖銷申請（draft → pending） |

## 出差預支沖銷申請

| Method | Path | 說明 |
|--------|------|------|
| GET | `/travel-write-off-requests/available-travels` | 可沖銷的出差預支申請清單 |
| GET/POST | `/travel-write-off-requests` | 出差預支沖銷申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-write-off-requests/{id}` | 出差預支沖銷申請 CRUD |
| PATCH | `/travel-write-off-requests/{id}/submit` | 送出出差預支沖銷申請（draft → pending） |

## 出勤打卡

| Method | Path | 說明 |
|--------|------|------|
| GET | `/attendances` | 出勤紀錄列表（分頁，套用部門可見性 scope；支援 `?dateFrom=YYYY-MM-DD&dateTo=YYYY-MM-DD` 區間篩選，前端依「日 / 週 / 月」模式換算） |
| GET | `/attendances/today` | 今日打卡紀錄（當前使用者；含 `todayLeaves` 陣列：當日所有已核准請假時段，供前端顯示提示與 disable 按鈕；無打卡紀錄時回傳 `Id=0` 空殼仍含請假資訊） |
| POST | `/attendances/clock-in` | 上班打卡（含 GPS；落在已核准請假 `[StartDate, EndDate)` 區間內會回 BadRequest） |
| POST | `/attendances/clock-out` | 下班打卡（含 GPS；同上規則） |
| POST | `/attendances/overtime-start` | 加班開始打卡（需核准的加班申請；不受請假時段阻擋） |
| POST | `/attendances/overtime-end` | 加班結束打卡（不受請假時段阻擋） |

> **請假時段阻擋規則**：上下班打卡以 `Clock.Now`（Asia/Taipei）比對員工 `LeaveRequests` 中 `ApprovalStatus='approved'` 的紀錄，落在 `StartDate <= now < EndDate` 半開區間內即阻擋並回含請假單編號 / 假別 / 時段的錯誤訊息。半天 / 小時請假時段已編碼於 datetime，時段外仍可打卡（如上午半天請假，下午可打上班卡；09:00–12:00 病假，12:00 整點可打卡）。加班打卡不套用此規則。實作於 [Api/Handlers/AttendanceHandler.cs](../Api/Handlers/AttendanceHandler.cs) `EnsureNotOnLeaveAsync`，Dapper SQL 於 [Api/Services/Dapper/AttendanceReadService.cs](../Api/Services/Dapper/AttendanceReadService.cs) `GetActiveLeaveAtAsync`。

## 報表（Reports）

三個報表（出缺勤、加班、請款）共用「日 / 週 / 月」三選一時段模式。前端 segmented control 切換模式後，依使用者輸入計算 `dateFrom` / `dateTo`（皆 `YYYY-MM-DD`，inclusive）送出；後端統一接 `dateFrom` / `dateTo`（取代舊有的 `year` / `month`）。週為 ISO 8601（週一→週日），共用工具於 [Admin/src/app/features/admin/reports/utils/date-range.ts](../Admin/src/app/features/admin/reports/utils/date-range.ts)。

| Method | Path | 說明 |
|--------|------|------|
| GET | `/attendances` | 出缺勤紀錄列表（共用上方出勤打卡端點，篩選參數：`employeeId / dateFrom / dateTo`） |
| GET | `/reports/overtime` | 加班紀錄報表（已核准的加班申請 + 實際打卡時數，篩選參數：`employeeId / projectId / dateFrom / dateTo`） |
| GET | `/reports/payment` | 款項統計報表（已送出的請款申請，篩選參數：`dateFrom / dateTo / paymentStatus`；`pr.CreatedAt` 為 DATETIME，`dateTo` 用 `< DATEADD(day, 1, @DateTo)` 半開區間涵蓋當日 23:59:59） |
| GET | `/reports/payment/export` | 款項統計匯出（不分頁、**一張發票一列**：`LEFT JOIN InvoiceItems`，無發票的請款仍輸出 1 列；篩選參數同上；權限同 `/reports/payment`） |

## 打卡提醒（手動觸發 + 紀錄查詢，僅 Superadmin）

| Method | Path | 說明 |
|--------|------|------|
| POST | `/admin/attendance-reminder/run?type=clockIn\|clockOut` | 繞過時點與週末檢查，強制對符合條件的員工推播 LINE 打卡提醒（除錯用），回傳 `recipientCount/pushedCount/failureCount/batchId` |
| GET | `/admin/attendance-reminder-logs` | 推播紀錄列表（分頁 + 篩選：日期區間、提醒類型、結果、失敗原因、員工、觸發來源） |
| GET | `/admin/attendance-reminder-logs/stats` | 統計卡資料（今日推播數 / 失敗數 / 批次 tick 數 + 最近 7 天趨勢） |
| GET | `/admin/attendance-reminder-logs/batches/{batchId}` | 同一批次（同一次 tick）所有紀錄，含 batchStart |
| GET | `/admin/attendance-reminder-logs/{id}` | 單筆紀錄詳情 |
| POST | `/admin/payment-reminder/run` | 手動觸發撥款日將屆提醒（除錯用，回傳 `batchId/upcomingItemCount/financeUserCount/successCount/skippedAlreadySent/failureCount`） |
| GET | `/admin/payment-reminder-logs` | 撥款提醒推播紀錄列表（分頁 + 篩選：日期區間、結果、觸發來源、財務人員）|

> 自動排程：
> - `AttendanceReminderFunction`（TimerTrigger）執行打卡提醒，cron 由 `AttendanceReminderCron` 控制
> - `PaymentReminderFunction`（TimerTrigger）每日 09:00 (Taipei) 執行撥款日將屆提醒，cron 由 `PaymentReminderCron` 控制；提前天數由 `SystemSetting.PaymentReminderDaysBefore` 控制（預設 3 天）；推播給財務體系部門（AC/FIN/Jabez HQ/CEO）全員，沿用 `ApprovalEmailEnabled` + `ApprovalLineEnabled` 開關
> 所有 GET 紀錄查詢端點透過 `AppRouter.IsSuperAdminRoute` 守門，僅 Superadmin 可見。

## 勞健保級距

| Method | Path | 說明 |
|--------|------|------|
| GET | `/insurance-brackets` | 級距列表 |
| GET | `/insurance-brackets/lookup?salary=xxx` | 根據薪資查詢對應級距（向上取最近級距） |
| POST | `/insurance-brackets` | 新增級距 |
| GET | `/insurance-brackets/{id}` | 取得單筆級距 |
| PUT/PATCH | `/insurance-brackets/{id}` | 更新級距 |
| DELETE | `/insurance-brackets/{id}` | 刪除級距 |

## 人事薪資

| Method | Path | 說明 |
|--------|------|------|
| GET | `/payroll?year=YYYY&month=MM` | 月薪計算（動態計算，不存 DB） |

## LINE 綁定 / 推播用量

| Method | Path | 說明 |
|--------|------|------|
| GET | `/line/bind-url` | 產生 LINE OAuth URL（含 state 防 CSRF） |
| POST | `/line/bind` | 用 OAuth code 換取 LINE userId 並綁定 |
| POST | `/line/unbind` | 解除 LINE 綁定 |
| GET | `/line/binding-status` | 查詢當前用戶 LINE 綁定狀態 |
| GET | `/line/quota` | 查詢 LINE Messaging API 月度推播用量（`type` / `limit` / `used` / `remaining`），需 `line-quota:read` 權限；Dashboard「LINE 推播用量」卡片使用 |

## 檔案代理（Blob Storage）

| Method | Path | 說明 |
|--------|------|------|
| GET | `/files/signatures/{fileName}` | 簽名檔代理（公開，PDF 匯出用） |
| GET | `/files/avatars/{fileName}` | 頭像代理（公開，topbar 顯示用） |
| GET | `/files/indigenous-proofs/{fileName}` | 原住民證明文件代理（需 `users:read`，HR 敏感 PII） |
| GET | `/files/low-income-proofs/{fileName}` | 低收入證明文件代理（需 `users:read`，HR 敏感 PII） |
| GET | `/files/disabled-proofs/{fileName}` | 殘障證明文件代理（需 `users:read`，HR 敏感 PII） |
| GET | `/files/id-cards/{fileName}` | 身分證影本代理（需 `users:read`，HR 敏感 PII） |
| GET | `/files/education-proofs/{fileName}` | 最高學歷證明代理（需 `users:read`，HR 敏感 PII） |

## 員工人事資料卡（HR Profile）

| Method | Path | 說明 |
|--------|------|------|
| GET | `/users/{id}/profile` | 取得員工人事資料卡（EmployeeProfile + 9 張子表）。Profile 不存在時回傳預設空殼 |
| PUT | `/users/{id}/profile` | 整批更新員工人事資料卡（multipart：`payload` JSON + `idCardFront` / `idCardBack` 檔案 + `removeIdCardFront` / `removeIdCardBack` 旗標）。9 張子表整批替換；薪資調整紀錄會自動同步「最新生效底薪」回 `User.BaseSalary` |

## 其他

| Method | Path | 說明 |
|--------|------|------|
| GET | `/settings` | 取得系統設定 |
| PATCH | `/settings` | 更新系統設定 |

---

## 跨業務關聯

- 申請類路由業務含義 → [docs/business/application-forms.md](business/application-forms.md)
- 簽核 / 審核任務業務 → [docs/business/approval-flow.md](business/approval-flow.md)
- 請假各 quota 端點業務 → [docs/business/leave-rules.md](business/leave-rules.md)
- 薪資 / 健保眷屬 → [docs/business/payroll-formula.md](business/payroll-formula.md) / [docs/business/hr-profile.md](business/hr-profile.md)
- LINE 綁定流程 → [docs/business/line-integration.md](business/line-integration.md)
- 打卡提醒排程 → [docs/business/attendance-reminder.md](business/attendance-reminder.md)
- 部門可見性影響的端點清單 → [docs/business/department-visibility.md](business/department-visibility.md)
- 輕量端點模式（`/users/lookup`、`/projects/active`、`/approval-items/active` 等）→ [backend-design.md §13](backend-design.md#13-輕量讀取端點模式lightweight-lookup-pattern)
