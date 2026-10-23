# 天然災害停班（颱風假）

> 2026-10-23 新增。後端實作：`Api/Common/DgpaStatusParser.cs`（解析）、`DisasterPayDeduction.cs`（扣薪判定／請假退回計算）、
> `Api/Services/DisasterDayOffService.cs`（建立／修改／取消／公告同步）、`DisasterLeaveRecalculator.cs`（請假時數重算）、
> `DisasterPayrollService.cs`（薪資扣款天數與影響名單）、`Api/Services/Dapper/DisasterDayOffReadService.cs`（單一真相讀取）、
> `Api/Functions/DisasterDayOffFunction.cs`（排程）。

## 1. 法規立場

- 天然災害停班**不是國定假日**，不寫進 `CalendarDay`，不套用國定假日加倍工資、假日津貼、加班日別（`OvertimePayCalculator`）、排班配額（`ShiftScheduleValidator`）的任何規則。
- 勞工因天災未出勤**不得視為曠職**：出缺勤報表不產生缺勤列，改產生 `rowKind = disaster` 的列。
- **照常出勤者照平日計薪**：仍可打卡，停班時段內不判遲到／早退；超時才依既有加班申請走加班。
- 與 `SystemSetting.FlexibleWorkStartDate`（四週彈性工時切換日）無關，新舊制同樣生效。

## 2. 資料與工作地

- `DisasterDayOff`（Date, County, Segment, IsPaid, Source, SourceText, Status, 建立／取消者 Id + 姓名快照）；`(Date, County) WHERE Status='active'` 唯一。取消的列保留，供「不再自動重建」判定。
- `DisasterDayOffFetchLog`：每次抓取的頁面更新時間、結果摘要、待人工項（JSON），供去重與除錯。
- 縣市代碼 `TaiwanCounties`（22 縣市，前端 `shared/utils/taiwan-counties.ts` 同步）。
- **工作地解析**：`User.WorkCounty ?? Department.County ?? 沿 ParentId 往上最近一個有設定的部門 County ?? null`。null ＝ 不套用，列入 HR 通知的「未設定工作地」人數。SQL 單一真相：`WorkCountyResolver.EffectiveCountyCte`。
- 單一讀取入口：`IDisasterDayOffReadService.GetSegmentsAsync(userIds, from, to)` → `(UserId, Date) → full/am/pm + IsPaid`，所有消費點共用。

## 3. 給薪／不給薪

| | 給薪 | 不給薪 |
|---|---|---|
| 未出勤 | 不扣薪 | 扣日薪（底薪 ÷ 30）× 天數（全天 1、半天 0.5） |
| 停班時段有出勤（打卡區間重疊） | 照平日 | 照平日、不扣 |
| 停班時段已有核准請假 | **退回假單時數**（特休／補休回補） | 假單不動、照原假別計薪，不另扣 |
| 新送假單涵蓋該日 | 該時段不算請假（全天：working-days 不列；半天：送簽時重算） | 照常算請假日 |
| 非本人工作日（排班例休、到職前／離職後） | — | 不扣 |
| 整月育嬰留停被剔除薪資單者 | — | 不扣 |

- 預設值：`SystemSetting.DisasterDayOffPaidDefault`（預設 true）。自動抓取的紀錄套用預設，HR 可逐筆（某日某縣市）改。
- ⚠ 薪資是即時重算、無月結快照：**修改已發薪月份的給薪設定，會讓歷史薪資跟著變**（前端在修改過去月份時跳警示）。

### 請假重算（`DisasterLeaveRecalculator`，冪等）
- 只動工作日型假別（`LeaveDayExpander.WorkingDayLeaveTypes`）的 pending／approved 假單；產假之外的連續日曆天型（`parental_leave`、`ceremonial_festival`）不動。
- 基準 ＝ `Hours + DisasterAdjustedHours`，先還原再重扣；建立／改時段／改給薪／取消都走同一支。
- 每日剩餘時數：全天停班 → 0；上午／下午停班 → 整日假扣 4h、同半天的半天假 → 0、另一半天不變、小時假依重疊小時扣（`DisasterLeaveAdjuster`）。
- Hours 變 0 的單維持原狀態。補休假接著呼叫 `CompensatoryLotService.SyncUsageAsync`。
- 銷假核准重算 Hours 時（`LeaveRevocationService.ApplyAsync`）同樣扣除給薪颱風假，口徑一致。
- 觸發點：颱風假建立／修改／取消、新假單 Submit（`LeaveRequestHandler`）。

