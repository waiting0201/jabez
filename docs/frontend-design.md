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
| Excel 匯出 | SheetJS (`xlsx`) | 一律**前端產檔**（`XLSX.writeFile()`），後端只回 JSON、不設 Content-Disposition |
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
- 表格外層 **必須**包 `<div class="table-responsive">`（`overflow-x: auto`），**且 `<table>` 必須另給 `min-w-[…]`**。
  只包 wrapper 不會捲動 —— `.table` 本身是 `width: 100%`，永遠塞得進容器，`overflow-x` 從不觸發，欄位只會被壓扁
- 詳情頁頁頭使用 `flex flex-wrap`，避免按鈕擠成一團
- 同一列多個按鈕需加 `flex-wrap gap-2` 讓窄螢幕自動換行
- 篩選列控制項在手機給 `w-full sm:w-auto`（或 `flex-1 sm:flex-none`），避免 inline `style="width:auto"` 硬擠成一團
- 分頁列一律用標準 pattern：外層 `flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between`，
  手機 `flex sm:hidden` 的 `‹ N / M ›` 簡化 pager + 桌機 `hidden sm:flex pagination` 完整頁碼列。
  頁碼列表走各清單頁檔案內的 `buildPageNumbers(current, total)`（`-1` = 省略號）。
  **注意 `page-link` 的 `.disabled` 只是樣式、不會擋 click**，換頁方法必須自行夾住邊界

### 欄位多的清單：兩種選擇

| 方式 | 寫法 | 適用 |
|---|---|---|
| A. 逐欄隱藏 | `<th>` / `<td>` 掛 `hidden md:table-cell` / `hidden lg:table-cell` / `hidden xl:table-cell` | 次要欄位在手機可以捨棄（user / vendor / payment 等清單） |
| B. 橫向捲動 | `<table class="table table-hover table-sticky-first mb-0 min-w-[…]">` | 每一欄手機也都要看得到（出缺勤紀錄） |

`.table-sticky-first`（定義於 [tailwind.css](../Admin/src/tailwind.css) `@layer components` 的 Tables 區塊）把**第一欄釘在左側**，
橫向捲動到右半邊時仍看得出這列是誰。內部切 `border-collapse: separate`（collapse 下 sticky 儲存格邊框不會繪製），
並補上 sticky 儲存格的 hover 底色（不透明背景會蓋掉 `.table-hover` 的整列變色）。

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

#### 關鍵字搜尋框

需要關鍵字搜尋的列表頁（專案管理、廠商管理…），搜尋框放在同一橫列的**篩選按鈕之前**，樣式固定如下：搜尋 icon 疊在 input 內左側、input 加 `ps-9` 讓出 icon 空間、支援 Enter 送出：

```html
<div class="flex items-center gap-2" style="max-width: 480px">
  <div class="relative flex-1">
    <svg class="sa-icon absolute left-3 top-1/2 -translate-y-1/2 text-muted" style="stroke: currentColor; width: 16px; height: 16px">
      <use href="/assets/icons/sprite.svg#search"></use>
    </svg>
    <input type="text" class="form-control ps-9" placeholder="搜尋廠商名稱／統編／…"
           [(ngModel)]="searchInput" (keydown.enter)="doSearch()">
  </div>
  <button class="btn btn-primary" (click)="doSearch()">篩選</button>
</div>
```

- placeholder 一律列出**實際會被比對的欄位**，以全形頓號「／」分隔，結尾加刪節號
- 關鍵字**送到後端**做 SQL `LIKE` 比對（不在前端 filter），輸入值 `searchInput` 與已送出的 `searchTerm` signal 分離，按下「篩選」／Enter 才同步
- 查無資料時空狀態文案要區分「查無符合「{{ searchTerm() }}」的…」與「尚無…資料。」

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
- [廠商管理](../Admin/src/app/features/admin/vendors/pages/vendor-list/vendor-list.html) — 只有關鍵字搜尋框的最簡形式（搜尋 icon 疊在 input 內、`(keydown.enter)` 送出）+ 分頁

> 新增報表 / 多條件列表頁時，**必須**先讀其中一份（推薦：加班紀錄，覆蓋最完整）作為範本，依此規範佈局，禁止自行設計 toolbar 樣式。

### 空值欄位的顯示（`—`）

清單 / 詳情頁顯示「可能沒有值」的欄位時，一律以 `|| '—'` 遞補，**不要**讓儲存格空白 —— 空白看起來像壞掉或載入失敗：

```html
<td class="font-monospace small">{{ r.requestNo || '—' }}</td>
```

**已套用：申請單號**。單號自 2026-09 起改於**送簽時**才產生（見
[backend-design.md §4.5](backend-design.md)），草稿的 `requestNo` 為 `null`，故 8 種申請的
model 皆宣告為 `requestNo: string | null`：

- **清單頁**單號欄 → `{{ r.requestNo || '—' }}`
- **詳情頁**標題 → `{{ r.requestNo || '（送簽後取號）' }}`（標題位置需要說明狀態，比破折號清楚）
- **PDF service** → 內文 `${r.requestNo || '—'}`；**檔名以 id 遞補**（`經費預支申請表-${r.requestNo || r.id}.pdf`），
  否則草稿匯出會產生 `經費預支申請表-.pdf` 並互相覆蓋
- 只服務「已送簽單」的畫面（簽核作業列表 / 詳情、款項統計報表、母單下拉選項）**維持非空型別**，不加 fallback

### 列表分頁（Pagination）

> **單一真相來源**：需要分頁的列表頁（專案管理、廠商管理…）一律採下列結構，**禁止**自行設計分頁列或改用前端切片。

- **後端分頁**：呼叫 `service.getPaged(page, pageSize, …)`，回 `PagedResult<T>`（[shared/models/paged-result.model.ts](../Admin/src/app/shared/models/paged-result.model.ts)）。每頁筆數以元件常數 `readonly PAGE_SIZE = 20` 宣告
- **狀態組合**：`page` / 篩選條件 / `refresh` 皆為 signal，用 `toObservable(computed(() => ({...})))` + `switchMap` 串成單一 `toSignal` 結果，再 `computed` 拆出 `items` / `totalCount` / `totalPages`
- 按「篩選」時 **必須** `this.page.set(1)`，否則會停在超出範圍的頁碼看到空清單
- 刪除當頁最後一筆且非第 1 頁時退回前一頁（`if (items().length === 1 && page() > 1) page.update(p => p - 1)`），避免停在空白頁
- 頁碼省略以 `buildPageNumbers(current, total)` 產生（`-1` 代表 `…`），總頁數 ≤ 9 時全列
- **版面**：分頁列放在 `card-body` 內、表格 `table-responsive` 之後，`@if (totalPages() > 1)` 才顯示；手機顯示 `‹ n / m ›` 精簡版，`sm:` 以上顯示完整頁碼 `ul.pagination`

```html
@if (totalPages() > 1) {
  <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between px-4 py-3 border-t">
    <span class="text-muted small text-center sm:text-left">共 {{ totalCount() }} 筆，第 {{ page() }} / {{ totalPages() }} 頁</span>
    <div class="flex sm:hidden items-center gap-1">…精簡版…</div>
    <ul class="hidden sm:flex pagination mb-0">…頁碼…</ul>
  </div>
}
```

已套用：[專案管理](../Admin/src/app/features/admin/projects/pages/project-list/project-list.html)、[廠商管理](../Admin/src/app/features/admin/vendors/pages/vendor-list/vendor-list.html)（推薦以廠商管理為範本，最精簡）。

**報表頁簡化版**：報表頁（出缺勤 / 加班 / 款項統計）改用「共 N 筆，第 x / y 頁 + 上一頁 / 下一頁」兩顆按鈕的簡化列，
狀態以 `currentPage` / `totalCount` / `totalPages` 三個 signal + `goToPage()` 表達（篩選走使用者主動點「篩選」，
不走 `toObservable` + `switchMap` 串流）。同樣遵守「按篩選必須 `currentPage.set(1)`」。
已套用：[加班紀錄](../Admin/src/app/features/admin/reports/pages/overtime-report/overtime-report.html)（範本）、
[出缺勤紀錄](../Admin/src/app/features/admin/reports/pages/attendance-report/attendance-report.html)。

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
- **篩選列與頁籤的關係**：與狀態無關的篩選（類型、申請人）應**各頁籤常駐**；只有在特定狀態才有意義的篩選（如撥款 / 退款）才綁該頁籤。判斷基準是「這個篩選在這個狀態下篩得出東西嗎」，不是「當初是在哪個頁籤做的」。
- 已採用：[簽核作業列表](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-list/approval-task-list.html) —— 全部類型下拉（所有人）＋ 申請人下拉（僅財務體系部門 / Superadmin）**5 個頁籤常駐**；撥款退款子篩選另加 `activeTab() === 'approved'` 條件（其他狀態的單尚未進入撥款階段）

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
- Inputs：`designatedSteps`（含 `stepOrder` / `designatedRequiresDepartment` / **`designatedJobTitleIds`**）、`users`（`UserLookup`，含 `departmentId` / `jobTitleId`）、`jobTitles`、`departments`、`initial`（編輯回填的 `DesignatedReviewer[]`，含 `approvalStepOrder` / `selectedDepartmentId`）。
- 每區塊可多列（可新增 / 刪除），人員候選有三種模式：
  1. `designatedRequiresDepartment=true` → 「先選部門→依部門篩人→選人」
  2. `designatedRequiresDepartment=false` 且**無**限定職稱 → 「先選職稱→再選人」
  3. **有限定職稱**（`designatedJobTitleIds` 非空，例外指定審核專用）→ 人員一律限縮在這些職稱內；部門模式為「該部門 ∩ 限定職稱」，非部門模式則**隱藏職稱下拉**、直接列全公司符合職稱者，且候選為空時顯示「查無符合限定職稱的人員」
