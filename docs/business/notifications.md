# 通知系統清單

本文件為系統所有 **Email** 與 **LINE** 通知的單一真相來源。涵蓋簽核流程通知、財務通知、帳號通知、薪資明細與打卡提醒。

> 修改任何通知行為時，**必須同步更新此文件**。

---

## 1. 概述

### 共用基礎服務

| 用途 | 服務 | 介面 |
|------|------|------|
| Email 寄送 | `Api/Services/EmailService.cs` | `IEmailService.SendAsync(to, subject, htmlBody)` |
| LINE 推播 | `Api/Services/LineService.cs` | `ILineService.PushMessageAsync(userId, message)` |
| LINE Flex Message 模板 | `Api/Services/LineFlexMessageBuilder.cs` | 9 個 `Build*Message` static 方法 |
| 簽核通知協調 | `Api/Services/ApprovalNotificationService.cs` | `IApprovalNotificationService` (8 個 method) |
| 打卡提醒 | `Api/Services/AttendanceReminderService.cs` | `IAttendanceReminderService` |

### 失敗策略

- **Email**：直接呼叫 SMTP（同步）。失敗以 `LogWarning` 紀錄，不影響業務流程。
- **LINE**：失敗以 `LogWarning`/`LogError` 紀錄；`PushResult.ErrorCategory` 分類「未加好友」/「已封鎖」/「其他」。Email 與 LINE 失敗互不影響。

### 站內鈴鐺（即時輪詢 + Toast）

除 Email / LINE 外，前端右上角鈴鐺提供**站內準即時通知**：

- **資料來源**：`GET /me/notification-counts` 回 `{approvals, myRequests, recentApprovals}`。
- **輪詢**：前端 `NotificationService.startPolling()`（由 `MainLayout` 啟停）每 **60 秒**呼叫一次更新 signal；鈴鐺紅點與 dropdown 明細同步更新（Angular signal 精準更新，**畫面不刷新**）。分頁切到背景（`document.hidden`）時**暫停發送**以省 Azure Functions 請求，切回前景立即補抓一次。
- **Toast（ngx-toastr）**：比對基準後跳出兩種提示：
  - **待我簽核增加** → `您有 N 件新的待簽核`（純前端比對 `approvals` 總和增量，零後端依賴）。
  - **我的單被核准** → `您有 N 件申請已核准`。因 `myRequests`（pending/returned）偵測不到核准（核准是離開 pending），後端 `recentApprovals` 額外回傳「最近 10 分鐘內被核准的我的單」`[{type, id, approvedAt}]`（`NotificationReadService.GetRecentApprovedMyRequestsAsync`：9 種父表 `ApprovalStatus='approved'` JOIN 多型 `ApprovalRecords` 取核准時間）。前端以 `localStorage` 記錄最後已提示 `approvedAt` 去重，**後端無狀態、無「已讀」資料表**。
- **首次** refresh 只設基準不跳 toast，避免登入瞬間洗版。

> 涉及檔案：`Admin/.../notifications/services/notification.service.ts`、`Admin/.../layout/main-layout/main-layout.ts`、`Api/Handlers/NotificationHandler.cs`、`Api/Services/Dapper/NotificationReadService.cs`、`Api/Models/Dtos/NotificationDtos.cs`。

---

## 2. 系統開關

於 `SystemSettings` 提供兩個全域開關（PATCH `/settings`），預設皆為 `true`：

| 欄位 | 預設 | 控制範圍 |
|------|:----:|---------|
| `ApprovalEmailEnabled` | `true` | 簽核流程相關 8 種 Email（待審核 / 結果 / 撥款 / 退款 / 財務） |
| `ApprovalLineEnabled` | `true` | 簽核流程相關 8 種 LINE 推播（範圍同上） |

**不受開關影響**（永遠寄送）：
- 帳號通知（`UserHandler.SendCredentialsAsync`）
- 薪資明細（`PayrollHandler.SendSlipsAsync`）
- 打卡提醒 LINE 推播（`AttendanceReminderService`）

設定位置：前端 `/admin/settings` → 「通知設定」卡片。

實作位置：`ApprovalNotificationService.ReadNotificationFlagsAsync()`，每個 `Notify*Async` 方法開頭讀取一次後分別守衛 Email / LINE 呼叫。

