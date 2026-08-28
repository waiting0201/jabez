# 出勤打卡規則（四個打卡動作的前置條件）

> 打卡 UI 在 `Admin/src/app/features/dashboard/pages/dashboard/`（即時時鐘 + GPS）；
> 後端在 `Api/Handlers/AttendanceHandler.cs`。
> 打卡「提醒推播」是另一件事，見 [attendance-reminder.md](attendance-reminder.md)。

---

## 四個打卡動作的前置條件

| 動作 | 端點 | 前置條件 |
|---|---|---|
| 上班打卡 | `POST /attendances/clock-in` | 今日尚未打上班卡；**當下不在已核准請假時段內** |
| 下班打卡 | `POST /attendances/clock-out` | 已打上班卡、尚未打下班卡；**當下不在已核准請假時段內** |
| 加班開始 | `POST /attendances/overtime-start` | 今日尚未打加班開始卡；須帶**屬於自己**且日期為今日的 `approved` 加班申請單；**一般上班日須先打下班卡，休假日免下班卡**（見下節）。不受請假時段阻擋 |
| 加班結束 | `POST /attendances/overtime-end` | 已打加班開始卡、尚未打加班結束卡。不受請假時段阻擋 |

**請假時段阻擋**：`AttendanceHandler.EnsureNotOnLeaveAsync` 以半開區間 `[StartDate, EndDate)` 判定，
只套用在上下班打卡；訊息會帶出假別與時段。**加班打卡刻意不套用** —— 請假中仍可能被要求加班。

