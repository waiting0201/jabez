# 請款簽核及工時管理系統 - CLAUDE.md

## 專案概述

本系統為企業內部的**請款簽核系統**與**請假/出差/加班申請管理系統**，提供費用申請流程簽核、員工資料管理、角色與權限控管、審核任務追蹤、**出勤打卡**（含 GPS 定位）等功能。

---

## 專案結構

```
/
├── Admin/          # 前端 Angular 21 應用程式
├── Api/            # 後端 Azure Functions .NET 9 API
└── Jabez.sln       # Visual Studio 方案檔
```

---

## 優先執行事項

每次收到 UI 相關任務時，**必須優先啟動 `frontend-design` skill**，再進行任何設計或實作。

### 有提供參考圖時：
- 必須**完全匹配**佈局、間距、字體排版與顏色
- 圖片使用 `https://placehold.co/` 佔位
- 文案使用通用佔位文字
- **切勿自行改良或增加設計**

### 無參考圖時：
- 從零開始設計，遵循高工藝標準（見防庸俗護欄）

### 截圖比對流程：
- 截圖輸出結果，與參考圖對照
- 修正差異後重新截圖
- **至少進行 2 輪比對**
- 直到看不出差異或使用者喊停為止

### CLAUDE的對話內容：
- 使用繁體中文

## Debug重要注意事項

### 權限功能：
- 需檢查使用者可使用的權限
- 使用者只能看到自己的資料
- 有審核權限的使用者，只能看到自己的資料

### Api程式：
- 程式需要可以建置正常
- 最新的migration

---

## Agent 分工機制

本專案使用 9 個專業化 Agent，依任務性質自動分派或手動指定。每個 Agent 擁有獨立的上下文與專屬工具，可並行執行以提升效率。

| # | Agent | 職責 | 適用場景 |
|---|-------|------|----------|
| 1 | **Explore** | 快速探索 codebase | 搜尋檔案、搜尋關鍵字、理解程式架構（如「API 端點怎麼運作？」） |
| 2 | **Plan** | 設計實作計畫 | 規劃功能實作步驟、識別關鍵檔案、評估架構取捨 |
| 3 | **frontend-architect** | 前端開發 | Angular、TypeScript、HTML、CSS、元件設計、狀態管理、路由、效能優化 |
| 4 | **backend-engineer** | 後端開發 | C# API 設計與實作、EF Core / Dapper、SQL 優化、Azure 部署、安全性審查 |
| 5 | **system-analyst** | 系統分析與技術文件 | 分析產品藍圖、產出系統架構設計、資料庫 Schema、API 規格文件 |
| 6 | **software-architect-blueprint** | 產品藍圖與需求分析 | 分析軟體需求、設計使用者流程、定義系統架構、產出開發路線圖 |
| 7 | **visual-design-architect** | UI/UX 設計 | 版型規劃、Wireframe、視覺層級設計、設計系統建議、元件佈局 |
| 8 | **code-review-optimizer** | 程式碼審查與優化 | Code Review、重構建議、效能優化、識別 Code Smell、設計模式改善 |
| 9 | **qa-test-engineer** | QA 測試與品質驗證 | 檢視程式碼錯誤、審查潛在 Bug、邊界條件檢查、使用測試資料進行 CRUD 功能測試（須符合系統邏輯）、品質問題識別 |

### 使用原則

- **並行執行**：獨立的任務可同時啟動多個 Agent（如前端 + 後端同步開發）
- **探索優先**：不確定程式架構時，先用 **Explore** 了解再動手
- **規劃先行**：非簡單任務（3 步以上），先用 **Plan** 產出實作計畫取得確認
- **專業分工**：前端任務交 **frontend-architect**、後端任務交 **backend-engineer**
- **品質把關**：重要功能完成後，用 **code-review-optimizer** 做 Code Review，用 **qa-test-engineer** 驗證品質與邊界條件

### 常見任務流程

