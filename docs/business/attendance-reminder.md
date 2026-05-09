# 打卡提醒（TimerTrigger + LINE 推播）

## 功能範圍

- 每日上班前 2 分鐘、下班前 2 分鐘各一次，自動推播 LINE Flex Message 提醒員工打卡
- 無需前端介入：員工即使未登入系統，只要已綁定 LINE 即可收到
- 排程由 `AttendanceReminderFunction` TimerTrigger 觸發；cron 由 app setting `AttendanceReminderCron` 控制

## 觸發邏輯

1. Cron `%AttendanceReminderCron%`（UTC）進入 Function；預設 `0 */1 23,0-1,8-10 * * *`，僅在 7-9 Taipei（= UTC 23,0,1）與 16-18 Taipei（= UTC 8,9,10）時段每分鐘觸發
2. 透過 `Clock.Now`（台北時區）取得當前 `HH:mm`
3. 比對 `SystemSetting.WorkStartTime - 2min` / `WorkEndTime - 2min`；未命中直接 return
4. 週末（Saturday/Sunday）直接 return（cron 跨午夜時 day-of-week 無法在單一表達式中正確涵蓋週一至週五，故由 Service 端統一過濾）
5. 命中 → Dapper 查詢對象 → LINE 推播

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
- **batchStart 紀錄**：每次推播前先寫一筆 `Status='batchStart' / UserId=null`，即使 0 對象也能驗證排程有跑、命中時點。
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
- **限定時段**：cron 只在 7-9 / 16-18 Taipei 時段每分鐘觸發（共 6 小時/日），其他時段不進入 Function；對應預設 `WorkStartTime=09:00` / `WorkEndTime=18:00` 並留 1 小時前後緩衝。若上下班時間調整至此區間外，須同步修改 `AttendanceReminderCron`（Production：Function App → Configuration）
- **幂等性**：依賴 Azure Functions Timer 的 singleton lock（AzureWebJobsStorage blob lease）保證同一 cron tick 只觸發一次，加上 `RunOnStartup=false` 與 `IsPastDue` 跳過防止意外重複
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
- **打卡時段阻擋規則 + 請假中跳過** → [leave-rules.md](leave-rules.md) + [api-routes.md §出勤打卡](../api-routes.md#出勤打卡)
- **時區處理 Clock.Now** → [backend-design.md §11](../backend-design.md#11-時區處理重要)
- **AttendanceReminderLog Entity** → [database-schema.md](../database-schema.md)
- **管理頁列表 UI** → [docs/frontend-design.md](../frontend-design.md)
