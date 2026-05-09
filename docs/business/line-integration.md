# LINE 整合

## 功能範圍

- **LINE 帳號綁定**：員工在右上角 profile dropdown 透過 LINE OAuth 綁定 LINE userId
- **LINE 簽核通知推播**：6 種簽核通知同時推播 LINE Flex Message（Email 保留）
- **LINE 推播用量監控**：Dashboard 顯示本月已用 / 月度上限 + 補額度連結（管理者專用）
- LINE Login 僅用於取得 userId 進行綁定，不作為登入方式
- 不做 LIFF、不做 Webhook

## 綁定流程

```
1. 用戶在 profile dropdown 點擊「綁定 LINE」
2. 前端呼叫 GET /line/bind-url → 取得 LINE OAuth URL + state
   (URL 含 bot_prompt=aggressive，授權後自動導向「加 OA 為好友」畫面)
3. 前端存 state 到 sessionStorage，導向 LINE 授權頁
4. 用戶在 LINE 授權 → 接著進入「加 OA 為好友」畫面 → 回導 /line/bind-callback?code=xxx&state=yyy
5. 前端驗證 state → POST /line/bind（帶 JWT + code）
6. 後端用 code 向 LINE 換取 id_token → 驗證取得 userId → 寫入 User.LineUserId
   後端並呼叫 GET /v2/bot/profile/{userId} 檢查好友狀態，回傳 IsBotFriend
7. 導回 dashboard，profile dropdown 依三態顯示：
   - 未綁定：顯示「綁定 LINE」按鈕
   - 已綁定 + OA 好友：顯示「LINE 已綁定」
   - 已綁定 + 非 OA 好友：顯示警告提示 +「加入好友」按鈕 +「重新檢查」
```

> **為何一定要加 OA 為好友**：LINE Messaging API `push-message` 硬性規定接收者必須已加 OA 為好友，否則 LINE 會回 HTTP 400 `The user hasn't added the LINE Official Account as a friend, or the LINE Official Account has been blocked by the user.`，推播一律失敗（只在 log 留錯誤訊息，Email 不受影響）。

## LINE 通知推播

簽核通知在 Email 發送後，自動查詢收件人的 `LineUserId`，有綁定則推播 Flex Message。推播失敗不影響 Email。

`LineService.PushMessageAsync` 會偵測 LINE 回應 body，若發現「未加好友 / 已封鎖」錯誤，會以 `LogError` 明確記錄原因（其他錯誤維持 warning），方便排查。

**9 種推播類型**：
1. `BuildReviewerMessage` — 待審核通知
2. `BuildApplicantResultMessage` — 審核結果（核准/退回/拒絕）
3. `BuildSpecificReviewerMessage` — 指定/升級/代理審核者通知
4. `BuildFinanceDeptMessage` — 財務撥款通知（最終核准請款 / 預支 / 出差預支 / 出差請款 時觸發）
5. `BuildRefundMessage` — 預支沖銷超額通知
6. `BuildTravelRefundMessage` — 出差沖銷超額通知
7. `BuildApplicantPaidMessage` — 撥款完成通知申請人（請款 / 預支 / 出差預支 / 出差請款）
8. `BuildApplicantRefundedMessage` — 退款完成通知申請人（預支 / 出差預支）
9. `BuildAttendanceReminderMessage` — 上下班打卡提醒（cron 觸發，不受簽核通知開關影響）

> **完整 Email × LINE 對照、開關控制範圍** → [notifications.md](notifications.md)

## LINE 推播用量監控

LINE Messaging API 採每月「免費 + 加購」配額制（type=`limited`），超量後 push 將失敗，會直接影響 9 種通知送達率。為避免管理者要等到通知漏發才察覺額度耗盡，Dashboard 在「定位資訊」卡片下方提供 **LINE 推播用量** 卡片：

