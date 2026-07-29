# Jabez 前端設計規範

本文件彙整 Jabez Admin 前端的視覺與互動設計規範。**新功能開發或修改頁面前，務必先參考本文件取得對齊樣式**；若與本文件衝突則以本文件為準（CLAUDE.md 同步引用本文件）。

---

## 1. 技術棧

| 項目 | 規格 | 備註 |
|---|---|---|
| 框架 | Angular 21.1 | Standalone Component 為唯一寫法，禁止 NgModule |
| 語言 | TypeScript 5.9 | strict mode 全開 |
| 樣式 | Tailwind CSS v4 + SCSS（component-scoped only） | 主入口 [Admin/src/tailwind.css](../Admin/src/tailwind.css) |
| 狀態管理 | Angular Signals | 禁止用 BehaviorSubject 管 component state |
| 表格 | @tanstack/angular-table | 列表頁一律使用 |
| 圖表 | ApexCharts (ng-apexcharts) | 報表頁使用 |
| 通知 | ngx-toastr | 禁止自製 alert/modal 替代 |
| PDF 匯出 | jsPDF + jspdf-autotable | 中文字型走 [pdf-core.service.ts](../Admin/src/app/shared/services/pdf-core.service.ts) |
| 圖檔壓縮 | [image-compression.service.ts](../Admin/src/app/shared/services/image-compression.service.ts) | HEIC→JPEG / Canvas resize / JPEG quality；PDF passthrough |
| Icon | SVG sprite | `<svg class="sa-icon"><use href="/assets/icons/sprite.svg#NAME"></use></svg>` |
| Modal / Dropdown | @ng-bootstrap | 僅用其行為，CSS class 在 tailwind.css `@layer components` |

> **禁止引入**：Bootstrap 5、node-waves、其他 CSS 框架。

### 1.1 樣式檔案結構

