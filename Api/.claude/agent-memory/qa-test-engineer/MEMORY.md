# QA Test Engineer Memory - Jabez API

## Project Key Files
- Router: `Api/Routing/AppRouter.cs` — permission enforcement via `GetRequiredPermission()` + `RequirePermission()`
- Auth: `Api/Handlers/AuthHandler.cs` — JWT generation, permissions from DB
- Seed: `Api/Data/Configurations/RolePermissionConfiguration.cs`, `PermissionConfiguration.cs`
- Migrations: `Api/Data/Migrations/`

## Known Patterns and Defect History

### W-5 Attendance RolePermissions — RESOLVED (2026-03-02)
- Root cause confirmed: `20260302030805_AddAttendancePermissions` was applied before RolePermissions INSERT was in Up().
- Fix: New migration `20260302031823_AddAttendanceRolePermissions` inserts 5 rows (admin/37, admin/38, manager/37, manager/38, viewer/37).
- DB confirmed: 5 rows present in RolePermissions for PermissionId IN ('37','38').
- Both migrations listed in __EFMigrationsHistory with ProductVersion 9.0.1.
- Bob (manager) JWT now contains attendances:read AND attendances:write.
- All 3 attendance endpoints tested successfully with Bob's token (2026-03-02):
  - GET /api/attendances/today → 200
  - GET /api/attendances → 200 (returns paginated list)
  - POST /api/attendances/clock-in → 200 (新建 id=1003, GPS 25.033/121.5654)
- Lesson learned: When a migration is already applied, RolePermissions data additions require a separate NEW migration.

### Carol Liu Status Discrepancy (2026-03-02) — ANOMALY
- Seed data: `UserConfiguration.cs` line 115 shows Carol `Status = "inactive"`
- Actual behavior: Carol can successfully log in (Status check passes)
- Possible cause: DB was not re-seeded after the Status field was set to inactive, or migration changed her status.
- Impact: Auth enforcement for inactive accounts appears broken for Carol.

### F-1 "returned" State — VERIFIED WORKING
- `PaymentRequestHandler.UpdateAsync` line 151 correctly checks `!= "draft" && != "returned"`
- `PaymentRequestHandler.SubmitAsync` line 297 correctly checks `!= "draft" && != "returned"`
- Pending PR update correctly returns 400.

## Viewer Role (viewer) Permissions
DB-confirmed (2026-03-02 after fix): users:read, roles:read, permissions:read, departments:read, job-titles:read,
approvals:read, projects:read, payment-requests:read, approval-tasks:read,
leave-requests:read, travel-requests:read, overtime-requests:read, attendances:read (id=37) — NOW IN DB

## Test Users
- Superadmin: sa@system.local / any password
- Alice Chen: alice@example.com / admin role
- Bob Wang: bob@example.com / manager role
- Carol Liu: carol@example.com / viewer role (but Status=inactive in seed — verify DB state)