**銷假的影響（2026-08 新增）**：判定另加「該日無已核准銷假」條件 —— 已核准銷假的日子不再阻擋打卡，
支援挖空中間日的部分銷假。**銷假送簽期間仍會阻擋**（父單維持 `approved`，核准後才放行），
見 [leave-rules.md §銷假規則](leave-rules.md#銷假規則2026-08-新增)。同一條件亦套用於休假日免下班卡判定
（`GetLeavesOnDateAsync`）、出缺勤報表的請假合併（見下節）、以及打卡提醒的請假排除。
SQL 端的判定片段收斂於 `LeaveRevocationService.NotRevokedClause`，EF 端為 `GetApprovedRevokedDatesAsync`。

---

## 休假日加班免下班卡（2026-07 新增）

### 問題

員工全日請假 → 上下班打卡被請假時段擋掉 → 永遠沒有 `ClockOutTime` →
加班開始因「須先打下班卡」而永遠 disabled。主管臨時要求加班且加班單已核准也打不了卡。
國定假日 / 週末（本來就不打上下班卡）有同樣問題。

### 規則

當日有**已核准且屬於自己**的加班申請單、且尚未打過加班開始時：

- **休假日** → 免下班卡即可打「加班開始」；今日無打卡紀錄時直接建立「只含加班時間」的 `AttendanceRecord`
- **一般上班日** → 維持原規則（須先打下班卡），避免在正常工時內就打加班卡導致時數失真

### 「休假日」定義（兩者任一成立）

| 條件 | 判定方式 |
|---|---|
| (a) 行事曆休假日 | `CalendarDay.IsHoliday = true`；該年度行事曆**無資料**時退回「週六 / 週日」判定 |
| (b) 當日全日請假 | 已核准請假涵蓋**上午段 08:00–12:00** 與**下午段 13:00–17:00** |

(a) 沿用 `Api/Common/WorkCalendarHelper.IsHolidayAsync` —— 與請假日計算共用「有行事曆用 `IsHoliday`、
沒資料退回六日」的同一份規則，不另外寫一份。

> **排班制員工（`User.IsShiftWorker = true`，2026-08 新增）恆不符合 (a)**：六日與國定假日對其而言都是工作日，
> 故**沒有「休假日免下班卡」的放寬**，週六打「加班開始」前仍須先打下班卡。旗標以**打卡者本人**解析。
> 條件 (b) 全日請假的豁免不受影響。詳見 [leave-rules.md §排班制員工不扣假日](leave-rules.md#排班制員工不扣假日2026-08-新增)。

(b) 兩段各需被**某一張單**完整覆蓋，可由：
- 一張全日單（Day 單位存 `00:00–23:59`、HalfDay 單位存 `08:00–17:00`）滿足，或
- 「上午半天 + 下午半天」兩張單共同滿足

只請半天 → 不成立（仍須正常打下班卡）。時段常數見 `Api/Common/Constants.cs` 的 `WorkdayHours`。

> 刻意**不用 `LeaveRequest.Hours >= 8`** 判定：多日請假的 `Hours` 是「天數 × 8」，
> 跨日的 Hour 制請假也可能超過 8 小時卻只涵蓋今天一部分，會誤判。

### 前後端同源

`GET /attendances/today` 回傳 `canOvertimeWithoutClockOut` 旗標，與 `POST /attendances/overtime-start`
的放行判定共用 `AttendanceHandler.CanOvertimeWithoutClockOutAsync`。
**前端不自行重組規則**，只讀旗標決定按鈕 disabled 與提示文案。

### 「只含加班時間」的 AttendanceRecord

休假日加班會產生 `ClockInTime` / `ClockOutTime` 皆為 `NULL`、只有加班起訖的紀錄。已確認：

- 出缺勤報表：對 null 時間輸出空字串，無工時 / 異常欄位 → 正常顯示
- 加班報表：`ActualHours` 由加班起訖計算 → 休假日加班終於有實際時數
- 薪資計算：`PayrollReadService` 不讀 `AttendanceRecords` → 不受影響
- 打卡提醒：只排除 `ClockInTime` / `ClockOutTime` 已有值者 → 行為不變

---

## 登入時自動補卡（漏打的歷史紀錄）

補卡在**登入當下**執行（[AuthHandler.LoginAsync](../../Api/Handlers/AuthHandler.cs)），只處理 `RecordDate < 今天` 的紀錄，
今天的漏打不會被補（當天還有機會自己打）。沒有排程 —— 員工不登入就不會補。

| 情境 | 撈取條件 | 補上的時間 |
|---|---|---|
| 漏打下班卡 | `ClockInTime IS NOT NULL AND ClockOutTime IS NULL` | **該日上班打卡時間 + 9 小時**（見下表） |
| 漏打加班結束卡 | `OvertimeStartTime IS NOT NULL AND OvertimeEndTime IS NULL` | 加班開始時間 + 該張加班單的 `EstimatedHours` |

**下班補卡一律 +9 小時，不分上下午打卡**：

| 上班打卡時間 | 加值 | 理由 | 例 |
|---|---|---|---|
| 上午（`Hour < 12`） | **+9 小時** | 淨工時 8 小時 + 午休 1 小時 | 09:00 → 18:00 |
| 下午（`Hour >= 12`） | **+9 小時** | 同上，補卡不再區分是否跨午休 | 13:30 → 22:30 |

淨工時 8 小時 = `WorkdayHours.FullDayHours`，午休 1 小時 = `LunchEndHour - LunchStartHour`，
兩者都取自 [Constants.cs](../../Api/Common/Constants.cs) 的 `WorkdayHours`，不在 Handler 內寫死。

**補出來的下班時間會標記 `AttendanceRecord.IsClockOutAuto = true`**，出缺勤清單於「下班時間」欄位後
加掛 badge「系統補卡」（`bg-warning-subtle`），Excel 匯出則於時間後加註「（系統補卡）」，
以區分本人打卡與系統代打。旗標的清除時機：本人補打下班卡（`POST /attendances/clock-out`）、
或管理者在出缺勤清單編輯 Modal 改動下班時間（`PATCH /attendances/{id}`，僅在值真的改變時清除）。

補完後於登入頁跳 toastr warning 列出被補的日期（`auto_clock_out` / `auto_overtime_end` 回應欄位）。

> **2026-08 變更**：下班補卡時間原本是「該日 `SystemSetting.WorkEndTime`（預設 18:00）」，
> 改為依上班打卡時間推算，並新增 `IsClockOutAuto` 標記。
> 初版依午休界線分流（上午 +9 / 下午 +8），**2026-08-28 起統一為一律 +9**（含午休），
> 避免同一天早班／晚班的補卡工時不一致。
> `WorkStartTime` / `WorkEndTime` 自此**只服務打卡提醒的時點判斷**，不再參與補卡；
> 早到 / 晚到者的工時因此不再被統一壓成 18:00 下班。

**沒打上班卡則完全不會有紀錄** —— `AttendanceRecord` 只由「上班打卡」或「休假日加班開始」建立，
補卡也不會觸發（條件要求 `ClockInTime` 不為 null）。
但**下班提醒推播仍會發送**（提醒只看當日有無 `ClockOutTime`，見 [attendance-reminder.md](attendance-reminder.md)）。
該日在出缺勤報表中是否出現，取決於當天有沒有請假 —— 見下節。

---

## 出缺勤報表：打卡 ∪ 請假日（2026-08 新增）

### 問題
報表原本以 `AttendanceRecords` 為主表 `LEFT JOIN LeaveRequests`，主表沒有的日子就沒有列。
**全天請假的人不打卡 → 完全不出現在報表**，管理者查不到「今天誰請假」；
只有「有打卡又有請假」（例如上午請假、下午上班）才看得到請假欄。

### 規則
`GET /attendances` 改回傳「打卡紀錄 ∪ 當日請假日」的合併結果，
單一真相為 [AttendanceLeaveMerger](../../Api/Common/AttendanceLeaveMerger.cs)。合併粒度＝**(員工, 日期) 一列**：

| 當日狀況 | 報表呈現 |
|---|---|
| 有打卡 + 有請假 | 同一列：打卡時間 + 假別 + 當日請假時數 |
| 只有請假、完全沒打卡 | **請假虛擬列**（`Id = null`）：上下班時間留空 + 「請假」badge + 假別 + 當日時數，**不可編輯** |
| 只有打卡 | 原樣 |
| 沒打卡也沒請假 | **不產生列**（缺勤不在本報表範圍） |

- **不限全天**：半天請假又完全沒打卡的日子同樣產生虛擬列（顯示 4 小時），採 union 語意
- **同日多張假單**（例：上午事假 + 下午特休）合併為**一列**：`leaveHours` 加總、`leaves[]` 保留逐張顆粒度，
  相容欄位 `leaveType` / `leaveStartDate` / `leaveEndDate` 填第一張
- **逐日時數**走 [LeaveDayExpander](../../Api/Common/LeaveDayExpander.cs)（與銷假、請假送單同一份行事曆 + 半天/小時規則），
  非 SQL 端另寫；展開結果會裁切到查詢區間（產假 56 天等可能超出）
- **銷假**：該日已核准銷假即不算請假日，該日虛擬列消失（部分銷假可挖空中間日）
- **假日**：`LeaveDayExpander` 不展開假日 → 國定假日 / 六日不會產生虛擬列

### 為什麼不能純 SQL
逐日請假時數必須走 `LeaveDayExpander`（C# 的行事曆判定與半天編碼），SQL 端複製一份必然漂移。
故改為「區間全量載入 → 記憶體合併 → 記憶體切頁」，代價是**查詢區間必須有界**：
未指定起訖時回退近一年，跨度超過 `AttendanceLeaveMerger.MaxRangeDays`（400 天）直接擋件。
前端的月篩選因此不再提供「全部年份 / 全部月份」。

---

## 出差註記（2026-08 新增）

出差在外辦公時 GPS 不在公司屬正常，但清單看不出差異。故打卡頁（`features/dashboard`）在四顆打卡按鈕上方
加一個勾選框「出差（在外辦公）」，出缺勤清單於日期欄掛「出差」badge。

- **粒度＝整天一個旗標**：`AttendanceRecord.IsBusinessTrip`，不分上班卡 / 下班卡
- **四個打卡動作皆帶出**（`ClockActionRequest.IsBusinessTrip`），每次打卡**以送出的值覆寫**當日旗標
- **勾選框初始值來自 `GET /attendances/today`**（`TodayAttendanceDto.IsBusinessTrip`）：
  已標記出差的當日再次打卡不會被誤清；要取消出差就取消勾選後再打下一次卡
- **編輯表單不提供切換**：`UpdateAsync` 刻意不動 `IsBusinessTrip`，出差只由本人打卡時認列
- **請假虛擬列恆為 `false`**（無 `AttendanceRecord`）

---

## 逾 9.5 小時工時提示（2026-08 新增）

出缺勤清單於下班時間欄掛「超過 9.5 小時」badge，供管理者關注工時 / 加班合理性。

- **判定＝下班時間 − 上班時間 > 9.5 小時**（**含午休**，即實際在班跨度）
- **純前端 derived**：不寫 DB、不進 DTO，由 `attendance-report.ts` 的 `computeWorkHours()` / `isLongWorkday()`
  以清單既有的 `clockInTime` / `clockOutTime` 計算；門檻常數 `LONG_WORKDAY_HOURS = 9.5` 為單一真相，
  清單 badge 與 Excel 匯出共用
- **缺任一端或跨度非正值不顯示**（只打上班卡、或人工改到下班早於上班）
- **不影響薪資與加班時數計算**：加班費仍以加班申請單與加班打卡為準

---

## 出缺勤備註（2026-08 新增）

`AttendanceRecord.Remark`（nvarchar(500)），供管理者在出缺勤編輯 Modal 記錄修改原因（例：忘刷卡，經主管確認）。

- **只在編輯表單可見可填**：清單不新增備註欄，Excel 匯出亦不含使用者填寫的 Remark
  （匯出的「備註」欄是系統註記：請假（未打卡）／出差／超過 9.5 小時）
- 走既有的 `PATCH /attendances/{id}`（`UpdateAttendanceRequest.Remark`），受 `reports-attendance:write` 控管
- 空白字串一律正規化為 `null`；超過 500 字回 400

---

## 打卡權限（2026-08 新增）

在此之前，打卡是系統中唯一完全不受權限控管的模組：`AppRouter.GetRequiredPermission` 對 `["attendances", ..]` 一律回 `null`，任何登入者都能打卡，**也能改任何人的出缺勤紀錄**（`PUT/PATCH /attendances/{id}` 連擁有者比對都沒有，而這會直接影響薪資與加班費）。

### 三個權限碼

| Code | Id | Module | 對象 | 端點 |
|---|---|---|---|---|
| `attendances:read` | 37 | 出勤打卡 | 員工對自己 | `GET /attendances/today` |
| `attendances:write` | 38 | 出勤打卡 | 員工對自己 | `POST /attendances/clock-in` `clock-out` `overtime-start` `overtime-end` |
| `reports-attendance:write` | 42 | Reports | 管理者對別人 | `PUT/PATCH /attendances/{id}` |

`GET /attendances`（出缺勤報表列表）沿用既有的 `reports-attendance:read`（Id 41），不新增碼。

「員工對自己」與「管理者對別人」刻意分成兩組 —— 共用一組會變成「能打自己的卡＝能改全公司的卡」。規範見 [backend-design.md §3.4](../backend-design.md#34-權限表)。

> **Id 37/38/42 是重用的歷史空號**（37/38 於 2026-03 被 `SyncSeedDataWithDatabase` 刪除，42 原為已刪除的 `reports-leave:read`）。刻意不取 78+，因為 `PermissionHandler.CreateAsync` 以 `max(Id)+1` 配號，78 起可能已被 UI 建立的權限占用；重用低號可免疫撞號。

### 「誰能改」vs「能對誰改」

權限碼只管前者。`AttendanceHandler.UpdateAsync` 另套**部門可見性 scope**（`IProjectAccessResolver`），與 `GET /attendances` 同範圍 —— 讀得到才改得到，跨範圍回 403「您沒有權限修改此員工的出缺勤紀錄。」

### 既有角色的回填

`AddAttendanceClockPermissions` migration 以 raw SQL 回填，**上線後行為與現況完全相同**，差別只在「從此可管理」：

- `attendances:read` / `write` → **所有現有角色**（含 UI 建立的自訂角色，`INSERT … SELECT FROM Roles`）
- `reports-attendance:write` → **只給已擁有 `reports-attendance:read` 的角色**，精準維持「能進報表頁就能編輯」

> ⚠️ **營運 SOP：日後從 UI 新建的角色，預設不會勾到「出勤打卡」模組。** 建新角色時務必手動勾選 `attendances:read` + `attendances:write`，否則該角色的員工隔天無法打卡。（migration 只回填當下已存在的角色。）

> ⚠️ **權限異動不會即時生效**：JWT 的 `permissions` claims 只在 login / refresh 時從 DB 重讀，access token 有效期內（`Jwt__ExpiryMinutes`，預設 60 分）仍是舊的。調整打卡權限後請該員工重新登入。詳見 [authentication.md](../authentication.md)。

### 不受權限影響的兩條路徑

兩者都不經 `AppRouter`，所以沒有 `attendances:write` 的人仍會被影響 —— 已知的語意矛盾，不影響正確性：

- **登入時自動補下班卡**（`AuthHandler`，見上方章節）是伺服器端直寫 DB
- **LINE 打卡提醒**（`AttendanceReminderFunction`）走 TimerTrigger，會照樣推播給已無打卡權限的人

### 已知缺口（未處理）

`UpdateAsync` 沒有稽核軌跡：改了誰的卡、誰改的、原值為何都沒留。另案評估。

---

## 跨業務關聯

- **請假時段與假別規則** → [leave-rules.md](leave-rules.md)
- **加班申請走簽核流程** → [approval-flow.md](approval-flow.md)、[application-forms.md](application-forms.md)
- **打卡提醒推播** → [attendance-reminder.md](attendance-reminder.md)
- **端點清單** → [api-routes.md §出勤打卡](../api-routes.md#出勤打卡)