| 檔案 | 內容 |
|---|---|
| [Admin/src/tailwind.css](../Admin/src/tailwind.css) | 主入口：`@layer base / components / utilities`；CIS design tokens；layout vars (`--app-header-height: 5rem`、`--menu-width: 18rem` 等) |
| [Admin/src/styles.scss](../Admin/src/styles.scss) | 僅 `@use 'ngx-toastr/toastr.css';` 一行 |
| `assets/icons/sprite.svg` | SVG sprite 集中管理；新 icon 從 [Feather Icons](https://feathericons.com/) 加入 |

### 1.2 歷史脈絡（重要）

- **2026-02 Bootstrap 5 完整移除**：Bootstrap 5 + node-waves 從 `package.json` 整批 remove；SmartAdmin theme assets 已刪除（`src/assets/sass/`、`src/assets/webfonts/smartadmin/`、`src/assets/css/`）
- **@ng-bootstrap 仍保留**：僅用其 JS 行為元件（`NgbDropdown` / `NgbModal` / `NgbOffcanvas` / `NgbCollapse`）；對應 CSS class 名稱已在 [tailwind.css](../Admin/src/tailwind.css) `@layer components` 重新定義，視覺與行為解耦
- **Bootstrap → Tailwind 間距對照**（移轉舊 component 時用）：Bootstrap `m-3` (1rem) → Tailwind `m-4`；Bootstrap `m-4` (1.5rem) → Tailwind `m-6`
- **App entry 變動**：[Admin/src/app/app.ts](../Admin/src/app/app.ts) 已移除 `Waves.attach` / `Waves.init`（node-waves 已刪除）

---

## 2. CIS 色彩系統

設計 token 全數定義於 [Admin/src/tailwind.css](../Admin/src/tailwind.css) 的 `:root`，PDF 用 RGB 常數位於 `payroll-list.ts` 等 PDF service 的 `CIS` 物件。

### 品牌主色

| Token | 色碼 | 用途 |
|---|---|---|
| `--forest` | `#699F34` | 品牌綠：按鈕、表頭、PDF 裝飾線 |
| `--forest-mid` | `#4A6B3A` | hover 狀態、次要強調 |
| `--forest-light` | `#6B8F5E` | 輔助綠 |

### 中性色 / 強調色

| Token | 色碼 | 用途 |
|---|---|---|
| `--text-primary` | `#525358` | 正文、標題 |
| `--text-secondary` | `#6E6F73` | 標籤、次要文字 |
| `--text-muted` | `#A39685` | 註解、浮水印 |
| `--accent` | `#8C7355` | 連結、焦點框 |
| `--accent-muted` | `#735E42` | 深棕變體 |

### 語意色

| Token | 色碼 | 用途 |
|---|---|---|
| `--green` | `#4A6B3A` | success |
| `--yellow` | `#B8892A` | warning |
| `--red` | `#A04040` | error / 扣款 |
| `--purple` | `#7C5E8C` | info |

### 背景 / 邊框

| Token | 色碼 | 用途 |
|---|---|---|
| `--bg-base` | `#F5F2ED` | 頁面底色 |
| `--bg-surface` | `#FDFAF5` | 卡片、面板 |
| `--bg-elevated` | `#EDE9E1` | 提升區塊 |
| `--border` | `#DDD6C8` | 邊框 |

### 側欄

| Token | 色碼 | 用途 |
|---|---|---|
| `--sidebar-bg` | `#699F34` | 側欄背景（品牌綠） |
| `--sidebar-surface` | `#5B8E2D` | 深一階（子選單底） |
| `--sidebar-hover` | `#78AD42` | hover 回饋 |
| `--sidebar-text` | `rgba(255,255,255,0.92)` | 選單文字 |
| `--sidebar-text-dim` | `rgba(255,255,255,0.58)` | 分類標題 |

### Logo

| 檔案 | 格式 | 用途 |
|---|---|---|
| `assets/img/logo.png` | PNG（透明背景、直式） | 網頁 UI、Topbar、Login 頁 |
| `assets/img/logo.jpg` | JPG（橫式含公司全名） | PDF 薪資明細表抬頭 |

---

## 3. 頁面排版

### 容器寬度

外層一律 `container-fluid py-3`，col 外層包 `<div class="row g-4">`。**所有 `col` 必須包含 `col-12` 基礎以確保手機全寬。**

### RWD 注意事項

- 所有 `col` **必須**包含 `col-12` 基礎（mobile-first 全寬）
- 明細表格外層 **必須**包 `<div class="table-responsive">`，確保手機可橫向捲動
- 詳情頁頁頭使用 `flex flex-wrap`，避免按鈕擠成一團
- 同一列多個按鈕需加 `flex-wrap gap-2` 讓窄螢幕自動換行

| 頁面類型 | RWD 寬度 | 範例 |
|---|---|---|
| A. 簡單主檔 | `col-12 col-lg-8 col-xl-6` | department / job-title / permission / insurance-bracket / project |
| B. 複雜主檔 | `col-12 col-xl-8` | user / role / payroll |
| C. 申請表（無明細） | `col-12 col-lg-10 col-xl-8` | leave-request / overtime-request |
| C. 申請表（有明細） | `col-12 col-xl-10` | payment / travel / advance / write-off |
| D. 詳情頁 | `col-12 col-xl-10` | advance-detail / write-off-detail / write-off-overview |
| E. 審核頁 | `col-12 col-lg-10 col-xl-8` | approval-task-review |
| G. 設定頁 | `col-12 col-md-6 col-xl-4`（多欄並排） | settings |

### 頁頭結構

**主檔 / 申請表單（簡單型）：**

```html
<div class="flex items-center gap-2 mb-6">
  <a routerLink="/admin/<list>" class="btn btn-sm btn-outline-secondary">
    <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
  </a>
  <h4 class="mb-0">{{ title }}</h4>
</div>
```

**詳情頁 / 含狀態的頁面（含 badge + 操作按鈕）：**

```html
<div class="flex flex-wrap items-center justify-between gap-2 mb-6">
  <div class="flex items-center gap-2 flex-wrap">
    <a routerLink="/admin/<list>" class="btn btn-sm btn-outline-secondary">
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
    </a>
    <h4 class="mb-0">{{ title }} {{ requestNo }}</h4>
    <span [class]="'badge ' + statusClass">{{ statusLabel }}</span>
  </div>
  <div class="flex flex-wrap gap-2">
    <!-- 操作按鈕 -->
  </div>
</div>
```

### 錯誤訊息列

```html
@if (errorMsg()) {
  <div appScrollIntoView class="alert alert-danger flex items-center gap-2 mb-6 py-2" role="alert">
    <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
    {{ errorMsg() }}
  </div>
}
```

**`appScrollIntoView` 必加**（[shared/directives/scroll-into-view.directive.ts](../Admin/src/app/shared/directives/scroll-into-view.directive.ts)）：這個錯誤列固定顯示在頁面最上方，但送出/儲存按鈕通常在表單很下面；沒有這個指令，使用者捲到底點送出、驗證沒過時訊息跳在頂部使用者看不到，會誤以為「按了沒反應」（2026-07 數位研發部請款申請回報過此問題）。此指令掛載時自動 `scrollIntoView({behavior:'smooth', block:'start'})`，全站表單頁（不含列表頁）一律加上。

### 報表 / 列表搜尋列（Toolbar Filter Pattern）

> **單一真相來源**：所有報表（出缺勤紀錄、加班紀錄、款項統計、專案水位表…）與需要多條件篩選的列表頁，**搜尋列一律採用此緊湊單列樣式**。**禁止再使用** 雙列 grid + 欄位上方 label 的舊版佈局。

#### 結構規則

- 容器：`<div class="px-4 py-3 border-b">` 放在卡片 `card-body p-0` 內最上方
- 內層：**單一**橫列 `<div class="flex flex-wrap items-center gap-2">`，所有控件依「**篩選欄位 → 時段模式 pill → 日期/年月控件 → 篩選按鈕**」順序排列
- **不得**使用欄位上方 `<label>`、不得使用 grid 佈局、不得多列堆疊（週期區間提示除外，見下）
- 全部 select 用 `class="form-select"`、日期 input 用 `class="form-control"`，**統一**搭配 inline 寬度：

  ```html
  style="width: auto; min-width: 120px"   <!-- 一般 select（年份/月份/狀態）-->
  style="width: auto; min-width: 140px"   <!-- 中等 select（付款狀態類）-->
  style="width: auto; min-width: 160px"   <!-- 較長 select（員工/專案）-->
  style="width: auto; min-width: 150px"   <!-- 日期 input -->
  ```

- 篩選按鈕固定為 `<button class="btn btn-primary" (click)="search()">篩選</button>`，**不**加 `w-full`

#### 時段模式 pill（日 / 週 / 月）

```html
<div class="inline-flex border rounded-md overflow-hidden">
  <button type="button" class="px-3 py-1.5 text-sm"
          [class.bg-primary]="filterMode()==='day'" [class.text-white]="filterMode()==='day'"
          (click)="filterMode.set('day')">日</button>
  <button type="button" class="px-3 py-1.5 text-sm border-l border-r"
          [class.bg-primary]="filterMode()==='week'" [class.text-white]="filterMode()==='week'"
          (click)="filterMode.set('week')">週</button>
  <button type="button" class="px-3 py-1.5 text-sm"
          [class.bg-primary]="filterMode()==='month'" [class.text-white]="filterMode()==='month'"
          (click)="filterMode.set('month')">月</button>
</div>
```

- 不需要的頁面（如專案水位表）省略整個 pill
- **禁止**前面加「時段：」label

#### 週模式：左右翻頁 + 本週

```html
<button type="button" class="btn btn-outline px-2" title="上一週" (click)="shiftWeek(-7)">‹</button>
<input type="date" class="form-control" style="width: auto; min-width: 150px"
       [ngModel]="selectedWeekDate()" (ngModelChange)="selectedWeekDate.set($event)" />
<button type="button" class="btn btn-outline px-2" title="下一週" (click)="shiftWeek(7)">›</button>
<button type="button" class="btn btn-outline whitespace-nowrap" title="回到本週" (click)="resetToThisWeek()">本週</button>
```

#### 週次區間提示

放在主橫列**外**、`px-4 py-3 border-b` 容器內、僅 week 模式顯示：

```html
@if (filterMode() === 'week' && weekRange(); as r) {
  <div class="text-xs text-muted mt-2">第 {{ r.weekNumber }} 週（{{ r.dateFrom }} 一 ~ {{ r.dateTo }} 日）</div>
}
```

#### 完整骨架範例

```html
<div class="card border-0 shadow-sm">
  <div class="card-body p-0">

    <div class="px-4 py-3 border-b">
      <div class="flex flex-wrap items-center gap-2">
        <!-- 1. 頁面專屬篩選（員工 / 專案 / 狀態 …）-->
        <select class="form-select" style="width: auto; min-width: 160px"
                [ngModel]="selectedEmployeeId()" (ngModelChange)="selectedEmployeeId.set($event)">
          <option value="">全部員工</option>
          @for (emp of employees(); track emp.id) {
            <option [value]="emp.id">{{ emp.code }} {{ emp.name }}</option>
          }
        </select>

        <!-- 2. 時段模式 pill（不需要時可省略）-->
        <div class="inline-flex border rounded-md overflow-hidden">
          <button type="button" class="px-3 py-1.5 text-sm"
                  [class.bg-primary]="filterMode()==='day'" [class.text-white]="filterMode()==='day'"
                  (click)="filterMode.set('day')">日</button>
          <!-- 週 / 月 同上 -->
        </div>

        <!-- 3. 依 mode 顯示日期 / 年月 -->
        @if (filterMode() === 'day') {
          <input type="date" class="form-control" style="width: auto; min-width: 150px"
                 [ngModel]="selectedDate()" (ngModelChange)="selectedDate.set($event)" />
        } @else if (filterMode() === 'week') {
          <!-- 上週 / 日期 / 下週 / 本週 -->
        } @else {
          <select class="form-select" style="width: auto; min-width: 120px" ...>年份</select>
          <select class="form-select" style="width: auto; min-width: 120px" ...>月份</select>
        }

        <!-- 4. 篩選按鈕（最後）-->
        <button class="btn btn-primary" (click)="search()">篩選</button>
      </div>

      @if (filterMode() === 'week' && weekRange(); as r) {
        <div class="text-xs text-muted mt-2">第 {{ r.weekNumber }} 週（{{ r.dateFrom }} 一 ~ {{ r.dateTo }} 日）</div>
      }
    </div>

    <!-- 表格 / 分頁 -->
  </div>
</div>
```

#### 已套用此 pattern 的頁面

- [專案水位表](../Admin/src/app/features/admin/reports/pages/project-water-level/project-water-level.html) — 不需時段切換，最簡形式
- [出缺勤紀錄](../Admin/src/app/features/admin/reports/pages/attendance-report/attendance-report.html) — 員工 + 時段
- [加班紀錄](../Admin/src/app/features/admin/reports/pages/overtime-report/overtime-report.html) — 員工 + 專案 + 時段
- [款項統計](../Admin/src/app/features/admin/reports/pages/payment-report/payment-report.html) — 付款狀態 + 時段

> 新增報表 / 多條件列表頁時，**必須**先讀其中一份（推薦：加班紀錄，覆蓋最完整）作為範本，依此規範佈局，禁止自行設計 toolbar 樣式。

#### 權限差異化篩選控件（依部門 / 權限顯示）

同一列表的篩選控件可依使用者身分決定是否渲染 —— 以 `computed()` 判定後用 `@if` 包住該控件，**不要**改用 disabled 或隱藏 option：

```html
@if (canSeeApplicantFilter()) {
  <select class="form-select form-select-sm w-auto"
          [value]="submittedByFilter()"
          (change)="setSubmittedByFilter($any($event.target).value)"
          aria-label="篩選申請人">
    <option value="">全部申請人</option>
    @for (a of applicantOptions(); track a.id) { <option [value]="a.id">{{ a.name }}</option> }
  </select>
}
```

規則：

- 判定 signal 命名 `canSeeXxxFilter`，**必須**與後端同一份部門 / 權限判定同步（例：申請人下拉 ↔ `DepartmentCodes.FinancialAndAbove` ↔ `FINANCIAL_AND_ABOVE_DEPT_CODES`）
- 下拉選項來源若是受限端點（如 `/approval-tasks/applicants` 非財務體系回 403），**只在判定為 true 時才發請求**（`switchMap(allowed => allowed ? api : of([]))`），避免非授權者觸發 403
- 前端隱藏只是 UI，後端**必須**同時忽略或拒絕該篩選參數
- 已採用：[簽核作業列表](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-list/approval-task-list.html) 已核准頁籤 —— 全部類型下拉（所有人）＋ 申請人下拉 / 撥款退款子篩選（僅財務體系部門 / Superadmin）

---

## 4. 卡片元件

所有區塊用統一卡片樣式。**禁止自製 panel / box**。

### 標準卡片

```html
<div class="card border-0 shadow-sm mb-4">
  <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
    <svg class="sa-icon text-primary" style="stroke: currentColor">
      <use href="/assets/icons/sprite.svg#ICON"></use>
    </svg>
    卡片標題
  </div>
  <div class="card-body">
    <div class="row g-3">
      <!-- 表單欄位 / 內容 -->
    </div>
  </div>
</div>
```

關鍵：
- `border-0 shadow-sm` — 無邊框、淡陰影
- `card-header` 必含 icon（左）+ 標題（右），`fw-600`
- 連續多張卡片之間 `mb-4`，最後一張不加 margin
- 卡片內 row gutter `g-3`；外層 layout row gutter `g-4`

### 卡片分組與排序

**一般申請表單（payment / leave / travel / overtime / advance）：**

1. 狀態提示卡（條件式，唯讀時顯示）
2. 基本資訊卡（所有表單欄位 + 備註）
3. 明細表格卡（如有：發票/費用/預算）
4. **指定審核者卡（獨立卡片，icon `#users`）**
5. 簽核流程（`<app-approval-timeline>`）

**沖銷申請表單（write-off / travel-write-off）：**

1. 主單選擇卡
2. 上傳發票卡
3. 花費明細表格卡
4. 沖銷備註卡
5. **指定審核者卡（獨立卡片）**

> 指定審核者一律為**獨立卡片**，不得內嵌於其他卡片中。

**指定審核者共用元件（[`<app-designated-reviewers-picker>`](../Admin/src/app/shared/components/designated-reviewers-picker/designated-reviewers-picker.ts)）：**
- 一條流程可有**多個**「申請人指定審核」步驟；元件依流程的 designated steps（`useApplicantDesignated=true`）分組，每步一個區塊。
- Inputs：`designatedSteps`（含 `stepOrder` / `designatedRequiresDepartment`）、`users`（`UserLookup`，含 `departmentId` / `jobTitleId`）、`jobTitles`、`departments`、`initial`（編輯回填的 `DesignatedReviewer[]`，含 `approvalStepOrder` / `selectedDepartmentId`）。
- 每區塊可多列（可新增 / 刪除）：`designatedRequiresDepartment=false` → 「先選職稱→再選人」；`designatedRequiresDepartment=true` → 「先選部門→依部門篩人→選人」。
- Output `reviewersChange`：`DesignatedReviewerPayload[]`（`reviewerId` / `stepOrder` 列序 / `approvalStepOrder` 所屬步驟 / `selectedDepartmentId`）；**ngOnChanges 重建群組後會立即 emit**，確保編輯回填未互動也有 payload（送出 / 驗證才不會誤判為空）。**命名刻意避開原生 `change` 事件**（見 §17 命名規範說明），舊名 `change` 曾在 zoneless 全域事件代理下偶發收到原生 Event 物件導致 `TypeError`。
- Output `suppressedStepsChange`：`number[]`，回報被抑制（部門最高層級 → 自動略過）的指定步驟 `stepOrder`；父表單送出驗證時對這些步驟**不要求**審核者。
- **多步驟連動行為（三項）**：
  1. **連動閘控**：第一個指定步驟未選好前，其後步驟下拉 / 新增鈕 disabled（提示「請先完成第一個指定審核步驟」）。
  2. **部門帶入**（僅 `designatedRequiresDepartment=true`）：第一個步驟所選部門自動帶入其後步驟部門下拉；使用者手動改過的列（`deptManuallyChanged`）不覆寫。
  3. **部門最高層級自動略過**：第一個步驟（部門模式）首列選到「所選部門中 `UserLookup.jobTitleLevel` 最小」的人 → 其後步驟整組 disable + 顯示「已指定部門最高層級，後續指定審核步驟將自動略過」，且 `_buildPayload()` 不輸出被抑制步驟（後端為權威判定）。
- **9 種申請表單 + 預審申請共 10 個表單一律使用此共用元件**（不再各自實作）；`UserLookup` 需含 `jobTitleLevel`（由 `GET /users/lookup` 提供）。
- 父表單（範本 [payment-form](../Admin/src/app/features/admin/payment-requests/pages/payment-form/payment-form.ts)）以 `(reviewersChange)` 存 payload、`(suppressedStepsChange)` 存被抑制步驟；`_buildFormData()` 直接 `JSON.stringify` 進 `designatedReviewers` 欄位；送出驗證「每個 designated step 至少 1 位（被抑制者除外）」。

---

## 5. Tab UI（Pill Button Pattern）

**禁止使用 ng-bootstrap NgbNav**。一律使用 Tailwind pill button：

### 標準 Tab 結構

參考 [approval-task-list.html](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-list/approval-task-list.html) 與 [user-form.html:45-57](../Admin/src/app/features/admin/users/pages/user-form/user-form.html#L45-L57)：

```html
<div class="flex gap-1 mb-4">
  <button type="button" class="btn btn-sm"
          [class]="activeTab() === 'tab1' ? 'btn-primary' : 'btn-outline-secondary'"
          (click)="switchTab('tab1')">頁籤 1</button>
  <button type="button" class="btn btn-sm"
          [class]="activeTab() === 'tab2' ? 'btn-primary' : 'btn-outline-secondary'"
          (click)="switchTab('tab2')">頁籤 2</button>
</div>

@if (activeTab() === 'tab1') {
  <!-- Tab 1 內容 -->
} @else if (activeTab() === 'tab2') {
  <!-- Tab 2 內容 -->
}
```

### TS 端

```typescript
activeTab = signal<'tab1' | 'tab2'>('tab1');
switchTab(tab: 'tab1' | 'tab2') {
  this.activeTab.set(tab);
  // optional: lazy load
}
```

### 多 Tab 共用同一 form 的場景

當多 Tab 同屬一張表單（user-form 為例），三個 Tab 共用同一個 `<form>` 與單次 `(ngSubmit)`，切 tab 不丟資料：

```html
<form [formGroup]="form" (ngSubmit)="submit()">
  <!-- tab 切換列 -->
  <div class="flex gap-1 mb-4">...</div>

  @if (activeTab() === 'basic') { <!-- Tab 1 卡片 --> }
  @else if (activeTab() === 'hr') { <!-- Tab 2 卡片 --> }
  @else if (activeTab() === 'dependents') { <!-- Tab 3 卡片 --> }

  <!-- submit / cancel 按鈕 -->
</form>
```

---

## 6. 表單規範

### 欄位排版

```html
<div class="row g-3">
  <div class="col-12 col-md-6">
    <label class="form-label fw-500">姓名 <span class="text-danger">*</span></label>
    <input type="text" class="form-control" formControlName="name" placeholder="請輸入姓名">
    @if (form.get('name')?.invalid && form.get('name')?.touched) {
      <div class="text-danger small mt-1">請輸入姓名。</div>
    }
  </div>
</div>
```

關鍵：
- Label 用 `form-label fw-500`，必填以 `<span class="text-danger">*</span>` 標示
- 卡片內 row gutter `g-3`
- 欄位之間 `mb-4`，最後一個 `mb-0`
- 卡片之間 `mt-6`
- 錯誤訊息：`text-danger small mt-1`，僅當 `invalid && touched` 顯示

### 欄位寬度（col-md-N）

| 欄位類型 | 寬度 |
|---|---|
| 短文字（姓名 / Email） | `col-12 col-md-6` |
| 長文字（地址 / 備註） | `col-12` |
| 短數字（年齡 / 數量） | `col-12 col-md-3` |
| 中數字（金額） | `col-12 col-md-4` |
| 日期 / 下拉 | `col-12 col-md-6` |
| 三欄並排（如年/月/日） | `col-12 col-md-4` |

### 控件樣式

| 控件 | class |
|---|---|
| Input | `form-control` |
| Select | `form-select` |
| Textarea | `form-control`（自動多行） |
| Checkbox | `form-check-input`（外包 `form-check`） |
| Radio | `form-check-input`（外包 `form-check`，name 統一）|
| 小尺寸（明細表格內） | 加 `form-control-sm` / `form-select-sm` |

### Radio 群組範例

```html
<label class="form-label fw-500">角色 <span class="text-danger">*</span></label>
<div class="flex flex-wrap gap-4 mt-1">
  @for (role of roles(); track role.id) {
    <div class="form-check">
      <input class="form-check-input" type="radio"
             name="roleId"
             [id]="'role-' + role.id"
             [value]="role.id"
             formControlName="roleId">
      <label class="form-check-label" [for]="'role-' + role.id">{{ role.name }}</label>
    </div>
  }
</div>
```

### 條件式欄位

當欄位依其他欄位狀態決定是否顯示，使用 `@if` 控制流：

```html
@if (form.value.isIndigenous === true) {
  <div class="col-12">
    <label class="form-label fw-500">原住民證明文件</label>
    <!-- 上傳區塊 -->
  </div>
}
```

### 日期多選 chips（逐日勾選）

在有限日期區間內做**多選、可不連續**的日期勾選時（如假日執行活動的參與人員參與日期），以 `btn btn-sm rounded-pill` toggle 按鈕逐日產生 chips，不用多個 `<input type="date">`：

```html
<div class="flex flex-wrap gap-1 mt-2">
  @for (chip of dayChips(); track chip.date) {
    <button type="button" class="btn btn-sm rounded-pill"
            [class.btn-danger]="chip.isHoliday && isDateSelected(entry, chip.date)"
            [class.btn-outline-danger]="chip.isHoliday && !isDateSelected(entry, chip.date)"
            [class.btn-primary]="!chip.isHoliday && isDateSelected(entry, chip.date)"
            [class.btn-outline-secondary]="!chip.isHoliday && !isDateSelected(entry, chip.date)"
            (click)="toggleDate(entry, chip.date)">
      {{ chip.label }}@if (chip.isHoliday) { <span class="ms-1">假</span> }
    </button>
  }
</div>
```

關鍵：
- chips 由區間逐日產生（signal），label 格式 `M/d(週X)`；區間變更時重建並**剪除已勾選但落出新區間的日期**
- 特殊日（假日）用 danger 系列標示並附「假」字；一般日用 primary（選取）/ outline-secondary（未選）
- 「未勾選＝預設行為」（如全程參與）時，卡片上方需固定註記說明，每列附即時 summary（`text-muted small`，如「已選 N 天（假日 M 天）」）
- 防呆：區間超過 **92 天**時停用 chips 並顯示警告文字（`text-warning small`），視為預設行為
- 唯讀模式改為純文字顯示已勾日期清單（`M/d、M/d`）或預設行為文字

實例：`holiday-travel-request-form.html` 參與執行人員卡片。

---

## 7. 明細列表（FormArray）

明細列表（發票項目、費用明細、HR 多筆紀錄等）一律以 `<table>` + `FormArray` 實作。

### 7.1 表格結構

```html
<div class="table-responsive">
  <table class="table table-sm align-middle mb-0">
    <thead class="table-light">
      <tr>
        <th class="w-10">#</th>
        <th>欄位 1</th>
        <th>欄位 2</th>
        <th class="text-right w-12">金額</th>
        <th class="w-10"></th>  <!-- 刪除按鈕欄 -->
      </tr>
    </thead>
    <tbody>
      @for (item of itemArray.controls; track $index; let i = $index) {
        <tr [formGroup]="$any(item)">
          <td class="align-middle">{{ i + 1 }}</td>
          <td>
            <input type="text" class="form-control form-control-sm" formControlName="field1">
          </td>
          <td>
            <input type="text" class="form-control form-control-sm" formControlName="field2">
          </td>
          <td class="text-right">
            <input type="number" class="form-control form-control-sm text-right" formControlName="amount">
          </td>
          <td class="text-right align-middle">
            <button type="button" class="btn btn-sm btn-ghost-danger inline-flex items-center"
                    (click)="removeItem(i)">
              <svg class="sa-icon" style="stroke:currentColor">
                <use href="/assets/icons/sprite.svg#x"></use>
              </svg>
            </button>
          </td>
        </tr>
      } @empty {
        <tr>
          <td colspan="5" class="text-center text-muted py-4 small">尚無明細，請點擊「新增」</td>
        </tr>
      }
    </tbody>
  </table>
</div>

<button type="button" class="btn btn-sm btn-outline-primary inline-flex items-center mt-3"
        (click)="addItem()">
  <svg class="sa-icon me-1" style="stroke:currentColor">
    <use href="/assets/icons/sprite.svg#plus"></use>
  </svg>
  新增
</button>
```

### 7.1.1 分組欄（同組第二列起留白）

當明細分屬多個群組（分類、批次…）時，最左欄顯示群組名，**同組第二列起留白**，不用 `rowspan`（`rowspan` 與 `@for` 難以共存，且列數變動時易錯位）。判斷式一律做成 component method，不在 template 內比對前一列。

```html
<th style="width:130px">批次</th>
...
@for (item of r.items; track item.id; let i = $index) {
  <td class="small">
    @if (isFirstOfRound(r, i)) {
      <span class="fw-500">{{ roundLabel(item.roundNo) }}</span>
      @if (roundDate(r, item.roundNo); as d) {
        <div class="text-muted">{{ d | date:'yyyy-MM-dd' }}</div>
      }
    }
  </td>
```
```ts
isFirstOfRound(r: AdvanceRequest, index: number): boolean {
  return index === 0 || r.items[index - 1].roundNo !== r.items[index].roundNo;
}
```

**已採用**：預支申請「批次」欄（[advance-detail](../Admin/src/app/features/admin/advance-requests/pages/advance-detail/advance-detail.html) / [approval-task-review](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-review/approval-task-review.html) 預支明細 / [advance-pdf.service](../Admin/src/app/features/admin/advance-requests/services/advance-pdf.service.ts) 的 `bodyRows`）；PDF 的「分類」欄沿用同一慣例。

> **加欄位別忘 tfoot**：`<tfoot>` 合計列的 `colspan` 必須同步 +1；PDF 的 `autoTable` 則要同時改 `head`、`columnStyles` 各欄寬（總和維持 1.0）與合計列的 `colSpan`。
>
> **標籤文字做成單一真相**：批次標籤 `roundLabel(n)` 定義在 [advance-request.model.ts](../Admin/src/app/features/admin/advance-requests/models/advance-request.model.ts)，detail / form / PDF / 簽核作業頁 / approval-timeline 五處共用，不各寫一套。

### 7.2 ⚠ 刪除按鈕標準（**重要**）

> **2026-05-09 起，所有明細列表的刪除按鈕一律統一為以下 pattern**。
> 範例參考：[travel-payment-form.html:285-288](../Admin/src/app/features/admin/travel-payment-requests/pages/travel-payment-form/travel-payment-form.html#L285-L288)、[user-form.html](../Admin/src/app/features/admin/users/pages/user-form/user-form.html)（HR Tab 9 個明細）

```html
<button type="button" class="btn btn-sm btn-ghost-danger inline-flex items-center"
        (click)="removeXxx(i)">
  <svg class="sa-icon" style="stroke:currentColor">
    <use href="/assets/icons/sprite.svg#x"></use>
  </svg>
</button>
```

特徵：
- Class **必為** `btn btn-sm btn-ghost-danger inline-flex items-center`（**禁用** `btn-outline-danger`、`btn-danger`）
- Icon **必為** `sprite.svg#x`（**禁用** `#trash`、`#trash-2`、`#delete`）
- SVG 一律加 `style="stroke:currentColor"`，確保 ghost-danger 紅色生效
- **純 icon-only**（無文字），靠 `aria-label` 或 tooltip 提供無障礙語意（若需要）

### 7.3 與其他「刪除按鈕」的差異

| 用途 | 樣式 | 範例 |
|---|---|---|
| 明細列表 row 刪除（icon-only） | `btn-ghost-danger` + `#x` | 上方 7.2 |
| 上傳檔案區塊「刪除檔案」（文字按鈕） | `btn-outline-danger` + 純文字 | `<button class="btn btn-sm btn-outline-danger">刪除頭像</button>` |
| 列表頁刪除主檔（破壞性，需確認） | `btn-danger` + 圖示 + 文字 | 通常配合 confirm modal |

> **常見錯誤**：誤把明細刪除做成 `btn-outline-danger` + 文字「刪除」，視覺過於搶眼。明細刪除應**低調**（ghost）。

### 7.4 TS 端 FormArray pattern

參考 [payment-form.ts:128-260](../Admin/src/app/features/admin/payment-requests/pages/payment-form/payment-form.ts)：

```typescript
form = this.fb.group({
  // ... 其他欄位
  invoices: this.fb.array([]),
});

get invoiceArray(): FormArray { return this.form.get('invoices') as FormArray; }
get invoiceControls(): AbstractControl[] { return this.invoiceArray.controls; }

private _invoiceGroup(item?: { id?: string; field1: string; amount: number }) {
  return this.fb.group({
    id:     [item?.id ?? null],
    field1: [item?.field1 ?? '', Validators.required],
    amount: [item?.amount ?? 0,  Validators.min(0)],
  });
}

addItem() { this.invoiceArray.push(this._invoiceGroup()); }
removeItem(i: number) { this.invoiceArray.removeAt(i); }
```

### 7.5 載入後資料回填

```typescript
loadData(items: Item[]) {
  this.invoiceArray.clear();
  items.forEach(it => this.invoiceArray.push(this._invoiceGroup(it)));
}
```

### 7.6 撥款明細（InstallmentsTable 共用元件 + 編輯版）

4 種有撥款的申請類型（請款 / 預支 / 出差預支 / 出差請款）共用以下 UI 模式：

#### 唯讀顯示版本（[`<app-installments-table>`](../Admin/src/app/shared/components/installments-table.ts)）

申請表單頁、詳情頁、列表頁皆引用此共用元件。**樣式對齊其他 detail 卡片**：

```html
<div class="card border-0 shadow-sm mb-6">
  <div class="card-header bg-transparent border-bottom flex items-center justify-between gap-2 fw-600 flex-wrap">
    <div class="flex items-center gap-2 flex-wrap">
      <svg class="sa-icon text-primary"><use href="...#credit-card"></use></svg>
      撥款明細
      <span class="badge ..."><!-- Unpaid / PartiallyPaid / FullyPaid --></span>
      <span class="text-muted small">已撥 X / Y 期</span>
    </div>
    <div class="text-muted small">撥款總額：已撥 / 總額 元</div>
  </div>
  <div class="card-body p-0">
    <div class="table-responsive">
      <table class="table table-sm mb-0"><!-- thead.table-light --></table>
    </div>
  </div>
</div>
```

**欄位**：期數 / 預計撥款日 / 實際撥款日 / 金額 / 備註 / 狀態（badge bg-success「已撥」or bg-secondary「未撥」）。已撥列底色加深（`bg-[--bg-base]`）。

**呼叫端**（一律不需外層 `mb-*` wrapper，元件內已含 `mb-6`）：
```html
<app-installments-table
  [installmentsInput]="r.installments"
  [paymentStatus]="r.paymentStatus"
  [totalAmount]="r.totalAmount" />
```

#### 編輯版本（[`<app-installments-editor>`](../Admin/src/app/shared/components/installments-editor.ts)）

**2026-07 抽出為共用元件**（原本寫在 approval-task-review 內部）。抽離原因：預支沖銷簽核頁需要**同一頁放兩個編輯器**（本單的差額撥款 + 關聯預支單的撥款明細），單一 `installmentsForm` 無法支撐。

兩種 `mode`：

| mode | 使用時機 | 差異 |
|------|---------|------|
| `review` | 待審核（pending）財務簽核當下 | 無「實際撥款日」欄、無儲存鈕；由外層審核表單於送出時一併帶出（`viewChild` → `editor.value()` / `editor.valid()`） |
| `manage` | 已核准（approved）後回來管理 | 多「實際撥款日」欄與「儲存撥款明細」按鈕，`(save)` 輸出 `InstallmentInput[]` 由父層 dispatch 到對應 service |

主要 input：`totalAmount`（應撥總額）/ `installments` / `title` / `totalLabel` / `hint` / `required` / `statusLabel` / `statusClass` / `message` / `error`。

> **預支沖銷的 `totalAmount` 不是整單金額**，而是 `refundDue`＝本次沖銷造成的超支增額（後端 `WriteOffRefundCalculator` 算好帶回）。`refundDue = 0` 時整個撥款區塊不顯示。

UI 行為規範（5 種申請類型一致）：

| 元件 | 規則 |
|------|------|
| **「+ 新增一期」按鈕** | 僅 `SUM(已填金額) ≥ 申請總額` 時禁用。⚠️ **不可再加 `FullyPaid` 條件**（見下方警告） |
| **「儲存撥款明細」按鈕** | 僅 `SUM ≠ 申請總額` 時禁用。⚠️ 同上 |
| **金額 input** | `min="1" step="1"`（整數，≥ 1）；`[attr.max]="installmentRowMax(task, i)"` 動態 = 申請總額 − 其他列已填 |
| **預計撥款日 / 實際撥款日 / 金額 / 備註 input** | 已撥款列（`isInstallmentLocked(row)`）：`[attr.readonly]="true"` + `[class.bg-light]="true"` |
| **刪除按鈕（⨯）** | 已撥款列：完全隱藏；只剩 1 列時也隱藏 |
| **剩餘額度 hint** | 在標題列顯示「剩餘 X 元」，即時反映 `申請總額 − installmentsSum()` |

**元件內部 helpers 命名**：
- `canAddRow()` — 是否可新增
- `sumValid()` — SUM 是否等於應撥總額
- `rowMax(index)` — 單列金額 max（剩餘額度）
- `isLocked(row)` — 列是否已鎖定

**父層取值 API**（`review` mode 由外層表單送出時用）：`value()` / `valid()` / `sum()` / `markAllAsTouched()`。

> ⚠️ **禁止用 `paymentStatus === 'FullyPaid'` 當禁用條件**（2026-07 移除）：**追加預支**核准後申請總額會變大，原本 FullyPaid 的單必須能補一期把新增金額排入。若沿用 FullyPaid 鎖定，財務端湊不到 `SUM == 總額`，而後端在財務步驟又強制必填撥款明細 → **整張單卡死無法核准**。已撥款列的保護改由 `isInstallmentLocked(row)`（欄位 readonly + 隱藏刪除鈕）與後端 `InstallmentValidator` 負責，兩者已足夠。

> 後端 `InstallmentValidator.Validate` 提供等同的伺服端防線（序號連續、SUM == 總額、已撥款列不可刪不可改），前端為 UX，後端為強制驗證。

### 7.7 沖銷金額摘要（[`<app-write-off-summary>`](../Admin/src/app/shared/components/write-off-summary.ts)）

預支沖銷的「沖銷資訊」卡片金額區，**詳情頁與簽核頁共用同一份呈現**（2026-07 取代原本兩處各自硬寫的 4 格摘要）。

版面：左右兩張小表 + 下方餘額列。

| 區塊 | 內容 |
|------|------|
| 左：**預支批次** | 第 1 次 / 第 2 次…（`AdvanceRequestItem.RoundNo`，標籤共用 [`roundLabel()`](../Admin/src/app/features/admin/advance-requests/models/advance-request.model.ts)）+ 預支日期 + 金額，footer 為「預支加總」 |
| 右：**已沖銷** | 第 1/2/3… 次沖銷（`WriteOffRecord.WriteOffNo`）+ 單號 + 金額，本單標 `本單` badge 並套底色，footer 為「已沖銷加總」 |
| 下：**餘額列** | 本次沖銷 / 待沖銷餘額（負數為 `text-red`）/ 本次應撥差額（`refundDue > 0` 才顯示） |

Input：`advanceRounds` / `writeOffHistory` / `currentGrandTotal` / `refundDue`。加總一律由陣列 `reduce` 推導，不吃任何快取欄位。

### 7.8 支票已支付欄（預支沖銷明細）

支票由公司**直接付給廠商**，不是撥給員工的錢，因此不進撥款分期，改以沖銷明細的勾選註記。

- **簽核頁整欄僅財務體系 / Superadmin 可見**（`canSeeCheckPaid` computed；`thead` / `tbody` / `tfoot` 三處同時 `@if` 包起來，非唯讀而是整欄不渲染）
- 簽核頁（財務體系 / Superadmin，單子 pending 或 approved）：checkbox，變更即呼叫 `PATCH /write-off-requests/{id}/check-payments`，樂觀更新不重載整頁
- 簽核頁（財務體系但單子非 pending / approved）/ 詳情頁：唯讀顯示 `✓`（`title` 帶勾選日期與勾選人）或 `—`
- `checkAmount === 0` 的列一律顯示 `—` 且不可勾（後端同步擋下）
- 表尾統計：`已支付 N / M 筆`

> ⚠️ 沖銷單被**退回修改**後申請人重填明細時，`UpdateAsync` 會整批取代 items，支票支付註記將被清空。實務上退回發生在財務核准前，可接受。

### 7.9 金額三欄連動（總價 = 現金 + 支票）

**預支沖銷申請**（[write-off-form](../Admin/src/app/features/admin/write-off-requests/pages/write-off-form/write-off-form.ts) 實際花費明細）與**預支申請**（[advance-form](../Admin/src/app/features/admin/advance-requests/pages/advance-form/advance-form.ts) 預算明細，含追加批次）的明細，**總價 = 現金 + 支票**，三欄輸入其中兩欄自動算出第三欄（`onAmountInput(ctrl, 'total' | 'cash' | 'check')` 綁在三個 input 的 `(input)`）。兩表單共用同一套規則，改一邊須同步改另一邊。

推算哪一欄取決於該列的總價是否**已確立**（`_pinnedTotals: Set<列 id>`）：

| 總價狀態 | 何時進入 | 輸入現金 | 輸入支票 |
|---|---|---|---|
| **已確立** | 單價×數量（`calcTotal` → `setTotal`）/ 手動輸入總價 / OCR 帶入（僅沖銷）/ 編輯・追加模式回填（`_itemGroup` 中 `totalPrice > 0`） | 推算支票 = 總價 − 現金 | 推算現金 = 總價 − 支票 |
| **未確立** | 「手動新增行」/「新增項目」的空白列 | 推算總價 = 現金 + 支票 | 同左 |

- **總價不會被現金 / 支票反推變小**（已確立時），否則單價×數量 算出的總價會被打壞
- 單價 / 數量 變動時**保留支票金額**，差額由現金吸收（支票為 0 時 ＝ 現金等於總價，與舊行為相同）
- 推算值一律 `Math.max(0, …)` 不為負；被 0 截斷時（如支票 > 總價）以 `amountWarnings`（`Map<列 id, string>`，比照 §12.5c 的 `invoiceWarnings` 樣式與刪列清理）顯示紅字提示，**僅提示、不阻擋送出**
- 所有推算寫入用 `setValue(v, {emitEvent: false})`，避免連鎖觸發
- 表頭下方固定一行 `text-muted small` 說明：「總價 = 現金 + 支票，任兩欄輸入後自動算出第三欄」（預支申請的唯讀模式 `isReadOnly` 不顯示）

### 7.10 清單分組母層 + 彙總頁（預支沖銷）

[write-off-list](../Admin/src/app/features/admin/write-off-requests/pages/write-off-list/write-off-list.html) 依 `advanceRequestId` 把同一張預支單的沖銷單 group 起來：**單筆直接畫一般列，≥ 2 筆才畫母層摘要列**（預設收合，點列切換展開；子列以 `border-l-2 border-[var(--forest)]` 標示縮排）。

母層列的規則：

- **操作欄有「檢視」**（icon `#eye`），連到彙總頁 `['by-advance', advanceRequestId]`；因為整列本身是 toggle，連結必須 `(click)="$event.stopPropagation()"`，否則點檢視會順帶收合群組
- 母層列不可用 `colspan` 蓋掉尾端欄位——尾欄要放操作鈕時，中間空白欄逐格補 `<td>`，且 `hidden lg:table-cell` 的欄位（如建立時間）空白 td 要**帶上同一組 class**，否則窄螢幕欄數對不齊
- 母層金額欄顯示「已沖銷 / 預支總額」，狀態 / 時間欄留白（群組沒有單一狀態）

彙總頁（[write-off-overview](../Admin/src/app/features/admin/write-off-requests/pages/write-off-overview/)）走**詳情頁版型**（`col-12 col-xl-10`）：預支資訊卡 → 沖銷單一覽表卡 → 預支費用明細卡 → `<app-installments-table>`（預支撥款）→ 逐張沖銷單卡（明細 / 附件 / 該次差額撥款 `<app-installments-table>`）。共用元件維持 card 結構，故一律放在卡片**外層同級**，不塞進 `card-body`（避免卡中卡）。

---

## 8. 按鈕規範

### 8.1 顏色語意

| 用途 | Class | 範例 |
|---|---|---|
| 主要動作（送出 / 建立 / 更新） | `btn btn-primary` | 「建立」「更新」「送出申請」 |
| 次要動作（取消 / 返回） | `btn btn-outline-secondary` | 「取消」「返回列表」 |
| 危險動作（刪除主檔） | `btn btn-danger` | 「刪除員工」 |
| 明細刪除（icon-only） | `btn btn-ghost-danger inline-flex items-center` | FormArray row 移除 |
| 文字刪除（檔案上傳區塊） | `btn btn-outline-danger` | 「刪除頭像」 |
| 補強提示動作 | `btn btn-outline-info` | 「寄出帳號通知」 |
| 編輯（warning 語意） | `btn btn-warning` | 「編輯」 |

### 8.2 按鈕尺寸

| 場景 | Class |
|---|---|
| 預設大小（表單底部主按鈕） | （不加 size class）|
| 卡片內次要按鈕 | `btn-sm` |
| 表格 row 內按鈕 | `btn-sm` |
| 大型 Hero CTA | `btn-lg`（少用）|

### 8.3 主要按鈕位置

**主檔表單底部：**

```html
<div class="mt-6 flex gap-2">
  <button type="submit" class="btn btn-primary">{{ isEdit ? '更新' : '建立' }}</button>
  <a routerLink="/admin/<list>" class="btn btn-outline-secondary">取消</a>
</div>
```

**申請表單底部（編輯模式）：**

```html
<div class="mt-6 flex gap-2">
  <button type="submit" class="btn btn-outline-secondary">{{ isEdit ? '儲存' : '儲存草稿' }}</button>
  <button type="button" class="btn btn-primary" (click)="submitForApproval()">送出申請</button>
  <a routerLink="/admin/<list>" class="btn btn-outline-secondary">取消</a>
</div>
```

**申請表單底部（唯讀模式）：**

```html
<div class="mt-6">
  <a routerLink="/admin/<list>" class="btn btn-outline-secondary">返回列表</a>
</div>
```

### 8.4 載入中狀態

按鈕呼叫非同步動作時，使用 `[disabled]` + spinner：

```html
<button type="button" class="btn btn-primary"
        [disabled]="submitting()"
        (click)="submit()">
  @if (submitting()) {
    <span class="inline-block w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin me-1"></span>
    處理中…
  } @else {
    送出
  }
</button>
```

---

## 9. 狀態提示卡

申請類頁面在送出後或唯讀模式顯示。**使用 `@if/@else if` 鏈式**，不用 `@switch`。

| 狀態 | 背景色 | 文字色 | Icon |
|---|---|---|---|
| pending | `bg-[rgba(13,110,253,0.08)]` | `text-primary` | `#clock` |
| returned | `bg-[rgba(255,193,7,0.08)]` | `text-warning` | `#alert-triangle` |
| approved | `bg-[rgba(37,162,68,0.08)]` | `text-success` | `#check-circle` |
| rejected | `bg-[rgba(220,53,69,0.08)]` | `text-danger` | `#x-circle` |

文案統一：「此申請{狀態描述}，不可再修改。」

範例：

```html
@if (status === 'pending') {
  <div class="card border-0 shadow-sm mb-4 bg-[rgba(13,110,253,0.08)]">
    <div class="card-body flex items-center gap-2 text-primary">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#clock"></use></svg>
      此申請審核中，不可再修改。
    </div>
  </div>
} @else if (status === 'approved') {
  <!-- ... -->
}
```

### 9.1 列表雙徽章模式（審核狀態 + 業務狀態）

列表頁的「簽核狀態」欄當需要同時呈現**審核進度**與**後續業務進度**時，採雙徽章排版：

- 左：審核狀態徽章（沿用 `APPROVAL_STATUS_CLASSES`）
- 右：業務狀態徽章（透過 component method 計算，只在特定 `approvalStatus` 下出現）

包在 `<div class="flex flex-wrap items-center gap-1">` 中，窄螢幕會自動換行；不顯示第二徽章時版面不變。

**已採用範例：**

| 列表 | 業務狀態 | 觸發條件 |
|---|---|---|
| [advance-list](../Admin/src/app/features/admin/advance-requests/pages/advance-list/advance-list.html) | `已結案` | `isClosed === true` |
| [advance-list](../Admin/src/app/features/admin/advance-requests/pages/advance-list/advance-list.html)（單號欄） | `含追加` | `currentRoundNo > 1`；不在狀態欄，直接跟在單號後（`ms-1`、`font-size:.7rem`） |
| [payment-list](../Admin/src/app/features/admin/payment-requests/pages/payment-list/payment-list.html) | `待撥款` / `已撥款` | `approvalStatus ∈ {pending, approved}`，再依 `paidAt` 是否填入決定 |
| [approval-task-list](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-list/approval-task-list.html) | `待撥款` / `已撥款` | `status ∈ {pending, approved}`，per-type 取 `paymentDetail.paidAt / advanceDetail.paidAt / …`；write_off / travel_write_off 僅超支才顯示；leave / overtime / holiday_travel 永不顯示 |

業務狀態徽章建議色：

| 語意 | class |
|---|---|
| 進行中 / 待處理 | `bg-warning-subtle text-warning-emphasis` |
| 完成 / 已結束 | `bg-primary-subtle text-primary`（CIS 森林綠，與 success 綠形成深淺差別） |
| 中性附註 | `bg-secondary-subtle text-secondary` |

### 9.2 跨列表一致性原則（**重要**）

**同一筆資料在不同列表頁出現時，審核狀態 + 業務狀態徽章的 label / CSS / status gate 必須完全一致**，否則使用者會看到同一筆東西呈現不同文字或顏色。

**Single Source of Truth 規則：**

1. **審核狀態 mapping**：只在 [payment-request.model.ts](../Admin/src/app/features/admin/payment-requests/models/payment-request.model.ts) 定義 `APPROVAL_STATUS_LABELS` / `APPROVAL_STATUS_CLASSES`。其他 feature model（如 [approval-task.model.ts](../Admin/src/app/features/admin/approval-tasks/models/approval-task.model.ts) 的 `TASK_STATUS_LABELS / TASK_STATUS_CLASSES`）以**直接賦值 re-export** 共用，禁止重複定義。
2. **業務狀態 mapping**（撥款 / 退款 / 結案 etc.）：同上，在來源 feature model 定義 `XXX_STATE_LABELS` / `XXX_STATE_CLASSES`，其他列表 re-export 使用。例如 `PAYMENT_STATE_LABELS` / `PAYMENT_STATE_CLASSES` 來自 payment-request.model。
3. **status gate（顯示時機）**：跨列表顯示同一筆資料的業務狀態徽章時，gate 條件必須一致。例如撥款徽章在 `payment-list` 是 `approvalStatus ∈ {pending, approved}`；簽核作業列表也須同樣 gate（即使 `pending` 在「待審核 tab」也要顯示「待撥款」黃徽章）。
4. **per-type 業務規則例外**：簽核作業列表因彙整多種申請類型，可保留 per-type 業務條件（如 write_off / travel_write_off 需「超支」才顯示徽章、holiday_travel 永不顯示等），但**徽章本身的 label/CSS 仍套用共用 mapping**。

**Code Review 檢查點：**

- [ ] 新增列表頁顯示審核狀態徽章 → 是否從 `payment-request.model` import `APPROVAL_STATUS_LABELS / CLASSES`？（禁止自行另寫一份）
- [ ] 新增「待撥款 / 已撥款」徽章 → 是否從 `payment-request.model` import `PAYMENT_STATE_LABELS / CLASSES`？
- [ ] 同一筆資料的 status gate 條件是否與既有列表一致？

---

## 10. Icon 系統

### 10.1 SVG Sprite

所有 icon 統一從 `/assets/icons/sprite.svg` 引用。**禁止**直接內嵌 SVG path、引入 Font Awesome / Material Icons / Lucide。

```html
<svg class="sa-icon"><use href="/assets/icons/sprite.svg#NAME"></use></svg>
```

### 10.2 sa-icon 樣式

```css
/* 預設於 tailwind.css */
.sa-icon {
  width: 1em;
  height: 1em;
  stroke-width: 1.75;
  fill: none;
  stroke: currentColor;
}
```

實際渲染色彩跟隨 `currentColor`（即父元素的 `color`）。當父元素未控制顏色（例如 `btn-ghost-danger` 在 hover 才變色），明確 inline `style="stroke:currentColor"` 確保兼容。

### 10.3 常用 Icon 對照表

| 用途 | sprite name |
|---|---|
| 返回 / 上一頁 | `arrow-left` |
| 新增 | `plus` |
| 刪除（明細）| `x` |
| 編輯 | `edit-3` 或 `pencil` |
| 儲存 | `save` |
| 上傳 | `upload` |
| 下載 | `download` |
| 列印 | `printer` |
| 發送 / Email | `mail` |
| 警告 | `alert-triangle` |
| 錯誤 | `x-circle` |
| 成功 | `check-circle` |
| 資訊 | `info` |
| 鐘 / 等待 | `clock` |
| 使用者 | `user` |
| 多人 | `users` |
| 部門 / 公司 | `briefcase` 或 `building` |
| 文件 | `file-text` |
| 信用卡 / ID | `credit-card` |
| 地圖 | `map-pin` |
| 相機 | `camera` |
| 設定 | `settings` |
| 搜尋 | `search` |
| 篩選 | `filter` |

> 加新 icon 前先檢查 sprite.svg 是否已存在；不在則從 [Feather Icons](https://feathericons.com/) 抓 SVG 加入 sprite。

---

## 11. 通知（Toastr）

統一使用 `ngx-toastr`，**不得自製 alert / modal 替代**。

### 11.1 注入

```typescript
private toastr = inject(ToastrService);
```

### 11.2 使用

| 類型 | API | 用途 |
|---|---|---|
| 成功 | `toastr.success(msg)` | 「儲存成功」「已送出」 |
| 錯誤 | `toastr.error(msg)` | 「儲存失敗，請稍後再試」 |
| 警告 | `toastr.warning(msg)` | 「人事資料儲存失敗，基本資料已更新」 |
| 資訊 | `toastr.info(msg)` | 一般提示 |

### 11.3 訊息文案

- 結尾用「。」
- 錯誤訊息應給出**可行動建議**：「請稍後再試」「請聯絡管理員」
- 不要洩漏技術細節（HTTP status / SQL error 等）

---

## 11.5 Quick-Add Modal Pattern（下拉旁即時新增）

當下拉選單（lookup endpoint）對應的主檔資料**可能不齊全**，且使用者在當下表單就會需要新增（例如請款表單選不到廠商）時，採用此 pattern：在下拉旁加「+ 新增 XXX」按鈕，開啟 NgbModal 收集主檔欄位，儲存成功後**自動回填**到下拉。

### 11.5.1 已採用的 Quick-Add

| 父表單 | 主檔 | 元件 |
|---|---|---|
| `payment-form` 廠商請款 | `Vendor` | `vendors/components/vendor-quick-add-modal/` |

### 11.5.2 元件結構

- 獨立 standalone Component（**不與 routed form 共用**），檔名 `<resource>-quick-add-modal.{ts,html}`
- 注入 `NgbActiveModal`，由父元件透過 `NgbModal.open(MyModal, {...})` 開啟
- 表單欄位精簡：只收**最必要**的主檔欄位（其他可選欄位之後到管理頁編輯）
- `@Input() prefillName?: string`（選用）：父元件可把使用者於下拉輸入的字串帶入名稱欄
- `submit()` 成功 → `activeModal.close(<LookupDto>)`，失敗 → 顯示 `errorMsg` 並保留表單
- `cancel()` → `activeModal.dismiss()`

### 11.5.3 父元件接收

```typescript
openQuickAddVendor() {
  const ref = this.modal.open(VendorQuickAddModal, {
    centered: true, backdrop: 'static', keyboard: false, size: 'lg',
  });
  ref.closed.subscribe((newVendor: VendorLookup | undefined) => {
    if (!newVendor) return;
    this.vendors.update(list =>
      [...list, newVendor].sort((a, b) => a.name.localeCompare(b.name, 'zh-Hant'))
    );
    this.form.get('vendorId')!.setValue(newVendor.id);
  });
}
```

### 11.5.4 觸發按鈕樣式

下拉選單與按鈕並排（`flex gap-2`）：

```html
<div class="flex gap-2">
  <select class="form-select" formControlName="vendorId">…</select>
  <button type="button"
          class="btn btn-outline-primary inline-flex items-center gap-1 whitespace-nowrap"
          (click)="openQuickAddVendor()">
    <svg class="sa-icon"><use href="/assets/icons/sprite.svg#plus"></use></svg>
    新增廠商
  </button>
</div>
```

### 11.5.5 後端權限取捨

Quick-add 的 `POST /<resource>` 端點通常會把 admin CRUD 權限（如 `vendors:write`）強加給請款人。**判斷規則**：

- 若主檔對「資料品質」要求高（如部門 / 角色） → 維持 `:write` 權限門檻、按鈕用 `*appHasPermission` 隱藏
- 若主檔本來就由請款人實務上產生（如廠商） → POST 端點 `null`（任何登入者皆可），**所有列表/編輯 / 刪除仍要權限**

> 詳見 [docs/backend-design.md §13 輕量讀取端點模式](backend-design.md#13-輕量讀取端點模式lightweight-lookup-pattern)。

---

## 12. 檔案上傳規範

### 12.1 標準上傳區塊

```html
<div class="col-12">
  <label class="form-label fw-500">上傳檔案</label>
  @if (filePreview()) {
    <div class="mb-3">
      <img [src]="filePreview()" alt="預覽" class="max-h-48 rounded border">
    </div>
    <button type="button" class="btn btn-sm btn-outline-danger" (click)="onRemoveFile()">刪除檔案</button>
  } @else {
    <input type="file" class="form-control" accept="image/*,.pdf,.heic,.heif"
           (change)="onFileSelected($event)">
    <div class="text-muted small mt-1">支援 JPG / PNG / HEIC / PDF，最大 1 MB</div>
  }
</div>
```

### 12.2 檔案壓縮（必要）

**所有圖檔上傳一律走** [image-compression.service.ts](../Admin/src/app/shared/services/image-compression.service.ts)：

```typescript
private imageCompression = inject(ImageCompressionService);

async onFileSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;

  const compressed = await this.imageCompression.compress(file, {
    maxSize: 1600,    // 證件 / 證明文件用 1600；avatar 用 800
    quality: 0.85,
  });

  if (compressed.size > 1024 * 1024) {
    this.toastr.error('上傳照片勿超過1MB');
    return;
  }

  this.fileSignal.set(compressed);
  // 產生本地預覽
  const reader = new FileReader();
  reader.onload = () => this.previewSignal.set(reader.result as string);
  reader.readAsDataURL(compressed);
}
```

行為：
- PDF（mime/副檔名）→ 直接回原檔不處理
- HEIC / HEIF → 走 heic2any 轉 JPEG
- 其餘圖檔 → Canvas 等比縮放至 maxSize × maxSize 內 + 輸出 JPEG quality

### 12.3 大小規範

| 檔案類型 | 上限 | maxSize 建議 |
|---|---|---|
| Avatar（頭像） | 1 MB | 800 |
| 簽名檔 | 1 MB | 800 |
| 證明文件（原住民 / 低收入 / 殘障 / 身分證 / 最高學歷） | 1 MB | 1600 |
| 發票 | 1 MB | 1600 |
| 整單批次附件（請款（廠商 / 一般）/ 預支沖銷） | 10 MB（後端安全網） | 1600（圖片自動壓縮，PDF 不壓） |

### 12.4 file proxy 端點

需 `users:read` 權限的敏感 PII（HR 證明文件）一律走授權代理：

| 端點 | 容器 |
|---|---|
| `/files/indigenous-proofs/{fileName}` | indigenous-proofs |
| `/files/low-income-proofs/{fileName}` | low-income-proofs |
| `/files/disabled-proofs/{fileName}` | disabled-proofs |
| `/files/id-cards/{fileName}` | id-cards |
| `/files/education-proofs/{fileName}` | education-proofs |

不敏感者（簽名、頭像）走公開路由：`/files/signatures/{fileName}` / `/files/avatars/{fileName}`。

廠商存摺封面（`/files/vendor-passbooks/{fileName}` → `vendor-passbooks` 容器）為**一般檔，需 JWT 但免特殊權限**：透過 `HttpClient` 走 Blob 代理（auth interceptor 自動附 Bearer），與 PII 同樣以 `URL.createObjectURL` 在新分頁開啟。

報價單（`quotes`）與整單批次附件（`request-attachments`）同為**一般檔，需 JWT 但免特殊權限**，但 blob name 含日期子路徑（`yyyy/MM/{guid}{ext}`），代理路由為 `/files/quotes/{*path}` / `/files/request-attachments/{*path}`（多段）。DB 存的是原始私有 blob URL，前端取用前**一律先過** [`resolveFileProxyUrl()`](../Admin/src/app/shared/services/pdf-core.service.ts)（把原始 blob URL 轉成代理路徑），再經 HttpClient（帶 JWT）下載。

> **鐵則：需 JWT 的檔案不可直接放 `<img [src]>` 或 `<iframe [src]>`。** `<img>` / `<a href>` / `<iframe>` 無法帶 Authorization header，會 401 破圖。
> - **公開容器**（signatures / avatars）→ 直接 `<img [src]="apiUrl + '/files/...'">`。
> - **需 token 的容器**（PII、vendor-passbooks、quotes、request-attachments、以及員工自助 `/me/files/...`）→ 一律 `HttpClient` 下載 Blob（interceptor 帶 token）→ `URL.createObjectURL` 設給 `<img>` / `<iframe>` 或 `window.open` 開新分頁。
> 員工「個人資訊」唯讀頁 [my-profile](../Admin/src/app/features/account/pages/my-profile/) 即依此規則：簽名 / 頭像走公開 `/files/`，身分證 / 學歷 / 三證明走 `/me/files/` blob 下載。
>
> **`FilePreviewModal` 預覽私有檔案**：modal 的 iframe / img 同樣不帶 JWT，故報價單 / 整單附件的「檢視」一律改用共用 [`FilePreviewLoader`](../Admin/src/app/shared/services/file-preview-loader.ts)（`resolveFileProxyUrl` → `HttpClient` 取 blob → `createObjectURL` → 回傳 `PreviewFileData`，關閉時 `revoke`），**不可**把原始 blob URL 直接丟進 modal。**歷史教訓（2026-06）**：預審 PDF 合併上傳檔曾因直接 `fetch()` 私有 blob URL 而 403 / CORS 靜默失敗（檔案沒被合併進去）；詳情頁 / 簽核頁的預覽亦同病，皆改走代理修正。

### 12.5b 整單批次附件（共用元件）

請款（廠商請款 / 一般請款）/ 預支沖銷表單明細下方支援批次上傳照片或文件，使用兩個共用元件：

- [`<app-attachments-upload>`](../Admin/src/app/shared/components/attachments-upload.ts)（可編輯）：`[existing]` 帶入既有附件、`[disabled]` 控制唯讀；內部自管新增 / 既有 / 刪除狀態，圖片以 `ImageCompressionService`（maxSize 1600）壓縮。父表單透過 `viewChild(AttachmentsUpload)` 取得實例，於 `_buildFormData()` 呼叫 `getMeta()`（JSON → `attachments` 欄位）與 `getNewFiles()`（檔案 → `attachmentFiles` 欄位）。請款兩種 type（vendor / general）皆帶附件。
- [`<app-attachments-list>`](../Admin/src/app/shared/components/attachments-list.ts)（唯讀）：`[attachments]` 帶入；用於申請詳情頁與簽核審核頁，逐項顯示檔名 + 檢視。附件存於私有 `request-attachments` 容器，「檢視」透過 [`FilePreviewLoader`](../Admin/src/app/shared/services/file-preview-loader.ts) 走 JWT 代理抓 blob 後再交 `FilePreviewModal`（見 §12.4 鐵則）。

### 12.5c 發票 OCR 上傳（一檔可展開多列）

請款 / 預審申請 / 預支沖銷 / 出差請款 / 出差預支沖銷五個表單的明細，上傳發票 / 票根時走 OCR 自動辨識帶入欄位。共用 [`PaymentRequestService.ocrInvoice`](../Admin/src/app/features/admin/payment-requests/services/payment-request.service.ts)（`POST /invoice-ocr`，後端 Google Gemini；預審申請走 `QuoteOcrHandler` 的 `quoteOcr`）。

`onFilesSelected` 流程（五個表單同一 pattern）：

1. 多選檔案 → 逐檔 `_convertHeicIfNeeded`（iPhone HEIC/HEIF → JPEG）。
2. 每檔先 push **一列 loading placeholder**（`ocrLoadingIds.add(id)` + `fileMap.set(id, file)` + `URL.createObjectURL` 預覽），即時回饋。
3. `Promise.all` 並行呼叫 `ocrInvoice`，**回傳為陣列**（一張圖可含多張發票/票根）：
   - 第 1 筆 patch 進 placeholder 那列；第 2..N 筆**各 push 一新列**，新列產生新 `id`、`fileMap.set(newId, file)` **指向同一個 File 物件**（→ 存檔時各 append 一份複本，N 列各存一份檔案）。
   - 陣列為空（沒辨識到）→ placeholder 留空供手動輸入。
4. `docType==='ticket'` 時各表單套用各自規則（`note='票號'`、出差請款另帶 `category='交通費'`）；金額帶入 `unitPrice/totalPrice`（+ 預支沖銷 `cashAmount`）、`quantity='1式'`。
5. `isAnyOcrPending` 控制送出按鈕禁用，存檔組 FormData 的逐列 `fileMap.get(id)` → `append('files')` 迴圈不變。**OCR 呼叫務必加前端逾時**（`.pipe(timeout(45000))`，略大於後端 Gemini 30 秒上限），否則請求卡住時 `ocrLoadingIds` 永不清除、`isAnyOcrPending` 恆為 true，會把「送出 / 儲存」按鈕**永久鎖住**且畫面無提示。同時 OCR 進行中**須在按鈕區顯示「辨識中…請稍候」hint**（避免使用者誤以為按鈕壞掉）。
6. **OCR 結算後（成功或失敗皆同）該列必須立即 `markAllAsTouched()`**（`finally` 區塊，依 `id` 找回該列 `FormGroup`；多列展開時每個新 push 的列也各自呼叫一次）。原因：`invoiceNo`/`itemName` 等必填欄位的紅框顯示條件是 `control.invalid && control.touched`，若 OCR 沒辨識到值導致欄位留白，使用者從未手動點過該欄位就不會顯示 `touched`，欄位不會顯示紅框、按鈕卻已因 `form.invalid` 鎖住——使用者完全看不出原因。五個共用此 OCR pattern 的表單（請款 / 預審申請 / 預支沖銷 / 出差請款 / 出差預支沖銷）皆須套用此規則，不可只修其中一個。
7. **買方抬頭/統編驗證**：OCR 結果含 `buyerName`/`buyerTaxId`，填值後對 `docType==='invoice'` 的列呼叫共用工具 [`validateInvoiceBuyer`](../Admin/src/app/shared/utils/invoice-buyer-validator.ts) 比對公司白名單（5 組抬頭＋統編）。**抬頭與統編需皆讀得到才判斷，任一缺漏即跳過不驗**（收銀機 / 二聯式 / 手寫讀不全）。不符時 `invoiceWarnings.set(rowId, msg)`（`invoiceWarnings = new Map<string,string>()`，key = 列 id），刪列時一併 `delete`。**警告僅顯示、不阻擋送出、不持久化**。警告列以 `<span class="inline-flex items-center gap-1">` 包 `sa-icon sa-icon-1x`（alert-triangle）＋訊息，**icon 與文字同一行**。

> 多張擠在一張照片時辨識準確度較低，各列辨識後仍需人工核對（欄位皆可手動修改）。

#### 明細列下方警告列 pattern

在 `@for` 的明細 `<tr>` **之後**，插入一條條件式警告列（沿用既有錯誤樣式 `text-danger small` + `alert-triangle` icon），`colspan` 取該表實際欄數（請款 7 / 出差請款 11 / 預支沖銷 12 / 出差預支沖銷 10）；有 readonly 模式者加 `!isReadOnly` 守衛：

```html
</tr>
@if (invoiceWarnings.has(ctrl.get('id')?.value)) {
  <tr>
    <td colspan="<欄數>" class="text-danger small py-1 ps-3 border-0">
      <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
      {{ invoiceWarnings.get(ctrl.get('id')?.value) }}
    </td>
  </tr>
}
```

### 12.5 外部 API 即時查詢欄位（blur 觸發 pattern）

某些欄位（如統編 → 廠商名稱）可在 blur 時打 API 自動帶入相關欄位，提升輸入速度。標準作法：

```typescript
onTaxIdBlur() {
  const taxIdCtrl = this.form.controls.taxId;
  const taxId     = (taxIdCtrl.value ?? '').trim();
  if (!taxId || taxIdCtrl.invalid) return;     // 格式不符不發 API
  if (this.looking()) return;                  // 防止重複觸發

  this.looking.set(true);
  this.vendorService.lookupByTaxId(taxId).subscribe({
    next: result => {
      this.looking.set(false);
      // 只填空欄位，避免覆寫使用者已輸入內容
      const patch: any = {};
      if (!this.form.controls.name.value)    patch.name    = result.name;
      if (!this.form.controls.address.value) patch.address = result.address;
      Object.keys(patch).length === 0
        ? this.toastr.info('已查到資料，但欄位皆已填寫，未覆寫。')
        : (this.form.patchValue(patch), this.toastr.success('已自動帶入'));
    },
    error: (err: HttpErrorResponse) => {
      this.looking.set(false);
      err.status === 404
        ? this.toastr.info('查無資料，請手動填寫')
        : this.toastr.error('查詢失敗，請稍後再試');
    },
  });
}
```

原則：
- 先驗證格式（pattern validator）通過再打 API，避免無效查詢
- 同一輪查詢進行中以 signal 旗標擋住重複觸發
- **不覆寫**使用者已填寫欄位（patch 前檢查 value）
- 三態 toast：成功 / 查無資料 / 系統錯誤

---

## 13. 路由與 Lazy Loading

所有 feature 在 `app.routes.ts` 用 `loadComponent` / `loadChildren` lazy load：

```typescript
{
  path: 'admin/users',
  canActivate: [authGuard, permissionGuard],
  data: { permission: 'users:read' },
  loadChildren: () => import('./features/admin/users/users.routes').then(m => m.routes),
},
```

### Feature 目錄結構

每個 feature 一律三層：

```
features/admin/<feature>/
├── models/        # interface / enum / 常數
├── pages/         # component（含 .ts / .html / .scss optional）
└── services/      # HTTP service / 業務 helper
```

---

## 14. 服務層 / HTTP

### 14.1 Component 禁止直接注入 HttpClient

所有 HTTP 一律封裝於 `features/<feature>/services/<feature>.service.ts`：

```typescript
@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/users`;

  getAll(): Observable<User[]> {
    return this.http.get<ApiResponse<User[]>>(this.base).pipe(map(r => r.data));
  }
  // ...
}
```

### 14.2 ApiResponse 解包

所有 API 回應格式為 `ApiResponse<T> = { success, message, data }`，由 `core/auth/interceptors/api-response.interceptor.ts` 集中解包；service 直接 return data。

### 14.3 multipart/form-data

帶檔案的 API 一律用 FormData：

```typescript
upsert(userId: string, payload: ProfileRequest, files: { idCardFront?: File | null; ... }) {
  const fd = new FormData();
  fd.append('payload', JSON.stringify(payload));
  if (files.idCardFront) fd.append('idCardFront', files.idCardFront);
  if (files.removeIdCardFront) fd.append('removeIdCardFront', 'true');
  return this.http.put<ApiResponse<ProfileDetail>>(`${this.base}/${userId}/profile`, fd);
}
```

---

## 15. State Management

### 15.1 Signal-only

Component 內部狀態 **一律使用 Signal**：

```typescript
loading = signal(false);
items   = signal<Item[]>([]);
error   = signal<string | null>(null);

readonly itemCount = computed(() => this.items().length);
```

### 15.2 禁止 BehaviorSubject 管 component state

只有跨多 component 的 reactive stream 可使用 RxJS Subject（少見）。

### 15.3 跨 component 狀態

跨 feature 共享狀態（如 AuthService）放在 `core/`，用 Signal + 公開 readonly 訪問：

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private _user = signal<User | null>(null);
  readonly user = this._user.asReadonly();
  readonly isSuperAdmin = computed(() => this._user()?.isSuperAdmin === true);

  setUser(u: User) { this._user.set(u); }
}
```

### 15.4 背景輪詢 + Toast Pattern（鈴鐺通知）

需要「準即時」更新但不導入 WebSocket / SignalR 時，用 root service 定時輪詢更新 signal（畫面不刷新），並在偵測到變化時跳 toast。範本：`NotificationService`（`features/admin/notifications/services/notification.service.ts`）。

- **輪詢**：`rxjs` `timer(0, INTERVAL_MS).subscribe(...)`，由畫面殼層（`MainLayout`）`ngOnInit` 啟動、`ngOnDestroy` 取消（`startPolling()` / `stopPolling()`）。
- **省請求**：每 tick 先判 `if (document.hidden) return;` 跳過發送；監聽 `visibilitychange`，切回前景立即補抓一次。
- **Toast 去重**：service 內保留比對基準（`private prevXxx` / `localStorage`），**首次** refresh 只設基準不跳 toast；toast 邏輯統一寫在 `refresh()` 的 `tap` 內，使輪詢 / 開 dropdown / 自送單後共用同一比對而天然去重。
- 間隔常數抽成 module 級 `const`（如 `POLL_INTERVAL_MS = 60_000`），勿散落魔術數字。

---

## 16. 控制流（@if / @for / @switch）

**禁止** `*ngIf` / `*ngFor` / `*ngSwitch`。一律使用 Angular 17+ 內建控制流：

| 結構指令 | 控制流 |
|---|---|
| `*ngIf="cond"` | `@if (cond) { ... }` |
| `*ngIf; else` | `@if (cond) { ... } @else { ... }` |
| `*ngFor="let x of list"` | `@for (x of list; track x.id) { ... } @empty { ... }` |
| `*ngSwitch` | `@switch (value) { @case ('a') { ... } @default { ... } }` |

### track 必填

`@for` **必須**指定 `track`：物件用 `track item.id`，純值用 `track $index`。

---

## 17. 命名規範

| 對象 | 規則 | 範例 |
|---|---|---|
| TypeScript 變數 / 函式 | camelCase | `getUserList`, `isActive` |
| Class 名 | PascalCase | `UserForm`, `EmployeeProfileService` |
| Interface | PascalCase（不加 `I` 前綴） | `User`, `EmployeeProfile` |
| 檔名 | kebab-case | `user-form.ts`, `employee-profile.service.ts` |
| CSS class | kebab-case | `card-header`, `btn-ghost-danger` |
| Sprite icon name | kebab-case | `arrow-left`, `check-circle` |
| 自訂元件 `@Output()` 名稱 | **禁止**與原生 DOM 事件同名（`change`/`click`/`input`/`focus`/`blur`/`submit`…） | `reviewersChange`（不可用 `change`） |

> **為何 `@Output()` 不能同名原生事件**：本專案用 `provideZonelessChangeDetection()`（見 [app.config.ts](../Admin/src/app/app.config.ts)）。曾發生 [`designated-reviewers-picker`](../Admin/src/app/shared/components/designated-reviewers-picker/designated-reviewers-picker.ts) 的 `@Output() change` 與內部原生 `<select>` 的 `change` 事件同名，zoneless 全域事件代理下父層 `(change)="onPickerChange($event)"` 偶發收到原生 `Event` 物件而非元件真正 emit 的資料，造成 `TypeError: xxx.some is not a function`。修正方式：改名為 `reviewersChange`（或依語意加動詞尾綴，如 `selectionChange`、`valueChange`），10 個共用此元件的申請表單 binding 需同步改名。**新元件命名 `@Output()` 時一律避開原生事件名稱**，不可只用單一表單驗證過就視為安全。

### Component 三件組

| 檔案 | 寫法 |
|---|---|
| TypeScript | `<feature>-<form\|list\|detail>.ts`（class `XxxForm` / `XxxList`） |
| Template | `<同名>.html` |
| SCSS（可選） | `<同名>.scss` — 僅當 Tailwind utility 不夠時 |

---

## 18. 連結引用（Markdown）

template / 文件中引用其他檔案時，使用相對路徑 markdown link：

```markdown
- 檔案：[user-form.ts](../Admin/src/app/features/admin/users/pages/user-form/user-form.ts)
- 行號：[user-form.ts:217](../Admin/src/app/features/admin/users/pages/user-form/user-form.ts#L217)
- 行範圍：[user-form.ts:217-253](../Admin/src/app/features/admin/users/pages/user-form/user-form.ts#L217-L253)
- 目錄：[users/](../Admin/src/app/features/admin/users/)
```

**禁止** 使用 backtick `` `code` `` 或 HTML tag 包檔名。

---

## 19. 一致性 Checklist（Code Review 用）

新增頁面 / PR 提交前自我檢查：

### 結構
- [ ] 使用 Standalone Component（`standalone: true`）
- [ ] 三層目錄 `models/` `pages/` `services/`
- [ ] 路由用 `loadComponent` / `loadChildren` lazy load

### 狀態 / 服務
- [ ] Component 用 `signal()` 不用 `BehaviorSubject`
- [ ] 用 `inject()` 注入，不用 constructor injection
- [ ] HTTP 封裝在 service，component 不注入 `HttpClient`

### 樣式
- [ ] 所有 utility 來自 Tailwind 或 `@layer components`
- [ ] **未引入** Bootstrap、Font Awesome、Material UI 等
- [ ] icon 用 `<svg class="sa-icon">` + sprite，未內嵌路徑
- [ ] 通知用 `ngx-toastr`，未自製 alert / modal

### 排版
- [ ] 容器寬度遵循 §3 表格
- [ ] 卡片用 `card border-0 shadow-sm`，header 含 icon + 標題
- [ ] 卡片之間 `mb-4`，最後一張無 margin
- [ ] 控制流用 `@if` / `@for`，未用 `*ngIf` / `*ngFor`
- [ ] `@for` 都加 `track`
- [ ] 報表 / 多條件列表頁的搜尋列遵循 §3「報表 / 列表搜尋列（Toolbar Filter Pattern）」：單列 `flex flex-wrap`、無欄位 label、inline 寬度 select、`btn-primary` 篩選按鈕

### 表單
- [ ] Label 用 `form-label fw-500`，必填加紅星
- [ ] 錯誤訊息僅在 `invalid && touched` 時顯示
- [ ] FormArray 明細的刪除按鈕用 §7.2 標準（`btn-ghost-danger` + `#x`）
- [ ] 頁首 `errorMsg` 錯誤列加 `appScrollIntoView`（§6「錯誤訊息列」），驗證失敗才不會跳訊息在使用者看不到的地方

### 檔案上傳
- [ ] 圖檔走 `imageCompression.compress()` 壓縮
- [ ] Size 上限 1 MB，超過時 toastr.error 提示
- [ ] HR 敏感 PII 走授權 file proxy

### 命名
- [ ] 檔名 kebab-case，class PascalCase
- [ ] interface 不加 `I` 前綴
