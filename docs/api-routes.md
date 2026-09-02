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
| GET | `/users?page=1&pageSize=20&search=關鍵字&departmentId=9&status=active&hasLaborPension=true` | 取得使用者列表。`search` 選填，模糊比對**姓名** `Name`；`departmentId` 選填，篩選部門；`status` 選填，在職狀態（白名單 `active` / `inactive`，非法值忽略）；`hasLaborPension` 選填，勞退自提（`true`＝自提率 > 0、`false`＝自提率為 0 或 null，非布林值忽略，**無 `payroll:read` 者送此參數回 403**，因可反推他人自提率）（四者可併用，與分頁參數正交，非數字的 `departmentId` 一律忽略）；**帶 `page` / `pageSize` 回 `PagedResult`，不帶則回平面陣列**（`pageSize` clamp 1~100，預設 20）。**欄位級權限**：無 `payroll:read` 者的 8 個薪資欄（底薪 / 伙食費 / 加班費 / 2 種加給 / 勞健保覆寫 / 勞退自提率）回 `null` |
| GET | `/users/lookup` | **輕量端點**：免 `users:read`，回 `{id, name, jobTitleId, status, departmentId, jobTitleLevel}`，供指定審核者下拉與「部門最高層級」判定（`jobTitleLevel` 數字越小越高） |
| POST | `/users` | 新增使用者。**欄位級權限**：無 `payroll:read` 者送的薪資欄一律忽略（存為 null），回應 DTO 亦抹除 |
| GET | `/users/{id}` | 取得單一使用者。**欄位級權限**：同 `GET /users`，無 `payroll:read` 者薪資欄回 `null` |
| PUT/PATCH | `/users/{id}` | 更新使用者。**欄位級權限**：無 `payroll:read` 者的薪資欄寫入一律忽略（既有值不變，不回 403），回應 DTO 亦抹除 |
| DELETE | `/users/{id}` | 刪除使用者 |
| POST | `/users/{id}/send-credentials` | 寄送帳號通知信並設置 `MustChangePassword = true`（預設密碼為生日 yyyyMMdd） |

## 角色與權限

| Method | Path | 權限 | 說明 |
|--------|------|------|------|
| GET | `/roles` | `roles:read` | 角色列表 |
| POST | `/roles` | `roles:write` | 新增角色 |
| GET | `/roles/{id}` | `roles:read` | 取得單一角色 |
| PUT/PATCH | `/roles/{id}` | `roles:write` | 更新角色 |
| DELETE | `/roles/{id}` | `roles:delete` | 刪除角色 |
| GET | `/permissions` | **登入即可** | 權限**列表**。⚠️ 刻意不鎖 Superadmin —— 角色編輯頁（`roles:write`）要靠它建權限勾選清單，鎖了會讓有 `roles:write` 的管理員無法編輯角色。非 Superadmin 看不到「權限管理」模組是由前端 role-form 過濾 |
| POST | `/permissions` | **Superadmin** | 新增權限 |
| GET/PUT/PATCH/DELETE | `/permissions/{id}` | **Superadmin** | 權限單筆 CRUD（單筆讀取＝權限編輯頁專用，該頁本就 Superadmin-only） |

> 前端 `/admin/permissions` 三條路由以 `permission: 'superadmin'` 的 route guard 擋 UI；**API 端自 2026-08 起亦由 `AppRouter.IsSuperAdminRoute` 強制**（在此之前只有前端擋，任何登入者都能呼叫 API 增刪權限）。

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
| GET | `/vendors/lookup` | **輕量端點**：免 `vendors:read` 權限，僅回 `IsActive=true` 的 `{id, name, taxId, idNumber}`，供請款申請下拉清單 |
| GET | `/vendors/lookup-by-tax-id?taxId=XXXXXXXX` | **輕量端點**：以統編查 GCIS 公司登記資料，回 `{taxId, name, address, contactPerson}`，免 `vendors:read`，僅需登入 |
| GET | `/vendors?page=1&pageSize=20&search=關鍵字` | 廠商列表（含使用筆數，需 `vendors:read`）；`search` 選填，模糊比對 `Name` / `TaxId` / `IdNumber` / `ContactPerson` / `Phone` / `BankAccountName`（匯款戶名常與廠商名稱不同，須可反查）；**帶 `page` / `pageSize` 回 `PagedResult`，不帶則回平面陣列**（`pageSize` clamp 1~100，預設 20） |
| POST | `/vendors` | 新增廠商（**multipart**：text part `payload` JSON + 檔案 `bankBookImage`（**必填**）/ `idCardFront` / `idCardBack`；統編與身分證字號擇一，填身分證字號時正反面必傳；**任何登入者皆可，無需權限**） |
| GET | `/vendors/{id}` | 取得廠商（需 `vendors:read`，回應含 `idNumber` / 匯款四欄 `bankAccountName` / `bankName` / `bankCode` / `bankAccount` / `bankBookImageUrl` / `idCardFrontUrl` / `idCardBackUrl`） |
| PUT/PATCH | `/vendors/{id}` | 更新廠商（**multipart**：text part `payload` + 檔案 `bankBookImage` / `idCardFront` / `idCardBack` 與 remove 旗標 `removeBankBookImage` / `removeIdCardFront` / `removeIdCardBack`；存摺封面為必填、身分證字號廠商須備齊正反面；需 `vendors:write`） |
| DELETE | `/vendors/{id}` | 刪除廠商（需 `vendors:delete`；若已被請款單引用會回 400，須改用停用；連同存摺封面與身分證影本 blob 一併刪除） |
| GET | `/files/vendor-passbooks/{fileName}` | 廠商存摺封面代理（需 JWT，免特殊權限，與 avatars/signatures 同層的一般檔案） |
| GET | `/files/vendor-id-cards/{fileName}` | 廠商身分證影本代理（需 JWT + `vendors:read`，屬敏感 PII） |

