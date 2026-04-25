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
    │   ├── travel-payment-requests/ # 出差請款申請（小額已代墊直接請款，無沖銷）
    │   ├── travel-requests/   # 出差預支申請（走沖銷流程）
    │   ├── overtime-requests/ # 加班申請（走簽核流程）
    │   ├── advance-requests/  # 預支申請
    │   ├── write-off-requests/ # 預支沖銷申請（獨立簽核流程）
    │   ├── travel-write-off-requests/ # 出差預支沖銷申請（獨立簽核流程）
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
│   ├── RouterFunction.cs              # HttpTrigger，catch-all route {*route}
│   └── AttendanceReminderFunction.cs  # TimerTrigger：每分鐘檢查上下班前 2 分鐘，命中則 LINE 推播打卡提醒
├── Routing/
│   └── AppRouter.cs                   # C# 12 List Pattern 路由分派器
├── Handlers/                          # 22 個 Handler（業務邏輯）
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
│   ├── TravelRequestHandler.cs        # 出差預支申請 CRUD（預支後沖銷）
│   ├── TravelPaymentRequestHandler.cs # 出差請款申請 CRUD（小額代墊直接請款）
│   ├── OvertimeRequestHandler.cs      # 加班申請 CRUD
│   ├── AdvanceRequestHandler.cs       # 預支申請 CRUD
│   ├── WriteOffRequestHandler.cs      # 預支沖銷申請 CRUD（獨立簽核流程）
│   ├── TravelWriteOffRequestHandler.cs # 出差預支沖銷申請 CRUD（獨立簽核流程）
│   ├── AttendanceHandler.cs           # 打卡（上班/下班/加班開始/加班結束）
│   ├── InsuranceBracketHandler.cs    # 勞健保級距 CRUD
│   ├── PayrollHandler.cs             # 人事薪資查詢（月薪計算）
│   ├── LineHandler.cs                # LINE 帳號綁定/解綁
│   ├── AttendanceReminderAdminHandler.cs # 打卡提醒手動觸發（Superadmin，除錯用）
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
│   └── Dtos/                          # 17 個 DTO 檔案（含 LineDtos）
├── Services/
│   ├── IJwtService.cs
│   ├── JwtService.cs                  # HS256 JWT 產生與驗證
│   ├── IEscalationService.cs          # 簽核升級服務介面
│   ├── EscalationService.cs           # 簽核升級邏輯（上層部門主管遞迴 + 代理人）
│   ├── EscalationResult.cs            # 升級結果 record
│   ├── ILineService.cs               # LINE API 操作介面
│   ├── LineService.cs                # LINE Platform REST API 封裝（token 換取 + 推播）
│   ├── LineFlexMessageBuilder.cs     # 6 種簽核通知 + 打卡提醒的 LINE Flex Message 模板
│   ├── IAttendanceReminderService.cs # 打卡提醒服務介面
│   ├── AttendanceReminderService.cs  # 打卡提醒協調：判斷時點、過濾對象、推播 LINE
│   └── Dapper/                        # Dapper 讀取服務（13 組 interface + 實作）
│       ├── UserReadService.cs
│       ├── RoleReadService.cs
│       ├── DepartmentReadService.cs
│       ├── JobTitleReadService.cs
│       ├── ApprovalReadService.cs
│       ├── ProjectReadService.cs
│       ├── PaymentRequestReadService.cs
│       ├── LeaveRequestReadService.cs
│       ├── TravelRequestReadService.cs
│       ├── TravelPaymentRequestReadService.cs
│       ├── OvertimeRequestReadService.cs
│       ├── AdvanceRequestReadService.cs
│       ├── WriteOffRequestReadService.cs
│       ├── TravelWriteOffRequestReadService.cs
│       ├── AttendanceReadService.cs
│       ├── AttendanceReminderReadService.cs
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
| POST | `/approval-tasks/batch-approve` | 批次核准多筆待審申請（僅 approved 動作，需 `approval-tasks:batch-approve` 權限；撥款/退款日留空，完成後以提醒清單回傳需補填者） |

#### 專案管理

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/projects` | 專案列表 / 新增 |
| GET/PUT/PATCH/DELETE | `/projects/{id}` | 專案 CRUD |

#### 請款 / 請假 / 出差 / 加班 / 預支申請

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/payment-requests` | 請款列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/payment-requests/{id}` | 請款 CRUD |
| PATCH | `/payment-requests/{id}/submit` | 送出請款申請（draft → pending） |
| PATCH | `/payment-requests/{id}/payment-date` | 更新撥款日期（財務體系部門：AC/FIN/Jabez HQ/CEO） |
| GET/POST | `/leave-requests` | 請假列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/leave-requests/{id}` | 請假 CRUD |
| PATCH | `/leave-requests/{id}/submit` | 送出請假申請（draft → pending） |
| GET | `/leave-requests/compensatory-hours` | 查詢可補休時數（總加班 − 已補休） |
| GET | `/leave-requests/annual-quota` | 查詢年假額度（依 HireDate 計算年資） |
| GET | `/leave-requests/ceremonial-quota` | 查詢歲時祭儀假額度（僅原住民，每年 3 天，跨年歸零） |
| GET | `/leave-requests/marriage-quota` | 查詢婚假配額（上限 8 天，不限年度） |
| GET | `/leave-requests/maternity-status` | 查詢產假狀態（是否已有活躍申請） |
| GET | `/leave-requests/bereavement-quota?relationship={rel}` | 查詢喪假配額（依親屬關係 3/6/8 天） |
| GET | `/leave-requests/senior-executive-eligibility` | 查詢高階主管假適用性（JobTitle.Level ≤ 3） |
| GET/POST | `/travel-requests` | 出差預支申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-requests/{id}` | 出差預支申請 CRUD |
| PATCH | `/travel-requests/{id}/submit` | 送出出差預支申請（draft → pending） |
| GET/POST | `/travel-payment-requests` | 出差請款申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-payment-requests/{id}` | 出差請款申請 CRUD |
| PATCH | `/travel-payment-requests/{id}/submit` | 送出出差請款申請（draft → pending） |
| PATCH | `/travel-payment-requests/{id}/payment-date` | 更新撥款日期（財務體系部門：AC/FIN/Jabez HQ/CEO） |
| GET/POST | `/overtime-requests` | 加班申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/overtime-requests/{id}` | 加班申請 CRUD |
| PATCH | `/overtime-requests/{id}/submit` | 送出加班申請（draft → pending） |
| GET/POST | `/advance-requests` | 預支申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/advance-requests/{id}` | 預支申請 CRUD |
| PATCH | `/advance-requests/{id}/submit` | 送出預支申請（draft → pending） |
| PATCH | `/advance-requests/{id}/payment-date` | 更新撥款日期（財務體系部門：AC/FIN/Jabez HQ/CEO） |