---

## 3. Email 通知清單（共 9 種）

### 3.1 簽核流程通知（7 種，由 `ApprovalNotificationService` 提供）

| # | Method | 主旨範本 | 收件人 | 觸發時機 | 主要呼叫位置 |
|---|--------|---------|--------|---------|------------|
| 1 | `NotifyReviewersAsync` | `[待審核] {label} #{id} — {申請人}` | 該步驟所有符合條件的審核者（依 JobTitle / Department / DirectSupervisor） | 申請送出 / 前一步核准後 | `ApprovalTaskHandler.cs` 通知下一步、各 Handler 的 `Submit*Async` |
| 2 | `NotifyApplicantAsync` | `[已核准/已退回/已拒絕] 您的{label} #{id} {tag}`；**追加預支**時 `{label}` 後加註批次 —— `您的預支申請（第 2 次追加） #12 已拒絕`，拒絕另加「原預支單維持核准」 | 申請人 | 審核動作後（approved / returned / rejected） | `ApprovalTaskHandler.cs` 「最終核准」「退回」「拒絕」分支；批次文案由 `contextLabel` 參數帶入（`roundNo > 1` 時才有值） |
| 3 | `NotifySpecificReviewerAsync` | `[待審核] {label} #{id} — {申請人}（指定 / 升級 / 代理 審核）` | 指定 / 升級 / 代理審核者 | 升級觸發、指定審核流程啟動、下一位指定審核者 | 各 Handler `Submit*Async`、`ApprovalTaskHandler.cs` (designated 自動代簽 while-loop) |
| 4 | `NotifyApplicantPaidAsync` | `[已撥款] 您的{label} #{id} 已撥款 — {amount} 元（第 N/M 期）` | 申請人 | **每筆 installment** 的 PaidAt 從 null → 有值（分期撥款情境下每筆推一次，標題附「第 N/M 期」）；無分期時退化為單筆通知 | `PaymentRequestHandler` / `AdvanceRequestHandler` / `TravelRequestHandler` / `TravelPaymentRequestHandler` 的 `UpsertInstallmentsAsync` |
| 5 | `NotifyApplicantRefundedAsync` | `[已退款] 您的{label} #{id} 退款已匯款 — {amount} 元` | 申請人 | 財務設定 RefundedAt 從 null → 有值 | `AdvanceRequestHandler` / `TravelRequestHandler` |
| 6 | `NotifyFinanceDeptAsync` | `[可撥款] {label} #{id} 已核准 — {申請人}` | 財務部（Department.Code = `FIN`）全員 | 請款 / 預支 / 出差預支 / 出差請款 最終核准後 | `ApprovalTaskHandler.cs:921, 957`（兩處最終核准分支，受 `IsFinanceApplicationType` 守衛） |
| 7 | `NotifyFinanceRefundAsync` | `[需匯款] 預支申請 #{id} 沖銷超額 — 差額 {金額} 元` | 財務部全員 | 預支沖銷核准且金額超過預支金額 | `ApprovalTaskHandler.CloseAdvanceRequestAsync` |
| 8 | `NotifyFinanceTravelRefundAsync` | `[需匯款] 出差申請 #{id} 沖銷超額 — 差額 {金額} 元` | financial 部全員 | 出差沖銷核准且金額超過出差金額 | `ApprovalTaskHandler.CloseTravelRequestAsync` |
| 9 | `NotifyFinanceUpcomingPaymentsAsync` | `[撥款提醒] 您有 N 筆預計撥款日將屆` | 財務體系部門（AC/FIN/Jabez HQ/CEO）全員 | 每日 09:00 (Taipei) TimerTrigger 自動跑，或 Superadmin 手動觸發 | `PaymentReminderService` + `PaymentReminderFunction` |

### 3.2 職務代理人通知（2 種）