## 簽核流程

| Method | Path | 說明 |
|--------|------|------|
| GET | `/approval-items/active?type=<applicationType>` | **輕量摘要：免 `approvals:read` 權限**，回傳「**呼叫者部門實際會走**」的啟用流程，型別為精簡版 `ApprovalFlowSummaryDto { id, applicationType, steps:[{stepOrder, useApplicantDesignated, designatedRequiresDepartment, designatedJobTitleIds}] }`（**刻意不含** `departmentId` / `departmentName` / `jobTitle` 等敏感設定欄位，避免未授權呼叫者探知流程內部；部門專屬優先，否則退回通用預設；部門由 JWT `department_id` 解析），供申請表單判斷是否顯示「指定審核者」欄位。**`useApplicantDesignated` 為「對呼叫者而言的有效值」**：步驟原生設定 **OR** 例外指定審核名單（`ApprovalStepExceptions`）命中呼叫者，使用者由 JWT `sub` 解析（缺 token 時退化為原生設定）。**`designatedJobTitleIds` 同為 per-caller 有效值**：僅命中例外者才帶出該步驟的限定職稱，未命中者一律為空陣列（不外洩設定）；非空＝指定審核者只能挑這些職稱的人 |
| GET/POST | `/approval-items` | 簽核項目列表 / 新增（需 `approvals:read` / `approvals:write`；body 含 `departmentId?` 部門維度，唯一性以 `(applicationType, departmentId)` 判定） |
| GET/PUT/PATCH/DELETE | `/approval-items/{id}` | 簽核項目 CRUD |
| POST | `/approval-items/{id}/steps` | 新增簽核步驟（body 可帶 `exceptionUserIds: Guid[]` 例外指定審核名單，**整批替換**；與 `useApplicantDesignated` 互斥，同時帶回 400。另可帶 `designatedJobTitleIds: int[]` 例外的限定職稱，**整批替換**；沒有例外名單卻設限定職稱回 400） |
| PUT/PATCH | `/approval-items/{id}/steps/{stepId}` | 更新簽核步驟（`exceptionUserIds` / `designatedJobTitleIds` 整批替換：`null`／未帶＝不動、`[]`＝清空；切成 `useApplicantDesignated=true` 或例外名單清空時，限定職稱一併自動清空） |
| DELETE | `/approval-items/{id}/steps/{stepId}` | 刪除簽核步驟 |

## 審核任務