#### 預支沖銷申請

| Method | Path | 說明 |
|--------|------|------|
| GET/POST | `/write-off-requests` | 預支沖銷申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/write-off-requests/{id}` | 預支沖銷申請 CRUD |
| PATCH | `/write-off-requests/{id}/submit` | 送出預支沖銷申請（draft → pending） |

#### 出差預支沖銷申請

| Method | Path | 說明 |
|--------|------|------|
| GET | `/travel-write-off-requests/available-travels` | 可沖銷的出差預支申請清單 |
| GET/POST | `/travel-write-off-requests` | 出差預支沖銷申請列表 / 新增（預設 draft） |
| GET/PUT/PATCH/DELETE | `/travel-write-off-requests/{id}` | 出差預支沖銷申請 CRUD |
| PATCH | `/travel-write-off-requests/{id}/submit` | 送出出差預支沖銷申請（draft → pending） |

#### 出勤打卡

| Method | Path | 說明 |
|--------|------|------|
| GET | `/attendances` | 出勤紀錄列表（分頁） |
| GET | `/attendances/today` | 今日打卡紀錄（當前使用者） |
| POST | `/attendances/clock-in` | 上班打卡（含 GPS） |
| POST | `/attendances/clock-out` | 下班打卡（含 GPS） |
| POST | `/attendances/overtime-start` | 加班開始打卡（需核准的加班申請） |
| POST | `/attendances/overtime-end` | 加班結束打卡 |

#### 打卡提醒（手動觸發，僅 Superadmin）

| Method | Path | 說明 |
|--------|------|------|
| POST | `/admin/attendance-reminder/run?type=clockIn\|clockOut` | 繞過時點與週末檢查，強制對符合條件的員工推播 LINE 打卡提醒（除錯用） |

> 自動排程由 `AttendanceReminderFunction`（TimerTrigger，每分鐘）執行，不透過 HTTP 觸發；此端點僅供本地/Production 驗證。

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

#### LINE 綁定

| Method | Path | 說明 |
|--------|------|------|
| GET | `/line/bind-url` | 產生 LINE OAuth URL（含 state 防 CSRF） |
| POST | `/line/bind` | 用 OAuth code 換取 LINE userId 並綁定 |
| POST | `/line/unbind` | 解除 LINE 綁定 |
| GET | `/line/binding-status` | 查詢當前用戶 LINE 綁定狀態 |

#### 檔案代理（Blob Storage）

| Method | Path | 說明 |
|--------|------|------|
| GET | `/files/signatures/{fileName}` | 簽名檔代理（公開，PDF 匯出用） |
| GET | `/files/avatars/{fileName}` | 頭像代理（公開，topbar 顯示用） |
| GET | `/files/indigenous-proofs/{fileName}` | 原住民證明文件代理（需 `users:read`，HR 敏感 PII） |

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

### 23 個資料表實體

| 實體 | 說明 |
|------|------|
| `User` | 使用者（含 DepartmentId、JobTitleId、IsSuperAdmin、LineUserId、IsIndigenous、Avatar、SignatureUrl、IndigenousProofUrl） |
| `Role` | 角色定義 |
| `Permission` | 權限代碼 |
| `UserRole` | 使用者 ↔ 角色（Junction） |
| `RolePermission` | 角色 ↔ 權限（Junction） |
| `RefreshToken` | Refresh Token 儲存 |
| `Department` | 部門主檔（含 ParentId 階層、**CanViewSiblings 同層兄弟部門可見旗標**） |
| `JobTitle` | 職稱主檔 |
| `ApprovalItem` | 簽核流程項目 |
| `ApprovalStep` | 簽核流程步驟（含 UseDirectSupervisor、UseApplicantDesignated） |
| `ApprovalRecord` | 簽核動作記錄（含 OnBehalfOfUserId 代理標記、IsEscalated 升級標記） |
| `EscalationOverride` | 升級審核指派（記錄被指派的升級/代理審核者，審核完成後清除） |
| `Project` | 專案主檔（含 **DepartmentId 必填**、ReceivedAmount 實收金額、ContractAmount 契約金額、BusinessAmount 業務執行金額） |
| `ProjectPaymentSchedule` | 專案請款期別明細（一期一筆：請款/發票/入帳日期與金額、扣款備註；扣款金額 = 發票 − 入帳，前端計算不存 DB） |
| `PaymentRequest` | 請款申請 |
| `InvoiceItem` | 請款明細（發票項目） |
| `LeaveRequest` | 請假申請（含 BereavementRelationship 喪假親屬關係） |
| `TravelRequest` | 出差預支申請（含 IsHolidayTravel、IsClosed 結案、GrandTotal 明細合計；事後走沖銷流程）。當 `IsHolidayTravel=true`（假日執行活動）時不含 Items 與發票明細，僅記錄活動地點/期間/參與人員 |
| `TravelRequestItem` | 出差預支明細（交通費、住宿費、餐費、雜支）；假日執行活動不使用 |
| `TravelPaymentRequest` | 出差請款申請（員工代墊後直接請款，無沖銷流程；含 EstimatedPaymentDate/PaidAt 撥款欄位） |
| `TravelPaymentRequestItem` | 出差請款明細（交通費、住宿費、餐費、雜支，含發票號碼、檔案上傳） |
| `OvertimeRequest` | 加班申請（走簽核流程） |
| `AdvanceRequest` | 預支申請 |
| `AdvanceRequestItem` | 預支明細 |
| `WriteOffRecord` | 預支沖銷申請（獨立簽核流程，關聯 AdvanceRequest，含 ApprovalStatus/CurrentStepOrder） |
| `WriteOffItem` | 沖銷明細（含發票號碼、檔案上傳） |
| `TravelWriteOffRecord` | 出差預支沖銷申請（獨立簽核流程，關聯 TravelRequest） |
| `TravelWriteOffItem` | 出差預支沖銷明細（含發票號碼、檔案上傳） |
| `RequestDesignatedReviewer` | 申請人指定審核者清單（多人依序審核） |
| `AttendanceRecord` | 出勤打卡紀錄（每人每天一筆，含 GPS） |
| `SystemSetting` | 系統設定 |
| `InsuranceBracket` | 勞健保級距（投保級距、員工負擔勞保、員工負擔健保） |

---

## 請假規則

### 假別一覽（15 種）

| # | 假別 | LeaveType | 時間單位 | 天數上限 | 薪資影響 |
|---|------|-----------|---------|---------|---------|
| 1 | 年假(特休假) | `annual` | 半天 | 依年資（3~30 天） | 有薪 |
| 2 | 事假 | `personal` | 小時 | 無上限 | 按天數扣除全額薪資 |
| 3 | 病假 | `sick` | 小時 | 無上限 | 按天數扣除半薪 |
| 4 | 補休 | `compensatory` | 半天（扣 4 小時/半天） | 依加班時數 | 有薪 |
| 5 | 公假 | `official` | 天 | 無上限 | 有薪 |
| 6 | 婚假 | `marriage` | 天 | 8 天（可不連續） | 有薪 |
| 7 | 產假 | `maternity` | 天（**選起始日、自動填 56 天**） | 56 天 | 有薪 |
| 8 | 流產假(3 個月以上) | `miscarriage_3m` | 天 | 28 天 | 有薪 |
| 9 | 流產假(2-3 個月) | `miscarriage_2to3m` | 天 | 7 天 | 有薪 |
| 10 | 流產假(未滿 2 個月) | `miscarriage_under2m` | 天 | 5 天 | 有薪 |
| 11 | 產檢假 | `prenatal_checkup` | 小時 | 7 天 | 有薪 |
| 12 | 陪產假 | `paternity` | 小時 | 7 天 | 有薪 |
| 13 | 喪假 | `bereavement` | 天 | 依親屬關係（3/6/8 天） | 有薪 |
| 14 | 歲時祭儀假 | `ceremonial_festival` | 天 | 3 天/年（跨年歸零，**限原住民**） | 有薪 |
| 15 | 高階主管假 | `senior_executive` | 半天 | **無上限** | **不扣任何項目**（協理以上專用，`JobTitle.Level ≤ 3`） |

### 時間單位規則

請假輸入依假別分為三種單位，儲存仍為 `LeaveRequest.Hours`（`decimal(5,1)`）：

| 單位 | 換算 | 輸入 UI | 適用假別 |
|------|------|---------|---------|
| 小時 (`hour`) | 自然小時（**整點**） | `datetime-local` 整點步進（分鐘僅 00） | 事假、病假、產檢假、陪產假 |
| 半天 (`half_day`) | 4 小時 = 半天 | 日期 + 上午/下午 選擇 | 年假、補休、高階主管假 |
| 整天 (`day`) | 8 小時 = 1 天 | 起迄日期選擇 | 公假、婚假、產假、喪假、歲時祭儀假、流產假系列 |

- **產假特例**：選擇起始日後，結束日自動填為起始日 + 55 天（共 56 天），總時數固定 448 小時。法規為一次請完，禁止重複活躍申請（同 `EmployeeId` 存在 `pending` / `approved` 產假）。
- **補休扣除**：申請 1 個半天（4 小時）→ 從可補休時數池扣 4 小時。
- **高階主管假權限閘門**：前後端皆檢查 `JobTitle.Level ≤ 3`；前端透過 JWT `job_title_level` claim 判斷選項可見性，後端在 `CreateAsync` / `UpdateAsync` / `SubmitAsync` 各階段驗證。
- **分鐘限制（小時單位）**：僅允許 `:00`（`step="3600"` 秒 = 整點步進），前後端皆驗證時數為整數倍。

### 年假額度規則（依年資）

| 年資 | 年假天數 |
|------|---------|
| 未滿 6 個月 | 0 天 |
| 滿 6 個月 ~ 未滿 1 年 | 3 天 |
| 滿 1 年 ~ 未滿 2 年 | 10 天（優於勞基法 7 日） |
| 滿 2 年 ~ 未滿 3 年 | 10 天 |
| 滿 3 年 ~ 未滿 5 年 | 14 天 |
| 滿 5 年 ~ 未滿 10 年 | 15 天 |
| 10 年以上 | 每年加 1 天，上限 30 天 |

> 年資根據 `User.HireDate` 計算。API 端點：`GET /leave-requests/annual-quota`。

### 喪假親屬關係與天數

| 天數 | 親屬關係 |
|------|---------|
| 8 天 | 配偶、父母、養父母、繼父母 |
| 6 天 | 祖父母（含外祖父母）、子女、配偶之父母、配偶之養父母或繼父母 |
| 3 天 | 曾祖父母、兄弟姊妹、配偶之祖父母 |

> 喪假須在 `LeaveRequest.BereavementRelationship` 欄位記錄親屬關係，前端以下拉選單選擇。

### 天數上限驗證（累計制）

- 送出申請（submit）時，後端查詢該使用者**同假別**、**已送出或已核准**的申請總時數
- 加上本次申請時數，檢查是否超過上限
- 天數換算：`累計時數 ÷ 8 小時 = 天數`
- 年假按**年度**累計，產假系列與喪假**不限年度**
- 喪假按**同親屬關係**分別累計

### 補休規則

- 依系統統計之加班工時扣抵
- 可補休時數 = 已核准加班申請 `EstimatedHours` 合計 − 已送出/已核准補休假 `Hours` 合計
- API 端點：`GET /leave-requests/compensatory-hours`

### 請假申請步驟

```
請假申請 → 選擇假別 → 填入開始/結束時間 → 請假原因 → 指定審核人
如需多層級審核：新增審核人順序等同審核順序
```

### 人事薪資頁面整合

- 薪資編輯頁顯示該月**所有已核准**的請假紀錄（假別、期間、天數）
- 薪資明細信件同步顯示「本月請假紀錄」表格
- 事假扣薪與病假扣薪仍於扣款項目中獨立計算

### 涉及元件

| 元件 | 說明 |
|------|------|
| `LeaveRequest.BereavementRelationship` | Entity 欄位：喪假親屬關係 |
| `LeaveRequestHandler.ValidateLeaveQuotaAsync()` | 天數上限驗證（累計制） |
| `LeaveRequestHandler.GetAnnualQuotaAsync()` | 年假額度 API |
| `LeaveRequestHandler.CalculateAnnualLeaveDays()` | 年資 → 年假天數計算 |
| `PayrollReadService` | 新增查詢該月所有請假明細 |
| `PayrollHandler.BuildLeaveDetailSection()` | 薪資明細信件請假紀錄 HTML |
| 前端 `leave-request.model.ts` | 13 種假別定義、喪假關係常數、天數上限常數 |
| 前端 `leave-request-form` | 假別下拉選單（分群組）、條件式欄位、額度提示 |
| 前端 `payroll-form` | 本月請假紀錄表格 |

---

## 請款簽核流程

### 簽核步驟（Seed 預設）

| 步驟 | 審核者 | 說明 |
|------|--------|------|
| Step 1 | 申請人部門的部門主管(JT=4) | 部門主管初核（`UseApplicantDepartment=true`） |
| Step 2 | 會計部主管(JT=4) | 取得紙本資料審核 |
| Step 3 | 財務部主管(JT=4) | 填入預計撥款日，核決及撥款 |
| Step 4 | 總監(JT=5, 總監室) | 最終核決 |

### 狀態流轉

```
draft → pending → approved / returned / rejected
```

- `draft`：草稿，可編輯
- `pending`：已送出，等待審核中（逐步推進 `CurrentStepOrder`）
- `approved`：所有步驟核准完成
- `returned`：退回申請人修改（可重新送出）
- `rejected`：拒絕（終止狀態）

### 核決後通知與撥款

當**最後一步**（Step 4 總監）核准後，系統自動：
1. 狀態變更為 `approved`
2. **通知申請人**：信件主旨 `[已核准] 請款申請 #XX`
3. **通知財務部全員**：信件主旨 `[可撥款] 請款申請 #XX 已核准`