| # | 來源 | 主旨 | 收件人 | 觸發時機 |
|---|------|------|--------|---------|
| 11 | `ApprovalNotificationService.NotifyLeaveAgentAsync` | `[職務代理] 您被指定為 {申請人} 的職務代理人` | 請假申請指定的 `AgentUserId`（職務代理人） | 請假**送出時**（含 Superadmin 自動核准）；僅知會、不參與簽核。受 `ApprovalEmailEnabled` 開關；目前僅 Email。（2026-07 新增） |
| 12 | `ApprovalNotificationService.NotifyLeaveRevocationAgentAsync` | 全銷 → `[職務代理] {申請人} 已銷假，代理職務解除`；部分銷 → `[職務代理] {申請人} 已部分銷假，代理期間調整` | 原請假單的 `AgentUserId` | **銷假核准時**（含 Superadmin 自動核准）；信中列出被取消的日期與剩餘請假時數。受 `ApprovalEmailEnabled` 開關；目前僅 Email。（2026-08 新增） |

> **銷假申請（`leave_revocation`）的審核通知**走通用簽核通知（待審核 / 審核結果），申請類型名稱為「銷假申請」，摘要格式為「取消{假別} N 天 / M 小時（MM/dd、MM/dd…）」。

### 3.3 帳號通知（1 種）

| # | 來源 | 主旨 | 收件人 | 觸發時機 |
|---|------|------|--------|---------|
| 9 | `UserHandler.SendCredentialsAsync` | `帳號通知 — 請登入並修改密碼` | 新員工 Email | 管理員手動點擊「寄出帳號通知」（密碼重設為生日 yyyyMMdd、`MustChangePassword=true`） |

### 3.4 薪資明細（1 種）

| # | 來源 | 主旨 | 收件人 | 觸發時機 |
|---|------|------|--------|---------|
| 10 | `PayrollHandler.SendSlipsAsync` | `薪資明細 — {year} 年 {month} 月` | `User.SendPaySlip = true` 且有 Email 的員工 | 管理員於薪資頁面手動觸發 |

> **計數說明**：簽核流程 8 種 + 帳號 1 種 + 薪資 1 種 = 「Email 共 10 種端點」；簽核流程內部的 `NotifyApplicantPaidAsync` / `NotifyApplicantRefundedAsync` 雖共用同一個 method，但分別覆蓋撥款與退款情境。

---

## 4. LINE 通知清單（共 9 種 Flex Message）

定義於 `Api/Services/LineFlexMessageBuilder.cs`，每個 Build 方法回傳 LINE Flex Message JSON。

### 4.1 簽核流程相關（8 種，與 Email 配套）

| # | Build 方法 | 用途 | 收件人 | 配套 Email | 呼叫於 |
|---|-----------|------|--------|-----------|--------|
| 1 | `BuildReviewerMessage` | 待審核通知 | 已綁 LINE 的審核者 | `NotifyReviewersAsync` | 同 method |
| 2 | `BuildApplicantResultMessage` | 審核結果（核准 / 退回 / 拒絕） | 已綁 LINE 的申請人 | `NotifyApplicantAsync` | 同 method |
| 3 | `BuildSpecificReviewerMessage` | 指定 / 升級 / 代理審核者 | 已綁 LINE 的特定審核者 | `NotifySpecificReviewerAsync` | 同 method |
| 4 | `BuildApplicantPaidMessage` | 撥款完成通知 | 已綁 LINE 的申請人 | `NotifyApplicantPaidAsync` | 同 method |
| 5 | `BuildApplicantRefundedMessage` | 退款完成通知 | 已綁 LINE 的申請人 | `NotifyApplicantRefundedAsync` | 同 method |
| 6 | `BuildFinanceDeptMessage` | 財務部撥款通知 | 已綁 LINE 的財務部成員 | `NotifyFinanceDeptAsync` | 同 method |
| 7 | `BuildRefundMessage` | 預支沖銷超額通知 | 已綁 LINE 的財務部成員 | `NotifyFinanceRefundAsync` | 同 method |
| 8 | `BuildTravelRefundMessage` | 出差沖銷超額通知 | 已綁 LINE 的財務部成員 | `NotifyFinanceTravelRefundAsync` | 同 method |

### 4.2 打卡提醒（1 種，僅 LINE）

| # | Build 方法 | 用途 | 收件人 | 觸發時機 |
|---|-----------|------|--------|---------|
| 9 | `BuildAttendanceReminderMessage` | 上下班打卡提醒 | 已綁 LINE 的員工 | TimerTrigger（cron 由 `AttendanceReminderCron` 控制）於上下班前 2 分鐘起算的 10 分鐘時間窗內命中時推播，一天一槽一次 |

