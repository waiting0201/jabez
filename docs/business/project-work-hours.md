# 專案工時佔比與工時總表（2026-10）

客戶要知道每位員工的工時花在哪些專案上：**下班打卡時填專案工時佔比**，再由報表〈工時總表〉彙總。

## 下班打卡的專案佔比

- **必填**：新舊制員工一律適用。打下班卡（`POST /attendances/clock-out`）須帶 `projectAllocations: [{ projectId, percentage }]`，驗證不過整筆下班卡不成立（回 400，打卡紀錄不變）。
- 驗證規則（單一真相 `Api/Common/ProjectAllocationValidator.cs`）：
  - 至少 1 個專案，同一專案不可重複。
  - 每筆佔比須 > 0 且 ≤ 100，且最多 2 位小數（對應 `decimal(5,2)`，避免存入時被四捨五入而使合計不是 100）。
  - **合計須精確等於 100%**（decimal 比對）。
  - 專案須存在且為進行中（`active`），已結案的專案不可選。
- 分配與下班卡在**同一次 SaveChanges** 寫入，不會出現「有下班卡、沒分配」的半套資料（系統補卡除外，見下）。
- 專案不限本人部門（可跨部門支援），前端專案清單取全部未結案專案。

## 當日修改、隔天鎖定

- `PUT /attendances/today/project-allocations`（`attendances:write`）：整批替換，驗證規則同上。
- 只能改**自己、今天、已打下班卡**的那一筆；尚未打下班卡回 400。端點只認「今天」，隔天起即鎖定（唯一例外見下節）。
- 回傳與 `GET /attendances/today` 相同的 DTO，其中 `projectAllocations` 依 `SortOrder` 排序（無則 `[]`）。

## 系統補下班卡日：進系統強制補填（2026-10-16）

登入時 `AttendanceAutoClockService` 補上的下班卡（`IsClockOutAuto = 1`）沒經過下班打卡視窗，所以沒有佔比。
這些日子在**下次進入系統**時強制補填：

- **範圍**：本人、`IsClockOutAuto = 1`、有下班時間、**`RecordDate ≥ 2026-10-16`（上線日，`ProjectAllocationValidator.StartDate`）**、早於今天、尚無任何佔比。上線前的舊資料不追溯，工時總表仍列「未分配」。
- `GET /attendances/missing-project-allocations`（`attendances:read`）列出待補日；`PUT` 同路徑（`attendances:write`）body `{ attendanceIds, projectAllocations }`，同一份佔比可一次套多天，驗證規則同 clock-out。
- **只能補一次**：每個 id 都必須仍符合上述範圍（尚無佔比），否則整批 400 —— 不能藉此改歷史已填的佔比，「隔天鎖定」只對系統補卡日開這一次口。判準單一真相為 `AttendanceHandler.MissingAllocationDays`，GET 與 PUT 共用。
- 前端：`MainLayout` 進入時（登入後、重新整理頁面皆會）由 `MissingAllocationPromptService` 查清單，有資料就**逐日**跳出無法關閉的下班打卡視窗（無 X、無取消、`backdrop: 'static'` + `keyboard: false`），視窗可勾「其餘 N 天套用相同佔比」一次填完。
  - 送出失敗：錯誤 toast 後重開同一天，不放行。
  - 查清單失敗、或沒有任何可選專案：放行（不因一次網路錯誤或主檔問題把人鎖死），下次進系統再問。
  - 無 `attendances:write`（不打卡的角色）不查不跳。

## 工時總表（`reports-work-hours:read`）

- 路由：`GET /reports/work-hours`（員工 × 專案彙總）、`GET /reports/work-hours/daily`（逐日明細，供 Excel 第二張工作表）。
- 參數：`dateFrom` / `dateTo`（缺省為本月，跨度上限 400 天）、`departmentId`、`projectId`。部門可見性套用 `IProjectAccessResolver`（員工所屬部門），Superadmin 帳號不列入。
- **計算方式（刻意簡化）**：
  - 母體＝區間內**有下班時間**（`ClockOutTime IS NOT NULL`）的打卡紀錄。
  - 專案工時 ＝ **每天 8 小時 × 佔比 ÷ 100**（常數 `WorkHoursReportHandler.HoursPerDay = 8m`）。
  - **不使用實際上下班時間，也不扣請假**（請半天假的日子同樣以 8 小時計，屬業務假設，有異議再調）。
  - **加班不併入**，只算正常上下班。
- **未分配**：有下班時間卻沒有專案分配的紀錄（上線前的舊資料、系統補下班卡後本人尚未進系統補填者）整天 8 小時歸入「未分配」（`projectId = null`）。帶 `projectId` 篩選時不列未分配。
- 彙總只列工時、不列天數（2026-10-16 決議移除，天數在多專案分攤時會重複計算、易誤讀）；逐日明細的 `isAutoClockOut` 標示該日下班卡是否為系統補卡。
- 排序：專案代號自然排序（英文前綴 → 數字段數值）、未分配最後，再依部門、員工姓名。

## 權限

- 新權限碼 `reports-work-hours:read`（Module 沿用 `reports-overtime:read`，名稱「工時總表」），由 migration `AddAttendanceProjectAllocations` 以 raw SQL 冪等新增（Id 優先取 79，被占用則 max+1）。
- **刻意不回填任何角色**：上線後由 Superadmin 到角色管理指派，並請該員**重新登入**（權限在 JWT 內）。
- 打卡與當日修改沿用 `attendances:write`，不新增碼。

## 相關資料表

- `AttendanceProjectAllocation`：FK → `AttendanceRecords` **Cascade**、→ `Projects` **NO_ACTION**；刪除專案時 `ProjectHandler.DeleteAsync` 會阻擋（已被工時分配引用）。見 [database-schema.md](../database-schema.md)。
