# Jabez QA Engineer Memory

## Project Overview
- 請款簽核及工時管理系統 (Payment Request Approval & Timesheet Management System)
- Backend: `/Users/tim/webapps/Jabez/Api` (.NET 9, Azure Functions v4, EF Core + Dapper)
- Frontend: `/Users/tim/webapps/Jabez/Admin` (Angular 21.1, Tailwind v4, standalone components)

## Key Quality Patterns (confirmed across files)

### Recurring Bug Patterns
- **Missing `getAll()` filter support**: Several backend handlers (`LeaveRequestHandler`, `TravelRequestHandler`, `OvertimeRequestHandler`) only expose `GetPagedAsync` in `GetAllAsync` — their Dapper ReadServices have `GetAllAsync()` but the handlers never call it. Frontend `getAll()` calls always return paged data.
- **Frontend `ApplicationType` enum missing `overtime`**: `approval.model.ts` defines `ApplicationType = 'payment_request' | 'leave' | 'travel'` but backend `ApprovalTaskHandler` and related code handles `'overtime'` as a valid type. Any UI code referencing `APPLICATION_TYPE_LABELS['overtime']` will return `undefined`.
- **`getApprovedForToday()` with unsupported query params**: `OvertimeRequestService.getApprovedForToday()` sends `{status: 'approved', today: 'true'}` but `OvertimeRequestHandler.GetAllAsync()` ignores these query params entirely — backend returns ALL overtime requests, not filtered.
- **`ApprovalTaskService.getAll()` vs `getPaged()` data shape mismatch**: `getAll()` unwraps to flat array but `ApprovalTaskHandler.GetAllAsync()` always returns `PagedResult`. The `getPaged()` method is consistent and used by list components correctly.

### Architecture Risks
- `PaymentRequest` entity defaults `ApprovalStatus = "pending"` but `CreateAsync` explicitly sets it to `"draft"` — entity default is misleading.
- `ApprovalTaskHandler.GetAllAsync` always returns a `PagedResult` (even via `getAll()`) — but the handler does in-memory pagination after fetching ALL records via SQL. At scale, this is a serious performance risk.
- Console.log statements left in production code: `api-response.interceptor.ts`, `auth.service.ts` — information leakage risk.
- `BehaviorSubject` used in `ApprovalTaskService` and `AttendanceService` violating stated convention of using Angular Signals.

### Auth & Security Notes
- `auth.interceptor.ts` handles 401 by logout + redirect but does NOT retry with refresh token — any token expiry during a session immediately logs the user out.
- `JwtService.ValidateToken` silently swallows all exceptions — no distinction between malformed and expired tokens.
- `AttendanceHandler` validates JWT twice per request (once in `AppRouter.IsPublicRoute` check, once in `GetUserIdAsync`) — minor inefficiency.

## Confirmed Field Mappings
- Backend `PagedResult<T>` uses `TotalCount` (PascalCase) → serialized to `totalCount` (camelCase) → Frontend `PagedResult<T>` uses `totalCount`. CONSISTENT.
- `TodayAttendanceDto` does NOT include `userId` field. Frontend `TodayAttendance` model has `userId?: string` — never populated by API.

## Reviewed Files (2026-02-26)
See `patterns.md` for full issue list from first comprehensive review.

## Full API Runtime Test (2026-03-02) — NEW CONFIRMED BUGS
See `api-runtime-test-2026-03-02.md` for full report.

### CRITICAL: Backend has NO permission enforcement
- `AppRouter.cs` only validates JWT existence, never checks `permissions` claims
- ANY authenticated user can call ANY endpoint regardless of role/permissions
- Confirmed: viewer role (Carol) successfully created roles and departments
- Location: `/Users/tim/webapps/Jabez/Api/Routing/AppRouter.cs` — no RequirePermission middleware

### Bug: RoleHandler.CreateAsync NullReferenceException
- `CreateRoleRequest.PermissionCodes` is `string[]` (non-nullable) but JSON deserialization returns null if omitted
- `foreach (var permId in body.PermissionCodes)` on line 46 throws NullReferenceException
- Workaround: client must always pass `"permissionCodes": []`
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/RoleHandler.cs` line 46

### Bug: PermissionHandler.CreateAsync NullReferenceException (EF Identity Map)
- `CreatePermissionRequest` requires explicit `Id` field — omitting causes EF NullableKeyIdentityMap error
- API returns 500; API docs/CLAUDE.md don't mention that `id` is mandatory for permissions
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/PermissionHandler.cs` line 42

