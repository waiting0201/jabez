# 簽核升級機制（Escalation）

升級機制有**兩個觸發來源**，兩者都是「往上層部門找人」，但入口與失敗行為不同：

1. **自審升級**（原有）：簽核步驟設定 `UseApplicantDepartment = true` 且申請人本身就是該步驟的審核者（例如部門主管送出申請），系統依申請類型往上層部門找主管，而非自動核准。
2. **上層級關卡無人接手**（2026-09 新增）：`UseDirectSupervisor` 步驟在申請人所屬部門找不到更高階者時，沿 `ParentId` 往上層部門找。**全部 9 種申請類型適用**，找不到時退回「跳過該關」而非報錯。**指派前會先排除「流程後續固定關卡本來就會簽到的人」**（見下方 §不與後續關卡撞人）。

> 簽核流程主軸（簽核步驟、自審跳過規則、跨步驟去重）見 [approval-flow.md](approval-flow.md)。
> 上層級關卡的完整規則見 [approval-flow.md §上層級審核模式](approval-flow.md#上層級審核模式usedirectsupervisor)。

## 兩個來源的差異

| | 自審升級 `TryEscalateAsync` | 上層級無人 `FindSuperiorInAncestorDepartmentsAsync` |
|---|---|---|
| 觸發條件 | `UseApplicantDepartment` 步驟 + 申請人即審核者 | `UseDirectSupervisor` 步驟 + 同部門無更高階者 |
| 適用申請類型 | 加班 / 請假 / 銷假 / 出差（請款類自動跳過） | **全部 9 種** |
| 找人條件 | 上層部門中職稱符合 `step.JobTitleId` 者 | 上層部門中 `JobTitle.Level < 申請人 Level` 者，取最接近的一位（同職級多人再依 `HireDate` → `Id` 排序，確保決定性） |
| 停在總監前 | 請假 / 銷假 / 加班會停 | **不停**（否則部門最高主管仍無人可審）；改以「不與後續關卡撞人」避免提前拉進總監 |
| 排除後續關卡人選 | ✗ | ✓（`laterStepScopes`，見下） |
| 在職過濾 | `Status = 'active'` | `Status = 'active'` |
| 找不到人時 | **丟 400 擋下送出** | **回 null → 維持跳過該關**（行為只增不減） |
| 共通產出 | `EscalationOverride` + `ApprovalRecord.IsEscalated` + 通知該員 | 同左 |

## 不與後續關卡撞人（`laterStepScopes`，2026-09）

上層級 fallback 的語意是「**這關沒人 → 找個更高的人補位**」。若流程**後面本來就有固定關卡會簽到那個人**，這一關就該維持跳過 —— 否則同一人得連簽兩關，而且會撞上總監的跨步驟去重而卡死：

```
（修正前）品牌事業部協理送請款 item 17
  Step1 上層級 → 同部門無更高階 → 升級指派「總監」
  …
  Step5 固定「總監室 / 總監」
    → SkipUnreviewableStepsAsync 的總監分支要求「全池皆已審」（總監室 2 位總監只有 1 位審過）
    → Step1 與 Step5 不相鄰，放寬條件也不成立 → 不自動跳過，停在 Step5
    → 該總監再簽 → AuthorizeStepAsync 丟 400「您已在先前步驟核准過此申請」
    → 只有另一位總監簽得掉 ＝ 卡死
```

故 `ApprovalFlowService` 在呼叫 fallback 時，會用 `BuildLaterFixedStepScopes()` 算出**該關之後的固定關卡審核者範圍**（部門 + 職稱，`StepReviewerScope`）傳入；候選人落在任一範圍就不指派、繼續往上找，全部被涵蓋則回 `null`＝維持跳過該關，由後面那一關把關。

**只有固定池關卡算數**（其餘都不保證真的有人接手，不能作為「這關可以跳過」的依據）：

| 後續步驟型態 | 是否納入 | 原因 |
|---|---|---|
| 固定部門 / 職稱（含 `UseApplicantDepartment`） | ✓ | 池明確，確定有人接手 |
| 被 `MinDays` 擋掉的步驟 | ✗ | 這張單根本不走這關 |
| 指定審核步驟 | ✗ | 人由申請人臨時點名，不保證是同一位 |
| 上層級步驟 | ✗ | 該關自身也可能無人 |
| 不限部門也不限職稱 | ✗ | 範圍等於全公司，會把所有候選人排除掉 |

實際效果（以本機資料模擬 208 個情境，衝突數 160 → 0）：

| 情境 | 結果 |
|---|---|
| 發展二部協理請假 2 天（Step2 / 3 被 MinDays 濾掉，後面無關卡） | 指派上層部門執行長 ✅ |
| 品牌事業部協理送請款（Step5 固定總監） | Step1 跳過 → Step2 部門協理 → … → Step5 總監簽 ✅ |
| 部門最高主管請假 ≥3 天（Step3 固定總監） | Step1 / Step2 跳過 → Step3 總監簽 ✅ |
| 直屬總監室的部門主管請假 <3 天（後面無關卡） | 指派總監 ✅（有人簽，不再自動核准） |

## 各申請類型的升級規則（自審升級）

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
| `EscalationService.TryEscalateAsync()` | 自審升級：遞迴往上層部門找主管、檢查請假、找代理人 |
| `EscalationService.FindSuperiorInAncestorDepartmentsAsync()` | 上層級關卡無人時往上層部門找更高階者（不判自審、不停在總監前、找不到回 null；含 `laterStepScopes` 排除與在職 / 決定性排序） |
| `ApprovalFlowService.BuildLaterFixedStepScopes()` | 算出「該關之後的固定關卡審核者範圍」供 fallback 排除（單一真相） |
| `StepReviewerScope` | 固定關卡的審核者範圍（部門 + 職稱）+ `Covers()` 判定 |
| `ApprovalFlowService.ResolveStartingStepAsync()` | 送單時解析：自審時呼叫 TryEscalateAsync（非 payment_request 類型）；上層級關卡無人時呼叫 FindSuperiorInAncestorDepartmentsAsync |
| `ApprovalFlowService.SkipUnreviewableStepsAsync()` | 簽核推進時解析：回傳值第 4 項 `escalation` 供 ApprovalTaskHandler 寫 override |
| `ApprovalTaskHandler.HasEscalationOverrideAsync()` | 授權共用：`UseDirectSupervisor` 分支與一般步驟分支皆以此放行升級審核者 |
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