財務部收到通知後，透過 `PATCH /payment-requests/{id}/payment-date` 填入：
- `EstimatedPaymentDate`：預計撥款日
- `PaidAt`：實際撥款日

> 此端點僅限**財務體系部門**（部門 Code ∈ AC / FIN / Jabez HQ / CEO，定義於 `Api/Common/Constants.cs` `DepartmentCodes.FinancialAndAbove`）或 **Superadmin** 操作。同樣規則套用於 `/advance-requests/{id}/payment-date`、`/travel-requests/{id}/payment-date`、`/travel-payment-requests/{id}/payment-date`，以及預支結案 / 出差結案端點。

### 批次核准（全選核准）

擁有 `approval-tasks:batch-approve` 權限的使用者，可在簽核作業「待審核」頁籤勾選多筆待審申請一次核准。

- **動作限定**：僅支援 `approved`；退回/拒絕仍須進入詳情頁個別操作。
- **權限獨立**：批次核准為獨立權限，不依賴 `approval-tasks:write`；未擁有此權限者按鈕不顯示，後端亦回 403。
- **逐筆驗證**：每筆仍經過 `AuthorizeStepAsync`（職稱/部門/指定/升級），失敗者回報於 `failed` 清單，不中斷其他項目。
- **撥款類留空**：批次核准 payment_request / advance 時 `EstimatedPaymentDate`、`PaidAt` 留空，後端回傳 `pendingPayment` 清單，前端以 banner 提示使用者「前往補填」撥款/退款日。
- **沖銷結案不觸發**：批次核准不會設定 `CloseAdvance`；沖銷結案仍須於詳情頁或獨立結案端點操作。

