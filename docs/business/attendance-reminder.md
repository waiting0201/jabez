# 打卡提醒（TimerTrigger + LINE 推播）

## 功能範圍

- 每日上班前 2 分鐘、下班前 2 分鐘各一次，自動推播 LINE Flex Message 提醒員工打卡
- 無需前端介入：員工即使未登入系統，只要已綁定 LINE 即可收到
- 排程由 `AttendanceReminderFunction` TimerTrigger 觸發；cron 由 app setting `AttendanceReminderCron` 控制

## 觸發邏輯

1. Cron `%AttendanceReminderCron%`（UTC）進入 Function；預設 `0 */1 23,0-1,8-10 * * *`，僅在 7-9 Taipei（= UTC 23,0,1）與 16-18 Taipei（= UTC 8,9,10）時段每分鐘觸發
2. `IsPastDue=true` **不 return**，只記 `LogWarning` 後照常執行（見下方「時間窗 + 冪等」）
3. 透過 `Clock.Now`（台北時區）取得當前時間
4. 判斷是否落在**提醒時間窗**內：`[WorkStartTime − 2min, +10min)` → `clockIn`、`[WorkEndTime − 2min, +10min)` → `clockOut`；都未命中直接 return
5. 週末（Saturday/Sunday）直接 return（cron 跨午夜時 day-of-week 無法在單一表達式中正確涵蓋週一至週五，故由 Service 端統一過濾）
6. **冪等閘**：查 `AttendanceReminderLogs` 今天這一槽（以 `TargetTimeTaipei` 區分上/下班）是否已有 `batchStart`，有就 return
7. 命中 → 先寫 `batchStart` → Dapper 查詢對象 → LINE 推播

### 時間窗 + 冪等（2026-08 重構，重要）

原本第 4 步是「`Clock.Now` 的 `HH:mm` 字串**精確等於** `WorkStartTime − 2min`」，加上 Function 端 `IsPastDue` 直接 return。這個組合對延遲零容忍，正式站因此出過兩類事故：

| 症狀 | 實際紀錄 | 原因 |
|---|---|---|
| **整天不發** | 2026-07-06、2026-08-06 的上班提醒 batchStart 為 0 筆 | Flex Consumption 冷啟動讓 08:58 的 tick 延後執行，`HH:mm` 已跳到 08:59 → 不命中；或被 `IsPastDue` 直接跳過 |
| **重複推播** | 2026-07-13 / 07-17 / 07-27 同一槽兩個 BatchId（08:58 與 08:59） | 同一個 occurrence 被兩個實例各跑一次，兩者相隔約 60 秒 |

修正方式是把「準時」的責任從平台移到程式：

- **時間窗**（`WindowMinutes = 10`）吸收冷啟動延遲 —— 窗內任何一次 tick 都算命中。窗的尾端（09:08 / 18:08 Taipei）仍落在 cron 涵蓋的時段內
- **`batchStart` 冪等閘**收斂成一天一槽一次 —— 窗內後續的 tick、以及同 occurrence 的第二個實例都會看到已存在的 `batchStart` 而 return
- 因為有冪等閘，`IsPastDue` 補跑變成安全操作，不再提前 return
- 手動觸發（`ForceRunAsync`）同樣寫 `batchStart`，所以當天手動推過之後排程不會再重複打擾員工
- 冪等查詢失敗一律當作「還沒發過」→ 寧可重複推播，也不要因為 log 表出狀況而整天不發

## 對象過濾條件（Dapper SQL）

- `User.LineUserId` 不為 null 且不為空字串
- `User.IsSuperAdmin = 0`
- `User.Status = 'active'`
- 未離職（`ResignDate` 為 null 或 > 今日）
- **非請假中**：今日不落在任何 `LeaveRequest.ApprovalStatus='approved'` 範圍內
- **未打卡**：上班提醒排除今日 `AttendanceRecord.ClockInTime` 已有值者；下班提醒排除 `ClockOutTime` 已有值者

## 手動觸發（除錯）

`POST /admin/attendance-reminder/run?type=clockIn|clockOut`（僅 Superadmin）
繞過時點與週末檢查，強制對符合條件員工推播；其餘過濾條件保留。回傳 `{ type, recipientCount, pushedCount, failureCount, batchId }`。

## 推播紀錄持久化

每次排程命中時點 / 手動觸發都會寫入 `AttendanceReminderLogs` 資料表，供前端「打卡提醒紀錄」頁查詢：

- **BatchId 串聯**：每次 `RunAsync` 開頭產生一個 `Guid`，同一次 tick 的所有紀錄共用。
- **batchStart 紀錄**：每次推播前先寫一筆 `Status='batchStart' / UserId=null`，即使 0 對象也能驗證排程有跑、命中時點。**寫入時機必須早於收件人查詢**（2026-08 調整）——它同時是冪等閘的依據，也讓「收件人 SQL 炸掉」與「排程根本沒觸發」在紀錄上可以區分；人數於查詢完成後以 UPDATE 補回 `UserNameSnapshot='recipientCount=N'`。收件人查詢若丟例外，會補一筆 `Status='failure' / ErrorCategory='system_error'`（`ErrorMessage` 前綴「收件人查詢失敗：」）。
- **逐筆推播紀錄**：對每位推播對象寫一筆 success/failure，含 `LineUserIdSnapshot / UserNameSnapshot`（歷史快照，員工解綁/離職後仍可查）、`HttpStatusCode`、`DurationMs`、`ErrorCategory`（`not_friend / token_invalid / rate_limited / network_error / unknown / system_error`）、`ErrorMessage`（截斷至 500 字）。
- **Dapper INSERT**：使用 `IDbConnection` 直接 INSERT，避免 EF ChangeTracker 在迴圈中累積污染；寫入失敗只記 `LogError`，**絕不 throw**，不影響推播主流程。
- **資料保留**：本次未實作清理機制；保守估每年 ~100K rows，仍在 SQL 可 sustain 範圍。未來可加 `CleanupAttendanceReminderLogsFunction` TimerTrigger 月清 6 個月前資料。