- **進度條 + 已用 / 上限**（< 70% 綠 / 70~89% 黃 / ≥ 90% 紅）
- **剩餘可發送則數 + 已用百分比**
- 一鍵連到 [https://manager.line.biz/](https://manager.line.biz/) 補額度

**權限**：`line-quota:read`（Permission Id `74`，Module「LINE 整合」）；Superadmin 自動通過。指派給特定 admin 角色即可控制可見範圍，未授權者卡片完全不顯示。

**呼叫流程**：
```
1. Dashboard ngOnInit() 內若 hasPermission('line-quota:read') 為 true 才呼叫
2. GET /api/line/quota → AppRouter.GetRequiredPermission 守門 (line-quota:read)
3. LineHandler.GetQuotaAsync → ILineService.GetMessageQuotaAsync
4. LineService.GetMessageQuotaAsync 並行打：
   - GET https://api.line.me/v2/bot/message/quota          → { type, value }
   - GET https://api.line.me/v2/bot/message/quota/consumption → { totalUsage }
5. 合併為 LineQuotaDto(Type, Limit, Used, Remaining) 回傳
6. 任一上游 API 失敗 → null → handler 回 502 → 前端顯示「無法取得用量」+ 補額度連結
```

**不快取、不入庫**：每次開 Dashboard 都即時呼叫 LINE API。LINE 不收 quota 查詢費用，且管理者開 Dashboard 的頻率極低，無需建表同步。

**type=none**（無上限方案）時：卡片只顯示「此 LINE 帳號為無上限方案」+ 連結，省略進度條。

## 涉及元件

| 元件 | 說明 |
|------|------|
| `User.LineUserId` / `User.LineLinkedAt` | Entity 欄位 |
| `ILineService` / `LineService` | LINE API 封裝（token 換取、推播、好友狀態查詢、月度 quota 查詢） |
| `ILineService.IsBotFriendAsync` | 呼叫 `GET /v2/bot/profile/{userId}` 判斷是否為 OA 好友 |
| `ILineService.GetMessageQuotaAsync` | 並行呼叫 `/v2/bot/message/quota` + `/v2/bot/message/quota/consumption`，合併為 `LineQuotaDto` |
| `LineFlexMessageBuilder` | 8 種 Flex Message 模板（品牌綠 #699F34 標頭） |
| `LineHandler` | 5 個 API：bind-url / bind / unbind / binding-status / quota |
| `LineBindingStatusDto` | `(IsBound, LineLinkedAt, IsBotFriend)` |
| `LineQuotaDto` | `(Type, Limit, Used, Remaining)` — Dashboard 用量卡片 |
| `ApprovalNotificationService` | 6 個通知方法各加入 LINE 推播 |
| 前端 `LineService` | `core/auth/services/line.service.ts`（共享 `isBound` / `isBotFriend` signal） |
| 前端 `LineQuotaService` | `features/dashboard/services/line-quota.service.ts`（Dashboard 用量卡片） |
| 前端 `ProfileDropdown` | 三態綁定 UI（未綁定 / 已綁定未加好友 / 已綁定為好友） |
| 前端 `LineBindCallback` | OAuth callback 頁面 |
| 前端 `Dashboard` | LINE 推播用量卡片（位於定位資訊下方，`canViewLineQuota()` 守門） |

## LINE 設定

**後端** `local.settings.json`（雙底線命名）：
- `Line__LoginChannelId` — LINE Login Channel ID
- `Line__LoginChannelSecret` — LINE Login Channel Secret
- `Line__MessagingChannelAccessToken` — Messaging API Long-lived Token
- `Line__MessagingChannelSecret` — Messaging API Channel Secret
- `Line__CallbackUrl` — OAuth callback URL

**前端** `environment.ts`：
- `lineLoginChannelId` — LINE Login Channel ID
- `lineCallbackUrl` — OAuth callback URL
- `lineOaFriendUrl` — LINE OA 加好友 URL（格式 `https://line.me/R/ti/p/@{basicId}`），供「已綁定但未加好友」狀態下的「加入好友」按鈕使用

> **重要**：
> - LINE Login 和 Messaging API 須在同一 Provider 下建立，LINE 才會使用相同 userId。
> - OAuth URL 必須帶 `bot_prompt=aggressive` 參數（已內建於 `LineHandler.GetBindUrlAsync`），綁定後 LINE 才會自動導向「加 OA 為好友」畫面；否則用戶只綁定 Login 但未加好友，所有 Messaging API 推播一律失敗。

---

## 跨業務關聯

- **完整通知清單（Email + LINE 對照、系統開關）** → [notifications.md](notifications.md)
- **打卡提醒透過 LINE 推播**（含失敗分類） → [attendance-reminder.md](attendance-reminder.md)
- **簽核通知觸發時機（撥款 / 退款 / 待審 / 結果）** → [approval-flow.md](approval-flow.md)
- **API 端點清單**（5 個 LINE 端點：bind-url / bind / unbind / binding-status / quota） → [api-routes.md §LINE 綁定 / 推播用量](../api-routes.md#line-綁定--推播用量)
- **環境變數雙底線慣例** → [backend-design.md §16](../backend-design.md#16-環境變數慣例)