### 自審跳過規則（僅限請款）

當申請人本身符合某步驟的審核者條件時（例如部門主管送出自己部門的請款），該步驟**自動跳過**（視為已通過），不觸發升級機制。若所有步驟都被跳過，申請**自動核准**。

此行為與加班/請假/出差不同 — 後者會觸發升級機制往上層部門找主管審核。

### 上層級審核模式（UseDirectSupervisor）

`ApprovalStep` 新增 `UseDirectSupervisor`（bool, 預設 false）欄位，啟用時系統自動找同部門中層級最接近的上級作為審核者。

**層級判斷：** `JobTitle.Level` 數字越小 = 層級越高。上層級 = 同部門中 `Level < 申請人 Level` 且 `Level` 最大（最接近）的人。

**逐步往上爬：** 多個連續的 `UseDirectSupervisor` 步驟會自動往上找不同層級：
- 第 1 個上層級步驟（rank=0）→ 找最接近的上級（例如資深工程師）
- 第 2 個上層級步驟（rank=1）→ 找第 2 層上級（例如主任工程師）
- 第 N 個上層級步驟 → 找第 N 層上級
- rank 計算方式：該步驟前有幾個 `UseDirectSupervisor` 步驟

**規則：**
- 同層級有多人 → 全部通知，任一人審核即通過
- 找不到更高層級的人 → 該步驟自動跳過（視為通過）
- 所有步驟都跳過 → 自動核准
- 此模式不走 EscalationService 升級機制
- 啟用時自動忽略 `DepartmentId` 和 `JobTitleId`（隱含使用申請人部門）