## LineService 推播失敗分類

`LineService.PushMessageAsync` 回傳 `PushResult(Success, HttpStatusCode, ErrorCategory, ErrorMessage)`：

| ErrorCategory | 觸發條件 | Log Level |
|---|---|---|
| `not_friend` | 400 + body 含 "hasn't added" 或 "blocked by the user" | LogError |
| `token_invalid` | 401 / 403 | LogCritical（整個推播管道失效） |
| `rate_limited` | 429 retry 後仍失敗 | LogWarning |
| `network_error` | `HttpRequestException` / `TaskCanceledException` | LogError |
| `unknown` | 其他非 2xx | LogWarning |
| `system_error` | AttendanceReminderService 迴圈內非預期例外 | LogError |

`ApprovalNotificationService` 6 處呼叫不取 `PushResult`，編譯仍相容。

## 設計決策

- **Cron Timezone**：UTC 觸發 + 內部 `Clock.Now` 比對，不依賴 `WEBSITE_TIME_ZONE` / `TZ` 環境變數，相容 Linux Consumption Plan
- **限定時段**：cron 只在 7-9 / 16-18 Taipei 時段每分鐘觸發（共 6 小時/日），其他時段不進入 Function；對應預設 `WorkStartTime=09:00` / `WorkEndTime=18:00` 並留 1 小時前後緩衝。若上下班時間調整至此區間外，須同步修改 `AttendanceReminderCron`（Production：Function App → Configuration）。⚠️ 調整時記得**時間窗尾端**（上/下班時刻 + 8 分）也必須落在 cron 涵蓋範圍內
- **幂等性**：**不依賴** Azure Functions Timer 的 singleton lock —— 正式站（Flex Consumption）實測會出現同一 occurrence 被兩個實例各跑一次。真正的去重靠 `AttendanceReminderLogs` 的 `batchStart` 查詢（見上方「時間窗 + 冪等」）
- **可觀測性**：`Program.cs` 需保留 `AddApplicationInsightsTelemetryWorkerService()` + `ConfigureFunctionsApplicationInsights()`，否則 isolated worker 的 `ILogger` 輸出不會進 App Insights，排程異常時只能靠 `AttendanceReminderLogs` 反推（2026-08 之前正式站即為此狀態）
- **成本**：Consumption Plan 每月約 10,800 次執行（限定時段後），遠低於免費額度（實質成本 0）

## 涉及元件

| 元件 | 說明 |
|------|------|
| `AttendanceReminderFunction` | TimerTrigger entrypoint |
| `IAttendanceReminderService` / `AttendanceReminderService` | 時點判斷 + 推播協調，含 BatchId 與 SafeWriteLogAsync |
| `IAttendanceReminderReadService` / `AttendanceReminderReadService` | Dapper 查詢符合條件的員工 |
| `AttendanceReminderRecipientDto` | `(UserId, LineUserId, UserName)` |
| `AttendanceReminderLog` Entity + `AttendanceReminderLogs` 資料表 | 推播紀錄持久化（BatchId、ErrorCategory、Snapshot 欄位） |
| `IAttendanceReminderLogReadService` / `AttendanceReminderLogReadService` | Dapper 查詢列表 / 詳情 / 批次 / 統計 |
| `AttendanceReminderLogDto` / `AttendanceReminderLogStatsDto` | 列表項目與統計卡資料 |
| `AttendanceReminderLogHandler` | 4 個 GET 端點（list / stats / batches / by id） |
| `PushResult` record | LINE 推播結果（含 ErrorCategory / HttpStatusCode） |
| `LineFlexMessageBuilder.BuildAttendanceReminderMessage` | 品牌綠 Flex Message 模板 |
| `AttendanceReminderAdminHandler` | 手動觸發 HTTP 端點（Superadmin） |
| 前端 `attendance-reminder-logs/` feature | 列表頁（含手動觸發按鈕、3 統計卡、7 天趨勢、6 維篩選）+ 批次詳情頁 |

---

## 跨業務關聯

- **LINE 推播管道 / 失敗分類** → [line-integration.md](line-integration.md)
- **打卡動作前置條件（含休假日加班免下班卡）** → [attendance-clock-rules.md](attendance-clock-rules.md)
- **打卡時段阻擋規則 + 請假中跳過** → [leave-rules.md](leave-rules.md) + [api-routes.md §出勤打卡](../api-routes.md#出勤打卡)
- **時區處理 Clock.Now** → [backend-design.md §11](../backend-design.md#11-時區處理重要)
- **AttendanceReminderLog Entity** → [database-schema.md](../database-schema.md)
- **管理頁列表 UI** → [docs/frontend-design.md](../frontend-design.md)