```
新功能開發：
  Explore（了解現有架構）→ Plan（規劃實作步驟）→ frontend-architect / backend-engineer（實作）→ code-review-optimizer（審查）→ qa-test-engineer（品質驗證）

UI 頁面設計：
  visual-design-architect（設計版型）→ frontend-architect（實作元件）

系統規劃：
  software-architect-blueprint（需求分析）→ system-analyst（技術文件）→ Plan（實作計畫）

Bug 修復：
  Explore（定位問題）→ frontend-architect / backend-engineer（修復）
```

---

## CIS 企業識別色彩規範

所有 UI 與 PDF 輸出須遵循以下品牌色彩。Design Tokens 定義於 `Admin/src/tailwind.css` `:root`，PDF 用 RGB 常數定義於 `payroll-list.ts` 的 `CIS` 物件。

### 品牌主色

| Token | 色碼 | 用途 |
|-------|------|------|
| `--forest` | `#699F34` | 品牌綠：按鈕、標題、表頭、PDF 裝飾線 |
| `--forest-mid` | `#4A6B3A` | 中綠：hover 狀態、次要強調 |
| `--forest-light` | `#6B8F5E` | 淺綠：輔助色 |

### 中性色（炭灰系）

| Token | 色碼 | 用途 |
|-------|------|------|
| `--text-primary` | `#525358` | 正文、標題、表格文字（CIS 炭灰） |
| `--text-secondary` | `#6E6F73` | 標籤、次要文字 |
| `--text-muted` | `#A39685` | 註解、浮水印、輔助說明 |

### 強調色（暖棕系）

| Token | 色碼 | 用途 |
|-------|------|------|
| `--accent` | `#8C7355` | 連結、焦點框、互動元素 |
| `--accent-muted` | `#735E42` | 深棕變體 |

### 語意色

| Token | 色碼 | 用途 |
|-------|------|------|
| `--green` | `#4A6B3A` | 成功 |
| `--yellow` | `#B8892A` | 警告 |
| `--red` | `#A04040` | 錯誤、扣款表頭 |
| `--purple` | `#7C5E8C` | 資訊 |

### 背景與邊框

| Token | 色碼 | 用途 |
|-------|------|------|
| `--bg-base` | `#F5F2ED` | 頁面底色 |
| `--bg-surface` | `#FDFAF5` | 卡片、面板 |
| `--bg-elevated` | `#EDE9E1` | 提升區塊 |
| `--border` | `#DDD6C8` | 邊框 |

### 側欄

| Token | 色碼 | 用途 |
|-------|------|------|
| `--sidebar-bg` | `#699F34` | 側欄背景（品牌綠） |
| `--sidebar-surface` | `#5B8E2D` | 深一階（子選單底） |
| `--sidebar-hover` | `#78AD42` | hover 回饋 |
| `--sidebar-text` | `rgba(255,255,255,0.92)` | 選單文字 |
| `--sidebar-text-dim` | `rgba(255,255,255,0.58)` | 分類標題 |

### Logo 檔案

| 檔案 | 格式 | 用途 |
|------|------|------|
| `assets/img/logo.png` | PNG（透明背景、直式） | 網頁 UI：Topbar、Login 頁 |
| `assets/img/logo.jpg` | JPG（橫式含公司全名） | PDF 薪資明細表抬頭 |

---

## 前端：Admin（Angular 21以上）

### 技術棧

- **框架**：Angular 21.1
- **語言**：TypeScript 5.9.2
- **樣式**：Tailwind CSS v4（`src/tailwind.css` — @layer base/components/utilities）+ SCSS（只用於 component-level scoping）
- **狀態管理**：Angular Signals
- **HTTP 通訊**：Angular HttpClient
- **路由**：Angular Router（Lazy Loading）
- **Table**：@tanstack/angular-table
- **圖表**：ApexCharts（ng-apexcharts）
- **通知**：ngx-toastr
- **PDF 匯出**：jsPDF + jspdf-autotable

