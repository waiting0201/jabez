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
│   └── business/            # 業務功能（11 個檔，每業務一檔）
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
│       └── notifications.md          # 通知系統清單（Email + LINE）
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
    ├── dashboard/              # 打卡系統（即時時鐘、上下班/加班打卡、GPS；**路由需 `attendances:read`**，選單同步；未持有者由根路由 `resolveLandingUrl()` 導向個人資訊）
    │   ├── models/attendance.model.ts
    │   ├── services/attendance.service.ts
    │   └── pages/dashboard/
    ├── auth/
    │   └── pages/ (login, register, forgot-password, lock-screen, two-factor)
    ├── account/             # 員工自助（change-password / line-bind-callback / my-profile）
    │   ├── services/my-profile.service.ts   # 呼叫 /me/user + /me/profile + /me/files + /me/payroll（自助唯讀）
    │   └── pages/my-profile/                # 「個人資訊」唯讀頁：avatar 下拉進入，**4 Tab** 全唯讀 —— 員工基本資料 / 人事資料卡 / 健保眷屬（前 3 個比照管理頁，含薪資）＋ **過往薪資**（2026-08 新增，走 `GET /me/payroll?months=12` 列出近 12 個月，一列一月，點「明細」展開共用元件 `<app-payroll-detail-card>`；到職前月份不列、當月標「本月尚未結算」；**薪資即時重算、無月結快照**，調薪後回溯歷史月份會用現行底薪，頁面已加註說明）
    ├── admin/
    │   ├── users/          # 使用者管理（user-form 含 3 Tab：員工基本資料 / 人事資料卡 / 健保眷屬；Tab1「員工資訊」含 **「排班制（六日與國定假日視為工作日）」** 勾選框（`isShiftWorker`，賣店 / 營業所用，清單頁姓名旁掛「排班制」badge）；含 employee-profile.service / hr-profile-pdf.service / 9 組 FormArray；**薪資為欄位級權限 `payroll:read`**：進得了員工管理（`users:read`）不等於看得到薪資，Tab1 的 8 個薪資／勞健保欄（2026-08 移除職務加給 / 主管加給 / 外派加給，加給剩其他加給 + 代扣代付款（2026-08 由「調整差額」更名，識別字仍為 `AdjustmentDifference`））、Tab2 薪資調整歷史、Tab3 健保費試算、列印 PDF 第 3 頁皆需另持 `payroll:read`，前端共用 `canSeeSalary` + `SALARY_CONTROLS`（`@if` 隱藏區塊 + `disable()` 控制項 + 送出前剔除 payload key），後端共用 [Api/Common/PayrollFieldAccess.cs](Api/Common/PayrollFieldAccess.cs) 抹除回應並拒絕寫入；薪資調整歷史改為**條件式**整批替換（`null`＝不變更）避免無權者送空陣列刪光歷史；`/me/user`、`/me/profile` 刻意全開，員工看自己的薪資不受影響）
    │   ├── roles/          # 角色管理（僅 Superadmin）
    │   ├── permissions/    # 權限管理（僅 Superadmin）
    │   ├── departments/    # 部門管理
    │   ├── job-titles/     # 職稱管理
    │   ├── vendors/        # 廠商管理（清單頁含**關鍵字搜尋列 + 分頁**，走 `GET /vendors?page=&pageSize=&search=`（每頁 20 筆，比照 projects），後端模糊比對 廠商名稱 / 統編 / 身分證字號 / 聯絡人 / 電話 / **匯款戶名**；含 vendor-quick-add-modal；統編/身分證字號類型切換，統編 blur 自動帶出 GCIS 公司資料；個人工作室上傳身分證正反面；存摺封面必填；**匯款資料為四個獨立欄位**：匯款戶名 `bankAccountName` / 匯款銀行 `bankName` / 銀行代號 `bankCode` / 銀行帳號 `bankAccount`，戶名常與廠商名稱不同（例：橘之鄉 → 旭工實業有限公司）故可用戶名反查，vendor-form 與 quick-add-modal 皆為四格）
    │   ├── approvals/      # 簽核流程設定（ApprovalItem + Steps；ApprovalItem 含 DepartmentId 部門維度，可為同一申請類型設「各部門專屬流程 + 通用預設」，送單時依申請人部門挑流程；子部門未設專屬流程時自動沿用最近祖先部門流程；Step 含 MinDays 天數門檻，null＝一律納入、N＝申請天數 ≥ N 才納入，目前供請假依天數分流；**非指定審核步驟可勾「例外指定審核」並逐一挑使用者（FormArray + select 列，整批替換 exceptionUserIds）**，名單內的申請人送單時該步驟改由申請人自行指定審核者，timeline 顯示「例外指定 N 人」badge；**例外名單下方另可設「限定職稱」（可多選，整批替換 designatedJobTitleIds）**，申請人的指定審核者只能挑這些職稱的人，timeline 顯示「限定職稱：…」badge）
    │   ├── approval-tasks/ # 待審核任務清單（已核准頁籤篩選列：全部類型下拉（所有人）＋**申請人下拉（僅財務體系部門 / Superadmin，選項來自 /approval-tasks/applicants）**＋撥款退款子篩選（僅財務體系；按鈕組 全部 / 尚未撥款 / 部分撥款 / 全部撥款 / **已結案**（`paymentStatus=closed`，只撈預支 / 出差預支 `IsClosed=1`，其餘類型後端 1=0 短路））；**摘要欄的預支申請加註送簽批次**：第1次顯示總額、第N次追加顯示「本次／總額」，批次標籤共用 advance-requests 的 roundLabel()；**狀態欄的預支 / 出差預支已結案時加註「已結案」badge**，與 advance-list / travel 詳情同一真相 `AdvanceRequest.IsClosed` / `TravelRequest.IsClosed`、同一樣式，holiday_travel 不走沖銷故排除；**簽核作業詳情頁（approval-task-review）另有「結案資訊」卡片**（`closureInfo(task)` → 共用元件 `<app-closure-info-card>`；`advance` / `travel` 顯示本單結案狀態 + 退款四欄，`write_off` / `travel_write_off` 顯示**關聯母單**（預支單 / 出差單）結案狀態，per-type 差異收斂在 `isRelatedClosure()` / `closureTitle()`），頁首同掛「已結案」badge）
    │   ├── projects/       # 專案管理
    │   ├── payment-requests/  # 請款申請（廠商請款 type=vendor / 一般請款 type=general 明細下方皆含整單批次附件上傳，共用 shared/components/attachments-upload；**請款原因必填**）
    │   ├── pre-review-requests/ # 預審申請（事前預審，clone 自請款；無撥款、不計入報表；品項類別下拉 + 報價單 OCR；含 pre-review-pdf.service 列印合併所有上傳檔；**預審說明必填**）
    │   ├── leave-requests/    # 請假申請（除歲時祭儀假與育嬰留職停薪外的 17 種假別選起迄日後皆扣國定假日與六日並列請假日清單，走輕量端點 /leave-requests/working-days；小時單位（事假/**家庭照顧假**/病假/產檢假/陪產假）跨日逐日累加只算工作日；**家庭照顧假**（`family_care`，性平法 §20）全年 7 日／56 小時上限、比照事假全額扣薪但薪資單獨立一列，家庭成員範圍僅表單提示不入庫；產假區間仍 56 個日曆天但只計其中工作日；含職務代理人下拉；依天數決定簽核關卡 <3 天單位主管 / ≥3 天 +部門最高主管+總監，靠 ApprovalStep.MinDays；**已核准的假可提「銷假」**：列表／唯讀檢視頁的「銷假」按鈕進 leave-revocation-form（`:id/revoke` / `leave-revocations/:id[/edit]` 三模式共用），逐日 chip 勾選要取消的日期（只含今天以後、未被其他銷假單佔用者）+ 銷假原因 + 指定審核者，送出後重跑同一份請假簽核；核准後父單 Hours 遞減、全銷轉 `cancelled` badge「已銷假」，部分銷加註「部分銷假」badge；
    │   │                      **育嬰留職停薪（2026-08 新增，兩個代碼）**：`parental_leave`（長期留停，**連續日曆天**、每名子女合計 730 天）+ `parental_leave_daily`（彈性單日新制，強制 `EndDate = StartDate`、每人每年 30 日且併入該子女總額度）；
    │   │                      資格為「在職滿 6 個月 + 子女未滿 3 歲」（Superadmin 繞過），新增欄位 `ChildBirthDate`（額度分組鍵）/ `ContinueInsurance`（僅記錄續保意願）；
    │   │                      `parental_leave` **刻意不列入 WorkingDayLeaveTypes**（否則跨年送件會被「行事曆未匯入」擋死、逐日 chip 也會爆量），故長期留停**不開放銷假**；
    │   │                      薪資「整月留停排除名單 + 當月按在職天數 ÷ 30 折減底薪與加給」，勞健保與勞退自提用折減前的 `insuredBaseSalary` 不打折，實領為負時前後端皆顯示「應補繳保費」警示；
    │   │                      年資扣除留停天數（`Api/Common/SeniorityHelper.cs` 單一真相，特休額度隨之暫停累積），額度端點 `GET /leave-requests/parental-quota`）
    │   ├── travel-payment-requests/ # 出差請款申請（小額已代墊直接請款，無沖銷）
    │   ├── travel-requests/   # 出差預支申請（走沖銷流程）
    │   ├── holiday-travel-requests/ # 假日執行活動申請（共用 TravelRequest entity，IsHolidayTravel=true，計入假日津貼；參與人員可逐日勾選個人參與日期，未勾選＝全程參與；**每個勾選日期可再指定「全天／上午／下午」**：chip 四態循環 未選 → 全天 → 上午 → 下午 → 未選，半天以 0.5 天計入假日津貼，個人天數存 `TravelRequestParticipant.HolidayDays decimal(5,1)`；申請人本人不逐日、不半天，一律沿用整單 `TravelRequest.HolidayDays`）
    │   ├── overtime-requests/ # 加班申請（走簽核流程；**關聯專案為必填明細（至少一列）**：FormArray 每列一個專案下拉（來源 /projects/active?all=true 全部未結案專案，支援跨部門；**下拉自動排除其他列已選過的專案**）+ 該案預估時數，欄位標題註記「同部門專案可複選；支援專案請獨立申請」（業務提示，非硬性過濾）；**預估總時數改為唯讀自動加總**（父表 `OvertimeRequest.EstimatedHours` 為 `OvertimeRequestProject` 子表的合計快取，後端 Create/Update 重算，補休時數 / 登入自動補打加班結束卡 / 通知摘要皆沿用此欄）；指定審核者卡片加註「跨部門支援時第一審核者填該專案協理、第二審核者選自部門協理」）
    │   ├── advance-requests/  # 預支申請（已核准單可新增「追加預支」批次：/:id/supplements/new 與 /:id/supplements/:round/edit 共用 advance-form 的追加模式；詳情頁預支日期改為批次清單、費用明細加「批次」欄；共用 roundLabel() 為批次標籤單一真相；**明細金額三欄連動：總價 = 現金(預支) + 支票(月結)，任兩欄輸入自動算出第三欄，規則與預支沖銷相同**）
    │   ├── write-off-requests/ # 預支沖銷申請（獨立簽核流程；**清單依預支單 group，母層列操作欄「檢視」進入彙總頁 write-off-overview（`/by-advance/:advanceId`）：一頁看完預支單完整資訊 + 該單全部沖銷單完整資訊**；明細下方含整單批次附件上傳，共用 shared/components/attachments-upload；新增表單選定預支單後，於「預支單」卡片下方唯讀列出該單全批次預支費用明細（含追加，依批次分組），資料由 /write-off-requests/available-advances 一併帶回；**沖銷資訊卡改為 `<app-write-off-summary>` 列出預支各批次金額 + 各次沖銷金額 + 待沖銷餘額 / 應撥差額**；**詳情頁與簽核頁另有「預支單結案資訊」卡（共用 `<app-closure-info-card>`，`showRefund=false` + `alwaysShow=true`：只呈現關聯預支單的已結案／未結案與結案時間，撥款金額仍由該頁既有「撥款」語彙欄位負責）**；**超支差額走分期撥款**，明細另有「支票已支付」註記欄，該欄在簽核頁對所有審核者顯示，但**僅財務管理部（`DepartmentCodes.FinanceStep`，與撥款日／結案同範圍，不含總監室／會計室）/ Superadmin 可勾選**，其他人 checkbox disabled 反白；**明細金額三欄連動：總價 = 現金花費 + 支票金額，任兩欄輸入自動算出第三欄**；**2026-08 重複建單修正**：表單送出／儲存加 `saving` in-flight 鎖（按鈕 disabled + spinner，避免上傳期間連按建出多筆）、create 成功即記住 `editId` 讓送簽失敗的重送走 update 而非再建一張、表單內按 Enter 不再直接送出；**「已沖銷」一律只計已核准**（下拉與詳情頁同基準），草稿／簽核中金額改以 `pendingWriteOffTotal` 顯示「另有 N 元沖銷中」提示；發票號碼唯一性檢查排除已拒絕的沖銷單；Superadmin 可對他人預支單建沖銷（與下拉範圍一致）；`RequestNo` 補上唯一索引宣告（含 travel_write_off））
    │   ├── travel-write-off-requests/ # 出差預支沖銷申請（獨立簽核流程；**詳情頁與簽核頁有「出差單結案資訊」卡（共用 `<app-closure-info-card>`，`showRefund=false` + `alwaysShow=true`：只呈現關聯出差單的已結案／未結案與結案時間，撥款金額仍由該頁既有「撥款」語彙欄位負責）**）
    │   ├── insurance-brackets/ # 勞健保級距維護
    │   ├── payroll/           # 人事薪資（月薪計算 + PDF 匯出 + **Excel 總表匯出**：查詢列「匯出總表」鈕，一位員工一列 × 32 欄（基本 4 / 應發 10 / 扣項 15 / 其他 3）+ 合計列，資料直接取自已載入的 `payroll()` signal（`GET /payroll` 本身不分頁），無後端變動）
    │   ├── attendance-reminder-logs/ # 打卡提醒推播紀錄（僅 Superadmin）
    │   ├── payment-reminder-logs/ # 撥款提醒推播紀錄 + 手動觸發（僅 Superadmin）
    │   ├── reports/        # 報表（出缺勤 / 加班 / 款項統計 / 專案水位）；**出缺勤紀錄列出「打卡紀錄 ∪ 當日請假日」**：全天請假沒打卡的人也會出現一列（`id=null` 虛擬列，上下班留空 + 「請假」badge + 假別 + 當日時數、不可編輯），同日多張假單合併為一列；假別中文 import 自 leave-request.model 的 19 種 LEAVE_TYPE_LABELS；已補上分頁（每頁 20，簡化版上一頁 / 下一頁），Excel 匯出走 `?export=true`；月篩選不再提供「全部年份 / 全部月份」（合併需有界區間）。款項統計 1 個 endpoint 支援 全部 + 6 個類別 dropdown（全部 / 請款 / 預支 / 預支沖銷 / 出差請款 / 出差預支 / 出差預支沖銷；「全部」為 6 種 UNION ALL），權限只看 `reports-payment:read`，不需各別 `xxx-requests:read`。**專案水位表的「總專案水位」欄（分母＝契約金額，含公司保留 40%）為欄位級權限 `reports-project-water-level:total`**：只有 `reports-project-water-level:read` 者頁面照進、業務執行水位照看，但總水位整欄消失（前端 `canSeeTotal` 同時控 `<th>` / `<td>` / 空列 colspan），後端 `ProjectWaterLevelHandler` 亦把 `TotalPercentage` / `PreImportUsedAmount` / `RemainingAmount` 抹為 null / 0
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

