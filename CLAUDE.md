# 請款簽核及工時管理系統 - CLAUDE.md

## 專案概述

本系統為企業內部的**請款簽核系統**與**請假/出差/加班申請管理系統**，提供費用申請流程簽核、員工資料管理、角色與權限控管、審核任務追蹤、**出勤打卡**（含 GPS 定位）等功能。

---

## 專案結構

```
/
├── Admin/          # 前端 Angular 21 應用程式
├── Api/            # 後端 Azure Functions .NET 10 API
├── docs/           # 設計與規範文件
│   ├── frontend-design.md   # 前端設計規範（CIS 色彩、卡片、Tab、明細列表、按鈕、icon、表單、檔案上傳…）
│   ├── backend-design.md    # 後端設計規範（Handler、DTO、Dapper、EF Core、Router、JWT、時區、檔案上傳…）
│   ├── api-routes.md        # API 路由清單
│   ├── database-schema.md   # 34 個 entity 清單
│   ├── authentication.md    # JWT 規格 / 登入流程 / Superadmin
│   └── business/            # 業務功能（14 個檔，每業務一檔）
│       ├── application-forms.md      # 9 種申請表類型總覽
│       ├── leave-rules.md            # 請假規則
│       ├── approval-flow.md          # 請款簽核流程
│       ├── approval-escalation.md    # 簽核升級機制
│       ├── pdf-signatures.md         # PDF 簽名欄
│       ├── department-visibility.md  # 部門可見性
│       ├── line-integration.md       # LINE 整合
│       ├── attendance-clock-rules.md # 出勤打卡規則
│       ├── attendance-reminder.md    # 打卡提醒
│       ├── payroll-formula.md        # 薪資公式
│       ├── hr-profile.md             # 員工人事資料卡
│       ├── notifications.md          # 通知系統清單（Email + LINE）
│       ├── flexible-work-hours.md   # 四週彈性工時（技術規格，**尚未實作**）
│       └── flexible-work-hours-client.md # 四週彈性工時（**客戶確認版**，無技術內容、逐項寫明做法）
│   └── tools/
│       └── md2pdf.mjs       # 客戶版 markdown → PDF（`npx marked` ＋ Chrome headless，CSS 內嵌；**產出一律放 `output/`**（已 gitignore）；技術版不對外、勿用此腳本轉）
└── Jabez.sln       # Visual Studio 方案檔
```

---

## 優先執行事項

每次收到任務時：