**可與現有模式混用：** 每個 ApprovalStep 獨立判斷，例如 Step 1 用 `UseDirectSupervisor=true`，Step 2 也用 `UseDirectSupervisor=true`（自動往上一層），Step 3 維持固定部門 + 職稱。

**涉及元件：**
| 元件 | 說明 |
|------|------|
| `ApprovalStep.UseDirectSupervisor` | Entity 欄位 |
| `ApprovalFlowService.FindNthSuperiorLevelAsync()` | 找同部門第 N 層上級 |
| `ApprovalTaskHandler.AuthorizeStepAsync()` | 驗證審核者是否為正確層級的上級 |
| `PaymentRequestReadService.StepMatchClause()` | Dapper SQL 以 ROW_NUMBER 計算 rank 匹配審核者 |
| `ApprovalNotificationService.NotifyReviewersAsync()` | 通知正確層級的上級 |
| 前端 `approval-flow.html` | 設定頁 checkbox 開關 |

### 申請人指定審核模式（UseApplicantDesignated）

`ApprovalStep` 新增 `UseApplicantDesignated`（bool, 預設 false）欄位，啟用時審核者由申請人在表單中**依序指定多人**。

**設計背景：** 因跨部門專案支援情境，簽核流程因人員配置不同而不固定，故由申請人在送出時自行決定第一步驟要哪些人審核、以何順序。

**資料模型：** 不使用申請表本身的欄位，而是獨立資料表 `RequestDesignatedReviewers`：

| 欄位 | 說明 |
|------|------|
| `RequestType` | `payment_request` / `leave` / `travel` / `overtime` / `advance` / `write_off` |
| `RequestId` | 關聯申請單 ID |
| `ReviewerId` | 審核者 User ID |
| `StepOrder` | 審核順序（1, 2, 3...），依序逐一通過 |
| `Status` | `pending` / `approved` / `returned` |
| `ReviewedAt` | 審核時間 |
| `Comment` | 審核備注 |

**流程設計：**
- Step 1 為 `UseApplicantDesignated=true`：走指定審核者多人順序流程
- Step 2+ 回歸現有固定流程（固定部門+職稱、UseDirectSupervisor 等）

**規則：**
- 送出（submit）時，如果流程中有 `UseApplicantDesignated` 步驟，`designatedReviewers` 清單必填且至少 1 人
- 依 `StepOrder` 升序逐一審核，前一人核准後才輪到下一人
- 指定審核者不需擁有全域 `approval-tasks:write` 權限，被指定即可審核
- 自審規則：leave / travel / overtime — 任何一位指定審核者是申請人本人則報錯；payment_request / advance / write_off — 申請人排第 1 位時自動跳過，排其他位置不允許
- 退回時：當前等待審核者狀態設為 `returned`，重送時所有指定審核者重置為 `pending`
- 此模式與 `UseDirectSupervisor`、`UseApplicantDepartment` 互斥（每個 ApprovalStep 擇一使用）
- 一個流程建議只有一個 `UseApplicantDesignated` 步驟

**存取控制（`GET /approval-tasks/{type}/{id}`）：**
- Superadmin：可查看所有
- 有 `approval-tasks:read` 權限：可查看所有
- 被指定為審核者（任何狀態）：可查看此申請單
- 曾審核過（有 ApprovalRecord）：可查看此申請單
- 其他人：403

**涉及元件：**
| 元件 | 說明 |
|------|------|
| `ApprovalStep.UseApplicantDesignated` | Entity 欄位 |
| `RequestDesignatedReviewer` | 獨立資料表，取代舊的單欄位設計 |
| `ApprovalFlowService.ResolveStartingStepAsync()` | 驗證指定審核者清單、自審規則、解析起始步驟 |
| `ApprovalTaskHandler.AuthorizeStepAsync()` | 驗證當前等待審核者（min StepOrder, Status=pending） |
| `ApprovalTaskHandler.ProcessReviewAsync()` | 核准後推進到下一位指定審核者，全部通過後推進 ApprovalStep |
| `PaymentRequestReadService.StepMatchClause()` | Dapper SQL：匹配 min(StepOrder) 且 Status=pending 的指定審核者 |
| `ApprovalTaskHandler.GetByIdAsync()` | 單筆查詢含存取控制 |
| 前端各申請表單 | 動態新增/刪除/排序多位指定審核者 UI |

---

## 專案可見性規則

專案清單（`Projects`）在前端的顯示（6 個申請表單下拉 + 專案管理列表 + 詳情頁）套用以下三層規則，依優先序判定，第一個符合者即套用：

### 規則

| 優先序 | 使用者類別 | 可見範圍 |
|---|---|---|
| 1 | Superadmin | 全部 |
| 2 | 部門 Code ∈ `AC`(會計部) / `FIN`(行政財務部) / `Jabez HQ`(雅比斯總公司管理部) / `CEO`(總監室) | 全部 |
| 3 | 一般員工 | 自己部門專案；若 `Department.CanViewSiblings = true` 加上**同 ParentId 的兄弟部門**專案 |

### 套用端點

- `GET /projects/active`（申請表單下拉，僅 `Status = 'active'`）
- `GET /projects`（專案管理列表 / 分頁）
- `GET /projects/{id}`（單筆詳情；不符 scope 回 404）
- `GET /reports/project-water-level`（專案水位表）

### 前置必要條件（資料完整性）

- `Project.DepartmentId` 必填（DB NOT NULL + 前後端驗證；FK `DeleteBehavior.Restrict`）
- `User.DepartmentId` 必填（Superadmin 例外；前後端均驗證）
- `Department.CanViewSiblings` 預設 false，由部門 CRUD 頁維護

### 涉及元件