- **人員候選單一真相**：`PickerEntry.candidateUsers`（原 `filteredUsers` / `departmentFilteredUsers` 兩欄已合併），由 `_candidatesFor(group, entry)` 計算、`_refreshEntry(group, entry, mode)` 寫入；`mode='reset'`＝使用者主動改條件則清空已選人員，`mode='keep'`＝程式帶入（回填 / 部門自動帶入）僅在落選時清空。新增過濾條件時只改 `_candidatesFor` 一處。
- Output `reviewersChange`：`DesignatedReviewerPayload[]`（`reviewerId` / `stepOrder` 列序 / `approvalStepOrder` 所屬步驟 / `selectedDepartmentId`）；**ngOnChanges 重建群組後會立即 emit**，確保編輯回填未互動也有 payload（送出 / 驗證才不會誤判為空）。**命名刻意避開原生 `change` 事件**（見 §17 命名規範說明），舊名 `change` 曾在 zoneless 全域事件代理下偶發收到原生 Event 物件導致 `TypeError`。
- Output `suppressedStepsChange`：`number[]`，回報被抑制（部門最高層級 → 自動略過）的指定步驟 `stepOrder`；父表單送出驗證時對這些步驟**不要求**審核者。
- **多步驟連動行為（三項）**：
  1. **連動閘控**：第一個指定步驟未選好前，其後步驟下拉 / 新增鈕 disabled（提示「請先完成第一個指定審核步驟」）。
  2. **部門帶入**（僅 `designatedRequiresDepartment=true`）：第一個步驟所選部門自動帶入其後步驟部門下拉；使用者手動改過的列（`deptManuallyChanged`）不覆寫。
  3. **部門最高層級自動略過**：第一個步驟（部門模式）首列選到「所選部門中 `UserLookup.jobTitleLevel` 最小」的人 → 其後步驟整組 disable + 顯示「已指定部門最高層級，後續指定審核步驟將自動略過」，且 `_buildPayload()` 不輸出被抑制步驟（後端為權威判定）。
- **9 種申請表單 + 預審申請共 10 個表單一律使用此共用元件**（不再各自實作）；`UserLookup` 需含 `jobTitleLevel`（由 `GET /users/lookup` 提供）。
- 父表單（範本 [payment-form](../Admin/src/app/features/admin/payment-requests/pages/payment-form/payment-form.ts)）以 `(reviewersChange)` 存 payload、`(suppressedStepsChange)` 存被抑制步驟；`_buildFormData()` 直接 `JSON.stringify` 進 `designatedReviewers` 欄位；送出驗證「每個 designated step 至少 1 位（被抑制者除外）」。

**簽核流程時間軸共用元件（[`<app-approval-timeline>`](../Admin/src/app/shared/components/approval-timeline.ts)）：**
- Inputs：`flow` / `approvalRecords` / `currentStepOrder` / `status` / `currentRoundNo`（追加預支批次）/ **`stepReviewers`**。
- **關卡名稱一律帶人名**：`上層級：張三（發展三部 · 專案經理）`、`指定審核：李四（…）、王五（…）`、`總監（總監室）：蔡志堅（…）`。「上層級」「指定審核」本身不是人名，不接人名就完全看不出誰要簽。多人以「、」相接，升級指派掛紫色「升級審核」badge；該關無人可簽則接紅字「：查無可簽核人員」。
- 每個關卡四種狀態，缺一就會讓人看不出「誰簽過、這關輪到誰」：
  1. **已簽核** —— 綠 ✓ / 紅 ✗ 圓圈 + 審核者姓名、代理 / 升級審核 badge、時間（到秒）、結果與簽核意見；關卡名稱**不**重複列人名（下方已是實際簽核者）。
  2. **審核中** —— 藍色圓圈 + 「審核中…」；該關無人可簽時再補一行紅字「這一關沒有人簽得到，請聯絡管理員調整簽核流程或人員職稱」。
  3. **已跳過** —— 序號小於目前關卡卻無簽核紀錄者（送單時被跳過不留 `ApprovalRecord`），灰字「已跳過」，與「尚未輪到」區分；關卡名稱**不**列人名（這張單不會再回頭走）。
  4. **尚未輪到** —— 灰圈 + 關卡名稱 + 人名（誰會簽後面幾關，送出當下就看得到）。
- `stepReviewers` 來源一律是 `ApprovalTask.stepReviewers`（後端 `GET /approval-tasks/{appType}/{id}` 於 pending 時逐關解析），**前端不自行推算誰能簽**；detail 頁綁 `task.stepReviewers ?? []`，form 頁存在 `stepReviewers` 欄位。後端沒帶回時（非 pending 單）一律不顯示人名，也不誤報「查無可簽核人員」。
- ⚠ [approval-task-review](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-review/approval-task-review.html) 另有一份**內嵌**時間軸（審核頁需與審核表單同頁佈局），改動時**兩處必須同步**。

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

### 主頁籤 + 子狀態列（兩層 pill）

當某個頁籤本身是一個「範圍」，範圍內還要再切狀態時，用**兩層 pill** 表達層級，不要把子狀態攤平成更多主頁籤（主頁籤列會爆量，且「範圍」與「狀態」兩個維度混在同一列會讓使用者以為它們互斥）。

- **主層**：實心 `btn-primary`（選中）/ `btn-outline-secondary`（未選）—— 即上方標準結構
- **子層**：外框 `btn-outline-primary`（選中）/ `btn-outline-secondary`（未選），置於主頁籤列下方、篩選列同一行

```html
@if (activeTab() === 'director') {
  <div class="flex gap-1">
    <button class="btn btn-sm"
            [class]="directorStatus() === 'pending' ? 'btn-outline-primary' : 'btn-outline-secondary'"
            (click)="setDirectorStatus('pending')">
      <svg class="sa-icon me-1" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#clock"></use></svg>
      待簽核
    </button>
    <!-- … 其餘子狀態同樣結構，icon 要嘛全給要嘛全不給 -->
  </div>
}
```

TS 端**兩個維度各一個 signal**（不要合併成複合字串），切主頁籤時重置子狀態、切子狀態時只回第一頁：

```typescript
type TaskTab = 'pending' | 'approved' | 'rejected' | 'returned' | 'director';
type DirectorStatus = 'pending' | 'approved' | 'returned' | 'rejected';

activeTab      = signal<TaskTab>('pending');
directorStatus = signal<DirectorStatus>('pending');

switchTab(tab: TaskTab) {
  this.activeTab.set(tab);
  this.directorStatus.set('pending');   // 主頁籤切換 → 子狀態回預設
  /* … 其餘篩選一併重置 … */
}
setDirectorStatus(s: DirectorStatus) {
  this.directorStatus.set(s);
  this.page.set(1);                     // 子狀態切換只回第一頁，保留類型 / 申請人篩選
}
```

送 API 時在 `switchMap` 內把兩個維度拆成兩個 query param（`scope` + `status`），**不要組成 `director_approved` 這種複合字串** —— 後端會被迫用前綴判斷，且該參數若同時拿去和 DB 欄位等值比對就會靜默回空。

已採用：[approval-task-list](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-list/) 的「總監室簽核」頁籤（四種子狀態）。

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