### Bug: PaymentRequestHandler.CreateAsync/UpdateAsync expects Multipart Form, not JSON
- `PaymentRequestHandler` uses `req.ReadFormAsync()` for create and update
- Sending `application/json` causes `System.InvalidOperationException: Incorrect Content-Type`
- ALL other handlers use `req.ReadFromJsonAsync<T>()` — this is the only multipart exception
- Not documented — API contract is inconsistent with all other endpoints
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/PaymentRequestHandler.cs` line 63

### Bug: ApprovalHandler.CreateAsync requires both Name AND Code (not documented)
- POST /api/approval-items with only `name` returns 400 "Name and Code are required"
- `code` is a mandatory undocumented field
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/ApprovalHandler.cs` line 39

### Bug: LeaveRequest allows negative `days` and any string `leaveType`
- No validation on `Days` field — negative values accepted (e.g., days=-5)
- No validation on `LeaveType` enum — `"invalid_type"` accepted and stored
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/LeaveRequestHandler.cs` lines 58-78

### Bug: String length not validated — SQL truncation error exposed as 500
- Sending 1000-char name to /departments causes `SqlException: String or binary data would be truncated`
- ExceptionMiddleware catches it but returns generic 500, not 400 with meaningful message
- No MaxLength validation in Handlers before DB write

### Info: XSS content stored without sanitization
- `<script>alert("XSS")</script>` stored as-is via department name
- Backend does not sanitize HTML in text fields
- Mitigation relies entirely on frontend output encoding

### Project entity missing name/description/startDate/endDate fields
- `ProjectDto` and `Project` entity have no `Name`, `Description`, `StartDate`, `EndDate`
- Frontend sends these fields in POST but backend ignores them
- Location: `/Users/tim/webapps/Jabez/Api/Models/Dtos/ProjectDtos.cs`

## Advance Request Module QA (2026-03-17)
See `advance-request-qa-2026-03-17.md` for full report.

### CRITICAL: RequestNo 產生有並發競爭條件
- `AdvanceRequestHandler.CreateAsync` 用 SELECT MAX 再 +1 的方式產生 ADV-yyyyMMdd-NNN
- `AdvanceRequests.RequestNo` 無 UNIQUE INDEX，並發時會產生重複單號
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/AdvanceRequestHandler.cs` line 90-102

### CRITICAL: GetByIdAsync/GetWriteOffsAsync 所有者驗證太嚴
- `SubmittedById == userId` 的驗證使財務部/審核者無法直接透過 `/advance-requests/{id}` 取得詳情
- 財務部在 ApprovalTaskReview 頁面呼叫 `service.getById()` 會返回 404
- Location: `AdvanceRequestHandler.cs` lines 65-66, 351-352, 462-463

### Warning: 硬編碼「財務部」字串判斷
- 後端 `UpdatePaymentDateAsync` 和前端 `canSetPaymentDate` / `canEditPaymentDate` 都用 `'財務部'` 字串比對
- 部門改名時功能靜默失效
- Locations: `AdvanceRequestHandler.cs` line 320, `approval-task-review.ts` lines 123, 131-138

### Warning: sortOrder 在沖銷表單中永遠為 0
- `write-off-form.ts` 的 `_buildFormData()` 設定 `sortOrder: 0` 給所有明細
- Location: `/Users/tim/webapps/Jabez/Admin/src/app/features/admin/advance-requests/pages/write-off-form/write-off-form.ts` line 192

### Pattern: advance 類型已正確加入 ApplicationType
- `approval.model.ts` 的 `ApplicationType` 已包含 `'advance'`（之前只有 payment_request/leave/travel/overtime）
- 前後端 enum 現已一致

## ApprovalItems / ApprovalSteps CRUD 測試 (2026-03-18)
See `approval-items-qa-2026-03-18.md` for full report.