| Method | Path | 說明 |
|--------|------|------|
| GET | `/approval-tasks` | 任務列表。`status`：`pending`（待審核）/ `approved`（已核准）/ `returned`（退回修改中）/ `rejected`（已拒絕）—— **走白名單正規化**，未帶＝維持既有行為（Superadmin 看全部非草稿、一般人看待審），帶了非法值一律當 `pending`。`scope=director`（總監室簽核範圍，與 `status` 四值自由組合）：`status=pending` ＝ 已輪到總監關卡（`CurrentStepOrder` 對應步驟 `JobTitle.Level=1`），其餘三態 ＝ 流程含總監關卡 + 對應 `ApprovalStatus`；僅財務管理部 / 會計室或 Superadmin 可查，其他部門回 403；財務管理部 / Superadmin 看全部，會計室只看流程中含會計室關卡的單。**舊值 `status=director_pending` 相容為 `scope=director&status=pending`**。另可帶 `paymentStatus`（撥款三態 `unpaid` / `partial` / `paid`，另加 `closed`＝已結案，只有預支 / 出差預支有結案概念、其餘類型不回傳）、`applicationType`（申請類型）、`submittedByUserId`（申請人；**僅財務體系部門或 Superadmin 生效**，其他人帶了一律忽略） |
| GET | `/approval-tasks/applicants` | 申請人下拉選項（曾送出非草稿申請者去重清單，依姓名排序）；**僅財務體系部門或 Superadmin**，其他人回 403 |
| GET | `/approval-tasks/{id}` | 取得任務詳情 |
| GET | `/approval-tasks/{appType}/{id}` | 取得指定類型的任務詳情。存取控制：Superadmin / 有 `approval-tasks:read` / 被指定為審核者 / 曾審核過 / **申請人本人**（詳情頁簽核歷程與 PDF 簽名章皆取自此端點），其餘 403 |
| PATCH | `/approval-tasks/{appType}/{id}/review` | 審核（核准 / 退回 / 拒絕）。body 可帶 `installments`：當為撥款類（payment_request / advance / travel / travel_payment）且**財務（FIN）步驟核准**時，撥款明細**必填**（加總須 == 申請總額），與審核動作同交易原子寫入。非財務步驟 / 非撥款類 / holiday_travel 不收 installments；批次核准不收。 |
| POST | `/approval-tasks/batch-approve` | 批次核准多筆待審申請（僅 approved 動作，需 `approval-tasks:batch-approve` 權限；撥款/退款日留空，完成後以提醒清單回傳需補填者） |