### 目錄結構

```
Admin/src/app/
├── core/
│   ├── auth/
│   │   ├── services/
│   │   │   └── auth.service.ts           # JWT 解碼、Signal 狀態、Mock 登入、權限判斷
│   │   ├── guards/
│   │   │   ├── auth.guard.ts             # 保護需登入路由
│   │   │   ├── no-auth.guard.ts          # 阻止已登入者進入登入頁
│   │   │   └── permission.guard.ts       # 權限判斷守衛
│   │   └── interceptors/
│   │       ├── auth.interceptor.ts       # 自動附加 Bearer Token
│   │       └── api-response.interceptor.ts
│   └── layout/
│       ├── services/
│       └── models/
├── layout/
│   ├── auth-layout/
│   ├── main-layout/
│   ├── content-with-right-panel/
│   └── components/
│       ├── sidenav/
│       ├── topbar/
│       ├── footer/
│       └── customizer/
└── features/
    ├── dashboard/              # 打卡系統（即時時鐘、上下班/加班打卡、GPS）
    │   ├── models/attendance.model.ts
    │   ├── services/attendance.service.ts
    │   └── pages/dashboard/
    ├── auth/
    │   └── pages/ (login, register, forgot-password, lock-screen, two-factor)
    ├── admin/
    │   ├── users/          # 使用者管理
    │   ├── roles/          # 角色管理（僅 Superadmin）
    │   ├── permissions/    # 權限管理（僅 Superadmin）
    │   ├── departments/    # 部門管理
    │   ├── job-titles/     # 職稱管理
    │   ├── approvals/      # 簽核流程設定（ApprovalItem + Steps）
    │   ├── approval-tasks/ # 待審核任務清單
    │   ├── projects/       # 專案管理
    │   ├── payment-requests/  # 請款申請
    │   ├── leave-requests/    # 請假申請
    │   ├── travel-requests/   # 出差申請
    │   ├── overtime-requests/ # 加班申請（走簽核流程）
    │   ├── insurance-brackets/ # 勞健保級距維護
    │   ├── payroll/           # 人事薪資（月薪計算 + PDF 匯出）
    │   └── settings/       # 系統設定
    └── error/
        └── pages/ (error-403, error-404, error-500)
```

### 開發規範

- 使用 **Standalone Components**（不使用 NgModule）
- 路由採用 **Lazy Loading**，每個 feature 獨立載入
- HTTP 請求統一透過 `features/<module>/services/` 中的 Service 呼叫
- 使用 **Angular Signals** 管理認證狀態
- 所有 API 路徑統一在 `environments/environment.ts` 的 `apiUrl` 管理
- Token 儲存於 `localStorage`，由 `auth.interceptor.ts` 自動附加 Bearer Token

### 常用指令

```bash
cd Admin
npm install               # 安裝相依套件
ng serve                  # 本地開發（預設 http://localhost:4200）
ng build --configuration production  # 正式環境建置
```