### BUG: UpdateStepAsync — departmentId 無法被更新（patch 語意問題）
- 測試案例：PUT step with `{useDirectSupervisor:false, departmentId:1, jobTitleId:4}`
- 實際結果：departmentId 仍為 null，jobTitleId 正確更新為 4
- 根本原因：`UpdateApprovalStepRequest.DepartmentId` 是 `int?`，傳入 `1` 時 `body.DepartmentId.HasValue` 為 true，但 `else if (step.UseApplicantDepartment)` 分支（line 190-194）在之前 `useApplicantDepartment` 為 true 時會再度將 `step.DepartmentId = null`，覆蓋剛設定的值
- 補充：前一步驟中 step 的 `UseApplicantDepartment` 是 true（因原本是 useDirectSupervisor 模式），而 body 只傳 `useDirectSupervisor:false` 但未傳 `useApplicantDepartment:false`，所以 `step.UseApplicantDepartment` 維持 true，觸發 else-if 清除 departmentId
- 換言之：切換模式時必須明確傳 `useApplicantDepartment:false`，否則 departmentId 會被強制清空
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/ApprovalHandler.cs` lines 176-194

### BUG: AddStepAsync 回傳的 data.id 是 ApprovalItem ID，非新建 Step ID
- POST /approval-items/{id}/steps 的回傳 data 是整個 ApprovalItemDto，其 `id` 是 ApprovalItem 的 ID
- 前端若直接用 `response.data.id` 取得新步驟 ID 會得到錯誤值（ApprovalItem 的 ID）
- 正確做法需從 `response.data.steps` 陣列中找出剛新增的步驟
- Location: `/Users/tim/webapps/Jabez/Api/Handlers/ApprovalHandler.cs` line 160

### 通過項目（2026-03-18）
- GET /approval-items — 列表正常，含完整 steps 資料
- GET /approval-items/{id} — 單筆查詢正常
- POST /approval-items — 建立正常，回傳 201
- PUT /approval-items/{id} — 更新名稱正常
- DELETE /approval-items/{id} + 確認 404 — 正常
- POST steps useDirectSupervisor=true — 自動清除 deptId/jtId，useApplicantDepartment 自動設 true
- PUT step 改回 useDirectSupervisor=true — deptId/jtId 正確被清除
- DELETE steps — 逐一刪除正常
- 驗證規則 1：useDirectSupervisor=true 不需 deptId/jtId → 通過 (200)
- 驗證規則 2：useApplicantDepartment=true 缺 jobTitleId → 400 + 正確訊息
- 驗證規則 3：兩個 ID 都缺 → 400 + 正確訊息
- 驗證規則 4：更新 code 為重複值 → 409（非 500）

## UseDirectSupervisor 簽核流程測試 (2026-03-18)
See `use-direct-supervisor-qa-2026-03-18.md` for full report.

### Level 定義確認：數字越大 = 層級越高
- DB 資料：工程師(1), 資深工程師(2), 主任工程師(3), 部門主管(4), 總監(5)
- 程式碼用 `Level < applicantLevel` 找上層級 = Level 數字越大越高
- 程式碼註解寫「Level 越小越高」是錯誤的誤導性文字

### CRITICAL: UseDirectSupervisor Step 多層時中間步驟無法跳過 → 申請卡死
- `ProcessReviewAsync` 在 Step N 通過後，直接 `incrementStep()` 推進到 Step N+1
- 但 Step N+1 若 UseDirectSupervisor 且該 rank 的上層級不存在，沒有任何人能審核
- 在送出時 `ResolveStartingStepAsync` 會跳過找不到上級的步驟
- 但在 **審核推進** 時沒有對應的「下一步驟跳過」邏輯
- 後果：申請永久卡在 pending 狀態，普通用戶無法審核，只有 SA 可以強制通過
- 復現條件：Eve(Level2) 送出請假 → Carol(Level1) 通過 Step1 → Step2 要求 rank=1 但不存在
- Location: `ApprovalTaskHandler.cs` `ProcessReviewAsync` + `ApprovalFlowService.cs` `ResolveStartingStepAsync`

### CRITICAL: UseDirectSupervisor AuthorizeStepAsync 當 targetLevel=0 時沒有阻擋高層級使用者
- 測試案例：Eve(Level2)的請假 Step2，rank=1 找不到人，targetLevel = default(int) = 0
- Tim(Level4) 成功審核了 Step2，原因待確認（可能是 `targetLevel == 0` 的條件反而放行了某些路徑）
- 重現：申請卡死後，業務部 Tim(Level4) 成功執行 PATCH review approved → 200 OK
- Location: `ApprovalTaskHandler.cs` `AuthorizeStepAsync` line 273：`if (targetLevel == 0 || reviewerLevel != targetLevel)`
  - 當 targetLevel=0（找不到人），這條件為 true → 應 throw Forbidden
  - 但測試中 Tim 成功通過，需進一步調查是否有其他路徑繞過此檢查

### 通過測試項目（UseDirectSupervisor，2026-03-18）
- Step1 正常：Tim(Level4) 送出 → David(Level3, rank=0) 看到並審核 → Step2 進入
- Step2 正常：Carol(Level1, rank=1) 看到並審核 → 全流程核准
- 授權拒絕正常：David 嘗試審核 Step2（rank=1 位置）→ 403 Forbidden
- 邊界 D-1 通過：Alice（部門唯一員工）送出 → 無上級 → 自動核准（系統正常）
- 退回流程正常：Eve 退回 David 的請假 → David 重新送出 → 成功
- 拒絕流程正常：Eve 拒絕 David 的請假 → status=rejected

## UseApplicantDesignated 指定審核流程測試 (2026-03-19)
See `designated-reviewer-qa-2026-03-19.md` for full report.

### BUG: LeaveRequestDto 缺少關鍵欄位 → GET /leave-requests/{id} 回傳不完整資料
- `LeaveRequestDto` 無 `CurrentStepOrder`、`ApprovalItemId`、`ReviewedById` 欄位
- GET /leave-requests/{id} 回傳這些欄位永遠為 null
- GET /approval-tasks 可正確回傳（使用不同 SQL 查詢路徑）
- 同一申請的狀態在不同 endpoint 有不同的資料完整度
- Location: `/Users/tim/webapps/Jabez/Api/Models/Dtos/LeaveRequestDtos.cs` + `LeaveRequestReadService.cs`

### BUG: approvalStatus 在 approval-tasks 回傳值中永遠為 null
- `GetApprovalTasksAsync` SQL 查詢有 `ApprovalStatus` 欄位，但 `ApprovalTaskDto` mapping 將其對應到 status=None
- 確認：GET /approval-tasks 中 leave 4012 的 approvalStatus=null，但 GET /leave-requests/4012 正確回傳 approved
- 同樣問題出現在 GetApprovalTaskByIdAsync — status 顯示為 null

### BUG: 自審漏洞 — 申請人可被指定為「非第一位」審核者，且可在輪到時自審通過
- submit 時只檢查第一位指定審核者（StepOrder 最小者）是否為申請人
- 若申請人指定自己為 StepOrder=2，系統允許送出，且輪到時可自審通過（leave 類型）
- 這與 CLAUDE.md 文件「leave / travel / overtime 自審 → 報錯」的規範衝突
- 測試確認：LR 4017 Carol 自指為第 2 位 → 審核成功，status=pending（未自動推進到 Step 2 固定審核）
- Location: `ApprovalFlowService.cs` `ResolveStartingStepAsync` 僅驗證 firstReviewer（min StepOrder）

### 通過測試項目（UseApplicantDesignated，2026-03-19）
- 正常流程：2 位指定審核者（Alice→Bob），逐步審核 ✅
- 未輪到者搶先審核被拒絕：Bob 在 Alice 前搶審 → 403 ✅
- 已審核者重複提交被拒絕：Alice 審完再審 → 403 ✅
- 退回後重送：DR 全部重置為 pending，Alice 可再審核 ✅
- Case A：送出時無指定審核者 → 400 ✅
- Case B：指定自己（第 1 位）→ 400 ✅
- 單一審核者後推進到下一固定步驟 ✅
- 非 SA 無全域 approval_tasks 權限但被指定仍可看到並審核任務 ✅

## 最近 5 Commit 整合測試 (2026-04-25)
See `5-commit-qa-2026-04-25.md` for full report.

### 重要確認
- `User.Avatar` 欄位名稱確認為 `Avatar`（非 `AvatarUrl`），JWT claim 為 `avatar`，前後端一致
- `ApplicationType` enum 在 `approval.model.ts` 已包含所有 9 種類型（含 travel_payment, holiday_travel）
- `FileHandler.IsSafeFileName` 過濾 `/` 和 `\`，但未過濾 URL 編碼形式（`%2f`, `%2e%2e`），有路徑穿越風險
- 打卡提醒 LINE 推播失敗用 `LogWarning` 而非 `LogError`，未區分「未加好友」與其他錯誤

### Payroll 假日津貼歸月邏輯確認
- `PrevMonthFirstDay = firstDay.AddMonths(-1)`，查詢 `EndDate >= PrevMonthFirstDay AND EndDate < CurrMonthFirstDay`
- 「4月薪資」算的是 EndDate 落在 3/1~3/31 的活動。CLAUDE.md 說「4月薪資計入 3月EndDate的活動」→ 正確
- CLAUDE.md 範例「3月活動 → 4月薪資」是對的，但這要求薪資月 = EndDate月 + 1月
- 查詢參數：PrevMonthFirstDay = 3/1，CurrMonthFirstDay = 4/1 → EndDate in [3/1, 4/1) → 即 3月份 EndDate → 進入 4月薪資 ✅

## 簽核作業資料可見性隔離測試 (2026-03-19)
See `visibility-isolation-qa-2026-03-19.md` for full report.

### CRITICAL: GET /approval-tasks/{type}/{id} 完全無存取控制
- 任何已登入使用者可透過 ID 枚舉讀取所有申請單完整詳情
- Location: `ApprovalTaskHandler.cs` `GetByIdAsync` — 無 userId 過濾

### BUG: ForbidResult 在 Azure Functions Isolated Worker 回 HTTP 500 而非 403
- 觸發：無 approval-tasks:write 權限的使用者執行 PATCH review
- Location: `ApprovalTaskHandler.cs` line 124: `return new ForbidResult()`

### GET /approval-tasks 清單隔離完全正確（2026-03-19 確認）
- 6 位使用者各自只看到應看到的任務，零誤判
- UseApplicantDesignated 時序保護正確（stepOrder=1 未完成前 stepOrder=2 不可見）
- 固定部門/職稱步驟授權拒絕正確（403）