---

## 5. Email × LINE 對照表

| 場景 | Email | LINE | 受開關控制 |
|------|:----:|:----:|:--------:|
| 待審核（一般） | ✅ | ✅ | ✅ |
| 待審核（指定 / 升級 / 代理） | ✅ | ✅ | ✅ |
| 審核結果（核准 / 退回 / 拒絕） | ✅ | ✅ | ✅ |
| 撥款完成（給申請人） | ✅ | ✅ | ✅ |
| 退款完成（給申請人） | ✅ | ✅ | ✅ |
| 撥款可執行（給財務） | ✅ | ✅ | ✅ |
| 預支沖銷超額（給財務） | ✅ | ✅ | ✅ |
| 出差沖銷超額（給財務） | ✅ | ✅ | ✅ |
| 帳號建立通知 | ✅ | ❌ | ❌ |
| 薪資明細 | ✅ | ❌ | ❌ |
| 打卡提醒（上下班前 2 分鐘） | ❌ | ✅ | ❌ |

---

## 6. 打卡提醒專章

詳見 [attendance-reminder.md](attendance-reminder.md)。重點：

- **觸發**：`AttendanceReminderFunction` (TimerTrigger)，cron 由 `AttendanceReminderCron` app setting 控制，預設限定台北時間 7-9 點與 16-18 點每分鐘執行一次
- **判斷時點**：以 `WorkStartTime` / `WorkEndTime` 為準，提醒提前 2 分鐘；命中條件為**時間窗** `[提前 2 分, +10 分)`（吸收冷啟動延遲），同一槽一天只發一次由 `batchStart` 冪等閘保證
- **過濾條件**：非週末 + 當日無覆蓋該時刻的請假記錄 + 有 LineUserId
- **持久化**：每次執行與每筆推播結果寫入 `AttendanceReminderLogs`（Superadmin 可於前端 `/admin/attendance-reminder-logs` 查詢）
- **推播間隔**：100ms（避免觸發 LINE 速率限制）
- **不受 `ApprovalLineEnabled` 影響**

---

## 7. 設定參考

### Email（SMTP）— `local.settings.json`

| 鍵 | 用途 |
|---|------|
| `Smtp:Host` | SMTP 伺服器 |
| `Smtp:Port` | Port |
| `Smtp:Username` | 帳號 |
| `Smtp:Password` | 密碼 |
| `Smtp:From` | 寄件人地址 |
| `Smtp:EnableSsl` | 是否啟用 SSL |

### LINE — `local.settings.json`（雙底線命名）

| 鍵 | 用途 |
|---|------|
| `Line__LoginChannelId` | LINE Login Channel ID |
| `Line__LoginChannelSecret` | LINE Login Channel Secret |
| `Line__MessagingChannelAccessToken` | Messaging API Long-lived Token |
| `Line__MessagingChannelSecret` | Messaging API Channel Secret |
| `Line__CallbackUrl` | OAuth callback URL |

> 詳見 [backend-design.md §16 環境變數慣例](../backend-design.md#16-環境變數慣例)。

---

## 8. HTML / Flex Message 風格

- **Email HTML**：CIS 品牌綠 `#699F34` 標頭、`#F5F2ED` 背景、`#525358` 主文字、`#A39685` 註腳
- **特殊主題**：撥款完成 Email 使用深綠 `#4A6B3A`；退款超額用琥珀 `#B8892A`
- **LINE Flex Message**：所有訊息頭部使用 `#699F34`（與 Email 一致）

---

## 9. 跨業務關聯

- **簽核通知觸發時機（提交 / 進階審核 / 撥款 / 退款）** → [approval-flow.md](approval-flow.md)
- **LINE 綁定流程與好友狀態檢查** → [line-integration.md](line-integration.md)
- **打卡提醒 cron 與失敗分類** → [attendance-reminder.md](attendance-reminder.md)
- **薪資明細產生公式** → [payroll-formula.md](payroll-formula.md)
- **API 端點清單** → [../api-routes.md](../api-routes.md)
- **SystemSetting 欄位** → [../database-schema.md](../database-schema.md)