### environment.ts

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:7071/api'
};
```

### 建置輸出

- `outputPath: "dist/Admin"` → Angular 輸出至 `dist/Admin/browser/`
- 勿設定 `outputPath: "dist/Admin/browser"`（會造成 `browser/browser/` 巢狀）

---

## 後端：Api（Azure Functions .NET 9）

### 技術棧

- **平台**：Azure Functions v4（Isolated Worker Model）
- **框架**：.NET 9
- **ORM**：EF Core（Migration、CRUD）+ Dapper（效能敏感讀取）
- **資料庫**：SQL Server（本地：`JabezDb`）
- **認證**：JWT Bearer Token（HS256）
- **路由**：單一入口 RouterFunction → AppRouter（C# 12 List Pattern）

### 目錄結構

```
Api/
├── Functions/
│   └── RouterFunction.cs              # 唯一 HttpTrigger，catch-all route {*route}
├── Routing/
│   └── AppRouter.cs                   # C# 12 List Pattern 路由分派器
├── Handlers/                          # 18 個 Handler（業務邏輯）
│   ├── AuthHandler.cs                 # 登入、刷新 Token
│   ├── UserHandler.cs
│   ├── RoleHandler.cs
│   ├── PermissionHandler.cs
│   ├── DepartmentHandler.cs
│   ├── JobTitleHandler.cs
│   ├── ApprovalHandler.cs             # ApprovalItem + Steps CRUD
│   ├── ApprovalTaskHandler.cs         # 待審核任務查詢與審核動作
│   ├── ProjectHandler.cs
│   ├── PaymentRequestHandler.cs
│   ├── LeaveRequestHandler.cs
│   ├── TravelRequestHandler.cs
│   ├── OvertimeRequestHandler.cs      # 加班申請 CRUD
│   ├── AttendanceHandler.cs           # 打卡（上班/下班/加班開始/加班結束）
│   ├── InsuranceBracketHandler.cs    # 勞健保級距 CRUD
│   ├── PayrollHandler.cs             # 人事薪資查詢（月薪計算）
│   ├── SettingsHandler.cs
│   └── HealthHandler.cs
├── Middleware/
│   └── ExceptionMiddleware.cs         # 全域例外處理
├── Data/
│   ├── AppDbContext.cs                # EF Core DbContext（含 Migration 自動套用）
│   ├── AppDbContextFactory.cs         # 用於 CLI Migration
│   ├── Configurations/                # EF Core 實體對應設定（20 個）
│   └── Migrations/                    # EF Core Migration 檔案
├── Models/
│   ├── Entities/                      # 21 個資料庫實體
│   └── Dtos/                          # 16 個 DTO 檔案
├── Services/
│   ├── IJwtService.cs
│   ├── JwtService.cs                  # HS256 JWT 產生與驗證
│   ├── IEscalationService.cs          # 簽核升級服務介面
│   ├── EscalationService.cs           # 簽核升級邏輯（上層部門主管遞迴 + 代理人）
│   ├── EscalationResult.cs            # 升級結果 record
│   └── Dapper/                        # Dapper 讀取服務（12 組 interface + 實作）
│       ├── UserReadService.cs
│       ├── RoleReadService.cs
│       ├── DepartmentReadService.cs
│       ├── JobTitleReadService.cs
│       ├── ApprovalReadService.cs
│       ├── ProjectReadService.cs
│       ├── PaymentRequestReadService.cs
│       ├── LeaveRequestReadService.cs
│       ├── TravelRequestReadService.cs
│       ├── OvertimeRequestReadService.cs
│       ├── AttendanceReadService.cs
│       ├── InsuranceBracketReadService.cs
│       └── PayrollReadService.cs
├── Common/
│   ├── ApiResponse.cs                 # 統一回應格式 ApiResponse<T>
│   ├── AppException.cs                # 自定義例外
│   └── Constants.cs
├── host.json
├── local.settings.json                # 本地開發設定（不進版控）
└── Api.csproj
```

### 路由分派設計

所有請求透過 `RouterFunction.cs` 接收（Azure Function），再由 `AppRouter.cs` 使用 **C# 12 List Pattern** 根據路徑與方法分派至對應 Handler：

```csharp
// Functions/RouterFunction.cs
[Function("RouterFunction")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "delete",
                 Route = "{*route}")] HttpRequestData req,
    string route,
    FunctionContext context)