- **對話回應**：無論任務類型，一律使用**繁體中文**回應使用者
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
- [leave-rules.md](docs/business/leave-rules.md) — 請假規則（19 種假別 / 時間單位 / 年假 / 喪假 / 補休 / 生理假 / **育嬰留職停薪** / 重疊驗證 / **銷假**）
- [approval-flow.md](docs/business/approval-flow.md) — 請款簽核流程（簽核步驟 / 批次核准 / 自審 / 上層級 / 指定審核 / 跨步驟去重 / **追加預支重跑簽核** / **銷假重跑請假簽核**）
- [approval-escalation.md](docs/business/approval-escalation.md) — 簽核升級機制（找上層部門主管 + 代理人）
- [pdf-signatures.md](docs/business/pdf-signatures.md) — 7 個 PDF 動態簽名欄規則
- [department-visibility.md](docs/business/department-visibility.md) — 部門可見性 ProjectAccessScope
- [line-integration.md](docs/business/line-integration.md) — LINE OAuth 綁定 + 簽核 / 撥款通知推播
- [attendance-clock-rules.md](docs/business/attendance-clock-rules.md) — 出勤打卡規則（四動作前置條件 + 請假時段阻擋 + 休假日加班免下班卡 + **打卡權限三碼**）
- [attendance-reminder.md](docs/business/attendance-reminder.md) — TimerTrigger 打卡提醒 + 推播紀錄持久化
- [payroll-formula.md](docs/business/payroll-formula.md) — 薪資 7 條公式 + 健保眷屬計算
- [hr-profile.md](docs/business/hr-profile.md) — 員工人事資料卡（3 Tab + 9 子表 + 整批替換）
- [notifications.md](docs/business/notifications.md) — 通知系統清單（9 種 Email + 9 種 LINE Flex Message + 系統開關 + 打卡提醒）
- [flexible-work-hours.md](docs/business/flexible-work-hours.md) — **四週彈性工時（技術規格，尚未實作）**：勞基法 §30-1 框架 / 09:00–18:00＋午休 12:30–13:30 / 個人排班排例休（**例假未排滿或觸及連續 12 天・14 天 2 例假即擋存**，休假未滿只警示）/ 出勤排休總覽表（三色）/ 打卡與加班連動 / **〈改班申請〉逐部門五條簽核路線**（2026-09-17 改，取代原單一三關；「含底下部門」走既有 `ApprovalItem` 祖先部門繼承，不需新功能）/ **國定假日出勤加倍工資**（前 8 小時加發 1 日日薪 ＋ 第 9 小時起走**平日級距**，故 `OvertimePayCalculator` 不需新增級距表、只需改日別解析；**2026-09-17 改：主管的活動日旗標可壓在國定假日上**，被指定者當天解鎖上下班打卡、不需加班單，第 9 小時起才提加班申請 —— 連帶使加倍工資的取數變成「打卡紀錄 ∪ 已核准加班單」且**須依日期去重**，同仁側月曆仍唯讀、仍不佔配額）/ 補休 FIFO（**1–6 月用至 7 月底→8 月薪資結算；7–12 月用至隔年 1 月底→隔年 2 月結算**）/ 假日執行活動退場 / **與現行系統差異對照 + §10 決議（含 2026-09-16 客戶回覆 21 項、2026-09-17 回覆 2 項）**
- [flexible-work-hours-client.md](docs/business/flexible-work-hours-client.md) — 四週彈性工時**客戶確認版**（v1.4）：無技術內容與法條罰鍰，A–F 六區塊**逐項寫明實際做法**（v1.3 移除「確認」勾選欄與「修正意見」欄，改為把畫面行為／時點／擋存條件寫清楚；**v1.4 依客戶 2026-09-17 回覆 2 項**：改班申請逐部門流程、國定假日活動出勤；文末原有的「兩點請一併確認」區塊已於 2026-09-18 依客戶要求整段移除，客戶版全文已無待確認事項）。⚠️ **與上一份需人工同步**，改技術規格時要回頭確認客戶版是否受影響

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
│   ├── layout/
│   │   ├── services/
│   │   └── models/
│   └── utils/
│       ├── avatar-style.ts
│       └── safe-storage.ts               # **瀏覽器儲存的安全存取（全站唯一入口）**：`safeLocal` / `safeSession`。
│                                         #   `localStorage` / `sessionStorage` 在 iOS Safari「阻擋所有 Cookie」時**連讀取 property 都丟 SecurityError**
│                                         #   （舊版 iOS 無痕視窗則是配額 0、`setItem` 丟 QuotaExceededError），而不是回傳 null。
│                                         #   2026-09 事故：`AuthService._token` 是裸的 field initializer，而首頁決策點 / authGuard / noAuthGuard
│                                         #   三個入口都會 `inject(AuthService)`，於是第一次導航必踩 → 服務建構失敗 → router 爆掉 → **整頁純白**
│                                         #   （連登入頁都白，`login.ts` 的「記住我」同樣是裸 field initializer），使用者完全無從自救。
│                                         #   讀失敗回 null、寫失敗改寫記憶體 fallback（儲存被封鎖者仍能在單次瀏覽期間正常登入操作）。
│                                         #   **禁止直接呼叫 `localStorage` / `sessionStorage`**，見 [docs/frontend-design.md §15.5](docs/frontend-design.md)
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
    ├── dashboard/              # 打卡系統（即時時鐘、上下班/加班打卡、GPS；**路由需 `attendances:read`**，選單同步；未持有者由根路由 `resolveLandingUrl()` 導向個人資訊；**打卡按鈕上方有「出差（在外辦公）」勾選框**（2026-08 新增）：整天一個旗標 `AttendanceRecord.IsBusinessTrip`，四個打卡動作皆帶出並覆寫，初始值由 `/attendances/today` 帶回故不會被第二次打卡誤清，出缺勤清單以「出差」badge 呈現）
    │   ├── models/attendance.model.ts
    │   ├── services/attendance.service.ts
    │   └── pages/dashboard/
    ├── auth/
    │   └── pages/ (login, register, forgot-password, lock-screen, two-factor)
    ├── account/             # 員工自助（change-password / line-bind-callback / my-profile）
    │   ├── services/my-profile.service.ts   # 呼叫 /me/user + /me/profile + /me/files + /me/payroll（自助唯讀）
    │   └── pages/my-profile/                # 「個人資訊」唯讀頁：avatar 下拉進入，**4 Tab** 全唯讀 —— 員工基本資料 / 人事資料卡 / 健保眷屬（前 3 個比照管理頁，含薪資）＋ **過往薪資**（2026-08 新增，走 `GET /me/payroll?months=12` 列出近 12 個月，一列一月，點「明細」展開共用元件 `<app-payroll-detail-card>`；到職前月份不列、當月標「本月尚未結算」；**薪資即時重算、無月結快照**，調薪後回溯歷史月份會用現行底薪，頁面已加註說明）
    ├── admin/
    │   ├── activity-days/    # **活動日管理（2026-09 新增）**：協理排定活動日 + 勾選預定人力，`activity-days:write` 才顯示選單。
    │   │                       ⚠ 部門下拉的 `ngModelChange` 會清空已選人力（候選名單整組換掉），
    │   │                       但**必須擋掉「值沒真的變」的那次觸發** —— 編輯表單載入時 select 初始化也會 emit，
    │   │                       無條件清空會把回填的預定人力洗掉、且畫面上完全看不出來
    │   ├── shift-schedules/  # **個人排班排例／休（四週彈性工時功能 A，2026-09 新增）**：月曆大方格頁，權限 `shift-schedule:read/write`。
    │   │                       專案**第一個 FullCalendar 使用點**（`dayGridMonth`，套件早已安裝但從未用過）——
    │   │                       只借月格骨架，每格狀態存在元件 signal、以 `dayCellClassNames` / `dayCellContent` 上色與標字，
    │   │                       **不使用 event 模型**。四個踩過的坑（重繪須走 `getApi().render()`、首次渲染吃 `initialDate`、
    │   │                       `.fc-daygrid-day-top` 不可 `display:none`、手機要橫向捲動）見 [docs/frontend-design.md §12.8](docs/frontend-design.md)。
    │   │                       三條擋存規則**不在前端重算**（後端 `ShiftScheduleValidator` 為單一真相），畫面只顯示回傳的 blocks / warnings
    │   ├── users/          # 使用者管理（清單頁含**部門下拉 + 在職狀態下拉 + 勞退下拉 + 員工姓名搜尋列 + 分頁**，走 `GET /users?page=&pageSize=&search=&departmentId=&status=&hasLaborPension=`（每頁 20 筆，比照 vendors），後端以姓名模糊比對、部門 / 狀態為等值比對，四者可併用，單一擴充點為 `UserReadService.BuildFilter`；**在職狀態**（`status=active|inactive`，預設全部，白名單正規化）；**勞退**（`hasLaborPension=true|false`，有無自提二選一，SQL 以 `ISNULL(LaborPensionSelfContributionRate,0)` 正規化故 null 與 0 同屬「未自提」）**為 `payroll:read` 欄位級權限**：無權者前端不顯示該下拉、後端收到參數回 403（否則可用篩選結果反推他人自提率，繞過 `PayrollFieldAccess.Mask`）；user-form 含 3 Tab：員工基本資料 / 人事資料卡 / 健保眷屬；Tab1「員工資訊」含 **「排班制（六日與國定假日視為工作日）」** 勾選框（`isShiftWorker`，賣店 / 營業所用，清單頁姓名旁掛「排班制」badge）；含 employee-profile.service / hr-profile-pdf.service / 9 組 FormArray；**薪資為欄位級權限 `payroll:read`**：進得了員工管理（`users:read`）不等於看得到薪資，Tab1 的 8 個薪資／勞健保欄（2026-08 移除職務加給 / 主管加給 / 外派加給，加給剩其他加給 + 代扣代付款（2026-08 由「調整差額」更名，識別字仍為 `AdjustmentDifference`））、Tab2 薪資調整歷史、Tab3 健保費試算、列印 PDF 第 3 頁皆需另持 `payroll:read`，前端共用 `canSeeSalary` + `SALARY_CONTROLS`（`@if` 隱藏區塊 + `disable()` 控制項 + 送出前剔除 payload key），後端共用 [Api/Common/PayrollFieldAccess.cs](Api/Common/PayrollFieldAccess.cs) 抹除回應並拒絕寫入；薪資調整歷史改為**條件式**整批替換（`null`＝不變更）避免無權者送空陣列刪光歷史；`/me/user`、`/me/profile` 刻意全開，員工看自己的薪資不受影響）
    │   ├── roles/          # 角色管理（僅 Superadmin）
    │   ├── permissions/    # 權限管理（僅 Superadmin）
    │   ├── departments/    # 部門管理
    │   ├── job-titles/     # 職稱管理
    │   ├── vendors/        # 廠商管理（清單頁含**關鍵字搜尋列 + 分頁**，走 `GET /vendors?page=&pageSize=&search=`（每頁 20 筆，比照 projects），後端模糊比對 廠商名稱 / 統編 / 身分證字號 / 聯絡人 / 電話 / **匯款戶名**；含 vendor-quick-add-modal；統編/身分證字號類型切換，統編 blur 自動帶出 GCIS 公司資料；個人工作室上傳身分證正反面；存摺封面必填；**匯款資料為四個獨立欄位**：匯款戶名 `bankAccountName` / 匯款銀行 `bankName` / 銀行代號 `bankCode` / 銀行帳號 `bankAccount`，戶名常與廠商名稱不同（例：橘之鄉 → 旭工實業有限公司）故可用戶名反查，vendor-form 與 quick-add-modal 皆為四格）
    │   ├── approvals/      # 簽核流程設定（ApprovalItem + Steps；ApprovalItem 含 DepartmentId 部門維度，可為同一申請類型設「各部門專屬流程 + 通用預設」，送單時依申請人部門挑流程；子部門未設專屬流程時自動沿用最近祖先部門流程；Step 含 MinDays 天數門檻，null＝一律納入、N＝申請天數 ≥ N 才納入，目前供請假依天數分流；**非指定審核步驟可勾「例外指定審核」並逐一挑使用者（FormArray + select 列，整批替換 exceptionUserIds）**，名單內的申請人送單時該步驟改由申請人自行指定審核者，timeline 顯示「例外指定 N 人」badge；**例外名單下方另可設「限定職稱」（可多選，整批替換 designatedJobTitleIds）**，申請人的指定審核者只能挑這些職稱的人，timeline 顯示「限定職稱：…」badge）
    │   ├── approval-tasks/ # 待審核任務清單（**列印按鈕 2026-09 移到簽核詳情頁頁首、不再綁 `task.status`**：原本 8 種紙本單的列印全擠在
    │   │                      「已核准」分支裡，審核者在待審 / 退回修改中 / 已拒絕三個階段都看不到，而「主管簽完就印紙本寄回會計室」正是待審階段要做的事；
    │   │                      同時補上原本缺的**出差預支 / 假日執行活動**兩種列印（共用 TravelRequest 但 PDF 版面不同，分走 TravelPdfService / HolidayTravelPdfService），
    │   │                      規則見 [docs/business/pdf-signatures.md](docs/business/pdf-signatures.md)；**5 個頁籤：待審核 / 已核准 / 退回修改中 / 已拒絕 / 總監室簽核**；篩選列各頁籤常駐：全部類型下拉（所有人）＋**申請人下拉（僅財務體系部門 / Superadmin，選項來自 /approval-tasks/applicants ＝ **在職員工 ∪ 曾送出非草稿申請者**，只取後者會讓還沒送過單的在職員工整個從下拉消失）**＋**申請日期區間（起 ~ 迄兩個 `<input type="date">` + 清除鈕，2026-09 新增）**：所有人可用、各頁籤常駐、改值即時重查並回第 1 頁，走 `GET /approval-tasks?dateFrom=&dateTo=`，基準＝**送簽日 `SubmittedAt`**（後端 `COALESCE(SubmittedAt, CreatedAt)`，與清單「申請日期」欄同值），迄日含當日、可單邊帶、起迄顛倒自然查無資料，各申請類型的 WHERE 共用 `DateRangeClause(alias)`；**「已核准」頁籤預設只列「我親自審過」的單**，另對請款申請開一條後路讓**財務體系**（`DepartmentCodes.FinancialAndAbove`）看得到全部已核准請款（撥款明細是核准後才填的）——2026-09 修正：該條原本寫死 SQL 字面量 `N'FIN'`，改制後無此 Code 而**對所有人靜默失效**，現改參數化 `d.Code IN @FinancialDeptCodes`，與 `UpdateInstallmentsAsync` 的授權集合一致；撥款退款子篩選**僅「已核准」頁籤**顯示（僅財務體系；按鈕組 全部 / 尚未撥款 / 部分撥款 / 全部撥款 / **已結案**（`paymentStatus=closed`，只撈預支 / 出差預支 `IsClosed=1`，其餘類型後端 1=0 短路））；**「退回修改中」頁籤**（2026-08 新增）列 `ApprovalStatus='returned'` 的單，可見範圍＝我審過 ∪ 我是 designee / 升級審核者 ∪ 流程含我職稱關卡；**「總監室簽核」頁籤**（2026-08 由「總監待簽核」擴充）走 `scope=director` + 四態子按鈕（待簽核 / 已核准 / 退回修改中 / 已拒絕），待簽核維持「已輪到總監關卡」、其餘三態放寬為「流程含總監 `JobTitle.Level=1` 關卡」，可見權限仍為 `DepartmentCodes.DirectorPendingView`（財務管理部 + 會計室）+ Superadmin，會計室另收斂為「流程含會計關卡」；**摘要欄的預支申請加註送簽批次**：第1次顯示總額、第N次追加顯示「本次／總額」，批次標籤共用 advance-requests 的 roundLabel()；**狀態欄的預支 / 出差預支已結案時加註「已結案」badge**，與 advance-list / travel 詳情同一真相 `AdvanceRequest.IsClosed` / `TravelRequest.IsClosed`、同一樣式，holiday_travel 不走沖銷故排除；**簽核作業詳情頁（approval-task-review）另有「結案資訊」卡片**（`closureInfo(task)` → 共用元件 `<app-closure-info-card>`；`advance` / `travel` 顯示本單結案狀態 + 退款四欄，`write_off` / `travel_write_off` 顯示**關聯母單**（預支單 / 出差單）結案狀態，per-type 差異收斂在 `isRelatedClosure()` / `closureTitle()`），頁首同掛「已結案」badge；**假日執行活動的簽核詳情頁另有「參與執行人員」卡片**（2026-09 新增）：一人一列列出 人員（申請人若列於清單中掛 badge；**申請人不再自動占一列**，卡片改為一律顯示、空清單以 `@empty` 提示「本單不計入假日津貼」）/ 參與日期（`9/5、9/6 上午`，未逐日勾選顯示「全程參與」）/ 假日天數（半天 0.5），**只在表尾顯示津貼合計、不列個人津貼**（2026-09 收斂：個人津貼 ÷ 天數＝該員日薪，會被反推出月薪；`HolidayAllowanceDto` 已移除 `Allowance` 欄，合計改由 `TravelTaskDetailDto.HolidayAllowanceTotal` 後端算好帶回，清單頁摘要同步只顯示每人天數 + 全單合計），資料由 `TravelTaskDetailDto.HolidayAllowances[].Dates` 帶回（`PaymentRequestReadService` 的參與者 SQL LEFT JOIN `TravelRequestParticipantDates` 後以 ParticipantId 分組），日期字串共用 holiday-travel-request.model 的 `formatParticipantDates()`，與申請詳情頁同一真相；**清單狀態保存於網址（2026-09 新增）**：頁籤 / 各篩選 / 頁碼以 query params 為單一真相（`tab` / `ds` / `pay` / `type` / `by` / `from` / `to` / `page`，只帶非預設值），清單以 `effect` + `replaceUrl` 同步進網址、以 field initializer 讀 snapshot 還原（白名單正規化，且 `tab=director` / `by` 連權限一起判，無權者退回預設），詳情頁三個「返回列表」與批次核准 banner 的「前往補填」原封不動帶回，**唯獨審核送出後導頁剔除 `page`**（該筆已離開原頁籤，原頁碼可能超出範圍變空清單）；pattern 見 [docs/frontend-design.md §13 清單狀態保存於網址](docs/frontend-design.md)）
    │   ├── projects/       # 專案管理
    │   ├── payment-requests/  # 請款申請（請款類型三選一：廠商請款 type=vendor / 一般請款 type=general / **其他 type=other**（2026-09 新增，比照一般請款不需選廠商）；明細下方皆含整單批次附件上傳，共用 shared/components/attachments-upload；**請款原因必填**）
    │   ├── pre-review-requests/ # 預審申請（事前預審，clone 自請款；無撥款、不計入報表；品項類別下拉 + 報價單 OCR；含 pre-review-pdf.service 列印合併所有上傳檔；**預審說明必填**）
    │   ├── leave-requests/    # 請假申請（**單號 `LV-yyyyMMdd-NNN`（銷假 `LVR-`），2026-09 新增**：送簽時取號、草稿為 null，清單頁首欄與表單標題旁顯示，規則見 [docs/business/application-forms.md](docs/business/application-forms.md)；除歲時祭儀假與育嬰留職停薪外的 17 種假別選起迄日後皆扣國定假日與六日並列請假日清單，走輕量端點 /leave-requests/working-days；小時單位（事假/**家庭照顧假**/病假/產檢假/陪產假）跨日逐日累加只算工作日；**家庭照顧假**（`family_care`，性平法 §20）全年 7 日／56 小時上限、比照事假全額扣薪但薪資單獨立一列，家庭成員範圍僅表單提示不入庫；產假區間仍 56 個日曆天但只計其中工作日；**半天型假別（年假 / 補休 / 高階主管假）的上午時段起點依假別**：**補休為 09:00–13:00**、其餘 08:00–12:00，單一真相為前端 `halfDayAmStartHour()` / `halfDayAmEndHour()`（leave-request.model.ts），只改存進 `StartDate` / `EndDate` 的代表時刻與顯示、**不改時數**（半天恆 4 小時），後端一律以「起 < 13:00 ＝上午、訖 > 13:00 ＝下午」分類時段（`LeaveDayExpander` 逐日展開仍取 08:00–12:00，故出缺勤 / 應出勤 / 自動補卡不受影響）；含職務代理人下拉；依天數決定簽核關卡 <3 天單位主管 / ≥3 天 +部門最高主管+總監，靠 ApprovalStep.MinDays；**已核准的假可提「銷假」**：列表／唯讀檢視頁的「銷假」按鈕進 leave-revocation-form（`:id/revoke` / `leave-revocations/:id[/edit]` 三模式共用），逐日 chip 勾選要取消的日期（只含今天以後、未被其他銷假單佔用者）+ 銷假原因 + 指定審核者，送出後重跑同一份請假簽核；核准後父單 Hours 遞減、全銷轉 `cancelled` badge「已銷假」，部分銷加註「部分銷假」badge；
    │   │                      **育嬰留職停薪（2026-08 新增，兩個代碼）**：`parental_leave`（長期留停，**連續日曆天**、每名子女合計 730 天）+ `parental_leave_daily`（彈性單日新制，強制 `EndDate = StartDate`、每人每年 30 日且併入該子女總額度）；
    │   │                      資格為「在職滿 6 個月 + 子女未滿 3 歲」（Superadmin 繞過），新增欄位 `ChildBirthDate`（額度分組鍵）/ `ContinueInsurance`（僅記錄續保意願）；
    │   │                      `parental_leave` **刻意不列入 WorkingDayLeaveTypes**（否則跨年送件會被「行事曆未匯入」擋死、逐日 chip 也會爆量），故長期留停**不開放銷假**；
    │   │                      薪資「整月留停排除名單 + 當月按在職天數 ÷ 30 折減底薪與加給」，勞健保與勞退自提用折減前的 `insuredBaseSalary` 不打折，實領為負時前後端皆顯示「應補繳保費」警示；
    │   │                      年資扣除留停天數（`Api/Common/SeniorityHelper.cs` 單一真相，特休額度隨之暫停累積），額度端點 `GET /leave-requests/parental-quota`）
    │   ├── travel-payment-requests/ # 出差請款申請（小額已代墊直接請款，無沖銷）
    │   ├── travel-requests/   # 出差預支申請（走沖銷流程；**預支款需求日 `advanceNeededDate`**（2026-09 新增，**必填**）：申請人希望款項撥入的日期，供財務排撥款參考，出現在申請表單 / 詳情頁 / 簽核作業詳情頁 / 列印 PDF，清單頁不列；**假日執行活動不使用此欄位**（走 multipart 分支、不解析該 key，值恆 null））
    │   ├── holiday-travel-requests/ # 假日執行活動申請（共用 TravelRequest entity，IsHolidayTravel=true，計入假日津貼；參與人員可逐日勾選個人參與日期，未勾選＝全程參與；**每個勾選日期可再指定「全天／上午／下午」**：chip 四態循環 未選 → 全天 → 上午 → 下午 → 未選，半天以 0.5 天計入假日津貼，個人天數存 `TravelRequestParticipant.HolidayDays decimal(5,1)`；**申請人不會自動被算成參與者**（2026-09 改）：要領假日津貼須自行加入參與人員清單，加入後比照一般參與者可逐日、可半天，同一人不可重複列入（DB 唯一索引 `(TravelRequestId, UserId)` + 後端 400 + 前端下拉排除已選過的人）。舊制申請人無條件領整單 `HolidayDays`，自己又勾進清單時會被 SUM 兩次而**領雙倍**，一併修掉；歷史單以 `Api/Data/Scripts/08` 回填）
    │   ├── overtime-requests/ # 加班申請（**單號 `OT-yyyyMMdd-NNN`，2026-09 新增**：送簽時取號、草稿為 null，清單頁首欄與表單標題旁顯示；走簽核流程；**關聯專案為必填明細（至少一列）**：FormArray 每列一個專案下拉（來源 /projects/active?all=true 全部未結案專案，支援跨部門；**下拉自動排除其他列已選過的專案**）+ 該案預估時數，欄位標題註記「同部門專案可複選；支援專案請獨立申請」（業務提示，非硬性過濾）；**預估總時數改為唯讀自動加總**（父表 `OvertimeRequest.EstimatedHours` 為 `OvertimeRequestProject` 子表的合計快取，後端 Create/Update 重算，補休時數 / 登入自動補打加班結束卡 / 通知摘要皆沿用此欄）；指定審核者卡片加註「跨部門支援時第一審核者填該專案協理、第二審核者選自部門協理」；**補償方式（補休 / 加班費）整單二擇一**（2026-08 新增，`OvertimeRequest.CompensationType`）：選「補休」時數計入補休池、選「加班費」則依勞基法**分段累進倍率**試算金額並隨**加班日次月**薪資發放，兩者互斥以免同一段工時雙重給付；選加班費時表單即時試算（走 `GET /overtime-requests/estimate`，顯示分段明細 / 平日或假日 badge / 超出上限不計酬警示 / 同日已有假日執行活動的重複給付警示），金額於送簽時算一次、終局核准時以核准當下底薪重算並落地為快照，退回 / 拒絕 / 改單則清空；**簽核詳情頁（approval-task-review）刻意不顯示金額**（2026-09 改，原為「讓審核者看得到總額」）：金額 ÷ 時數 = 時薪 × 加權倍率 → 可反推底薪，與加班報表 `reports-overtime:amount` 同一顧慮，改列**分段計酬級距** chips（`2.0 小時 × 1.34`、`1.0 小時 × 1.67`）＋「計酬 N 小時｜平日/假日加班｜超出上限不計酬」小字；級距由後端 `OvertimePayCalculator.SplitHourTiers(PayableHours, IsHolidayOvertime)` 導出並以 `OvertimeTaskDetailDto.HourTiers` 帶回，**`OvertimePayAmount` 整欄從該 DTO 移除**（前端隱藏不算擋住，payload 仍看得到）——這是「找得到資訊量為零的等價呈現時，直接換掉優於開權限碼」的案例，故不另立權限碼；申請人自己的表單試算 / 唯讀快照 / 加班申請清單頁照常顯示金額，**申請人本人從簽核詳情頁進入時同樣看不到金額，屬刻意取捨**；倍率與時薪的單一真相為 [Api/Common/OvertimePayCalculator.cs](Api/Common/OvertimePayCalculator.cs)，快照寫入的單一真相為 [Api/Services/OvertimeCompensationService.cs](Api/Services/OvertimeCompensationService.cs)）
    │   ├── advance-requests/  # 預支申請（已核准單可新增「追加預支」批次：/:id/supplements/new 與 /:id/supplements/:round/edit 共用 advance-form 的追加模式；詳情頁預支日期改為批次清單、費用明細加「批次」欄；共用 roundLabel() 為批次標籤單一真相；**明細金額三欄連動：總價 = 現金(預支) + 支票(月結)，任兩欄輸入自動算出第三欄，規則與預支沖銷相同**；**預支款需求日 `advanceNeededDate`**（2026-09 新增，**必填**，含追加批次）：申請人希望款項撥入的日期，供財務排撥款參考，**比照預支日期為逐批次欄位**（Round 1 存 `AdvanceRequest`、Round ≥2 存 `AdvanceRequestSupplement`，經 `BuildRounds` 合成 `AdvanceRoundDto`），出現在申請表單 / 詳情頁 / 簽核作業詳情頁 / 列印 PDF，清單頁不列）；**費用明細分類下拉 12 項**（2026-09 新增 食材進貨 / 備品耗材 / 商品進貨 / 臨時人力），值以中文字面存 DB（後端無白名單），預支與沖銷兩份 `ITEM_CATEGORIES` 常數必須同步；**列印 PDF 補上右上角單號**（2026-09，8 種紙本單裡唯一漏掉的一張，紙本寄回會計室後無法對回系統單號）
    │   ├── write-off-requests/ # 預支沖銷申請（獨立簽核流程；**清單依預支單 group，母層列操作欄「檢視」進入彙總頁 write-off-overview（`/by-advance/:advanceId`）：一頁看完預支單完整資訊 + 該單全部沖銷單完整資訊**；明細下方含整單批次附件上傳，共用 shared/components/attachments-upload；新增表單選定預支單後，於「預支單」卡片下方唯讀列出該單全批次預支費用明細（含追加，依批次分組），資料由 /write-off-requests/available-advances 一併帶回；**沖銷資訊卡改為 `<app-write-off-summary>` 列出預支各批次金額 + 各次沖銷金額 + 待沖銷餘額 / 應撥差額**；**詳情頁與簽核頁另有「預支單結案資訊」卡（共用 `<app-closure-info-card>`，`showRefund=false` + `alwaysShow=true`：只呈現關聯預支單的已結案／未結案與結案時間，撥款金額仍由該頁既有「撥款」語彙欄位負責）**；**超支差額走分期撥款**，明細另有「支票已支付」註記欄，該欄在簽核頁對所有審核者顯示，但**僅財務管理部（`DepartmentCodes.FinanceStep`，與撥款日／結案同範圍，不含總監室／會計室）/ Superadmin 可勾選**，其他人 checkbox disabled 反白；**明細金額三欄連動：總價 = 現金花費 + 支票金額，任兩欄輸入自動算出第三欄**；**2026-08 重複建單修正**：表單送出／儲存加 `saving` in-flight 鎖（按鈕 disabled + spinner，避免上傳期間連按建出多筆）、create 成功即記住 `editId` 讓送簽失敗的重送走 update 而非再建一張、表單內按 Enter 不再直接送出；**「已沖銷」一律只計已核准**（下拉與詳情頁同基準），草稿／簽核中金額改以 `pendingWriteOffTotal` 顯示「另有 N 元沖銷中」提示；發票號碼唯一性檢查排除已拒絕的沖銷單；Superadmin 可對他人預支單建沖銷（與下拉範圍一致）；`RequestNo` 補上唯一索引宣告（含 travel_write_off））；**費用明細分類下拉 12 項**（2026-09 新增 食材進貨 / 備品耗材 / 商品進貨 / 臨時人力），值以中文字面存 DB（後端無白名單），預支與沖銷兩份 `ITEM_CATEGORIES` 常數必須同步
    │   ├── travel-write-off-requests/ # 出差預支沖銷申請（獨立簽核流程；**詳情頁與簽核頁有「出差單結案資訊」卡（共用 `<app-closure-info-card>`，`showRefund=false` + `alwaysShow=true`：只呈現關聯出差單的已結案／未結案與結案時間，撥款金額仍由該頁既有「撥款」語彙欄位負責）**）
    │   ├── insurance-brackets/ # 勞健保級距維護
    │   ├── payroll/           # 人事薪資（月薪計算 + PDF 匯出 + **Excel 總表匯出**：查詢列「匯出總表」鈕，一位員工一列 × 33 欄（基本 4 / 應發 11 / 扣項 15 / 其他 3；2026-08 新增「加班費(加班申請)」欄）+ 合計列，資料直接取自已載入的 `payroll()` signal（`GET /payroll` 本身不分頁），無後端變動）
    │   ├── attendance-reminder-logs/ # 打卡提醒推播紀錄（僅 Superadmin）
    │   ├── payment-reminder-logs/ # 撥款提醒推播紀錄 + 手動觸發（僅 Superadmin）
    │   ├── reports/        # 報表（出缺勤 / 加班 / 款項統計 / 專案水位）；**加班紀錄的「補償方式」/「加班費」兩欄 2026-09 修正**：
    │   │                      `overtime-report.ts` 的 `fetchData()` 把 API 回應逐欄手動 map 成 `OvertimeReportRow`，
    │   │                      新增欄位時漏 map 會**靜默顯示錯值**而非型別錯誤 —— `compensationType` 為 `undefined` 時
    │   │                      badge 一律落到「補休」（選加班費的單看起來像選了補休），`overtimePayAmount` 為 `undefined` 時
    │   │                      `!== null` 成立、金額欄印出空字串而非「—」。Excel 匯出同樣是獨立的一份 map，兩處必須一起補（**新增欄位、以及對既有欄位加上權限管制，兩種情境都算**）；**加班報表的 Excel 匯出 2026-09 另修掉「只有前 100 筆」**：前端送 `pageSize: 9999` 但後端 `Math.Clamp(ps, 1, 100)` 會壓回 100，匯出永遠截斷且無提示，改為比照出缺勤報表送 `export=true`（上限 `OvertimeReportHandler.ExportMaxPageSize = 5000`），並在 `totalCount > items.length` 時以 toastr 明講「匯出不完整」；同時 `OvertimeRequestReadService.LoadProjectsAsync` 改**分批**查詢（`ProjectLoadChunkSize = 1000`）—— Dapper 的 `IN @Ids` 會展開成逐一參數，一次 5000 筆會撞上 SQL Server 2100 參數上限而整句拋例外。**款項統計不走這條路**：它的匯出要把主表 LEFT JOIN 子表、一張單展開成多列明細，與畫面粒度不同，故早已另開專用不分頁端點 `GET /reports/payment/export`（`PaymentReportReadService.GetExportRowsAsync`），兩種做法的選用準則見 [docs/backend-design.md §4.1](docs/backend-design.md)；**出缺勤紀錄列出「打卡紀錄 ∪ 當日請假日 ∪ 缺勤日」**：全天請假沒打卡的人也會出現一列（`id=null` 虛擬列，上下班留空 + 「請假」badge + 假別 + 當日時數、不可編輯），同日多張假單合併為一列；**2026-09 三項擴充**：① **請假欄改顯示逐日精確時段**（`事假 09:00–13:00 (4h)` / `年假(特休假) 上午` / `婚假 全天`，同日多張一張一行，共用 leave-request.model 的 `formatLeaveDaySegment()`，資料來自後端 `leaves[].daySegment / dayStart / dayEnd`）；② **新增缺勤列**（`rowKind=absent`，工作日既沒打卡也沒請假，紅字「缺勤」badge、不可編輯）—— **請假列與缺勤列同樣 `id=null`，一律以 `rowKind` 分辨**，track key 分成 `a{id}` / `l{...}` / `x{...}` 三組；③ **「未打卡」badge**（有應出勤時段卻無上班時間，例如只請半天卻整天沒打卡）與**上班欄的「系統補卡」badge**（`isClockInAuto`），應出勤時段（`expectedStart/expectedEnd`）掛在上下班兩格的 tooltip、不佔欄位（維持 9 欄與 `min-w-[1040px]`）；**另有兩個 badge（2026-08 新增）**：日期欄的「出差」（`isBusinessTrip`，打卡時勾選）與下班欄的「超過 9.5 小時」（下班−上班 > 9.5h 含午休，**純前端 derived**、門檻常數 `LONG_WORKDAY_HOURS` 為單一真相，不進 DB / DTO，不影響薪資與加班時數）；**編輯 Modal 加備註欄**（`AttendanceRecord.Remark`，500 字，只在編輯表單可見可填，清單不列）；假別中文 import 自 leave-request.model 的 19 種 LEAVE_TYPE_LABELS；分頁（每頁 20）改用全站標準 pattern（手機 `‹ N / M ›` + 桌機頁碼列，共用 `buildPageNumbers()`），Excel 匯出走 `?export=true`；月篩選不再提供「全部年份 / 全部月份」（合併需有界區間）；**手機版採橫向捲動而非逐欄隱藏（2026-09）**：9 欄全數保留（原本請假 / 當日時數 / 加班開始 / 加班結束 4 欄在手機被 `hidden md:table-cell` 藏掉、且 `.table-responsive` 因 `.table` 是 `width:100%` 無 `min-width` 而從未真正捲動），改為 `<table class="… table-sticky-first min-w-[1040px]">`＋新增的 `.table-sticky-first`（釘住員工姓名欄，見 [docs/frontend-design.md §3](docs/frontend-design.md)）；篩選列同步補 `w-full sm:w-auto`。款項統計 1 個 endpoint 支援 全部 + 6 個類別 dropdown（全部 / 請款 / 預支 / 預支沖銷 / 出差請款 / 出差預支 / 出差預支沖銷；「全部」為 6 種 UNION ALL），權限只看 `reports-payment:read`，不需各別 `xxx-requests:read`。**專案水位表的「總專案水位」欄（分母＝契約金額，含公司保留 40%）為欄位級權限 `reports-project-water-level:total`**：只有 `reports-project-water-level:read` 者頁面照進、業務執行水位照看，但總水位整欄消失（前端 `canSeeTotal` 同時控 `<th>` / `<td>` / 空列 colspan），後端 `ProjectWaterLevelHandler` 亦把 `TotalPercentage` / `PreImportUsedAmount` / `RemainingAmount` 抹為 null / 0。**加班紀錄報表的「加班費」欄為欄位級權限 `reports-overtime:amount`（2026-09 新增，只開給財務承辦與總監）**：只有 `reports-overtime:read` 者頁面照進、時數與「補償方式」badge 照看，但加班費整欄消失（前端 `canSeeAmount` 同時控 `<th>` / `<td>` / 空列 colspan **/ Excel 匯出的 wsData 條件式 spread**——本頁兩份欄位 map 各自獨立，只藏畫面不藏匯出等於沒擋），後端 `OvertimeReportHandler` 亦把 `OvertimePayAmount` 抹為 null（`PagedResult<T>` 是 record 且 `Items` 為 `IEnumerable`，須連外層一起 `with` 重建）；`CompensationType` 與時數刻意保留——反推金額需要時薪快照 → 底薪，而底薪已受 `payroll:read` 管制。**權限碼上線時刻意不回填既有角色**（與水位表先例相反，預設全關），須由 Superadmin 到角色管理手動指派並請該員重新登入
    │   └── settings/       # 系統設定（含 PaymentReminderDaysBefore 撥款提醒天數）
    └── error/
        └── pages/ (error-403, error-404, error-500)
