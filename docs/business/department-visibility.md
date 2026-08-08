# 部門可見性規則（原「專案可見性規則」）

部門可見性規則最早為「專案清單」設計，後擴充至**員工資料相關報表**（出缺勤紀錄、加班紀錄、款項統計、報表員工下拉）。底層由 `IProjectAccessResolver` 解析 `ProjectAccessScope(SeeAll, AllowedDepartmentIds)`，名稱保留「Project」字眼但**語意已是通用部門 scope**，套用於兩類過濾欄位：
- `Project.DepartmentId`（專案歸屬部門）
- `User.DepartmentId`（員工 / 申請人歸屬部門）

依優先序判定，第一個符合者即套用。

## 規則

| 優先序 | 使用者類別 | 可見範圍 |
|---|---|---|
| 1 | Superadmin | 全部 |
| 2 | `Department.CanSeeAll = true` 的部門成員 | 全部 |
| 3 | 一般員工 | 自己部門；若 `CanViewSiblings = true` 加同 ParentId 兄弟部門；若 `CanViewDescendants = true` 加所有遞迴下層子部門；若 `CanViewParent = true` 加直接父部門（不遞迴祖先） |

> **設計決策（CanSeeAll）**：原本 Rule 2 以寫死的 `DepartmentCodes.FinancialAndAbove`（`AC` / `FIN` / `Jabez HQ` / `CEO`）字串集合判定，2026-04 改為由 `Department.CanSeeAll` 旗標驅動，避免部門代碼變動時必須改程式重新部署。Migration `AddDepartmentVisibilityFlags` 已對既有 4 個財務 / 管理 / 總監部門 seed `CanSeeAll = 1`，行為不變。`DepartmentCodes.FinancialAndAbove` 常數**保留**供「撥款 / 退款 / 結案 / 批次核准」等業務操作權限使用（與可見性 SeeAll 屬不同概念）。
>
> **設計決策（CanViewSiblings）**：只擴及**同層兄弟部門**，**父部門本身不可見**。父部門通常是管理單位（如總監室），其專案屬於管理層級資料，不應對下層子部門開放。
>
> **設計決策（CanViewDescendants）**：擴及**本部門 + 所有遞迴後代部門**，可與 `CanViewSiblings` 併用（聯集 = 同層兄弟 ∪ 所有下層）。實作採記憶體 DFS 遍歷（`ProjectAccessResolver.GetDescendantIdsAsync`），避免引入 SQL CTE；部門表筆數小成本可接受。
>
> **設計決策（CanViewParent）**：只擴及**直接父部門**（`ParentId` 指到的那一個），**不遞迴向上找祖先**。理由：與「同層兄弟（一層）」對稱，避免基層員工意外看到 CEO/總監室層級的資料；若未來需要遞迴祖先再開另一個旗標。頂層部門（`ParentId = null`）啟用此旗標時不擴展、不報錯。可與 `CanViewSiblings` / `CanViewDescendants` 任意組合，皆採聯集。Migration `AddCanViewParentToDepartment`（2026-04-30）僅新增欄位 default false，不 seed 任何部門。

## 套用端點

**過濾鍵：`Project.DepartmentId`**
- `GET /projects/active`（申請表單下拉，僅 `Status = 'active'`）
- `GET /projects`（專案管理列表 / 分頁）
- `GET /projects/{id}`（單筆詳情；不符 scope 回 404）
- `GET /reports/project-water-level`（專案水位表）
  - ⚠️ 部門可見性只決定「看得到**哪些專案**（列）」；「總專案水位」**欄**另受欄位級權限 `reports-project-water-level:total` 控制（見 [backend-design.md §欄位級權限](../backend-design.md)）。兩者正交，缺任一都會少東西看。

**過濾鍵：`User.DepartmentId`（員工 / 申請人歸屬部門）**
- `GET /attendances`（出缺勤紀錄報表，JOIN `Users` 後過濾；支援 `dateFrom / dateTo` 區間篩選）
- `GET /reports/overtime`（加班紀錄報表，JOIN `Users` 後過濾；支援 `dateFrom / dateTo` 區間篩選）
- `GET /reports/payment`（款項統計報表，JOIN `Users` 後過濾；支援 `dateFrom / dateTo` 區間篩選）
- `GET /reports/payment/export`（款項統計匯出 = 一張發票一列，與 `/reports/payment` 共用同一 `BuildWhereAndParameters`，部門 scope 過濾邏輯一致）
- `GET /users/lookup?scope=department`（報表員工下拉，**不帶 `scope` 參數時維持原行為，回傳全公司**）

## 前置必要條件（資料完整性）

- `Project.DepartmentId` 必填（DB NOT NULL + 前後端驗證；FK `DeleteBehavior.Restrict`）
- `User.DepartmentId` 必填（Superadmin 例外；前後端均驗證）
- `Department.CanSeeAll` / `CanViewSiblings` / `CanViewDescendants` / `CanViewParent` 預設皆 false，由部門 CRUD 頁維護

## 涉及元件

