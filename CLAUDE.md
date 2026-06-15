# 請款簽核及工時管理系統 - CLAUDE.md

## 專案概述

本系統為企業內部的**請款簽核系統**與**請假/出差/加班申請管理系統**，提供費用申請流程簽核、員工資料管理、角色與權限控管、審核任務追蹤、**出勤打卡**（含 GPS 定位）等功能。

---

## 專案結構

```
/
├── Admin/          # 前端 Angular 21 應用程式
├── Api/            # 後端 Azure Functions .NET 9 API
├── docs/           # 設計與規範文件
│   ├── frontend-design.md   # 前端設計規範（CIS 色彩、卡片、Tab、明細列表、按鈕、icon、表單、檔案上傳…）
│   ├── backend-design.md    # 後端設計規範（Handler、DTO、Dapper、EF Core、Router、JWT、時區、檔案上傳…）
│   ├── api-routes.md        # API 路由清單
│   ├── database-schema.md   # 34 個 entity 清單
│   ├── authentication.md    # JWT 規格 / 登入流程 / Superadmin
│   └── business/            # 業務功能（11 個檔，每業務一檔）
│       ├── application-forms.md      # 9 種申請表類型總覽
│       ├── leave-rules.md            # 請假規則
│       ├── approval-flow.md          # 請款簽核流程
│       ├── approval-escalation.md    # 簽核升級機制
│       ├── pdf-signatures.md         # PDF 簽名欄
│       ├── department-visibility.md  # 部門可見性
│       ├── line-integration.md       # LINE 整合
│       ├── attendance-reminder.md    # 打卡提醒
│       ├── payroll-formula.md        # 薪資公式
│       ├── hr-profile.md             # 員工人事資料卡
│       └── notifications.md          # 通知系統清單（Email + LINE）
└── Jabez.sln       # Visual Studio 方案檔
```

---

## 優先執行事項

每次收到任務時：

- **UI / 前端任務**：必須優先啟動 `frontend-design` skill，並先讀 [docs/frontend-design.md](docs/frontend-design.md) 確認排版、卡片、明細列表、按鈕等規範後再進行設計或實作
- **後端任務**：先讀 [docs/backend-design.md](docs/backend-design.md) 確認 Handler / DTO / Dapper / EF Core / Router 規範後再實作

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

## 文件導覽

> **CLAUDE.md 為導讀文件**；所有設計規範、業務細節、API 清單一律拆到 `docs/`。修改任何業務或技術規範時，**必須同步更新對應文件**（見下方「功能新增與修改規範」）。

### 設計規範
- [docs/frontend-design.md](docs/frontend-design.md) — 前端設計規範（CIS 色彩、卡片、Tab、明細列表、按鈕、icon、表單、檔案上傳）
- [docs/backend-design.md](docs/backend-design.md) — 後端技術規範（Handler、DTO、Dapper、EF Core、Router、JWT、時區、檔案上傳、輕量端點模式）

### 參考清單
- [docs/api-routes.md](docs/api-routes.md) — API 路由清單（全部端點分類整理）
- [docs/database-schema.md](docs/database-schema.md) — 34 個資料表實體清單
- [docs/authentication.md](docs/authentication.md) — JWT 規格 / 登入流程 / Superadmin 隱藏帳號

### 業務功能（docs/business/）
- [application-forms.md](docs/business/application-forms.md) — 9 種申請表類型總覽 + 流程關係 + holiday vs travel 差異
- [leave-rules.md](docs/business/leave-rules.md) — 請假規則（15 種假別 / 時間單位 / 年假 / 喪假 / 補休 / 重疊驗證）
- [approval-flow.md](docs/business/approval-flow.md) — 請款簽核流程（簽核步驟 / 批次核准 / 自審 / 上層級 / 指定審核 / 跨步驟去重）
- [approval-escalation.md](docs/business/approval-escalation.md) — 簽核升級機制（找上層部門主管 + 代理人）
- [pdf-signatures.md](docs/business/pdf-signatures.md) — 7 個 PDF 動態簽名欄規則
- [department-visibility.md](docs/business/department-visibility.md) — 部門可見性 ProjectAccessScope
- [line-integration.md](docs/business/line-integration.md) — LINE OAuth 綁定 + 簽核 / 撥款通知推播
- [attendance-reminder.md](docs/business/attendance-reminder.md) — TimerTrigger 打卡提醒 + 推播紀錄持久化
- [payroll-formula.md](docs/business/payroll-formula.md) — 薪資 7 條公式 + 健保眷屬計算
- [hr-profile.md](docs/business/hr-profile.md) — 員工人事資料卡（3 Tab + 9 子表 + 整批替換）
- [notifications.md](docs/business/notifications.md) — 通知系統清單（9 種 Email + 9 種 LINE Flex Message + 系統開關 + 打卡提醒）