```

### Dapper vs EF Core 使用原則

| 情境 | 使用 |
|------|------|
| 列表查詢、多表 JOIN、效能敏感 | **Dapper**（Services/Dapper/） |
| CRUD 操作、資料異動、Transaction | **EF Core** |
| Schema 管理（建表、Migration） | **EF Core Migration** |

### API 路由規劃

#### 公開路由（不需 JWT）

| Method | Path | 說明 |
|--------|------|------|
| GET | `/health` | 健康檢查 |
| POST | `/auth/login` | 登入取得 JWT |
| POST | `/auth/refresh` | 刷新 Token |

#### 使用者管理

| Method | Path | 說明 |
|--------|------|------|
| GET | `/users` | 取得使用者列表 |
| POST | `/users` | 新增使用者 |
| GET | `/users/{id}` | 取得單一使用者 |
| PUT/PATCH | `/users/{id}` | 更新使用者 |
| DELETE | `/users/{id}` | 刪除使用者 |

#### 角色與權限（僅 Superadmin）

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/roles` | 角色列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/roles/{id}` | 角色 CRUD |
| GET/POST | `/permissions` | 權限列表 / 新增 |
| GET/PUT/DELETE | `/permissions/{id}` | 權限 CRUD |

#### 部門與職稱

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/departments` | 部門列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/departments/{id}` | 部門 CRUD |
| GET/POST | `/job-titles` | 職稱列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/job-titles/{id}` | 職稱 CRUD |

#### 簽核流程

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/approval-items` | 簽核項目列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/approval-items/{id}` | 簽核項目 CRUD |
| POST | `/approval-items/{id}/steps` | 新增簽核步驟 |
| PUT/PATCH | `/approval-items/{id}/steps/{stepId}` | 更新簽核步驟 |
| DELETE | `/approval-items/{id}/steps/{stepId}` | 刪除簽核步驟 |

#### 審核任務

| Method | Path | 說明 |
|--------|------|------|
| GET | `/approval-tasks` | 待審核任務列表 |
| GET | `/approval-tasks/{id}` | 取得任務詳情 |
| PATCH | `/approval-tasks/{appType}/{id}/review` | 審核（核准 / 退回） |

#### 專案管理

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/projects` | 專案列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/projects/{id}` | 專案 CRUD |

