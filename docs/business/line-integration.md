# LINE 整合

## 功能範圍

- **LINE 帳號綁定**：員工在右上角 profile dropdown 透過 LINE OAuth 綁定 LINE userId
- **LINE 簽核通知推播**：6 種簽核通知同時推播 LINE Flex Message（Email 保留）
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

**8 種推播類型**：
1. `BuildReviewerMessage` — 待審核通知
2. `BuildApplicantResultMessage` — 審核結果（核准/退回/拒絕）
3. `BuildSpecificReviewerMessage` — 指定/升級/代理審核者通知
4. `BuildFinanceDeptMessage` — 財務撥款通知
5. `BuildRefundMessage` — 預支沖銷超額通知
6. `BuildTravelRefundMessage` — 出差沖銷超額通知
7. `BuildApplicantPaidMessage` — 撥款完成通知申請人（請款 / 預支 / 出差預支 / 出差請款）
8. `BuildApplicantRefundedMessage` — 退款完成通知申請人（預支 / 出差預支）

## 涉及元件

| 元件 | 說明 |
|------|------|
| `User.LineUserId` / `User.LineLinkedAt` | Entity 欄位 |
| `ILineService` / `LineService` | LINE API 封裝（token 換取、推播、好友狀態查詢） |
| `ILineService.IsBotFriendAsync` | 呼叫 `GET /v2/bot/profile/{userId}` 判斷是否為 OA 好友 |
| `LineFlexMessageBuilder` | 8 種 Flex Message 模板（品牌綠 #699F34 標頭） |
| `LineHandler` | 4 個 API：bind-url / bind / unbind / binding-status（後 3 者回傳 IsBotFriend） |
| `LineBindingStatusDto` | `(IsBound, LineLinkedAt, IsBotFriend)` |
| `ApprovalNotificationService` | 6 個通知方法各加入 LINE 推播 |
| 前端 `LineService` | `core/auth/services/line.service.ts`（共享 `isBound` / `isBotFriend` signal） |
| 前端 `ProfileDropdown` | 三態綁定 UI（未綁定 / 已綁定未加好友 / 已綁定為好友） |
| 前端 `LineBindCallback` | OAuth callback 頁面 |

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

- **打卡提醒透過 LINE 推播**（含失敗分類） → [attendance-reminder.md](attendance-reminder.md)
- **簽核通知觸發時機（撥款 / 退款 / 待審 / 結果）** → [approval-flow.md](approval-flow.md)
- **API 端點清單**（4 個 LINE 端點） → [api-routes.md §LINE 綁定](../api-routes.md#line-綁定)
- **環境變數雙底線慣例** → [backend-design.md §16](../backend-design.md#16-環境變數慣例)
