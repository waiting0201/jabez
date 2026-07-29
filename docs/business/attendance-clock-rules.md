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

## 跨業務關聯

- **請假時段與假別規則** → [leave-rules.md](leave-rules.md)
- **加班申請走簽核流程** → [approval-flow.md](approval-flow.md)、[application-forms.md](application-forms.md)
- **打卡提醒推播** → [attendance-reminder.md](attendance-reminder.md)
- **端點清單** → [api-routes.md §出勤打卡](../api-routes.md#出勤打卡)