#### 請款 / 請假 / 出差 / 加班申請

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/payment-requests` | 請款列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/payment-requests/{id}` | 請款 CRUD |
| PATCH | `/payment-requests/{id}/submit` | 送出請款申請（draft → pending） |
| GET/POST | `/leave-requests` | 請假列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/leave-requests/{id}` | 請假 CRUD |
| PATCH | `/leave-requests/{id}/submit` | 送出請假申請（draft → pending） |
| GET/POST | `/travel-requests` | 出差列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-requests/{id}` | 出差 CRUD |
| PATCH | `/travel-requests/{id}/submit` | 送出出差申請（draft → pending） |
| GET/POST | `/overtime-requests` | 加班申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/overtime-requests/{id}` | 加班申請 CRUD |
| PATCH | `/overtime-requests/{id}/submit` | 送出加班申請（draft → pending） |

#### 出勤打卡

| Method | Path | 說明 |
|--------|------|------|
| GET | `/attendances` | 出勤紀錄列表（分頁） |
| GET | `/attendances/today` | 今日打卡紀錄（當前使用者） |
| POST | `/attendances/clock-in` | 上班打卡（含 GPS） |
| POST | `/attendances/clock-out` | 下班打卡（含 GPS） |
| POST | `/attendances/overtime-start` | 加班開始打卡（需核准的加班申請） |
| POST | `/attendances/overtime-end` | 加班結束打卡 |

#### 勞健保級距

| Method | Path | 說明 |
|--------|------|------|
| GET | `/insurance-brackets` | 級距列表 |
| GET | `/insurance-brackets/lookup?salary=xxx` | 根據薪資查詢對應級距（向上取最近級距） |
| POST | `/insurance-brackets` | 新增級距 |
| GET | `/insurance-brackets/{id}` | 取得單筆級距 |
| PUT/PATCH | `/insurance-brackets/{id}` | 更新級距 |
| DELETE | `/insurance-brackets/{id}` | 刪除級距 |

#### 人事薪資

| Method | Path | 說明 |
|--------|------|------|
| GET | `/payroll?year=YYYY&month=MM` | 月薪計算（動態計算，不存 DB） |

#### 其他

| Method | Path | 說明 |
|--------|------|------|
| GET | `/settings` | 取得系統設定 |
| PATCH | `/settings` | 更新系統設定 |

### 常用指令

```bash
cd Api
dotnet restore                          # 還原套件
dotnet build                            # 建置
func start                              # 本地啟動 Azure Functions（Port 7071）
dotnet ef migrations add <Name>         # 新增 Migration
dotnet ef database update               # 套用 Migration
```

---

## 資料庫設計

### 資料庫名稱：`JabezDb`

### 21 個資料表實體

| 實體 | 說明 |
|------|------|
| `User` | 使用者（含 DepartmentId、JobTitleId、IsSuperAdmin） |
| `Role` | 角色定義 |
| `Permission` | 權限代碼 |
| `UserRole` | 使用者 ↔ 角色（Junction） |
| `RolePermission` | 角色 ↔ 權限（Junction） |
| `RefreshToken` | Refresh Token 儲存 |
| `Department` | 部門主檔 |
| `JobTitle` | 職稱主檔 |
| `ApprovalItem` | 簽核流程項目 |
| `ApprovalStep` | 簽核流程步驟 |
| `ApprovalRecord` | 簽核動作記錄（含 OnBehalfOfUserId 代理標記、IsEscalated 升級標記） |
| `EscalationOverride` | 升級審核指派（記錄被指派的升級/代理審核者，審核完成後清除） |
| `Project` | 專案主檔 |
| `PaymentRequest` | 請款申請 |
| `InvoiceItem` | 請款明細（發票項目） |
| `LeaveRequest` | 請假申請 |
| `TravelRequest` | 出差申請（含 IsHolidayTravel 假日出差欄位） |
| `OvertimeRequest` | 加班申請（走簽核流程） |
| `AttendanceRecord` | 出勤打卡紀錄（每人每天一筆，含 GPS） |
| `SystemSetting` | 系統設定 |
| `InsuranceBracket` | 勞健保級距（投保級距、員工負擔勞保、員工負擔健保） |

---

## 簽核升級機制（Escalation）

當簽核步驟設定 `UseApplicantDepartment = true` 且申請人本身就是該步驟的審核者（自審情境，例如部門主管送出申請），系統會根據申請類型自動往上層部門尋找合適的審核者，而非自動核准。

### 各申請類型的升級規則

| | 加班 | 請假 | 出差 | 請款 |
|---|---|---|---|---|
| 往上層部門找主管 | ✓ | ✓ | ✓ | ✗（維持自動跳過） |
| 主管請假時找代理人 | ✓ | ✗ | ✗ | — |
| 遞迴往上 | ✓ | ✓ | ✓ | — |
| 停在董事長之前 | ✓ | ✓ | ✗ | — |
| 找不到人時 | 報錯 | 報錯 | 報錯 | — |

### 升級流程（以加班為例）

```
部門主管送出加班申請
  → Step 1 設定為 UseApplicantDepartment=true, JobTitleId=4
  → 偵測到「自己審自己」→ 觸發升級
  → 找上層部門（ParentId）的部門主管（JobTitleId=4）
    → 找到且未請假 → 由該主管審核
    → 找到但請假中 → 找該主管的代理人（AgentUserId）
      → 有代理人 → 由代理人審核（ApprovalRecord 標記 OnBehalfOfUserId）
      → 無代理人 → 繼續往上層部門找（遞迴）
    → 沒找到 → 繼續往上層部門找
  → 到達董事長（JobTitleId=5）前停止
  → 都找不到 → 拋出錯誤「找不到可審核的主管，無法送出申請」