| 元件 | 說明 |
|---|---|
| `Department.CanViewSiblings` | Entity 旗標，由部門 CRUD 頁維護 |
| `Api/Common/Constants.cs` `DepartmentCodes.FinancialAndAbove` | 財務體系部門 Code 集合（AC / FIN / Jabez HQ / CEO） |
| `Api/Services/IProjectAccessResolver` + `ProjectAccessResolver` | 解析 JWT claims → `ProjectAccessScope(SeeAll, AllowedDepartmentIds)` |
| `Api/Services/Dapper/ProjectReadService` | 四個讀取方法皆依 scope 組合 WHERE（`DepartmentId IN @AllowedIds` 或 `1=0`） |
| `Api/Services/Dapper/ProjectWaterLevelReadService` | 專案水位表同樣依 scope 組合 WHERE |
| `Api/Handlers/ProjectHandler` | 所有 GET 先呼叫 resolver；寫入後以 SeeAll scope 讀回避免寫入者讀不到自己的資料 |
| `Api/Handlers/ProjectWaterLevelHandler` | GET 先呼叫 resolver 取 scope 再傳給 reader |
| JWT `department_id` claim | Resolver 查 CanViewSiblings 與同層兄弟部門用 |
| `Api/Routing/AppRouter` | JWT 驗證後將 principal 寫入 `HttpContext.User`，供 Handler 經 `IHttpContextAccessor` 取得 |

### 6 個申請表單的下拉空值提示

當使用者的可見專案清單為空時，下拉下方顯示灰字「您目前可申請的專案清單為空，請聯絡主管或確認部門設定。」：

- [payment-form](Admin/src/app/features/admin/payment-requests/pages/payment-form/payment-form.html)
- [advance-form](Admin/src/app/features/admin/advance-requests/pages/advance-form/advance-form.html)
- [overtime-request-form](Admin/src/app/features/admin/overtime-requests/pages/overtime-request-form/overtime-request-form.html)
- [travel-request-form](Admin/src/app/features/admin/travel-requests/pages/travel-request-form/travel-request-form.html)
- [travel-payment-form](Admin/src/app/features/admin/travel-payment-requests/pages/travel-payment-form/travel-payment-form.html)
- [holiday-travel-request-form](Admin/src/app/features/admin/holiday-travel-requests/pages/holiday-travel-request-form/holiday-travel-request-form.html)

### 不套用過濾的端點（維持原行為）

- `/approval-tasks`（申請單既有列表過濾已足夠隔離）
- `/payroll`（人事薪資顯示 projectCode）

---

## 簽核升級機制（Escalation）

當簽核步驟設定 `UseApplicantDepartment = true` 且申請人本身就是該步驟的審核者（自審情境，例如部門主管送出申請），系統會根據申請類型自動往上層部門尋找合適的審核者，而非自動核准。

### 各申請類型的升級規則

| | 加班 | 請假 | 出差 | 請款 | 預支 | 沖銷 |
|---|---|---|---|---|---|---|
| 往上層部門找主管 | ✓ | ✓ | ✓ | ✗（自動跳過） | ✗（自動跳過） | ✗（自動跳過） |
| 主管請假時找代理人 | ✓ | ✗ | ✗ | — | — | — |
| 遞迴往上 | ✓ | ✓ | ✓ | — | — | — |
| 停在總監之前 | ✓ | ✓ | ✗ | — | — | — |
| 找不到人時 | 報錯 | 報錯 | 報錯 | — | — | — |

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
  → 到達總監（JobTitleId=5）前停止
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
- Claims：`sub`（使用者 ID）、`name`、`email`、`jti`、`roles`、`permissions`、`is_superadmin`、`department_name`、`department_code`、`job_title_name`、`job_title_level`、`avatar`

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

## LINE 整合

### 功能範圍

- **LINE 帳號綁定**：員工在右上角 profile dropdown 透過 LINE OAuth 綁定 LINE userId
- **LINE 簽核通知推播**：6 種簽核通知同時推播 LINE Flex Message（Email 保留）
- LINE Login 僅用於取得 userId 進行綁定，不作為登入方式
- 不做 LIFF、不做 Webhook

### 綁定流程

```
1. 用戶在 profile dropdown 點擊「綁定 LINE」
2. 前端呼叫 GET /line/bind-url → 取得 LINE OAuth URL + state
   (URL 含 bot_prompt=aggressive，授權後自動導向「加 OA 為好友」畫面)
3. 前端存 state 到 sessionStorage，導向 LINE 授權頁
4. 用戶在 LINE 授權 → 接著進入「加 OA 為好友」畫面 → 回導 /line/bind-callback?code=xxx&state=yyy
5. 前端驗證 state → POST /line/bind（帶 JWT + code）
6. 後端用 code 向 LINE 換取 id_token → 驗證取得 userId → 寫入 User.LineUserId
   後端並呼叫 GET /v2/bot/profile/{userId} 檢查好友狀態，回傳 IsBotFriend
7. 導回 dashboard，profile dropdown 依三態顯示：
   - 未綁定：顯示「綁定 LINE」按鈕
   - 已綁定 + OA 好友：顯示「LINE 已綁定」
   - 已綁定 + 非 OA 好友：顯示警告提示 +「加入好友」按鈕 +「重新檢查」
```

> **為何一定要加 OA 為好友**：LINE Messaging API `push-message` 硬性規定接收者必須已加 OA 為好友，否則 LINE 會回 HTTP 400 `The user hasn't added the LINE Official Account as a friend, or the LINE Official Account has been blocked by the user.`，推播一律失敗（只在 log 留錯誤訊息，Email 不受影響）。

### LINE 通知推播

簽核通知在 Email 發送後，自動查詢收件人的 `LineUserId`，有綁定則推播 Flex Message。推播失敗不影響 Email。

`LineService.PushMessageAsync` 會偵測 LINE 回應 body，若發現「未加好友 / 已封鎖」錯誤，會以 `LogError` 明確記錄原因（其他錯誤維持 warning），方便排查。