| 元件 | 說明 |
|---|---|
| `Department.CanSeeAll` | Entity 旗標，勾選後該部門成員擁有 SeeAll；取代原寫死的 `DepartmentCodes.FinancialAndAbove` 判定 |
| `Department.CanViewSiblings` | Entity 旗標，勾選後可見同 ParentId 兄弟部門 |
| `Department.CanViewDescendants` | Entity 旗標，勾選後可見本部門 + 所有遞迴下層子部門 |
| `Department.CanViewParent` | Entity 旗標，勾選後可見直接父部門（不遞迴祖先）；頂層部門啟用時不擴展 |
| `Api/Common/Constants.cs` `DepartmentCodes.FinancialAndAbove` | 財務體系部門 Code 集合，**僅供「撥款 / 退款 / 結案 / 批次核准」業務操作權限使用**，不再參與可見性判定。同時涵蓋舊短碼（`AC` / `FIN` / `Jabez HQ` / `CEO`）與 2026 改制後英文全名碼（`Accounting Department` / `Financial Management Department` / `Office of the Director`），改組織不致失效；前端對應 `approval-task-list.ts` 的 `PAYMENT_FILTER_DEPT_CODES`，兩處須同步 |
| `Api/Services/IProjectAccessResolver` + `ProjectAccessResolver` | 解析 ClaimsPrincipal + DB 旗標 → `ProjectAccessScope(SeeAll, AllowedDepartmentIds)` |
| `ProjectAccessResolver.GetDescendantIdsAsync` | 載入全部 Departments 後在記憶體 DFS 遍歷取得遞迴後代 Id；含 visited HashSet 防呆循環依賴 |
| `Api/Services/Dapper/ProjectReadService` | 四個讀取方法皆依 scope 組合 WHERE（`DepartmentId IN @AllowedIds` 或 `1=0`） |
| `Api/Services/Dapper/ProjectWaterLevelReadService` | 專案水位表同樣依 scope 組合 WHERE |
| `Api/Services/Dapper/AttendanceReadService` | 出缺勤列表 SQL 改 `INNER JOIN Users u`，加 `u.DepartmentId IN @AllowedDeptIds` 子句 |
| `Api/Services/Dapper/OvertimeReportReadService` | 加班報表 SQL 改 `INNER JOIN Users u`，加同上子句 |
| `Api/Services/Dapper/PaymentReportReadService` | 請款報表 SQL 既有 `JOIN Users u ON pr.SubmittedById = u.Id`，加同上子句 |
| `Api/Services/Dapper/UserReadService.GetLookupAsync(scope)` | 員工 lookup 加 `WHERE DepartmentId IN @AllowedDeptIds` |
| `Api/Handlers/ProjectHandler` | 所有 GET 先呼叫 resolver；寫入後以 SeeAll scope 讀回避免寫入者讀不到自己的資料 |
| `Api/Handlers/ProjectWaterLevelHandler` | GET 先呼叫 resolver 取 scope 再傳給 reader |
| `Api/Handlers/AttendanceHandler` / `OvertimeReportHandler` / `PaymentReportHandler` | GET 先呼叫 resolver 取 scope 再傳給 reader |
| `Api/Handlers/UserHandler.GetLookupAsync` | 接 `?scope=department` 時呼叫 `reader.GetLookupAsync(scope)`；不帶參數維持原行為 |
| JWT `department_id` claim | Resolver 用以查詢該部門所有可見性旗標（CanSeeAll / CanViewSiblings / CanViewDescendants / CanViewParent）|
| `Api/Routing/AppRouter` | JWT 驗證後將 principal 寫入 `HttpContext.User`，供 Handler 經 `IHttpContextAccessor` 取得 |

## 6 個申請表單的下拉空值提示

當使用者的可見專案清單為空時，下拉下方顯示灰字「您目前可申請的專案清單為空，請聯絡主管或確認部門設定。」：

- [payment-form](../../Admin/src/app/features/admin/payment-requests/pages/payment-form/payment-form.html)
- [advance-form](../../Admin/src/app/features/admin/advance-requests/pages/advance-form/advance-form.html)
- [overtime-request-form](../../Admin/src/app/features/admin/overtime-requests/pages/overtime-request-form/overtime-request-form.html)
- [travel-request-form](../../Admin/src/app/features/admin/travel-requests/pages/travel-request-form/travel-request-form.html)
- [travel-payment-form](../../Admin/src/app/features/admin/travel-payment-requests/pages/travel-payment-form/travel-payment-form.html)
- [holiday-travel-request-form](../../Admin/src/app/features/admin/holiday-travel-requests/pages/holiday-travel-request-form/holiday-travel-request-form.html)

## 不套用過濾的端點（維持原行為）

- `/approval-tasks`（申請單既有列表過濾已足夠隔離）
- `/payroll`（人事薪資顯示 projectCode）
- `/users`、`/users/lookup`（不帶 `?scope=department` 時）— 全公司可見，供管理頁與指定審核者下拉使用
- `/projects/years`（僅回傳年份不洩漏明細）
- 各申請列表（`/payment-requests`、`/leave-requests`、`/overtime-requests` 等）— 各自有「我自己 / Superadmin」邏輯

---

## 跨業務關聯

- **撥款 / 退款 / 結案 / 批次核准權限** → [approval-flow.md](approval-flow.md)（用 `DepartmentCodes.FinancialAndAbove` 而非可見性 scope）
- **後端 Resolver 技術實作** → [backend-design.md §14 部門可見性](../backend-design.md#14-部門可見性project-access-scope)
- **Department Entity 旗標** → [database-schema.md](../database-schema.md)
- **報表端點清單** → [api-routes.md §報表](../api-routes.md#報表reports)