> ⚠ **選項數量不可控時一律改用 `form-select` 下拉，不可平鋪 radio**：radio 群組只適用於選項固定且少量（角色、是 / 否、類型切換）的情境。若選項來自資料庫且筆數會成長（如「全部未結案專案」可達數百筆），平鋪會撐爆版面 —— 改用下拉；需要多選時改為[明細列表（FormArray）](#7-明細列表formarray)，每列一個下拉。加班申請的關聯專案即由 radio 平鋪改為此形式。

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

### 日期欄位回填（`<input type="date">`，**重要**）

後端 DTO 的日期欄位一律是 `DateTime?`，`Api/Program.cs` 未掛任何 date-only 轉換器，序列化出來是 **`"2026-03-24T00:00:00"`（含時間）**；而 `<input type="date">` **只接受 `yyyy-MM-dd`**，收到帶時間的字串會被瀏覽器判為非法值 → **輸入框顯示空白**（但 FormControl 內部仍是原字串，使用者會誤以為資料掉了）。

編輯模式回填日期一律用字串切割：

```ts
// ✅ 唯一寫法（全站 30+ 處慣例）
startDate:   r.startDate?.toString().slice(0, 10) ?? '',
invoiceDate: item.invoiceDate?.toString().slice(0, 10) ?? '',
```

**禁止**：

```ts
// ❌ toISOString() 會轉 UTC，台北 +8 的午夜被換算成前一日 → 日期少一天
startDate: new Date(r.startDate).toISOString().split('T')[0],

// ❌ instanceof Date 三元式：model 常誤標成 Date，runtime 實際是 string，
//    永遠走 else 分支，沒切時間就空白
startDate: r.startDate instanceof Date ? r.startDate.toISOString().split('T')[0] : String(r.startDate),
```

model 的日期欄位型別**標 `string` 不標 `Date`**（與 runtime 一致），避免又寫出 `instanceof Date` 分支。若同一個 model 同時用於讀取與送出而型別衝突，拆成 `XxxRequest`（讀）與 `XxxRequestPayload`（送）兩個介面（見 `overtime-request.model.ts`）。

**送出端同理**：直接送表單的 `yyyy-MM-dd` 字串，不要包成 `new Date(...)`。後端 `DateTime` / `DateTime?` 可正常解析純日期字串（已實測：送 `"2026-03-24"` → DB 存 `2026-03-24T00:00:00`，無位移），包成 `Date` 反而多一層 UTC 轉換風險。

同一條規則也適用於**用日期組其他字串**的場合（如出缺勤報表把 `recordDate` 併上時分秒送出）：

```ts
// ❌ "2026-08-12T00:00:00" 無時區標記 → 以本地時間解析 → toISOString 轉 UTC → 退回前一天
const dateStr = new Date(record.rawRecordDate).toISOString().substring(0, 10);
// ✅
const dateStr = record.rawRecordDate.slice(0, 10);
```

> 歷史：commit `7ce86bcb`（生日／到職日／離職日時區偏移少一天）確立不得經 `Date` 物件轉換；後續預支沖銷 / 出差預支沖銷 / 出差請款三張單因漏做截斷，導致單據退回修正後重新編輯時發票日期與出差起訖日全空白。

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

實例：`leave-revocation-form.html` 逐日銷假 chips（單純兩態 toggle）。

**變體：chip 四態循環（帶時段）** — 每個勾選日除了「選 / 不選」還要再分時段時（如假日執行活動的參與人員可指定全天 / 上半天 / 下半天），**不新增下拉或額外按鈕**，改讓同一顆 chip 循環：**未選 → 全天 → 上午 → 下午 → 未選**。

```html
<button type="button" class="btn btn-sm rounded-pill"
        [class.btn-danger]="chip.isHoliday && isDateSelected(entry, chip.date)"
        [class.btn-outline-danger]="chip.isHoliday && !isDateSelected(entry, chip.date)"
        [class.btn-primary]="!chip.isHoliday && isDateSelected(entry, chip.date)"
        [class.btn-outline-secondary]="!chip.isHoliday && !isDateSelected(entry, chip.date)"
        [attr.aria-label]="chip.label + ' ' + (slotOf(entry, chip.date) ? slotLabel[slotOf(entry, chip.date)!] : '未選')"
        (click)="cycleDate(entry, chip.date)">
  {{ chip.label }}@if (chip.isHoliday) { <span class="ms-1">假</span> }
  @if (slotOf(entry, chip.date); as slot) {
    @if (slot !== 'full') {
      <span class="badge bg-white text-dark ms-1 align-middle"
            style="font-size:.65rem; padding:.1rem .3rem; font-weight:600">{{ slotLabel[slot] }}</span>
    }
  }
</button>
```

關鍵：
- **色系完全沿用基礎 pattern**（假日 danger / 平日 primary，選中實心 / 未選 outline），時段不佔用色彩語意
- 「全天」＝純色 chip 無後綴，視覺與兩態 pattern 完全相同；只有半天才疊一顆白底小 pill（`badge bg-white text-dark`），在 danger / primary 兩種底色上對比皆足夠，不需新 token、不需自訂 CSS class
- 循環表以 `Record<Slot, Slot | null>` 常數表達（`null`＝下一態是取消勾選），不用 if/else 串接
- summary 與唯讀文字改為**加權天數**：「已選 2 天（假日 1.5 天）」、「8/2、8/3 上午、8/4 下午」；天數格式化統一走 model 的 `formatParticipantDays()`（整數不補小數、半天顯示一位）
- 卡片上方說明需明寫循環順序：「點擊日期依序切換 **全天 → 上午 → 下午 → 取消**」
- 時段字面值與權重的單一真相放在 feature model（`ParticipantDaySlot` / `PARTICIPANT_SLOT_LABELS` / `participantSlotWeight`），需與後端 `Constants.cs` 的 `ParticipantDateSlots` 同步

實例：`holiday-travel-request-form.html` 參與執行人員卡片。

**變體：單一集合的逐日勾選（銷假）** — 不隸屬任何 FormArray 列、整張表單只有一組選取時，chips 直接掛在卡片 body，並在卡片 header 右側附「全選 / 全部取消勾選」按鈕：

```html
<div class="card-header ... flex items-center justify-between gap-2 fw-600">
  <div class="flex items-center gap-2"><svg …/> 選擇要取消的日期</div>
  @if (!isReadOnly && dayChips().length > 0) {
    <button type="button" class="btn btn-sm btn-outline-secondary" (click)="toggleAll()">
      {{ allSelected() ? '全部取消勾選' : '全選' }}
    </button>
  }
</div>
```

- chip label 帶單位資訊（`MM/dd (三) 8h`），選取態 `btn-primary` / 未選 `btn-outline-secondary`
- 底部即時 summary：`已選 N 天 / M 小時`，其下以 `text-muted`（`font-size:.75rem`）補一行規則說明
- 唯讀模式 chips 保留但 `[disabled]`，用來呈現「已取消的日期」清單
- 選取狀態以 `signal<Set<string>>` 保存，`selectedDays` / `selectedHours` / `allSelected` 皆為 `computed`

實例：`leave-revocation-form.html`「選擇要取消的日期」卡片。

---

## 7. 明細列表（FormArray）

明細列表（發票項目、費用明細、HR 多筆紀錄、**加班申請關聯專案**等）一律以 `<table>` + `FormArray` 實作。

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

### 7.1.2 欄寬標準（px）

> 上方骨架用 `w-10` / `w-12` 只是示意；**專案實務一律用 inline `style="min-width:Npx"` / `style="width:Npx"`**，因為明細表包在 `table-responsive` 內、欄數多，Tailwind 的 `w-*` 無法表達「至少多寬、可再撐開」。新表格請直接沿用下表數值，不要另訂。

- **固定小欄用 `width`**（不需要被內容撐開）：項次唯讀 `48px`（容納雙位數）／項次可編輯 `64px`（格內為 `<input type="number">`，瀏覽器的上下微調箭頭另佔約 11px，48px 只剩兩字寬）、刪除鈕欄 `40px`、per-row 檔案欄 `72px`、僅圖示的檔案欄 `40px`、分組欄（批次）`130px`
- **其餘用 `min-width`**：金額欄 `150px`（七位數 + 千分位不被擠壓，**明細表內最寬的欄**）、日期 `120px`、發票號碼 / 項目說明 `130px`、分類 `90px`、備註 `80px`、數量/單位 `70px`

| 欄位 | 寬度 | 屬性 |
|---|---|---|
| 批次（分組欄） | 130px | `width` |
| 分類 | 90px | `min-width` |
| 項次（唯讀文字） | **48px** | `width` |
| 項次（可編輯 number input） | **64px** | `width` |
| 發票號碼 | 130px | `min-width` |
| 發票日期 | 120px | `min-width` |
| 項目說明 | 130px | `min-width` |
| **金額（單價 / 總價 / 現金 / 支票）** | **150px** | `min-width` |
| 數量/單位 | 70px | `min-width` |
| 備註 | 80px | `min-width` |
| 支票已支付（checkbox） | 110px | `width` |
| 檔案（per-row 上傳／預覽） | **72px** | `width` |
| 檔案（僅圖示，無上傳鈕） | 40px | `width` |
| 刪除鈕欄 | **40px** | `width` |

**檔案欄的 72px 是下限**：已上傳狀態要並排「預覽 + 移除」兩顆 `btn btn-sm p-1`，該情境下 icon 必須用 `sa-icon sa-icon-1x`（18px）而非預設 20px，否則兩顆按鈕塞不進 72px 扣掉 `table-sm` cell padding 後的 64px。

> **72px 的前提是 file input 真的被藏起來**：per-row 上傳鈕的 `<input type="file">` 必須加 Tailwind 的 `class="hidden"`。2026-08 曾在 [advance-form](../Admin/src/app/features/admin/advance-requests/pages/advance-form/advance-form.html) 寫成 Bootstrap 的 `d-none`，而 Bootstrap CSS 已於 2026-02 移除、`tailwind.css` 只重定義了帶斷點的 `.d-md-none` 之類，無前綴的 `.d-none` 根本不存在 —— 該欄整個渲染出原生檔案輸入框（約 240px），把整張明細表撐開、其餘欄位被壓縮。詳見 §12.6。

**已套用**（2026-08，預支 / 預支沖銷全系列）：[advance-form](../Admin/src/app/features/admin/advance-requests/pages/advance-form/advance-form.html) / [advance-detail](../Admin/src/app/features/admin/advance-requests/pages/advance-detail/advance-detail.html) / [write-off-form](../Admin/src/app/features/admin/write-off-requests/pages/write-off-form/write-off-form.html)（唯讀預支明細 + 編輯實際花費明細兩張表）/ [write-off-detail](../Admin/src/app/features/admin/write-off-requests/pages/write-off-detail/write-off-detail.html) / [write-off-overview](../Admin/src/app/features/admin/write-off-requests/pages/write-off-overview/write-off-overview.html)（兩張表）。

> 明細表沒有共用元件，每張表各自維護 `<th>`；**改欄寬時同一系列的表要一起改**，否則詳情頁與編輯頁欄位對不齊。

### 7.1.3 欄位順序標準

明細表的欄位順序**跨申請單一致**，不因單別自訂。基準順序（有該欄才出現，沒有就跳過）：

```
檔案 → 發票號碼 → 發票日期 → 分類 → 項次 → 項目說明 → 單價 → 數量/單位 → 總價 → 現金 → 支票 → 備註 → 刪除鈕
```

- **發票欄（發票號碼 / 發票日期）一律緊接在檔案欄之後、分類之前**，不放表尾。理由是操作動線：上傳單據 → OCR 回填 → 就地核對號碼與日期，三件事在視線同一區塊完成；擺到 `備註` 後面會讓使用者在寬表裡左右來回。
- **備註永遠是最後一個資料欄**（刪除鈕欄除外）。
- 分組欄（如沖銷的「批次」）例外，置於最左。

> **改順序＝改四處**：form / detail / [approval-task-review](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-review/approval-task-review.html) 的該類型明細表 / PDF service，四處要一起動，且每處的 `<tfoot>` colspan 與 PDF `columnStyles` 索引都要跟著重算（見 §7.1.1 的提醒）。
>
> **已套用**：2026-08 出差請款（TPR）從「發票欄置尾」改為與請款 / 預支沖銷一致的置前 —— [travel-payment-form](../Admin/src/app/features/admin/travel-payment-requests/pages/travel-payment-form/travel-payment-form.html) / [travel-payment-detail](../Admin/src/app/features/admin/travel-payment-requests/pages/travel-payment-detail/travel-payment-detail.html) / approval-task-review 的 `travel_payment` 區塊 / [travel-payment-pdf.service.ts](../Admin/src/app/features/admin/travel-payment-requests/services/travel-payment-pdf.service.ts)。成因是該單 clone 自出差預支（無發票欄）後把兩欄接在既有欄位尾巴，而非比照請款系列。

### 7.1.4 申請日期欄（送簽後才有值）

「申請日期」＝**送簽日**（後端 `submittedAt`），不是建立草稿的 `createdAt`。草稿還沒有這個值，
顯示規則與「單號」完全一致：

| 位置 | 欄名 / 標籤 | 綁定 | 草稿顯示 |
|---|---|---|---|
| 各申請清單頁 | **一律「申請日期」**（不用「建立時間 / 申請時間」） | `submittedAt` | `—` |
| 詳情頁資訊卡 | 申請日期 | `submittedAt` | `（送簽後產生）` |
| 簽核作業清單 / 詳情 | 申請日期 | `task.submittedAt` | 不適用（草稿不進簽核） |
| 列印 PDF | 申請日期 / 簽名欄「申請者」格日期 | `submittedAt` | 不適用 |
| 款項統計報表 | 申請日期（欄位**與日期區間篩選**同基準） | `submittedAt` | 不適用（報表排除草稿） |

```html
<!-- 清單頁 -->
<td class="text-muted small hidden lg:table-cell">
  {{ r.submittedAt ? (r.submittedAt | date:'yyyy-MM-dd') : '—' }}
</td>

<!-- 詳情頁 -->
<div class="text-muted small">申請日期</div>
<div class="fw-500">{{ r.submittedAt ? (r.submittedAt | date:'yyyy-MM-dd') : '（送簽後產生）' }}</div>
```

> `createdAt` 仍保留在 model 上（建立草稿時間），但**不再用於任何「申請日期」的顯示**。
> 主檔類清單（廠商 / 職稱 / 角色 / 簽核流程設定）的「建立時間」欄不是申請單，維持 `createdAt` 不動。

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

明細列含日期欄位時，**格式轉換放在呼叫端**（builder 只收已格式化好的 string），寫法見 [§6 日期欄位回填](#日期欄位回填input-typedate重要)：

```typescript
items.forEach(it => this.invoiceArray.push(
  this._invoiceGroup(..., it.invoiceDate?.toString().slice(0, 10) ?? '')
));
```

### 7.5.1 下拉排除已選項目（互斥 select）

明細每列都要從**同一份清單**挑一個不重複的項目時（如加班申請的關聯專案：一列一專案、同單不可重複），每列的下拉必須排除其他列已選過的值 —— 前端先擋掉，後端仍須保留重複驗證：

```ts
/** 第 index 列可選的專案：排除其他列已選過的（後端亦擋重複專案） */
availableProjects(index: number): Project[] {
  const taken = new Set<number>(
    this.projectControls
        .filter((_, i) => i !== index)
        .map(g => g.get('projectId')!.value as number | null)
        .filter((v): v is number => v != null));
  return this.projects.filter(p => !taken.has(p.id));
}
```

```html
<select class="form-select form-select-sm" formControlName="projectId">
  <option [ngValue]="null" disabled>請選擇專案</option>
  @for (p of availableProjects(i); track p.id) {
    <option [ngValue]="p.id">{{ p.code }} - {{ p.name }}</option>
  }
</select>
```

> 目前該列已選的值**不會**被自己排除（`filter((_, i) => i !== index)`），否則下拉會顯示空白。

### 7.5.2 明細合計顯示於表頭欄位

明細各列的數值需要在表單上方以「總計」呈現時（如加班申請的預估總時數 = 各專案時數加總），該欄改為**唯讀顯示 + 說明文字**，不可留成可編輯的 input（否則會出現兩個真相）：

```html
<label class="form-label fw-500">預估總時數（小時）</label>
<p class="form-control-plaintext fw-600 mb-0">{{ totalHours() | number:'1.1-1' }} h</p>
<div class="form-text">由下方各專案時數自動加總，不可直接編輯。</div>
```

`FormArray` 的值變動**無法用 `computed()` 追蹤**，須訂閱 `valueChanges`（與 §7.4 的 `project-form` 合計寫法一致）：

```ts
this.projectsArray.valueChanges.subscribe(() => this.recomputeTotalHours());

private recomputeTotalHours() {
  const sum = this.projectControls.reduce((acc, g) => acc + (+(g.get('estimatedHours')!.value ?? 0) || 0), 0);
  this.totalHours.set(Math.round(sum * 10) / 10);   // 對齊後端 decimal(5,1)
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

- **簽核頁整欄對所有審核者顯示**（2026-07 改：原本非財務體系整欄不渲染，改為欄位照常顯示、checkbox `disabled` 反白），可否操作靠 `canMarkCheckPaid(task)`
- **可勾範圍＝財務管理部**（`FINANCE_STEP_DEPT_CODES`，比對**登入者自身部門**）**或 Superadmin**。2026-07 收窄：原本用 `auth.isFinanceDept()`（`FINANCIAL_AND_ABOVE_DEPT_CODES`，含總監室 / 會計室 / Jabez HQ）導致財務管理部以外的人也能勾，現與撥款日 / 撥款明細 / 結案同範圍
- 簽核頁（財務管理部 / Superadmin，單子 pending 或 approved）：checkbox 可勾，變更即呼叫 `PATCH /write-off-requests/{id}/check-payments`，樂觀更新不重載整頁
- 簽核頁（非財務管理部，或單子非 pending / approved）：同一顆 checkbox `disabled` 反白，仍顯示已勾狀態；依 §8.5 綁 `[title]` 說明原因，原因收斂在 `checkPaidDisabledHint(task)`（空字串＝可勾），title 文字由 `checkPaidTitle(task, item)` 產出（已勾選改顯示勾選日期與勾選人）
- 詳情頁 / 彙總頁：唯讀顯示 `✓`（`title` 帶勾選日期與勾選人）或 `—`
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

### 7.11 單月薪資明細（[`<app-payroll-detail-card>`](../Admin/src/app/shared/components/payroll-detail-card.ts)）

「應發項目 / 扣款項目（+ 本月請假紀錄）」的唯讀薪資明細，2026-08 從 `payroll-form` 抽出成共用元件，供兩處使用：

| 使用者 | 頁面 | `showNetSalary` |
|---|---|---|
| 人事（`payroll:read`） | `/admin/payroll/{id}` 薪資調整頁 | `false`（該頁另有可即時預覽的「實領薪資」卡片，會隨表單輸入變動） |
| 一般員工 | 個人資訊 →「過往薪資」Tab 展開列 | `true`（表尾附「實領薪水」列＋育嬰留停負數警語） |

- 結構同其他 detail 卡片：`card border-0 shadow-sm` + `card-header` + `card-body p-0` + `table table-hover`。
- 分區列用 `<tr class="table-light">`＋`text-primary`（應發，icon `#plus-circle`）／`text-danger`（扣款，icon `#minus-circle`）。
- 加給、各假別扣薪、勞退自提**有值才顯示該列**（`@if`），避免一堆 0 的雜訊。
- 假別中文與時數格式一律 import [`LEAVE_TYPE_LABELS` / `formatLeaveDuration`](../Admin/src/app/features/admin/leave-requests/models/leave-request.model.ts)，不在元件內另抄一份對照表。

> 展開列的用法：`<td colspan="欄數" class="p-4 bg-[var(--bg-elevated)]">` 內放此元件，讓明細與清單列在視覺上分層。

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

### 8.4.1 申請表單「儲存 / 送出」必須有 in-flight 鎖（**重要**）

申請表單的儲存 / 送出會做 multipart 上傳（發票、附件），一送好幾秒。
**按鈕在請求期間必須 disabled + spinner**，否則畫面毫無反應，使用者會再按一次，
**每按一次就是一個 POST，同一張單會被建立多筆**（2026-08 預支沖銷實際踩過）。

三個要點缺一不可：

```ts
/** 儲存 / 送出進行中（鎖按鈕 + spinner） */
saving = signal(false);

submitForApproval() {
  if (this.saving()) return;                     // ① 方法層再擋一次（連點的第二下可能早於變更偵測）
  this.saving.set(true);
  const save$ = this.editId                      // ② 判斷依據是「後端已有這張單的 id」，不是路由模式旗標 isEdit
    ? this.service.update(this.editId, fd)
    : this.service.create(fd);
  save$.subscribe({
    next: saved => {
      this.editId = saved.id;                    // ③ create 成功立刻記住 id
      this.service.submit(saved.id).subscribe({  //    → 後續 submit 失敗時重送走 update，不會再建一張新單
        next:  () => { this.saving.set(false); this._onSubmitted([...]); },
        error: err => { this.saving.set(false); this.errorMsg.set((err.error?.message ?? '送出失敗') + '（草稿已保留，修正後可直接再送出）'); },
      });
    },
    error: err => { this.saving.set(false); this.errorMsg.set(err.error?.message ?? '儲存失敗'); },
  });
}
```

- `isEdit` 只用於「標題 / 版面呈現」，**不可**在 create 成功後翻轉（會讓新增模式的預支單選擇區塊整塊換掉）
- 錯誤時務必 `saving.set(false)`，否則按鈕永久鎖死
- **11 支申請表單已全數套用**（請款 / 預支（含追加批次）/ 出差預支 / 出差請款 / 預支沖銷 / 出差預支沖銷 / 請假 / 銷假 / 加班 / 假日執行活動 / 預審）。新增申請表單時比照辦理
  - **簽核作業詳情頁的「提交審核」同樣要鎖**（2026-09 補）：後端雖有 `ApprovalStatus != "pending"` 守門，但連按的兩個請求會在第一個 commit 前一起讀到 pending，
    同一關卡因此寫入**兩筆 ApprovalRecord**（實際發生過：同一位總監相隔 1.1 秒的兩筆 step3 紀錄），PDF 簽名欄與簽核時間軸會出現重複簽章。
    `submitting` signal + 方法層 `if (this.submitting()) return;`，**成功後導頁不解鎖**、只在錯誤時 `set(false)`
  - 銷假表單的鎖寫在 `canSave` getter（`!this.saving() && …`），兩個方法都走該 getter，效果相同
  - 預支的「新增追加批次」是 `POST /advance-requests/{id}/supplements` 建立即送簽的單一請求，連按同樣會建出兩個批次，故 `_submitSupplement()` 內也上鎖

### 8.4.2 表單內按 Enter 不得直接送出

申請表單為 `<form [formGroup] (ngSubmit)="save()">` + `type="submit"` 的儲存鈕，
瀏覽器預設**在任一 `<input>` 按 Enter 就會觸發 ngSubmit**，使用者打完金額順手按 Enter
就會建立草稿並跳回列表，只看到頁面莫名跳走，誤以為資料沒存到而重做一次。

```html
<form [formGroup]="form" (ngSubmit)="save()" (keydown.enter)="onEnterKey($event)">
```

```ts
/** 表單內按 Enter 不送出（textarea 換行不受影響） */
onEnterKey(event: Event) {
  const tag = (event.target as HTMLElement)?.tagName;
  if (tag !== 'TEXTAREA') event.preventDefault();
}
```

### 8.5 disabled 必須說明原因

當按鈕的 `[disabled]` 來自**業務條件**（而非單純 loading）時，必須同時綁 `[title]` 說明原因，
否則使用者只看到一顆灰掉的按鈕、無從得知要先做什麼。

把原因收斂成一個 `computed`（回傳空字串代表可按），而非在 template 內寫多層三元式：

```ts
/** 加班開始 disabled 時的原因提示 */
overtimeStartHint = computed<string>(() => {
  const r = this.todayRecord();
  if (r?.overtimeStartTime) return '今日已打加班開始卡';
  if (this.approvedRequests().length === 0) return '今日無已核准的加班申請單';
  if (!r?.clockOutTime && !r?.canOvertimeWithoutClockOut) return '請先打下班卡（今日為上班日）';
  return '';
});
```

```html
<button class="btn btn-outline-primary"
        [disabled]="!canOvertimeStart()"
        [title]="overtimeStartHint()"
        (click)="onOvertimeStartClick()">加班開始</button>
```

範例：dashboard 打卡四鈕（`features/dashboard/pages/dashboard/`）。
條件牽涉後端業務規則時，**由後端回一個結論旗標**（如 `canOvertimeWithoutClockOut`），
前端不重組規則，避免前後端判定漂移。

### 8.6 列印 PDF 按鈕的顯示條件

**7 種紙本財務單**（請款 / 預支 / 預支沖銷 / 出差預支 / 出差預支沖銷 / 出差請款 / 假日執行活動）
的申請詳情頁，列印按鈕一律：

```html
<!-- 紙本流程：主管簽核完畢後即印出紙本寄回會計室，故送出（非草稿）後就能列印 -->
@if (r.approvalStatus !== 'draft') {
  <button class="btn btn-outline-secondary inline-flex items-center gap-1"
          (click)="printXxx()" [disabled]="pdfLoading()"> … </button>
}
```

- **不可收緊成 `=== 'approved'`**：送出成功彈窗（[submit-success-modal.ts](../Admin/src/app/shared/components/submit-success-modal.ts)）明寫「請於**單位主管簽核完畢後**，再印出<單別>連同紙本單據寄回會計室」——紙本要在流程**中途（pending）**印，收緊會直接擋掉紙本流程。
- **draft 不可印**：草稿還沒有單號以外的簽核事實，印出來是張空白單。
- **未簽的關卡在簽名欄留白**：`buildDynamicSignBlocks` 依 `flow.steps` 產生欄位、有 `ApprovalRecord` 的才填簽章與日期，pending 印出來就是「已簽的有章、未簽的留白」。
- **PDF service 內不可再放狀態閘**：`printXxx()` 只擋資料不足（如 `if (!task.paymentDetail) return;`），不得再寫 `task.status !== 'approved'` —— 否則按鈕看得到、按了沒反應（此坑已於 2026-08 在 `payment-pdf.service.ts` 踩過）。
- **例外**：預審申請不走紙本流程，維持 `approved` 才可列印；簽核作業頁（approval-task-review）的審核者列印同樣維持 `approved`。

---

## 8.6 即時試算卡片（Live Estimate Card）

表單欄位變動後由後端即時算出金額 / 天數並回顯的區塊（目前唯一採用：加班申請的「加班費試算」）。**不是** `alert`，也不是獨立 `card` —— 它屬於所在卡片的一部分，用 `border rounded p-4 bg-light-subtle` 內嵌。

**結構（由上而下）**

| 區 | 內容 | 樣式 |
|---|---|---|
| 標題列 | 左「{項目}試算」+ 右狀態（`計算中…` / 唯讀時的 `核准快照` badge） | `flex items-center justify-between mb-3`，標題 `fw-600` |
| 分段明細 | `table table-sm`，一列一段（算式 / 數量 / 金額） | 包 `table-responsive` |
| 結果列 | 金額大字 + 條件 badge + 補充說明 | `flex flex-wrap items-baseline gap-4`，金額 `fs-4 fw-600 text-primary` |
| 警示 | 超出上限 / 前置資料缺漏 → `alert-warning`；業務衝突 → `alert-danger` | `alert ... flex items-center gap-2 py-2`，icon `#alert-triangle` |
| 註腳 | 金額何時、以什麼身分生效 | `form-text mt-2` |

**規則**

1. **一定要顯示算式，不能只給總額** —— 使用者看到單一數字不會相信，看到「2h ×1.34 + 6h ×1.67」才會。分段明細不是裝飾。
2. **請求要節流**：`takeUntilDestroyed + debounceTime(300) + distinctUntilChanged + switchMap + catchError(() => of(null))`，範式比照 [user-form.ts](../Admin/src/app/features/admin/users/pages/user-form/user-form.ts) 的「底薪 → 勞健保級距 lookup」。`distinctUntilChanged` 必須逐欄比對試算輸入，否則無關欄位的每次鍵入都會打一次 API。
3. **失敗靜默**：`catchError` 回 `null` 讓卡片退回「請先填寫…」提示，不要跳 toastr —— 使用者還在填表，這不是錯誤。
4. **唯讀模式顯示快照、不重新試算**：已送出 / 已核准的單顯示後端存的金額並標「核准快照」badge，加註「日後調薪不會回溯變動」。重新試算會讓畫面金額與實發金額不一致。
5. **前置資料缺漏不擋送出**：改以 `alert-warning` 說明（例：「尚未設定底薪，無法試算，請洽人事」），送出鍵維持可用。

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
| [approval-task-list](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-list/approval-task-list.html) | `已結案` | `isClosed(t)`：`advance` 看 `advanceDetail.isClosed`、`travel` 看 `travelDetail.isClosed`（**與 advance-list / travel 詳情同一真相 `AdvanceRequest.IsClosed` / `TravelRequest.IsClosed`、同一 class `bg-elevated text-secondary`**）；`holiday_travel` 共用 TravelRequest 但不走沖銷、永不結案故排除；狀態欄因此可能同時出現三個徽章（審核狀態 + 撥款 + 已結案），排版沿用同一 `flex flex-wrap gap-1` 容器 |

**「結案資訊」卡片＝共用元件 [`<app-closure-info-card>`](../Admin/src/app/shared/components/closure-info-card.ts)**（2026-07 從 4 份重複 HTML 收斂）。欄位順序固定：結案狀態 / 結案時間 / 應退還差額 / 實際退款金額 / 預計退款日 / 退款日（未退款顯示「尚未退款」），`row g-3` + `col-6 col-md-3`，card header icon `#check-circle`。

| input | 用途 |
|---|---|
| `isClosed` / `closedAt` / `refundAmount` / `refundedAmount` / `estimatedRefundDate` / `refundedAt` | 六個資料欄位 |
| `title` | 預設「結案資訊」；兩種沖銷頁傳「預支單結案資訊」/「出差單結案資訊」以標明是**關聯母單**的狀態 |
| `cardClass` | 間距：detail 頁 `mb-6`（預設）、簽核頁 `mt-6` |
| `showRefund` | 沖銷頁傳 `false`：同一組金額 / 日期在沖銷頁已以**「撥款」語彙**呈現（差額撥款分期 + 已核准卡片 + 出差沖銷的預計撥款日 / 撥款日 / 撥款金額），兩種標籤並存會語意混淆，故該頁只留結案狀態 |
| `alwaysShow` | 沖銷頁傳 `true`：未結案時也要看得到「未結案」badge（`bg-warning-subtle text-warning-emphasis`）。其他頁沿用「六欄全空則整卡不渲染」 |

已採用：[advance-detail](../Admin/src/app/features/admin/advance-requests/pages/advance-detail/advance-detail.html)、[travel-detail](../Admin/src/app/features/admin/travel-requests/pages/travel-detail/travel-detail.html)、[write-off-detail](../Admin/src/app/features/admin/write-off-requests/pages/write-off-detail/write-off-detail.html)、[travel-write-off-detail](../Admin/src/app/features/admin/travel-write-off-requests/pages/travel-write-off-detail/travel-write-off-detail.html)、[approval-task-review](../Admin/src/app/features/admin/approval-tasks/pages/approval-task-review/approval-task-review.html)。

簽核頁以 `closureInfo(task)` 供資料，另有兩個 helper 收斂 per-type 差異：`isRelatedClosure(task)`（是否為沖銷類 → 決定 `showRefund` / `alwaysShow`）與 `closureTitle(task)`。取值來源：`advance` / `travel` 為本單自身欄位；`write_off` 取 `advanceIsClosed` / `advanceClosedAt`、`travel_write_off` 取 `travelIsClosed` / `travelClosedAt`（沖銷單本身無結案概念，一律回傳資料讓 `alwaysShow` 決定呈現）。卡片置於申請資訊區與簽核流程時間軸之間，同頁「已核准」卡片**不再重複**列 advance / travel 的預計退款日 / 退款日。advance / travel 詳情頁與簽核頁頁首另掛「已結案（yyyy-MM-dd）」徽章。

業務狀態徽章建議色：

| 語意 | class |
|---|---|
| 進行中 / 待處理 | `bg-warning-subtle text-warning-emphasis` |
| 完成 / 已結束 | `bg-primary-subtle text-primary`（CIS 森林綠，與 success 綠形成深淺差別） |
| 唯讀資料標記（請假 / 分類 / 來源註記） | `bg-primary-subtle text-primary` |
| 中性附註 | `bg-secondary-subtle text-secondary` |

> ⚠️ `text-*-emphasis` 在 `tailwind.css` **只定義了 `text-warning-emphasis`**，配其他色時不可照抄 warning 的寫法，
> 一律改用上表既有組合。同一列可能並存多顆徽章時（例：出缺勤紀錄的「系統補卡」黃 + 「請假」綠），
> **必須挑不同色系**，否則兩顆同色徽章難以分辨。

### 9.2 跨列表一致性原則（**重要**）

**同一筆資料在不同列表頁出現時，審核狀態 + 業務狀態徽章的 label / CSS / status gate 必須完全一致**，否則使用者會看到同一筆東西呈現不同文字或顏色。

**Single Source of Truth 規則：**

1. **審核狀態 mapping**：只在 [payment-request.model.ts](../Admin/src/app/features/admin/payment-requests/models/payment-request.model.ts) 定義 `APPROVAL_STATUS_LABELS` / `APPROVAL_STATUS_CLASSES`。其他 feature model（如 [approval-task.model.ts](../Admin/src/app/features/admin/approval-tasks/models/approval-task.model.ts) 的 `TASK_STATUS_LABELS / TASK_STATUS_CLASSES`）以**直接賦值 re-export** 共用，禁止重複定義。
2. **業務狀態 mapping**（撥款 / 退款 / 結案 etc.）：同上，在來源 feature model 定義 `XXX_STATE_LABELS` / `XXX_STATE_CLASSES`，其他列表 re-export 使用。例如 `PAYMENT_STATE_LABELS` / `PAYMENT_STATE_CLASSES` 來自 payment-request.model。
3. **status gate（顯示時機）**：跨列表顯示同一筆資料的業務狀態徽章時，gate 條件必須一致。例如撥款徽章在 `payment-list` 是 `approvalStatus ∈ {pending, approved}`；簽核作業列表也須同樣 gate（即使 `pending` 在「待審核 tab」也要顯示「待撥款」黃徽章）。
4. **per-type 業務規則例外**：簽核作業列表因彙整多種申請類型，可保留 per-type 業務條件（如 write_off / travel_write_off 需「超支」才顯示徽章、holiday_travel 永不顯示等），但**徽章本身的 label/CSS 仍套用共用 mapping**。
5. **列舉值 → 中文 mapping**（假別、假別分類…）：同樣只在來源 feature model 定義，其他頁 `import` 共用。假別 `LEAVE_TYPE_LABELS`（17 種）的來源是 [leave-request.model.ts](../Admin/src/app/features/admin/leave-requests/models/leave-request.model.ts)；出缺勤紀錄原本自留一份只含 4 種的複本，導致婚假 / 公假 / 產假等直接顯示英文代碼 —— 這正是禁止重複定義的原因。

**Code Review 檢查點：**

- [ ] 新增列表頁顯示審核狀態徽章 → 是否從 `payment-request.model` import `APPROVAL_STATUS_LABELS / CLASSES`？（禁止自行另寫一份）
- [ ] 新增「待撥款 / 已撥款」徽章 → 是否從 `payment-request.model` import `PAYMENT_STATE_LABELS / CLASSES`？
- [ ] 顯示假別中文 → 是否從 `leave-request.model` import `LEAVE_TYPE_LABELS`？（禁止只列常用幾種）
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

## 11.6 送出成功 Modal（`<app-submit-success-modal>`）

申請送出成功後的「申請成功」提醒視窗，一律開共用元件 [`shared/components/submit-success-modal.ts`](../Admin/src/app/shared/components/submit-success-modal.ts)。
**禁止**再於各頁內嵌 `<ng-template #successModal>`（此彈窗曾在 8 個表單樣板重複貼 8 份，2026-08 收斂）。

### 11.6.1 兩個 Input（擇一）

| Input | 用途 |
|---|---|
| `@Input() formType?: PaperFormApplicationType` | 走「印出紙本寄回會計室」流程的 **7 種財務單**，文案自動帶入單別名稱 |
| `@Input() message?: string` | 不走紙本流程者的自訂訊息（目前僅預審申請「預審申請已送出，等待審核中」） |

> 用 `@Input()` 而非 signal `input()`：`NgbModalRef` 只暴露 `componentInstance`，無法對 `InputSignal` 賦值。同 §11.5 的 `VendorQuickAddModal.prefillName`。

`formType` 的文案為：

> 請於單位主管簽核完畢後，再印出**{單別}**連同紙本單據寄回會計室進行行政財務流程。

**單別名稱單一真相** = [`approvals/models/approval.model.ts`](../Admin/src/app/features/admin/approvals/models/approval.model.ts) 的 `APPLICATION_FORM_NAMES`（請款單 / 預支單 / 出差請款單 / 預支沖銷單 / 出差預支沖銷單 / 出差預支單 / 假日執行活動單）。新增申請類型時，`APPLICATION_TYPE_LABELS` 與 `APPLICATION_FORM_NAMES` 兩張表一起補。

### 11.6.2 呼叫方式（兩種，差在關閉後的動作）

**表單頁**（8 支 `*-form`）：接 `ref.result`，關閉後導回列表 —— 統一收斂成 private `_onSubmitted(target)` helper：

```typescript
private _onSubmitted(target: unknown[]) {
  const ref = this.modal.open(SubmitSuccessModal, { centered: true, backdrop: 'static', keyboard: false });
  ref.componentInstance.formType = 'payment_request';
  ref.result.then(() => this.router.navigate(target))
            .catch(() => this.router.navigate(target));
}
```

**詳情頁**（6 支 `*-detail` 的草稿送出）：**不接** `ref.result`，關閉後留在原頁：

```typescript
this.service.submit(r.id).subscribe(updated => {
  this.request.set(updated);
  const ref = this.modal.open(SubmitSuccessModal, { centered: true, backdrop: 'static', keyboard: false });
  ref.componentInstance.formType = 'payment_request';
});
```

`write-off-detail` / `travel-write-off-detail` 送出前既有的 native `confirm(...)` 保留 —— 那是「送出前的破壞性確認」，與「送出後的成功提醒」語意不同，彈窗插在 confirm 通過後的 `next` 分支。

### 11.6.3 涵蓋範圍

| 申請 | 表單頁 | 詳情頁草稿送出 | `formType` |
|---|---|---|---|
| 請款 | ✓ | ✓ | `payment_request` |
| 預支 | ✓ | —（無 detail 送出鈕） | `advance` |
| 出差請款 | ✓ | ✓ | `travel_payment` |
| 預支沖銷 | ✓ | ✓（confirm 後） | `write_off` |
| 出差預支沖銷 | ✓ | ✓（confirm 後） | `travel_write_off` |
| 出差預支 | ✓ | ✓ | `travel` |
| 假日執行活動 | ✓ | ✓ | `holiday_travel` |
| 預審 | ✓（傳 `message`） | —（刻意不加） | — |

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
| 證明文件（原住民 / 低收入 / 身心障礙 / 身分證 / 最高學歷） | 1 MB | 1600 |
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
7. **買方抬頭/統編驗證**：OCR 結果含 `buyerName`/`buyerTaxId`/`sellerTaxId`，填值後對 `docType==='invoice'` 的列呼叫共用工具 [`validateInvoiceBuyer(buyerName, buyerTaxId, sellerTaxId)`](../Admin/src/app/shared/utils/invoice-buyer-validator.ts) 比對公司白名單（6 組抬頭＋統編）。**抬頭與統編需皆讀得到才判斷，任一缺漏即跳過不驗**（收銀機 / 二聯式 / 手寫讀不全）。**第三參數 `sellerTaxId` 必傳**：買方統編與賣方統編相同時代表 OCR 抄到「營業人蓋用統一發票專用章」，視同讀不到而不跳警告（手寫發票買受人統編潦草時最常發生）。不符時 `invoiceWarnings.set(rowId, msg)`（`invoiceWarnings = new Map<string,string>()`，key = 列 id），刪列時一併 `delete`。統編檢查碼不合、但與某組白名單**僅差 1 碼且抬頭相容**時視為手寫誤讀該組，同樣不跳警告；**抬頭相容另含「同長度僅差 1 個字」**（統編已完全命中白名單時，長中文公司名差 1 字幾乎必為 OCR 形近字誤讀，如「雅比斯」被讀成「羅比斯」；長度 ≥ 6 才適用）。**警告訊息一律帶出讀到的統編**（三種：抬頭與統編不符 / 統編辨識不完整 / 統編不在白名單），使用者才能自行判斷是 OCR 抓錯欄位還是真的開錯抬頭。**警告僅顯示、不阻擋送出、不持久化**。警告列以 `<span class="inline-flex items-center gap-1">` 包 `sa-icon sa-icon-1x`（alert-triangle）＋訊息，**icon 與文字同一行**。

**警告列附「確認無誤」checkbox**：容錯規則沒接住的誤判（OCR 讀成完全不同的公司名、統編抄到他欄等）由使用者自行放行 —— 元件上另備 `invoiceConfirmed = new Set<string>()`（key 同為列 id），勾選後該列警告由 `text-danger` 轉 `text-muted` 並在訊息前加「已確認無誤：」。**確認狀態與警告一樣純顯示**：不阻擋送出（本來就不擋）、不進 FormControl、不寫 DB、重開草稿不重現。`_checkBuyer()` 開頭須 `invoiceConfirmed.delete(rowId)`（同一列重新 OCR → 舊確認失效）、`removeItem()` 須比照 `invoiceWarnings` 一併 `delete`。四個表單皆須套用，不可只修其中一個。

> 多張擠在一張照片時辨識準確度較低，各列辨識後仍需人工核對（欄位皆可手動修改）。

#### 明細列下方警告列 pattern

在 `@for` 的明細 `<tr>` **之後**，插入一條條件式警告列（沿用既有錯誤樣式 `text-danger small` + `alert-triangle` icon），`colspan` 取該表實際欄數（請款 7 / 出差請款 11 / 預支沖銷 12 / 出差預支沖銷 10）；有 readonly 模式者加 `!isReadOnly` 守衛：

```html
</tr>
@if (invoiceWarnings.has(ctrl.get('id')?.value)) {
  <tr>
    <td colspan="<欄數>" class="small py-1 ps-3 border-0"
        [class.text-danger]="!invoiceConfirmed.has(ctrl.get('id')?.value)"
        [class.text-muted]="invoiceConfirmed.has(ctrl.get('id')?.value)">
      <span class="inline-flex items-center gap-3 flex-wrap">
        <span class="inline-flex items-center gap-1">
          <svg class="sa-icon sa-icon-1x" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
          @if (invoiceConfirmed.has(ctrl.get('id')?.value)) { <span>已確認無誤：</span> }
          {{ invoiceWarnings.get(ctrl.get('id')?.value) }}
        </span>
        <span class="form-check mb-0">
          <input type="checkbox" class="form-check-input"
                 [id]="'invoiceConfirm-' + ctrl.get('id')?.value"
                 [checked]="invoiceConfirmed.has(ctrl.get('id')?.value)"
                 (change)="toggleInvoiceConfirm(ctrl.get('id')?.value, $event)">
          <label class="form-check-label" [for]="'invoiceConfirm-' + ctrl.get('id')?.value">確認無誤</label>
        </span>
      </span>
    </td>
  </tr>
}
```

> 只有買方警告列附 checkbox（人工放行有意義）；同檔的 `amountWarnings`（總價 ≠ 現金＋支票）維持純紅字列。

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

### 12.6 明細列內的 icon 觸發上傳（per-row）

明細表的檔案欄空間只有 72px，放不下 §12.1 那種全寬上傳區塊，一律改成「`<label class="btn">` 包 icon + 隱藏的 file input」，點 icon 等同點 input：

```html
<!-- 未上傳：只有一顆上傳 icon -->
<label class="btn btn-sm btn-outline-secondary mb-0" style="cursor:pointer">
  <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#upload"></use></svg>
  <input type="file" class="hidden" accept=".jpg,.jpeg,.png,.heic,.heif,.pdf"
         (change)="onFileSelected($event, i)">
</label>

<!-- 已上傳：預覽 + 移除，icon 降為 sa-icon-1x 才塞得進 72px -->
<div class="flex items-center gap-1">
  <button type="button" class="btn btn-sm btn-ghost-primary p-1" (click)="openPreview(...)" [title]="fileName">
    <svg class="sa-icon sa-icon-1x" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#file-text"></use></svg>
  </button>
  <button type="button" class="btn btn-sm btn-ghost-danger p-1" (click)="removeFile(i)">
    <svg class="sa-icon sa-icon-1x" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
  </button>
</div>
```

> **⚠ 隱藏 file input 一律用 Tailwind 的 `class="hidden"`，禁用 Bootstrap 的 `d-none`。**
> Bootstrap CSS 已於 2026-02 從專案移除（見 §1.2），`tailwind.css` 只手寫重定義了帶斷點的 `.d-sm-none` / `.d-md-none` 等，**無前綴的 `.d-none` 不存在**，寫了等於沒藏 —— 該格會完整渲染原生檔案輸入框（Chrome 約 240px），撐開整張 `table-responsive` 明細表並壓縮其他欄位。2026-08 已修正 [advance-form](../Admin/src/app/features/admin/advance-requests/pages/advance-form/advance-form.html)（全站唯一一處誤用）。同理，任何從 Bootstrap 範例複製來的 class 都要先確認 `tailwind.css` 有沒有對應定義。

檔名不另外顯示文字（欄寬不夠），只掛在 `[title]` 當 tooltip；預覽走 `<app-file-preview-modal>` 而非另開下載連結。

---

## 12.7 Excel 匯出（SheetJS 前端產檔）

Excel 一律**在前端產生**：後端只回 JSON（不設 `Content-Disposition`、不引入 ClosedXML / EPPlus），前端 `import * as XLSX from 'xlsx'` 組表後 `XLSX.writeFile(wb, 檔名)`。已採用：出缺勤 / 加班 / 款項統計三張報表 + **人事薪資總表**。

### 12.7.1 資料來源二選一

| 情境 | 做法 |
|---|---|
| 列表**有分頁**（reports 三頁） | 匯出時**重打一次 API**取全量。後端須加 `?export=true` 旗標把 `pageSize` 上限放寬到模組常數（見 [backend-design.md §分頁](backend-design.md)），**禁止**只把 `pageSize` 送 9999 —— 後端仍夾到 100，Excel 會被靜默截斷 |
| API 本來就**不分頁**（人事薪資） | **直接讀已載入的 signal**，不再打 API。少一次往返，也不會有「畫面與檔案內容不一致」的時間差 |

### 12.7.2 兩種組表方式

- `XLSX.utils.json_to_sheet(rows)`：只有表頭 + 資料列時用（出缺勤 / 加班）。
- `XLSX.utils.aoa_to_sheet(aoa)`：需要**摘要列 / 合計列**時用（款項統計 / 人事薪資總表）。結構為 `[[摘要], [], [表頭], ...資料列, [合計列]]`。

### 12.7.3 合計列取值

合計列**一律取後端已算好的 Total 欄位**，不在前端重新加總 —— 否則畫面 summary card 與 Excel 會出現兩份真相。後端沒提供合計的欄（天數 / 比率 / 說明文字）留空字串。

### 12.7.4 格式化：只有 `!cols` 與 `cell.z` 有效

```ts
// 欄寬：中文字佔 2 格，不能直接用 header.length
const displayWidth = (t: string) => [...t].reduce((n, ch) => n + (ch.charCodeAt(0) > 0x2e80 ? 2 : 1), 0);
ws['!cols'] = headers.map(h => ({wch: Math.max(displayWidth(h) + 2, 10)}));

// 金額欄千分位；天數 / 比率維持 General
cell.z = '#,##0';
```

- ⚠️ **`cell.s`（字型 / 底色 / 對齊）在社群版 SheetJS 完全無效**，寫了也不會進檔案。[payment-report.ts](../Admin/src/app/features/admin/reports/pages/payment-report/payment-report.ts) 裡那段 `alignment: {wrapText: true}` 是無效程式碼，勿再複製。
- ⚠️ **天數 / 比率欄不要套 `'#,##0.##'`** —— 整數會顯示成「6.」多一個小數點。不指定 `z`（General）即可正確顯示 `6` 與 `1.5`。
- 金額一律寫入 **number 而非字串**，否則使用者無法在 Excel 直接 SUM。

### 12.7.5 按鈕與狀態

```html
<button class="btn btn-outline inline-flex items-center gap-1"
        [disabled]="exporting() || records().length === 0" (click)="exportExcel()">
  <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#download"></use></svg>
  @if (exporting()) { 匯出中... } @else { 匯出 Excel }
</button>
```

固定 `btn btn-outline` + `download` icon + `exporting` signal 鎖；無資料時 `disabled` 而非隱藏。產檔以 `try / catch / finally` 包住，失敗吐 toastr error，`finally` 一定要解鎖。

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

### 登入後首頁決策（landing redirect）

**規則：任何「回到主頁」一律指向 `/`，不得硬寫 `/dashboard`。**

`/dashboard`（打卡頁）自 2026-08 起需 `attendances:read`，不再是人人都進得去的頁面。若各處繼續硬指 `/dashboard`，沒有該權限的人會撞上 403 —— 而 403 頁的「回到主頁」按鈕本身也指向 `/dashboard`，形成 **403 → 回主頁 → 又 403** 的無窮迴圈。

改為由 `app.routes.ts` 的根路由當**唯一決策點**，實際落點集中在 [`resolveLandingUrl()`](../Admin/src/app/core/auth/utils/landing.ts)：

```typescript
// app.routes.ts —— Angular 支援 function 形式的 redirectTo，在 injection context 內執行
{
  path: '',
  pathMatch: 'full',
  redirectTo: () => {
    const auth = inject(AuthService);
    return auth.isLoggedIn() ? resolveLandingUrl(auth) : '/auth/login';
  },
},

// core/auth/utils/landing.ts
export function resolveLandingUrl(auth: AuthService): string {
  return auth.hasPermission('attendances:read') ? '/dashboard' : '/account/my-profile';
}
```

已收斂到 `/` 的呼叫點：`no-auth.guard.ts`、`login.ts`（`returnUrl` 的預設值）、`line-bind-callback.ts`、`error-403.ts`、`error-404.ts`、`app-logo.ts`。**新增任何「首頁」連結時比照辦理**；未來若 `/account/my-profile` 也加上權限，只需改 `resolveLandingUrl` 一處。

### 受權限控管的選單項目

側欄項目加 `requiredPermission` 即可（[data.ts](../Admin/src/app/layout/components/data.ts)），區段標題會由 `app-menu.ts` 的 `isTitleVisible()` 在整區皆不可見時自動隱藏，不需另外處理。**選單隱藏只是視覺層**：對應路由必須同時掛 `permissionGuard` + `data.permission`，頁內的寫入按鈕再用 `*appHasPermission` 包一層（三層都要，後端才是最終防線）。

### 依權限隱藏整個表格欄（Column-level Permission）

當「進得了頁面」與「看得到某一欄」是兩個權限時（例：[專案水位表](../Admin/src/app/features/admin/reports/pages/project-water-level/) 的「總專案水位」欄需 `reports-project-water-level:total`），**不要用 `*appHasPermission`**，改在 component 存一個 boolean 當單一真相：

```typescript
readonly canSeeTotal = inject(AuthService).hasPermission('reports-project-water-level:total');
```

```html
@if (canSeeTotal) { <th style="min-width: 180px">總專案水位</th> }
...
@if (canSeeTotal) { <td>…</td> }
...
<td [attr.colspan]="canSeeTotal ? 8 : 7" class="text-center text-muted py-4">
```

理由：`<th>` / `<td>` 之外，**空資料列的 `colspan` 也要跟著變**（漏改會跑版）。三處共用同一個欄位比「兩處用指令、第三處另外算」不易走鐘。後端必須同步把該欄回 `null`（見 [backend-design.md 欄位級權限](backend-design.md)）—— 前端隱藏只是視覺層。

### 依權限隱藏表單區塊（Section-level Permission）

當被管制的不是表格欄，而是**表單裡的一整段欄位**時（例：[員工管理](../Admin/src/app/features/admin/users/pages/user-form/) 的薪資與勞健保欄、薪資調整歷史，需 `payroll:read`），同樣在 component 存一個 boolean 當單一真相，但另有四件事必須一起處理：

```typescript
/** 受管制的控制項名單；與後端的抹除清單一一對應，新增欄位時兩邊都要改 */
const SALARY_CONTROLS = ['baseSalary', 'mealAllowance', /* … */] as const;

readonly canSeeSalary = this.authService.hasPermission('payroll:read');
```

1. **模板用 `@if` 包整段**。`@if` 不產生 DOM 節點，被包住的 `col-*` 直接從 `row g-3` 的 grid flow 消失，排版不受影響 —— 不需要調整 colspan 之類的補償。
2. **FormGroup 不要改成條件式建立**，改在 `ngOnInit` 對名單裡的控制項 `disable({emitEvent: false})`。條件式建立會讓 `patchValue`、各種 getter、submit 解構全都要加 null 判斷，改動面大且易漏；`disable()` 的副作用剛好都是想要的 —— disabled 控制項**不進 `form.value`、也不參與驗證**（否則隱藏欄位上的 `min` / `max` 會擋住存檔）。
3. **送出前明確剔除 payload key**（`for (const k of SALARY_CONTROLS) delete payload[k]`）。安全相關的欄位不倚賴 Angular 的隱含行為。若對應的後端端點是**整批替換**（送 `[]` 等於刪光），更要整個 key 不送而非送空陣列 —— 見 [backend-design.md 欄位級權限規則 6](backend-design.md)。
4. **連帶處理衍生輸出**：由被管制欄位算出的試算值（getter）要加早退 `if (!this.canSeeSalary) return null;`；會查外部端點的訂閱要條件式跳過（否則噴必然 403 的 XHR，且那支端點本身可能是反推原料的側門）；**PDF / Excel 匯出要連同該段的 `addPage()` 一起跳過**，只藏內容會留下一張只有頁首的空白頁。

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

#### ⚠ FormArray 的 track：索引綁定的列一律 track 控制項本體

跑 `FormArray.controls` 的 `@for`，能不能用 `track $index` 取決於**列內怎麼綁控制項**：

| 列內綁法 | 可用的 track | 說明 |
|---|---|---|
| `[formGroup]="$any(item)"`（**實例**綁定，§7 明細列表標準） | `track $index` 可 | 每次變更偵測都重新拿到正確的控制項實例 |
| `[formControlName]="$index"` / `[formGroupName]="i"`（**索引**綁定） | **必須** `track ctrl`（或穩定 `rowId`，見加班申請關聯專案） | 見下方原因 |

原因：`FormArray.removeAt()` **不會**觸發 `_onCollectionChange`，`FormGroupDirective` 因此不會重新解析各列的控制項；
而 `FormControlName.ngOnChanges` 只在 `_added === false` 時 `_setUpControl()`，所以 `name`（`$index`）改變也**不會**重新綁。
`track $index` 時 Angular 只會砍掉最後一列並沿用其餘 DOM，被沿用的列仍指向**刪除前**同索引的舊控制項 → 畫面值整批錯位；
若下拉選項還會排除其他列已選值（`availableUsers()` / `availableJobTitles()` / `availableProjects()`），錯位的值會被濾掉而**顯示空白**，
使用者改選也只會寫進已被移除的控制項（靜默遺失）。改 `track ctrl` 後 DOM 節點跟著控制項搬移，索引綁定自然對齊。

實例：簽核流程設定的「例外指定審核」名單與其「限定職稱」（[approval-flow.html](../Admin/src/app/features/admin/approvals/pages/approval-flow/approval-flow.html)）。

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
- [ ] 送出成功彈窗用 `<app-submit-success-modal>`（§11.6），未自寫 `<ng-template #successModal>`
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
