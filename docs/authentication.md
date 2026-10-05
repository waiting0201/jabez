# 認證系統

本文件記錄 Jabez 系統的 JWT 認證業務規格、登入流程、與 Superadmin 隱藏帳號規則。技術面實作（HS256 演算法、BCrypt 密碼雜湊、雙底線環境變數慣例）見 [backend-design.md §9](backend-design.md#9-jwt-認證)。

## JWT 規格

- 演算法：HS256
- Issuer：`jabez-api`
- Audience：`jabez-admin`
- 存取 Token 有效期：60 分鐘
- Refresh Token 有效期：7 天（每次輪替重算），**另有絕對期限 30 天**（見下方〈Refresh Token 政策〉）
- Claims：`sub`（使用者 ID）、`name`、`email`、`jti`、`roles`、`permissions`、`is_superadmin`、`department_name`、`department_code`、`job_title_name`、`job_title_level`、`avatar`、**`pwd_change_required`**（僅尚未完成強制改密碼者帶 `"true"`，見〈強制改密碼〉）

## 登入流程

1. `POST /auth/login` → **鎖定檢查**（見〈登入失敗鎖定〉）→ 驗證帳密（BCrypt 密碼驗證）→ 寫入登入嘗試紀錄
2. 查詢使用者角色與權限
3. Superadmin：取得 DB 中所有權限
4. 一般使用者：取得角色對應權限
5. 產生 Access Token + Refresh Token
6. Refresh Token 存入 DB（`RefreshTokens` 資料表，`SessionStartedAt` ＝ 本次登入時間）＋ 成功的登入嘗試紀錄 → **第一次 `SaveChangesAsync`**（登入主流程到此結束）
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

## 登入失敗鎖定與登入紀錄

常數集中於 [AuthPolicy.cs](../Api/Common/AuthPolicy.cs)；判定與寫入收斂在 [LoginAttemptTracker.cs](../Api/Services/LoginAttemptTracker.cs)。

- **規則**：同一個 Email 在最近 `LockoutMinutes`（15）分鐘內、自最近一次成功登入之後，累積 `MaxFailedLogins`（5）次失敗（密碼錯誤 / 帳號不存在）→ 鎖到「最後一次失敗 + 15 分鐘」。成功登入歸零。
- **鎖定中**回 **HTTP 429**，訊息「登入失敗次數過多，帳號已暫時鎖定，請於 N 分鐘後再試。」，連密碼都不驗；鎖定期間的嘗試以 `locked` 記錄但**不計入**失敗數（否則鎖定會被無限延長）。
- **以 Email 為鍵、含不存在的帳號**：若只鎖真實帳號，攻擊者可由「第 5 次起出現鎖定訊息」得知某 Email 存在。以 Email 為鍵則存在與否行為一致。代價：任何人可對某 Email 連錯 5 次把該員鎖 15 分鐘（固有取捨，管理員無需介入、15 分鐘自解）。
- **不以 IP 為鎖定鍵**：`X-Forwarded-For` 可偽造、辦公室共用出口 IP 會連坐；IP 僅記錄供稽核。
- **時序側信道（timing side channel）防護**：帳號不存在（或密碼空白）時仍對一個啟動時產生的假雜湊跑一次 `BCrypt.Verify`，回應時間與「密碼錯誤」一致。
- **登入嘗試紀錄 `LoginAttempts`**：每次嘗試一列（Email / UserId? / 成功與否 / 失敗原因 `bad_password` `unknown_email` `inactive` `locked` / IP / User-Agent / 時間）。`UserId` 不建外鍵（帳號不存在時本就沒有，且使用者硬刪除後稽核紀錄要保留）。本階段**無查詢畫面、無自動清除**，需要時直接查表。

## 強制改密碼（後端強制）

`User.MustChangePassword = true` 時，`/auth/login` 與 `/auth/refresh` 簽發的 Access Token 會帶 `pwd_change_required: "true"` claim。

- **後端**：[AppRouter.cs](../Api/Routing/AppRouter.cs) 驗完 JWT、做任何權限檢查之前，若 token 帶此 claim，除 `POST /auth/change-password` 外**一律 403**（訊息「首次登入必須先修改密碼…」）。`/auth/refresh`、`/auth/logout` 為公開路由，不受影響。刻意不放行任何讀取端點。
- **前端**：[password-change.guard.ts](../Admin/src/app/core/auth/guards/password-change.guard.ts)（`MainLayout` 的 `canActivateChild`）讀同一個 claim，把使用者鎖在 `/account/change-password?forced=1`。
- **設為 true 的時機**：`UserHandler.CreateAsync`（一律，不論預設生日密碼或管理員自填）、管理員透過 Update 設定**他人**密碼、`SendCredentialsAsync`、員工匯入工具。管理員改**自己**的密碼不算。
- **清為 false 的時機**：本人 `ChangePasswordAsync` 成功。
- ⚠️ 旗標只在簽發 token 時讀取；管理員事後才把某人設為 true 時，對方手上尚未過期的 Access Token（≤60 分鐘）不帶 claim，但同一動作會撤銷其 Refresh Token，故最遲 60 分鐘後必須重新登入而被攔下。

## Refresh Token 政策

| 項目 | 規則 |
|---|---|
| 輪替 | 每次 `/auth/refresh` 撤銷舊 token、簽發新 token；新 token 沿用舊 token 的 `SessionStartedAt` |
| 絕對期限 | `SessionStartedAt` + `RefreshAbsoluteDays`（30）天後一律 401「Session expired」，必須重新輸入密碼（輪替不延長） |
| 重用偵測 | 已撤銷的 token 再次出現：撤銷時間在 `RefreshReuseGraceSeconds`（30）秒內視為多分頁同時 refresh 的競態，只回 401；超過則視為被盜用，**撤銷該使用者全部 token** |
| 登出 | `POST /auth/logout`（公開路由）撤銷傳入的 refresh token，一律回 200；前端登出時 fire-and-forget 呼叫，失敗不阻擋本機登出 |
| 一律撤銷全部 | 改密碼、管理員設定 / 重設他人密碼、寄帳號通知信、帳號停用（Status → inactive）、使用者角色變更 |

撤銷的單一入口：[RefreshTokenRevoker.RevokeAllAsync](../Api/Services/RefreshTokenRevoker.cs)（一次 `ExecuteUpdate`，不依賴 `SaveChanges`）。
⚠️ **只能讓 Refresh Token 立即失效**：已簽發的 Access Token（60 分鐘）無法收回，因為 `AppRouter` 驗 JWT 是無狀態的、不查 DB。

## 簽名檔需登入

`GET /files/signatures/{fileName}` **自 2026-10 起需登入**（原為公開路由）：檔名＝userId 可推導，公開等於任何人皆可蒐集全公司簽名圖。登入即可、免特殊權限。`/files/avatars/{fileName}` 仍為公開。前端取用方式見 [frontend-design.md §12.4](frontend-design.md)。

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

- **新增使用者** → 預設密碼為使用者出生日期 `yyyyMMdd`（八碼數字）；建立時**一律標記強制改密碼**（`MustChangePassword = true`）
- **Seed Superadmin** → `Admin@123`（正式環境必須立即變更）
- 雜湊演算法：BCrypt（[BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net) NuGet）

### 修改密碼的觸發條件

1. **使用者主動修改**
   - 入口：頂部 Profile Dropdown → 「修改密碼」
   - 路由：`/account/change-password`（無 `forced` 參數）
   - 必須已登入（`authGuard`）

2. **強制更換密碼（首次登入 / 被管理員重設）**
   - 觸發點：新增使用者；管理員於使用者管理頁設定他人密碼；管理員執行「寄出帳號通知信」（[UserHandler.cs](../Api/Handlers/UserHandler.cs) `SendCredentialsAsync`）
   - 流程：
     1. 後端設 `User.MustChangePassword = true`（寄通知信者另寄信，不在信中明文揭露密碼，僅告知「預設密碼為生日 yyyyMMdd」推導規則）
     2. 員工以預設密碼登入，token 帶 `pwd_change_required` claim，`/auth/login` 回應夾帶 `must_change_password: true`
     3. 前端登入頁與全域 `passwordChangeGuard` 導至 `/account/change-password?forced=1`；**後端在此期間只放行改密碼端點**（見〈強制改密碼〉）
     4. 修改成功後，後端把 `MustChangePassword` 改回 `false` 並撤銷該員全部 refresh token，前端登出、請員工重新登入取得不帶 claim 的新 token

> 寄通知信前後端會檢查使用者是否填有生日，否則拒絕操作（無生日 → 無法生成預設密碼）。

### 修改密碼的驗證規則

| 規則 | 強制位置 | 備註 |
|------|---------|------|
| 舊密碼、新密碼皆為必填 | 前端 + 後端 | 後端缺欄位回 400 |
| 新密碼最少 **8 碼**（`AuthPolicy.PasswordMinLength`） | 前端 `Validators.minLength(8)` + 後端 `ChangePasswordAsync` | |
| 舊密碼必須正確（BCrypt.Verify） | 後端 | 錯誤回 400「舊密碼不正確。」 |
| 新密碼不可與舊密碼相同 | 前端 + 後端 | |
| 新密碼不可等於生日八碼 `yyyyMMdd` | 後端 | 預設密碼是公開推導規則，改成它等於沒改 |
| 新密碼與確認密碼必須相同 | 前端 | 後端不檢查 |

> 管理員於使用者表單設定的（臨時）密碼仍為最少 6 碼：該密碼設定後對方必須強制改掉，不另提高。
> 目前**未實作**：密碼複雜度（英數混用 / 大小寫 / 特殊字元）、密碼歷史檢查、密碼定期過期、忘記密碼 / 重設密碼流程。

## 使用者管理的角色指派限制

`UserHandler` 的 `CreateAsync` / `UpdateAsync` 指派角色時（`EnsureCanAssignRolesAsync`）：

- 指定的角色必須存在（否則 400）。
- **Superadmin** 不受限。
- **非 Superadmin 不可修改自己的角色**（新增或移除皆 403）。
- **非 Superadmin 新增的每個角色，其全部權限必須是操作者自身權限的子集合**（比對 JWT `permissions` claim），否則 403 並列出缺少的權限。目標使用者原本就有的角色不重驗；移除角色屬降權，不檢查。
- 角色有變動（新增或移除）→ 撤銷該使用者全部 refresh token，逼其重新登入取得新權限快照。
- ⚠️ 角色**本身**的權限內容由 `PUT /roles/{id}`（`roles:write`）修改，目前**不受**上述子集合限制 —— 持有 `roles:write` 者仍可替角色加權限，視同該權限碼為高度敏感，請只授予可信任者。

## API 端點

| Method | Path | 說明 |
|--------|------|------|
| POST | `/auth/login` | 登入取得 JWT（公開路由） |
| POST | `/auth/refresh` | 刷新 Token（公開路由） |
| POST | `/auth/logout` | 撤銷傳入的 refresh token（公開路由，一律 200） |
| POST | `/auth/change-password` | 已登入使用者修改密碼（需 JWT；強制改密碼期間唯一放行的端點） |
| POST | `/users/{id}/send-credentials` | 管理員寄帳號通知信、設置 `MustChangePassword = true` 並撤銷其 refresh token |

完整 API 路由清單見 [api-routes.md](api-routes.md)。

---

## 跨業務關聯

- **JWT 技術規範** → [backend-design.md §9](backend-design.md#9-jwt-認證)（HS256、BCrypt、環境變數雙底線）
- **JWT 在路由權限檢查的角色** → [backend-design.md §3.4 權限表](backend-design.md#34-權限表)
- **前端 Token 處理** → [auth.interceptor.ts](../Admin/src/app/core/auth/interceptors/auth.interceptor.ts)（自動附加 Bearer Token + 401 攔截）
- **權限管理頁面** → `/admin/permissions` 僅 Superadmin（前端 route guard + **API 端自 2026-08 起由 `AppRouter.IsSuperAdminRoute` 強制**：`POST/PUT/PATCH/DELETE /permissions[/{id}]` 與 `GET /permissions/{id}`；`GET /permissions` 列表刻意開放，角色編輯頁依賴它）。`/admin/roles` 走 `roles:read` / `roles:write` / `roles:delete`，**不是** Superadmin-only
- **JWT Claims 在前端的使用** → `auth.service.ts` 解碼 JWT 取 `roles` / `permissions` / `job_title_level` 等