## 後端：Api（Azure Functions .NET 10）

> **設計規範與技術棧詳見** [docs/backend-design.md](docs/backend-design.md)（Handler / DTO / Dapper / EF Core / Router / JWT / 時區 / 檔案上傳 / 命名 / Code Review Checklist 一律統一定義於該文件）

### 目錄結構

```
Api/
├── Functions/
│   ├── RouterFunction.cs              # HttpTrigger，catch-all route {*route}
│   ├── AttendanceReminderFunction.cs  # TimerTrigger：限定 7-9 / 16-18 Taipei 時段每分鐘檢查，落在「上下班前 2 分鐘起算 10 分鐘時間窗」內則 LINE 推播；cron 由 `AttendanceReminderCron` app setting 控制。**`IsPastDue` 不跳過**（冷啟動延遲會整天不發），改由 Service 端 `batchStart` 冪等閘去重；**六日只推排班制員工**（`IsShiftWorker`，賣店照常營業），一個都沒有時維持整批跳過，平日仍不看行事曆（國定假日照推）
│   └── PaymentReminderFunction.cs     # TimerTrigger：每日 09:00 Taipei 跑撥款日將屆提醒；cron 由 `PaymentReminderCron` 控制；提前天數讀 `SystemSetting.PaymentReminderDaysBefore`，推給財務體系部門全員
├── Routing/
│   └── AppRouter.cs                   # C# 12 List Pattern 路由分派器
├── Handlers/                          # 25 個 Handler（業務邏輯）
│   ├── AuthHandler.cs                 # 登入、刷新 Token（登入時自動補打漏打的下班卡＝**上班打卡時間 + 9 小時（上午打卡，含午休）／+ 8 小時（下午打卡）**，並標記 `IsClockOutAuto` 供出缺勤清單顯示「系統補卡」badge；加班結束卡＝加班開始 + 申請單預估時數）
│   ├── UserHandler.cs                 # 使用者 CRUD（含原住民 / 低收入 / 殘障證明 + 健保 / 勞保覆寫）；GetMineAsync = GET /me/user 員工讀自己（免 users:read）
│   ├── EmployeeProfileHandler.cs     # 員工人事資料卡 GET / PUT（multipart：HR JSON + 身分證正反面 + 最高學歷證明 + 存摺封面）；GetMineAsync = GET /me/profile 員工讀自己（免 users:read）
│   ├── RoleHandler.cs
│   ├── PermissionHandler.cs
│   ├── DepartmentHandler.cs
│   ├── JobTitleHandler.cs             # 職稱 CRUD（刪除時清洗 ApprovalStepDesignatedJobTitles 的 NO_ACTION 外鍵）
│   ├── VendorHandler.cs               # 廠商管理 CRUD（匯款資料四欄 BankAccountName 戶名 / BankName 銀行 / BankCode 代號 / BankAccount 帳號；清單支援 `?search=` 關鍵字模糊比對 名稱 / 統編 / 身分證字號 / 聯絡人 / 電話 / 匯款戶名，並支援 `?page=&pageSize=` 分頁：帶分頁參數回 PagedResult、不帶回平面陣列；multipart 支援存摺封面（必填）/ 身分證正反面上傳；統編與身分證字號擇一；lookup / lookup-by-tax-id / POST 開放任何登入者；刪除受 PaymentRequest 引用保護）
│   ├── ApprovalHandler.cs             # ApprovalItem + Steps CRUD（ApprovalItem 含 DepartmentId 部門維度；唯一性以 (ApplicationType, DepartmentId) 判定；/active 依呼叫者部門解析流程，優先序：自身部門 > 最近祖先部門（沿 ParentId 往上）> 通用預設）；Step 含 DesignatedRequiresDepartment（指定審核步驟可設「需先選部門再選人」，支援一條流程多個指定步驟；多指定步驟前端連動見 shared/components/designated-reviewers-picker：連動閘控 + 部門帶入 + 部門最高層級自動略過；**Step 另含例外指定審核名單 `exceptionUserIds`（ApprovalStepException 子表，整批替換）**：非指定審核步驟可挑指定使用者，名單內的申請人送單時該步驟改由申請人自行指定審核者，與 UseApplicantDesignated 互斥；**例外步驟另可設限定職稱 `designatedJobTitleIds`（ApprovalStepDesignatedJobTitle 子表，整批替換）**，限制申請人只能指定這些職稱的人，非例外步驟一律清空）
│   ├── ApprovalTaskHandler.cs         # 待審核任務查詢與審核動作（列表另支援 applicationType / submittedByUserId 篩選；**申請人篩選與 GET /approval-tasks/applicants 限財務體系部門或 Superadmin**，判定共用 CanFilterByApplicant → DepartmentCodes.FinancialAndAbove；**單筆詳情 `GET /approval-tasks/{appType}/{id}` 的存取控制另放行「申請人本人」**（`IsApplicantAsync` 逐型別比對 SubmittedById / EmployeeId），否則申請人拿不到 flow / approvalRecords，請款列印按鈕不出現、兩張沖銷表印出無簽核欄的 PDF；**沖銷結案採「登記制」**：財務於其簽核關卡勾 `closeAdvance` 只設沖銷單的 `PendingClose`，待整張沖銷單轉 `approved` 才真正寫母單 `IsClosed`（財務多為倒數第二關，提前結案會讓總監退回後無法補開沖銷單）；退回／拒絕清除登記；步驟判定走 `IsFinanceStepAsync`（`DepartmentCodes.FinanceStep`，**禁止硬編碼 "FIN"**），勾了但非財務步驟改回 400 不再靜默）
│   ├── ProjectHandler.cs
│   ├── PaymentRequestHandler.cs       # 請款申請 CRUD（單號 PR-yyyyMMdd-NNN）
│   ├── PreReviewRequestHandler.cs     # 預審申請 CRUD + Submit（單號 PRV-yyyyMMdd-NNN；報價單上傳 blob container=quotes；無 installments、不計入報表）
│   ├── QuoteOcrHandler.cs             # 報價單 OCR（POST /quote-ocr，回傳品項列表 itemName/amount/note）
│   ├── LeaveRequestHandler.cs
│   ├── LeaveRevocationHandler.cs      # 銷假申請 CRUD + Submit（GET /leave-requests/{id}/revocable-dates 逐日可銷清單；POST /leave-requests/{id}/revocations；/leave-revocations/*；ApprovalItem 以 "leave" 解析＝跑原本的請假簽核，簽核紀錄以 "leave_revocation" 隔離）
│   ├── TravelRequestHandler.cs        # 出差預支申請 CRUD（單號 TR-yyyyMMdd-NNN；假日執行活動為 HTR-yyyyMMdd-NNN；預支後沖銷）
│   ├── TravelPaymentRequestHandler.cs # 出差請款申請 CRUD（單號 TPR-yyyyMMdd-NNN；小額代墊直接請款）
│   ├── OvertimeRequestHandler.cs      # 加班申請 CRUD
│   ├── AdvanceRequestHandler.cs       # 預支申請 CRUD（單號 ADV-yyyyMMdd-NNN）＋**追加預支批次**（POST/PATCH/DELETE /advance-requests/{id}/supplements[/{roundNo}]；新增即送簽、無草稿階段；有進行中批次時禁止整單編輯/刪除）
│   ├── WriteOffRequestHandler.cs      # 預支沖銷申請 CRUD（獨立簽核流程）＋**依預支單彙總檢視**（GET /write-off-requests/by-advance/{advanceRequestId}，回傳預支單完整資訊 + 該單全部沖銷單）＋**差額撥款分期**（PATCH /write-off-requests/{id}/installments，SUM 對應 RefundDue 超支增額）＋**支票已支付註記**（PATCH /{id}/check-payments）
│   ├── TravelWriteOffRequestHandler.cs # 出差預支沖銷申請 CRUD（獨立簽核流程）
│   ├── AttendanceHandler.cs           # 打卡（上班/下班/加班開始/加班結束；請假時段內擋上下班打卡；**休假日（行事曆假日／六日）或當日全日請假時，加班開始免下班卡**（**排班制員工 `User.IsShiftWorker` 恆不適用休假日條件**，週六仍須先打下班卡），無紀錄則建立只含加班時間的紀錄；**2026-08 起納入權限管理**：打卡走 `attendances:read/write`（員工對自己）、出缺勤報表列表與 `PUT/PATCH /attendances/{id}` 走 `reports-attendance:read/write`（管理者對別人），後者另在 Handler 內套部門可見性 scope 控管「能改誰」）
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
│   └── Seed/                          # 一次性匯入工具（共用 RocDateParser 解民國年）
│       ├── EmployeeImporter + EmployeeImportDtos + employee-import.json  # 員工人事資料（RUN_EMPLOYEE_IMPORT 旗標，IMPORT_UPLOAD_FILES 控制附件上傳）
│       ├── ProjectImporter + ProjectImportDtos + project-import.json     # 專案資料（RUN_PROJECT_IMPORT 旗標，PROJECT_IMPORT_DRY_RUN 只印不寫；來源 reference/專案資料-115.07.29.xls；以 Code upsert、期別明細全量重建）
│       └── VendorImporter + VendorImportDtos + vendor-import.json        # 廠商匯款資料 31 筆（RUN_VENDOR_IMPORT 旗標，VENDOR_IMPORT_DRY_RUN 只印不寫；來源 reference/壯圍沙丘廠商匯款資料0812.xlsx，來源「匯款帳號」一格拆成四欄；以 Name upsert；來源缺統編／存摺封面，直寫 entity 繞過 Handler 驗證，Note 標記待補，後台編輯儲存時仍會被擋須先補件）
├── Models/
│   ├── Entities/                      # 53 個資料庫實體（新增 **銷假申請 LeaveRevocation + 逐日明細 LeaveRevocationDate**（獨立子單，父單送簽期間不動；LeaveRequest 另加 OriginalHours 與 `cancelled` 終止狀態）/ **簽核步驟例外指定審核名單 ApprovalStepException** + **例外的限定職稱 ApprovalStepDesignatedJobTitle** / **預支沖銷差額分期 WriteOffInstallment**（第 5 種分期撥款子表）/ **WriteOffRecord + TravelWriteOffRecord 新增 `PendingClose`**（財務登記結案，待整張單核准才生效）/ **追加預支批次 AdvanceRequestSupplement**（只存 RoundNo≥2，Round 1 = 父單本身）/ **TravelRequestParticipantDate 參與人員個別參與日期** / EmployeeProfile / EducationRecord / EmploymentHistoryRecord / FamilyMember / ProfessionalTraining / LanguageAbility / JobTransferRecord / RewardPunishmentRecord / SalaryAdjustmentRecord / HealthInsuranceDependent / **5 個分期撥款表 PaymentRequestInstallment / AdvanceRequestInstallment / TravelRequestInstallment / TravelPaymentRequestInstallment / WriteOffInstallment** / **PaymentReminderLog** / **整單批次附件 PaymentRequestAttachment / WriteOffAttachment** / **預審申請 PreReviewRequest / PreReviewItem / PreReviewRequestAttachment**）
│   └── Dtos/                          # 21 個 DTO 檔案（新增 **LeaveRevocationDtos** / EmployeeProfileDtos / **InstallmentDtos** / **PreReviewRequestDtos**）
├── Services/
│   ├── IJwtService.cs
│   ├── JwtService.cs                  # HS256 JWT 產生與驗證
│   ├── IEscalationService.cs          # 簽核升級服務介面
│   ├── EscalationService.cs           # 簽核升級邏輯（上層部門主管遞迴 + 代理人）
│   ├── EscalationResult.cs            # 升級結果 record
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
│       ├── AttendanceReadService.cs        # 出缺勤三支原料查詢：ListInRangeAsync（打卡，不分頁）/ ListApprovedLeavesInRangeAsync（假單）/ ListApprovedRevokedDatesAsync（銷假日，批次），合併與切頁由 AttendanceLeaveMerger 負責
│       ├── CachedCalendarDayReadService.cs  # 行事曆快取 decorator（以年為粒度），解 LeaveDayExpander 逐張假單展開的 N+1；刻意不註冊 DI，只在唯讀合併流程 new
│       ├── AttendanceReminderReadService.cs
│       ├── AttendanceReminderLogReadService.cs
│       ├── InsuranceBracketReadService.cs
│       ├── EmployeeProfileReadService.cs   # 一次 QueryMultiple 讀回 EmployeeProfile + 9 張子表
│       ├── InstallmentReadService.cs       # 共用：依父表 ID 撈 4 種 installments + JOIN User SignatureUrl + 三態 status 計算
│       ├── PaymentReminderReadService.cs   # UNION 4 種 installments，撈 PaidAt 為空且 ExpectedDate 在 N 天內的紀錄
│       └── PayrollReadService.cs           # 月薪計算（含健保眷屬數 + 覆寫值 fallback）；`CalculateMonthlyPayrollAsync(year, month, employeeId = null)` 帶 employeeId 時只算該員工，供 /me/payroll 共用同一份公式
├── Common/
│   ├── ApiResponse.cs                 # 統一回應格式 ApiResponse<T>
│   ├── AppException.cs                # 自定義例外
│   ├── AttachmentProcessor.cs         # 整單批次附件共用：multipart 解析 + magic-byte 驗證 + 上傳 request-attachments（一般請款 / 預支沖銷共用）
│   ├── DesignatedReviewerHelper.cs    # 申請人指定審核者共用：BuildEntities / ReadForFlowAsync / ValidateAndNormalizeAsync / GetSuppressedDesignatedStepOrdersAsync（一條流程多個指定步驟，以 ApprovalStepOrder 綁定步驟；9 種申請類型共用；第一指定步驟＝所選部門最高職稱時抑制其後指定步驟：驗證免填 + 簽核乾淨跳過）；**例外指定審核的兩個真相**：送單前查例外表 `GetEffectiveDesignatedStepOrdersAsync`、送單後看 designee 快照 `EffectiveDesignatedStepOrders`，ValidateAndNormalizeAsync 另負責剔除非法 designee 綁定（防提權）與**限定職稱驗證**（例外命中且有設限定職稱時，designee 職稱不符丟 400）
│   ├── FlexibleDateTimeJsonConverter.cs # 寬鬆日期解析（人事資料卡 payload 用；Safari 不支援 input type=month 手打年月字串）
│   ├── WorkCalendarHelper.cs          # 公司行事曆共用判定（「有行事曆用 CalendarDay.IsHoliday、沒資料退回六日」的單一真相）：區間版 ComputeWorkingDatesAsync 供 LeaveRequestHandler 算請假日／時數，單日版 IsHolidayAsync 供 AttendanceHandler 判休假日免下班卡
│   ├── LeaveDayExpander.cs            # 請假單「逐日展開」單一真相（Date + Hours）：供銷假逐日勾選、核准後重算 Hours、出缺勤報表請假合併；假別分類常數 WorkingDayLeaveTypes / TimeUnitMap 亦收斂於此，LeaveRequestHandler 轉引
│   ├── AttendanceLeaveMerger.cs       # 出缺勤報表「打卡 ∪ 當日請假日」合併單一真相：(員工, 日期) 一列，只有請假無打卡時產生 Id=null 虛擬列；逐日時數走 LeaveDayExpander，故採「區間全量載入 → 記憶體合併 → 記憶體切頁」，區間跨度上限 MaxRangeDays=400 天、匯出 pageSize 上限 ExportMaxPageSize=5000
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
- **撥款提醒**：[PaymentReminderService](Api/Services/PaymentReminderService.cs) UNION 4 種 installments 推算（**不含**沖銷差額分期，另案評估）
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
