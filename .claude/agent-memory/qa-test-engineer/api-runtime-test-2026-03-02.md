# Jabez API Runtime Test Report — 2026-03-02

## Test Summary
- Backend: dotnet build succeeded, 0 errors, 0 warnings
- Frontend: ng build succeeded, no errors
- API health check: healthy
- Test scope: All CRUD endpoints, auth, permissions, boundary conditions

## Test Results by Module

### Auth (PASS with caveats)
- POST /auth/login (sa@system.local): PASS — returns valid JWT + refresh token
- POST /auth/refresh: PASS — rotates token correctly
- POST /auth/refresh with invalid token: PASS — returns 401
- POST /auth/login with empty email: PASS — returns 400
- POST /auth/login with unknown email: PASS — returns 401

### Users (PASS)
- GET /users: PASS — excludes superadmin
- POST /users: PASS
- GET /users/{id}: PASS
- PUT /users/{id}: PASS
- DELETE /users/{id}: PASS
- DELETE superadmin: PASS — correctly blocked

### Departments (PASS)
- All CRUD: PASS

### Job Titles (PASS)
- All CRUD: PASS

### Roles (FAIL — CreateAsync NullReferenceException)
- GET /roles: PASS
- POST /roles without permissionCodes: FAIL — NullReferenceException (line 46 RoleHandler.cs)
- POST /roles with permissionCodes=[]: PASS
- PUT /roles/{id}: PASS
- DELETE /roles/{id}: PASS

### Permissions (FAIL — CreateAsync requires explicit id)
- GET /permissions: PASS
- POST /permissions without id field: FAIL — EF NullableKeyIdentityMap error (500)
- POST /permissions with id field: PASS
- PUT /permissions/{id}: PASS
- DELETE /permissions/{id}: PASS

### Projects (PARTIAL — missing fields)
- GET /projects: PASS — but name/description/startDate/endDate columns absent from DTO
- POST /projects: PASS — but name/description/startDate/endDate silently ignored
- PUT /projects/{id}: PASS
- DELETE /projects/{id}: PASS

### Approval Items (FAIL — CreateAsync requires Code)
- GET /approval-items: PASS
- POST without code field: FAIL — 400 "Name and Code are required"
- POST with code field: PASS
- POST /approval-items/{id}/steps: PASS
- PUT /approval-items/{id}: PASS
- DELETE /approval-items/{id}/steps/{stepId}: PASS
- DELETE /approval-items/{id}: PASS

### Payment Requests (FAIL — JSON vs multipart mismatch)
- GET /payment-requests: PASS
- POST with application/json: FAIL — InvalidOperationException (500) — MUST use multipart/form-data
- POST with multipart/form-data: PASS
- GET /payment-requests/{id}: PASS
- PUT/PATCH via multipart: not fully tested
- PATCH /{id}/submit: PASS — draft→pending works
- DELETE (non-draft): PASS — correctly blocked

### Leave Requests (PASS with caveats)
- All CRUD: PASS
- Date validation (endDate < startDate): PASS — correctly blocked
- Days validation: FAIL — negative days (-5) accepted
- LeaveType validation: FAIL — invalid enum values accepted (no whitelist)
- Business logic (non-draft delete): PASS — correctly blocked

### Travel Requests (PASS)
- All CRUD: PASS
- Business logic (non-draft delete/edit): PASS — correctly blocked

### Overtime Requests (PASS with caveats)
- POST with wrong field name (date instead of overtimeDate): FAIL — 400 "OvertimeDate is required"
- POST with correct overtimeDate: PASS
- EstimatedHours = 0: PASS — correctly blocked
- EstimatedHours = -1: PASS — correctly blocked
- All CRUD: PASS
- Business logic: PASS

### Attendance (PASS)
- GET /attendances/today: PASS
- POST /attendances/clock-in: PASS — stores GPS
- Duplicate clock-in: PASS — correctly blocked
- POST /attendances/clock-out: PASS
- GET /attendances: PASS

### Approval Tasks (PASS)
- GET /approval-tasks: PASS — returns all pending tasks across types

### Settings (PASS)
- GET /settings: PASS
- PATCH /settings: PASS

## Security Test Results

### Authentication Protection (PASS)
- No token: PASS — 401 returned
- Invalid token: PASS — 401 returned
- Forged token (wrong signature): PASS — 401 returned

### Permission/Authorization (CRITICAL FAIL)
- ANY authenticated user can call ANY endpoint
- AppRouter.cs validates JWT but NEVER checks permissions claims
- Confirmed: viewer role created a role and a department successfully
- This is a fundamental authorization bypass affecting the entire API

### Data Isolation (PASS)
- User A cannot see User B's leave/overtime requests — scoped by JWT sub claim

### Superadmin Protection (PASS)
- Cannot be deleted
- Not shown in user list

## Boundary Condition Results

| Test | Result |
|------|--------|
| Empty string name | PASS — 400 returned |
| Null request body | PASS — 400 returned |
| Non-existent ID | PASS — 404 returned |
| Invalid ID format | PASS — 400 returned |
| Invalid JSON | PASS — 500 (could be 400) |
| Duplicate email | PASS — 409 conflict |
| Duplicate role ID | PASS — 409 conflict |
| Very large salary (999999999999999) | PASS — accepted (no max validation) |
| 1000-char string | FAIL — SQL truncation 500 (not 400) |
| SQL injection string | PASS — EF parameterized query prevents injection |
| XSS in text field | FAIL — stored as-is, no sanitization |
| endDate < startDate (leave) | PASS — 400 returned |
| Negative days (leave) | FAIL — accepted, stored |
| Invalid leaveType | FAIL — accepted, stored |