```

### 關鍵元件

| 元件 | 說明 |
|------|------|
| `EscalationService.cs` | 核心升級邏輯：遞迴往上層部門找主管、檢查請假、找代理人 |
| `ApprovalFlowService.cs` | 自審時呼叫 EscalationService（非 payment_request 類型） |
| `EscalationOverride` 資料表 | 記錄升級指派（審核者 + 代理誰），供 Dapper 查詢與 AuthorizeStep 使用 |
| `ApprovalRecord.OnBehalfOfUserId` | 代理審核標記（代替誰審核） |
| `ApprovalRecord.IsEscalated` | 是否為升級審核 |

### 請假中判斷

查詢 `LeaveRequests` 表中 `ApprovalStatus = 'approved'` 且 `StartDate <= 今天 <= EndDate` 的記錄。僅加班申請的升級流程會檢查。

### 前端顯示

簽核流程時間軸中，升級審核的紀錄會顯示：
- 代理審核：`代理 XXX`（棕色 badge）
- 直接升級：`升級審核`（紫色 badge）

---

## 認證系統

### JWT 規格

- 演算法：HS256
- Issuer：`jabez-api`
- Audience：`jabez-admin`
- 存取 Token 有效期：60 分鐘
- Refresh Token 有效期：7 天
- Claims：`sub`（使用者 ID）、`name`、`email`、`jti`、`roles`、`permissions`、`is_superadmin`

### 登入流程

1. `POST /auth/login` → 驗證帳密（BCrypt 密碼驗證）
2. 查詢使用者角色與權限
3. Superadmin：取得 DB 中所有權限
4. 一般使用者：取得角色對應權限
5. 產生 Access Token + Refresh Token
6. Refresh Token 存入 DB（`RefreshTokens` 資料表）

### Superadmin（隱藏帳號）

- **Email**：`sa@system.local`
- **密碼**：`Admin@123`（正式環境請立即變更）
- **GUID**：`00000000-0000-0000-0000-000000000001`
- `User.IsSuperAdmin = true`（由 `UserConfiguration.cs` Seed）
- JWT 包含 `is_superadmin: true` claim，並帶有 DB 中所有權限
- 前端 `hasPermission()` 對 Superadmin 一律回傳 `true`
- 路由/選單 `permission: 'superadmin'` 代表僅 Superadmin 可見
- 使用者列表 SQL 過濾：`WHERE IsSuperAdmin = 0`
- Superadmin 無法被編輯或刪除

---

## 環境設定

### local.settings.json（不進版控）

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Jwt__Secret": "YourSuperSecretKeyForHS256MustBeAtLeast32Chars!!",
    "Jwt__Issuer": "jabez-api",
    "Jwt__Audience": "jabez-admin",
    "Jwt__ExpiryMinutes": "60",
    "Jwt__RefreshExpiryDays": "7"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=JabezDb;User Id=sa;Password=Strong@Password123;TrustServerCertificate=True;"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

---

## 薪水計算公式（人事薪資模組）

1. **日薪** = 底薪 ÷ 30（四捨五入至整數）
2. **假日津貼** = 日薪 × 假日出差天數（來自該月已核准且 `IsHolidayTravel=true` 的出差申請）
3. **勞保費 / 健保費**：根據底薪查詢勞健保級距表（向上取最近級距）
4. **實領薪水** = 底薪 + 假日津貼 - 勞保費 - 健保費

> 人事薪資為動態計算，不儲存於資料庫。前端可匯出 PDF 薪資表。

---

## 開發注意事項

1. **CORS**：本地開發時 Api 已允許所有來源（`"CORS": "*"`）
2. **JWT**：Token 過期處理由前端 `auth.interceptor.ts` 攔截 401 後自動 Refresh，失敗則導向登入頁
3. **密碼驗證**：`AuthHandler` 使用 BCrypt 驗證密碼，`UserHandler` 新增/更新使用者時以 BCrypt 雜湊密碼；Seed 資料預設密碼為 `Admin@123`
4. **EF Core Migration**：每次資料庫異動須建立新 Migration，禁止直接修改現有 Migration
5. **Dapper 查詢**：SQL 語法集中於 `Services/Dapper/` 中的 ReadService，禁止在 Handler 直接撰寫 SQL
6. **錯誤回應格式**：統一使用 `ApiResponse<T>`（`Common/ApiResponse.cs`）
7. **環境變數**：JWT 設定採 Azure Functions 雙底線慣例（`Jwt__Secret`），對應 `IConfiguration["Jwt:Secret"]`
8. **DB 自動初始化**：啟動時自動執行 EF Migration 並 Seed 初始資料（Superadmin、預設 Role/Permission）
9. **測試規範**：測試功能時，必須實際輸入測試資料進行測試，不得僅以目視或靜態檢查代替。確認 CRUD 流程（新增、讀取、更新、刪除）與業務邏輯皆正常運作後，方可視為測試通過。
10. **系統時區**：所有涉及日期時間的處理（包含前端顯示與後端邏輯），一律使用**台北時間（Asia/Taipei, UTC+8）**。後端取得當前時間應使用 `TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"))`，前端則確保日期時間以台北時區呈現。

