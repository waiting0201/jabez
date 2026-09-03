# 認證系統

本文件記錄 Jabez 系統的 JWT 認證業務規格、登入流程、與 Superadmin 隱藏帳號規則。技術面實作（HS256 演算法、BCrypt 密碼雜湊、雙底線環境變數慣例）見 [backend-design.md §9](backend-design.md#9-jwt-認證)。

## JWT 規格

- 演算法：HS256
- Issuer：`jabez-api`
- Audience：`jabez-admin`
- 存取 Token 有效期：60 分鐘
- Refresh Token 有效期：7 天
- Claims：`sub`（使用者 ID）、`name`、`email`、`jti`、`roles`、`permissions`、`is_superadmin`、`department_name`、`department_code`、`job_title_name`、`job_title_level`、`avatar`

## 登入流程

1. `POST /auth/login` → 驗證帳密（BCrypt 密碼驗證）
2. 查詢使用者角色與權限
3. Superadmin：取得 DB 中所有權限
4. 一般使用者：取得角色對應權限
5. 產生 Access Token + Refresh Token
6. Refresh Token 存入 DB（`RefreshTokens` 資料表）→ **第一次 `SaveChangesAsync`**（登入主流程到此結束）
7. **自動補卡副作用** → 獨立的第二次 `SaveChangesAsync`（try/catch 吞掉 `DbUpdateException`）

登入是唯一能收斂「漏打卡」的時機，故 `/auth/login` 回應另夾帶三個補卡結果欄位
（皆為 `{ count, dates[] }`，無補卡時為 `null`），前端登入頁據此跳 toastr warning：

| 欄位 | 補了什麼 |
|---|---|
| `auto_clock_in` | 漏打的上班卡（該日有下班卡或加班卡＝人確實來過） |
| `auto_clock_out` | 漏打的下班卡 |
| `auto_overtime_end` | 漏打的加班結束卡 |

補卡規則（只填空欄、不建新列、避開請假時段）詳見
[attendance-clock-rules.md](business/attendance-clock-rules.md#登入時自動補卡漏打的歷史紀錄)。

> **副作用必須獨立交易**：補卡與 Refresh Token 共用同一次 `SaveChanges` 時，
> 同帳號併發登入撞唯一索引會讓整個登入回 500。見 [backend-design.md §6.3](backend-design.md#63-跨流程共用寫入核心不-savechanges-的-helper)。

> Token 過期處理由前端 [auth.interceptor.ts](../Admin/src/app/core/auth/interceptors/auth.interceptor.ts) 攔截 401 後自動呼叫 `/auth/refresh`，失敗則導向登入頁。

### ⚠️ 權限異動不會即時生效

`permissions` claims 是**登入 / refresh 當下**從 DB 重讀的快照（[AuthHandler.cs](../Api/Handlers/AuthHandler.cs) 的 `LoginAsync` 與 `RefreshAsync` 各有一份相同邏輯）。管理員在後台調整角色權限後：

- **舊 access token 在有效期內（60 分鐘）仍帶著舊權限** —— 加的權限用不到、收的權限擋不住。
- 攔截器**只在 401 觸發 refresh，403 不會**。所以「權限不足」的請求會一路失敗到 token 自然過期，最長一小時，不會自動修復。
- 立即生效的唯一方式是**請該使用者重新登入**。

因此**新增權限碼並在 Router 啟用檢查，屬於高風險部署**：舊 token 缺新碼會被擋。建議兩階段 —— 先只上「新增權限 + 回填角色」的 migration，跨過一個 token 週期後再上 Router 的檢查。

## Superadmin（隱藏帳號）

- **Email**：`sa@system.local`
- **密碼**：`Admin@123`（正式環境請立即變更）
- **GUID**：`00000000-0000-0000-0000-000000000001`
- `User.IsSuperAdmin = true`（由 [UserConfiguration.cs](../Api/Data/Configurations/UserConfiguration.cs) Seed）
- JWT 包含 `is_superadmin: true` claim，並帶有 DB 中所有權限
- 前端 `hasPermission()` 對 Superadmin 一律回傳 `true`
- 路由 / 選單 `permission: 'superadmin'` 代表僅 Superadmin 可見
- 使用者列表 SQL 過濾：`WHERE IsSuperAdmin = 0`
- Superadmin 無法被編輯或刪除（API 端強制阻擋）
- Mock login：dev 模式使用 `sa@system.local` 取得 Superadmin mock JWT

## 密碼規則

### 預設密碼

- **新增使用者** → 預設密碼為使用者出生日期 `yyyyMMdd`（八碼數字）；建立時不會自動標記強制改密碼
- **Seed Superadmin** → `Admin@123`（正式環境必須立即變更）
- 雜湊演算法：BCrypt（[BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net) NuGet）

### 修改密碼的觸發條件

系統目前**僅有兩種**觸發修改密碼的情境：

1. **使用者主動修改**
   - 入口：頂部 Profile Dropdown → 「修改密碼」
   - 路由：`/account/change-password`（無 `forced` 參數）
   - 必須已登入（`authGuard`）

2. **強制更換密碼（首次登入）**
   - 觸發點：管理員在使用者管理頁面執行「寄出帳號通知信」（[UserHandler.cs](../Api/Handlers/UserHandler.cs) `SendCredentialsAsync`）
   - 流程：
     1. 後端設 `User.MustChangePassword = true`，並寄發帳號通知信（不在信中明文揭露密碼，僅告知「預設密碼為生日 yyyyMMdd」推導規則）
     2. 員工以預設密碼登入，`/auth/login` 回應中夾帶 `must_change_password: true`
     3. 前端登入頁 ([login.ts](../Admin/src/app/features/auth/pages/login/login.ts)) 偵測該旗標，強制導至 `/account/change-password?forced=1`
     4. 修改成功後，後端自動把 `MustChangePassword` 改回 `false`

> 寄通知信前後端會檢查使用者是否填有生日，否則拒絕操作（無生日 → 無法生成預設密碼）。

### 修改密碼的驗證規則

| 規則 | 強制位置 | 備註 |
|------|---------|------|
| 舊密碼、新密碼皆為必填 | 前端 + 後端 | 後端缺欄位回 400 |
| 新密碼最少 **6 碼** | 前端 `Validators.minLength(6)` + 後端 [AuthHandler.cs](../Api/Handlers/AuthHandler.cs) `ChangePasswordAsync` | |
| 舊密碼必須正確（BCrypt.Verify） | 後端 | 錯誤回 400「舊密碼不正確。」 |
| 新密碼與確認密碼必須相同 | 前端 | 後端不檢查 |
| **新密碼可與舊密碼相同** | — | 系統不阻擋使用者把新密碼設為與舊密碼相同（前後端皆無此驗證） |

> 目前**未實作**：密碼複雜度（英數混用 / 大小寫 / 特殊字元）、密碼歷史檢查、密碼定期過期、忘記密碼 / 重設密碼流程。

## API 端點

| Method | Path | 說明 |
|--------|------|------|
| POST | `/auth/login` | 登入取得 JWT（公開路由） |
| POST | `/auth/refresh` | 刷新 Token（公開路由） |
| POST | `/auth/change-password` | 已登入使用者修改密碼（需 JWT） |
| POST | `/users/{id}/send-credentials` | 管理員寄帳號通知信並設置 `MustChangePassword = true` |

完整 API 路由清單見 [api-routes.md](api-routes.md)。

---

## 跨業務關聯

- **JWT 技術規範** → [backend-design.md §9](backend-design.md#9-jwt-認證)（HS256、BCrypt、環境變數雙底線）
- **JWT 在路由權限檢查的角色** → [backend-design.md §3.4 權限表](backend-design.md#34-權限表)
- **前端 Token 處理** → [auth.interceptor.ts](../Admin/src/app/core/auth/interceptors/auth.interceptor.ts)（自動附加 Bearer Token + 401 攔截）
- **權限管理頁面** → `/admin/permissions` 僅 Superadmin（前端 route guard + **API 端自 2026-08 起由 `AppRouter.IsSuperAdminRoute` 強制**：`POST/PUT/PATCH/DELETE /permissions[/{id}]` 與 `GET /permissions/{id}`；`GET /permissions` 列表刻意開放，角色編輯頁依賴它）。`/admin/roles` 走 `roles:read` / `roles:write` / `roles:delete`，**不是** Superadmin-only
- **JWT Claims 在前端的使用** → `auth.service.ts` 解碼 JWT 取 `roles` / `permissions` / `job_title_level` 等