---

## 前端：Admin（Angular 21以上）

> **設計規範與技術棧詳見** [docs/frontend-design.md](docs/frontend-design.md)（CIS 色彩、Logo、技術棧、設計 token 一律統一定義於該文件）

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
    │   ├── users/          # 使用者管理（user-form 含 3 Tab：員工基本資料 / 人事資料卡 / 健保眷屬；含 employee-profile.service / hr-profile-pdf.service / 9 組 FormArray）
    │   ├── roles/          # 角色管理（僅 Superadmin）
    │   ├── permissions/    # 權限管理（僅 Superadmin）
    │   ├── departments/    # 部門管理
    │   ├── job-titles/     # 職稱管理
    │   ├── vendors/        # 廠商管理（含 vendor-quick-add-modal；統編 blur 自動帶出 GCIS 公司資料；含存摺封面上傳）
    │   ├── approvals/      # 簽核流程設定（ApprovalItem + Steps）
    │   ├── approval-tasks/ # 待審核任務清單
    │   ├── projects/       # 專案管理
    │   ├── payment-requests/  # 請款申請
    │   ├── leave-requests/    # 請假申請
    │   ├── travel-payment-requests/ # 出差請款申請（小額已代墊直接請款，無沖銷）
    │   ├── travel-requests/   # 出差預支申請（走沖銷流程）
    │   ├── holiday-travel-requests/ # 假日執行活動申請（共用 TravelRequest entity，IsHolidayTravel=true，計入假日津貼）
    │   ├── overtime-requests/ # 加班申請（走簽核流程）
    │   ├── advance-requests/  # 預支申請
    │   ├── write-off-requests/ # 預支沖銷申請（獨立簽核流程）
    │   ├── travel-write-off-requests/ # 出差預支沖銷申請（獨立簽核流程）
    │   ├── insurance-brackets/ # 勞健保級距維護
    │   ├── payroll/           # 人事薪資（月薪計算 + PDF 匯出）
    │   ├── attendance-reminder-logs/ # 打卡提醒推播紀錄（僅 Superadmin）
    │   ├── payment-reminder-logs/ # 撥款提醒推播紀錄 + 手動觸發（僅 Superadmin）
    │   ├── reports/        # 報表（出缺勤 / 加班 / 款項統計 / 專案水位）；款項統計 1 個 endpoint 支援 全部 + 6 個類別 dropdown（全部 / 請款 / 預支 / 預支沖銷 / 出差請款 / 出差預支 / 出差預支沖銷；「全部」為 6 種 UNION ALL），權限只看 `reports-payment:read`，不需各別 `xxx-requests:read`
    │   └── settings/       # 系統設定（含 PaymentReminderDaysBefore 撥款提醒天數）
    └── error/
        └── pages/ (error-403, error-404, error-500)
