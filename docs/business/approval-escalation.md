# 簽核升級機制（Escalation）

當簽核步驟設定 `UseApplicantDepartment = true` 且申請人本身就是該步驟的審核者（自審情境，例如部門主管送出申請），系統會根據申請類型自動往上層部門尋找合適的審核者，而非自動核准。

> 簽核流程主軸（簽核步驟、自審跳過規則、跨步驟去重）見 [approval-flow.md](approval-flow.md)。

## 各申請類型的升級規則

| | 加班 | 請假 | 出差 | 請款 | 預支 | 沖銷 |
|---|---|---|---|---|---|---|
| 往上層部門找主管 | ✓ | ✓ | ✓ | ✗（自動跳過） | ✗（自動跳過） | ✗（自動跳過） |
| 主管請假時找代理人 | ✓ | ✗ | ✗ | — | — | — |
| 遞迴往上 | ✓ | ✓ | ✓ | — | — | — |
| 停在總監之前 | ✓ | ✓ | ✗ | — | — | — |
| 找不到人時 | 報錯 | 報錯 | 報錯 | — | — | — |

## 升級流程（以加班為例）

```
部門主管送出加班申請
  → Step 1 設定為 UseApplicantDepartment=true, JobTitleId=4
  → 偵測到「自己審自己」→ 觸發升級
  → 找上層部門（ParentId）的部門主管（JobTitleId=4）
    → 找到且未請假 → 由該主管審核
    → 找到但請假中 → 找該主管的代理人（AgentUserId）
      → 有代理人 → 由代理人審核（ApprovalRecord 標記 OnBehalfOfUserId）
      → 無代理人 → 繼續往上層部門找（遞迴）
    → 沒找到 → 繼續往上層部門找
  → 到達總監（JobTitleId=5）前停止
  → 都找不到 → 拋出錯誤「找不到可審核的主管，無法送出申請」
```

## 關鍵元件

| 元件 | 說明 |
|------|------|
| `EscalationService.cs` | 核心升級邏輯：遞迴往上層部門找主管、檢查請假、找代理人 |
| `ApprovalFlowService.cs` | 自審時呼叫 EscalationService（非 payment_request 類型） |
| `EscalationOverride` 資料表 | 記錄升級指派（審核者 + 代理誰），供 Dapper 查詢與 AuthorizeStep 使用 |
| `ApprovalRecord.OnBehalfOfUserId` | 代理審核標記（代替誰審核） |
| `ApprovalRecord.IsEscalated` | 是否為升級審核 |

## 請假中判斷

查詢 `LeaveRequests` 表中 `ApprovalStatus = 'approved'` 且 `StartDate <= 今天 <= EndDate` 的記錄。僅加班申請的升級流程會檢查。

## 前端顯示

簽核流程時間軸中，升級審核的紀錄會顯示：
- 代理審核：`代理 XXX`（棕色 badge）
- 直接升級：`升級審核`（紫色 badge）

---

## 跨業務關聯

- **簽核流程主軸**（請款自審跳過、上層級審核、指定審核、跨步驟去重） → [approval-flow.md](approval-flow.md)
- **9 種申請表的自審分組** → [application-forms.md](application-forms.md)
- **跨步驟去重的 supervisorIds（總監）排除** → [approval-flow.md §跨步驟同人去重](approval-flow.md#跨步驟同人去重限縮總監-or-相鄰-step)
- **EscalationOverride / ApprovalRecord Entity** → [database-schema.md](../database-schema.md)
