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