```

### 開發規範

> **詳見** [docs/frontend-design.md](docs/frontend-design.md) §13 路由 / §14 HTTP service / §15 Signal / §17 命名

- 所有 API 路徑統一在 `Admin/src/environments/environment.ts` 的 `apiUrl` 管理
- Token 儲存於 `localStorage`（**一律經 `core/utils/safe-storage.ts` 的 `safeLocal`**，禁止直接呼叫原生 API，見 [docs/frontend-design.md §15.5](docs/frontend-design.md)），由 `core/auth/interceptors/auth.interceptor.ts` 自動附加 Bearer Token
- `Admin/src/index.html` 底部有**啟動失敗保底畫面**（不依賴框架的 ES5 inline script）：Angular 沒 render 出東西時顯示可讀中文說明 + 重新載入鈕，取代原本的一片純白；chunk 版本錯開時自動重載一次。**判定用 `offsetHeight` 而非 `firstElementChild`**（router 導航失敗時 `<router-outlet>` 照樣在），且因 Angular 會吞掉 router 錯誤、`window.onerror` 靠不住，**逾時檢查才是主要路徑**。**另有開場能力探測 + 技術資訊區塊**（2026-09）：JS（class static block）／CSS（`oklch`）任一不支援就直接顯示「請更新至 iOS 16.4 以上」而非「請重新載入」，並印出 UA / 探測結果 / 第一個例外訊息供截圖回報。見 [docs/frontend-design.md §15.6](docs/frontend-design.md)
- **PDF 中文字型為 subset**（`Admin/src/assets/fonts/NotoSansTC-*.subset.ttf`，由 `Admin/scripts/generate-charset.py` + `npm run subset-fonts` 產生）：字集**必須含 Big5 第一 + 第二字面**（約 13,000 字）。2026-09 修正——原本只收第一字面，而**姓名罕用字多在第二字面**（例「闓」U+95D3），缺字時 jsPDF **不報錯、不印方框，是整個字消失**（「劉闓毅」印成「劉毅」），畫面與程式皆看不出異常。改字集後須重跑子集化並把 `.subset.ttf` 一起進版控，見 [docs/frontend-design.md §8.8](docs/frontend-design.md)。**字型 URL 另帶版本戳 `?v=FONT_SUBSET_VERSION`（2026-09-18 新增）**：字型是 Angular `assets`（檔名不帶 content hash）且 `loadFonts()` 單例快取，換字集不換 URL 時瀏覽器會沿用舊字型 —— 上次修正上線後仍有人印出缺「瑋」的單、同一人的另一張單卻正常，差別只在那次載到新字型或快取的舊字型。該常數＝兩個 `.subset.ttf` 的內容短雜湊，**由 `npm run subset-fonts` 自動改寫、勿手改**，跑完 `*.subset.ttf` / `tc-charset.txt` / `pdf-core.service.ts` 三者一起進版控，見 [docs/frontend-design.md §8.8.1](docs/frontend-design.md)
- **最低支援瀏覽器＝Safari / iOS 16.4**（`main.js` 的 `static{...}` 是 parse 期 SyntaxError、Tailwind v4 的 `@property` 亦同）：2026-09 決議**不降 build target**（實測降版可行且 bundle 幾乎不變，但選擇請使用者更新 iOS），故專案刻意不設 `.browserslistrc`。日後若降版，index.html 的能力探測條件須一起改，見 [docs/frontend-design.md §15.7](docs/frontend-design.md)

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

## 後端：Api（Azure Functions .NET 10）

> **設計規範與技術棧詳見** [docs/backend-design.md](docs/backend-design.md)（Handler / DTO / Dapper / EF Core / Router / JWT / 時區 / 檔案上傳 / 命名 / Code Review Checklist 一律統一定義於該文件）

### 目錄結構

```
Api/
├── Functions/
│   ├── RouterFunction.cs              # HttpTrigger，catch-all route {*route}
│   ├── AttendanceReminderFunction.cs  # TimerTrigger：限定 7-9 / 16-18 Taipei 時段每分鐘檢查，落在「上下班前 2 分鐘起算 **30 分鐘**時間窗」內則 LINE 推播（2026-09-09 由 10 分鐘放寬，見 attendance-reminder.md）；cron 由 `AttendanceReminderCron` app setting 控制。**`IsPastDue` 不跳過**（冷啟動延遲會整天不發），改由 Service 端 `batchStart` 冪等閘去重；**六日只推排班制員工**（`IsShiftWorker`，賣店照常營業），一個都沒有時維持整批跳過，平日仍不看行事曆（國定假日照推）
│   └── PaymentReminderFunction.cs     # TimerTrigger：每日 09:00 Taipei 跑撥款日將屆提醒；cron 由 `PaymentReminderCron` 控制；提前天數讀 `SystemSetting.PaymentReminderDaysBefore`，推給財務體系部門全員
├── Routing/
│   └── AppRouter.cs                   # C# 12 List Pattern 路由分派器
├── Handlers/                          # 25 個 Handler（業務邏輯）
│   ├── AuthHandler.cs                 # 登入、刷新 Token（登入時自動補卡，邏輯收斂在 `Services/AttendanceAutoClockService.cs`；**只填既有紀錄的空欄、絕不建立新列**：完全無打卡痕跡的日子交由報表的缺勤 / 未打卡列呈現，故不需回溯視窗常數。三種缺口：**漏打上班卡**（2026-09 新增，該日有下班卡或加班卡＝人確實來過，補到「當日應出勤起」，僅工作日且需持有 `attendances:write`）/ 下班卡＝**上班打卡時間 + 9 小時（含午休），不分上下午打卡** / 加班結束卡＝加班開始 + 申請單預估時數；**補卡時間一律避開已核准請假時段**（走 `ExpectedWorkWindow`）：上午請假者補上班 13:00、下午請假者補下班 12:00，**但無請假時絕不縮短**（以 `EndAdjustedByLeave` 為閘門，否則 09:00 上班者會從 18:00 被壓成 17:00）；分別標記 `IsClockInAuto` / `IsClockOutAuto` 供出缺勤清單顯示「系統補卡」badge。**Refresh Token 與補卡必須分成兩次 SaveChanges**：補卡是登入副作用，併發登入撞唯一索引時共用交易會讓整個登入回 500）
│   ├── UserHandler.cs                 # 使用者 CRUD（含原住民 / 低收入 / 身心障礙證明 + 健保 / 勞保覆寫）；GetMineAsync = GET /me/user 員工讀自己（免 users:read）
│   ├── EmployeeProfileHandler.cs     # 員工人事資料卡 GET / PUT（multipart：HR JSON + 身分證正反面 + 最高學歷證明 + **存摺封面 ×2**：銀行帳戶分第一 / 第二兩組，各含分行 + 帳號 + 存摺封面，兩張共用 ProcessPassbookAsync、blob 以 `_passbook` / `_passbook2` 區隔）；GetMineAsync = GET /me/profile 員工讀自己（免 users:read）
│   ├── RoleHandler.cs
│   ├── PermissionHandler.cs
│   ├── DepartmentHandler.cs
│   ├── JobTitleHandler.cs             # 職稱 CRUD（刪除時清洗 ApprovalStepDesignatedJobTitles 的 NO_ACTION 外鍵）
│   ├── VendorHandler.cs               # 廠商管理 CRUD（匯款資料四欄 BankAccountName 戶名 / BankName 銀行 / BankCode 代號 / BankAccount 帳號；清單支援 `?search=` 關鍵字模糊比對 名稱 / 統編 / 身分證字號 / 聯絡人 / 電話 / 匯款戶名，並支援 `?page=&pageSize=` 分頁：帶分頁參數回 PagedResult、不帶回平面陣列；multipart 支援存摺封面（必填）/ 身分證正反面上傳；統編與身分證字號擇一；lookup / lookup-by-tax-id / POST 開放任何登入者；刪除受 PaymentRequest 引用保護）
│   ├── ApprovalHandler.cs             # ApprovalItem + Steps CRUD（ApprovalItem 含 DepartmentId 部門維度；唯一性以 (ApplicationType, DepartmentId) 判定；/active 依呼叫者部門解析流程，優先序：自身部門 > 最近祖先部門（沿 ParentId 往上）> 通用預設）；Step 含 DesignatedRequiresDepartment（指定審核步驟可設「需先選部門再選人」，支援一條流程多個指定步驟；多指定步驟前端連動見 shared/components/designated-reviewers-picker：連動閘控 + 部門帶入 + 部門最高層級自動略過；**Step 另含例外指定審核名單 `exceptionUserIds`（ApprovalStepException 子表，整批替換）**：非指定審核步驟可挑指定使用者，名單內的申請人送單時該步驟改由申請人自行指定審核者，與 UseApplicantDesignated 互斥；**例外步驟另可設限定職稱 `designatedJobTitleIds`（ApprovalStepDesignatedJobTitle 子表，整批替換）**，限制申請人只能指定這些職稱的人，非例外步驟一律清空）
│   ├── ApprovalTaskHandler.cs         # 待審核任務查詢與審核動作（列表另支援 applicationType / submittedByUserId 篩選（與 status 正交，各頁籤共用）；**`status` 走 `ValidListStatuses` 白名單正規化**（pending / approved / returned / rejected，非法值→pending，避免像舊 `returned` 一樣靜默落到待審分支）＋**`scope=director` 範圍參數**（總監室簽核，與 status 四態組合，舊 `status=director_pending` 相容）；**申請人篩選與 GET /approval-tasks/applicants 限財務體系部門或 Superadmin**，判定共用 CanFilterByApplicant → DepartmentCodes.FinancialAndAbove；**單筆詳情 `GET /approval-tasks/{appType}/{id}` 的存取控制另放行「申請人本人」**（`IsApplicantAsync` 逐型別比對 SubmittedById / EmployeeId），否則申請人拿不到 flow / approvalRecords，請款列印按鈕不出現、兩張沖銷表印出無簽核欄的 PDF；**沖銷結案採「登記制」**：財務於其簽核關卡勾 `closeAdvance` 只設沖銷單的 `PendingClose`，待整張沖銷單轉 `approved` 才真正寫母單 `IsClosed`（財務多為倒數第二關，提前結案會讓總監退回後無法補開沖銷單）；退回／拒絕清除登記；步驟判定走 `IsFinanceStepAsync`（`DepartmentCodes.FinanceStep`，**禁止硬編碼 "FIN"**），勾了但非財務步驟改回 400 不再靜默；**單筆詳情另回 `stepReviewers`**（**逐關**列出實際可簽核的人，僅 pending 計算，判定順序＝升級指派 > 指定審核 designee（維持申請人排定次序）> 上層級 / 固定池，與 `AuthorizeStepAsync` 同一套；某關為空＝該關查無可簽核人員，前端時間軸把人名接在關卡名稱後、無人則印紅字），解析收斂在 `ApprovalFlowService.ResolveStepReviewersAsync`）
│   ├── ProjectHandler.cs
│   ├── PaymentRequestHandler.cs       # 請款申請 CRUD（單號 PR-yyyyMMdd-NNN，**送簽時取號**）
│   ├── PreReviewRequestHandler.cs     # 預審申請 CRUD + Submit（單號 PRV-yyyyMMdd-NNN，**送簽時取號**；報價單上傳 blob container=quotes；無 installments、不計入報表）
│   ├── QuoteOcrHandler.cs             # 報價單 OCR（POST /quote-ocr，回傳品項列表 itemName/amount/note）
│   ├── LeaveRequestHandler.cs
│   ├── LeaveRevocationHandler.cs      # 銷假申請 CRUD + Submit（GET /leave-requests/{id}/revocable-dates 逐日可銷清單；POST /leave-requests/{id}/revocations；/leave-revocations/*；ApprovalItem 以 "leave" 解析＝跑原本的請假簽核，簽核紀錄以 "leave_revocation" 隔離）
│   ├── TravelRequestHandler.cs        # 出差預支申請 CRUD（單號 TR-yyyyMMdd-NNN，假日執行活動為 HTR-yyyyMMdd-NNN，**皆送簽時取號**；預支後沖銷）
│   ├── TravelPaymentRequestHandler.cs # 出差請款申請 CRUD（單號 TPR-yyyyMMdd-NNN，**送簽時取號**；小額代墊直接請款）
│   ├── OvertimeRequestHandler.cs      # 加班申請 CRUD（含補償方式 compensatory / pay；`GET /overtime-requests/estimate?date=&hours=` 加班費即時試算，對象一律取 JWT sub、不接受 employeeId）
│   ├── AdvanceRequestHandler.cs       # 預支申請 CRUD（單號 ADV-yyyyMMdd-NNN，**送簽時取號**；追加批次沿用父單單號）＋**追加預支批次**（POST/PATCH/DELETE /advance-requests/{id}/supplements[/{roundNo}]；新增即送簽、無草稿階段；有進行中批次時禁止整單編輯/刪除）
│   ├── WriteOffRequestHandler.cs      # 預支沖銷申請 CRUD（獨立簽核流程）＋**依預支單彙總檢視**（GET /write-off-requests/by-advance/{advanceRequestId}，回傳預支單完整資訊 + 該單全部沖銷單）＋**差額撥款分期**（PATCH /write-off-requests/{id}/installments，SUM 對應 RefundDue 超支增額）＋**支票已支付註記**（PATCH /{id}/check-payments）
│   ├── TravelWriteOffRequestHandler.cs # 出差預支沖銷申請 CRUD（獨立簽核流程）
│   ├── AttendanceHandler.cs           # 打卡（上班/下班/加班開始/加班結束；請假時段內擋上下班打卡；**休假日（行事曆假日／六日）或當日全日請假時，加班開始免下班卡**（**排班制員工 `User.IsShiftWorker` 恆不適用休假日條件**，週六仍須先打下班卡），無紀錄則建立只含加班時間的紀錄；**2026-08 起納入權限管理**：打卡走 `attendances:read/write`（員工對自己）、出缺勤報表列表與 `PUT/PATCH /attendances/{id}` 走 `reports-attendance:read/write`（管理者對別人），後者另在 Handler 內套部門可見性 scope 控管「能改誰」）
│   ├── ShiftScheduleHandler.cs       # **個人排班排例／休（四週彈性工時功能 A，2026-09 新增）**：GET / PUT `/shift-schedules`（整月整批替換）。
│   │                                    擋存判準與開放期各自收斂成純函式單一真相（`Api/Common/ShiftScheduleValidator.cs` / `ShiftScheduleWindow.cs`），
│   │                                    讀寫共用同一份、前端只顯示不重算。**國定假日不入表**（唯讀、不佔配額）、**上班日不落地**（查無紀錄即上班日）
│   ├── ShiftScheduleReportHandler.cs # **出勤／排休總覽表（四週彈性工時功能 B，2026-09 新增）**：`GET /reports/shift-schedule`，
│   │                                    `reports-shift-schedule:read`。一人一列 × 當月每日一欄，不分頁。
│   │                                    日別組裝走共用的 `ShiftScheduleMap`；**活動日的第三色只套在被指派為預定人力的人身上**
│   │                                    （用 `isActivityDay` 會讓整欄的人全亮起來，看不出誰真的要出勤）；
│   │                                    `noCoverage` 警示只在**週一至週五且非國定假日**成立
│   ├── ActivityDayHandler.cs         # **活動日（四週彈性工時 §3.2，2026-09 新增）**：各部門協理排定活動日 + 預定人力，`activity-days:read/write`。
│   │                                    **疊加旗標非第 5 種日別**（可壓在國定假日上），Handler 完全不碰 `ShiftScheduleDay`；
│   │                                    改期**不自動改寫個人班表**，只回報需調整的同仁。⚠ 衝突判定不是「重跑三條檢核」——
│   │                                    三條檢核的輸入與活動日無關、永遠不會因改期而不合格，真正的衝突是
│   │                                    「活動日當天該員排定為例假／休假」（國定假日除外）
│   ├── InsuranceBracketHandler.cs    # 勞健保級距 CRUD
│   ├── PayrollHandler.cs             # 人事薪資查詢（月薪計算）；GetMineAsync = GET /me/payroll 員工讀自己近 N 個月薪資（免 payroll:read，逐月呼叫帶 employeeId 的同一支計算，依 HireDate 擋掉到職前月份，months clamp 1~24）
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
│   ├── Configurations/                # EF Core 實體對應設定（34 個，新增 EmployeeProfile + 9 張子表 + 健保眷屬 + PreReviewRequest / PreReviewItem / PreReviewRequestAttachment）
│   ├── Migrations/                    # EF Core Migration 檔案
│   ├── Scripts/                       # 一次性維運 SQL（非程式）：01 診斷「送單後會卡死」的簽核關卡（唯讀，遞迴 CTE 重現流程解析優先序）、
│   │                                  #   02 修正請假 Step2「申請人部門的協理」→ 上層級審核（@Commit 空跑開關 + MinDays 遺失防呆）、
│   │                                  #   03 停用測試帳號（Email @example.com / 姓名含「測試」→ Status='inactive'，避免混進簽核候選池與去重全池判定；
│   │                                  #      刻意跳過仍背著 pending 指定審核 / 升級指派 / 職務代理者，停用＝無法登入會讓那些單沒人審）。staging / 正式站各跑一次、
│   │                                  #   04 清除測試申請單與測試專案（@Commit 空跑開關；帳號一律不動）：以「測試帳號送出 ∪ 掛在測試專案上 ∪ 明列單號 ∪ 預審全表」
│   │                                  #      取聯集，再補上相依單據（沖銷←預支 / 出差、銷假←請假），最後刪 5 個測試專案。
│   │                                  #      兩個關鍵：① 三張多型無 FK 的表（ApprovalRecords / EscalationOverrides / RequestDesignatedReviewers）
│   │                                  #      必須趕在父列消失前用 (AppType, AppId) 清掉，否則殘列掛著 ReviewerId 擋住日後刪使用者；
│   │                                  #      ② OvertimeRequestProjects.ProjectId 是 NO_ACTION，殘一列就擋住刪專案，故引用測試專案的加班單須整張納入範圍。
│   │                                  #      附件 blob 不會被刪（成為孤兒 blob，本機不影響功能）、
│   │                                  #   04b 同 04 的逐筆展開版（一張單一組 4 行 DELETE：3 行簽核足跡 + 1 行父列），
│   │                                  #      供逐筆檢視 / 挑著跑；區塊順序同樣是先子單（沖銷 / 銷假）後母單，
│   │                                  #      要保留某張單就整組四行一起註解。兩版刪除結果已比對逐欄一致、
│   │                                  #   05 推進「卡在上層級關卡但查無可簽核人員」的請假單（@Commit 空跑開關）：
│   │                                  #      職級 / 部門異動後，原本有人可簽的上層級關卡會變成 0 位候選人，單子停在該關誰都撈不到。
│   │                                  #      以條件比對定位（不寫死 Id），推進到下一個有人可簽的**固定**關卡（跳過 MinDays 擋掉者 /
│   │                                  #      指定審核 / 上層級，同 BuildLaterFixedStepScopes 的判準）；找不到安全落點者不動、交人工。
│   │                                  #      不發通知，推進後須自行告知新的審核者、
│   │                                  #   06 刪除指定單號的預支申請單（@Commit 空跑開關 + @AllowPaid 已撥款保護）：
│   │                                  #      以 **RequestNo 定位**（不寫死 Id），故同一份可在本機 / staging / 正式站跑；
│   │                                  #      刪除規則同 04b（先子單沖銷的三張多型足跡與本體、再母單足跡與本體），
│   │                                  #      Items / Installments / Supplements 走 CASCADE。目標單有已撥款分期時預設整份中止、
│   │                                  #   07 移轉指定預支單（含其沖銷子單）的申請人：代錄帳號 → 實際員工（@Commit 空跑開關 + 7 道閘門）。
│   │                                  #      以 **RequestNo → Email 對照表**定位（不寫死 Id；Email 有 filtered unique index 且純 ASCII，
│   │                                  #      Linux 版 sqlcmd 無 -f codepage 可指定輸入編碼，中文字面量解碼出包也不影響寫入）。
│   │                                  #      只改 `SubmittedById` 一欄（AdvanceRequests / WriteOffRecords 兩表皆無 UpdatedAt），沖銷子單繼承母單申請人；
│   │                                  #      `ApprovalItemId` / `SubmittedAt` / 三張多型足跡表皆為送簽快照，刻意不重算（已核准單屬歷史）。
│   │                                  #      現任申請人已是目標人 → 跳過該列（故可安全重跑）；是第三者 → 整份中止。
│   │                                  #      ⚠ pending 單必須先驗「新申請人不是目前固定關卡的唯一候選人」：
│   │                                  #      `ApprovalFlowService.ResolveReviewerPoolAsync` 三個分支都排除申請人本人，
│   │                                  #      踩到就會製造出一張永遠沒人能簽的單（＝ 05 在救的狀態）、
│   │                                  #   08 回填假日執行活動的申請人為「參與執行人員」（@Commit 空跑開關，冪等可重跑）：
│   │                                  #      搭配「申請人不再自動被算成參與者」的程式變更。薪資即時重算無月結快照，
│   │                                  #      故把 pending / returned / approved 的假日活動單的申請人補一列
│   │                                  #      （HolidayDays=NULL＝全程參與，等值於舊制的整單 HolidayDays），讓既有金額不變；
│   │                                  #      已在清單中者跳過 —— 這批人舊制被 SUM 兩次（溢發雙倍），跑完會回正為單份、
│   │                                  #   09 補登一張「已核准」的育嬰留職停薪請假單含簽核足跡（@Commit 空跑開關，冪等可重跑）：
│   │                                  #      紙本早已核准、系統無單，而年資扣除 / 特休 / 出缺勤 / 730 天額度全以 LeaveRequests 為唯一來源。
│   │                                  #      **系統其實允許從前台補登過去日期**（前後端皆無「起始日 ≥ 今天」檢查，下界只有 RequestDateGuard 的
│   │                                  #      今日 −3 年，且請假重疊驗證不看打卡），此腳本只是免去三位主管為舊案重簽。
│   │                                  #      一次寫三張表：LeaveRequests（approved）+ ApprovalRecords（每個生效關卡一列）
│   │                                  #      + RequestDesignatedReviewers（指定審核關卡）；**不寫 EscalationOverrides**（核准當下本就會被刪）。
│   │                                  #      三個關鍵：① `EndDate` 必須是 **23:59**（EndOfDay），寫 00:00 會讓最後一天的打卡阻擋與重疊檢查失效；
│   │                                  #      ② `Hours` = **日曆天 × 8**（parental_leave 不在 WorkingDayLeaveTypes，六日與國定假日照算）；
│   │                                  #      ③ `ChildBirthDate` 是 730 天額度的分組鍵，不寫則 parental-quota 永遠算不到這張單。
│   │                                  #      不寫死 Id：員工 / 指定審核者以 Email 解析，流程依部門沿 ParentId 往上（同 ResolveApprovalItemIdAsync
│   │                                  #      的優先序），固定關卡審核者依 部門 + 職稱 解析且**排除 @example.com 測試帳號與 Superadmin**
│   │                                  #      （正式站仍有 active 的測試主管帳號，不排除會把簽名記到測試帳號上），每關須恰好 1 位否則整份中止、
│   │                                  #   10 診斷跨表重複的發票號碼（唯讀）：搭配發票唯一性收斂成 InvoiceUniquenessChecker 的程式變更。
│   │                                  #      出差請款從未做過檢查、跨表查詢又漏排除已拒絕單，故可能留有歷史重複；納入檢查後，
│   │                                  #      使用者編輯這些舊單（發票欄根本沒動）會被別張舊單擋住而存不回去，上線前先跑一次交人工清理。
│   │                                  #      ⚠ CJK 手打文字的排除**必須用 `Latin1_General_BIN2`** collation —— 中文 collation 下
│   │                                  #      字元範圍 `[一-鿿]` 不照 Unicode 碼位排序，「收據」會判成 0 而靜默失效、
│   │                                  #   11 變更指定預支單的所屬專案（@Commit 空跑開關，冪等可重跑）：已核准單前台無法編輯
│   │                                  #      （UpdateAsync 僅開放 draft / returned），掛錯專案只能以腳本更正。以 **RequestNo + Projects.Code**
│   │                                  #      定位（不寫死 Id），**只改 `AdvanceRequests.ProjectId` 一欄**（該表無 UpdatedAt）。
│   │                                  #      子表全部不必動：Items / Installments / Supplements 皆無專案欄，**WriteOffRecords 也沒有自己的
│   │                                  #      ProjectId**（全站一律透過母單回扣專案），故沖銷子單與已撥分期會自動跟著搬家 ——
│   │                                  #      這也代表**已撥金額的專案歸屬會一起改變**，空跑報表須先確認金額。
│   │                                  #      簽核流程依**申請人部門**解析、與專案無關，故 ApprovalItemId 等送簽快照刻意不重算；
│   │                                  #      款項統計的部門可見性看的也是申請人部門，換專案不影響誰看得到這張單。
│   │                                  #      閘門：單號查無 / 目標 Code 非唯一命中（Projects.Code 無唯一索引）/ 新專案已結案 /
│   │                                  #      現有專案非預期（代表交辦後又被人改過）
│   └── Seed/                          # 一次性匯入工具（共用 RocDateParser 解民國年）
│       ├── EmployeeImporter + EmployeeImportDtos + employee-import.json  # 員工人事資料（RUN_EMPLOYEE_IMPORT 旗標，IMPORT_UPLOAD_FILES 控制附件上傳）
│       ├── ProjectImporter + ProjectImportDtos + project-import.json     # 專案資料（RUN_PROJECT_IMPORT 旗標，PROJECT_IMPORT_DRY_RUN 只印不寫；來源 reference/專案資料-115.07.29.xls；以 Code upsert、期別明細全量重建）
│       └── VendorImporter + VendorImportDtos + vendor-import*.json       # 廠商 / 個人受款人資料（RUN_VENDOR_IMPORT 旗標，VENDOR_IMPORT_DRY_RUN 只印不寫，**VENDOR_IMPORT_FILE 指定資料檔**，預設 vendor-import.json）
│                                                                          #   · vendor-import.json（31 筆）：來源 reference/壯圍沙丘廠商匯款資料0812.xlsx，來源「匯款帳號」一格拆成四欄；缺統編／地址／存摺封面，Note 標記待補
│                                                                          #   · vendor-import-1150820.json（109 筆 = 廠商 79 + 個人 30）：來源 reference/廠商及個人資料建置表_1150820.xlsx，兩 sheet 欄位相同，唯一差別是識別碼欄（廠商 → TaxId / 個人 → IdNumber）；統編、身分證、地址齊全，Note 只存來源原文
│                                                                          #   **去重鍵：TaxId → IdNumber → Name**（TaxId / IdNumber 皆有 filtered unique index，只用 Name 會在廠商更名時撞索引；識別碼與名稱各自命中不同廠商則跳過交人工判讀）
│                                                                          #   直寫 entity 繞過 Handler 的識別碼必填 / 格式驗證 / 存摺封面必填，故匯入的廠商在後台編輯儲存時仍會被擋須先補件
├── Models/
│   ├── Entities/                      # 53 個資料庫實體（新增 **銷假申請 LeaveRevocation + 逐日明細 LeaveRevocationDate**（獨立子單，父單送簽期間不動；LeaveRequest 另加 OriginalHours 與 `cancelled` 終止狀態）/ **簽核步驟例外指定審核名單 ApprovalStepException** + **例外的限定職稱 ApprovalStepDesignatedJobTitle** / **預支沖銷差額分期 WriteOffInstallment**（第 5 種分期撥款子表）/ **WriteOffRecord + TravelWriteOffRecord 新增 `PendingClose`**（財務登記結案，待整張單核准才生效）/ **追加預支批次 AdvanceRequestSupplement**（只存 RoundNo≥2，Round 1 = 父單本身）/ **TravelRequestParticipantDate 參與人員個別參與日期** / EmployeeProfile / EducationRecord / EmploymentHistoryRecord / FamilyMember / ProfessionalTraining / LanguageAbility / JobTransferRecord / RewardPunishmentRecord / SalaryAdjustmentRecord / HealthInsuranceDependent / **5 個分期撥款表 PaymentRequestInstallment / AdvanceRequestInstallment / TravelRequestInstallment / TravelPaymentRequestInstallment / WriteOffInstallment** / **PaymentReminderLog** / **整單批次附件 PaymentRequestAttachment / WriteOffAttachment** / **預審申請 PreReviewRequest / PreReviewItem / PreReviewRequestAttachment**）
│   └── Dtos/                          # 21 個 DTO 檔案（新增 **LeaveRevocationDtos** / EmployeeProfileDtos / **InstallmentDtos** / **PreReviewRequestDtos**）
├── Services/
│   ├── WorkdayScheduleProvider.cs     # **工作時段的版本化取用管道（四週彈性工時，2026-09 新增）**：把 `SystemSetting.FlexibleWorkStartDate`
│   │                                    讀成 per-request 快取。`Constants.cs` 的 `WorkdayHours` 已由 5 個 `const int` 擴充為
│   │                                    `WorkdaySchedule` record ＋ `Legacy`（08:00–17:00／午休 12:00–13:00）／`Flexible`（09:00–18:00／午休 12:30–13:30）
│   │                                    ＋ `For(date, switchDate)`。**舊資料不遷移**故系統內同時存在兩套時段，判定基準一律是
│   │                                    **該筆資料自己的日期**（請假看 `StartDate`、打卡看 `RecordDate`、報表看該列日期），不是今天。
│   │                                    ⚠ **切換日必須訂在未來月初、不可回溯設定** —— 回溯會讓舊制建立的單被新制時段重新解讀，
│   │                                    全日假憑空多出 17:00–18:00 的假性應出勤（已於 staging 實測，見 flexible-work-hours.md §10.5）。
│   │                                    5 個 `const int` 原地保留（值等同 Legacy），故既有 18 處引用零改動
│   ├── IJwtService.cs
│   ├── JwtService.cs                  # HS256 JWT 產生與驗證
│   ├── AttendanceAutoClockService.cs  # 登入自動補卡共用（static，不呼叫 SaveChanges，比照 LeaveRevocationService）：三種缺口（漏打上班 / 下班 / 加班結束）一次撈回，時間走 ExpectedWorkWindow 避開請假時段；**只填空欄不建新列**
│   ├── IEscalationService.cs          # 簽核升級服務介面
│   ├── EscalationService.cs           # 簽核升級邏輯（上層部門主管遞迴 + 代理人）＋ **上層級關卡無人時往上層部門接手**（2026-09，`FindSuperiorInAncestorDepartmentsAsync`）：`UseDirectSupervisor` 步驟在同部門找不到更高階者時，沿部門 `ParentId` 往上找 `Level <` 申請人的最接近一位並以升級審核指派，找不到才退回原本的「跳過該關」；全部 9 種申請類型適用，修正「部門最高主管送單一路跳到底 → 無人審即自動核准」；**指派前先排除「流程後續固定關卡本來就會簽到的人」**（`laterStepScopes` / `StepReviewerScope`，範圍由 `ApprovalFlowService.BuildLaterFixedStepScopes` 算出，只認固定池關卡：MinDays 擋掉 / 指定審核 / 上層級 / 全不限者皆不算），否則「Step1 升級到總監 + 最後一關固定總監」會變同一人連簽兩關，並撞上總監跨步驟去重的「全池皆已審」限縮而卡死；同職級多人再依 `HireDate` → `Id` 排序確保決定性（送單與推進兩次解析拿到同一人）；「同部門有無上級」三處判定（`ApprovalFlowService.FindNthSuperiorLevelAsync` / `ApprovalTaskHandler.AuthorizeStepAsync` / 待審清單 SQL）一律加上 `Status='active'`，離職者不再撐住一個沒人能審的層級
│   ├── EscalationResult.cs            # 升級結果 record
│   ├── OvertimeCompensationService.cs # 加班補償方式共用（static，不呼叫 SaveChanges）：Compensatory / Pay 常數 + Normalize（未知→補休，安全側）
│   │                                    + ApplyAsync（算並寫入 4 個快照欄）/ ClearSnapshot（退回・拒絕・改單）/ HasHolidayTravelConflictAsync（假日津貼重複給付警示）
│   ├── LeaveRevocationService.cs      # 銷假共用：ApplyAsync（核准後從「該假單所有已核准銷假的 distinct 日期」整組重算父單 Hours、全銷轉 cancelled，冪等且併發安全）+ 下游「該日未銷假」共用排除片段
│   ├── ILineService.cs               # LINE API 操作介面
│   ├── LineService.cs                # LINE Platform REST API 封裝（token 換取 + 推播 + 月度 quota 查詢）
│   ├── PushResult.cs                 # LINE 推播結果 record（含 ErrorCategory 分類）
│   ├── LineFlexMessageBuilder.cs     # 6 種簽核通知 + 打卡提醒的 LINE Flex Message 模板
│   ├── IAttendanceReminderService.cs # 打卡提醒服務介面
│   ├── AttendanceReminderService.cs  # 打卡提醒協調：時間窗判斷時點（非精確等值）、`batchStart` 冪等閘（一天一槽一次）、過濾對象、推播 LINE
│   ├── IPaymentReminderService.cs    # 撥款提醒服務介面
│   ├── PaymentReminderService.cs     # 撥款日將屆提醒：撈 4 種待撥 installments、過濾財務部、推 LINE+Email、寫 PaymentReminderLog（同日去重）
│   ├── InstallmentValidator.cs       # 分期撥款共用驗證：序號連續 / SUM == 總額 / 已撥款列保護
│   ├── InstallmentUpsertService.cs   # 分期撥款共用 upsert 核心（validate+diff，不 SaveChanges）；獨立 endpoint 與「財務核准當下原子寫入」共用；以 IInstallmentEntity 泛型化
│   ├── AdvanceSupplementService.cs   # 追加預支共用：RollbackAsync（駁回 / 主動放棄兩入口共用，還原父單快照）＋ ResolveCurrentRoundAsync（「此人已審過」四處判定的批次範圍解析，非 advance 恆回 1）
│   ├── InstallmentUpsertResult.cs    # UpsertInstallments 結果 record
│   ├── IGcisService.cs               # 政府開放資料 GCIS 商工登記查詢介面
│   ├── GcisService.cs                # GCIS Open Data REST API 包裝（以統編查公司名稱 / 地址 / 負責人）
│   └── Dapper/                        # Dapper 讀取服務（含 EmployeeProfileReadService）
│       ├── UserReadService.cs
│       ├── RoleReadService.cs
│       ├── DepartmentReadService.cs
│       ├── JobTitleReadService.cs
│       ├── VendorReadService.cs
│       ├── ShiftScheduleReadService.cs     # **per-user 日別解析**（取代 `WorkCalendarHelper` 的 `bool ignoreHolidays` 二元旗標）：
│       │                                    三段退回「個人排班 → 國定假日 → 舊制行事曆判定」。批次版一次撈回整區間全部人（單次 SQL）——
│       │                                    per-user 後出缺勤報表原本「依 IsShiftWorker 分兩組、整趟最多 2 次工作日計算」的 memo 會失效
│       ├── WorkPatternReadService.cs      # 員工出勤型態：IsShiftWorkerAsync（排班制旗標，request-scoped memo）；供請假 / 銷假 / 打卡以「假單所有人 / 打卡本人」解析，勿用呼叫者 id
│       ├── ApprovalReadService.cs
│       ├── ProjectReadService.cs
│       ├── PaymentRequestReadService.cs
│       ├── PreReviewRequestReadService.cs
│       ├── LeaveRequestReadService.cs
│       ├── LeaveRevocationReadService.cs
│       ├── TravelRequestReadService.cs
│       ├── TravelPaymentRequestReadService.cs
│       ├── OvertimeRequestReadService.cs
│       ├── AdvanceRequestReadService.cs
│       ├── WriteOffRequestReadService.cs
│       ├── TravelWriteOffRequestReadService.cs
│       ├── AttendanceReadService.cs        # 出缺勤四支原料查詢：ListInRangeAsync（打卡，不分頁）/ ListApprovedLeavesInRangeAsync（假單）/ ListApprovedRevokedDatesAsync（銷假日，批次）/ **ListClockingEmployeesAsync（應出勤員工母體，供缺勤列：非超管 + 在職 + 持有 `attendances:write` + 部門 scope）**，合併與切頁由 AttendanceLeaveMerger 負責
│       ├── CachedCalendarDayReadService.cs  # 行事曆快取 decorator（以年為粒度），解 LeaveDayExpander 逐張假單展開的 N+1；刻意不註冊 DI，只在唯讀合併流程 new
│       ├── AttendanceReminderReadService.cs
│       ├── AttendanceReminderLogReadService.cs
│       ├── InsuranceBracketReadService.cs
│       ├── EmployeeProfileReadService.cs   # 一次 QueryMultiple 讀回 EmployeeProfile + 9 張子表
│       ├── InstallmentReadService.cs       # 共用：依父表 ID 撈 4 種 installments + JOIN User SignatureUrl + 三態 status 計算
│       ├── PaymentReminderReadService.cs   # UNION **5 種** installments（2026-09 納入預支沖銷差額 WriteOffInstallments），撈 PaidAt 為空且 ExpectedDate 在 N 天內的紀錄
│       └── PayrollReadService.cs           # 月薪計算（含健保眷屬數 + 覆寫值 fallback）；`CalculateMonthlyPayrollAsync(year, month, employeeId = null)` 帶 employeeId 時只算該員工，供 /me/payroll 共用同一份公式
├── Common/
│   ├── ApiResponse.cs                 # 統一回應格式 ApiResponse<T>
│   ├── AppException.cs                # 自定義例外
│   ├── AttachmentProcessor.cs         # 整單批次附件共用：multipart 解析 + magic-byte 驗證 + 上傳 request-attachments（一般請款 / 預支沖銷共用）
│   ├── DesignatedReviewerHelper.cs    # 申請人指定審核者共用：BuildEntities / ReadForFlowAsync / ValidateAndNormalizeAsync / GetSuppressedDesignatedStepOrdersAsync（一條流程多個指定步驟，以 ApprovalStepOrder 綁定步驟；9 種申請類型共用；第一指定步驟＝所選部門最高職稱時抑制其後指定步驟：驗證免填 + 簽核乾淨跳過）；**例外指定審核的兩個真相**：送單前查例外表 `GetEffectiveDesignatedStepOrdersAsync`、送單後看 designee 快照 `EffectiveDesignatedStepOrders`，ValidateAndNormalizeAsync 另負責剔除非法 designee 綁定（防提權）與**限定職稱驗證**（例外命中且有設限定職稱時，designee 職稱不符丟 400）
│   ├── FlexibleDateTimeJsonConverter.cs # 寬鬆日期解析（人事資料卡 payload 用；Safari 不支援 input type=month 手打年月字串）
│   ├── WorkCalendarHelper.cs          # 公司行事曆共用判定（「有行事曆用 CalendarDay.IsHoliday、沒資料退回六日」的單一真相）：區間版 ComputeWorkingDatesAsync 供 LeaveRequestHandler 算請假日／時數，單日版 IsHolidayAsync 供 AttendanceHandler 判休假日免下班卡
│   ├── RequestNoGenerator.cs          # 申請單號取號單一真相（{prefix}yyyyMMdd-NNN 當日流水號）：
│   │                                    **2026-09 起於 SubmitAsync 取號、不再於 CreateAsync**，草稿 RequestNo 為 null（欄位 nullable + filtered unique index）；
│   │                                    **10 種申請類型全部有單號**（2026-09 補上請假 `LV-` / 加班 `OT-` / 銷假 `LVR-`，既有非草稿單以 migration 依送簽日回填）；
│   │                                    三條守則：只在單號為空時取（退回重送不改號）／放在狀態閘門後、Superadmin 自動核准早退前／追加預支批次沿用父單號；
│   │                                    **送簽當下的第二個戳記＝`SubmittedAt`（申請日期，2026-09 新增）**：10 張申請父表各一欄 `DateTime?`，草稿為 null，
│   │                                    以 `x.SubmittedAt ??= Clock.Now;` 緊接取號寫入，與取號共用同三條守則；
│   │                                    `CreatedAt` 維持原義（建立草稿時間）不動，全站「申請日期」一律讀 `SubmittedAt`，兩個戳記必須一起做
│   ├── RequestViewAccess.cs           # 申請單「單筆詳情」檢視授權單一真相（2026-09 新增）：申請人本人 ∪ Superadmin ∪ 持 `approval-tasks:read`
│   │                                    ∪ 曾審核（ApprovalRecord）∪ 指定審核者 ∪ 升級指派（EscalationOverride），不符回 404。
│   │                                    起因：**簽核詳情頁的列印 PDF 是走申請單自身的 `GET /{type}-requests/{id}` 取原料**，而該端點原本只認申請人，
│   │                                    審核者一按列印就拿到 404 →「載入 XX 申請資料失敗，無法匯出 PDF」（travel / travel_payment 全壞、
│   │                                    write_off 系列在**待審階段**壞）。故 `GET /approval-tasks/{appType}/{id}` 與 5 支申請單端點**共用這一份判準**，
│   │                                    收緊授權時要一起收。**advance 為反向案例**：原本毫無存取控制（逐一試 id 可讀遍全公司預支明細，
│   │                                    而列表只列自己的單），2026-09 一併收斂＝補缺口而非放寬。兩個地雷：① TravelRequests 一表兩型，須依 `IsHolidayTravel` 傳 `travel` / `holiday_travel`，
│   │                                    否則查不到自己的簽核足跡；② 路由層權限碼仍在最外層（無 `xxx-requests:read` 者在 AppRouter 就 403，根本進不到這裡）
│   ├── RequestDateGuard.cs            # 申請單「使用者輸入日期」年份合理性單一真相（2026-09 新增，純函式無 I/O）：今日 ±3 年（子女出生日期另為「過去 3 年內且不得晚於今日」），
│   │                                    超出回 400 且訊息點名「是否誤填民國年」。10 種申請表的 Create / Update 全數套用（Ensure / EnsureAll / EnsureEach / EnsurePastWithin）。
│   │                                    三個地雷：① 範圍是防呆不是業務規則（±3 年須容納育嬰留停 730 天的迄日）；② 必須排在該類型的資格 / 額度驗證之前，
│   │                                    否則誤植年份會先撞上「子女未滿 3 歲」這類訊息；③ `default(DateTime)` 不進 guard（要回「必填」而非「0001-01-01 超出範圍」）。
│   │                                    前端同一組數字在 `Admin/src/app/shared/utils/date-bounds.ts`（日期 input 的 min / max），兩處必須一起改
│   ├── InvoiceUniquenessChecker.cs    # 發票號碼唯一性單一真相（2026-09 新增）：批次內去重 + 跨**四張**明細表
│   │                                    （InvoiceItems 請款 / WriteOffItems 預支沖銷 / TravelWriteOffItems 出差沖銷 /
│   │                                    TravelPaymentRequestItems 出差請款），四個 Handler 的 Create + Update 共用，
│   │                                    更新時以 `(InvoiceSource, Id)` 排除自身。**佔號規則**：draft / pending / returned /
│   │                                    approved 皆佔號（草稿即使從未送簽也算），只有 `rejected` 不佔；OCR 辨識本身不寫 DB 故不佔號。
│   │                                    **訊息必須點名佔用者**（`號碼（單別 單號／申請人／狀態）`，草稿印「尚未取號」）——
│   │                                    2026-09 事故：同仁一次掃多張發票試 OCR 留下草稿，拆單時撞號而「發票號碼已存在」沒說是哪張，
│   │                                    使用者既不知草稿也算數、也找不到要刪哪張。收斂前三個 Handler 各寫一份，
│   │                                    請款查沖銷表 / 出差沖銷的兩個跨表查詢都漏加 `!= 'rejected'`（已拒絕的單**永久佔號**、無從自救），
│   │                                    出差請款則完全未檢查。DB 無唯一索引故改規則免 migration；歷史重複診斷見 Scripts/10
│   ├── OvertimePayCalculator.cs       # 勞基法加班費「倍率 / 時薪 / 分段累進」單一真相（純函式）：
│   │                                    平日 1–2h ×1.34、3h 起 ×1.67（上限 4h）；假日 1–2h ×1.34、3–8h ×1.67、9h 起 ×2.67（上限 12h）；
│   │                                    時薪＝ROUND(底薪 ÷ 240, 2)；金額只在總額捨入一次（AwayFromZero）；
│   │                                    日別走 WorkCalendarHelper.IsHolidayAsync，**排班制員工恆判平日**；超出上限截斷計酬但不擋送出；
│   │                                    另出 **`SplitHourTiers(hours, isHoliday)`**（只切分段時數、不算錢，供簽核詳情頁顯示級距而不洩金額）
│   │                                    與 `CapHoursFor(isHoliday)`，`Calculate` 改由前者產生 Segments 再乘時薪 —— 級距表仍是唯一真相，**不得 copy 到前端**
│   ├── LeaveDayExpander.cs            # 請假單「逐日展開」單一真相（Date + Hours + **Segment / Start / End 逐日時段**，2026-09 新增）：供銷假逐日勾選、核准後重算 Hours、出缺勤報表請假合併與時段顯示；時段代碼 full / am / pm / partial（`Constants.LeaveDaySegments`）一律 clamp 在 08:00–17:00，Hours 沿用既有整點差語意故與 End−Start 不必然等長；假別分類常數 WorkingDayLeaveTypes / TimeUnitMap 亦收斂於此，LeaveRequestHandler 轉引
│   ├── ExpectedWorkWindow.cs          # 「該日應出勤（可打卡）時段」單一真相（2026-09 新增，純函式無 I/O，比照 OvertimePayCalculator）：以 08:00–17:00 扣掉當日請假時段，含跨午休正規化（上午假 08–12 → 13:00 開工、下午假 13–17 → 12:00 下班），中段小時假刻意不縮；Start/End 為 null＝當日免出勤。**兩個 AdjustedByLeave 旗標不可省**：無請假時 End 恆為 17:00，補下班卡若無條件取 min 會把 09:00 上班者從 18:00 壓成 17:00。消費點：出缺勤報表應出勤欄 + 未打卡判定、登入自動補卡
│   ├── AttendanceLeaveMerger.cs       # 出缺勤報表「打卡 ∪ 當日請假日 ∪ **缺勤日**」合併單一真相：(員工, 日期) 一列，以 **`RowKind`（clock / leave / absent）** 標示種類 —— 請假列與缺勤列同樣 Id=null，**前端不可再用 Id 判斷**；缺勤列＝工作日無打卡且無請假（今天與未來不算、依 HireDate/ResignDate 夾邊界、展開上限 AbsenceMaxCells=60000）；每列另帶 ExpectedStart/End（走 ExpectedWorkWindow，無請假的工作日為 08:00–17:00、休假日為 null）；逐日時數與時段走 LeaveDayExpander，故採「區間全量載入 → 記憶體合併 → 記憶體切頁」，區間跨度上限 MaxRangeDays=400 天、匯出 pageSize 上限 ExportMaxPageSize=5000。**缺勤判定必須用 leavesByDay 的 Remove 前快照**，否則「有打卡又有請假」的日子會被誤判成缺勤
│   ├── ClockRules.cs                  # **四週彈性工時的打卡規則（純函式，2026-09 新增）**：打卡窗 08:30／準時界線 09:30（超過仍可打、只記遲到）／
│   │                                    應下班時間＝實際上班打卡＋9 小時（請上午半天假者＋4 小時，午休已過不再扣）／
│   │                                    下班三態（早退・正常・逾時，以 T 與 T+30 分為界）／半天假 13:00 交接容許帶 ±5 分。
│   │                                    ＋ `ClockDayPolicy`：**日別鎖定的單一真相**（例假日全鎖連加班申請都不給提、休假日鎖上下班、
│   │                                    國定假日僅活動日預定人力解鎖）。⚠ 全部只在切換日之後生效，切換日前呼叫端不會走到這裡
│   ├── ShiftScheduleMap.cs            # **排班「整月日別組裝」與「國定假日載入」共用實作**（2026-09 新增）：
│   │                                    規格 §10.4 明訂月曆讀取／整月寫入／配額重算**必須共用同一支 helper**，
│   │                                    加上總覽表與活動日改期重跑檢核共 4 個消費點，全部收斂於此。
│   │                                    ⚠ 「查無紀錄即上班日」與 `ShiftScheduleReadService` 的三段退回**刻意不同** ——
│   │                                    後者服務執行期判定（打卡 / 請假扣假日），沒排班要退回舊制行事曆；
│   │                                    本 helper 回答「這個人這個月排了什麼」，沒排就是沒排，不該替他補上週末＝休假
│   ├── WorkDayType.cs                 # **四週彈性工時的四值日別**（work / rest_day / statutory_off / public_holiday）＋
│   │                                    **`PublicHolidayRule`：國定假日 ＝ `IsHoliday` 且 `Description` 非空**。
│   │                                    行事曆把週六日也標成 `IsHoliday=1`（Description 為空），只看旗標會讓 2026-10 的 11 天全變唯讀格
│   │                                    （實際國定假日只有 4 天），員工**永遠排不滿 4 例 4 休**。必須用 `GetByYearAsync` 而非 `GetHolidayDatesAsync`。
│   │                                    月曆讀取／整月寫入／配額重算三個消費點共用此判準
│   ├── ShiftScheduleValidator.cs      # 排班「能不能存」單一真相（純函式）：`例假 4 天排滿 ∧ 連續上班 ≤12 天 ∧ 任意 14 天內 ≥2 例假` ⇒ 可存；
│   │                                    休假未排滿只警示。滾動 14 天視窗**跨月**（併入前後月已定案班表，次月未排則該側不檢核）。
│   │                                    ⚠ 空白月曆第一次儲存必然被擋，為預期行為，UI 須一進畫面就提示
│   ├── ShiftScheduleWindow.cs         # 排班開放期單一真相（純函式）：10–25 日排次月／當月僅當日 08:30 前改當天／歷史唯讀／
│   │                                    當月到職者自 `User.CredentialsSentAt` 起 3 個工作天寬限（以 `CalendarDay` 判定，**刻意不用個人班表**）
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
>
> **全站申請表單共同規範（2026-08）**：11 支申請表單（含銷假、預支追加批次）一律具備
> **儲存 / 送出 in-flight 鎖**（`saving` signal → 按鈕 disabled + spinner）、
> **create 成功後改走 update**（以「後端已有這張單的 id」判定，不是 `isEdit` 路由旗標）、
> **表單內按 Enter 不送出**。缺任一項都會讓同一筆申請被建成兩張單，詳見
> [docs/frontend-design.md §8.4.1 / §8.4.2](docs/frontend-design.md)。
>
> **申請日期＝送簽日（2026-09）**：10 張申請父表新增 `SubmittedAt`，草稿為 null（清單顯示「—」、
> 詳情顯示「（送簽後產生）」），送簽當下與單號同時蓋章、退回重送不改。清單頁欄名一律「申請日期」，
> 詳情頁 / 簽核頁 / 列印 PDF / 款項統計報表（含**日期區間篩選**）皆改讀 `submittedAt`；
> `CreatedAt` 保留原義（建立草稿時間），不再用於顯示。
>
> **日期欄位年份防呆（2026-09）**：所有使用者填寫的日期欄位（加班日期 / 請假起迄 / 出差起迄 /
> 預支日期與需求日 / 發票日期 / 品項日期 / 子女出生日期）限制在**今日 ±3 年**，前端以日期 input 的
> `min` / `max`（單一真相 `Admin/src/app/shared/utils/date-bounds.ts`，行動裝置的原生選擇器直接轉不到範圍外）、
> 後端以 [Api/Common/RequestDateGuard.cs](Api/Common/RequestDateGuard.cs) 回 400 兩端守門，
> 兩處數字必須一起改。起因是一張加班單被打成民國年（`115/09/06` → 西元 0115 年），
> 打卡頁以「加班日期 = 今日」撈不到該單，員工整天無法打加班卡且因單已核准而無法自行修正。

