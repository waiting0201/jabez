# Jabez — QA Issue Patterns (First Full Review, 2026-02-26)

## Critical Issues Found in This Review

### 1. ApplicationType enum missing 'overtime' (Frontend)
- File: `Admin/src/app/features/admin/approvals/models/approval.model.ts` line 1
- Backend `ApprovalTaskHandler` and `PaymentRequestReadService` treat `'overtime'` as valid
- Frontend `ApplicationType = 'payment_request' | 'leave' | 'travel'` — 'overtime' is absent
- `APPLICATION_TYPE_LABELS` and `APPLICATION_TYPE_CLASSES` have no 'overtime' key
- Any template rendering `appTypeLabel[task.applicationType]` for overtime tasks returns `undefined`

### 2. getApprovedForToday() with unsupported query parameters (Frontend/Backend mismatch)
- File: `Admin/src/app/features/admin/overtime-requests/services/overtime-request.service.ts` line 42–46
- Sends `{status: 'approved', today: 'true'}` as query params
- Backend `OvertimeRequestHandler.GetAllAsync()` only reads `page` and `pageSize` — ignores `status` and `today`
- Returns ALL overtime requests, unfiltered. Dashboard will display wrong overtime options.

### 3. ApprovalTask model missing OvertimeDetail (Frontend)
- File: `Admin/src/app/features/admin/approval-tasks/models/approval-task.model.ts`
- Backend `ApprovalTaskDto` returns `OvertimeTaskDetailDto? OvertimeDetail`
- Frontend `ApprovalTask` interface has no `overtimeDetail` property
- Any overtime task's detail will be silently dropped; `getSummary()` in list will always return '—' for overtime

### 4. PaymentRequest entity wrong default status
- File: `Api/Models/Entities/PaymentRequest.cs` line 10
- `ApprovalStatus = "pending"` as default
- All handlers explicitly set "draft" on create, BUT if EF ever creates a record without explicit assignment, status defaults to "pending" silently bypassing draft state

### 5. ApprovalTaskHandler in-memory pagination (Performance/Correctness risk)
- File: `Api/Handlers/ApprovalTaskHandler.cs` lines 49–53
- `GetAllAsync` fetches ALL approval tasks into memory, then paginates in C# code
- With large data sets, this will cause serious memory and performance issues
- No database-level pagination for combined approval tasks view

### 6. AttendanceHandler JWT double-validation
- File: `Api/Handlers/AttendanceHandler.cs` line 172–178
- `GetUserIdAsync` calls `jwtService.ValidateRequestAsync(req)` again after `AppRouter` already validated it
- Minor performance waste but not a correctness issue — validation happens twice per attendance request

### 7. TodayAttendance model userId field never populated
- File: `Admin/src/app/features/dashboard/models/attendance.model.ts` line 2
- Frontend has `userId: string` in `TodayAttendance`
- Backend `TodayAttendanceDto` does NOT include userId — API never returns it
- Field always `undefined` in frontend

### 8. BehaviorSubject in services violating stated Angular Signals convention
- Files: `approval-task.service.ts`, `attendance.service.ts`, `permission.service.ts`, multiple page components
- CLAUDE.md states "使用 Angular Signals 管理認證狀態" and general signals preference
- Several services use `BehaviorSubject` for state management instead

### 9. Console.log statements in production interceptor code
- File: `Admin/src/app/core/auth/interceptors/api-response.interceptor.ts` lines 28–30
- `console.log('[Interceptor] unwrap ApiResponse:', ...)` — logs every API response including data
- File: `Admin/src/app/core/auth/services/auth.service.ts` lines 79–84
- Multiple `console.log` and `console.error` in login flow

### 10. auth.interceptor.ts does not implement token refresh
- File: `Admin/src/app/core/auth/interceptors/auth.interceptor.ts` lines 17–22
- 401 response immediately triggers logout and redirect to login
- Backend has refresh token support (`POST /auth/refresh`) but frontend never uses it
- Any token expiry during active session causes immediate forced logout

### 11. approvalTaskService.getById() ignores applicationType parameter
- File: `Admin/src/app/features/admin/approval-tasks/services/approval-task.service.ts` line 26–28
- Method signature: `getById(id: number, applicationType?: string): Observable<ApprovalTask>`
- `applicationType` parameter is declared but never used — always calls `GET /approval-tasks/{id}` without it
- Backend `GetByIdAsync(string id)` uses backward-compat overload that scans ALL types — may return wrong task if IDs collide across types

### 12. PaymentRequest.CreateAsync does not set SubmittedById
- File: `Api/Handlers/PaymentRequestHandler.cs` lines 110–122
- Creates a `PaymentRequest` without setting `SubmittedById`
- Dapper SQL in `PaymentRequestReadService` joins `Users sub ON pr.SubmittedById = sub.Id` — `SubmittedBy` will always be `null` for newly created requests
- Same issue in `LeaveRequestHandler`, `TravelRequestHandler`, `OvertimeRequestHandler` — none set `EmployeeId` from JWT

### 13. InvoiceItem.id type mismatch
- Frontend `InvoiceItem.id` is `string` (model line 33)
- Backend `InvoiceItemDto.Id` is `int`
- In `payment-form.ts` line 86: `String(inv.id)` is used to convert, suggesting the frontend acknowledges this
- But in `_invoiceGroup` the `id` is treated as a string for `fileMap` key — this works, but mixing `int` from API with local `string` IDs could cause subtle bugs

### 14. PaymentRequest UpdateAsync allows status 'pending' edit but loses step context
- File: `Api/Handlers/PaymentRequestHandler.cs` line 138
- Status check: `if (pr.ApprovalStatus != "draft" && pr.ApprovalStatus != "pending")` — allows editing while pending
- When editing a pending request, the approval flow step and status are not reset
- A request mid-approval can have its invoices replaced without triggering a re-review at step 1

### 15. Returned request re-submission only in LeaveRequest/TravelRequest/OvertimeRequest UpdateAsync
- Files: `LeaveRequestHandler.cs`, `TravelRequestHandler.cs`, `OvertimeRequestHandler.cs`
- If `wasReturned`, UpdateAsync resets step to 1 and sets status to "pending"
- But `PaymentRequestHandler.UpdateAsync` does NOT have this logic — returned payment requests cannot re-enter the approval flow via update

### 16. ExceptionMiddleware message duplication
- File: `Api/Middleware/ExceptionMiddleware.cs` line 34
- `WriteErrorAsync(context, appEx.StatusCode, appEx.Message, appEx.Message)` — both `message` and `errorDetail` receive the same value
- `ApiResponse.Fail(message, errorDetail)` stores it in both `Message` and `Errors[0]` — error detail is duplicated in response

### 17. Approval flow for returned PaymentRequest missing
- `SubmitAsync` only transitions from "draft" to "pending" — there's no re-submit path for "returned" payment requests
- LeaveRequest/Travel/OvertimeRequest auto-transition in UpdateAsync when `wasReturned`
- PaymentRequest has no such mechanism in either UpdateAsync or SubmitAsync — returned payment requests are permanently stuck

## Architecture-level Concerns

1. All request handlers (Leave, Travel, Overtime, PaymentRequest) create records without setting the `EmployeeId`/`SubmittedById` from the JWT claim — the current user's identity is never captured in the record.

2. `OvertimeRequestHandler` allows setting `ApprovalStatus = "draft"` as default but the entity class uses `"pending"` as default — inconsistency between handler and entity.

3. The entire approval task list is loaded in memory by `IPaymentRequestReadService.GetApprovalTasksAsync()` before pagination — this will not scale.