## 4. 人事行政總處公告自動抓取

- 來源：`https://www.dgpa.gov.tw/typh/daily/nds.html`（公開網頁；NCDR CAP feed 需會員，暫不採）。
- 排程：`DisasterDayOffFunction`，cron app setting `DisasterDayOffCron`（建議 `0 */10 * * * *`）。以頁面「更新時間：yyyy/MM/dd HH:mm:ss」去重（有成功紀錄的相同更新時間就略過，不寫紀錄、不通知）。
- 頁面結構：標題 `Header_YMD`（民國年，分行）、`<TABLE id="Table">` 每縣市一列，取該列最後兩個 TD（縣市名稱、狀態文字）；表頭列與備註列自然略過。縣市名稱容許台／臺。頁面解析不到任何縣市（改版）視為失敗，不誤判為「全部照常」。

### 解析規則（`DgpaStatusParser`，純函式）
「寧可漏套交人工，不可誤套」。狀態文字可含多個日期的多個句子，以「今天／明天／後天」切子句。

| 文字 | 結果 |
|---|---|
| 停止上班（無時段詞） | full |
| 上午…停止上班 | am |
| 下午（及晚上）…停止上班 | pm |
| 上午照常、下午停止上班 | pm |
| 照常上班；或只有「停止上課」 | 不建立 |
| 子句含鄉／鎮／區／村／里或「部分」「偏遠」等 | **partial：不套用，通知 HR** |
| 僅晚間停班、同子句又停又照常、缺日期詞、其他無法辨識 | **unknown：不套用，通知 HR** |

### 自動同步行為
- 新縣市公告 → `source = dgpa`、套用預設給薪 → 觸發請假重算 → 通知 HR。
- **HR 取消過的同日同縣市（cancelled 列存在）永不自動重建。**
- 公告後來改為照常、或時段與現有有效紀錄不同 → **不自動撤銷／修改**，列為待人工項通知 HR。
- 待人工項與上一次成功紀錄相同者不重複通知。
- 連續抓取失敗只留第一筆失敗紀錄。

### 人工交接條件（HR 需要處理）
部分鄉鎮停班、無法辨識的句型、公告改為照常（系統內仍有颱風假）、公告時段與系統不符、有同仁未設定工作地。

## 5. 各消費點行為

| 位置 | 行為 |
|---|---|
| `ExpectedWorkWindow` | 停班時段比照請假時段扣除（全天免出勤；半天跨午休正規化） |
| `AttendanceLeaveMerger` | 不產生缺勤列；停班時段不標未打卡；每列帶 `disasterSegment`／`disasterIsPaid`；本人工作日遇停班且無打卡無請假 → `rowKind = disaster`（Id = null，取代缺勤列，可含今天）；給薪退回的請假時數不顯示 |
| `AttendanceAutoClockService` | 走 ExpectedWorkWindow，全天停班日不補卡 |
| `AttendanceReminderService` | full → 上下班提醒都不推；am → 不推上班；pm → 不推下班（自訂時段者同）；12:55 交接提醒 full 不推、下班前「準備休假」pm 不推 |
| `AttendanceHandler` | 仍可打卡；全天／上午停班不判遲到；全天／下午停班不判早退、逾時 |
| `PayrollReadService` | 加入「颱風假扣薪」＝日薪（折減前）× 不給薪未出勤天數，計入實領扣項 |
| 不動 | `OvertimePayCalculator` 日別、國定假日加倍工資、假日津貼、`ShiftScheduleValidator`、`CalendarDay` |

## 6. 權限與 API

`disaster-days:read` / `disaster-days:write`（migration 以 Code 動態取號，**不回填角色**，由 Superadmin 指派給 HR，重新登入後生效）。`POST /disaster-days/fetch` 僅 Superadmin。`GET /me/disaster-day-off` 登入即可（輕量端點）。路由見 [api-routes.md](../api-routes.md)。

## 7. 上線前人工作業
各部門填「所在縣市」（總公司、海銀行、東發號須客戶告知）、必要時個人設工作地；權限指派給 HR；設定 app setting `DisasterDayOffCron`。