**6 種推播類型**：
1. `BuildReviewerMessage` — 待審核通知
2. `BuildApplicantResultMessage` — 審核結果（核准/退回/拒絕）
3. `BuildSpecificReviewerMessage` — 指定/升級/代理審核者通知
4. `BuildFinanceDeptMessage` — 財務撥款通知
5. `BuildRefundMessage` — 預支沖銷超額通知
6. `BuildTravelRefundMessage` — 出差沖銷超額通知

### 涉及元件

| 元件 | 說明 |
|------|------|
| `User.LineUserId` / `User.LineLinkedAt` | Entity 欄位 |
| `ILineService` / `LineService` | LINE API 封裝（token 換取、推播、好友狀態查詢） |
| `ILineService.IsBotFriendAsync` | 呼叫 `GET /v2/bot/profile/{userId}` 判斷是否為 OA 好友 |
| `LineFlexMessageBuilder` | 6 種 Flex Message 模板（品牌綠 #699F34 標頭） |
| `LineHandler` | 4 個 API：bind-url / bind / unbind / binding-status（後 3 者回傳 IsBotFriend） |
| `LineBindingStatusDto` | `(IsBound, LineLinkedAt, IsBotFriend)` |
| `ApprovalNotificationService` | 6 個通知方法各加入 LINE 推播 |
| 前端 `LineService` | `core/auth/services/line.service.ts`（共享 `isBound` / `isBotFriend` signal） |
| 前端 `ProfileDropdown` | 三態綁定 UI（未綁定 / 已綁定未加好友 / 已綁定為好友） |
| 前端 `LineBindCallback` | OAuth callback 頁面 |

### LINE 設定

**後端** `local.settings.json`（雙底線命名）：
- `Line__LoginChannelId` — LINE Login Channel ID
- `Line__LoginChannelSecret` — LINE Login Channel Secret
- `Line__MessagingChannelAccessToken` — Messaging API Long-lived Token
- `Line__MessagingChannelSecret` — Messaging API Channel Secret
- `Line__CallbackUrl` — OAuth callback URL

**前端** `environment.ts`：
- `lineLoginChannelId` — LINE Login Channel ID
- `lineCallbackUrl` — OAuth callback URL
- `lineOaFriendUrl` — LINE OA 加好友 URL（格式 `https://line.me/R/ti/p/@{basicId}`），供「已綁定但未加好友」狀態下的「加入好友」按鈕使用

> **重要**：
> - LINE Login 和 Messaging API 須在同一 Provider 下建立，LINE 才會使用相同 userId。
> - OAuth URL 必須帶 `bot_prompt=aggressive` 參數（已內建於 `LineHandler.GetBindUrlAsync`），綁定後 LINE 才會自動導向「加 OA 為好友」畫面；否則用戶只綁定 Login 但未加好友，所有 Messaging API 推播一律失敗。

---

## 打卡提醒（TimerTrigger + LINE 推播）

### 功能範圍

- 每日上班前 2 分鐘、下班前 2 分鐘各一次，自動推播 LINE Flex Message 提醒員工打卡
- 無需前端介入：員工即使未登入系統，只要已綁定 LINE 即可收到
- 排程由 `AttendanceReminderFunction` TimerTrigger 每分鐘觸發

### 觸發邏輯

1. Cron `0 */1 * * * *`（UTC 每分鐘）進入 Function
2. 透過 `Clock.Now`（台北時區）取得當前 `HH:mm`
3. 比對 `SystemSetting.WorkStartTime - 2min` / `WorkEndTime - 2min`；未命中直接 return
4. 週末（Saturday/Sunday）直接 return
5. 命中 → Dapper 查詢對象 → LINE 推播

### 對象過濾條件（Dapper SQL）

- `User.LineUserId` 不為 null 且不為空字串
- `User.IsSuperAdmin = 0`
- `User.Status = 'active'`
- 未離職（`ResignDate` 為 null 或 > 今日）
- **非請假中**：今日不落在任何 `LeaveRequest.ApprovalStatus='approved'` 範圍內
- **未打卡**：上班提醒排除今日 `AttendanceRecord.ClockInTime` 已有值者；下班提醒排除 `ClockOutTime` 已有值者

### 手動觸發（除錯）

`POST /admin/attendance-reminder/run?type=clockIn|clockOut`（僅 Superadmin）
繞過時點與週末檢查，強制對符合條件員工推播；其餘過濾條件保留。回傳 `{ type, pushedCount }`。

### 設計決策

- **Cron Timezone**：UTC 觸發 + 內部 `Clock.Now` 比對，不依賴 `WEBSITE_TIME_ZONE` / `TZ` 環境變數，相容 Linux Consumption Plan
- **幂等性**：不持久化發送紀錄；依賴 Azure Functions Timer 的 singleton lock（AzureWebJobsStorage blob lease）保證同一 cron tick 只觸發一次，加上 `RunOnStartup=false` 與 `IsPastDue` 跳過防止意外重複
- **成本**：Consumption Plan 每月 43,200 次執行、~553 GB-s，遠低於免費額度（實質成本 0）

### 涉及元件

| 元件 | 說明 |
|------|------|
| `AttendanceReminderFunction` | TimerTrigger entrypoint |
| `IAttendanceReminderService` / `AttendanceReminderService` | 時點判斷 + 推播協調 |
| `IAttendanceReminderReadService` / `AttendanceReminderReadService` | Dapper 查詢符合條件的員工 |
| `AttendanceReminderRecipientDto` | `(UserId, LineUserId, UserName)` |
| `LineFlexMessageBuilder.BuildAttendanceReminderMessage` | 品牌綠 Flex Message 模板 |
| `AttendanceReminderAdminHandler` | 手動觸發 HTTP 端點（Superadmin） |

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
2. **假日津貼** = 日薪 × 假日執行活動天數（**上個月**歸月：以已核准假日執行活動申請的 `EndDate` 所屬月份歸月，獎金計入次月薪資。例：3 月活動 → 4 月薪資；跨月活動（如 3/30~4/2）EndDate=4/2 歸 4 月 → 5 月薪資）
3. **勞保費 / 健保費**：根據底薪查詢勞健保級距表（向上取最近級距）
4. **事假扣薪** = 日薪 × 事假天數（按天數扣除全額薪資）
5. **病假扣薪** = 日薪 × 0.5 × 病假天數（按天數扣除半薪）
6. **實領薪水** = 底薪 + 假日津貼 - 勞保費 - 健保費 - 事假扣薪 - 病假扣薪