## 專案管理

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/projects` | 專案列表 / 新增 |
| GET | `/projects/active` | 未結案專案下拉（輕量端點，免 `projects:read`）；預設依部門可見範圍過濾，帶 `?all=true` 不過濾（加班申請跨部門支援用） |
| GET/PUT/PATCH/DELETE | `/projects/{id}` | 專案 CRUD |

## 請款 / 請假 / 出差 / 加班 / 預支申請

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/payment-requests` | 請款列表 / 新增（預設 draft，multipart 含 `vendorId` — 當 `type=vendor` 時必填且必須是 IsActive=true 的廠商） |
| GET/PUT/PATCH/DELETE | `/payment-requests/{id}` | 請款 CRUD（DTO 含 `vendorId / vendorName / vendorTaxId`） |
| PATCH | `/payment-requests/{id}/submit` | 送出請款申請（draft → pending） |
| PATCH | `/payment-requests/{id}/installments` | upsert 一或多筆撥款明細（**僅 ApprovalStatus == approved**；SUM 嚴格驗證 = TotalAmount；已撥款列鎖定不可改不可刪；每筆 PaidAt null→value 觸發一次「已撥款」通知含 N/M 期；僅財務體系部門：AC/FIN/Jabez HQ/CEO）。validate+diff 持久化核心由 `InstallmentUpsertService.Apply` 共用（與審核時原子寫入同一份邏輯）。 |
| GET/POST | `/leave-requests` | 請假列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/leave-requests/{id}` | 請假 CRUD |
| PATCH | `/leave-requests/{id}/submit` | 送出請假申請（draft → pending） |
| GET | `/leave-requests/compensatory-hours` | 查詢可補休時數（期初匯入 + 系統加班 − 已補休；期初 116/6/30 到期歸零） |
| GET | `/leave-requests/annual-quota` | 查詢年假額度（依 HireDate 計算年資） |
| GET | `/leave-requests/ceremonial-quota` | 查詢歲時祭儀假額度（僅原住民，每年 3 天，跨年歸零） |
| GET | `/leave-requests/menstrual-quota` | 查詢生理假配額（限女性，回 `isFemale` + 每月 1 天 / 全年 12 天） |
| GET | `/leave-requests/marriage-quota` | 查詢婚假配額（上限 8 天，不限年度） |
| GET | `/leave-requests/maternity-status` | 查詢產假狀態（是否已有活躍申請） |
| GET | `/leave-requests/bereavement-quota?relationship={rel}` | 查詢喪假配額（依親屬關係 3/6/8 天） |
| GET | `/leave-requests/senior-executive-eligibility` | 查詢高階主管假適用性（JobTitle.Level ≤ 3） |
| GET | `/leave-requests/senior-executive-quota` | 查詢高階主管假額度（每年 24 天，曆年歸零；`?year=` 指定年度，預設當年度） |
| GET | `/leave-requests/parental-quota` | 查詢育嬰留職停薪額度（`?childBirthDate=` 帶入才算得出該名子女的 730 天總額度與 3 歲資格；彈性單日年度 30 日額度不分子女，未帶亦回傳） |
| GET | `/leave-requests/{id}/revocable-dates[?excludeRevocationId=]` | **銷假**：可銷假日期逐日清單（已排除已核准銷假日、進行中銷假單佔用的日、今天以前的日；編輯草稿時以 `excludeRevocationId` 排除自己） |
| POST | `/leave-requests/{id}/revocations` | 新增銷假草稿（`dates[]` + `reason` + `designatedReviewers[]`） |
| GET | `/leave-revocations` | 銷假列表（非 Superadmin 只看自己） |
| GET | `/leave-revocations/{id}` | 銷假單筆（含逐日清單 + 指定審核者 + 原假單資訊） |
| PUT/PATCH | `/leave-revocations/{id}` | 更新銷假（僅 draft / returned；逐日明細整批替換） |
| DELETE | `/leave-revocations/{id}` | 刪除銷假（僅 draft / returned；一併清 ApprovalRecords / EscalationOverrides / RequestDesignatedReviewers） |
| PATCH | `/leave-revocations/{id}/submit` | 送出銷假（draft/returned → pending，**重跑一次原本的請假簽核流程**） |
| GET | `/leave-requests/working-days?start=&end=&leaveType=` | 計算扣除國定假日與六日後的實際請假日清單與天數（工作日型假別才扣假日＝除歲時祭儀假外的 16 種；任何登入者可呼叫，免 `calendar-days:read`；回 `hasCalendarData / holidayDates[] / workingDates[] / workingDays`） |
| GET/POST | `/travel-requests` | 出差預支申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-requests/{id}` | 出差預支申請 CRUD |
| PATCH | `/travel-requests/{id}/submit` | 送出出差預支申請（draft → pending） |
| GET/POST | `/travel-payment-requests` | 出差請款申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-payment-requests/{id}` | 出差請款申請 CRUD |
| PATCH | `/travel-payment-requests/{id}/submit` | 送出出差請款申請（draft → pending） |
| PATCH | `/travel-payment-requests/{id}/installments` | upsert 分期撥款（同 PaymentRequest 行為） |
| GET | `/holiday-travel-requests` | 假日執行活動申請列表（共用 TravelRequest，`IsHolidayTravel=true`） |
| POST | `/holiday-travel-requests` | 新增假日執行活動申請（預設 draft，無 Items 與發票明細）；multipart 的 `participants` JSON 中 `dates[]` 元素為 `{date, slot}`（slot：`full` / `am` / `pm`，缺席＝`full`；亦相容舊版純日期字串） |
| GET/PUT/PATCH/DELETE | `/holiday-travel-requests/{id}` | 假日執行活動申請 CRUD（participants 整組替換，`dates[]` 同上） |
| PATCH | `/holiday-travel-requests/{id}/submit` | 送出假日執行活動申請（draft → pending） |
| PATCH | `/holiday-travel-requests/{id}/installments` | upsert 分期撥款（同 PaymentRequest 行為） |
| GET | `/holiday-travel-requests/count-holidays?startDate=...&endDate=...` | 計算指定區間內的假日天數（用於計算假日津貼）；回傳含 `holidayDates[]`（yyyy-MM-dd 假日清單，供參與日期 chips 標示） |
| GET/POST | `/overtime-requests` | 加班申請列表 / 新增（預設 draft）。payload 須帶 **`projects[]`（`projectId` + `estimatedHours`），必填至少 1 筆**、`compensationType`（`compensatory` 補休 / `pay` 加班費，未知值一律正規化為 `compensatory`）；驗證：每列時數 > 0、同單不可重複專案、專案須存在。回應 `projects[]` 含 `projectCode` / `projectName` / `estimatedHours`，`estimatedHours` 為各列合計（後端計算，不接受客戶端傳入） |
| GET/PUT/PATCH/DELETE | `/overtime-requests/{id}` | 加班申請 CRUD。更新時 `projects[]` **整批替換且必填**（不支援省略），一併重算父表合計；`compensationType` 為 `null` 時不變更。**任何更新一律清空加班費快照**（日期 / 時數 / 補償方式可能已變動），重新送簽時再算 |
| PATCH | `/overtime-requests/{id}/submit` | 送出加班申請（draft → pending）。送出時依 `compensationType` 寫入加班費快照（`pay` 才算；補休型清空） |
| GET | `/overtime-requests/estimate?date=&hours=` | **加班費即時試算**（表單用，權限沿用 `overtime-requests:read`）。對象一律取 JWT `sub`，**刻意不接受 `employeeId`**（回傳含時薪可反推底薪）。回傳 `hourlyRate` / `requestedHours` / `payableHours` / `excessHours` / `capHours` / `amount` / `isHoliday` / `segments[]`（分段明細）/ `hasBaseSalary` / `hasHolidayTravelConflict` |
| GET/POST | `/advance-requests` | 預支申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/advance-requests/{id}` | 預支申請 CRUD |
| PATCH | `/advance-requests/{id}/submit` | 送出預支申請（draft → pending）；追加批次被退回後也走此端點重送 |
| POST | `/advance-requests/{id}/supplements` | **新增追加預支批次並直接送簽**（無草稿階段）。僅 `approved && !IsClosed && 無進行中追加`；multipart 帶 `advanceDate` / `reason` / `items` / `files`；明細寫入 `RoundNo = CurrentRoundNo + 1`，併入父單總額後重跑同一份 advance 簽核流程 |
| PATCH | `/advance-requests/{id}/supplements/{roundNo}` | 編輯被退回的追加批次（僅 `returned` 且 `roundNo == CurrentRoundNo`）；只替換該批次明細，不送簽 |
| DELETE | `/advance-requests/{id}/supplements/{roundNo}` | 放棄追加批次（僅 `returned` 且 `roundNo == CurrentRoundNo`）；刪該批次明細/Blob/簽核紀錄，父單還原為 `approved` |
| PATCH | `/advance-requests/{id}/installments` | upsert 分期撥款（同 PaymentRequest 行為）；追加核准後 SUM 須等於**新**總額 |
| PATCH | `/travel-requests/{id}/installments` | upsert 分期撥款（同 PaymentRequest 行為） |