---

## Git 分支策略

```
main          # 正式環境
develop       # 開發整合
feature/*     # 功能開發（feature/payment-request）
hotfix/*      # 緊急修復
```

---

## 功能新增與修改規範

**每次新增或修改功能時，必須同步更新以下三處：**

1. **Admin/**（前端）：新增/修改對應的 Component、Service、Route、Guard
2. **Api/**（後端）：新增/修改對應的 Handler、Dtos、Entities、Migration（如有 DB 異動）
3. **CLAUDE.md**：更新受影響的章節（目錄結構、API 路由、資料表、注意事項等）

> 若只改其中一處而未同步其他兩處，視為不完整的變更。

### UI 樣式一致性

- 所有頁面使用 **Tailwind CSS** utilities 與 `@layer components` 定義的語意類別，不得引入 Bootstrap 或其他 CSS 框架
- 表格一律使用 `@tanstack/angular-table`，樣式與現有列表頁保持一致
- 表單排版、間距、按鈕顏色語意（primary 新增、danger 刪除、warning 編輯）需與現有頁面相同
- 通知訊息一律使用 `ngx-toastr`，不得自製 alert 或 modal 替代
- 新頁面須放置於 `main-layout` 下，使用相同的 sidenav / topbar / footer 結構

### 程式碼寫法與架構一致性

**前端（Angular）：**
- 一律使用 **Standalone Component**，不得引入 NgModule
- 狀態管理使用 **Angular Signals**，不使用 BehaviorSubject 管理元件內部狀態
- HTTP 請求封裝於 `features/<module>/services/` 內，Component 不得直接注入 `HttpClient`
- 路由採 **Lazy Loading**，每個 feature 在 `app.routes.ts` 以 `loadComponent` / `loadChildren` 載入
- 新 feature 目錄結構須遵循：`models/`、`pages/`、`services/` 三層

**後端（.NET）：**
- 新功能須新增對應 `Handler`（放於 `Handlers/`）並在 `AppRouter.cs` 以 List Pattern 登記路由
- 讀取查詢一律使用 **Dapper**（新增 `Services/Dapper/<Module>ReadService.cs`）
- 寫入操作一律使用 **EF Core**（透過 `AppDbContext`）
- 所有端點回傳 `ApiResponse<T>`，不得直接回傳裸型別
- 新增資料表須建立 EF Core Migration，不得手動修改資料庫

---

## 程式碼規範

- **命名**：C# PascalCase、TypeScript camelCase、資料庫欄位 PascalCase
- **注解**：公開方法 / 複雜邏輯必須有注解（中文或英文均可）
- **單一職責**：Handler 處理 HTTP 轉換，Dapper ReadService 處理讀取查詢，EF Core 處理寫入
- **非同步**：所有 I/O 操作使用 `async / await`，禁止 `.Result` 或 `.Wait()`
- **回應格式**：所有 API 端點回傳 `ApiResponse<T>`，格式為 `{ success, message, data }`