> 人事薪資為動態計算，不儲存於資料庫。前端可匯出 PDF 薪資表。
> 薪資編輯頁與薪資明細信件額外顯示該月**所有已核准的請假紀錄**（全假別，非僅事假/病假）。

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

### 頁面排版規範

所有 Form / Detail 頁面須遵循以下統一排版規範，確保視覺一致性與專業感。

#### 頁面分類與寬度規則

| 類型 | 統一寬度（RWD，皆含 `col-12`） | 適用頁面 |
|------|------|------|
| A. 簡單主檔 | `col-12 col-lg-8 col-xl-6` | department, job-title, permission, insurance-bracket, project |
| B. 複雜主檔 | `col-12 col-xl-8` | user, role, payroll |
| C. 申請（無明細表格） | `col-12 col-lg-10 col-xl-8` | leave-request, overtime-request |
| C. 申請（有明細表格） | `col-12 col-xl-10` | payment, travel, advance, write-off, travel-write-off |
| D. 詳情頁 | `col-12 col-xl-10` | advance-detail, write-off-detail, travel-write-off-detail |
| E. 審核頁 | `col-12 col-lg-10 col-xl-8` | approval-task-review |
| G. 設定頁 | `col-12 col-md-6 col-xl-4`（多欄並排） | settings |

> 所有寬度必須保留 `col-12` 基礎，確保手機裝置全寬顯示。外層容器統一 `container-fluid py-3`，col 外層包 `<div class="row g-4">`。

#### 頁頭結構

**主檔 / 申請表單：**
```html
<div class="flex items-center gap-2 mb-6">
  <a routerLink="..." class="btn btn-sm btn-outline-secondary">
    <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
  </a>
  <h4 class="mb-0">{{ title }}</h4>
</div>
```

**詳情頁（含狀態 badge + 操作按鈕）：**
```html
<div class="flex flex-wrap items-center justify-between gap-2 mb-6">
  <div class="flex items-center gap-2 flex-wrap">
    <a routerLink="..." class="btn btn-sm btn-outline-secondary">←</a>
    <h4 class="mb-0">{{ title }} {{ requestNo }}</h4>
    <span [class]="'badge ' + statusClass">{{ statusLabel }}</span>
  </div>
  <div class="flex flex-wrap gap-2"><!-- 操作按鈕 --></div>
</div>
```

#### 卡片頭部統一樣式

所有卡片（card）統一使用以下 card-header 結構：

```html
<div class="card border-0 shadow-sm">
  <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
    <svg class="sa-icon text-primary" style="stroke: currentColor">
      <use href="/assets/icons/sprite.svg#ICON_NAME"></use>
    </svg>
    卡片標題
  </div>
  <div class="card-body"><!-- 內容 --></div>
</div>
```

#### 卡片分組與排序

**一般申請表單（payment / leave / travel / overtime / advance）：**
1. 狀態提示卡（條件式，唯讀時顯示）
2. 基本資訊卡（所有表單欄位 + 備註）
3. 明細表格卡（如有：發票/費用/預算明細）
4. **指定審核者卡（獨立卡片，icon `#users`）**
5. 簽核流程（`<app-approval-timeline>`）

**沖銷申請表單（write-off / travel-write-off）：**
1. 主單選擇卡（預支單/出差單）
2. 上傳發票卡
3. 花費明細表格卡
4. 沖銷備註卡
5. **指定審核者卡（獨立卡片，icon `#users`）**

> 指定審核者一律為獨立卡片，不得內嵌於其他卡片中。

#### 按鈕位置規範

**主檔表單底部：**
```html
<div class="mt-6 flex gap-2">
  <button type="submit" class="btn btn-primary">{{ isEdit ? '更新' : '建立' }}</button>
  <a routerLink="..." class="btn btn-outline-secondary">取消</a>
</div>
```

**申請表單底部（編輯模式）：**
```html
<div class="mt-6 flex gap-2">
  <button type="submit" class="btn btn-outline-secondary">{{ isEdit ? '儲存' : '儲存草稿' }}</button>
  <button type="button" class="btn btn-primary" (click)="submitForApproval()">送出申請</button>
  <a routerLink="..." class="btn btn-outline-secondary">取消</a>
</div>
```

**申請表單底部（唯讀模式）：**
```html
<div class="mt-6">
  <a routerLink="..." class="btn btn-outline-secondary">返回列表</a>
</div>
```

#### 欄位間距規範

- 卡片內每個欄位 / row 之間：`mb-4`，最後一個 `mb-0`
- 卡片內 row gutter：`row g-3`
- 外層 layout row gutter：`row g-4`
- Label：`form-label fw-500`
- 卡片之間：`mt-6`

#### 狀態提示卡規範

使用 `@if/@else if` 鏈式（不用 `@switch`），四種狀態色彩：

| 狀態 | 背景色 | 文字色 | Icon |
|------|--------|--------|------|
| pending | `bg-[rgba(13,110,253,0.08)]` | `text-primary` | `#clock` |
| returned | `bg-[rgba(255,193,7,0.08)]` | `text-warning` | `#alert-triangle` |
| approved | `bg-[rgba(37,162,68,0.08)]` | `text-success` | `#check-circle` |
| rejected | `bg-[rgba(220,53,69,0.08)]` | `text-danger` | `#x-circle` |

文案統一：「此申請{狀態描述}，不可再修改。」

#### RWD 注意事項

- 所有 `col` 必須包含 `col-12` 基礎（mobile-first 全寬）
- 明細表格使用 `table-responsive` 確保手機可橫向捲動
- 詳情頁頁頭使用 `flex-wrap` 確保按鈕換行

---

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