## 預支沖銷申請

| Method | Path | 說明 |
|--------|------|------|
| GET | `/write-off-requests/available-advances` | 可沖銷的預支申請清單（`AvailableAdvanceDto[]`：已核准且未結案；含 `rounds` 各預支批次與 `items` 全批次費用明細，供新增表單唯讀對照，免 `advance-requests:read`；`writtenOffTotal` **只計已核准**、草稿／簽核中另計於 `pendingWriteOffTotal`；Superadmin 可見全部，`POST /write-off-requests` 的可建範圍與此一致） |
| GET | `/write-off-requests/by-advance/{advanceRequestId}` | 依預支單彙總檢視（`AdvanceWriteOffOverviewDto`：預支單完整資訊 `advance` + 該單底下全部沖銷單完整資訊 `writeOffs[]`，含明細 / 指定審核者 / 附件 / 差額撥款分期 / `refundDue`）；權限同 `write-off-requests:read`，可見性＝Superadmin、預支單申請人、或該單任一沖銷單的申請人 / 審核者 / 指定審核者 |
| GET/POST | `/write-off-requests` | 預支沖銷申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/write-off-requests/{id}` | 預支沖銷申請 CRUD |
| PATCH | `/write-off-requests/{id}/submit` | 送出預支沖銷申請（draft → pending） |
| PATCH | `/write-off-requests/{id}/installments` | 沖銷差額撥款分期 upsert（**僅 approved**、限財務體系 / Superadmin；`SUM(Amount)` 須等於 `RefundDue`＝本次沖銷造成的超支增額；`RefundDue = 0` 時回 400） |
| PATCH | `/write-off-requests/{id}/check-payments` | 沖銷明細「支票金額已支付」註記（限財務體系 / Superadmin；pending 或 approved 皆可；`CheckAmount = 0` 的明細不可勾） |

## 出差預支沖銷申請

| Method | Path | 說明 |
|--------|------|------|
| GET | `/travel-write-off-requests/available-travels` | 可沖銷的出差預支申請清單 |
| GET/POST | `/travel-write-off-requests` | 出差預支沖銷申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-write-off-requests/{id}` | 出差預支沖銷申請 CRUD |
| PATCH | `/travel-write-off-requests/{id}/submit` | 送出出差預支沖銷申請（draft → pending） |