---

## 請假規則

> **詳見** [docs/business/leave-rules.md](docs/business/leave-rules.md)（19 種假別、時間單位、年假、喪假、補休、生理假、家庭照顧假、育嬰留職停薪、重疊驗證、銷假）

---

## 請款簽核流程

> **詳見** [docs/business/approval-flow.md](docs/business/approval-flow.md)（簽核步驟、批次核准、自審跳過、上層級審核、指定審核、跨步驟去重、追加預支 / 銷假重跑簽核）

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
>
> 衍生的「**自己讀自己**」(self / me) 模式（員工讀自己完整資料含薪資 / PII，免 `users:read` / `payroll:read`）見 [§13.4](docs/backend-design.md#134-自己讀自己模式self--me-endpoints)：`GET /me/user`、`GET /me/profile`、`GET /me/files/{container}/{fileName}`（白名單容器 + userId 前綴檢查）、`GET /me/payroll?months=12`（近 N 個月薪資，即時重算型）。

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

## Git 分支策略與部署（單一 repo，分支即環境）

> 2026-06 已移除 Admin submodule，攤平為**單一 git repo**（`Admin/` 為一般資料夾）。前端 + 後端皆由**分支觸發的 GitHub Actions** 自動部署，不再有 submodule pointer / 手動部署流程。

```
staging       # 測試環境（push → kind-pebble SWA + jabez-api-staging）
master        # 正式環境（push → victorious-field SWA + jabez-api）
```

- **remote**：`Remote_GitHub`（`waiting0201/jabez`，部署來源）＋ `Remote_NAS`（離線備份）；同一分支可推兩個 remote
- **前端**：`.github/workflows/azure-static-web-apps-{kind-pebble,victorious-field}-*.yml`（`working-directory: Admin`、`app_location: Admin/dist/Admin/browser`）
- **後端**：`.github/workflows/api-deploy.yml`（`paths: Api/**`；依分支選 `jabez-api-staging` / `jabez-api`，需 GitHub secrets `AZURE_FUNCTIONAPP_PUBLISH_PROFILE_STAGING` / `_PROD`）
- **發版**：`staging` 驗證 → merge 進 `master` → push `master` 觸發正式部署
- `Api/local.settings.json` 永不進版控（含密鑰；歷史已用 filter-repo 清洗，archive/pre-flatten-* 分支保留攤平前內容）

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

2026-05 上線「分期撥款」，**5 種**申請類型（PaymentRequest / AdvanceRequest / TravelRequest / TravelPaymentRequest / **WriteOffRecord**）的撥款資料**統一由子表 `XxxInstallment[]`** 表達：

> **WriteOffRecord（預支沖銷）為 2026-07 新增的第 5 種，規則不同**：`SUM(Amount)` 對應的不是整單金額，而是 [WriteOffRefundCalculator](Api/Common/WriteOffRefundCalculator.cs) 算出的 `RefundDue`＝**本次沖銷造成的超支增額**。未超支（RefundDue = 0）不會有任何 installment，財務核准時也不要求填寫。詳見 [docs/business/approval-flow.md](docs/business/approval-flow.md#預支沖銷差額分期撥款2026-07-新增)。

- **撥款狀態**：由 [InstallmentReadService.ComputeStatus](Api/Services/Dapper/InstallmentReadService.cs) 計算三態（`Unpaid` / `PartiallyPaid` / `FullyPaid`），全部從子表推算
- **List filter「已撥款 / 未撥款」**：[PaymentRequestReadService](Api/Services/Dapper/PaymentRequestReadService.cs) 的 `PaymentStatusClause` 用 `EXISTS / NOT EXISTS` 子查詢 `XxxInstallments`
- **PDF 出納簽名章**：4 個 PDF service 取 `installments[]` 最後一期已撥款者的 `PaidBySignatureUrl` + `PaidAt`
- **撥款明細寫入兩個入口（共用 [InstallmentUpsertService.Apply](Api/Services/InstallmentUpsertService.cs)）**：
  - 財務**核准當下**：`PATCH /approval-tasks/{appType}/{id}/review` 帶 `installments`，與審核同交易原子寫入；財務（FIN）步驟核准撥款類時**必填**（holiday_travel 除外、批次核准除外）
  - 核准**後**修改 / 填實際撥款日：`PATCH /{type}-requests/{id}/installments`（**僅 approved**），舊 `PATCH /{type}-requests/{id}/payment-date` 已移除
- **撥款提醒**：[PaymentReminderService](Api/Services/PaymentReminderService.cs) UNION **5 種** installments 推算（2026-09 納入預支沖銷差額分期；沖銷單無 ProjectId，專案代號取自母預支單）
- **唯讀顯示**：[`<app-installments-table>`](Admin/src/app/shared/components/installments-table.ts) 共用元件（card 結構，跟其他 detail 卡片一致），5 種申請的 detail / form 頁皆引用
- **編輯共用元件**：[`<app-installments-editor>`](Admin/src/app/shared/components/installments-editor.ts)（2026-07 從 approval-task-review 抽出）—— `review` / `manage` 兩種 mode；抽離主因是預支沖銷簽核頁需同頁放兩個編輯器（本單差額撥款 + 關聯預支單撥款明細）
- **編輯 UI 限制**（[approval-task-review](Admin/src/app/features/admin/approval-tasks/pages/approval-task-review/)）：
  - 「+ 新增一期」：`SUM ≥ 總額` 時禁用（**2026-07 移除 `FullyPaid` 條件**：追加預支後總額變大，原已全額撥款的單必須能補期，否則湊不到 `SUM == 總額` 而卡死簽核）
  - 「儲存撥款明細」：`SUM ≠ 總額` 時禁用（同上）
  - 金額 input：`min=1`，`max=剩餘額度`（總額 − 其他列已填）
  - 已撥款列：4 欄位（預計撥款日 / 實際撥款日 / 金額 / 備註）全 readonly + 灰底；刪除按鈕隱藏
  - 後端 `InstallmentValidator.Validate` 提供等同驗證（序號連續 / SUM == 總額 / 已撥款列保護）
- **追加預支的影響**：預支追加核准後 `GrandTotal` 變大，`SUM(installments)` 須等於**新**總額 —— 已撥款列鎖定，財務**補一期**新增金額；`FullyPaid` 會因此變回 `PartiallyPaid`（見 [docs/business/approval-flow.md](docs/business/approval-flow.md#追加預支重跑簽核2026-07-新增)）

歷史：原採兩階段過渡策略，Phase 1 父表保留 `EstimatedPaymentDate` / `PaidAt` / `PaidByUserId` 作 cache；2026-05 Phase 2 完成，DROP 4 張父表的 3 個 cache 欄位 + FK + Index，由 [BackfillInstallmentsFromParentCache](Api/Data/Migrations/) 與 [RemovePaymentDateCacheFromParents](Api/Data/Migrations/) 兩個 migration 串接執行。