```

### 開發規範

> **詳見** [docs/frontend-design.md](docs/frontend-design.md) §13 路由 / §14 HTTP service / §15 Signal / §17 命名

- 所有 API 路徑統一在 `Admin/src/environments/environment.ts` 的 `apiUrl` 管理
- Token 儲存於 `localStorage`，由 `core/auth/interceptors/auth.interceptor.ts` 自動附加 Bearer Token

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

> **設計規範與技術棧詳見** [docs/backend-design.md](docs/backend-design.md)（Handler / DTO / Dapper / EF Core / Router / JWT / 時區 / 檔案上傳 / 命名 / Code Review Checklist 一律統一定義於該文件）

### 目錄結構

```
Api/
├── Functions/
│   ├── RouterFunction.cs              # HttpTrigger，catch-all route {*route}
│   ├── AttendanceReminderFunction.cs  # TimerTrigger：限定 7-9 / 16-18 Taipei 時段每分鐘檢查上下班前 2 分鐘，命中則 LINE 推播；cron 由 `AttendanceReminderCron` app setting 控制
│   └── PaymentReminderFunction.cs     # TimerTrigger：每日 09:00 Taipei 跑撥款日將屆提醒；cron 由 `PaymentReminderCron` 控制；提前天數讀 `SystemSetting.PaymentReminderDaysBefore`，推給財務體系部門全員
├── Routing/
│   └── AppRouter.cs                   # C# 12 List Pattern 路由分派器
├── Handlers/                          # 23 個 Handler（業務邏輯）
│   ├── AuthHandler.cs                 # 登入、刷新 Token
│   ├── UserHandler.cs                 # 使用者 CRUD（含原住民 / 低收入 / 殘障證明 + 健保 / 勞保覆寫）
│   ├── EmployeeProfileHandler.cs     # 員工人事資料卡 GET / PUT（multipart：HR JSON + 身分證正反面 + 最高學歷證明）
│   ├── RoleHandler.cs
│   ├── PermissionHandler.cs
│   ├── DepartmentHandler.cs
│   ├── JobTitleHandler.cs
│   ├── VendorHandler.cs               # 廠商管理 CRUD（multipart 支援存摺封面上傳；lookup / lookup-by-tax-id / POST 開放任何登入者；刪除受 PaymentRequest 引用保護）
│   ├── ApprovalHandler.cs             # ApprovalItem + Steps CRUD
│   ├── ApprovalTaskHandler.cs         # 待審核任務查詢與審核動作
│   ├── ProjectHandler.cs
│   ├── PaymentRequestHandler.cs       # 請款申請 CRUD（單號 PR-yyyyMMdd-NNN）
│   ├── LeaveRequestHandler.cs
│   ├── TravelRequestHandler.cs        # 出差預支申請 CRUD（單號 TR-yyyyMMdd-NNN；假日執行活動為 HTR-yyyyMMdd-NNN；預支後沖銷）
│   ├── TravelPaymentRequestHandler.cs # 出差請款申請 CRUD（單號 TPR-yyyyMMdd-NNN；小額代墊直接請款）
│   ├── OvertimeRequestHandler.cs      # 加班申請 CRUD
│   ├── AdvanceRequestHandler.cs       # 預支申請 CRUD（單號 ADV-yyyyMMdd-NNN）
│   ├── WriteOffRequestHandler.cs      # 預支沖銷申請 CRUD（獨立簽核流程）
│   ├── TravelWriteOffRequestHandler.cs # 出差預支沖銷申請 CRUD（獨立簽核流程）
│   ├── AttendanceHandler.cs           # 打卡（上班/下班/加班開始/加班結束）
│   ├── InsuranceBracketHandler.cs    # 勞健保級距 CRUD
│   ├── PayrollHandler.cs             # 人事薪資查詢（月薪計算）
│   ├── LineHandler.cs                # LINE 帳號綁定/解綁 + 月度推播用量查詢（line-quota:read）
│   ├── AttendanceReminderAdminHandler.cs # 打卡提醒手動觸發（Superadmin，除錯用）
│   ├── AttendanceReminderLogHandler.cs   # 打卡提醒推播紀錄查詢（Superadmin）
│   ├── PaymentReminderLogHandler.cs      # 撥款提醒推播紀錄查詢 + 手動觸發（Superadmin）
│   ├── SettingsHandler.cs
│   └── HealthHandler.cs
├── Middleware/
│   └── ExceptionMiddleware.cs         # 全域例外處理
├── Data/
│   ├── AppDbContext.cs                # EF Core DbContext（含 Migration 自動套用）
│   ├── AppDbContextFactory.cs         # 用於 CLI Migration
│   ├── Configurations/                # EF Core 實體對應設定（31 個，新增 EmployeeProfile + 9 張子表 + 健保眷屬）
│   ├── Migrations/                    # EF Core Migration 檔案
│   └── Seed/                          # 一次性員工人事資料匯入工具（EmployeeImporter + RocDateParser + EmployeeImportDtos + employee-import.json；RUN_EMPLOYEE_IMPORT 旗標觸發，IMPORT_UPLOAD_FILES 控制附件上傳）
├── Models/
│   ├── Entities/                      # 40 個資料庫實體（新增 EmployeeProfile / EducationRecord / EmploymentHistoryRecord / FamilyMember / ProfessionalTraining / LanguageAbility / JobTransferRecord / RewardPunishmentRecord / SalaryAdjustmentRecord / HealthInsuranceDependent / **4 個分期撥款表 PaymentRequestInstallment / AdvanceRequestInstallment / TravelRequestInstallment / TravelPaymentRequestInstallment** / **PaymentReminderLog**）
│   └── Dtos/                          # 19 個 DTO 檔案（新增 EmployeeProfileDtos / **InstallmentDtos**）
├── Services/
│   ├── IJwtService.cs
│   ├── JwtService.cs                  # HS256 JWT 產生與驗證
│   ├── IEscalationService.cs          # 簽核升級服務介面
│   ├── EscalationService.cs           # 簽核升級邏輯（上層部門主管遞迴 + 代理人）
│   ├── EscalationResult.cs            # 升級結果 record
│   ├── ILineService.cs               # LINE API 操作介面
│   ├── LineService.cs                # LINE Platform REST API 封裝（token 換取 + 推播 + 月度 quota 查詢）
│   ├── PushResult.cs                 # LINE 推播結果 record（含 ErrorCategory 分類）
│   ├── LineFlexMessageBuilder.cs     # 6 種簽核通知 + 打卡提醒的 LINE Flex Message 模板
│   ├── IAttendanceReminderService.cs # 打卡提醒服務介面
│   ├── AttendanceReminderService.cs  # 打卡提醒協調：判斷時點、過濾對象、推播 LINE
│   ├── IPaymentReminderService.cs    # 撥款提醒服務介面
│   ├── PaymentReminderService.cs     # 撥款日將屆提醒：撈 4 種待撥 installments、過濾財務部、推 LINE+Email、寫 PaymentReminderLog（同日去重）
│   ├── InstallmentValidator.cs       # 分期撥款共用驗證：序號連續 / SUM == 總額 / 已撥款列保護
│   ├── InstallmentUpsertService.cs   # 分期撥款共用 upsert 核心（validate+diff，不 SaveChanges）；獨立 endpoint 與「財務核准當下原子寫入」共用；以 IInstallmentEntity 泛型化
│   ├── InstallmentUpsertResult.cs    # UpsertInstallments 結果 record
│   ├── IGcisService.cs               # 政府開放資料 GCIS 商工登記查詢介面
│   ├── GcisService.cs                # GCIS Open Data REST API 包裝（以統編查公司名稱 / 地址 / 負責人）
│   └── Dapper/                        # Dapper 讀取服務（含 EmployeeProfileReadService）
│       ├── UserReadService.cs
│       ├── RoleReadService.cs
│       ├── DepartmentReadService.cs
│       ├── JobTitleReadService.cs
│       ├── VendorReadService.cs
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
│       ├── AttendanceReminderLogReadService.cs
│       ├── InsuranceBracketReadService.cs
│       ├── EmployeeProfileReadService.cs   # 一次 QueryMultiple 讀回 EmployeeProfile + 9 張子表
│       ├── InstallmentReadService.cs       # 共用：依父表 ID 撈 4 種 installments + JOIN User SignatureUrl + 三態 status 計算
│       ├── PaymentReminderReadService.cs   # UNION 4 種 installments，撈 PaidAt 為空且 ExpectedDate 在 N 天內的紀錄
│       └── PayrollReadService.cs           # 月薪計算（含健保眷屬數 + 覆寫值 fallback）
├── Common/
│   ├── ApiResponse.cs                 # 統一回應格式 ApiResponse<T>
│   ├── AppException.cs                # 自定義例外
│   └── Constants.cs
├── host.json
├── local.settings.json                # 本地開發設定（不進版控）
└── Api.csproj
```

### 路由分派設計 / Dapper vs EF Core 使用原則

> **詳見** [docs/backend-design.md §3 路由分派設計](docs/backend-design.md#3-路由分派設計) 與 [§6 Dapper vs EF Core 使用原則](docs/backend-design.md#6-dapper-vs-ef-core-使用原則)

### API 路由規劃

> **完整路由清單詳見** [docs/api-routes.md](docs/api-routes.md)

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

> **詳見** [docs/database-schema.md](docs/database-schema.md)（34 個 entity 清單）

---

## 申請表類型總覽（9 種）

> **詳見** [docs/business/application-forms.md](docs/business/application-forms.md)

---

## 請假規則

> **詳見** [docs/business/leave-rules.md](docs/business/leave-rules.md)（15 種假別、時間單位、年假、喪假、補休、重疊驗證）

---

## 請款簽核流程

> **詳見** [docs/business/approval-flow.md](docs/business/approval-flow.md)（簽核步驟、批次核准、自審跳過、上層級審核、指定審核、跨步驟去重）

---

## PDF 簽名欄

> **詳見** [docs/business/pdf-signatures.md](docs/business/pdf-signatures.md)

---

## 部門可見性規則

> **詳見** [docs/business/department-visibility.md](docs/business/department-visibility.md)

---

## 簽核升級機制（Escalation）

> **詳見** [docs/business/approval-escalation.md](docs/business/approval-escalation.md)

---

## 認證系統

> **詳見** [docs/authentication.md](docs/authentication.md)（JWT 規格 / 登入流程 / Superadmin）

---

## LINE 整合

> **詳見** [docs/business/line-integration.md](docs/business/line-integration.md)

---

## 打卡提醒（TimerTrigger + LINE 推播）

> **詳見** [docs/business/attendance-reminder.md](docs/business/attendance-reminder.md)

---

## 環境設定

> 本地開發 `local.settings.json` 範例詳見 [docs/backend-design.md §16 環境變數慣例](docs/backend-design.md#16-環境變數慣例)

---

## 薪水計算公式（人事薪資模組）

> **詳見** [docs/business/payroll-formula.md](docs/business/payroll-formula.md)

---

## 員工人事資料卡（HR Profile）

> **詳見** [docs/business/hr-profile.md](docs/business/hr-profile.md)

---

## 輕量讀取端點模式（Public Lookup Pattern）

> **詳見** [docs/backend-design.md §13 輕量讀取端點模式](docs/backend-design.md#13-輕量讀取端點模式lightweight-lookup-pattern)（已採用清單、設計原則、何時新增、歷史教訓）

---

## 開發注意事項

> 後端技術規範（EF Migration / Dapper / ApiResponse / 環境變數 / 時區 / 檔案上傳 / 註解同步）詳見 [docs/backend-design.md](docs/backend-design.md)
> 前端技術規範詳見 [docs/frontend-design.md](docs/frontend-design.md)

業務規範與本專案特定行為：

1. **CORS**：本地開發時 Api 已允許所有來源（`"CORS": "*"`）
2. **JWT 過期處理**：前端 `auth.interceptor.ts` 攔截 401 後自動 Refresh，失敗則導向登入頁
3. **預設密碼**：`AuthHandler` 使用 BCrypt 驗證；新使用者預設密碼為 `Birthday yyyyMMdd`，Seed Superadmin 為 `Admin@123`（正式環境必須變更）
4. **DB 自動初始化**：啟動時自動執行 EF Migration 並 Seed 初始資料（Superadmin、預設 Role/Permission）
5. **測試規範**：測試功能時，必須實際輸入測試資料進行測試，不得僅以目視或靜態檢查代替。確認 CRUD 流程（新增、讀取、更新、刪除）與業務邏輯皆正常運作後，方可視為測試通過。

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

**每次新增或修改功能時，必須同步更新以下六處：**

1. **Admin/**（前端）：新增/修改對應的 Component、Service、Route、Guard
2. **Api/**（後端）：新增/修改對應的 Handler、Dtos、Entities、Migration（如有 DB 異動）
3. **CLAUDE.md**：本檔為導讀層，更新「文件導覽」與「目錄結構」（不再保存業務細節）
4. **[docs/frontend-design.md](docs/frontend-design.md)**：**只要前端的視覺 / 互動規範有任何調整**（新 pattern、按鈕樣式、icon 用法、表單佈局、明細列表、Tab 結構、檔案上傳流程、設計 token 增減等），**必須同步更新**該文件對應章節
5. **[docs/backend-design.md](docs/backend-design.md)**：**只要後端的技術規範有任何調整**（新 Handler / Service 模式、DTO 命名、Dapper / EF Core 用法、Router 機制、JWT / 時區 / 檔案上傳規範、命名規則等），**必須同步更新**該文件對應章節
6. **對應業務 / 參考檔**：
   - 業務變動 → `docs/business/<對應>.md`（例如新增請假規則 → `leave-rules.md`、改簽核流程 → `approval-flow.md`、新通知類型 → `notifications.md` + `line-integration.md`）
   - API 路由變動 → [docs/api-routes.md](docs/api-routes.md)
   - 資料表 / Entity 變動 → [docs/database-schema.md](docs/database-schema.md)
   - 認證機制變動 → [docs/authentication.md](docs/authentication.md)

> 若只改其中一處而未同步其他五處，視為不完整的變更。
> **單一真相來源（Single Source of Truth）**：
> - 視覺 / 互動規範 → [docs/frontend-design.md](docs/frontend-design.md)
> - 後端技術規範 → [docs/backend-design.md](docs/backend-design.md)
> - API 路由清單 → [docs/api-routes.md](docs/api-routes.md)
> - 資料表 entity 清單 → [docs/database-schema.md](docs/database-schema.md)
> - 認證機制 → [docs/authentication.md](docs/authentication.md)
> - 業務功能 → [docs/business/](docs/business/) 對應檔
> - 通知系統清單（Email + LINE） → [docs/business/notifications.md](docs/business/notifications.md)
> - 業務導讀 / 文件導航 → CLAUDE.md（本檔）

### UI 樣式一致性 / 頁面排版規範

> **完整規範詳見** [docs/frontend-design.md](docs/frontend-design.md)
>
> 涵蓋：CIS 色彩系統 §2、頁面寬度與容器 §3、卡片元件 §4、Tab UI §5、表單規範 §6、明細列表（含 ⚠ 刪除按鈕標準）§7、按鈕規範 §8、狀態提示卡 §9、Icon 系統 §10、Toastr §11、檔案上傳 §12、路由 §13、HTTP service §14、Signal §15、控制流 §16、命名 §17、Code Review Checklist §19。

---

### 程式碼寫法與架構一致性

**前端（Angular）：詳見** [docs/frontend-design.md](docs/frontend-design.md)（Standalone Component、Signal、HTTP service、Lazy Loading、三層目錄結構等架構規範統一定義於此）

**後端（.NET）：詳見** [docs/backend-design.md](docs/backend-design.md)（Handler / DTO / Dapper ReadService / EF Core / Router / Migration / ApiResponse 等架構規範統一定義於此）

---

## Coding Style 一致性（重要）

> **背景**：本專案的程式碼會在**不同時段、不同對話**中持續開發。為避免同一專案出現多種寫法、命名風格、檔案結構，**每次撰寫或修改程式碼前，必須先參考既有相似檔案的寫法**，再依照相同模式進行。

### 強制原則

1. **先讀後寫**：新增功能前，**必須先讀至少一份同類型既有檔案**作為範本（例如新增 Handler 前先讀 `PaymentRequestHandler.cs`、新增 Angular Form 前先讀 `payment-form.ts`）。不可憑空想像架構。
2. **跟隨既有模式**：命名、檔案結構、目錄階層、import 順序、方法排列順序、錯誤處理風格、回應格式 **一律比照既有檔案**。發現既有寫法有問題時，先提出討論再統一重構，不可單獨在新檔案改寫。
3. **同類功能同寫法**：所有 Handler 套用相同的 try/await/ApiResponse 模式；所有 ReadService 套用相同的 Dapper SQL 風格；所有 Angular Component 套用相同的 Signal + Service 注入模式。
4. **禁止個人風格混入**：不得引入既有檔案沒用過的程式設計模式（如 RxJS Observable 取代 Signal、自訂 IoC 容器取代 DI、Repository Pattern 取代 Dapper ReadService）。

### Coding Style Checklist（每次撰寫前自我檢查）

#### 後端（.NET）

> **詳見** [docs/backend-design.md §17.1 後端 Checklist](docs/backend-design.md#171-後端net)（涵蓋 Handler 命名 / ApiResponse / AppException / DTO 位置 / Dapper vs EF Core / async-await / Clock.Now / 路由次序 / Migration 等項目）

#### 前端（Angular）

> **詳見** [docs/frontend-design.md §19 一致性 Checklist](docs/frontend-design.md#19-一致性-checklistcode-review-用)（涵蓋 Standalone Component / Signal / HTTP service 封裝 / inject() / 控制流 / Tailwind / toastr / icon / Lazy Loading 等項目）

#### 命名與結構
- [ ] C# 類別 / 方法 / 屬性 PascalCase；TypeScript 變數 / 函式 camelCase；DB 欄位 PascalCase；CSS class kebab-case
- [ ] Angular 檔名 kebab-case（`payment-form.ts`），class 名 PascalCase（`PaymentFormComponent`）
- [ ] Feature 目錄一律 `models/` `pages/` `services/` 三層

### 違反一致性的處理

- **小幅偏離**（命名 / 檔案位置）：發現後立即修正，補齊到既有風格。
- **架構性偏離**（引入新模式 / 新框架 / 新狀態管理方式）：**禁止單獨變更**，須先在 CLAUDE.md 提案討論並更新規範後才能套用，並一次性重構所有同類檔案。
- **Code Review 重點**：審查時優先確認「與既有檔案是否一致」，再看正確性與效能。

> **判斷原則**：當你不確定該怎麼寫，就找 3 份相似的既有檔案，**取多數派寫法**。寧可保持「不完美但統一」，也不要「個別完美但分散」。

---

## 程式碼規範

> **後端**：[docs/backend-design.md](docs/backend-design.md)（§4 Handler / §5 DTO / §6 Dapper vs EF Core / §10 ApiResponse / §15 命名 / §17 Checklist / §18 一致性原則）
> **前端**：[docs/frontend-design.md](docs/frontend-design.md)（§17 命名 / §19 Checklist）

---

## 分期撥款（單一真相 = installments）

2026-05 上線「分期撥款」，4 種申請類型（PaymentRequest / AdvanceRequest / TravelRequest / TravelPaymentRequest）的撥款資料**統一由子表 `XxxInstallment[]`** 表達：

- **撥款狀態**：由 [InstallmentReadService.ComputeStatus](Api/Services/Dapper/InstallmentReadService.cs) 計算三態（`Unpaid` / `PartiallyPaid` / `FullyPaid`），全部從子表推算
- **List filter「已撥款 / 未撥款」**：[PaymentRequestReadService](Api/Services/Dapper/PaymentRequestReadService.cs) 的 `PaymentStatusClause` 用 `EXISTS / NOT EXISTS` 子查詢 `XxxInstallments`
- **PDF 出納簽名章**：4 個 PDF service 取 `installments[]` 最後一期已撥款者的 `PaidBySignatureUrl` + `PaidAt`
- **撥款明細寫入兩個入口（共用 [InstallmentUpsertService.Apply](Api/Services/InstallmentUpsertService.cs)）**：
  - 財務**核准當下**：`PATCH /approval-tasks/{appType}/{id}/review` 帶 `installments`，與審核同交易原子寫入；財務（FIN）步驟核准撥款類時**必填**（holiday_travel 除外、批次核准除外）
  - 核准**後**修改 / 填實際撥款日：`PATCH /{type}-requests/{id}/installments`（**僅 approved**），舊 `PATCH /{type}-requests/{id}/payment-date` 已移除
- **撥款提醒**：[PaymentReminderService](Api/Services/PaymentReminderService.cs) UNION 4 種 installments 推算
- **唯讀顯示**：[`<app-installments-table>`](Admin/src/app/shared/components/installments-table.ts) 共用元件（card 結構，跟其他 detail 卡片一致），4 種申請的 detail / form 頁皆引用
- **編輯 UI 限制**（[approval-task-review](Admin/src/app/features/admin/approval-tasks/pages/approval-task-review/)）：
  - 「+ 新增一期」：`SUM ≥ 總額` 或 `FullyPaid` 時禁用
  - 「儲存撥款明細」：`SUM ≠ 總額` 或 `FullyPaid` 時禁用
  - 金額 input：`min=1`，`max=剩餘額度`（總額 − 其他列已填）
  - 已撥款列：4 欄位（預計撥款日 / 實際撥款日 / 金額 / 備註）全 readonly + 灰底；刪除按鈕隱藏
  - 後端 `InstallmentValidator.Validate` 提供等同驗證（序號連續 / SUM == 總額 / 已撥款列保護）

歷史：原採兩階段過渡策略，Phase 1 父表保留 `EstimatedPaymentDate` / `PaidAt` / `PaidByUserId` 作 cache；2026-05 Phase 2 完成，DROP 4 張父表的 3 個 cache 欄位 + FK + Index，由 [BackfillInstallmentsFromParentCache](Api/Data/Migrations/) 與 [RemovePaymentDateCacheFromParents](Api/Data/Migrations/) 兩個 migration 串接執行。