## 預審申請

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/pre-review-requests` | 預審申請列表 / 新增（預設 draft，multipart 含品項與報價單檔上傳，blob container = `quotes`；單號 `PRV-yyyyMMdd-NNN`） |
| GET/PUT/PATCH/DELETE | `/pre-review-requests/{id}` | 預審申請 CRUD（明細含品項類別 / 品項名稱 / 金額 / 備註 / 日期 / 報價單檔；**無撥款流程、不計入款項統計報表**） |
| PATCH | `/pre-review-requests/{id}/submit` | 送出預審申請（draft → pending） |
| POST | `/quote-ocr` | 報價單 OCR 辨識（multipart，後端透過 Google Gemini API），回傳品項列表 `{itemName, amount, note}`，供前端自動展開明細。登入即可用，不需特殊權限 |

## 出勤打卡

> **權限（2026-08 起）**：打卡本身走 `attendances:read` / `attendances:write`（員工對自己），出缺勤報表列表與人工修改走 `reports-attendance:read` / `reports-attendance:write`（管理者對別人）。兩組刻意分離，詳見 [attendance-clock-rules.md](business/attendance-clock-rules.md#打卡權限2026-08-新增)。

| Method | Path | 權限 | 說明 |
|--------|------|------|------|
| GET | `/attendances` | `reports-attendance:read` | 出勤紀錄列表（分頁，套用部門可見性 scope；支援 `?dateFrom=YYYY-MM-DD&dateTo=YYYY-MM-DD` 區間篩選，前端依「日 / 週 / 月」模式換算）。**回傳「打卡紀錄 ∪ 當日請假日」合併結果**（`AttendanceLeaveMerger`）：`id = null` 代表當日只有已核准請假、無打卡的**虛擬列**（不可編輯）；另含 `userId` / `leaveHours`（當日時數合計）/ `leaves[]`（當日逐張假單，同日多張合併為一列）。**區間必須有界**：未指定起訖回退近一年，跨度 > 400 天回 400。`?export=true` 時 `pageSize` 上限放寬至 5000（一般為 100） |
| GET | `/attendances/today` | `attendances:read` | 今日打卡紀錄（當前使用者；含 `todayLeaves` 陣列：當日所有已核准請假時段，供前端顯示提示與 disable 按鈕；含 `canOvertimeWithoutClockOut` 旗標：今日免下班卡即可打加班開始，與 overtime-start 的放行判定同源；無打卡紀錄時回傳 `Id=0` 空殼仍含請假資訊） |
| POST | `/attendances/clock-in` | `attendances:write` | 上班打卡（含 GPS；落在已核准請假 `[StartDate, EndDate)` 區間內會回 BadRequest） |
| POST | `/attendances/clock-out` | `attendances:write` | 下班打卡（含 GPS；同上規則） |
| POST | `/attendances/overtime-start` | `attendances:write` | 加班開始打卡（不受請假時段阻擋）。需帶**屬於自己**且當日已核准的加班申請；一般上班日須先打下班卡，**休假日（行事曆 `IsHoliday` / 該年度無行事曆時的六日）或當日全日已核准請假時免下班卡**，且今日無打卡紀錄時自動建立「只含加班時間」的 AttendanceRecord |
| POST | `/attendances/overtime-end` | `attendances:write` | 加班結束打卡（不受請假時段阻擋） |
| PUT/PATCH | `/attendances/{id}` | `reports-attendance:write` | 人工修改出缺勤紀錄（上下班 / 加班起訖）。權限碼控管「誰能改」，Handler 內另套**部門可見性 scope** 控管「能改誰」（與 `GET /attendances` 同範圍，讀得到才改得到，非同範圍回 403）。下班時間被改動時清掉 `IsClockOutAuto`（系統補卡）標記 |

> **請假時段阻擋規則**：上下班打卡以 `Clock.Now`（Asia/Taipei）比對員工 `LeaveRequests` 中 `ApprovalStatus='approved'` 的紀錄，落在 `StartDate <= now < EndDate` 半開區間內即阻擋並回含請假單編號 / 假別 / 時段的錯誤訊息。半天 / 小時請假時段已編碼於 datetime，時段外仍可打卡（如上午半天請假，下午可打上班卡；09:00–12:00 病假，12:00 整點可打卡）。加班打卡不套用此規則。實作於 [Api/Handlers/AttendanceHandler.cs](../Api/Handlers/AttendanceHandler.cs) `EnsureNotOnLeaveAsync`，Dapper SQL 於 [Api/Services/Dapper/AttendanceReadService.cs](../Api/Services/Dapper/AttendanceReadService.cs) `GetActiveLeaveAtAsync`。

## 報表（Reports）

三個報表（出缺勤、加班、請款）共用「日 / 週 / 月」三選一時段模式。前端 segmented control 切換模式後，依使用者輸入計算 `dateFrom` / `dateTo`（皆 `YYYY-MM-DD`，inclusive）送出；後端統一接 `dateFrom` / `dateTo`（取代舊有的 `year` / `month`）。週為 ISO 8601（週一→週日），共用工具於 [Admin/src/app/features/admin/reports/utils/date-range.ts](../Admin/src/app/features/admin/reports/utils/date-range.ts)。

| Method | Path | 說明 |
|--------|------|------|
| GET | `/attendances` | 出缺勤紀錄列表（共用上方出勤打卡端點，篩選參數：`employeeId / dateFrom / dateTo / export`；含請假虛擬列，詳見上方說明）。權限：`reports-attendance:read`；列上的人工修改另需 `reports-attendance:write` |
| GET | `/reports/overtime` | 加班紀錄報表（已核准的加班申請 + 實際打卡時數，篩選參數：`employeeId / projectId / dateFrom / dateTo`；`projectId` 以 `OvertimeRequestProjects` 的 `EXISTS` 子查詢篩選。每列回 `projects[]` 含各案時數，維持「一張加班單一列」；另回 `compensationType` / `overtimePayAmount`，供 HR 直接分辨哪些加班已轉現金） |
| GET | `/reports/payment` | 款項統計報表（依類別查詢 6 種付款相關申請）。**必填** `category`（白名單：`all` / `payment` / `advance` / `writeoff` / `travel-payment` / `travel` / `travel-writeoff`，未帶或不合法 → 400）。`all` = 全部，6 種類別主查詢 `UNION ALL` 後依 `CreatedAt DESC` 分頁；明細依各列 `SourceCategory` 分組撈回對應子表。篩選參數：`dateFrom / dateTo / paymentStatus`；`{主表}.CreatedAt` 為 DATETIME，`dateTo` 用 `< DATEADD(day, 1, @DateTo)` 半開區間涵蓋當日 23:59:59。沖銷類無 installments，`paymentStatus` 被忽略（`all` 時此忽略行為一致）。權限：`reports-payment:read`，**不**需要各別 `xxx-requests:read`。 |
| GET | `/reports/payment/export` | 款項統計匯出（不分頁、**一列一明細**：主表 LEFT JOIN 對應子表（InvoiceItems / AdvanceRequestItems / WriteOffItems / TravelPaymentRequestItems / TravelRequestItems / TravelWriteOffItems），無明細仍輸出 1 列）；參數同上（含 `all`，6 種 export 查詢 `UNION ALL`）；前端依 `category` 對應右側 4 欄表頭（請款/沖銷/出差類別 → 發票號碼/品名/發票日期/金額；預支 → 類別/品名/數量/金額；`all` → 通用 發票號碼/類別、品名、發票日期/數量、金額，明細第 3 欄 per-row 取值）。所有「不適用欄位」皆以 `CAST(NULL AS …)` 明確轉型（裸 `NULL` 會被 SQL Server 視為 int，Dapper 映射 `string?`/`DateTime?` 會拋型別轉換例外 → 500）。 |
| GET | `/reports/project-water-level` | 專案水位表。回傳 `DisbursedAmount` = 四種支出加總：① 請款已撥分期（PaymentRequest 非 draft + Installment.PaidAt 非 null）② 已核准預支沖銷 GrandTotal（透過 AdvanceRequest.ProjectId）③ 出差請款已撥分期 ④ 已核准出差沖銷 GrandTotal（透過 TravelRequest.ProjectId）。`Percentage` / `TotalPercentage` 皆以 `DisbursedAmount` 計算（分母為 0 / NULL 時回 `null`，前端顯示「—」）。**2026-07 起回傳可見範圍內的全部專案**（原本 `DisbursedAmount = 0` 不回傳，導致尚無撥款紀錄時整張表空白）。篩選參數：`year / status`；套用部門可見性。權限：`reports-project-water-level:read`（進得了報表）＋**欄位級 `reports-project-water-level:total`**：沒有後者時 `TotalPercentage` / `PreImportUsedAmount` / `RemainingAmount` 一律回 `null` / `0`（三者皆為總水位的原料，不可只藏百分比），`Percentage` / `ContractAmount` / `DisbursedAmount` 照常回傳。 |

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

## 當前使用者聚合資訊（Me）

| Method | Path | 說明 |
|--------|------|------|
| GET | `/me/notification-counts` | 鈴噹通知件數聚合：回 `{approvals, myRequests, recentApprovals}`。`approvals` / `myRequests` 為 9 種申請類型 → 件數的 dictionary（前者走 reviewer 過濾，後者統計當前使用者送出且狀態為 `pending` / `returned` 的件數）。`recentApprovals` 為當前使用者「最近 10 分鐘內被核准」的單清單 `[{type, id, approvedAt}]`，供前端輪詢時比對時間戳跳「已核准」toast（後端無狀態，去重由前端 localStorage 處理）。登入即可呼叫；前端每 60 秒輪詢（分頁背景暫停） |
| GET | `/me/user` | 員工查看**自己**的帳號資料（回傳與 `/users/{id}` 同型別 `UserDetailDto`，含薪資 / 加給 / 勞健保覆寫 / 各證明檔 URL / 頭像 / 簽名 / 部門 / 職稱）。從 JWT `sub` 取自身 id，**登入即可，不需 `users:read`**。供「個人資訊」唯讀頁用。**刻意不套薪資欄位級權限**：員工看自己的薪資是既有需求 |
| GET | `/me/profile` | 員工查看**自己**的人事資料卡（回傳與 `/users/{id}/profile` 同型別 `EmployeeProfileDetailDto`，含 9 張子表 + 健保眷屬）。**登入即可，不需 `users:read`**，且**刻意不套薪資欄位級權限**（自己的薪資調整歷史照常回傳） |
| GET | `/me/payroll?months=12` | 員工查看**自己**近 N 個月的薪資明細（`months` 預設 12、clamp 1~24；回 `MyPayrollHistoryDto`＝`{months:[{year, month, isCurrentMonth, payroll}]}`，`payroll` 與 `/payroll` 同型別 `EmployeePayrollDto`）。**登入即可，不需 `payroll:read`**，端點不接受 employeeId 參數故無法查別人。逐月呼叫 `CalculateMonthlyPayrollAsync(y, m, userId)`，**薪資為即時重算、無月結快照**（底薪 / 加給取自 `Users` 表當下的值），到職日之前的月份不列入。供「個人資訊」→「過往薪資」Tab 用 |
| GET | `/me/files/{container}/{fileName}` | 員工自助讀取**自己的** PII 檔案代理。白名單容器：`id-cards` / `education-proofs` / `passbooks` / `indigenous-proofs` / `low-income-proofs` / `disabled-proofs` / `avatars` / `signatures`；非白名單回 404。安全機制：`fileName` 必須以自身 `userId` 開頭（後接 `.` 或 `_`），否則 403，避免員工竄改 fileName 讀他人檔案。**登入即可，不需 `users:read`**（管理端 `/files/<container>` 仍需 `users:read`） |

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
| GET | `/files/disabled-proofs/{fileName}` | 身心障礙證明文件代理（需 `users:read`，HR 敏感 PII） |
| GET | `/files/id-cards/{fileName}` | 身分證影本代理（需 `users:read`，HR 敏感 PII） |
| GET | `/files/education-proofs/{fileName}` | 最高學歷證明代理（需 `users:read`，HR 敏感 PII） |
| GET | `/files/passbooks/{fileName}` | 員工存摺封面代理（需 `users:read`，HR 敏感 PII） |
| GET | `/files/quotes/{*path}` | 報價單代理（需 JWT，免特殊權限，與 vendor-passbooks 同層的一般業務檔案；blob name 含日期子路徑 `yyyy/MM/{guid}{ext}`，故 path 為多段） |
| GET | `/files/request-attachments/{*path}` | 整單批次附件代理（需 JWT，免特殊權限；一般請款 / 預支沖銷 / 預審 共用；blob name 含日期子路徑，path 為多段） |

## 員工人事資料卡（HR Profile）

| Method | Path | 說明 |
|--------|------|------|
| GET | `/users/{id}/profile` | 取得員工人事資料卡（EmployeeProfile + 9 張子表）。Profile 不存在時回傳預設空殼。**欄位級權限**：無 `payroll:read` 者 `salaryAdjustmentRecords` 回 `[]`，其餘 8 張子表照常 |
| PUT | `/users/{id}/profile` | 整批更新員工人事資料卡（multipart：`payload` JSON + `idCardFront` / `idCardBack` / `highestEducationProof` / `bankBookImage` / `bankBookImage2` 檔案 + `removeIdCardFront` / `removeIdCardBack` / `removeHighestEducationProof` / `removeBankBook` / `removeBankBook2` 旗標；兩張存摺封面對應第一 / 第二銀行帳戶，blob 命名 `{userId}_passbook{ext}` / `{userId}_passbook2{ext}`）。9 張子表整批替換；薪資調整紀錄會自動同步「最新生效底薪」回 `User.BaseSalary`。**欄位級權限**：`salaryAdjustmentRecords` 為**條件式**整批替換 —— 缺 `payroll:read` 或 payload 省略該 key（`null`）時整段跳過（既有薪資歷史不刪不改、也不做 User 同步）；送 `[]` 才是清空 |

> 員工要查看**自己**的人事資料卡（唯讀，免 `users:read`）改走 [`/me/profile` + `/me/user` + `/me/files`](#當前使用者聚合資訊me)（避免管理權限強加到一般員工）。

## 其他

| Method | Path | 說明 |
|--------|------|------|
| GET | `/settings` | 取得系統設定 |
| PATCH | `/settings` | 更新系統設定 |
| POST | `/invoice-ocr` | 發票 / 收據 / 交通票根 OCR 辨識（multipart 欄位 `file`，後端透過 Google Gemini API）。**一張圖可包含多張，回傳 `data` 為陣列**（每張一筆 `{docType, invoiceNo, amount, invoiceDate, buyerName, buyerTaxId, sellerTaxId}`，無辨識結果回 `[]`）。`buyerName`/`buyerTaxId` 為統一發票買方抬頭/統編（票根固定空字串），供前端比對公司白名單顯示警告；`sellerTaxId` 為賣方（發票專用章）統編，供前端交叉比對——與 `buyerTaxId` 相同時代表 OCR 抄到賣方統編，不跳警告。**`invoiceNo` 後處理**：字軌與數字分開印時合併（`ED 22598786` → `ED22598786`）；統一發票抓到 8 碼純數字且等於 `buyerTaxId` / `sellerTaxId` 時視為誤抓統編欄而清空。登入即可用，不需特殊權限 |

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
