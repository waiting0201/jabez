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
（`GetLeavesOnDateAsync`）、出缺勤報表的請假欄 LEFT JOIN、以及打卡提醒的請假排除。

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
| 漏打下班卡 | `ClockInTime IS NOT NULL AND ClockOutTime IS NULL` | **該日上班打卡時間 + 8 或 9 小時**（見下表） |
| 漏打加班結束卡 | `OvertimeStartTime IS NOT NULL AND OvertimeEndTime IS NULL` | 加班開始時間 + 該張加班單的 `EstimatedHours` |

**下班補卡的加值以「上班打卡是否在午休前」決定**（午休界線 = `WorkdayHours.LunchStartHour` 12:00）：

| 上班打卡時間 | 加值 | 理由 | 例 |
|---|---|---|---|
| 上午（`Hour < 12`） | **+9 小時** | 工時跨越午休 12:00–13:00，需含 1 小時不計薪的休息 | 09:00 → 18:00 |
| 下午（`Hour >= 12`） | **+8 小時** | 不跨午休，淨工時即實際在場時間 | 13:30 → 21:30 |

淨工時 8 小時 = `WorkdayHours.FullDayHours`，午休 1 小時 = `LunchEndHour - LunchStartHour`，
兩者都取自 [Constants.cs](../../Api/Common/Constants.cs) 的 `WorkdayHours`，不在 Handler 內寫死。

**補出來的下班時間會標記 `AttendanceRecord.IsClockOutAuto = true`**，出缺勤清單於「下班時間」欄位後
加掛 badge「系統補卡」（`bg-warning-subtle`），Excel 匯出則於時間後加註「（系統補卡）」，
以區分本人打卡與系統代打。旗標的清除時機：本人補打下班卡（`POST /attendances/clock-out`）、
或管理者在出缺勤清單編輯 Modal 改動下班時間（`PATCH /attendances/{id}`，僅在值真的改變時清除）。

補完後於登入頁跳 toastr warning 列出被補的日期（`auto_clock_out` / `auto_overtime_end` 回應欄位）。

> **2026-08 變更**：下班補卡時間原本是「該日 `SystemSetting.WorkEndTime`（預設 18:00）」，
> 改為依上班打卡時間推算（上午 +9 / 下午 +8），並新增 `IsClockOutAuto` 標記。
> `WorkStartTime` / `WorkEndTime` 自此**只服務打卡提醒的時點判斷**，不再參與補卡；
> 早到 / 晚到者的工時因此不再被統一壓成 18:00 下班。

**沒打上班卡則完全不會有紀錄** —— `AttendanceRecord` 只由「上班打卡」或「休假日加班開始」建立，
因此該日在出缺勤報表中是整列不存在（不是顯示缺勤），補卡也不會觸發（條件要求 `ClockInTime` 不為 null）。
但**下班提醒推播仍會發送**（提醒只看當日有無 `ClockOutTime`，見 [attendance-reminder.md](attendance-reminder.md)）。

---

## 跨業務關聯

- **請假時段與假別規則** → [leave-rules.md](leave-rules.md)
- **加班申請走簽核流程** → [approval-flow.md](approval-flow.md)、[application-forms.md](application-forms.md)
- **打卡提醒推播** → [attendance-reminder.md](attendance-reminder.md)
- **端點清單** → [api-routes.md §出勤打卡](../api-routes.md#出勤打卡)
